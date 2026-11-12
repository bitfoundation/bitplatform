#if NET11_0_OR_GREATER
using System.Net;
using Bit.Bswup.Tests.Hosting.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bit.Bswup.Tests.Hosting;

internal static class SampleHostFactories
{
    public static readonly Lazy<WebApplicationFactory<Bit.Bswup.FullSample.Server.Components.App>> FullSample = new(() => new());

    public static async Task DisposeAsync()
    {
        if (FullSample.IsValueCreated) await FullSample.Value.DisposeAsync();
    }
}

/// <summary>
/// The FullSample is what the README and the MCP setup guide hand people as the way to wire Bswup into a Blazor
/// Web App, so it is booted as it ships - its own Program.cs, App.razor and service-worker.js - rather than trusted
/// to keep matching the harness.
/// </summary>
[TestClass]
public class FullSampleHostingTests
{
    [TestMethod]
    public async Task The_document_loads_Blazor_and_Bswup_and_the_progress_UI()
    {
        using var client = SampleHostFactories.FullSample.Value.CreateClient();
        using var response = await client.GetAsync("/", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.Contains("<script src=\"_framework/blazor.web.js\" autostart=\"false\"></script>", html);
        Assert.Contains("_content/Bit.Bswup/bit-bswup.js", html);
        Assert.Contains("_content/Bit.Bswup/bit-bswup.progress.js", html);
        Assert.Contains("data-bit-bswup-config=\"true\"", html);
    }

    [TestMethod]
    public async Task The_service_worker_imports_Bswup()
    {
        using var client = SampleHostFactories.FullSample.Value.CreateClient();
        using var response = await client.GetAsync("/service-worker.js", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Every_asset_the_samples_worker_precaches_is_served_with_the_bytes_its_hash_describes()
    {
        using var client = SampleHostFactories.FullSample.Value.CreateClient();

        var manifest = await PrecacheManifest.LoadAsync(client, "/");
        // The sample's own assetsExclude (see its service-worker.js).
        var problems = await manifest.VerifyServedAsync(client, "/",
            @"^Bit\.Bswup\.FullSample\.Client\.styles\.css$", @"weather\.json$", @"^_framework/blazor\.webassembly\.js$");

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/Bit.Bswup.FullSample.Server.styles.css")]
    [DataRow("/Bit.Bswup.FullSample.Client.bundle.scp.css")]
    [DataRow("/_framework/blazor.web.js")]
    public async Task Every_external_asset_the_samples_worker_precaches_is_served(string url)
    {
        using var client = SampleHostFactories.FullSample.Value.CreateClient();
        using var response = await client.GetAsync(url, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{url} is listed in the sample's externalAssets but not served.");
    }

    public TestContext TestContext { get; set; }
}
#endif
