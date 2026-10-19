using ImageMagick;
using System.Net.Http.Headers;

namespace Boilerplate.Tests.E2E.Features.ErrorHandling;

/// <summary>
/// An api action may answer with a plain localized message instead of a problem details json, such as
/// <c>AttachmentController</c>'s image validation errors. The client's <c>ExceptionDelegatingHandler</c> shows it to
/// the user only when the response carries the Request-Id header the api stamps on every response (See
/// <c>UseRequestIdHeader</c>), so a plain text body written by something in front of the api is never mistaken for one.
/// <para>
/// <c>Boilerplate.Tests</c> pins both halves against a test server. What this one asks is whether the header survives
/// the way to a real user: through Cloudflare, which may drop or rewrite the headers of what it passes on.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public class PlainTextApiErrorTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>Sales hosts its api itself, AdminPanel calls the standalone one - the two hosting shapes.</summary>
    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (standalone API)")]
    public async Task ARejectedUpload_Should_ShowTheUserTheServersOwnMessage(App app)
    {
        var cancellationToken = TestContext.CancellationToken;

        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.ApiOf(app));
        var authManager = apiClient.Services.GetRequiredService<AuthManager>();

        await authManager.SignIn(new() { Email = StoreUser.Email, Password = StoreUser.Password }, cancellationToken);

        try
        {
            // Below the 256 x 256 the small kind needs, so UploadAttachment turns it away with ImageTooSmall before it
            // touches the picture StoreUser already has.
            var rejected = await Assert.ThrowsExactlyAsync<BadRequestException>(() => Upload(apiClient, TooSmallPng()),
                $"{app}'s api should turn a too small picture away, and the client should read why from the response.");

            Assert.Contains("100x100", rejected.Message,
                $"{app} showed '{rejected.Message}' instead of the api's own explanation of the rejection.");
        }
        finally
        {
            await authManager.SignOut(CancellationToken.None);
        }
    }

    /// <summary>
    /// Not only on the error responses: a plain text error never goes through <c>ApiServerExceptionHandler</c>, so the
    /// header has to be on everything the api answers. A query key of its own, so no edge cache answers for the api.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi (standalone API)")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi (standalone API)")]
    public async Task EveryApiResponse_Should_CarryARequestId(string host)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        using var response = await httpClient.GetAsync(new Uri(new Uri(host), $"e2e/route/that-does-not-exist?e2e={Guid.NewGuid()}"), TestContext.CancellationToken);

        Assert.IsTrue(response.Headers.TryGetValues("Request-Id", out var requestId) && string.IsNullOrWhiteSpace(requestId.Single()) is false,
            $"{host} answered {(int)response.StatusCode} without a Request-Id, so the client reads its errors as written by a proxy in front of it.");
    }

    private static byte[] TooSmallPng()
    {
        using var image = new MagickImage(MagickColors.Teal, 100, 100);
        return image.ToByteArray(MagickFormat.Png);
    }

    private async Task Upload(DeployedApiClient apiClient, byte[] picture)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(picture);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "e2e-too-small.png"); // The endpoint binds an IFormFile parameter named "file".

        using var response = await apiClient.HttpClient.PostAsync("api/v1/Attachment/UploadUserProfilePicture", form, TestContext.CancellationToken);
    }
}
