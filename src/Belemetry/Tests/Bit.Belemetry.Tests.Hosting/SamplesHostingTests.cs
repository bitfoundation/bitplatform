#if NET10_0_OR_GREATER
using System.Net;
using Bit.Belemetry.Tests.Hosting.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Belemetry.Tests.Hosting;

internal static class SampleHostFactories
{
    public static readonly Lazy<WebApplicationFactory<Bit.Belemetry.Samples.Server.Components.App>> Server = new(() => Isolate(new WebApplicationFactory<Bit.Belemetry.Samples.Server.Components.App>()));
    public static readonly Lazy<WebApplicationFactory<Bit.Belemetry.Samples.Wasm.Components.App>> WebAssembly = new(() => Isolate(new WebApplicationFactory<Bit.Belemetry.Samples.Wasm.Components.App>()));
    public static readonly Lazy<WebApplicationFactory<Bit.Belemetry.Samples.Auto.Components.App>> Auto = new(() => Isolate(new WebApplicationFactory<Bit.Belemetry.Samples.Auto.Components.App>()));

    public static HttpClient CreateClient(string sample) => sample switch
    {
        "server" => Server.Value.CreateClient(),
        "wasm" => WebAssembly.Value.CreateClient(),
        "auto" => Auto.Value.CreateClient(),
        _ => throw new ArgumentOutOfRangeException(nameof(sample), sample, null)
    };

    public static async Task DisposeAsync()
    {
        if (Server.IsValueCreated) await Server.Value.DisposeAsync();
        if (WebAssembly.IsValueCreated) await WebAssembly.Value.DisposeAsync();
        if (Auto.IsValueCreated) await Auto.Value.DisposeAsync();
    }

    /// <summary>
    /// The samples run as they ship, except that their exports go nowhere: no request to a local collector that may not
    /// be running, and nothing persisted to this machine for a retry.
    /// </summary>
    private static WebApplicationFactory<T> Isolate<T>(WebApplicationFactory<T> factory) where T : class =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(new BelemetryHttpMessageHandlerProvider(new AcceptingHandler()));
            services.AddBitBelemetryServices(o => o.Persistence.Enabled = false);
        }));

    private sealed class AcceptingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }
}

/// <summary>
/// The hosting samples are what the README hands to people as the way to wire Belemetry into each render mode, so they
/// are booted as they ship - their own Program.cs and App.razor - rather than trusted to keep matching the harness.
/// </summary>
[TestClass]
public class SamplesHostingTests
{
    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_home_page_is_prerendered_with_the_browser_config(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(html, "<h1>Belemetry samples</h1>");

        using var config = RenderedHtml.BrowserConfig(html);
        Assert.IsNotNull(config, "The sample does not render <BelemetryScript />.");
        Assert.AreEqual($"belemetry-samples-{sample}", config.RootElement.GetProperty("service").GetProperty("name").GetString());

        var otlp = config.RootElement.GetProperty("exporters").EnumerateArray().Single(e => e.GetProperty("type").GetString() == "otlp");
        Assert.AreEqual("http://localhost:4318/", otlp.GetProperty("endpoint").GetString());
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_script_and_the_shared_pages_assets_are_served(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);

        foreach (var asset in new[] { "/_content/Bit.Belemetry/bit-belemetry.js", "/_content/Bit.Belemetry.Samples.Core/samples.js" })
        {
            using var response = await client.GetAsync(asset);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, asset);
            Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType, asset);
        }
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_fetch_target_answers(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/api/weather");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
#endif
