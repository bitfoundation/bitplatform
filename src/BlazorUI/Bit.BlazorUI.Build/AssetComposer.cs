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

        /// <summary>The full bundle's size in bytes, as the same pieces write it.</summary>
        public long FullLength { get; } = fullLength;

        /// <summary>The trimmed bundle's size in bytes.</summary>
        public long TrimmedLength { get; } = trimmedLength;

        public int KeptPieces { get; } = keptPieces;

        public int AllPieces { get; } = allPieces;
    }

    /// <summary>The glyphs of an icon set, in stylesheet order, with the parts of the stylesheet around them.</summary>
    public sealed class IconGlyphs(string prefix, string suffix, List<KeyValuePair<string, string>> rules)
    {
        public string Prefix { get; } = prefix;

        public string Suffix { get; } = suffix;

        /// <summary>Each glyph's name and its rule.</summary>
        public List<KeyValuePair<string, string>> Rules { get; } = rules;
    }

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static List<Output> ComposeScripts(PackageAssets package, AssetSelection selection, string outputRoot)
    {
        var outputs = new List<Output>();
        if (package.ScriptBundle is null || package.ScriptModules.Count == 0) return outputs;

        var pieces = package.ScriptModules.Select(m => (Module: m, Text: ReadPiece(package, m.File))).ToList();
        var kept = pieces.Where(p => selection.Scripts.Contains(p.Module)).ToList();

        var text = package.ScriptPrologue + string.Concat(kept.Select(p => p.Text));
        var full = package.ScriptPrologue + string.Concat(pieces.Select(p => p.Text));
        outputs.Add(Write(package, package.ScriptBundle, text, outputRoot, full, kept.Count, pieces.Count));
        return outputs;
    }

    public static List<Output> ComposeStyles(PackageAssets package, AssetSelection selection, string outputRoot)
    {
        var outputs = new List<Output>();
        if (package.StyleBundles.Count == 0) return outputs;

        var pieces = package.StyleChunks.Select(c => (Chunk: c, Text: ReadPiece(package, c.File))).ToList();
        var kept = pieces.Where(p => selection.Styles.Contains(p.Chunk)).ToList();
        var chunks = string.Concat(kept.Select(p => p.Text));
        var allChunks = string.Concat(pieces.Select(p => p.Text));

        foreach (var bundle in package.StyleBundles)
        {
            var prefix = ReadPiece(package, bundle.Prefix);
            var suffix = ReadPiece(package, bundle.Suffix);
            var css = WithCharset(prefix + chunks + suffix);
            outputs.Add(Write(package, bundle.Bundle, css, outputRoot, WithCharset(prefix + allChunks + suffix), kept.Count, pieces.Count));
        }

        return outputs;
    }

    public static Output ComposeIcons(PackageAssets package, IconSet set, IconGlyphs glyphs, AssetSelection selection, string outputRoot)
    {
        var kept = glyphs.Rules.Where(g => selection.Icons.Contains(g.Key)).ToList();
        var css = WithCharset(glyphs.Prefix + string.Concat(kept.Select(g => g.Value)) + glyphs.Suffix);
        var full = WithCharset(glyphs.Prefix + string.Concat(glyphs.Rules.Select(g => g.Value)) + glyphs.Suffix);
        return Write(package, set.Bundle, css, outputRoot, full, kept.Count, glyphs.Rules.Count);
    }

    /// <summary>The glyphs of an icon set, in stylesheet order, with the parts around them.</summary>
    public static IconGlyphs ReadGlyphs(PackageAssets package, IconSet set)
    {
        var path = package.Resolve(set.File);
        var obj = Json.AsObject(Json.Parse(File.ReadAllText(path, Encoding.UTF8)), path);

        var rules = new List<KeyValuePair<string, string>>();
        foreach (var entry in Json.AsObject(obj.TryGetValue("glyphs", out var g) ? g : null, path))
        {
            rules.Add(new(entry.Key, entry.Value as string ?? throw new FormatException($"{path}: the glyph '{entry.Key}' has no rule.")));
        }
        return new IconGlyphs(Json.GetString(obj, "prefix", path), Json.GetString(obj, "suffix", path), rules);
    }

    private static string ReadPiece(PackageAssets package, string relativePath) => File.ReadAllText(package.Resolve(relativePath), Encoding.UTF8);

    // The way the sass CLI marks a compressed stylesheet holding anything beyond ASCII (and build-assets.mjs the full
    // bundles): a byte order mark, without which a stylesheet served with no charset is read in the linking page's.
    private static string WithCharset(string css) => css.Any(c => c > 0x7F) ? "﻿" + css : css;

    // Both sizes in the bytes they are served as, the byte order mark included, so the saving reported is the one a
    // browser sees: a glyph of the private use area is three bytes and one character.
    private static Output Write(PackageAssets package, string bundle, string text, string outputRoot, string full, int kept, int all)
    {
        var path = System.IO.Path.Combine(outputRoot, package.PackageId, bundle.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        // Only when it changed, so a publish that changed nothing leaves the timestamp - and what is derived from it,
        // the compressed copies - alone.
        var bytes = Utf8.GetBytes(text);
        if (File.Exists(path) is false || File.ReadAllBytes(path).SequenceEqual(bytes) is false) File.WriteAllBytes(path, bytes);

        return new Output(package.PackageId, bundle, path, Utf8.GetByteCount(full), bytes.LongLength, kept, all);
    }
}
