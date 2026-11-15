//+:cnd:noEmit
using Bit.Belemetry;
using Microsoft.Extensions.Configuration;

namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// A deployment's servers send their telemetry to the dashboard over gRPC inside the Docker Compose network: an address a
/// page and an app can't reach, in a protocol a page can't speak. So the CD builds the app with the web app's own address
/// as <c>Belemetry:OtlpEndpoint</c>, whose <c>/v1/</c> paths the tunnel (or Caddy) sends to the dashboard's OTLP/HTTP
/// port (See .docs/15, In a Deployment).
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class BrowserTelemetryTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Server.Web writes the browser script's settings into every page it serves, before Blazor starts and in Blazor
    /// Server, where nothing else would export what only the page sees: its errors, page views and web vitals.
    /// </summary>
    [TestMethod]
    [DataRow("https://app.example.invalid", true)]
    [DataRow("https://user:password@app.example.invalid", false)]
    [DataRow("ftp://app.example.invalid", false)]
    public async Task ThePage_Should_PostToTheBelemetryOtlpEndpoint_WhileTheServerSendsOverGrpc(string endpoint, bool reachesThePage)
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfiguration: configuration =>
            {
                configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://compose-dashboard:18889";
                configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
                configuration["Belemetry:OtlpEndpoint"] = endpoint;
            })
            .Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppAddress };

        var html = await visitorHttpClient.GetStringAsync("/", TestContext.CancellationToken);

        Assert.AreEqual(reachesThePage, html.Contains("{\"type\":\"otlp\"", StringComparison.Ordinal), "A page can't speak gRPC, so it gets an OTLP exporter only through the Belemetry:OtlpEndpoint.");
        if (reachesThePage)
        {
            Assert.Contains($"{{\"type\":\"otlp\",\"endpoint\":\"{endpoint}/\",\"protocol\":\"http/protobuf\"", html);
        }
        Assert.DoesNotContain("compose-dashboard", html, "A page never gets the servers' own endpoint.");
    }

    /// <summary>
    /// The WebAssembly app and the Windows, Android and iOS apps have no other endpoint, so their .NET side posts there
    /// too, over OTLP/HTTP whatever the protocol the servers use.
    /// </summary>
    [TestMethod]
    public void AClient_Should_PostToTheBelemetryOtlpEndpoint_WhenItIsTheOnlyOne()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
            ["Belemetry:OtlpEndpoint"] = "https://app.example.invalid"
        }).Build();

        var otlp = new BelemetryOptions().ConfigureAppTelemetry(configuration).OtlpExporters.Single();

        Assert.AreEqual(new Uri("https://app.example.invalid"), otlp.Endpoint);
        Assert.AreEqual(BelemetryOtlpProtocol.HttpProtobuf, otlp.Protocol);
        Assert.AreEqual(otlp.Endpoint, otlp.BrowserEndpoint);
    }

    /// <summary>
    /// The CSP allows the address the page posts to: Belemetry:OtlpEndpoint, or the server's own OTLP/HTTP endpoint when
    /// that one is ignored.
    /// </summary>
    [TestMethod]
    [DataRow("https://app.example.invalid", "https://app.example.invalid")]
    [DataRow("https://user:password@app.example.invalid", "https://collector.example.invalid")]
    public void TheCsp_Should_AllowTheAddressThePagePostsTo(string browserEndpoint, string pagePostsTo)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://collector.example.invalid",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
            ["Belemetry:OtlpEndpoint"] = browserEndpoint
        }).Build();

        var otlp = new BelemetryOptions().ConfigureAppTelemetry(configuration).OtlpExporters.Single();
        var csp = Boilerplate.Client.Core.Components.Layout.ContentSecurityPolicy.BuildCspString(new(),
            new AbsoluteServerAddressProvider { GetAddress = () => new Uri("https://api.example.invalid/") }, configuration);
        var connectSrc = csp.Split(';').Single(directive => directive.Trim().StartsWith("connect-src ", StringComparison.Ordinal));

        Assert.AreEqual(pagePostsTo, (otlp.BrowserEndpoint ?? otlp.Endpoint)!.GetLeftPart(UriPartial.Authority));
        Assert.Contains($" {pagePostsTo}", connectSrc);
        if (pagePostsTo != browserEndpoint)
        {
            Assert.DoesNotContain("app.example.invalid", connectSrc, "An ignored Belemetry:OtlpEndpoint isn't allowed either.");
        }
    }
}
