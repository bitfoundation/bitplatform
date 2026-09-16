using OtpNet;
using System.IO.Compression;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Tests.E2E.Features.Gdpr;

/// <summary>
/// Articles 15, 20 and 17 against Sales as one journey: an account signs in, leaves something in every store the app
/// declares an <c>IPersonalDataSource</c> for, downloads the export, and is then deleted. What the zip says is checked
/// against the deployment's own database, and so is what the delete left behind.
/// <para>
/// Over the api rather than a browser: the export is a zip no page renders, and a push subscription is the one thing a
/// headless browser cannot produce. The account is created with two factor authentication on because the authenticator's
/// code doubles as the elevated access token both endpoints are gated on (See IdentityController.RefreshToken) - the
/// mailed alternative would have to be read out of Sales' Hangfire, which runs on isolated storage the dev MCP of this
/// run cannot see.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class PersonalDataJourneyTests
{
    private const string password = "123456";

    /// <summary>
    /// The sources Sales registers, in the order <c>IPersonalDataSource.Order</c> puts them. No <c>todoItems</c>: that
    /// one belongs to the Todo module and this deployment is built with <c>--module Sales</c>. A source added to the
    /// codebase fails this line, which is the point - an export that quietly skips a store is the failure mode here.
    /// </summary>
    private static readonly string[] expectedSections = ["account", "sessions", "attachments", "pushSubscriptions", "tenants"];

    private readonly List<Func<Task>> cleanups = [];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnExport_Should_HandOverEveryStore_AndDeletingTheAccount_Should_EmptyThemAll()
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var marker = Guid.NewGuid().ToString("N")[..10];
        var email = $"e2e-{marker}@bitplatform.dev";
        var fullName = $"E2E Subject {marker}";
        var birthDate = new DateTimeOffset(1991, 2, 3, 0, 0, 0, TimeSpan.Zero);
        var authenticatorKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"e2e-{marker}", Title = $"E2E {marker}" };

        // Registered before the writes, so a half-made fixture is undone too.
        RegisterForCleanup(() => DeleteFixture(tenant.Id, email, authenticatorKey));

        var userId = await CreateUser(tenant, email, fullName, birthDate, authenticatorKey);

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(cancellationToken);

        // ---- Signed in on Sales: the password, then the authenticator's code ----
        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.Sales);
        var authManager = apiClient.Services.GetRequiredService<AuthManager>();

        var requiresTwoFactor = await authManager.SignIn(new() { Email = email, Password = password, RememberMe = true }, cancellationToken);
        Assert.IsTrue(requiresTwoFactor, "Two factor authentication is on for this account, so the password alone must not sign it in.");

        await authManager.SignIn(new() { Email = email, Password = password, RememberMe = true, TwoFactorCode = Code(authenticatorKey) }, cancellationToken);

        var sessionId = await GetSessionId(apiClient);

        // ---- Something in every store the export has a section for ----
        using (await Upload(apiClient.HttpClient, "UploadUserProfilePicture", "e2e-profile.png")) { }

        var pushSubscription = NewPushSubscription(marker);
        await apiClient.Services.GetRequiredService<IPushNotificationController>().Subscribe(pushSubscription, cancellationToken);

        // Deliberately outside the account's own stores: no column records who uploaded an AI chat image, so the
        // sections below must not list it and the delete further down must not reach it.
        Guid aiChatImageId;
        using (var uploaded = await Upload(apiClient.HttpClient, "UploadAiChatImage", "e2e-ai-chat.png"))
        {
            aiChatImageId = Guid.Parse((await uploaded.Content.ReadAsStringAsync(cancellationToken)).Trim('"'));
        }

        // ---- Elevated access: both the export and the delete are behind it ----
        var elevatedAccessToken = await authManager.RefreshToken(requestedBy: nameof(PersonalDataJourneyTests), elevatedAccessToken: Code(authenticatorKey));
        Assert.IsFalse(string.IsNullOrEmpty(elevatedAccessToken), "The elevating refresh answered with no access token.");

        var elevatedUntil = IAuthTokenProvider.ParseAccessToken(elevatedAccessToken!, validateExpiry: false).GetElevatedSessionExpiresOn();
        Assert.IsNotNull(elevatedUntil, "The refreshed token carries no elevated session claim, so the export would be refused.");
        Assert.IsGreaterThan(DateTimeOffset.UtcNow, elevatedUntil.Value, "The elevated window was already over when it was granted.");

        // ---- Article 15 / 20: the zip ----
        using var export = await apiClient.HttpClient.GetAsync(IUserController.ExportPersonalDataUri, cancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, export.StatusCode, "The export was refused.");
        Assert.IsNotNull(export.Content.Headers.ContentType, "The export answered with no Content-Type.");
        Assert.AreEqual("application/zip", export.Content.Headers.ContentType.MediaType);
        Assert.IsNotNull(export.Headers.CacheControl, "The export answered with no Cache-Control.");
        Assert.IsTrue(export.Headers.CacheControl.NoStore, "A copy of an identity must not be storable by anything on the way back.");
        Assert.Contains(userId.ToString(), export.Content.Headers.ContentDisposition?.FileName ?? "", "The zip is not named after the account it is about.");

        using var archive = new ZipArchive(new MemoryStream(await export.Content.ReadAsByteArrayAsync(cancellationToken)), ZipArchiveMode.Read);

        Assert.AreEqual("data.json", archive.Entries[0].FullName, "data.json is written first so the readable half opens before the blobs.");

        var dataJson = await ReadEntry(archive, "data.json");
        var root = JsonNode.Parse(dataJson)!;

        Assert.AreEqual(userId.ToString(), root["subjectUserId"]!.GetValue<Guid>().ToString());
        Assert.IsNotNull(root["exportedOn"], "The export does not say when it was taken.");
        Assert.Contains("Credentials", root["notice"]!.GetValue<string>(), "The notice no longer explains what was withheld.");

        var sections = root["sections"]!.AsObject();

        Assert.AreSequenceEqual(expectedSections, sections.Select(section => section.Key), "The export's sections are not the sources Sales registers, in IPersonalDataSource.Order.");

        // Article 15(1)(a)(c)(d): a dump of rows on its own answers none of these.
        foreach (var (key, section) in sections)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(section!["purpose"]?.GetValue<string>()), $"Section '{key}' does not say what it is held for.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(section["retention"]?.GetValue<string>()), $"Section '{key}' does not say how long it is kept.");
            Assert.IsNotNull(section["recipients"]?.AsArray(), $"Section '{key}' does not say who else receives it.");
            Assert.IsNotNull(section["erasurePath"]?.GetValue<string>(), $"Section '{key}' does not say how an erasure reaches it.");
            Assert.IsNotNull(section["data"], $"Section '{key}' carries no data at all.");
            Assert.IsNotNull(section["files"]?.AsArray(), $"Section '{key}' does not list its files.");
        }

        await AssertAccountSection(dbContext, sections["account"]!, userId, dataJson, authenticatorKey, cancellationToken);
        await AssertSessionsSection(dbContext, sections["sessions"]!, userId, sessionId, tenant.Id, cancellationToken);
        await AssertAttachmentsSection(archive, sections["attachments"]!);
        AssertPushSubscriptionsSection(sections["pushSubscriptions"]!, pushSubscription, sessionId, dataJson);
        AssertTenantsSection(sections["tenants"]!, tenant);

        // The AI chat image is the documented exception, and the export says so rather than staying silent about it.
        Assert.DoesNotContain(aiChatImageId.ToString(), dataJson, "An AI chat image reached the export, which cannot look it up by account.");
        Assert.Contains("AI chat", sections["attachments"]!["notes"]!.GetValue<string>(), "The attachments section no longer names what it cannot tell the reader.");

        // ---- Article 17: the delete, and what it was supposed to reach ----
        await apiClient.Services.GetRequiredService<IUserController>().Delete(cancellationToken);

        await using var afterDelete = await globalApiClient.DbContextFactory!.CreateDbContextAsync(cancellationToken);

        Assert.AreEqual(0, await afterDelete.Users.IgnoreQueryFilters().CountAsync(user => user.Id == userId, cancellationToken), "The account row survived its own deletion.");
        Assert.AreEqual(0, await afterDelete.UserSessions.IgnoreQueryFilters().CountAsync(session => session.UserId == userId, cancellationToken), "A session of the deleted account is still signed in.");
        Assert.AreEqual(0, await afterDelete.PushNotificationSubscriptions.IgnoreQueryFilters().CountAsync(sub => sub.DeviceId == pushSubscription.DeviceId, cancellationToken), "The device can still be pushed to.");
        Assert.AreEqual(0, await afterDelete.Attachments.IgnoreQueryFilters().CountAsync(attachment => attachment.Id == userId, cancellationToken), "The profile picture's rows are still there.");
        Assert.AreEqual(0, await afterDelete.UserTokens.IgnoreQueryFilters().CountAsync(token => token.UserId == userId, cancellationToken), "The authenticator's shared key outlived the account it belonged to.");
        // The section claims CascadeFromUser rather than a delete of its own, which is a claim about the schema.
        Assert.AreEqual(0, await afterDelete.TenantUsers.IgnoreQueryFilters().CountAsync(membership => membership.UserId == userId, cancellationToken), "The organisation membership did not cascade from the deleted user.");

        // Left behind on purpose: only the hourly retention job reaches it (See AttachmentsPersonalDataSource.Notes,
        // and RetentionJobsTests, which is where that job is proven to run).
        Assert.AreEqual(1, await afterDelete.Attachments.IgnoreQueryFilters().CountAsync(attachment => attachment.Id == aiChatImageId, cancellationToken),
                        "The AI chat image was deleted with the account, which no code path can do - it is not looked up by user.");

        // Last, because the blobs go after the commit and a failure there is only logged: the rows above being gone is
        // not the same as the photograph being unreachable, and the erasure answers 200 either way.
        foreach (var kind in new[] { AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal })
        {
            var status = await DeployedAttachments.StatusOf(DeployedApps.Sales, userId, kind, cancellationToken);

            Assert.AreEqual(HttpStatusCode.NotFound, status,
                            $"The erased account's {kind} is still served ({(int)status}). ErasePublished swallows what went wrong, so the deployment's log is where it says why.");
        }
    }

    /// <summary>Everything the Users row holds, minus what proves the reader is that person (Article 15(4)).</summary>
    private async Task AssertAccountSection(AppDbContext dbContext, JsonNode section, Guid userId, string dataJson, string authenticatorKey, CancellationToken cancellationToken)
    {
        var stored = await dbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.Email, user.UserName, user.FullName, user.Gender, user.BirthDate, user.PasswordHash, user.SecurityStamp, user.HasProfilePicture })
            .SingleAsync(cancellationToken);

        var account = section["data"]!;

        Assert.AreEqual(stored.Email, account["email"]!.GetValue<string>());
        Assert.AreEqual(stored.UserName, account["userName"]!.GetValue<string>());
        Assert.AreEqual(stored.FullName, account["fullName"]!.GetValue<string>());
        // Enums by name: "Gender": 1 means nothing to the person reading it.
        Assert.AreEqual(stored.Gender.ToString(), account["gender"]!.GetValue<string>());
        Assert.AreEqual(stored.BirthDate, account["birthDate"]!.GetValue<DateTimeOffset>());
        Assert.IsTrue(account["emailConfirmed"]!.GetValue<bool>());
        Assert.IsTrue(account["twoFactorEnabled"]!.GetValue<bool>());
        Assert.AreEqual(stored.HasProfilePicture, account["hasProfilePicture"]!.GetValue<bool>());
        Assert.IsTrue(stored.HasProfilePicture, "The upload did not mark the account as having a profile picture.");

        // What the account is opened with is not a fact about the person, and handing it out would only weaken it.
        Assert.DoesNotContain(stored.PasswordHash!, dataJson, "The password hash is in the export.");
        Assert.DoesNotContain(stored.SecurityStamp!, dataJson, "The security stamp is in the export.");
        Assert.DoesNotContain(authenticatorKey, dataJson, "The authenticator's shared key is in the export.");
    }

    /// <summary>The sessions the settings page shows, in a form the user can keep.</summary>
    private async Task AssertSessionsSection(AppDbContext dbContext, JsonNode section, Guid userId, Guid sessionId, Guid tenantId, CancellationToken cancellationToken)
    {
        var storedIds = await dbContext.UserSessions.IgnoreQueryFilters().AsNoTracking()
            .Where(session => session.UserId == userId)
            .Select(session => session.Id)
            .ToArrayAsync(cancellationToken);

        var exported = section["data"]!.AsArray();
        var exportedIds = exported.Select(session => session!["id"]!.GetValue<Guid>()).ToArray();

        Assert.AreSequenceEqual(storedIds.Order(), exportedIds.Order(), "The sessions section and the database do not agree on which devices are signed in.");
        Assert.Contains(sessionId, exportedIds, "The session that asked for the export is not in it.");

        var current = exported.Single(session => session!["id"]!.GetValue<Guid>() == sessionId)!;

        Assert.AreEqual(tenantId, current["tenantId"]!.GetValue<Guid>());
        Assert.IsNotNull(current["startedOn"], "A session with no start is not a record of anything.");
        // Unix seconds on the row; a timestamp is what the reader can do something with.
        Assert.IsGreaterThan(DateTimeOffset.UtcNow.AddDays(-1), current["startedOn"]!.GetValue<DateTimeOffset>(), "StartedOn did not survive the decode from unix seconds.");
        Assert.IsNull(current["authorizedApplication"]?.GetValue<string>(), "A session of the user's own device is being reported as an authorized application's.");
    }

    /// <summary>Metadata in data.json, the images themselves as files - base64 in json is a third larger.</summary>
    private static async Task AssertAttachmentsSection(ZipArchive archive, JsonNode section)
    {
        var exported = section["data"]!.AsArray();

        Assert.AreSequenceEqual(new[] { nameof(AttachmentKind.UserProfileImageSmall), nameof(AttachmentKind.UserProfileImageOriginal) },
                                exported.Select(attachment => attachment!["kind"]!.GetValue<string>()),
                                "The profile picture's two kinds are not both in the export.");

        // The small one is re-encoded to WebP; the original keeps the format it arrived in. GetFilePath gives only the
        // resized kinds an extension (See AttachmentController.GetFilePath), so the original's is read back from the
        // stored bytes - a file the subject has to guess the format of is a poor answer to an Article 20 request.
        var files = section["files"]!.AsArray().Select(file => file!.GetValue<string>()).ToArray();

        Assert.AreSequenceEqual(new[] { "files/attachments/UserProfileImageOriginal.png", "files/attachments/UserProfileImageSmall.webp" }, files.Order(),
                                "The files the attachments section names are not the ones the profile picture is stored as.");

        foreach (var file in files)
        {
            var entry = archive.GetEntry(file);
            Assert.IsNotNull(entry, $"The export names '{file}' but the zip has no such entry.");
            Assert.IsGreaterThan(0, entry.Length, $"'{file}' is an empty file.");
        }

        // The bytes, not just the names: an export that ships the right file list and the wrong content answers nothing.
        var small = await ReadEntryBytes(archive, "files/attachments/UserProfileImageSmall.webp");
        Assert.AreSequenceEqual("RIFF"u8.ToArray(), small[..4], "The small profile image is not the WebP the resize produces.");
        Assert.AreSequenceEqual("WEBP"u8.ToArray(), small[8..12], "The small profile image is not the WebP the resize produces.");

        var original = await ReadEntryBytes(archive, "files/attachments/UserProfileImageOriginal.png");
        Assert.AreSequenceEqual(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, original[..4], "The original profile image did not keep the format it was uploaded in.");
    }

    /// <summary>The devices the account can be pushed to - without the keys that would let anyone push to them.</summary>
    private static void AssertPushSubscriptionsSection(JsonNode section, PushNotificationSubscriptionDto subscribed, Guid sessionId, string dataJson)
    {
        var exported = section["data"]!.AsArray();

        Assert.AreEqual(1, exported.Count, "The account subscribed one device.");

        var device = exported[0]!;

        Assert.AreEqual(subscribed.DeviceId, device["deviceId"]!.GetValue<string>());
        Assert.AreEqual(subscribed.Platform, device["platform"]!.GetValue<string>());
        Assert.AreEqual(subscribed.Endpoint, device["endpoint"]!.GetValue<string>());
        Assert.AreEqual(sessionId, device["userSessionId"]!.GetValue<Guid>(), "The device is not tied back to the session that registered it.");
        Assert.IsGreaterThan(DateTimeOffset.UtcNow, device["expiresOn"]!.GetValue<DateTimeOffset>(), "The subscription was exported already expired.");

        // Credentials for the subscription, not facts about the person - which is what the section's notes claim.
        Assert.DoesNotContain(subscribed.P256dh!, dataJson, "The Web Push public key is in the export.");
        Assert.DoesNotContain(subscribed.Auth!, dataJson, "The Web Push auth secret is in the export.");
        Assert.Contains("encryption keys", section["notes"]!.GetValue<string>(), "The push section no longer says which fields it withholds.");
    }

    private static void AssertTenantsSection(JsonNode section, Tenant tenant)
    {
        var exported = section["data"]!.AsArray();

        Assert.AreEqual(1, exported.Count, "The account belongs to one organisation.");
        Assert.AreEqual(tenant.Id, exported[0]!["tenantId"]!.GetValue<Guid>());
        Assert.AreEqual(tenant.Title, exported[0]!["tenantTitle"]!.GetValue<string>());
        Assert.IsNotNull(exported[0]!["acceptedOn"], "An accepted membership is being exported as a pending invitation.");
    }

    /// <summary>
    /// A tenant, a confirmed member of it, and the authenticator enrolment. Two factor is on from the start: the
    /// authenticator's code is what elevates the session later, and without it the elevated access token has to be
    /// requested by mail first (See IdentityController.RefreshToken).
    /// </summary>
    private async Task<Guid> CreateUser(Tenant tenant, string email, string fullName, DateTimeOffset birthDate, string authenticatorKey)
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
            FullName = fullName,
            Gender = Gender.Female,
            BirthDate = birthDate,
            TwoFactorEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedOn = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        await dbContext.Tenants.AddAsync(tenant, TestContext.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.CancellationToken);
        // Accepted, so signing in selects this tenant (See IdentityController.GetTenantId).
        await dbContext.TenantUsers.AddAsync(new TenantUser { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id, AcceptedOn = DateTimeOffset.UtcNow }, TestContext.CancellationToken);
        // Where UserManager.SetAuthenticatorKeyAsync puts it; both names are internal constants of Identity's own store.
        await dbContext.UserTokens.AddAsync(new UserToken { UserId = user.Id, LoginProvider = "[AspNetUserStore]", Name = "AuthenticatorKey", Value = authenticatorKey }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return user.Id;
    }

    /// <summary>
    /// A subscription the server will accept: <c>browser</c> takes the Web Push triple, and the endpoint's keys are
    /// read by AdsPush rather than stored as given, so the public key has to be a real P-256 point.
    /// </summary>
    private static PushNotificationSubscriptionDto NewPushSubscription(string marker)
    {
        using var keyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = keyPair.PublicKey.ExportParameters().Q;

        return new()
        {
            // Not the browser's `${p256dh}-${auth}`: the device id IS exported, and this test asserts the keys are not.
            DeviceId = $"e2e-{marker}",
            Platform = "browser",
            Endpoint = $"https://fcm.googleapis.com/fcm/send/e2e-{marker}",
            P256dh = Base64Url.EncodeToString([0x04, .. publicKey.X!, .. publicKey.Y!]),
            Auth = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16))
        };
    }

    private static string Code(string authenticatorKey) => new Totp(Base32Encoding.ToBytes(authenticatorKey)).ComputeTotp();

    private static async Task<Guid> GetSessionId(DeployedApiClient apiClient)
    {
        var accessToken = await apiClient.Services.GetRequiredService<IAuthTokenProvider>().GetAccessToken();

        return IAuthTokenProvider.ParseAccessToken(accessToken!, validateExpiry: false).GetSessionId();
    }

    /// <summary>The multipart post the app makes; the response is the caller's to read and dispose.</summary>
    private async Task<HttpResponseMessage> Upload(HttpClient httpClient, string action, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(TestImages.ProfilePicturePng());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);

        var response = await httpClient.PostAsync($"api/v1/Attachment/{action}", form, TestContext.CancellationToken);

        Assert.IsTrue(response.IsSuccessStatusCode, $"{action} answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(TestContext.CancellationToken)}");

        return response;
    }

    private static async Task<string> ReadEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new AssertFailedException($"The export has no '{path}' entry.");

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    private static async Task<byte[]> ReadEntryBytes(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new AssertFailedException($"The export has no '{path}' entry.");

        await using var stream = entry.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        return buffer.ToArray();
    }

    private void RegisterForCleanup(Func<Task> cleanup) => cleanups.Add(cleanup);

    /// <summary>
    /// The picture through the app's own endpoint, which is what removes the blobs, then the rows. Only reached when
    /// the journey did not get as far as deleting the account itself.
    /// </summary>
    private static async Task DeleteFixture(Guid tenantId, string email, string authenticatorKey)
    {
        try
        {
            await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.Sales);
            var authManager = apiClient.Services.GetRequiredService<AuthManager>();

            await authManager.SignIn(new() { Email = email, Password = password }, CancellationToken.None);
            await authManager.SignIn(new() { Email = email, Password = password, TwoFactorCode = Code(authenticatorKey) }, CancellationToken.None);

            await apiClient.Services.GetRequiredService<IAttachmentController>().DeleteUserProfilePicture(CancellationToken.None);
        }
        catch (Exception)
        {
            // The account is already gone, or never had a picture on it.
        }

        // Not on the test's own token: a canceled or timed out test still owes the deployment its cleanup.
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var userIds = await dbContext.TenantUsers.IgnoreQueryFilters().Where(membership => membership.TenantId == tenantId)
            .Select(membership => membership.UserId).ToArrayAsync(CancellationToken.None);

        // The subscription's tenant is a foreign key of its own, so it goes before the tenant whatever its user did.
        await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters().Where(sub => sub.TenantId == tenantId).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.UserSessions.IgnoreQueryFilters().Where(session => userIds.Contains(session.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.Attachments.IgnoreQueryFilters().Where(attachment => userIds.Contains(attachment.Id)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.UserTokens.IgnoreQueryFilters().Where(token => userIds.Contains(token.UserId)).ExecuteDeleteAsync(CancellationToken.None);
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
