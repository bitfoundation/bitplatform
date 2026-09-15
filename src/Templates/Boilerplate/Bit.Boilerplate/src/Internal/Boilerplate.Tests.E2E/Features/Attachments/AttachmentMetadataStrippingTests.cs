using ImageMagick;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Tests.E2E.Features.Attachments;

/// <summary>
/// A phone photo carries an EXIF block with GPS coordinates, capture time and camera serial, and <c>GetAttachment</c>
/// is anonymous: anyone holding a user id can fetch what the upload stored. <c>UploadAttachment</c> re-encodes every
/// kind and calls <c>Strip()</c> for exactly that reason.
/// <para>
/// <c>Boilerplate.Tests</c> pins the strip against a test server's own disk storage. What this one asks is what the
/// deployment hands to an anonymous visitor: through Azure Blob Storage and out through Cloudflare, where a caching or
/// storage change could put the uploaded bytes back in front of the public without touching the code under test.
/// Browserless on purpose - the picture is chosen through a native file dialog, and the bytes are the whole subject.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public class AttachmentMetadataStrippingTests
{
    private const string password = "123456";

    private readonly List<Func<Task>> cleanups = [];

    public TestContext TestContext { get; set; } = default!;

    /// <summary>Sales serves its own attachments, AdminPanel's come from the standalone API - the two hosting shapes.</summary>
    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (standalone API)")]
    public async Task APhotoServedToAnAnonymousVisitor_Should_CarryNoneOfItsExif(App app)
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var api = DeployedApps.ApiOf(app);
        var cancellationToken = TestContext.CancellationToken;

        var photo = TestImages.PhotoWithExifJpeg();

        using (var fixture = new MagickImage(photo))
        {
            Assert.IsNotNull(fixture.GetExifProfile(),
                "The fixture carries no EXIF, so this test would pass without the deployment having stripped anything.");
        }

        var email = $"e2e-{Guid.NewGuid():N}"[..14] + "@bitplatform.dev";
        var userId = await CreateUser(email);

        // Registered before the upload: the picture lives in blob storage, not only in the database.
        RegisterForCleanup(() => DeleteUser(userId, email, api));

        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(api);
        await apiClient.Services.GetRequiredService<AuthManager>().SignIn(new() { Email = email, Password = password, RememberMe = true }, cancellationToken);

        await Upload(apiClient, photo);

        foreach (var kind in new[] { AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal })
        {
            var served = await FetchAnonymously(api, userId, kind);

            using var stored = new MagickImage(served);

            Assert.IsNull(stored.GetExifProfile(),
                $"{api} serves {kind} with its EXIF intact. The endpoint is anonymous, so the coordinates of wherever the photo was taken are public to anyone holding the user id.");

            // The profile is what Strip removes, but a copy of the raw bytes anywhere on the way would keep the text.
            Assert.DoesNotContain(TestImages.ExifSoftware, Encoding.Latin1.GetString(served), StringComparison.Ordinal,
                $"{api} serves {kind} with the uploaded bytes' EXIF text still in it, whatever the profile says.");
        }

        // Stripping means re-encoding, so the one thing the original must keep is the format it arrived in - nothing
        // records what that was, and GetAttachment serves it as application/octet-stream.
        using var original = new MagickImage(await FetchAnonymously(api, userId, AttachmentKind.UserProfileImageOriginal));

        Assert.AreEqual(MagickFormat.Jpeg, original.Format, $"{api} converted the original kind to another format while stripping it.");
    }

    /// <summary>
    /// No credentials, and a query key of its own: the endpoint is cached at the edge for a week, so a shared url could
    /// answer from a copy stored before the deployment under test. A fresh one still goes through Cloudflare.
    /// </summary>
    private async Task<byte[]> FetchAnonymously(string api, Guid userId, AttachmentKind kind)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        using var response = await httpClient.GetAsync($"{api}api/v1/Attachment/GetAttachment/{userId}/{kind}?e2e={Guid.NewGuid()}", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{api} answered {(int)response.StatusCode} for the {kind} it was just given.");

        return await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);
    }

    private async Task Upload(DeployedApiClient apiClient, byte[] photo)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(photo);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "e2e-photo.jpg"); // The endpoint binds an IFormFile parameter named "file".

        using var response = await apiClient.HttpClient.PostAsync("api/v1/Attachment/UploadUserProfilePicture", form, TestContext.CancellationToken);

        Assert.IsTrue(response.IsSuccessStatusCode,
            $"The upload answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(TestContext.CancellationToken)}");
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

    /// <summary>
    /// The picture through the app's own endpoint, which is what removes the blobs; then the sessions and the user. Not
    /// on the test's token: a canceled or timed out test still owes the deployment its cleanup.
    /// </summary>
    private static async Task DeleteUser(Guid userId, string email, string api)
    {
        try
        {
            await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(api);
            await apiClient.Services.GetRequiredService<AuthManager>().SignIn(new() { Email = email, Password = password }, CancellationToken.None);
            await apiClient.Services.GetRequiredService<IAttachmentController>().DeleteUserProfilePicture(CancellationToken.None);
        }
        catch (ResourceNotFoundException)
        {
            // The upload never got as far as writing one. Anything else is the deployment failing to delete a blob,
            // which is worth hearing about - the rows below go either way, from the finally.
        }
        finally
        {
            var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
            await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

            await dbContext.UserSessions.IgnoreQueryFilters().Where(session => session.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
            await dbContext.Attachments.IgnoreQueryFilters().Where(attachment => attachment.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
            await dbContext.Users.IgnoreQueryFilters().Where(user => user.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
        }
    }

    private void RegisterForCleanup(Func<Task> cleanup) => cleanups.Add(cleanup);

    [TestCleanup]
    public async ValueTask FixtureCleanup()
    {
        foreach (var cleanup in cleanups)
            await cleanup();

        cleanups.Clear();
    }
}
