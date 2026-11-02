#if NET11_0_OR_GREATER
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
    private static readonly string[] AllSamples = [Samples.Server, Samples.WebAssembly, Samples.Auto];

    /// <summary>
    /// What the sample pages load by URL rather than through Blazor: the library and the stylesheet, and the documents
    /// the Workers, Messaging, Streams, Service Worker and E2E pages hand to the browser.
    /// </summary>
    private static readonly (string Path, string MediaType)[] UrlLoadedFiles =
    [
        ("/_content/Bit.Butil/bit-butil.js", "text/javascript"),
        ("/_content/Bit.Butil.Samples.Core/app.css", "text/css"),
        ("/workers/sample-worker.js", "text/javascript"),
        ("/workers/e2e-worker.js", "text/javascript"),
        ("/workers/e2e-shared-worker.js", "text/javascript"),
        ("/frames/sample-frame.html", "text/html"),
        ("/frames/e2e-frame.html", "text/html"),
        ("/data/stream-sample.txt", "text/plain"),
        ("/sw.js", "text/javascript"),
    ];

    public static IEnumerable<object[]> UrlLoadedFilesOfEverySample
        => AllSamples.SelectMany(sample => UrlLoadedFiles.Select(file => new object[] { sample, file.Path, file.MediaType }));

    [TestMethod]
    [DataRow(Samples.Server)]
    [DataRow(Samples.WebAssembly)]
    [DataRow(Samples.Auto)]
    public async Task The_home_page_is_prerendered(string sample)
    {
        // Index.razor lives in Samples.Core like every other sample page, not in the host, so it is served only because
        // the host maps that assembly's @page endpoints - a host that forgot AddAdditionalAssemblies 404s right here.
        using var client = Samples.CreateClient(sample);
        using var response = await client.GetAsync("/");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "<h1>Bit.Butil</h1>");
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
    [DynamicData(nameof(UrlLoadedFilesOfEverySample))]
    public async Task What_the_sample_pages_load_by_url_is_served_as_itself(string sample, string path, string mediaType)
    {
        // A host without these renders the pages that load them fine and then fails every call on them. The shared nav
        // menu links every page from every host - E2E included - so every host serves every one of them.
        using var client = Samples.CreateClient(sample);
        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // A page route answering for an asset would still be a 200; the media type is what tells them apart.
        Assert.AreEqual(mediaType, response.Content.Headers.ContentType?.MediaType, $"{path} was answered by something other than the file.");
    }

    [TestMethod]
    [DataRow(Samples.Server)]
    [DataRow(Samples.WebAssembly)]
    [DataRow(Samples.Auto)]
    public async Task The_stream_sample_is_exactly_the_size_the_e2e_page_asserts(string sample)
    {
        // The E2E page's Streams section reads this file to its end and asserts exactly 1024 bytes. A copy saved with a
        // trailing newline, or with its line endings normalized, is still served as text/plain - the test above cannot
        // tell.
        using var client = Samples.CreateClient(sample);
        using var response = await client.GetAsync("/data/stream-sample.txt");

        Assert.HasCount(1024, await response.Content.ReadAsByteArrayAsync());
    }
}
#endif
