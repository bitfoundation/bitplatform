using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bit.Belemetry.Tests.E2E.Infrastructure;

/// <summary>
/// Reads a support bundle the way any tool would - as a ZIP of JSON documents described by its manifest - without the
/// library, so the suites check the format contract itself rather than the library agreeing with itself.
/// </summary>
internal static class SupportBundleFiles
{
    public static Dictionary<string, string> Read(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        return zip.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var reader = new StreamReader(e.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        });
    }

    /// <summary>The manifest's format and version, and every file it lists present with its size and SHA-256.</summary>
    public static void AssertValid(Dictionary<string, string> files)
    {
        Assert.IsTrue(files.ContainsKey("manifest.json"), "No manifest.json.");
        using var manifest = JsonDocument.Parse(files["manifest.json"]);
        var root = manifest.RootElement;
        Assert.AreEqual("bit-belemetry-support-bundle", root.GetProperty("format").GetString());
        Assert.AreEqual(1, root.GetProperty("schemaVersion").GetInt32());

        foreach (var file in root.GetProperty("files").EnumerateArray())
        {
            var name = file.GetProperty("name").GetString()!;
            Assert.IsTrue(files.TryGetValue(name, out var content), $"The manifest lists {name}, which the bundle does not hold.");
            var bytes = Encoding.UTF8.GetBytes(content);
            Assert.AreEqual(file.GetProperty("bytes").GetInt64(), bytes.Length, name);
            Assert.AreEqual(file.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(bytes)), name);
        }

        foreach (var required in new[] { "report.json", "report.md", "timeline.json" })
        {
            Assert.IsTrue(files.ContainsKey(required), $"No {required}.");
        }
    }
}
