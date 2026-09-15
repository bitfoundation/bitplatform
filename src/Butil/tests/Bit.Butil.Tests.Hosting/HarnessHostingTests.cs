using System.Net;
using ButilTests.Harness.Web;
using ButilTests.Hosting.Infrastructure;
using Microsoft.Extensions.Logging;
using Modes = ButilTests.Harness.Web.HarnessRenderModes;

namespace ButilTests.Hosting;

/// <summary>
/// What each render mode's server response has to contain for the browser suites to have anything to drive: the
/// harness pages prerendered (or deliberately not), the library's JavaScript served in the shape the host was
/// started with, and the documents the harness pages load by URL.
/// </summary>
[TestClass]
public class HarnessHostingTests
{
    [TestMethod]
    [DataRow(Modes.Ssr, "/e2e")]
    [DataRow(Modes.Server, "/e2e")]
    [DataRow(Modes.WebAssembly, "/e2e")]
    [DataRow(Modes.Auto, "/e2e")]
    [DataRow(Modes.Ssr, "/e2e-observers")]
    [DataRow(Modes.Server, "/e2e-observers")]
    [DataRow(Modes.WebAssembly, "/e2e-observers")]
    [DataRow(Modes.Auto, "/e2e-observers")]
    public async Task A_harness_page_is_rendered_into_the_first_response_but_not_yet_ready(string mode, string route)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync(route);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // "ready" is written by the first interactive render. A prerendered page that already said so would let a
        // browser test click a button whose handler is not attached yet, and the click would be lost.
        Assert.AreEqual("starting", RenderedHtml.TextOf(html, "status"), "The prerendered page claims to be ready.");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task A_prerendered_harness_page_reports_the_static_renderer(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/e2e");
        var html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual("dotnet", RenderedHtml.AttributeOf(html, "status", "data-platform"));
#if NET9_0_OR_GREATER
        Assert.AreEqual("Static", RenderedHtml.AttributeOf(html, "status", "data-renderer"));
#endif
    }

    [TestMethod]
    [DataRow(Modes.ServerNoPrerender)]
    [DataRow(Modes.WebAssemblyNoPrerender)]
    [DataRow(Modes.AutoNoPrerender)]
    public async Task A_mode_without_prerendering_sends_only_the_interactive_component_marker(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/e2e");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.IsNull(RenderedHtml.TextOf(html, "status"), "A non-prerendered mode rendered the harness page on the server.");
        StringAssert.Contains(html, "<!--Blazor:");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    [DataRow(Modes.ServerNoPrerender)]
    public async Task A_bundle_mode_host_references_the_bundle_and_says_so(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/e2e");
        var html = await response.Content.ReadAsStringAsync();

        StringAssert.Contains(html, "<script src=\"_content/Bit.Butil/bit-butil.js\"></script>");
        StringAssert.Contains(html, "data-scripts=\"bundle\"");
    }

    [TestMethod]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    public async Task A_lazy_mode_host_leaves_the_bundle_off_the_page_and_says_so(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode, HarnessScripts.Lazy).CreateClient().GetAsync("/e2e");
        var html = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(html.Contains("bit-butil.js", StringComparison.Ordinal), "The bundle is on a lazy-scripts page.");
        // The WebAssembly client reads this to switch itself to lazy loading.
        StringAssert.Contains(html, "data-scripts=\"lazy\"");
    }

    [TestMethod]
    [DataRow("/_content/Bit.Butil/bit-butil.js", "text/javascript")]
    [DataRow("/_content/Bit.Butil/modules/crypto.js", "text/javascript")]
    [DataRow("/_content/Bit.Butil/modules/utils.js", "text/javascript")]
    [DataRow("/_content/Bit.Butil.Samples.Core/app.css", "text/css")]
    [DataRow("/workers/e2e-worker.js", "text/javascript")]
    [DataRow("/workers/e2e-shared-worker.js", "text/javascript")]
    [DataRow("/frames/e2e-frame.html", "text/html")]
    [DataRow("/data/stream-sample.txt", "text/plain")]
    public async Task What_the_harness_pages_load_by_url_is_served_as_itself(string path, string mediaType)
    {
        using var response = await HarnessHostFactory.Shared(Modes.Auto).CreateClient().GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // A page route answering for an asset would still be a 200; the media type is what tells them apart.
        Assert.AreEqual(mediaType, response.Content.Headers.ContentType?.MediaType, $"{path} was answered by something other than the file.");
    }

    [TestMethod]
    public async Task The_stream_sample_is_exactly_the_size_the_streams_harness_asserts()
    {
        using var response = await HarnessHostFactory.Shared(Modes.Auto).CreateClient().GetAsync("/data/stream-sample.txt");

        Assert.HasCount(1024, await response.Content.ReadAsByteArrayAsync());
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task Prerendering_the_harness_pages_logs_no_errors(string mode)
    {
        await using var factory = new HarnessHostFactory(mode);
        using var client = factory.CreateClient();

        foreach (var route in new[] { "/e2e", "/e2e-observers" })
        {
            using var _ = await client.GetAsync(route);
        }

        var errors = factory.Logs.Entries.Where(e => e.Level >= LogLevel.Error).ToArray();
        Assert.AreEqual(0, errors.Length, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }
}
