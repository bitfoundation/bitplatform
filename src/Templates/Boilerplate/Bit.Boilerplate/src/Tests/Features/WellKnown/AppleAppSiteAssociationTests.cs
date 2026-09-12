namespace Boilerplate.Tests.Features.WellKnown;

[TestClass, TestCategory("IntegrationTest")]
public partial class AppleAppSiteAssociationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// iOS Universal Links require the host to serve the extension-less
    /// <c>/.well-known/apple-app-site-association</c> file with a <c>Content-Type</c> of <c>application/json</c>;
    /// Apple's CDN silently rejects the file when it is returned as any other media type. Nothing can infer that type
    /// from a path with no extension - Azure Static Web Apps goes as far as overriding a route's own header with
    /// <c>application/octet-stream</c> - so the file on disk is
    /// <c>Boilerplate.Client.Web/wwwroot/.well-known/apple-app-site-association.json</c>, whose extension earns it the
    /// right type, and both hosts rewrite the extension-less url to it: Server.Web in its <c>/.well-known</c> branch
    /// (See <c>Program.Middlewares.cs</c>) and Static Web Apps in <c>staticwebapp.config.json</c>. This test requests
    /// the url iOS asks for and asserts a 200 whose body is well-formed JSON delivered as <c>application/json</c>.
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
        var contentType = response.Content.Headers.ContentType;

        Assert.IsNotNull(contentType);
        Assert.AreEqual("application/json", contentType.MediaType);

        // The payload must be well-formed JSON describing the app's associated domains.
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        using var json = JsonDocument.Parse(body);
        Assert.IsTrue(json.RootElement.TryGetProperty("applinks", out _), "The association file must contain an 'applinks' section.");
    }

    /// <summary>
    /// Both hosts reach the single file by rewriting, so the rewrite is the thing that can go missing. Server.Web's is
    /// covered by the test above; this one pins Static Web Apps', which no test can exercise - the platform serves
    /// those apps, not Kestrel - along with the <c>.json</c> mime type that is the whole point of the rewrite.
    /// </summary>
    [TestMethod]
    public async Task StaticWebAppsConfig_Should_RewriteTheAssociationUrl_ToTheJsonFile()
    {
        const string configRelativePath = @"src\Client\Boilerplate.Client.Web\wwwroot\staticwebapp.config.json";

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && File.Exists(Path.Combine(directory.FullName, configRelativePath)) is false)
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, $"No {configRelativePath} above the test binaries.");

        var wwwroot = Path.GetDirectoryName(Path.Combine(directory.FullName, configRelativePath))!;

        Assert.IsTrue(File.Exists(Path.Combine(wwwroot, @".well-known\apple-app-site-association.json")),
            "The association file itself is gone; both hosts rewrite the extension-less url to it.");

        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory.FullName, configRelativePath), TestContext.CancellationToken));

        var route = config.RootElement.GetProperty("routes").EnumerateArray()
            .SingleOrDefault(r => r.GetProperty("route").GetString() == "/.well-known/apple-app-site-association");

        Assert.AreNotEqual(JsonValueKind.Undefined, route.ValueKind,
            "staticwebapp.config.json has no route for the url iOS asks for, so Static Web Apps would answer 404.");

        Assert.AreEqual("/.well-known/apple-app-site-association.json", route.GetProperty("rewrite").GetString(),
            "The route must rewrite to the .json file; a headers-only route does not work, Static Web Apps overrides Content-Type from the extension.");

        Assert.AreEqual("application/json", config.RootElement.GetProperty("mimeTypes").GetProperty(".json").GetString(),
            "Without this mime type the rewrite lands on a file Static Web Apps still refuses to call json.");
    }
}
