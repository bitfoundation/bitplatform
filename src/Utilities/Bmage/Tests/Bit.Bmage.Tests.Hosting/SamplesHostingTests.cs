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

        StringAssert.Contains(html, "<h1>Bmage samples</h1>");
        StringAssert.Contains(html, "data-renderer=\"Static\"");
        StringAssert.Contains(html, "<dd id=\"environment\">Prerendering</dd>");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_managed_engine_runs_on_the_server_while_prerendering(string sample)
    {
        var html = await GetPageAsync(sample, "/engine");

        StringAssert.Contains(html, "src=\"data:image/png;base64,iVBORw0KGgo");
        StringAssert.Contains(html, "Original: 360 × 240 PNG");
        StringAssert.Contains(html, "encoded on <strong>the server</strong>");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_capability_probe_falls_back_to_the_managed_codecs_while_prerendering(string sample)
    {
        var html = await GetPageAsync(sample, "/capabilities");

        StringAssert.Contains(html, "<dd id=\"capabilities-environment\">Prerendering</dd>");
        // AVIF and WebP need a browser; JPEG is the managed fallback.
        StringAssert.Contains(html, "<dd id=\"capabilities-choice\">Jpeg</dd>");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_process_and_editor_pages_are_prerendered(string sample)
    {
        StringAssert.Contains(await GetPageAsync(sample, "/process"), "class=\"bmg-dropzone");

        var editor = await GetPageAsync(sample, "/editor");
        StringAssert.Contains(editor, "<h1>Editor</h1>");
        StringAssert.Contains(editor, "bmg-prerender");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task An_unknown_url_is_answered_with_the_not_found_page_and_a_404(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/definitely/not/here");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "<h1>Not found</h1>");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_error_page_renders_statically(string sample)
    {
        var html = await GetPageAsync(sample, "/Error");

        StringAssert.Contains(html, "<h1>Error</h1>");
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [TestMethod]
    [DataRow("server", "_content/Bit.Bmage/bit-bmage.js", "text/javascript")]
    [DataRow("server", "_content/Bit.Bmage/bit-bmage-worker.js", "text/javascript")]
    [DataRow("server", "_content/Bit.Bmage/bit-bmage-editor.css", "text/css")]
    [DataRow("server", "_content/Bit.Bmage.Samples.Core/app.css", "text/css")]
    [DataRow("wasm", "_content/Bit.Bmage/bit-bmage.js", "text/javascript")]
    [DataRow("wasm", "_content/Bit.Bmage/bit-bmage-worker.js", "text/javascript")]
    [DataRow("wasm", "_content/Bit.Bmage/bit-bmage-editor.css", "text/css")]
    [DataRow("wasm", "_content/Bit.Bmage.Samples.Core/app.css", "text/css")]
    [DataRow("auto", "_content/Bit.Bmage/bit-bmage.js", "text/javascript")]
    [DataRow("auto", "_content/Bit.Bmage/bit-bmage-worker.js", "text/javascript")]
    [DataRow("auto", "_content/Bit.Bmage/bit-bmage-editor.css", "text/css")]
    [DataRow("auto", "_content/Bit.Bmage.Samples.Core/app.css", "text/css")]
    public async Task The_library_and_sample_assets_are_served(string sample, string path, string mediaType)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(mediaType, response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<string> GetPageAsync(string sample, string path)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
        return await response.Content.ReadAsStringAsync();
    }
}
