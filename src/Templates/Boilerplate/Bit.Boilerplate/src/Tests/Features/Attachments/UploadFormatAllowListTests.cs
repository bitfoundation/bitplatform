using ImageMagick;
using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Features.Attachments;

namespace Boilerplate.Tests.Features.Attachments;

[TestClass, TestCategory("IntegrationTest")]
public class UploadFormatAllowListTests
{
    [TestMethod]
    public async Task EveryAllowedFormat_Should_BeAccepted_AndKeptAsTheOriginal()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var httpClient = client.HttpClient;

        try
        {
            foreach (var format in AttachmentController.AllowedImageFormats)
            {
                using var image = TestImageFactory.Solid(MagickColors.Teal, 512, 512);

                await Upload(httpClient, image.ToByteArray(format));

                using var original = new MagickImage(await Download(httpClient, userId, AttachmentKind.UserProfileImageOriginal));

                Assert.AreEqual(format, original.Format, $"A {format} upload must be accepted and served back as {format}.");
            }
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }

    [TestMethod]
    public async Task AnUploadInAnyOtherFormat_Should_BeRefused_AndLeaveTheExistingPictureUntouched()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var httpClient = client.HttpClient;

        try
        {
            using (var image = TestImageFactory.Solid(MagickColors.Red, 512, 512))
            {
                await Upload(httpClient, image.ToByteArray(MagickFormat.Png));
            }

            var before = await Download(httpClient, userId, AttachmentKind.UserProfileImageSmall);

            foreach (var (format, bytes) in OtherFormats())
            {
                await Assert.ThrowsExactlyAsync<BadRequestException>(() => Upload(httpClient, bytes),
                    $"A {format} upload must be refused as bad input before ImageMagick decodes it.");
            }

            var after = await Download(httpClient, userId, AttachmentKind.UserProfileImageSmall);
            Assert.AreSequenceEqual(before, after, "A refused upload must not have touched the stored picture.");
        }
        finally
        {
            await DeleteProfilePicture(httpClient);
        }
    }

    [TestMethod]
    public void ImageMagick_Should_RefuseEveryCoderOutsideTheAllowedFormats()
    {
        foreach (var (format, bytes) in OtherFormats())
        {
            Assert.ThrowsExactly<MagickPolicyErrorException>(() => new MagickImageInfo(bytes),
                $"ImageMagick handed {format} to a coder: the security policy Program.Services sets before anything else touches ImageMagick is not in effect.");
        }

        using var allowed = TestImageFactory.Solid(MagickColors.Teal, 16, 16);

        Assert.AreEqual(MagickFormat.Png, new MagickImageInfo(allowed.ToByteArray(MagickFormat.Png)).Format);
    }

    private static IEnumerable<(string Format, byte[] Bytes)> OtherFormats()
    {
        yield return ("SVG", """<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512"><rect width="512" height="512" fill="red"/></svg>"""u8.ToArray());
        yield return ("PostScript", "%!PS-Adobe-3.0\n%%BoundingBox: 0 0 512 512\nnewpath 0 0 moveto 512 512 lineto stroke showpage\n"u8.ToArray());
        yield return ("BMP", Bitmap(512, 512));
    }

    private static byte[] Bitmap(int width, int height)
    {
        var rowSize = (width * 3 + 3) / 4 * 4;
        var pixelDataSize = rowSize * height;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("BM"u8);
        writer.Write(54 + pixelDataSize);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);
        writer.Write(new byte[pixelDataSize]);
        writer.Flush();

        return stream.ToArray();
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        return server;
    }

    private async Task Upload(HttpClient httpClient, byte[] imageBytes)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "picture.png");

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
