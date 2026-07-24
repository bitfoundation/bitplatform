namespace Boilerplate.Tests.Features.WellKnown;

[TestClass, TestCategory("IntegrationTest")]
public partial class AppleAppSiteAssociationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// iOS Universal Links require the host to serve the extension-less
    /// <c>/.well-known/apple-app-site-association</c> file with a <c>Content-Type</c> of <c>application/json</c>;
    /// Apple's CDN silently rejects the file when it is returned as any other media type. Because the file has no
    /// extension, the default <c>UseStaticFiles</c> content-type provider cannot infer a type, so the host must serve
    /// the <c>.well-known</c> folder with <c>ServeUnknownFileTypes = true</c> and
    /// <c>DefaultContentType = "application/json"</c> (See <c>Boilerplate.Server.Web/Program.Middlewares.cs</c>). The
    /// physical file ships in <c>Boilerplate.Client.Web/wwwroot/.well-known/apple-app-site-association</c> and is served
    /// by the Server.Web host through the Blazor static-assets pipeline. This test requests the file over HTTP through
    /// the DI <see cref="HttpClient"/> and asserts a 200 response whose body is well-formed JSON delivered as
    /// <c>application/json</c>.
    /// </summary>
    [TestMethod]
    public async Task AppleAppSiteAssociation_Should_BeServedAsJson()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var appleAppSiteAssociationUrl = new Uri(server.WebAppServerAddress, "/.well-known/apple-app-site-association");

        using var response = await httpClient.GetAsync(appleAppSiteAssociationUrl, TestContext.CancellationToken);

        Assert.AreEqual(200, (int)response.StatusCode, "The apple-app-site-association file must be served by the host.");

        // Apple only accepts the association file when it is delivered as application/json (no other media type works).
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        // The payload must be well-formed JSON describing the app's associated domains.
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        using var json = JsonDocument.Parse(body);
        Assert.IsTrue(json.RootElement.TryGetProperty("applinks", out _), "The association file must contain an 'applinks' section.");
    }
}
