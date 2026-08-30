using ImageMagick;
using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Tests.Features.Attachments;

/// <summary>
/// A phone photo carries an EXIF block with GPS coordinates, capture time and camera serial. Uploading one as a profile
/// picture must not put any of that in a blob that <c>GetAttachment</c> serves anonymously - the resize does not remove
/// it on its own, and the <c>*Original</c> kind used to be stored byte for byte.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class AttachmentMetadataStrippingTests
{
    [TestMethod]
    public async Task UploadingAPictureWithExif_Should_StoreNeitherKindWithIt()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var uploaded = JpegWithExif();

        using (var check = new MagickImage(uploaded))
        {
            Assert.IsNotNull(check.GetExifProfile(), "The fixture must actually carry EXIF, otherwise this test passes without checking anything.");
        }

        try
        {
            await Upload(httpClient, uploaded);

            foreach (var kind in new[] { AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal })
            {
                using var stored = new MagickImage(await Download(httpClient, userId, kind));

                Assert.IsNull(stored.GetExifProfile(),
                    $"{kind} kept its EXIF. GetAttachment is anonymous, so the GPS coordinates of wherever the photo was taken are then public to anyone holding the user id.");
            }
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }

    /// <summary>
    /// Stripping means re-encoding, so the one thing the <c>*Original</c> kind must still keep is the format it arrived
    /// in - <c>GetAttachment</c> serves it as <c>application/octet-stream</c> and nothing records what it was.
    /// </summary>
    [TestMethod]
    public async Task StrippingTheOriginal_Should_LeaveItInTheFormatItWasUploadedIn()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        try
        {
            await Upload(httpClient, JpegWithExif());

            using var original = new MagickImage(await Download(httpClient, userId, AttachmentKind.UserProfileImageOriginal));

            Assert.AreEqual(MagickFormat.Jpeg, original.Format, "The original kind must not be silently converted to the resized kind's format.");
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

    /// <summary>512px so it clears the 256px floor the small kind enforces, as a JPEG because that is what a camera produces.</summary>
    private static byte[] JpegWithExif()
    {
        using var image = new MagickImage(MagickColors.Teal, 512, 512);

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Software, "boilerplate-test");
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        image.SetProfile(exif);

        return image.ToByteArray(MagickFormat.Jpeg);
    }

    private async Task Upload(HttpClient httpClient, byte[] imageBytes)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "picture.jpg"); // The endpoint binds an IFormFile parameter named "file".

        using var response = await httpClient.PostAsync("api/v1/Attachment/UploadUserProfilePicture", form, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<byte[]> Download(HttpClient httpClient, Guid userId, AttachmentKind kind)
    {
        return await httpClient.GetByteArrayAsync($"api/v1/Attachment/GetAttachment/{userId}/{kind}", TestContext.CancellationToken);
    }

    private async Task DeleteProfilePicture(HttpClient httpClient)
    {
        try
        {
            using var _ = await httpClient.DeleteAsync("api/v1/Attachment/DeleteUserProfilePicture", CancellationToken.None);
        }
        catch (Exception) { }
    }

    public TestContext TestContext { get; set; } = default!;
}
