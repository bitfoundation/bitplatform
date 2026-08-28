using ImageMagick;
using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Tests.Features.Attachments;

/// <summary>
/// <c>AttachmentController.UploadAttachment</c> is split into a validate phase and a mutate phase, and both halves of
/// that split have already been paid for once:
/// <list type="bullet">
/// <item>It used to delete the existing rows and the existing blob <b>before</b> decoding the upload. A file that was
/// too small, or that ImageMagick could not decode, therefore destroyed the picture the user already had while
/// leaving <c>HasProfilePicture</c> true - so the profile pointed at a blob that no longer existed and no code path
/// could restore or clear it.</item>
/// <item>The first fix for that then went the other way and removed-and-re-added the rows in one
/// <c>SaveChangesAsync</c>. <c>Attachment</c>'s key is composite - <c>{ Id, Kind }</c> - and <c>GetFilePath</c> is
/// deterministic over exactly those two values, so the re-added rows collided with the tracked deleted ones and EF
/// threw before reaching the database. Every <b>re-upload</b> failed while the first upload passed, which is the one
/// case a happy-path test does not cover.</item>
/// </list>
/// Both are invisible without a second upload, in either direction, against an account that already has a picture.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class AttachmentReplacementTests
{
    /// <summary>
    /// Upload, then upload again with different content. The second call must succeed, must overwrite in place
    /// (still exactly two rows, since the key is <c>{ Id, Kind }</c> and the path is derived from it), and the served
    /// image must actually be the new one - a "successful" re-upload that leaves the old blob behind is the same bug
    /// wearing a green test.
    /// </summary>
    [TestMethod]
    public async Task ReUploadingAProfilePicture_Should_ReplaceItInPlace()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        try
        {
            await Upload(httpClient, SolidImage(MagickColors.Red));

            var first = await Download(httpClient, userId);
            Assert.AreEqual(2, await CountAttachments(server, userId), "One upload writes the small and the original kind.");

            await Upload(httpClient, SolidImage(MagickColors.Blue));

            Assert.AreEqual(2, await CountAttachments(server, userId),
                "A re-upload must overwrite the existing rows, not add a second pair - the key is { Id, Kind } and the blob path is derived from it.");

            CollectionAssert.AreNotEqual(first, await Download(httpClient, userId),
                "The served image must be the one just uploaded; an in-place overwrite that never wrote the new bytes would look identical here.");
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }

    /// <summary>
    /// The rejection path. A 100x100 upload is refused because it is smaller than the 256x256 the small kind needs -
    /// and the picture the user already had must be exactly as it was: same bytes served, still two rows, and
    /// <c>HasProfilePicture</c> still true.
    /// <para>
    /// The <c>HasProfilePicture</c> assertion is the one that matters most. It is written server-side after a
    /// successful upload and cleared only by <c>DeleteUserProfilePicture</c>, so a rejected upload that deleted the
    /// rows left the flag true with nothing behind it, and the user could neither see their picture nor delete it.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AnUploadRejectedAfterValidation_Should_LeaveTheExistingPictureUntouched()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        try
        {
            await Upload(httpClient, SolidImage(MagickColors.Red));
            var before = await Download(httpClient, userId);

            // The DI HttpClient turns a non-success response into an exception (See ExceptionDelegatingHandler); the
            // ImageTooSmall body is plain text, so the status code is what identifies the rejection.
            var rejected = await Assert.ThrowsExactlyAsync<HttpRequestException>(
                () => Upload(httpClient, SolidImage(MagickColors.Blue, 100)),
                "An image below the required size must be refused.");

            Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode);

            Assert.AreSequenceEqual(before, await Download(httpClient, userId), "The refused upload must not have touched the stored blob.");

            Assert.AreEqual(2, await CountAttachments(server, userId), "The refused upload must not have removed the existing rows.");

            Assert.IsTrue(await HasProfilePicture(server, userId),
                "HasProfilePicture must still be true. If a rejected upload can clear the blob while leaving this flag set, " +
                "the profile permanently points at something that is not there.");
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }


    /// <summary>
    /// The other rejection path this class's own header names as already paid for once: a file ImageMagick cannot
    /// decode. It must be refused as bad input (400, like the too-small rejection) rather than surfacing as a 500
    /// with a Critical log entry - and the picture the user already had must be exactly as it was.
    /// </summary>
    [TestMethod]
    public async Task AnUndecodableUpload_Should_BeRejectedAsBadRequest_AndLeaveTheExistingPictureUntouched()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        try
        {
            await Upload(httpClient, SolidImage(MagickColors.Red));
            var before = await Download(httpClient, userId);

            // Deterministic non-image bytes, posted with a spoofed image/png content type: the server must judge the
            // content, not the caller-controlled header. The DI HttpClient turns the non-success response into an
            // exception (See ExceptionDelegatingHandler); the rejection body is plain text, so the status identifies it.
            var garbageBytes = new byte[1024];
            Array.Fill(garbageBytes, (byte)'x');
            var rejected = await Assert.ThrowsExactlyAsync<HttpRequestException>(
                () => Upload(httpClient, garbageBytes),
                "A payload ImageMagick cannot decode must be refused as bad input.");

            Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode,
                "An undecodable upload is the caller's fault: a 500 here means it reached the unknown-error handler " +
                "and logged Critical from ordinary user input.");

            Assert.AreSequenceEqual(before, await Download(httpClient, userId), "The refused upload must not have touched the stored blob.");

            Assert.AreEqual(2, await CountAttachments(server, userId), "The refused upload must not have removed the existing rows.");

            Assert.IsTrue(await HasProfilePicture(server, userId));
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>A solid-colour PNG, so two uploads differ in content in a way the served WebP cannot hide.</summary>
    private static byte[] SolidImage(MagickColor color, uint size = 512)
    {
        using var image = new MagickImage(color, size, size);
        return image.ToByteArray(MagickFormat.Png);
    }

    private async Task Upload(HttpClient httpClient, byte[] imageBytes)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "picture.png"); // The endpoint binds an IFormFile parameter named "file".

        using var response = await httpClient.PostAsync("api/v1/Attachment/UploadUserProfilePicture", form, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<byte[]> Download(HttpClient httpClient, Guid userId)
    {
        // attachmentId for a profile picture is the user id; the small kind is the resized WebP.
        return await httpClient.GetByteArrayAsync(
            $"api/v1/Attachment/GetAttachment/{userId}/{AttachmentKind.UserProfileImageSmall}", TestContext.CancellationToken);
    }

    private async Task DeleteProfilePicture(HttpClient httpClient)
    {
        // Best effort: the per-run account's blobs and rows outlive the run otherwise, and a test that fails early
        // must not take the cleanup down with it.
        try
        {
            using var _ = await httpClient.DeleteAsync("api/v1/Attachment/DeleteUserProfilePicture", CancellationToken.None);
        }
        catch (Exception) { }
    }

    private async Task<int> CountAttachments(AppTestServer server, Guid attachmentId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Attachments.CountAsync(att => att.Id == attachmentId, TestContext.CancellationToken);
    }

    private async Task<bool> HasProfilePicture(AppTestServer server, Guid userId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Set<User>().Where(u => u.Id == userId).Select(u => u.HasProfilePicture).SingleAsync(TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
