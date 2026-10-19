using System.Globalization;
using System.IO.Compression;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The trimming of a Blazor Web App whose WebAssembly client has a service worker: Bit.BlazorUI.Tests.Trimming.Hosted. The
/// client writes service-worker-assets.js during its own publish, from the packages' full bundles and every vendored file,
/// and the server trims afterwards - so what a service worker is told to install is right only where the server's publish
/// rewrote the list to match what it serves (#13907). And the same client published on its own, a standalone PWA, whose
/// manifest the SDK writes after the trimming, with nothing to rewrite.
/// </summary>
/// <remarks>
/// Whether a publish rewrote the manifest is read off the task's own line in the build output, and asserted either way.
/// Positively, so that a fixture that stopped producing the mismatch - a client trimming like its server - fails rather
/// than pass over a manifest nobody had to touch; negatively, so that a manifest with nothing wrong in it is not rewritten
/// into a new version that makes every installed app download its precache again.
/// </remarks>
[TestClass]
public sealed class HostedAppTrimmingTests
{
    private const string ScriptBundle = "_content/Bit.BlazorUI/scripts/bit.blazorui.js";
    private const string StyleBundle = "_content/Bit.BlazorUI/styles/bit.blazorui.css";
    private const string Leaflet = "_content/Bit.BlazorUI.Extras/leaflet/";
    private const string Modules = "_content/Bit.BlazorUI/scripts/modules/";

    private static PublishResult Bundles => Publisher.Publish(Publisher.Hosted, "BitBlazorUITrimAssets=true");

    private static PublishResult Lazy => Publisher.Publish(Publisher.Hosted, "BitBlazorUITrimAssets=true", "BitBlazorUILazyScripts=true");

    private static PublishResult Standalone => Publisher.Publish(Publisher.HostedClient);

    [TestMethod]
    public void TheServiceWorkerShouldListTheBundlesTheServerTrimmedAndNotTheFilesItLeftOut()
    {
        var publish = Bundles;
        publish.AssertSucceeded();

        // The client listed the full bundles and the Extras' vendored files; the server serves trimmed bundles and none of
        // those files - so both halves of the rewrite have something to do.
        var rewrite = publish.ServiceWorkerRewrite;
        Assert.IsTrue(rewrite is { Dropped: > 0, Rehashed: > 0 }, $"The manifest should have lost entries and taken trimmed hashes, but {Describe(rewrite)}");

        publish.AssertServiceWorkerListsWhatIsServed();

        var (_, assets) = publish.ReadServiceWorkerManifest();
        Assert.IsTrue(assets.ContainsKey(ScriptBundle), "The manifest no longer lists the trimmed script bundle.");
        Assert.IsTrue(assets.ContainsKey(StyleBundle), "The manifest no longer lists the trimmed style bundle.");
        Assert.IsFalse(assets.Keys.Any(url => url.StartsWith(Leaflet, StringComparison.Ordinal)), "The manifest lists Leaflet, which the app does not use.");
    }

    [TestMethod]
    public void ALazyScriptsServiceWorkerShouldListExactlyTheModulesTheServerPublishes()
    {
        var publish = Lazy;
        publish.AssertSucceeded();

        // The client listed every module of the lazy scripts; the server publishes the ones the app can import.
        var rewrite = publish.ServiceWorkerRewrite;
        Assert.IsTrue(rewrite is { Dropped: > 0 }, $"The manifest should have lost the modules the server left out, but {Describe(rewrite)}");

        publish.AssertServiceWorkerListsWhatIsServed();

        var (_, assets) = publish.ReadServiceWorkerManifest();
        Assert.IsFalse(assets.ContainsKey(ScriptBundle), "A lazy app's manifest lists the script bundle.");

        var listed = assets.Keys.Where(url => url.StartsWith(Modules, StringComparison.Ordinal) && url.EndsWith(".js", StringComparison.Ordinal))
                           .Select(url => url[Modules.Length..^".js".Length]).Order(StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(publish.PublishedModules("Bit.BlazorUI"), listed, $"The manifest lists the modules {string.Join(", ", listed)}.");
    }

    [TestMethod]
    public void AStandalonePwaShouldListItsOwnTrimmedFilesWithNothingToRewrite()
    {
        var publish = Standalone;
        publish.AssertSucceeded();

        // The SDK writes this manifest after the trimming, from the trimmed files: here so that an SDK moving the manifest
        // ahead of the trimming - or a change moving the trimming after it - shows up as a manifest that is wrong again.
        Assert.IsNull(publish.ServiceWorkerRewrite, $"A standalone app's manifest was rewritten, where the SDK wrote it from the trimmed files: {Describe(publish.ServiceWorkerRewrite)}");

        publish.AssertServiceWorkerListsWhatIsServed();

        var (_, assets) = publish.ReadServiceWorkerManifest();
        Assert.IsTrue(assets.ContainsKey(ScriptBundle), "The manifest does not list the trimmed script bundle.");
        Assert.IsFalse(assets.Keys.Any(url => url.StartsWith(Leaflet, StringComparison.Ordinal)), "The manifest lists Leaflet, which the app does not use.");
    }

    [TestMethod]
    [DataRow("bundles")]
    [DataRow("lazy")]
    [DataRow("standalone")]
    public void TheCompressedCopiesShouldBeTheFilesTheyAreCopiesOf(string scenario)
    {
        var publish = Of(scenario);
        publish.AssertSucceeded();

        // The server picks a copy by the request's Accept-Encoding, so a copy left over from a file the publish replaced is
        // the old file served to every browser. The service worker script's own copies only where the publish rewrote it:
        // the ones the SDK publishes of an untouched one are not this feature's to check.
        var (_, assets) = publish.ReadServiceWorkerManifest();
        var files = new List<string> { "service-worker-assets.js" };
        if (publish.ServiceWorkerRewrite is not null) files.Add("service-worker.js");
        files.AddRange(assets.Keys);

        var wrong = new List<string>();
        foreach (var file in files.Select(f => Path.Combine(publish.WebRoot, f.Replace('/', Path.DirectorySeparatorChar))).Where(File.Exists))
        {
            foreach (var (extension, decompress) in new (string, Func<Stream, Stream>)[]
            {
                (".gz", stream => new GZipStream(stream, CompressionMode.Decompress)),
                (".br", stream => new BrotliStream(stream, CompressionMode.Decompress)),
            })
            {
                var copy = file + extension;
                if (File.Exists(copy) is false) continue;

                using var content = new MemoryStream();
                using (var stream = decompress(File.OpenRead(copy))) stream.CopyTo(content);

                if (content.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(file)) is false) wrong.Add($"{Path.GetRelativePath(publish.WebRoot, copy).Replace('\\', '/')} is not {Path.GetFileName(file)} compressed");
            }
        }

        Assert.AreEqual(0, wrong.Count, Summarize(wrong));
    }

    /// <summary>
    /// Every endpoint the server answers with names a file that was published, and describes that file: its length, its
    /// ETag, its integrity. MapStaticAssets answers straight off the endpoints manifest - those headers are written into it
    /// at publish time, not read off the file - so a file swapped without its endpoints is a response with another file's
    /// Content-Length (a truncated or padded download) or integrity (a script the browser refuses to run).
    /// </summary>
    /// <remarks>
    /// A compressed response describes two files, and each value is checked against the right one: Content-Length and the
    /// strong ETag are the compressed file's, while the integrity and a weak ETag are those of the file it decompresses to.
    /// </remarks>
    [TestMethod]
    [DataRow("bundles")]
    [DataRow("lazy")]
    public void EveryEndpointShouldDescribeTheFileItServes(string scenario)
    {
        var publish = Of(scenario);
        publish.AssertSucceeded();

        var wrong = new List<string>();
        foreach (var endpoint in publish.ReadEndpoints())
        {
            var route = endpoint.GetProperty("Route").GetString();
            var assetFile = endpoint.GetProperty("AssetFile").GetString()!;
            var path = Path.Combine(publish.WebRoot, assetFile.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(path) is false)
            {
                wrong.Add($"{route} is served from {assetFile}, which was not published");
                continue;
            }

            var encoded = endpoint.TryGetProperty("Selectors", out var selectors)
                && selectors.EnumerateArray().Any(selector => selector.GetProperty("Name").GetString() == "Content-Encoding");
            var identity = encoded ? path[..^Path.GetExtension(path).Length] : path;

            if (File.Exists(identity) is false)
            {
                wrong.Add($"{route} serves {assetFile} compressed, and the file it decompresses to was not published");
                continue;
            }

            foreach (var header in endpoint.GetProperty("ResponseHeaders").EnumerateArray())
            {
                var name = header.GetProperty("Name").GetString();
                var value = header.GetProperty("Value").GetString()!;

                if (name == "Content-Length" && value != new FileInfo(path).Length.ToString(CultureInfo.InvariantCulture))
                {
                    wrong.Add($"{route} ({assetFile}) answers with Content-Length {value} for a {new FileInfo(path).Length:N0}-byte file");
                }
                else if (name == "ETag")
                {
                    var weak = value.StartsWith("W/", StringComparison.Ordinal);
                    if ((weak ? value[2..] : value) != $"\"{PublishResult.Sha256(weak ? identity : path)}\"") wrong.Add($"{route} ({assetFile}) answers with another file's {(weak ? "weak " : "")}ETag");
                }
            }

            if (endpoint.TryGetProperty("EndpointProperties", out var properties))
            {
                foreach (var property in properties.EnumerateArray())
                {
                    if (property.GetProperty("Name").GetString() == "integrity" && property.GetProperty("Value").GetString() != $"sha256-{PublishResult.Sha256(identity)}")
                    {
                        wrong.Add($"{route} ({assetFile}) carries another file's integrity");
                    }
                }
            }
        }

        Assert.AreEqual(0, wrong.Count, Summarize(wrong));
    }

    private static PublishResult Of(string scenario) => scenario switch
    {
        "bundles" => Bundles,
        "lazy" => Lazy,
        "standalone" => Standalone,
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
    };

    private static string Describe((string Line, int Dropped, int Rehashed)? rewrite) => rewrite is { } r
        ? $"the rewrite was: {r.Line}"
        : "the manifest was never rewritten - either the rewrite did not run, or the client now lists what the server serves and the fixture no longer tests a mismatch";

    /// <summary>The first few of a list of problems, which is enough to act on, and how many there were.</summary>
    private static string Summarize(List<string> wrong) => string.Join("; ", wrong.Take(5)) + (wrong.Count > 5 ? $"; and {wrong.Count - 5} more" : "");
}
