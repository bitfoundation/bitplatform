using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Bit.BlazorUI.Tests.Trimming.Infrastructure;

namespace Bit.BlazorUI.Tests.Trimming;

[TestClass]
public sealed class HostedAppTrimmingTests
{
    private static PublishResult Trimmed => Publisher.Publish(Publisher.Hosted, "BitBlazorUITrimAssets=true");

    [TestMethod]
    public void TheServiceWorkerManifestShouldListOnlyWhatThePublishServesWithItsHash()
    {
        var publish = Trimmed;
        publish.AssertSucceeded();

        var (_, assets) = ReadManifest(publish);
        var wrong = new List<string>();
        foreach (var (url, hash) in assets)
        {
            var path = Path.Combine(publish.WebRoot, url.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path) is false) wrong.Add($"{url} is listed but not published");
            else if (Hash(File.ReadAllBytes(path)) != hash) wrong.Add($"{url} is listed as {hash} but published as {Hash(File.ReadAllBytes(path))}");
        }

        Assert.IsEmpty(wrong, string.Join(Environment.NewLine, wrong));
        Assert.IsTrue(assets.ContainsKey("_content/Bit.BlazorUI/scripts/bit.blazorui.js"), "The manifest no longer lists the trimmed script bundle.");
        Assert.IsFalse(assets.Keys.Any(url => url.StartsWith("_content/Bit.BlazorUI.Extras/leaflet/", StringComparison.Ordinal)), "The manifest lists Leaflet, which the app does not use.");
    }

    [TestMethod]
    public void TheServiceWorkerShouldCarryTheRewrittenManifestsVersion()
    {
        var publish = Trimmed;
        publish.AssertSucceeded();

        var (version, _) = ReadManifest(publish);
        var script = File.ReadAllText(Path.Combine(publish.WebRoot, "service-worker.js"));

        StringAssert.Contains(script, $"Manifest version: {version}");
    }

    [TestMethod]
    public void TheCompressedCopiesShouldMatchTheRewrittenFiles()
    {
        var publish = Trimmed;
        publish.AssertSucceeded();

        foreach (var name in new[] { "service-worker-assets.js", "service-worker.js" })
        {
            var plain = File.ReadAllBytes(Path.Combine(publish.WebRoot, name));

            using (var gzip = new GZipStream(File.OpenRead(Path.Combine(publish.WebRoot, name + ".gz")), CompressionMode.Decompress))
            {
                CollectionAssert.AreEqual(plain, ReadAll(gzip), $"{name}.gz is not the published {name}.");
            }

            using (var brotli = new BrotliStream(File.OpenRead(Path.Combine(publish.WebRoot, name + ".br")), CompressionMode.Decompress))
            {
                CollectionAssert.AreEqual(plain, ReadAll(brotli), $"{name}.br is not the published {name}.");
            }
        }
    }

    private static (string Version, Dictionary<string, string> Assets) ReadManifest(PublishResult publish)
    {
        var text = File.ReadAllText(Path.Combine(publish.WebRoot, "service-worker-assets.js"));
        using var json = JsonDocument.Parse(text[text.IndexOf('{')..(text.LastIndexOf('}') + 1)]);

        var assets = json.RootElement.GetProperty("assets").EnumerateArray()
            .ToDictionary(a => a.GetProperty("url").GetString()!, a => a.GetProperty("hash").GetString()!, StringComparer.Ordinal);

        return (json.RootElement.GetProperty("version").GetString()!, assets);
    }

    private static string Hash(byte[] content) => "sha256-" + Convert.ToBase64String(SHA256.HashData(content));

    private static byte[] ReadAll(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
