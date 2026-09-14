//+:cnd:noEmit
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;
//#if (advancedTests == true)
using Boilerplate.Tests.Features.DevMcp;
//#endif

namespace Boilerplate.Tests.Features.Identity.OAuth;

/// <summary>
/// The operator's view of authorized applications. Only dynamic registrations have rows, so a screen listing that
/// table would look complete while omitting every modern client, which is stored nowhere.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class OAuthClientManagementTests
{
    public TestContext TestContext { get; set; } = default!;

    private const string TestRedirectUri = "http://127.0.0.1:33418/callback";

    /// <summary>
    /// Unique per test: the suite shares one database and this screen reports across every user, so a fixed id would
    /// make one test's grants visible to another's assertions.
    /// </summary>
    private readonly string testClientId = $"managed-test-client-{Guid.NewGuid():N}";

    private void ConfigureTestClient(ConfigurationManager configuration)
    {
        configuration["OAuth:Clients:0:ClientId"] = testClientId;
        configuration["OAuth:Clients:0:ClientName"] = "Configured Test Client";
        configuration["OAuth:Clients:0:RedirectUris:0"] = TestRedirectUri;
    }

    /// <summary>
    /// A client with a grant but no stored registration still has to appear, or revoking it is impossible from here.
    /// </summary>
    [TestMethod]
    public async Task TheClientList_Should_IncludeAClientThatHasNoStoredRegistration()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        // Stands in for a metadata-document client: a grant whose client id matches nothing stored. Marked on this
        // test's own session, found from its access token - the newest row belongs to whoever signed in last.
        var selfDescribedClientId = $"https://example.test/{Guid.NewGuid():N}/client.json";
        var sessionId = IAuthTokenProvider.ParseAccessToken(await DevMcpTestUtils.AccessToken(scope), validateExpiry: false).GetSessionId();

        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.OAuthGrants.AddAsync(new OAuthGrant
            {
                UserSessionId = sessionId,
                ClientId = selfDescribedClientId,
                Scope = OAuthScopes.DevMcp,
                Resource = "https://example.test/dev-mcp",
                RefreshTokenId = Guid.CreateVersion7()
            }, TestContext.CancellationToken);
            await dbContext.SaveChangesAsync(TestContext.CancellationToken);
        }

        var clients = await scope.ServiceProvider.GetRequiredService<IOAuthClientManagementController>()
                                                 .GetAllClients(TestContext.CancellationToken);

        var selfDescribed = clients.SingleOrDefault(client => client.ClientId == selfDescribedClientId);

        Assert.IsNotNull(selfDescribed,
            "A client holding a grant must be listed even though nothing about it is stored - that is every client " +
            "that uses a metadata document, which is every modern one.");

        Assert.AreEqual(OAuthClientKind.MetadataDocument, selfDescribed.Kind);
        Assert.AreEqual("example.test", selfDescribed.Origin,
            "The origin is the part such a client cannot forge, so it is what the operator is shown.");
        Assert.AreEqual(1, selfDescribed.ActiveGrants);

        Assert.IsNotNull(clients.SingleOrDefault(client => client.ClientId == testClientId && client.Kind is OAuthClientKind.Configured),
            "A client an operator declared in configuration is listed too, and marked as the only vouched-for kind.");
    }

    /// <summary>
    /// Revoking is the whole point of the screen: it has to actually end access, not just tidy a list.
    /// </summary>
    [TestMethod]
    public async Task RevokingAClient_Should_EndItsAccessForEveryUser()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>().Approve(new()
        {
            ClientId = testClientId,
            RedirectUri = TestRedirectUri,
            ResponseType = "code",
            Scope = OAuthScopes.DevMcp,
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Resource = $"{server.WebAppServerAddress.ToString().TrimEnd('/')}/dev-mcp"
        }, TestContext.CancellationToken);

        var code = QueryHelpers.ParseQuery(new Uri(approval.RedirectUrl).Query)["code"].ToString();

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var tokenResponse = await httpClient.PostAsync("oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = testClientId,
            ["redirect_uri"] = TestRedirectUri,
            ["code_verifier"] = verifier
        }), TestContext.CancellationToken);

        var granted = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(TestContext.CancellationToken)).RootElement;
        var refreshToken = granted.GetProperty("refresh_token").GetString()!;

        var management = scope.ServiceProvider.GetRequiredService<IOAuthClientManagementController>();

        Assert.AreEqual(1, (await management.GetAllClients(TestContext.CancellationToken))
                              .Single(client => client.ClientId == testClientId).ActiveGrants);

        var revoked = await management.RevokeGrants(new() { ClientId = testClientId }, TestContext.CancellationToken);

        Assert.AreEqual(1, revoked);

        // The proof is not the count going to zero, it is the credential no longer working.
        using var afterRevoke = await httpClient.PostAsync("oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = testClientId
        }), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, afterRevoke.StatusCode,
            "Revoking has to end access, so the refresh token issued to this client must stop working.");

        Assert.AreEqual(0, (await management.GetAllClients(TestContext.CancellationToken))
                              .Single(client => client.ClientId == testClientId).ActiveGrants);
    }
}
