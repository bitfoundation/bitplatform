using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Antiforgery;

/// <summary>
/// Server.Api's AutoCsrfProtectionFilter lets a non-safe request skip the antiforgery token only when it is JSON (which
/// a cross-site page cannot send without a CORS preflight) or carries an Authorization header (which a browser never
/// adds by itself). Anything else - a form, a multipart upload - authenticated by the access_token cookie alone is
/// exactly what a cross-site form post looks like, so it needs the antiforgery token of the user it claims to be.
/// <para>
/// Against Sales: its API runs inside Server.Web, so the pages that issue tokens and the API that checks them share
/// one set of data protection keys.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class AntiforgeryTests
{
    private const string password = "123456";

    /// <summary>AutoCsrfProtectionFilter's own refusal.</summary>
    private const string refusal = "Anti-forgery token validation failed";

    /// <summary>
    /// ChatbotController.TranscribeSpeech is [Authorize], non-JSON, and answers an empty recording with a
    /// BadRequestException before calling any speech provider - so passing the filter costs nothing and writes nothing.
    /// </summary>
    private const string transcribeSpeech = "api/v1/Chatbot/TranscribeSpeech";

    /// <summary>
    /// The endpoint's own refusal, as problem details keyed by the exception (production leaves out its "No recording
    /// provided" reason); the filter's refusal is plain text instead.
    /// </summary>
    private const string endpointRefusalKey = $"\"key\":\"{nameof(BadRequestException)}\"";

    /// <summary>The name EditForm gives the hidden token input, which antiforgery also reads from a multipart body.</summary>
    private const string tokenFieldName = "__RequestVerificationToken";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task NonJsonPost_Should_NeedTheAntiforgeryTokenIssuedToTheSameUser()
    {
        await SkipWithoutGlobalAdminCredentials();

        var email = $"e2e-{Guid.NewGuid():N}"[..14] + "@bitplatform.dev";
        var userId = await CreateUser(email);

        try
        {
            var accessToken = await SignIn(email);
            var accessTokenCookie = $"access_token={accessToken}";

            // ---- Control: with an Authorization header the filter steps aside, and the endpoint itself answers ----
            using (var response = await PostEmptyRecording(bearer: accessToken))
                await ExpectPastTheFilter(response, "with the access token as a bearer header");

            // ---- The access_token cookie alone, no token: what a cross-site form post carries ----
            using (var response = await PostEmptyRecording(cookies: accessTokenCookie))
                await ExpectRefused(response, "with the access_token cookie and no antiforgery token");

            // ---- A real token, but issued to an anonymous visitor: bound to another (no) user ----
            var anonymous = await GetFormToken("en-US/sign-in", cookies: null);
            using (var response = await PostEmptyRecording(cookies: $"{accessTokenCookie}; {anonymous.Cookie}", token: anonymous.Token))
                await ExpectRefused(response, "with an antiforgery token issued to an anonymous visitor");

            // ---- The token Server.Web rendered for this user (the profile form of the settings page) ----
            var own = await GetFormToken("en-US/settings", cookies: accessTokenCookie);
            using (var response = await PostEmptyRecording(cookies: $"{accessTokenCookie}; {own.Cookie}", token: own.Token))
                await ExpectPastTheFilter(response, "with the antiforgery token issued to this user");
        }
        finally
        {
            await DeleteUser(userId);
        }
    }

    private async Task ExpectRefused(HttpResponseMessage response, string what)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"A multipart post {what} answered: {body}");
        Assert.Contains(refusal, body, $"A multipart post {what} should be refused by AutoCsrfProtectionFilter.");
    }

    private async Task ExpectPastTheFilter(HttpResponseMessage response, string what)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.DoesNotContain(refusal, body, $"A multipart post {what} should get past AutoCsrfProtectionFilter.");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"A multipart post {what} answered: {body}");
        Assert.Contains(endpointRefusalKey, body, $"A multipart post {what} should reach TranscribeSpeech itself.");
    }

    /// <summary>An empty file part, plus the token as a form field when given - a form post, as a browser would send it.</summary>
    private async Task<HttpResponseMessage> PostEmptyRecording(string? bearer = null, string? cookies = null, string? token = null)
    {
        using var form = new MultipartFormDataContent();
        var recording = new ByteArrayContent([]);
        recording.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(recording, name: "file", fileName: "recording.wav");

        if (token is not null)
            form.Add(new StringContent(token), tokenFieldName);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(DeployedApps.Sales), transcribeSpeech)) { Content = form };

        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        if (cookies is not null)
            request.Headers.Add("Cookie", cookies);

        return await Send(request);
    }

    /// <summary>
    /// A server-rendered EditForm carries its antiforgery token as a hidden input, and the response that rendered it
    /// sets the matching cookie - on a page that is not served from a shared cache (See SharedResponseCacheCompatibleAntiforgery).
    /// </summary>
    private async Task<(string Cookie, string Token)> GetFormToken(string path, string? cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(DeployedApps.Sales), path));

        if (cookies is not null)
            request.Headers.Add("Cookie", cookies);

        using var response = await Send(request);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{path} answered {(int)response.StatusCode}.");

        var cookie = response.Headers.TryGetValues("Set-Cookie", out var setCookies)
            ? setCookies.Select(c => c.Split(';')[0]).FirstOrDefault(c => c.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))
            : null;
        Assert.IsNotNull(cookie, $"{path} set no antiforgery cookie.");

        var match = TokenInput().Match(html);
        Assert.IsTrue(match.Success, $"{path} rendered no form with an antiforgery token.");

        return (cookie, WebUtility.HtmlDecode(match.Groups["token"].Value));
    }

    /// <summary>Cookies are this test's to write by hand, and a redirect is an answer of its own.</summary>
    private async Task<HttpResponseMessage> Send(HttpRequestMessage request)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(2) };

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }

    /// <summary>Through the app's own AuthManager, as the Sales client does; returns the access token it was given.</summary>
    private async Task<string> SignIn(string email)
    {
        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.Sales);
        var authManager = apiClient.Services.GetRequiredService<AuthManager>();

        await authManager.SignIn(new() { Email = email, Password = password }, TestContext.CancellationToken);

        var accessToken = await authManager.GetFreshAccessToken(requestedBy: nameof(AntiforgeryTests));
        Assert.IsFalse(string.IsNullOrEmpty(accessToken), $"Signing {email} in on Sales gave no access token.");

        return accessToken!;
    }

    private async Task<Guid> CreateUser(string email)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email.Split('@')[0],
            NormalizedUserName = email.Split('@')[0].ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedOn = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return user.Id;
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteUser(Guid userId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await db.UserSessions.IgnoreQueryFilters().Where(session => session.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(u => u.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
    }

    /// <summary>As AppTestBase's: the user is made through the global admin's database access.</summary>
    private static async Task SkipWithoutGlobalAdminCredentials()
    {
        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.AdminPanelApi);
        var configuration = apiClient.Services.GetRequiredService<IConfiguration>();

        if (string.IsNullOrWhiteSpace(configuration["GlobalAdminEmail"]) || string.IsNullOrWhiteSpace(configuration["GlobalAdminPassword"]))
            Assert.Inconclusive("'GlobalAdminEmail' / 'GlobalAdminPassword' are not in this project's user secrets, and this test creates its user through the global admin.");
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""")]
    private static partial Regex TokenInput();
}
