//+:cnd:noEmit
using ImageMagick;
using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// What an Article 15 / 20 answer has to contain, and what it must not.
/// <para>
/// The failure pinned here is silent rather than loud: a store whose source was never registered produces a zip that
/// looks complete and is not. Asserting on the sections is what makes a forgotten registration fail here.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class PersonalDataExportTests
{
    public required TestContext TestContext { get; set; }

    /// <summary>
    /// The whole path as a user drives it. The picture matters because it is the one part that is not a database row:
    /// a source that exports metadata and forgets the blob hands over a description of a photograph.
    /// </summary>
    [TestMethod]
    public async Task ExportingOwnData_Should_ContainEveryStore_AndTheProfilePicture()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        await UploadProfilePicture(httpClient);

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        using var response = await httpClient.GetAsync(IUserController.ExportPersonalDataUri, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType;
        Assert.IsNotNull(contentType, "The response has to declare what it is; the client saves the file by it.");
        Assert.AreEqual("application/zip", contentType.MediaType, "The download has to arrive as a zip for the client to save it as one.");

        using var archive = new ZipArchive(await response.Content.ReadAsStreamAsync(TestContext.CancellationToken), ZipArchiveMode.Read);

        var dataEntry = archive.GetEntry("data.json");
        Assert.IsNotNull(dataEntry, "data.json is the machine-readable half Article 20 asks for; the zip is only its envelope.");

        var sections = JsonNode.Parse(dataEntry.Open())!["sections"]!;

        Assert.AreEqual(email, sections["account"]!["data"]!["email"]!.GetValue<string>(),
            "The account section has to describe the signed-in user, not whoever the endpoint happened to look up.");

        Assert.IsNotEmpty(sections["sessions"]!["data"]!.AsArray(),
            "The request itself was made from a signed-in session, so at least that one must appear.");

        Assert.IsNotNull(archive.GetEntry("files/attachments/UserProfileImageSmall.webp"),
            "The profile picture was uploaded above and is the person's own data - exporting only its row leaves them with a description of a photograph.");

        foreach (var (key, section) in sections.AsObject())
        {
            Assert.IsNotNull(section!["purpose"], $"Section '{key}' has no purpose: Article 15(1)(a) asks why the data is held, and a dump of rows does not answer it.");
            Assert.IsNotNull(section["retention"], $"Section '{key}' has no retention: Article 15(1)(d) asks how long it is kept.");
        }
    }

    /// <summary>
    /// A copy of an entire identity is worth as much to a stolen token as deleting it is, so it sits behind the same
    /// gate <c>Delete</c> uses rather than behind merely being signed in.
    /// </summary>
    [TestMethod]
    public async Task ExportingOwnData_Should_RequireElevatedAccess()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => httpClient.GetAsync(IUserController.ExportPersonalDataUri, TestContext.CancellationToken),
            "Being signed in is not enough: the export is behind ELEVATED_ACCESS.");
    }

    /// <summary>
    /// Credentials prove the person is who they say they are; handing back their own password hash informs them of
    /// nothing and weakens the account. Article 15(4) is what allows leaving them out.
    /// </summary>
    [TestMethod]
    public async Task ExportingOwnData_Should_NotIncludeCredentials()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        using var response = await httpClient.GetAsync(IUserController.ExportPersonalDataUri, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var archive = new ZipArchive(await response.Content.ReadAsStreamAsync(TestContext.CancellationToken), ZipArchiveMode.Read);

        using var reader = new StreamReader(archive.GetEntry("data.json")!.Open());
        var dataJson = await reader.ReadToEndAsync(TestContext.CancellationToken);

        foreach (var credential in (string[])["passwordHash", "securityStamp", "concurrencyStamp", "p256dh"])
        {
            Assert.DoesNotContain(credential, dataJson, StringComparison.OrdinalIgnoreCase,
                $"'{credential}' reached the export. A source is projecting an entity instead of its own export record.");
        }
    }

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
}
