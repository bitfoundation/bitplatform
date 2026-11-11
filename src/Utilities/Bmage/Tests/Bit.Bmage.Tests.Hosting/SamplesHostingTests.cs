using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace Bit.Bmage.Tests.Hosting;

internal static class SampleHostFactories
{
    public static readonly Lazy<WebApplicationFactory<Bit.Bmage.Samples.Server.Components.App>> Server = new(() => new());
    public static readonly Lazy<WebApplicationFactory<Bit.Bmage.Samples.Wasm.Components.App>> WebAssembly = new(() => new());
    public static readonly Lazy<WebApplicationFactory<Bit.Bmage.Samples.Auto.Components.App>> Auto = new(() => new());

    // Redirects are not followed, so a re-executed miss or error is seen as the host answered it.
    private static readonly WebApplicationFactoryClientOptions Options = new() { AllowAutoRedirect = false };

    public static HttpClient CreateClient(string sample) => sample switch
    {
        "server" => Server.Value.CreateClient(Options),
        "wasm" => WebAssembly.Value.CreateClient(Options),
        "auto" => Auto.Value.CreateClient(Options),
        _ => throw new ArgumentOutOfRangeException(nameof(sample), sample, null)
    };

    public static async Task DisposeAsync()
    {
        if (Server.IsValueCreated) await Server.Value.DisposeAsync();
        if (WebAssembly.IsValueCreated) await WebAssembly.Value.DisposeAsync();
        if (Auto.IsValueCreated) await Auto.Value.DisposeAsync();
    }
}

/// <summary>
/// The hosting samples are how Bmage is wired into each render mode, so they are booted as they ship rather than
/// trusted to keep working: every page prerenders, the managed engine runs on the server while it does, and every
/// asset the library and the editor need is served.
/// </summary>
[TestClass]
public class SamplesHostingTests
{
    [AssemblyCleanup]
    public static Task CleanupAsync() => SampleHostFactories.DisposeAsync();

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_home_page_is_prerendered_and_reports_the_prerendering_environment(string sample)
    {
        var html = await GetPageAsync(sample, "/");

        Assert.Contains("<h1>Bmage samples</h1>", html);
        Assert.Contains("data-renderer=\"Static\"", html);
        Assert.Contains("<dd id=\"environment\">Prerendering</dd>", html);
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_managed_engine_runs_on_the_server_while_prerendering(string sample)
    {
        var html = await GetPageAsync(sample, "/engine");

        Assert.Contains("src=\"data:image/png;base64,iVBORw0KGgo", html);
        Assert.Contains("Original: 360 × 240 PNG", html);
        Assert.Contains("encoded on <strong>the server</strong>", html);
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_capability_probe_falls_back_to_the_managed_codecs_while_prerendering(string sample)
    {
        var html = await GetPageAsync(sample, "/capabilities");

        Assert.Contains("<dd id=\"capabilities-environment\">Prerendering</dd>", html);
        // AVIF needs a browser; WebP is the first choice the managed codecs can write.
        Assert.Contains("<dd id=\"capabilities-choice\">WebP</dd>", html);
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_process_and_editor_pages_are_prerendered(string sample)
    {
        Assert.Contains("class=\"bmg-dropzone", await GetPageAsync(sample, "/process"));

        var editor = await GetPageAsync(sample, "/editor");
        Assert.Contains("<h1>Editor</h1>", editor);
        Assert.Contains("bmg-prerender", editor);
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task An_unknown_url_is_answered_with_the_not_found_page_and_a_404(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/definitely/not/here", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("<h1>Not found</h1>", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_error_page_renders_statically(string sample)
    {
        var html = await GetPageAsync(sample, "/Error");

        Assert.Contains("<h1>Error</h1>", html);
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [TestMethod]
    [DataRow("server", "_content/Bit.Bmage.Blazor/bit-bmage.js", "text/javascript")]
    [DataRow("server", "_content/Bit.Bmage.Blazor/bit-bmage-worker.js", "text/javascript")]
    [DataRow("server", "_content/Bit.Bmage.Blazor/bit-bmage-editor.css", "text/css")]
    [DataRow("server", "_content/Bit.Bmage.Samples.Core/app.css", "text/css")]
    [DataRow("wasm", "_content/Bit.Bmage.Blazor/bit-bmage.js", "text/javascript")]
    [DataRow("wasm", "_content/Bit.Bmage.Blazor/bit-bmage-worker.js", "text/javascript")]
    [DataRow("wasm", "_content/Bit.Bmage.Blazor/bit-bmage-editor.css", "text/css")]
    [DataRow("wasm", "_content/Bit.Bmage.Samples.Core/app.css", "text/css")]
    [DataRow("auto", "_content/Bit.Bmage.Blazor/bit-bmage.js", "text/javascript")]
    [DataRow("auto", "_content/Bit.Bmage.Blazor/bit-bmage-worker.js", "text/javascript")]
    [DataRow("auto", "_content/Bit.Bmage.Blazor/bit-bmage-editor.css", "text/css")]
    [DataRow("auto", "_content/Bit.Bmage.Samples.Core/app.css", "text/css")]
    public async Task The_library_and_sample_assets_are_served(string sample, string path, string mediaType)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync(path, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var expectedValue = mediaType;
        var actualValue = response.Content.Headers.ContentType?.MediaType;
        Assert.AreEqual(expectedValue, actualValue);
    }

    private static async Task<string> GetPageAsync(string sample, string path)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
        return await response.Content.ReadAsStringAsync();
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
