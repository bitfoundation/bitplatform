using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI;

/// <summary>
/// Pins the glyph subset of Bit.BlazorUI.AI (Styles/fabric.mdl2.bit.blazoui.ai.scss and its woff2) to what the package's
/// components draw with it. A glyph outside the core font subset renders as an empty box unless the app loads the full
/// set of Bit.BlazorUI.Icons, so every bit-icon-ai--Name a component writes has to be in the subset - and a name the
/// subset carries that nothing uses any more is weight every app downloads for nothing.
/// </summary>
[TestClass]
public sealed partial class BitAiGlyphFontTests
{
    [TestMethod]
    public void EveryGlyphTheComponentsDrawShouldBeInTheSubsetAndNoOther()
    {
        var used = UsedGlyphs();
        var declared = DeclaredGlyphs();

        var missing = used.Except(declared, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var unused = declared.Except(used, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        Assert.AreEqual(0, missing.Length, $"Drawn but not in the AI glyph subset: {string.Join(", ", missing)}");
        Assert.AreEqual(0, unused.Length, $"In the AI glyph subset but drawn by no component: {string.Join(", ", unused)}");
    }

    [TestMethod]
    public void NoGlyphOfTheCoreSubsetShouldBeCopiedIntoTheAiOne()
    {
        var core = GlyphClass("bit-icon").Matches(SourceFiles.Read("Bit.BlazorUI", "Styles", "fabric.mdl2.bit.blazoui.scss"))
                                         .Select(m => m.Groups["name"].Value)
                                         .ToHashSet(StringComparer.Ordinal);

        var copies = DeclaredGlyphs().Where(core.Contains).Order(StringComparer.Ordinal).ToArray();

        Assert.AreEqual(0, copies.Length, $"Already in the core font, so plain IconName draws them: {string.Join(", ", copies)}");
    }

    [TestMethod]
    public void TheSubsetFontShouldShipBesideItsStylesheet()
    {
        var stylesheet = SourceFiles.Read("Bit.BlazorUI.AI", "Styles", "fabric.mdl2.bit.blazoui.ai.scss");
        var url = Regex.Match(stylesheet, @"url\('\.\./fonts/(?<file>[^']+)'\)");

        Assert.IsTrue(url.Success, "The @font-face of the AI glyph subset names no font file.");
        Assert.IsTrue(File.Exists(SourceFiles.GetPath("Bit.BlazorUI.AI", "wwwroot", "fonts", url.Groups["file"].Value)),
                      $"wwwroot/fonts/{url.Groups["file"].Value} is missing.");

        StringAssert.Contains(SourceFiles.Read("Bit.BlazorUI.AI", "Styles", "bit.blazorui.ai.scss"), "fabric.mdl2.bit.blazoui.ai.scss",
                              "The bundle does not import the AI glyph subset.");
    }

    private static string[] DeclaredGlyphs()
    {
        return GlyphClass("bit-icon-ai").Matches(SourceFiles.Read("Bit.BlazorUI.AI", "Styles", "fabric.mdl2.bit.blazoui.ai.scss"))
                                        .Select(m => m.Groups["name"].Value)
                                        .Distinct(StringComparer.Ordinal)
                                        .ToArray();
    }

    private static string[] UsedGlyphs()
    {
        var root = SourceFiles.GetDirectory("Bit.BlazorUI.AI");

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                        .Where(file => file.EndsWith(".razor", StringComparison.Ordinal) || file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".ts", StringComparison.Ordinal))
                        .Where(file => file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "node_modules" or "wwwroot") is false)
                        .SelectMany(file => UsedGlyph().Matches(SourceFiles.ReadFullPath(file)).Select(m => m.Groups["name"].Value))
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
    }

    private static Regex GlyphClass(string prefix) => new($@"\.{Regex.Escape(prefix)}--(?<name>[A-Za-z0-9]+)::before");

    [GeneratedRegex(@"bit-icon-ai--(?<name>[A-Za-z0-9]+)")]
    private static partial Regex UsedGlyph();
}
