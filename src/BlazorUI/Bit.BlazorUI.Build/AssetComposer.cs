using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bit.BlazorUI.Build;

/// <summary>Writes the trimmed bundles: each one the pieces of the full bundle that were kept, in the full bundle's order.</summary>
public static class AssetComposer
{
    /// <summary>One bundle written in place of a package's own.</summary>
    public sealed class Output(string packageId, string bundle, string path, long fullLength, long trimmedLength, int keptPieces, int allPieces)
    {
        public string PackageId { get; } = packageId;

        /// <summary>The bundle's path under the package's wwwroot.</summary>
        public string Bundle { get; } = bundle;

        /// <summary>Where the trimmed copy was written.</summary>
        public string Path { get; } = path;

        public long FullLength { get; } = fullLength;

        public long TrimmedLength { get; } = trimmedLength;

        public int KeptPieces { get; } = keptPieces;

        public int AllPieces { get; } = allPieces;
    }

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static List<Output> ComposeScripts(PackageAssets package, AssetSelection selection, string outputRoot)
    {
        var outputs = new List<Output>();
        if (package.ScriptBundle is null || package.ScriptModules.Count == 0) return outputs;

        var kept = package.ScriptModules.Where(selection.Scripts.Contains).ToList();

        var text = new StringBuilder(package.ScriptPrologue);
        foreach (var module in kept) text.Append(ReadPiece(package, module.File));

        var full = package.ScriptPrologue.Length + package.ScriptModules.Sum(m => (long)ReadPiece(package, m.File).Length);
        outputs.Add(Write(package, package.ScriptBundle, text.ToString(), outputRoot, full, kept.Count, package.ScriptModules.Count));
        return outputs;
    }

    public static List<Output> ComposeStyles(PackageAssets package, AssetSelection selection, string outputRoot)
    {
        var outputs = new List<Output>();
        if (package.StyleBundles.Count == 0) return outputs;

        var kept = package.StyleChunks.Where(selection.Styles.Contains).ToList();
        var chunks = string.Concat(kept.Select(c => ReadPiece(package, c.File)));
        var allChunks = package.StyleChunks.Sum(c => (long)ReadPiece(package, c.File).Length);

        foreach (var bundle in package.StyleBundles)
        {
            var prefix = ReadPiece(package, bundle.Prefix);
            var suffix = ReadPiece(package, bundle.Suffix);
            var css = WithCharset(prefix + chunks + suffix);
            outputs.Add(Write(package, bundle.Bundle, css, outputRoot, prefix.Length + allChunks + suffix.Length, kept.Count, package.StyleChunks.Count));
        }

        return outputs;
    }

    public static List<Output> ComposeIcons(PackageAssets package, AssetSelection selection, string outputRoot)
    {
        var outputs = new List<Output>();

        foreach (var set in package.IconSets)
        {
            var glyphs = ReadGlyphs(package, set, out var prefix, out var suffix);
            var kept = glyphs.Where(g => selection.Icons.Contains(g.Key)).ToList();
            var css = WithCharset(prefix + string.Concat(kept.Select(g => g.Value)) + suffix);
            var full = prefix.Length + glyphs.Sum(g => (long)g.Value.Length) + suffix.Length;
            outputs.Add(Write(package, set.Bundle, css, outputRoot, full, kept.Count, glyphs.Count));
        }

        return outputs;
    }

    /// <summary>The glyphs of an icon set, in stylesheet order, with the parts around them.</summary>
    public static List<KeyValuePair<string, string>> ReadGlyphs(PackageAssets package, IconSet set, out string prefix, out string suffix)
    {
        var path = package.Resolve(set.File);
        var obj = Json.AsObject(Json.Parse(File.ReadAllText(path, Encoding.UTF8)), path);
        prefix = Json.GetString(obj, "prefix", path);
        suffix = Json.GetString(obj, "suffix", path);

        var glyphs = new List<KeyValuePair<string, string>>();
        foreach (var entry in Json.AsObject(obj.TryGetValue("glyphs", out var g) ? g : null, path))
        {
            glyphs.Add(new(entry.Key, entry.Value as string ?? throw new FormatException($"{path}: the glyph '{entry.Key}' has no rule.")));
        }
        return glyphs;
    }

    private static string ReadPiece(PackageAssets package, string relativePath) => File.ReadAllText(package.Resolve(relativePath), Encoding.UTF8);

    // The way the sass CLI marks a compressed stylesheet holding anything beyond ASCII (and build-assets.mjs the full
    // bundles): a byte order mark, without which a stylesheet served with no charset is read in the linking page's.
    private static string WithCharset(string css) => css.Any(c => c > 0x7F) ? "\uFEFF" + css : css;

    private static Output Write(PackageAssets package, string bundle, string text, string outputRoot, long fullLength, int kept, int all)
    {
        var path = System.IO.Path.Combine(outputRoot, package.PackageId, bundle.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        // Only when it changed, so a publish that changed nothing leaves the timestamp - and what is derived from it,
        // the compressed copies - alone.
        var bytes = Utf8.GetBytes(text);
        if (File.Exists(path) is false || File.ReadAllBytes(path).SequenceEqual(bytes) is false) File.WriteAllBytes(path, bytes);

        return new Output(package.PackageId, bundle, path, fullLength, bytes.LongLength, kept, all);
    }
}
