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

    /// <summary>
    /// The names of the scripts of a package that the published script bundle holds, in bundle order: the one run of the
    /// pieces, in their order, that the bundle is made of after its prologue - so a piece whose text also stands inside
    /// another one is never taken for kept where it was left out. Fails when no run of them makes the bundle.
    /// </summary>
    public List<string> KeptScripts(string package)
    {
        var pieces = GetPieces(package);
        var bundle = ReadAsset(package, pieces.ScriptBundle!);
        Assert.IsTrue(bundle.StartsWith(pieces.ScriptPrologue, StringComparison.Ordinal), $"{package}'s published script does not start with the prologue of its pieces.");

        var kept = Decompose(bundle, pieces.ScriptPrologue.Length, bundle.Length, pieces.ScriptModules.Select(m => (m.Name, Piece(pieces, m.File))).ToList());
        Assert.IsNotNull(kept, $"{package}'s published script is not its prologue and a run of its pieces in the bundle's order.");
        return kept;
    }

    /// <summary>
    /// The names of the component stylesheets of a package that a published stylesheet holds, in cascade order: the run of
    /// the component pieces between the bundle's own two parts. Fails when no run of them makes the stylesheet.
    /// </summary>
    public List<string> KeptStyles(string package, string? bundle = null)
    {
        var pieces = GetPieces(package);
        var styleBundle = bundle is null ? pieces.StyleBundles[0] : pieces.StyleBundles.Single(b => b.Bundle == bundle);
        var css = ReadAsset(package, styleBundle.Bundle).TrimStart('﻿');
        var prefix = Piece(pieces, styleBundle.Prefix);
        var suffix = Piece(pieces, styleBundle.Suffix);
        Assert.IsTrue(css.Length >= prefix.Length + suffix.Length && css.StartsWith(prefix, StringComparison.Ordinal) && css.EndsWith(suffix, StringComparison.Ordinal),
                      $"{package}/{styleBundle.Bundle} does not start and end with its own parts.");

        var kept = Decompose(css, prefix.Length, css.Length - suffix.Length, pieces.StyleChunks.Select(c => (c.Name, Piece(pieces, c.File))).ToList());
        Assert.IsNotNull(kept, $"{package}/{styleBundle.Bundle} is not its own parts around a run of its component pieces in the bundle's order.");
        return kept;
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

    /// <summary>The names of the modules of a package's lazy scripts the publish holds (scripts/modules/&lt;name&gt;.js).</summary>
    public List<string> PublishedModules(string package)
    {
        var folder = GetAssetPath(package, "scripts/modules");
        return System.IO.Directory.Exists(folder)
            ? [.. System.IO.Directory.EnumerateFiles(folder, "*.js").Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(n => n, StringComparer.Ordinal)]
            : [];
    }

    /// <summary>Whether the publish holds a file of a package's static web assets.</summary>
    public bool HasAsset(string package, string relativePath) => File.Exists(GetAssetPath(package, relativePath));

    /// <summary>
    /// The JavaScript initializers a Blazor Web App's publish lists for Blazor to import as it starts (its modules manifest),
    /// by their routes - fingerprinted on the .NET 9 SDK and later.
    /// </summary>
    public List<string> ListedInitializers()
    {
        var manifest = System.IO.Directory.EnumerateFiles(WebRoot, "*.modules.json").SingleOrDefault();
        if (manifest is null) return [];
        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        return [.. document.RootElement.EnumerateArray().Select(e => e.GetString()!)];
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

    // The pieces, in their order, whose texts written one after another are exactly text[start..end] - a search over which
    // ones are left out, since a piece can begin with what another one does. An empty piece adds nothing and is never
    // counted as kept; null when no choice of them makes the text.
    private static List<string>? Decompose(string text, int start, int end, List<(string Name, string Text)> pieces)
    {
        var failed = new HashSet<(int Piece, int Position)>();
        var kept = new List<string>();

        bool Match(int index, int position)
        {
            if (position == end) return true;
            if (index == pieces.Count || failed.Contains((index, position))) return false;

            var piece = pieces[index].Text;
            if (piece.Length > 0 && position + piece.Length <= end && string.CompareOrdinal(text, position, piece, 0, piece.Length) == 0)
            {
                kept.Add(pieces[index].Name);
                if (Match(index + 1, position + piece.Length)) return true;
                kept.RemoveAt(kept.Count - 1);
            }

            if (Match(index + 1, position)) return true;

            failed.Add((index, position));
            return false;
        }

        return Match(0, start) ? kept : null;
    }

    private static string Tail(string text) => text.Length <= 6000 ? text : "..." + text[^6000..];
}
