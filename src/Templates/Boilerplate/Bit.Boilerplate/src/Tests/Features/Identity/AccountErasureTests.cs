//+:cnd:noEmit
using ImageMagick;
using FluentStorage.Storage;
using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Attachments;
//#if (notification == true)
using Boilerplate.Shared.Features.PushNotification;
//#endif

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// What has to be gone once an account is deleted, beyond the <c>Users</c> row itself.
/// <para>
/// <c>userManager.DeleteAsync</c> covers only what the database cascades from that row, and the two stores asserted
/// here cascade from nothing: <c>Attachment</c> has no foreign key to <c>User</c> at all - for the profile kinds its
/// <c>Id</c> IS the user id - and <c>PushNotificationSubscription</c>'s foreign key to <c>UserSession</c> is
/// <c>DeleteBehavior.SetNull</c>, so deleting the sessions orphans the subscription rather than removing it. Both
/// survived a "successful" account deletion before <c>UserErasureService</c> existed, and neither is visible from the
/// endpoint's own code.
/// </para>
/// <para>
/// The residue is not cosmetic. The blob stays reachable through the anonymous <c>AttachmentController.GetAttachment</c>
/// endpoint, whose address is nothing but the user id; and an orphaned subscription is read by
/// <c>PushNotificationService.RequestPush</c> as an anonymous visitor's device, which keeps a deleted account's phone in
/// the audience of every tenant wide broadcast.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class AccountErasureTests
{
    /// <summary>
    /// The self-service path, driven the way a user drives it: upload a picture, elevate, press Delete. The blob paths
    /// are read BEFORE the delete, because the <c>Attachments</c> row is the only thing that records where the blob is -
    /// which is also precisely why erasing the row without erasing the blob leaves something nothing can find again.
    /// </summary>
    [TestMethod]
    public async Task DeletingOwnAccount_Should_EraseTheProfilePicture_ItsBlob_AndEverySession()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        await UploadProfilePicture(httpClient);

        var blobPaths = await ReadAttachmentPaths(server, userId);

        Assert.HasCount(2, blobPaths, "One upload writes the small and the original kind, so there are two blobs to erase.");

        foreach (var blobPath in blobPaths)
        {
            Assert.IsTrue(await BlobExists(server, blobPath), $"The upload should have written {blobPath}; without it the assertions below prove nothing.");
        }

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        await scope.ServiceProvider.GetRequiredService<IUserController>().Delete(TestContext.CancellationToken);

        Assert.IsFalse(await UserExists(server, userId), "The account itself must be gone.");

        Assert.AreEqual(0, await CountUserSessions(server, userId), "Every session of the erased account must be gone.");

        Assert.AreEqual(0, await CountAttachments(server, userId),
            "Attachment has no foreign key to User, so nothing cascades here - the rows have to be deleted explicitly.");

        foreach (var blobPath in blobPaths)
        {
            Assert.IsFalse(await BlobExists(server, blobPath),
                $"{blobPath} is still in storage. GetAttachment is anonymous and its whole address is the user id, so the picture of an erased account stays fetchable by anyone who ever saw that id.");
        }
    }

    //#if (notification == true)
    /// <summary>
    /// The subscription is the one piece that gets quietly <b>kept</b> rather than missed: the <c>SetNull</c> cascade
    /// fires, so the row is still there with its <c>DeviceId</c> and its Web Push keys, only no longer attached to
    /// anything. Asserting the row count alone would pass against that bug, so this also asserts nothing was left
    /// dangling with a null <c>UserSessionId</c>.
    /// </summary>
    [TestMethod]
    public async Task DeletingOwnAccount_Should_EraseThePushSubscription_RatherThanOrphanIt()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var deviceId = Guid.CreateVersion7().ToString();

        await scope.ServiceProvider.GetRequiredService<IPushNotificationController>()
            .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = Guid.CreateVersion7().ToString() }, TestContext.CancellationToken);

        Assert.IsTrue(await PushSubscriptionExists(server, deviceId), "The subscription should have been stored; without it the assertion below proves nothing.");

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        await scope.ServiceProvider.GetRequiredService<IUserController>().Delete(TestContext.CancellationToken);

        Assert.IsFalse(await PushSubscriptionExists(server, deviceId),
            "The subscription outlived the account. Its foreign key to UserSession is SetNull, and RequestPush reads a null UserSessionId as an anonymous visitor's device - so an erased account's phone stays in the audience of every broadcast.");
    }
    //#endif


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    private async Task UploadProfilePicture(HttpClient httpClient)
    {
        using var image = new MagickImage(MagickColors.Red, 512, 512);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(image.ToByteArray(MagickFormat.Png));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "picture.png"); // The endpoint binds an IFormFile parameter named "file".

        using var response = await httpClient.PostAsync("api/v1/Attachment/UploadUserProfilePicture", form, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string[]> ReadAttachmentPaths(AppTestServer server, Guid userId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Attachments
            .Where(att => att.Id == userId && att.Path != null)
            .Select(att => att.Path!)
            .ToArrayAsync(TestContext.CancellationToken);
    }

    private async Task<int> CountAttachments(AppTestServer server, Guid userId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Attachments.CountAsync(att => att.Id == userId, TestContext.CancellationToken);
    }

    private async Task<int> CountUserSessions(AppTestServer server, Guid userId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserSessions.CountAsync(us => us.UserId == userId, TestContext.CancellationToken);
    }

    private async Task<bool> UserExists(AppTestServer server, Guid userId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Set<User>().AnyAsync(user => user.Id == userId, TestContext.CancellationToken);
    }

    //#if (notification == true)
    private async Task<bool> PushSubscriptionExists(AppTestServer server, string deviceId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.PushNotificationSubscriptions.AnyAsync(sub => sub.DeviceId == deviceId, TestContext.CancellationToken);
    }
    //#endif

    private async Task<bool> BlobExists(AppTestServer server, string blobPath)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IStore>().ObjectExists(blobPath, TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
