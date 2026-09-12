using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Realtime;

/// <summary>
/// A profile picture changed in one session of a user shows in the user's other sessions at once: AttachmentController
/// pushes PROFILE_UPDATED over AppHub to every other session of that user holding a SignalR connection, and MainLayout
/// replaces the current user with what it carries. Both sessions must be of the same app - Sales, Todo and AdminPanel
/// each have their own SignalR, with no backplane between them - but they need not be on the same platform: a hybrid
/// app and a browser both connect to that app's api host.
/// </summary>
public abstract partial class RealtimeProfileTestsBase : AppTestBase
{
    protected const string password = "123456";

    /// <summary>
    /// <paramref name="theAppUploads"/> says which of the two sessions posts the picture. A browser can do either, but
    /// the hybrid heads only watch: the picture is chosen through the WebView's own file chooser, which is a native
    /// dialog rather than something the page hands over. What SignalR has to deliver is the same either way.
    /// </summary>
    protected async Task AProfilePictureChange_Should_ReachTheUsersOtherSession(App app, bool theAppUploads)
    {
        await SkipWithoutGlobalAdminCredentials();

        var email = $"e2e-{Guid.NewGuid():N}"[..14] + "@bitplatform.dev";
        var userId = await CreateUser(email);

        // Registered before anything is uploaded: the picture lives in blob storage, not only in the database.
        RegisterForCleanup(() => DeleteUser(userId, email, app));

        // ---- The app under test, and a browser session of the same user ----
        var appPage = await OpenApp(app);
        await WaitUntilInteractive(appPage);
        await SignIn(appPage, email, password);

        var browserContext = await NewBrowserContext(Browser);
        RegisterForCleanup(async () => await browserContext.DisposeAsync());
        var browserPage = await browserContext.NewPageAsync();

        await browserPage.GotoAsync(DeployedApps.AddressOf(app));
        await WaitUntilInteractive(browserPage);
        await SignIn(browserPage, email, password);

        var uploader = theAppUploads ? appPage : browserPage;
        var watcher = theAppUploads ? browserPage : appPage;

        // BitPersona shows initials until the user has a picture.
        var watcherAvatar = watcher.Locator("header .persona img").First;
        await Expect(watcherAvatar).ToHaveCountAsync(0);

        await UploadProfilePicture(uploader);

        // Its own header first: the upload went through.
        await Expect(uploader.Locator("header .persona img").First).ToHaveAttributeAsync("src", ProfileImageUrl(userId));

        // ---- The other session was told over SignalR: no reload, no navigation ----
        await Expect(watcherAvatar).ToHaveAttributeAsync("src", ProfileImageUrl(userId), new() { Timeout = 30_000 });
    }

    /// <summary>
    /// Opened by its key (SettingsPage expands the section its route names), and through the section's own "Upload new
    /// image" link: the layout's AI chat panel has a file input of its own, earlier in the page.
    /// </summary>
    private async Task UploadProfilePicture(IPage page)
    {
        await page.GoToInApp($"{PageUrls.Settings}/{PageUrls.SettingsSections.Profile}");

        var fileChooser = await page.RunAndWaitForFileChooserAsync(async () =>
            await page.GetByText(AppStrings.UploadNewImage, new() { Exact = true }).ClickAsync());

        var upload = await page.RunAndWaitForResponseAsync(async () => await fileChooser.SetFilesAsync(new FilePayload
        {
            Name = "e2e-profile.png",
            MimeType = "image/png",
            Buffer = TestImages.ProfilePicturePng()
        }), response => response.Url.Contains("/api/v1/Attachment/UploadUserProfilePicture", StringComparison.OrdinalIgnoreCase));

        // 204: UploadUserProfilePicture answers with no content.
        Assert.IsTrue(upload.Ok, $"The profile picture upload answered {upload.Status}: {await upload.TextAsync()}");
    }

    /// <summary>UserDto.GetProfileImageUrl: the small profile image, versioned by the user's row.</summary>
    private static Regex ProfileImageUrl(Guid userId) => new($"/api/v1/Attachment/GetAttachment/{userId}/UserProfileImageSmall\\?v=", RegexOptions.IgnoreCase);

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
    /// The picture through the app's own endpoint, which also removes the blobs; then the sessions and the user. Not on
    /// the test's token: a canceled or timed out test still owes the deployment its cleanup.
    /// </summary>
    private static async Task DeleteUser(Guid userId, string email, App app)
    {
        try
        {
            await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.ApiOf(app));
            await apiClient.Services.GetRequiredService<AuthManager>().SignIn(new() { Email = email, Password = password }, CancellationToken.None);
            await apiClient.Services.GetRequiredService<IAttachmentController>().DeleteUserProfilePicture(CancellationToken.None);
        }
        catch (ResourceNotFoundException)
        {
            // No picture was ever uploaded - nothing in blob storage to remove.
        }
        finally
        {
            // Whatever the picture's removal did: a failure there is reported, but must not leave the user behind.
            var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
            await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

            await db.UserSessions.IgnoreQueryFilters().Where(session => session.UserId == userId).ExecuteDeleteAsync(CancellationToken.None);
            await db.Attachments.IgnoreQueryFilters().Where(attachment => attachment.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
            await db.Users.IgnoreQueryFilters().Where(u => u.Id == userId).ExecuteDeleteAsync(CancellationToken.None);
        }
    }
}
