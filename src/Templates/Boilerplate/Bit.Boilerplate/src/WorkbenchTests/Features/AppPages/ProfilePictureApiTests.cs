using System.Net.Http.Headers;
using Boilerplate.Workbench.Infrastructure.Services.StandIns;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>
/// The profile picture upload the Workbench takes at the app's address: as the server's, it keeps only an image the page
/// accepts, told by its bytes, as the picture's address answers anyone with it.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ProfilePictureApiTests
{
    public TestContext TestContext { get; set; } = default!;

    private WorkbenchTestServer server = default!;
    private HttpClient client = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        server = await WorkbenchTestsInitializer.SharedServer;
        client = server.CreateClient();
    }

    [TestCleanup]
    public void Cleanup() => client.Dispose();

    [TestMethod]
    [DataRow("text/html", "<script>alert(document.domain)</script>")]
    [DataRow("image/svg+xml", """<svg xmlns="http://www.w3.org/2000/svg"><script>alert(document.domain)</script></svg>""")]
    [DataRow("image/png", "<html><script>alert(document.domain)</script></html>")]
    public async Task WhatIsNoImageThePageAccepts_Should_BeRefused(string declaredType, string content)
    {
        using var form = new MultipartFormDataContent();
        var file = new StringContent(content);
        file.Headers.ContentType = new(declaredType);
        form.Add(file, "file", "picture");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Attachment/UploadUserProfilePicture") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", WorkbenchClaims.AccessToken(WorkbenchClaims.Of(new()), DateTimeOffset.UtcNow, elevatedUntil: null));

        using var response = await client.SendAsync(request, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(AppStrings.UnsupportedImageFormat, (await response.Content.ReadFromJsonAsync<string>(TestContext.CancellationToken)));
    }
}
