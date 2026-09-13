using FluentStorage.Storage;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Features.Attachments;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Tests.Features.Attachments;

/// <summary>
/// <c>AiChatImagesRetentionJobRunner</c> is the only thing that ever deletes an image attached to an AI chat message.
/// Nothing links one back to the person who sent it - <c>UploadAiChatImage</c> mints a fresh id per upload so that no
/// caller can name someone else's blob - so it is out of reach of <c>UserErasureService</c> and of every other deletion
/// path in the app. If this job stops working the pictures are simply kept forever, and a "0 deleted" run looks exactly
/// like a healthy one, which is why the two halves are pinned separately: what must go, and what must NOT.
/// </summary>
/// <remarks>
/// <c>DoNotParallelize</c>: the sweep is global, so a concurrent run deletes another test's expired fixture between it
/// being created and being asserted on.
/// </remarks>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public class AiChatImagesRetentionTests
{
    /// <summary>
    /// An image older than the configured period must lose both its blob and its row. The blob assertion is the one
    /// that matters: deleting the row alone leaves a file that nothing in the system can name again, while still being
    /// fetchable by anyone holding the id through the anonymous <c>GetAttachment</c> endpoint.
    /// </summary>
    [TestMethod]
    public async Task EnforceRetention_Should_DeleteAnExpiredImage_AndItsBlob()
    {
        await using var server = await StartServer();

        var retention = server.WebApp.Services.GetRequiredService<ServerApiSettings>().AiChatImagesRetention;

        Assert.IsGreaterThan(TimeSpan.Zero, retention, "The job refuses to run without a positive retention period.");

        var (attachmentId, blobPath) = await StoreAiChatImage(server, createdOn: DateTimeOffset.UtcNow - retention - TimeSpan.FromMinutes(1));

        Assert.IsTrue(await BlobExists(server, blobPath), "The blob should have been stored; without it the assertion below proves nothing.");

        await EnforceRetention(server);

        Assert.IsFalse(await AttachmentExists(server, attachmentId), "An expired AI chat image's row must be deleted.");

        Assert.IsFalse(await BlobExists(server, blobPath),
            $"{blobPath} is still in storage. Nothing else in the app can find this blob - its row was the only record of where it is - so a row-only delete keeps the picture forever.");
    }

    /// <summary>
    /// The other half, and the reason the first assertion is not enough on its own: a job that deleted everything it
    /// found would pass the test above while emptying the panel mid-conversation.
    /// </summary>
    [TestMethod]
    public async Task EnforceRetention_Should_LeaveAnImageThatIsStillWithinItsRetentionPeriod()
    {
        await using var server = await StartServer();

        var (attachmentId, blobPath) = await StoreAiChatImage(server, createdOn: DateTimeOffset.UtcNow);

        await EnforceRetention(server);

        Assert.IsTrue(await AttachmentExists(server, attachmentId), "A picture that has not expired yet must survive the sweep.");

        Assert.IsTrue(await BlobExists(server, blobPath), "A picture that has not expired yet must keep its blob.");

        await DeleteAiChatImage(server, attachmentId, blobPath);
    }

    /// <summary>
    /// A blob that has already gone - deleted by hand, or by a run that removed it and then failed to save - must still
    /// take its row with it. Left behind, the row is retried on every run forever.
    /// </summary>
    [TestMethod]
    public async Task EnforceRetention_Should_DeleteTheRow_WhenItsBlobIsAlreadyGone()
    {
        await using var server = await StartServer();

        var retention = server.WebApp.Services.GetRequiredService<ServerApiSettings>().AiChatImagesRetention;

        var (attachmentId, blobPath) = await StoreAiChatImage(server, createdOn: DateTimeOffset.UtcNow - retention - TimeSpan.FromMinutes(1));

        await using (var scope = server.WebApp.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IStore>().DeleteObject(blobPath, TestContext.CancellationToken);
        }

        await EnforceRetention(server);

        Assert.IsFalse(await AttachmentExists(server, attachmentId),
            "The row must go even when its blob was already missing, otherwise it is re-examined by every future run.");
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// Writes the row and the blob the way <c>AttachmentController.UploadAiChatImage</c> does - a fresh id, the path
    /// <c>GetFilePath</c> derives from it - with <paramref name="createdOn"/> chosen by the test, because a job that
    /// measures age cannot be exercised by anything that was uploaded just now.
    /// </summary>
    private async Task<(Guid AttachmentId, string BlobPath)> StoreAiChatImage(AppTestServer server, DateTimeOffset createdOn)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var attachmentId = Guid.CreateSequentialGuid();
        var appSettings = scope.ServiceProvider.GetRequiredService<ServerApiSettings>();
        var blobPath = AttachmentController.GetFilePath(appSettings, attachmentId, AttachmentKind.AiChatImage);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Attachments.AddAsync(new Attachment
        {
            Id = attachmentId,
            Kind = AttachmentKind.AiChatImage,
            Path = blobPath,
            CreatedOn = createdOn
        }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        await scope.ServiceProvider.GetRequiredService<IStore>()
            .SetBytes(blobPath, [1, 2, 3], cancellationToken: TestContext.CancellationToken);

        return (attachmentId, blobPath);
    }

    private async Task EnforceRetention(AppTestServer server)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<AiChatImagesRetentionJobRunner>()
            .EnforceRetention(TestContext.CancellationToken);
    }

    private async Task<bool> AttachmentExists(AppTestServer server, Guid attachmentId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Attachments.AnyAsync(att => att.Id == attachmentId, TestContext.CancellationToken);
    }

    private async Task<bool> BlobExists(AppTestServer server, string blobPath)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IStore>().ObjectExists(blobPath, TestContext.CancellationToken);
    }

    /// <summary>
    /// The un-expired picture is never swept by definition, so the test that creates one cleans up after itself - the
    /// database and the blob store both outlive the run.
    /// </summary>
    private async Task DeleteAiChatImage(AppTestServer server, Guid attachmentId, string blobPath)
    {
        try
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Attachments
                .Where(att => att.Id == attachmentId)
                .ExecuteDeleteAsync(TestContext.CancellationToken);

            await scope.ServiceProvider.GetRequiredService<IStore>().DeleteObject(blobPath, TestContext.CancellationToken);
        }
        catch (Exception) { } // Best effort: a test that failed earlier must not be reported as this cleanup failing.
    }

    public TestContext TestContext { get; set; } = default!;
}
