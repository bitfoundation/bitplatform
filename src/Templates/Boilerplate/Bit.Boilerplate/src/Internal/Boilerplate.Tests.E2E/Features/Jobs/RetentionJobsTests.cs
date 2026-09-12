using System.Net.Http.Headers;
using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Identity;
using Boilerplate.Server.Api.Features.Attachments;
using Boilerplate.Server.Api.Features.Identity.OAuth;
using Boilerplate.Server.Api.Features.PushNotification;
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Tests.E2E.Features.Jobs;

/// <summary>
/// The five recurring retention jobs, on the deployments that run them. Nothing else removes what they remove, so
/// each one is all that stands between an expired row and a device identifier, an ip and a city kept on file forever -
/// which is a retention the privacy notice promises on their behalf, and the export repeats section by section.
/// <para>
/// Every job gets two rows of its own kind: one backdated past its retention and one just made. Then the jobs are run
/// by hand through the Hangfire dashboard - the global admin's access_token cookie plus the antiforgery token the page
/// carries, since waiting for the daily schedule is not a test - and the expired half has to be gone while the fresh
/// half is untouched. A job that deletes everything passes only the first of those.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class RetentionJobsTests
{
    private const string password = "123456";

    /// <summary>
    /// AdminPanelApi, because this one runs the jobs rather than only looking at them. All three deployments share a
    /// database, so a row deleted here is the row that would be deleted anywhere.
    /// </summary>
    private const string host = DeployedApps.AdminPanelApi;

    /// <summary>A year back: every runner caps its batch and takes the oldest first, so this sorts to the front.</summary>
    private static readonly TimeSpan wellPastEveryRetention = TimeSpan.FromDays(365);

    private static readonly string[] retentionJobIds =
    [
        UserSessionsRetentionJobRunner.RecurringJobId,
        UnconfirmedUsersRetentionJobRunner.RecurringJobId,
        OAuthRetentionJobRunner.RecurringJobId,
        PushSubscriptionsRetentionJobRunner.RecurringJobId,
        AiChatImagesRetentionJobRunner.RecurringJobId
    ];

    private readonly List<Func<Task>> cleanups = [];

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// A job that was never scheduled does not run, and nothing anywhere says so - the app registers these at every
    /// start and the dashboard is the only place the result shows. Per deployment because the registration belongs to
    /// the pipeline, and an integrated api has a pipeline of its own: Server.Web's copy is what serves Sales, and it
    /// was missing the line that schedules them.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi (standalone api)")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi (standalone api)")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated api)")]
    public async Task EveryDeployment_Should_HaveTheRetentionJobsScheduled(string deployment)
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        await using var dashboard = await OpenRecurringJobs(deployment, TestContext.CancellationToken);

        var scheduled = ScheduledJobIds(dashboard.Html);

        foreach (var recurringJobId in retentionJobIds)
        {
            // Renaming a runner leaves the old schedule behind and registers nothing new (See ScheduleAppRecurringJobs).
            Assert.Contains(recurringJobId, scheduled,
                            $"{deployment} does not run '{recurringJobId}', so that retention is not being enforced there. It schedules: [{string.Join(", ", scheduled)}].");
        }
    }

    [TestMethod]
    public async Task TheRetentionJobs_Should_RemoveWhatIsPastItsRetention_AndLeaveTheRest()
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var marker = Guid.NewGuid().ToString("N")[..10];
        var stale = DateTimeOffset.UtcNow - wellPastEveryRetention;
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"e2e-{marker}", Title = $"E2E {marker}" };
        var keeperEmail = $"e2e-keeper-{marker}@bitplatform.dev";

        // Registered before the writes, so a half-made fixture is undone too.
        RegisterForCleanup(() => DeleteFixture(tenant.Id));

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(cancellationToken);

        // ---- An account nobody ever confirmed, old enough that no lawful basis is left for holding it ----
        var unconfirmedUserId = await CreateUser(dbContext, tenant, $"e2e-unconfirmed-{marker}@bitplatform.dev", confirmed: false, createdOn: stale);

        // ---- And one in use, which every other fixture hangs off ----
        var keeperUserId = await CreateUser(dbContext, tenant, keeperEmail, confirmed: true, createdOn: DateTimeOffset.UtcNow);

        await using var expiringDevice = await SignIn(keeperEmail, cancellationToken);
        await using var keptDevice = await SignIn(keeperEmail, cancellationToken);

        var expiringSessionId = await GetSessionId(expiringDevice);
        var keptSessionId = await GetSessionId(keptDevice);

        // Each device subscribes for itself, the way the app does - the row's session is what a user-related push aims at.
        var expiringSubscription = NewPushSubscription($"{marker}-expiring");
        var keptSubscription = NewPushSubscription($"{marker}-kept");
        await expiringDevice.Services.GetRequiredService<IPushNotificationController>().Subscribe(expiringSubscription, cancellationToken);
        await keptDevice.Services.GetRequiredService<IPushNotificationController>().Subscribe(keptSubscription, cancellationToken);

        var expiringImageId = await UploadAiChatImage(keptDevice, cancellationToken);
        var keptImageId = await UploadAiChatImage(keptDevice, cancellationToken);

        var expiringCodeId = await CreateAuthorizationCode(dbContext, keeperUserId, expiresOn: stale, cancellationToken);
        var keptCodeId = await CreateAuthorizationCode(dbContext, keeperUserId, expiresOn: DateTimeOffset.UtcNow.AddMinutes(5), cancellationToken);

        // ---- Backdated where no endpoint can put them: every one of these is server-stamped ----
        await dbContext.UserSessions.IgnoreQueryFilters().Where(session => session.Id == expiringSessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.StartedOn, stale.ToUnixTimeSeconds())
                .SetProperty(session => session.RenewedOn, (long?)stale.ToUnixTimeSeconds()), cancellationToken);

        await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters().Where(sub => sub.DeviceId == expiringSubscription.DeviceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(sub => sub.ExpirationTime, stale.ToUnixTimeSeconds()), cancellationToken);

        await dbContext.Attachments.IgnoreQueryFilters().Where(attachment => attachment.Id == expiringImageId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(attachment => attachment.CreatedOn, stale), cancellationToken);

        // Everything is where it should be before the jobs run, or a pass proves nothing.
        Assert.AreEqual(HttpStatusCode.OK, await DeployedAttachments.StatusOf(host, expiringImageId, AttachmentKind.AiChatImage, cancellationToken),
                        "The AI chat image that is about to expire was never stored.");

        // ---- Triggered through the dashboard, then given time to actually run ----
        await TriggerRetentionJobs(cancellationToken);

        await WaitUntilGone(async () =>
        {
            await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(cancellationToken);

            List<string> leftovers = [];

            if (await db.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == unconfirmedUserId, cancellationToken))
                leftovers.Add($"the unconfirmed account {unconfirmedUserId}");

            if (await db.UserSessions.IgnoreQueryFilters().AnyAsync(session => session.Id == expiringSessionId, cancellationToken))
                leftovers.Add($"the expired session {expiringSessionId}");

            if (await db.PushNotificationSubscriptions.IgnoreQueryFilters().AnyAsync(sub => sub.DeviceId == expiringSubscription.DeviceId, cancellationToken))
                leftovers.Add($"the expired push subscription of device {expiringSubscription.DeviceId}");

            if (await db.Attachments.IgnoreQueryFilters().AnyAsync(attachment => attachment.Id == expiringImageId, cancellationToken))
                leftovers.Add($"the expired AI chat image {expiringImageId}");

            if (await db.OAuthAuthorizationCodes.IgnoreQueryFilters().AnyAsync(code => code.Id == expiringCodeId, cancellationToken))
                leftovers.Add($"the expired authorization code {expiringCodeId}");

            return leftovers;
        }, cancellationToken);

        // ---- What the jobs are not allowed to touch ----
        await using var afterJobs = await globalApiClient.DbContextFactory!.CreateDbContextAsync(cancellationToken);

        Assert.IsTrue(await afterJobs.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == keeperUserId, cancellationToken),
                      "An account in use was deleted as an unconfirmed one.");
        Assert.IsTrue(await afterJobs.UserSessions.IgnoreQueryFilters().AnyAsync(session => session.Id == keptSessionId, cancellationToken),
                      "A session that can still be renewed was deleted, which signs a device out for no reason.");
        Assert.IsTrue(await afterJobs.PushNotificationSubscriptions.IgnoreQueryFilters().AnyAsync(sub => sub.DeviceId == keptSubscription.DeviceId, cancellationToken),
                      "A live push subscription was deleted, so that device stops receiving notifications for no reason.");
        Assert.IsTrue(await afterJobs.Attachments.IgnoreQueryFilters().AnyAsync(attachment => attachment.Id == keptImageId, cancellationToken),
                      "An AI chat image was deleted before its retention was up.");
        Assert.IsTrue(await afterJobs.OAuthAuthorizationCodes.IgnoreQueryFilters().AnyAsync(code => code.Id == keptCodeId, cancellationToken),
                      "An authorization code was deleted while it was still exchangeable.");

        // ---- The blob, not just the row: dropping the row first strands a file nothing can name again ----
        Assert.AreEqual(HttpStatusCode.NotFound, await DeployedAttachments.StatusOf(host, expiringImageId, AttachmentKind.AiChatImage, cancellationToken),
                        "The expired AI chat image's row is gone but the image is still served.");
        Assert.AreEqual(HttpStatusCode.OK, await DeployedAttachments.StatusOf(host, keptImageId, AttachmentKind.AiChatImage, cancellationToken),
                        "The AI chat image inside its retention is no longer served.");
    }

    /// <summary>Runs the five jobs now, rather than waiting for a schedule that is daily and hourly.</summary>
    private async Task TriggerRetentionJobs(CancellationToken cancellationToken)
    {
        await using var dashboard = await OpenRecurringJobs(host, cancellationToken);

        var scheduled = ScheduledJobIds(dashboard.Html);

        foreach (var recurringJobId in retentionJobIds)
        {
            Assert.Contains(recurringJobId, scheduled, $"{host} has no recurring job named '{recurringJobId}' to trigger. It schedules: [{string.Join(", ", scheduled)}].");
        }

        using var form = new FormUrlEncodedContent(retentionJobIds.Select(id => KeyValuePair.Create("jobs[]", id)));
        using var trigger = new HttpRequestMessage(HttpMethod.Post, "hangfire/recurring/trigger") { Content = form };
        trigger.Headers.Add(MetaContent(dashboard.Html, "csrf-header"), MetaContent(dashboard.Html, "csrf-token"));

        using var triggered = await dashboard.HttpClient.SendAsync(trigger, cancellationToken);

        Assert.IsLessThan(400, (int)triggered.StatusCode, $"Triggering the retention jobs answered {(int)triggered.StatusCode}: {await triggered.Content.ReadAsStringAsync(cancellationToken)}");
    }

    /// <summary>
    /// The dashboard is behind <c>System.Jobs_Manage</c>, which only the global admin role implies, and the bearer
    /// handler reads the access_token cookie - which is how the app's diagnostics modal opens it in a browser tab.
    /// </summary>
    private static async Task<RecurringJobsPage> OpenRecurringJobs(string deployment, CancellationToken cancellationToken)
    {
        // The run's shared session is AdminPanelApi's, and a deployment only takes the tokens its own API issued.
        var ownSession = deployment is DeployedApps.AdminPanelApi
            ? null
            : await DeployedApiClientProvider.SignInGlobalAdminOn(deployment, cancellationToken);

        try
        {
            var services = (ownSession ?? await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken)).Services;
            var accessToken = await services.GetRequiredService<AuthManager>().GetFreshAccessToken(requestedBy: nameof(RetentionJobsTests));

            Assert.IsFalse(string.IsNullOrEmpty(accessToken), $"The global admin has no access token for {deployment}.");

            var cookies = new CookieContainer();
            cookies.Add(new Uri(deployment), new System.Net.Cookie("access_token", accessToken) { Path = "/" });

            var httpClient = new HttpClient(new HttpClientHandler { CookieContainer = cookies })
            {
                BaseAddress = new Uri(deployment),
                Timeout = TimeSpan.FromMinutes(2)
            };

            try
            {
                // This GET is also what hands over the antiforgery cookie a POST back has to carry.
                using var page = await httpClient.GetAsync("hangfire/recurring", cancellationToken);

                Assert.AreEqual(HttpStatusCode.OK, page.StatusCode, $"The global admin could not open {deployment}hangfire/recurring.");

                return new RecurringJobsPage(ownSession, httpClient, await page.Content.ReadAsStringAsync(cancellationToken));
            }
            catch
            {
                httpClient.Dispose();
                throw;
            }
        }
        catch
        {
            await SignOutAndDispose(ownSession);
            throw;
        }
    }

    /// <summary>The page, and the client that read it - its antiforgery cookie is half of what a POST back needs.</summary>
    private sealed class RecurringJobsPage(DeployedApiClient? ownSession, HttpClient httpClient, string html) : IAsyncDisposable
    {
        public HttpClient HttpClient { get; } = httpClient;

        public string Html { get; } = html;

        public async ValueTask DisposeAsync()
        {
            HttpClient.Dispose();

            await SignOutAndDispose(ownSession);
        }
    }

    /// <summary>A session of this test's own on that deployment, not the run's shared one.</summary>
    private static async ValueTask SignOutAndDispose(DeployedApiClient? ownSession)
    {
        if (ownSession is null)
            return;

        await ownSession.Services.GetRequiredService<AuthManager>().SignOut(CancellationToken.None);
        await ownSession.DisposeAsync();
    }

    /// <summary>
    /// The recurring jobs the page lists - the checkbox value is the id its own "Trigger now" posts back, so this is
    /// the same name on both sides of the request.
    /// </summary>
    private static string[] ScheduledJobIds(string html)
    {
        return [.. Regex.Matches(html, "name=\"jobs\\[\\]\"\\s+value=\"([^\"]+)\"", RegexOptions.IgnoreCase).Select(match => match.Groups[1].Value)];
    }

    /// <summary>The dashboard's own script reads its antiforgery pair out of these two meta tags.</summary>
    private static string MetaContent(string html, string name)
    {
        var match = Regex.Match(html, $"<meta\\s+name=\"{name}\"\\s+content=\"([^\"]*)\"", RegexOptions.IgnoreCase);

        Assert.IsTrue(match.Success, $"The dashboard page carries no '{name}' meta tag, so its POSTs cannot be answered.");

        return match.Groups[1].Value;
    }

    /// <summary>
    /// A triggered job is enqueued, not run: a Hangfire server has to pick it up, and there are five of them. What is
    /// still there when the wait is over is named, because "a retention job did not delete something" is only useful
    /// if it says which.
    /// </summary>
    private static async Task WaitUntilGone(Func<Task<List<string>>> whatIsStillThere, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            var leftovers = await whatIsStillThere();

            if (leftovers.Count is 0)
                return;

            if (DateTimeOffset.UtcNow > deadline)
            {
                // A runner that cannot delete swallows the failure and keeps the row on purpose, so the row being here
                // is all this sees - the deployment's log is where the reason is.
                Assert.Fail($"The retention jobs ran on {host}, but {string.Join(", ", leftovers)} {(leftovers.Count is 1 ? "is" : "are")} still in the database. Its log says why the runner skipped them.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private async Task<Guid> CreateUser(AppDbContext dbContext, Tenant tenant, string email, bool confirmed, DateTimeOffset createdOn)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email.Split('@')[0],
            NormalizedUserName = email.Split('@')[0].ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = confirmed,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedOn = createdOn
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        if (await dbContext.Tenants.IgnoreQueryFilters().AnyAsync(existing => existing.Id == tenant.Id, TestContext.CancellationToken) is false)
        {
            await dbContext.Tenants.AddAsync(tenant, TestContext.CancellationToken);
        }

        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        // Accepted, so signing in selects this tenant - which a push subscription then records as its own.
        await dbContext.TenantUsers.AddAsync(new TenantUser { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, AcceptedOn = DateTimeOffset.UtcNow }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return user.Id;
    }

    /// <summary>
    /// A code the way <c>OAuthService</c> stores one - the code itself is never kept, only its hash, so nothing here
    /// has to be exchangeable for the job to have something to find.
    /// </summary>
    private static async Task<Guid> CreateAuthorizationCode(AppDbContext dbContext, Guid userId, DateTimeOffset expiresOn, CancellationToken cancellationToken)
    {
        var code = new OAuthAuthorizationCode
        {
            Id = Guid.NewGuid(),
            CodeHash = Base64Url.EncodeToString(SHA256.HashData(Guid.NewGuid().ToByteArray())),
            ClientId = "e2e",
            RedirectUri = "http://127.0.0.1/callback",
            Resource = host,
            Scope = "openid",
            CodeChallenge = Base64Url.EncodeToString(SHA256.HashData(Guid.NewGuid().ToByteArray())),
            UserId = userId,
            ExpiresOn = expiresOn.ToUnixTimeSeconds()
        };

        await dbContext.OAuthAuthorizationCodes.AddAsync(code, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return code.Id;
    }

    /// <summary>A device of its own: one session each, so a fixture can be aimed at one and not the other.</summary>
    private static async Task<DeployedApiClient> SignIn(string email, CancellationToken cancellationToken)
    {
        var apiClient = DeployedApiClientProvider.CreateApiClientFor(host);

        try
        {
            await apiClient.Services.GetRequiredService<AuthManager>()
                .SignIn(new() { Email = email, Password = password, RememberMe = true }, cancellationToken);

            return apiClient;
        }
        catch
        {
            await apiClient.DisposeAsync();
            throw;
        }
    }

    private static async Task<Guid> GetSessionId(DeployedApiClient apiClient)
    {
        var accessToken = await apiClient.Services.GetRequiredService<IAuthTokenProvider>().GetAccessToken();

        return IAuthTokenProvider.ParseAccessToken(accessToken!, validateExpiry: false).GetSessionId();
    }

    /// <summary>The id is minted by the server, so it comes back in the answer rather than going out in the request.</summary>
    private static async Task<Guid> UploadAiChatImage(DeployedApiClient apiClient, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(TestImages.ProfilePicturePng());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "e2e-ai-chat.png");

        using var response = await apiClient.HttpClient.PostAsync("api/v1/Attachment/UploadAiChatImage", form, cancellationToken);

        Assert.IsTrue(response.IsSuccessStatusCode, $"UploadAiChatImage answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");

        return Guid.Parse((await response.Content.ReadAsStringAsync(cancellationToken)).Trim('"'));
    }

    /// <summary>A subscription the server will accept; See PersonalDataJourneyTests for what each field is.</summary>
    private static PushNotificationSubscriptionDto NewPushSubscription(string marker)
    {
        using var keyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = keyPair.PublicKey.ExportParameters().Q;

        return new()
        {
            DeviceId = $"e2e-{marker}",
            Platform = "browser",
            Endpoint = $"https://fcm.googleapis.com/fcm/send/e2e-{marker}",
            P256dh = Base64Url.EncodeToString([0x04, .. publicKey.X!, .. publicKey.Y!]),
            Auth = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16))
        };
    }

    private void RegisterForCleanup(Func<Task> cleanup) => cleanups.Add(cleanup);

    /// <summary>
    /// Not on the test's own token: a canceled or timed out test still owes the deployment its cleanup. The AI chat
    /// images are left to their own job - nothing here can reach the blob, and stranding it is worse than keeping the
    /// row that names it for another hour.
    /// </summary>
    private static async Task DeleteFixture(Guid tenantId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var userIds = await dbContext.TenantUsers.IgnoreQueryFilters().Where(membership => membership.TenantId == tenantId)
            .Select(membership => membership.UserId).ToArrayAsync(CancellationToken.None);

        await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters().Where(sub => sub.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.OAuthAuthorizationCodes.IgnoreQueryFilters().Where(code => userIds.Contains(code.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.UserSessions.IgnoreQueryFilters().Where(session => userIds.Contains(session.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.TenantUsers.IgnoreQueryFilters().Where(membership => membership.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.Users.IgnoreQueryFilters().Where(user => userIds.Contains(user.Id)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.Tenants.IgnoreQueryFilters().Where(tenant => tenant.Id == tenantId).ExecuteDeleteAsync(CancellationToken.None);
    }

    [TestCleanup]
    public async ValueTask FixtureCleanup()
    {
        foreach (var cleanup in cleanups)
            await cleanup();

        cleanups.Clear();
    }
}
