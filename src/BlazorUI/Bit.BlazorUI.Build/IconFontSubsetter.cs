using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Cuts the font of an icon set (fonts/FabMDL2.4.66.woff2) down to the glyphs of the icons a publish keeps, so the font the
/// trimmed stylesheet loads holds the glyphs it draws and nothing else - fonttools on Node.js where it can run (the
/// smallest WOFF2, its glyphs transformed as WOFF2 allows), this assembly's own WOFF2 reader and writer where it cannot
/// (on the dotnet CLI, whose runtime has Brotli).
/// </summary>
/// <remarks>
/// A cut is kept beside the trimmed stylesheet (obj) with a stamp of what it was cut from - the font's own bytes, the glyphs,
/// the engines asked for, their versions and the node the Node.js one runs on - and used again while all of them stay the
/// same: a publish pays for it once, and one whose first engine failed does not try it again until something changes,
/// saying again why it failed.
/// </remarks>
public static class IconFontSubsetter
{
    /// <summary>Which engine cuts a font: Node.js with the .NET cut where it fails, or one of the two alone.</summary>
    public enum Engine { Auto, Node, DotNet }

    /// <summary>The version of the .NET cut, in the stamp of each cut it makes: raised when what it writes changes.</summary>
    private const string DotNetVersion = "1";

    // A glyph rule's content: an escape (\E710) or the character itself, which is how sass writes a compressed stylesheet.
    private static readonly Regex Content = new(@"content:\s*""(?:\\(?<hex>[0-9A-Fa-f]{1,6})\s?|(?<char>[\uD800-\uDBFF][\uDC00-\uDFFF]|[^""\\]))", RegexOptions.CultureInvariant);

    // A codepoint as a csproj writes it in a <BitBlazorUIIcon>: U+E710, \E710 (as CSS escapes it) or 0xE710. And /E710,
    // which is what MSBuild hands a task for \E710 on Linux and macOS, where it reads an item's backslashes as a path's.
    private static readonly Regex CodepointText = new(@"^(?:[Uu]\+|[\\/]|0[xX])(?<hex>[0-9A-Fa-f]{1,6})$", RegexOptions.CultureInvariant);

    /// <summary>What became of a font: its cut (null where it is published whole), and why each engine that failed did.</summary>
    public sealed class Result
    {
        public AssetComposer.Output? Output { get; set; }

        /// <summary>The engine that cut it: "Node.js" or ".NET".</summary>
        public string? CutBy { get; set; }

        /// <summary>Whether the cut is the one an earlier publish made from the very same inputs.</summary>
        public bool Reused { get; set; }

        public List<string> Failures { get; } = [];
    }

    /// <summary>The codepoint a glyph rule draws (.bit-icon--Add::before{content:"\E710"}), or null for a rule naming none.</summary>
    public static int? Codepoint(string rule)
    {
        var match = Content.Match(rule);
        if (match.Success is false) return null;

        if (match.Groups["hex"].Success) return int.Parse(match.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return char.ConvertToUtf32(match.Groups["char"].Value, 0);
    }

    /// <summary>A codepoint as a <c>&lt;BitBlazorUIIcon&gt;</c> item writes it: U+E710, \E710 or 0xE710.</summary>
    public static bool TryParseCodepoint(string text, out int codepoint)
    {
        var match = CodepointText.Match(text.Trim());
        codepoint = match.Success ? int.Parse(match.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) : 0;
        return match.Success;
    }

    public static Result Subset(PackageAssets package, IconSet set, AssetComposer.IconGlyphs glyphs, AssetSelection selection, string fontPath, Engine engine,
                                string outputRoot, string toolsDirectory, string? nodePath)
    {
        var result = new Result();

        var kept = glyphs.Rules.Where(g => selection.Icons.Contains(g.Key)).ToList();
        if (kept.Count == glyphs.Rules.Count) return result;

        // Every glyph kept is drawn from the font: one whose codepoint cannot be read would be cut away, and the icon drawn
        // as nothing, so the font is published whole instead.
        var codepoints = new SortedSet<int>();
        foreach (var rule in kept)
        {
            if (Codepoint(rule.Value) is not { } codepoint)
            {
                result.Failures.Add($"the rule of the {rule.Key} glyph names no codepoint");
                return result;
            }
            codepoints.Add(codepoint);
        }

        var output = Path.Combine(outputRoot, package.PackageId, set.Font!.Replace('/', Path.DirectorySeparatorChar));
        var stampPath = output + ".stamp";
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var font = File.ReadAllBytes(fontPath);

        // What the Node.js cut runs - the tool the lockfile pins, on the node named or the PATH's - is in the stamp, so a cut
        // the .NET one stood in for is made again once Node.js can run.
        string? pinError = null;
        var pin = engine == Engine.DotNet ? null : NodeFontSubsetter.Pin.Read(toolsDirectory, out pinError);
        var node = engine == Engine.DotNet ? "not asked for" : $"{pin?.Hash ?? "none"} on {NodeFontSubsetter.Locate(nodePath) ?? "no node"}";

        var key = string.Join("\n", Hash(font), engine, $"Node.js {node}", $".NET {DotNetVersion}", string.Join(" ", codepoints.Select(c => c.ToString("X", CultureInfo.InvariantCulture))));
        var stamp = File.Exists(stampPath) ? File.ReadAllText(stampPath) : null;
        if (stamp is not null && File.Exists(output) && stamp.StartsWith(key + "\n", StringComparison.Ordinal))
        {
            // The engine that cut it, then why each one before it failed: said again, as the publish that cut it said it.
            var lines = stamp.Substring(key.Length + 1).Split('\n');
            result.Reused = true;
            result.CutBy = lines[0];
            result.Failures.AddRange(lines.Skip(1));
            result.Output = Output(package, set, output, font.LongLength, kept.Count, glyphs.Rules.Count);
            return result;
        }

        if (File.Exists(stampPath)) File.Delete(stampPath);

        var partial = output + ".partial";
        foreach (var candidate in engine == Engine.Auto ? [Engine.Node, Engine.DotNet] : new[] { engine })
        {
            var error = pinError;
            var cut = candidate == Engine.Node
                ? pin is not null && NodeFontSubsetter.TrySubset(fontPath, codepoints, partial, pin, nodePath, out error)
                : TryDotNet(font, codepoints, partial, out error);

            var name = candidate == Engine.Node ? "Node.js" : ".NET";
            if (cut is false)
            {
                // One line each, as the stamp keeps them.
                result.Failures.Add($"{name}: {error}".Replace('\r', ' ').Replace('\n', ' '));
                continue;
            }

            if (File.Exists(output)) File.Delete(output);
            File.Move(partial, output);
            File.WriteAllText(stampPath, string.Join("\n", new[] { key, name }.Concat(result.Failures)));

            result.CutBy = name;
            result.Output = Output(package, set, output, font.LongLength, kept.Count, glyphs.Rules.Count);
            return result;
        }

        if (File.Exists(partial)) File.Delete(partial);
        return result;
    }

    private static bool TryDotNet(byte[] font, IReadOnlyCollection<int> codepoints, string output, out string? error)
    {
        error = null;
        if (Brotli.IsAvailable is false)
        {
            error = "MSBuild runs on the .NET Framework here (Visual Studio's), which has no Brotli to read and write WOFF2 with - the dotnet CLI's has";
            return false;
        }

        try
        {
            File.WriteAllBytes(output, Woff2.Write(TrueTypeSubsetter.Subset(Woff2.Read(font), codepoints)));
            return true;
        }
        // What a font it cannot read, or a runtime whose Brotli it cannot drive, throws - the BrotliStream found by name
        // throwing from its constructor as a TargetInvocationException: the font is then published whole, never the
        // publish failed.
        catch (Exception e) when (e is FormatException or NotSupportedException or IOException or UnauthorizedAccessException or IndexOutOfRangeException
                                       or ArgumentException or InvalidDataException or InvalidOperationException or TargetInvocationException)
        {
            error = (e as TargetInvocationException)?.InnerException?.Message ?? e.Message;
            return false;
        }
    }

    /// <summary>The SHA-256 of what a cut is made from (the font, the lockfile of the Node.js one), in hex.</summary>
    internal static string Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static AssetComposer.Output Output(PackageAssets package, IconSet set, string path, long fullLength, int kept, int all)
        => new(package.PackageId, set.Font!, path, fullLength, new FileInfo(path).Length, kept, all);
}
