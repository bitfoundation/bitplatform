using Bit.Bmage;
using Bit.Bmage.Pipeline;
using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Features.Attachments;

namespace Boilerplate.Tests.Features.Attachments;

[TestClass, TestCategory("IntegrationTest")]
public class UploadFormatAllowListTests
{
    [TestMethod]
    public async Task EveryAllowedFormat_Should_BeAccepted_AndKeptAsAFullSizeWebPOriginal()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var httpClient = client.HttpClient;

        try
        {
            foreach (var format in AttachmentController.AllowedImageFormats)
            {
                var image = format is ImageFormat.Avif or ImageFormat.Gif
                    ? TestImageFactory.Asset($"teal-512.{format.ToString().ToLowerInvariant()}")
                    : TestImageFactory.Solid(TestImageFactory.Teal, 512, 512, format);

                await Upload(httpClient, image);

                var original = TestImageFactory.Identify(await Download(httpClient, userId, AttachmentKind.UserProfileImageOriginal));

                Assert.AreEqual(ImageFormat.WebP, original.Format, $"A {format} upload must be accepted and its original stored as WebP.");
                Assert.AreEqual((512, 512), (original.Width, original.Height), $"The original of a {format} upload must keep its full size.");
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
            await Upload(httpClient, TestImageFactory.Solid(TestImageFactory.Red, 512, 512));

            var before = await Download(httpClient, userId, AttachmentKind.UserProfileImageSmall);

            foreach (var (format, bytes) in OtherFormats())
            {
                await Assert.ThrowsExactlyAsync<BadRequestException>(() => Upload(httpClient, bytes),
                    $"A {format} upload must be refused as bad input before it is decoded.");
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
    public void Bmage_Should_IdentifyNoOtherFormatAsAnAllowedOne()
    {
        using var services = new ServiceCollection().AddBitBmageHeadlessServices().BuildServiceProvider();
        using var scope = services.CreateScope();
        var imageProcessor = scope.ServiceProvider.GetRequiredService<ImageProcessor>();

        foreach (var (format, bytes) in OtherFormats())
        {
            Assert.IsFalse(AttachmentController.AllowedImageFormats.Contains(imageProcessor.Identify(bytes)?.Format ?? ImageFormat.Unknown),
                $"{format} was identified as an allowed format, so UploadAttachment would hand it to a decoder.");
        }

        var identified = imageProcessor.Identify(TestImageFactory.Solid(TestImageFactory.Teal, 16, 16));

        Assert.IsNotNull(identified);
        Assert.AreEqual(ImageFormat.Png, identified.Format);
    }

    private static IEnumerable<(string Format, byte[] Bytes)> OtherFormats()
    {
        yield return ("SVG", """<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512"><rect width="512" height="512" fill="red"/></svg>"""u8.ToArray());
        yield return ("PostScript", "%!PS-Adobe-3.0\n%%BoundingBox: 0 0 512 512\nnewpath 0 0 moveto 512 512 lineto stroke showpage\n"u8.ToArray());
        yield return ("PPM", [.. "P6\n512 512\n255\n"u8, .. new byte[512 * 512 * 3]]);

        var png = TestImageFactory.Solid(TestImageFactory.Teal, 64, 64);
        yield return ("ICO", [0, 0, 1, 0, 1, 0, 64, 64, 0, 0, 1, 0, 32, 0, .. BitConverter.GetBytes(png.Length), 22, 0, 0, 0, .. png]);
        yield return ("TIFF", [0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
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
