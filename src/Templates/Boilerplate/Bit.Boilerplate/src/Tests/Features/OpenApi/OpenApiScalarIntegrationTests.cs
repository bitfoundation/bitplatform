using System.Net;

namespace Boilerplate.Tests.Features.OpenApi;

[TestClass, TestCategory("IntegrationTest")]
public partial class OpenApiScalarIntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Verifies the API documentation surface is wired up end-to-end (See <c>Program.Middlewares</c>: <c>MapOpenApi</c>
    /// and <c>MapScalarApiReference</c>, both output-cached). Two things must hold for the docs to actually work:
    /// <list type="number">
    /// <item>
    /// <c>GET /openapi/v1.json</c> returns the generated OpenAPI document - an anonymous endpoint delivered as
    /// <c>application/json</c> whose body is well-formed JSON carrying the mandatory top-level <c>openapi</c> version
    /// string (3.x, per <c>AddOpenApi</c>'s <c>OpenApiSpecVersion.OpenApi3_1</c>) and a <c>paths</c> object.
    /// </item>
    /// <item>
    /// <c>GET /scalar</c> returns the Scalar API reference UI as an HTML page. Scalar is the interactive client that
    /// renders the document above (it is also what <c>/swagger</c> redirects to), so serving its HTML proves the
    /// reference UI is mounted.
    /// </item>
    /// </list>
    /// Both endpoints are anonymous, so no sign-in is required; requests go through the DI <see cref="HttpClient"/>
    /// resolved from a request scope (its BaseAddress is the test server address).
    /// </summary>
    [TestMethod]
    public async Task OpenApiAndScalar_Should_BeServed()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // 1) The generated OpenAPI document must be valid JSON describing the API surface.
        using var openApiResponse = await httpClient.GetAsync("openapi/v1.json", TestContext.CancellationToken);

        var openApiBody = await openApiResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, openApiResponse.StatusCode, $"The OpenAPI document endpoint must be mapped. Body: '{openApiBody}'.");
        Assert.AreEqual("application/json", openApiResponse.Content.Headers.ContentType?.MediaType);

        using var openApiJson = JsonDocument.Parse(openApiBody);
        // Every OpenAPI 3.x document has a top-level "openapi" version string and a "paths" object.
        Assert.IsTrue(openApiJson.RootElement.TryGetProperty("openapi", out var openApiVersion), "The OpenAPI document must declare its 'openapi' version.");
        Assert.IsTrue(openApiVersion.GetString()?.StartsWith("3.") is true, $"Unexpected OpenAPI version '{openApiVersion.GetString()}'.");
        Assert.IsTrue(openApiJson.RootElement.TryGetProperty("paths", out _), "The OpenAPI document must contain a 'paths' section.");

        // 2) The Scalar API reference UI (which renders the document above) must be served as an HTML page.
        using var scalarResponse = await httpClient.GetAsync("scalar", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, scalarResponse.StatusCode, "The Scalar API reference UI must be mapped.");
        Assert.AreEqual("text/html", scalarResponse.Content.Headers.ContentType?.MediaType);

        var scalarBody = await scalarResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        // The rendered reference page bootstraps the Scalar client, so its markup references "scalar".
        Assert.IsTrue(scalarBody.Contains("scalar", StringComparison.OrdinalIgnoreCase), "The Scalar UI HTML was not returned.");
    }
}
