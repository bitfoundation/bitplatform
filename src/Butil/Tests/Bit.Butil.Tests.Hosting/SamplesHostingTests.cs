#if NET10_0_OR_GREATER
using System.Net;
using Samples = ButilTests.Hosting.Infrastructure.SampleHostFactories;

namespace ButilTests.Hosting;

/// <summary>
/// The Blazor Web App hosting samples are what the README and the MCP setup guides hand to people as the way to wire
/// Butil into each render mode, so they are booted as they ship - their own Program.cs and App.razor - rather than
/// trusted to keep matching the harness host.
/// </summary>
[TestClass]
public class SamplesHostingTests
{
    [TestMethod]
    [DataRow(Samples.Server)]
    [DataRow(Samples.WebAssembly)]
    [DataRow(Samples.Auto)]
    public async Task The_home_page_is_prerendered(string sample)
    {
        using var client = Samples.CreateClient(sample);
        using var response = await client.GetAsync("/");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "<h1>Bit.Butil</h1>");
    }

    [TestMethod]
    [DataRow(Samples.Server)]
    [DataRow(Samples.WebAssembly)]
    [DataRow(Samples.Auto)]
    public async Task A_deep_link_to_a_sample_page_is_prerendered(string sample)
    {
        // The pages live in Samples.Core rather than in the host, so a host that forgot to map that assembly's
        // endpoints serves its home page through the router and 404s every other URL a visitor lands on directly.
        using var client = Samples.CreateClient(sample);
        using var response = await client.GetAsync("/clipboard");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "<h1>Clipboard</h1>");
    }

    [TestMethod]
    [DataRow(Samples.Server)]
    [DataRow(Samples.WebAssembly)]
    [DataRow(Samples.Auto)]
    public async Task The_bundle_is_on_the_page_before_the_blazor_script(string sample)
    {
        using var client = Samples.CreateClient(sample);
        var html = await client.GetStringAsync("/");

        // The app boots as soon as blazor.web.js runs, so window.BitButil has to exist by then.
        var bundle = html.IndexOf("<script src=\"_content/Bit.Butil/bit-butil.js\"></script>", StringComparison.Ordinal);
        var blazor = html.IndexOf("<script src=\"_framework/blazor.web.js\"></script>", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, bundle, "The bundle's script tag is not on the page.");
        Assert.IsGreaterThan(bundle, blazor, "The bundle is loaded after the Blazor script, or the Blazor script is missing.");
    }

    [TestMethod]
    [DataRow(Samples.Server, "/_content/Bit.Butil/bit-butil.js", "text/javascript")]
    [DataRow(Samples.WebAssembly, "/_content/Bit.Butil/bit-butil.js", "text/javascript")]
    [DataRow(Samples.Auto, "/_content/Bit.Butil/bit-butil.js", "text/javascript")]
    [DataRow(Samples.Server, "/_content/Bit.Butil.Samples.Core/app.css", "text/css")]
    [DataRow(Samples.WebAssembly, "/_content/Bit.Butil.Samples.Core/app.css", "text/css")]
    [DataRow(Samples.Auto, "/_content/Bit.Butil.Samples.Core/app.css", "text/css")]
    [DataRow(Samples.Server, "/workers/sample-worker.js", "text/javascript")]
    [DataRow(Samples.WebAssembly, "/workers/sample-worker.js", "text/javascript")]
    [DataRow(Samples.Auto, "/workers/sample-worker.js", "text/javascript")]
    [DataRow(Samples.Server, "/frames/sample-frame.html", "text/html")]
    [DataRow(Samples.WebAssembly, "/frames/sample-frame.html", "text/html")]
    [DataRow(Samples.Auto, "/frames/sample-frame.html", "text/html")]
    [DataRow(Samples.Server, "/data/stream-sample.txt", "text/plain")]
    [DataRow(Samples.WebAssembly, "/data/stream-sample.txt", "text/plain")]
    [DataRow(Samples.Auto, "/data/stream-sample.txt", "text/plain")]
    [DataRow(Samples.Server, "/sw.js", "text/javascript")]
    [DataRow(Samples.WebAssembly, "/sw.js", "text/javascript")]
    [DataRow(Samples.Auto, "/sw.js", "text/javascript")]
    public async Task What_the_sample_pages_load_by_url_is_served_as_itself(string sample, string path, string mediaType)
    {
        // The Workers, Messaging, Streams and Service Worker pages load these by URL; a host without them renders
        // those pages fine and then fails every call on them.
        using var client = Samples.CreateClient(sample);
        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // A page route answering for an asset would still be a 200; the media type is what tells them apart.
        Assert.AreEqual(mediaType, response.Content.Headers.ContentType?.MediaType, $"{path} was answered by something other than the file.");
    }
}
#endif
