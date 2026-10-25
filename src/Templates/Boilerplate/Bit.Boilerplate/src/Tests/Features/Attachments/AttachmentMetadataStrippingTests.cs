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
        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var httpClient = client.HttpClient;

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
    /// A phone photo's pixels are stored sideways with an EXIF tag that says how to turn them, so stripping the EXIF
    /// must not leave the picture lying on its side. Its ICC profile stays, or a wide-gamut photo loses its colors.
    /// </summary>
    [TestMethod]
    public async Task StrippingAPhonePhoto_Should_KeepItUpright_AndKeepItsColorProfile()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var httpClient = client.HttpClient;

        var uploaded = SidewaysJpegWithColorProfile();

        using (var check = new MagickImage(uploaded))
        {
            Assert.AreEqual(Orientation.RightTop, check.Orientation, "The fixture must actually be sideways, otherwise this test passes without checking anything.");
        }

        try
        {
            await Upload(httpClient, uploaded);

            using var original = new MagickImage(await Download(httpClient, userId, AttachmentKind.UserProfileImageOriginal));

            Assert.AreEqual((300u, 600u), (original.Width, original.Height), "The original was stored lying on its side.");
            Assert.IsNotNull(original.GetColorProfile(), "The original lost its ICC profile.");
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>512px so it clears the 256px floor the small kind enforces, as a JPEG because that is what a camera produces.</summary>
    private static byte[] JpegWithExif()
    {
        using var image = TestImageFactory.Solid(MagickColors.Teal, 512, 512);

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Software, "boilerplate-test");
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        image.SetProfile(exif);

        return image.ToByteArray(MagickFormat.Jpeg);
    }

    /// <summary>600 x 300 pixels with EXIF orientation 6 (turn 90 degrees clockwise), so it shows as 300 x 600.</summary>
    private static byte[] SidewaysJpegWithColorProfile()
    {
        using var image = TestImageFactory.Solid(MagickColors.Teal, 600, 300);

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)6);
        image.SetProfile(exif);
        image.Orientation = Orientation.RightTop;
        image.SetProfile(ColorProfiles.AdobeRGB1998);

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
