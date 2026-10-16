using System.Text;
using System.Text.Json;
using Bit.BlazorUI.Build;

namespace Bit.BlazorUI.Tests.Trimming.Infrastructure;

/// <summary>What a publish produced: its folder, how it ended and what it said.</summary>
public sealed class PublishResult(string directory, int exitCode, string output)
{
    private static readonly Dictionary<string, PackageAssets> Assets = [];

    public string Directory { get; } = directory;

    public int ExitCode { get; } = exitCode;

    public string Output { get; } = output;

    public void AssertSucceeded()
    {
        Assert.AreEqual(0, ExitCode, $"The publish failed:\n{Tail(Output)}");
    }

    /// <summary>The wwwroot of the publish (a WebAssembly app's lives under wwwroot as well).</summary>
    public string WebRoot => Path.Combine(Directory, "wwwroot");

    /// <summary>A static web asset of a package, as the publish holds it.</summary>
    public string GetAssetPath(string package, string relativePath) => Path.Combine(WebRoot, "_content", package, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public string ReadAsset(string package, string relativePath)
    {
        var path = GetAssetPath(package, relativePath);
        Assert.IsTrue(File.Exists(path), $"The publish has no {package}/{relativePath}.");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    /// <summary>The pieces a package's build wrote - the ones the trimming put the publish's bundles together from.</summary>
    public static PackageAssets GetPieces(string package)
    {
        lock (Assets)
        {
            if (Assets.TryGetValue(package, out var assets)) return assets;

            var folder = Path.Combine(Publisher.Root, package, "obj", "bit-assets");
            Assert.IsTrue(System.IO.Directory.Exists(folder), $"Missing {folder}: the publish should have built {package}.");
            return Assets[package] = PackageAssets.Load(folder);
        }
    }

    /// <summary>The names of the scripts of a package that the published script bundle holds, in bundle order.</summary>
    public List<string> KeptScripts(string package)
    {
        var pieces = GetPieces(package);
        var bundle = ReadAsset(package, pieces.ScriptBundle!);
        return pieces.ScriptModules.Where(m => Piece(pieces, m.File) is var text && text.Trim().Length > 0 && bundle.Contains(text, StringComparison.Ordinal))
                                   .Select(m => m.Name)
                                   .ToList();
    }

    /// <summary>The names of the component stylesheets of a package that a published stylesheet holds, in cascade order.</summary>
    public List<string> KeptStyles(string package, string? bundle = null)
    {
        var pieces = GetPieces(package);
        var css = ReadAsset(package, bundle ?? pieces.StyleBundles[0].Bundle);
        return pieces.StyleChunks.Where(c => Piece(pieces, c.File) is var text && text.Trim().Length > 0 && css.Contains(text, StringComparison.Ordinal))
                                 .Select(c => c.Name)
                                 .ToList();
    }

    /// <summary>The published script bundle of a package, against the bundle the pieces it holds make, in their order.</summary>
    public void AssertScriptsAreThePiecesInOrder(string package)
    {
        var pieces = GetPieces(package);
        var kept = KeptScripts(package);
        var expected = pieces.ScriptPrologue + string.Concat(pieces.ScriptModules.Where(m => kept.Contains(m.Name)).Select(m => Piece(pieces, m.File)));
        Assert.AreEqual(expected, ReadAsset(package, pieces.ScriptBundle!), $"{package}'s published script is not the prologue and the pieces it keeps, in the bundle's order.");
    }

    /// <summary>Every published stylesheet of a package, against its own part, the component pieces it holds and its own part again.</summary>
    public void AssertStylesAreThePiecesInOrder(string package)
    {
        var pieces = GetPieces(package);
        foreach (var bundle in pieces.StyleBundles)
        {
            var kept = KeptStyles(package, bundle.Bundle);
            var css = Piece(pieces, bundle.Prefix) + string.Concat(pieces.StyleChunks.Where(c => kept.Contains(c.Name)).Select(c => Piece(pieces, c.File))) + Piece(pieces, bundle.Suffix);
            if (css.Any(c => c > 0x7F)) css = "\uFEFF" + css;
            Assert.AreEqual(css, ReadAsset(package, bundle.Bundle).Insert(0, File.ReadAllBytes(GetAssetPath(package, bundle.Bundle)) is [0xEF, 0xBB, 0xBF, ..] ? "\uFEFF" : ""),
                $"{package}/{bundle.Bundle} is not its own part, the component pieces it keeps and its own part again.");
        }
    }

    /// <summary>The endpoints of the publish, from the manifest a Blazor Web App's server serves its static web assets by.</summary>
    public List<JsonElement> ReadEndpoints()
    {
        var manifest = System.IO.Directory.EnumerateFiles(Directory, "*.staticwebassets.endpoints.json").SingleOrDefault();
        Assert.IsNotNull(manifest, "The publish has no static web assets endpoints manifest.");
        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        return document.RootElement.GetProperty("Endpoints").EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static string Piece(PackageAssets pieces, string relativePath) => File.ReadAllText(pieces.Resolve(relativePath), Encoding.UTF8);

    private static string Tail(string text) => text.Length <= 6000 ? text : "..." + text[^6000..];
}
