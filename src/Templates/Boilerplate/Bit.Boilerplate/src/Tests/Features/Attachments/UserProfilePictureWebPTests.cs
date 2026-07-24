using ImageMagick;
using System.Net.Http.Headers;
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Attachments;

[TestClass, TestCategory("IntegrationTest")]
public partial class UserProfilePictureWebPTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Verifies the server-side image pipeline of <c>AttachmentController.UploadUserProfilePicture</c>: uploading a
    /// NON-webp source image (the repo's 512x512 <c>images/icons/bit-icon-512.png</c>) must produce a
    /// <see cref="AttachmentKind.UserProfileImageSmall"/> attachment that is resized to 256x256 and re-encoded to WebP
    /// via ImageMagick (<c>sourceImage.ToByteArray(MagickFormat.WebP)</c>). After signing in with the seeded default
    /// account, the test POSTs the PNG as multipart/form-data through the authenticated DI <see cref="HttpClient"/>
    /// (the <c>AuthDelegatingHandler</c> attaches the bearer token automatically), then GETs the small attachment back
    /// (<c>GetAttachment/{userId}/UserProfileImageSmall</c>, an anonymous endpoint) and asserts the downloaded bytes
    /// really decode as WebP - and are 256x256 - proving the format conversion actually happened rather than the
    /// original PNG being served back.
    /// </summary>
    [TestMethod]
    public async Task UploadUserProfilePicture_Should_StoreAndServeSmallImageAsWebP()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // Sign in with the seeded default account (id 8ff71671-a1d6-4f97-abb9-d87d7b47d6e7). A raw HttpClient resolved
        // from the same scope shares the token store, so requests it sends are authenticated by AuthDelegatingHandler.
        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
        await authManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var currentUser = await scope.ServiceProvider.GetRequiredService<IUserController>()
            .GetCurrentUser(TestContext.CancellationToken);

        // Real, non-webp repo image served at the web root (512x512 PNG >= the 256x256 minimum, so it is not rejected
        // with ImageTooSmall). Downloading it through the running server avoids brittle on-disk asset paths.
        var sourceImageBytes = await httpClient.GetByteArrayAsync("images/icons/bit-icon-512.png", TestContext.CancellationToken);

        // Sanity: the source really is NOT WebP, otherwise the assertion below would be meaningless.
        Assert.AreNotEqual(MagickFormat.WebP, new MagickImageInfo(sourceImageBytes).Format);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(sourceImageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        // The endpoint binds an IFormFile parameter named "file".
        form.Add(fileContent, "file", "bit-icon-512.png");

        using var uploadResponse = await httpClient.PostAsync("api/v1/Attachment/UploadUserProfilePicture", form, TestContext.CancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        // Download the generated small (256x256) attachment. attachmentId for a profile picture is the user id.
        var downloadedBytes = await httpClient.GetByteArrayAsync(
            $"api/v1/Attachment/GetAttachment/{currentUser.Id}/{AttachmentKind.UserProfileImageSmall}",
            TestContext.CancellationToken);

        var downloadedInfo = new MagickImageInfo(downloadedBytes);

        // The core assertion: the stored/served small profile image is genuinely WebP, regardless of the PNG we uploaded.
        Assert.AreEqual(MagickFormat.WebP, downloadedInfo.Format);
        Assert.AreEqual(256u, downloadedInfo.Width);
        Assert.AreEqual(256u, downloadedInfo.Height);
    }
}
