using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bit.BlazorUI.Build;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Trimming;

/// <summary>
/// The .NET cut of the icon font (BitBlazorUITrimIconFont=DotNet, and Auto's fallback where Node.js fails): the WOFF2 reader
/// putting the transformed glyf table back into glyphs, the subsetter, the writer - run on the very font Bit.BlazorUI.Icons
/// ships, and on a font built here for what that one does not have (a composite glyph).
/// </summary>
/// <remarks>
/// ServerAppTrimmingTests in Bit.BlazorUI.Tests.Trimming checks the cut through real publishes, against the font each engine
/// wrote. That the reader puts every glyph back the way fonttools does was checked once against fonttools' own decompression,
/// outline by outline; what is pinned here is that every glyph reads, that a cut holds exactly the glyphs of its characters,
/// byte for byte the font's own, and that what is written reads back as it was.
/// </remarks>
[TestClass]
public sealed class BitIconFontCutTests
{
    private const int Add = 0xE710;
    private const int Delete = 0xE74D;

    // The icons of the stylesheet (and BitIconName) whose codepoint FabMDL2 4.66 has no glyph for, so they draw nothing at all:
    // ContactInfoMirrored, U+EA4A. Named here so a new one is not let through unseen.
    private static readonly string[] NoGlyph = ["ContactInfoMirrored"];

    private static readonly string FontPath = SourceFiles.GetPath("Bit.BlazorUI.Icons", "wwwroot", "fonts", "FabMDL2.4.66.woff2");

    private static readonly Lazy<TrueTypeFont> Icons = new(() => Woff2.Read(File.ReadAllBytes(FontPath)));

    [TestMethod]
    public void EveryGlyphOfTheIconFontShouldReadAndBeWrittenBackAsItWas()
    {
        var font = Icons.Value;
        Assert.AreEqual(Big16(font.Tables["maxp"], 4), font.Glyphs.Count, "The glyphs read are not as many as maxp says.");

        foreach (var glyph in font.Glyphs.Where(g => g.Length > 0))
        {
            Assert.IsTrue(glyph.Length >= 10 && Big16(glyph, 0) is >= 0 and < 0x8000 or 0xFFFF, "A glyph read has no TrueType header.");
        }

        var back = Woff2.Read(Woff2.Write(font));
        Assert.AreEqual(font.Glyphs.Count, back.Glyphs.Count);
        for (var i = 0; i < font.Glyphs.Count; i++) AssertSameGlyph(font.Glyphs[i], back.Glyphs[i], $"Glyph {i} changed on its way through the writer.");

        // Every table but head, whose loca format and checksum the writer sets.
        foreach (var (tag, table) in font.Tables.Where(t => t.Key != "head"))
        {
            CollectionAssert.AreEqual(table, back.Tables[tag], $"The '{tag}' table changed on its way through the writer.");
        }
    }

    [TestMethod]
    public void EveryGlyphTheIconStylesheetDrawsShouldBeACharacterOfTheFont()
    {
        // The stylesheet and the font are two files of the package: a rule drawing a codepoint the font has no glyph for
        // would draw nothing whether cut or not, and the cut would fail on it.
        var glyphs = PackageAssets.Load(SourceFiles.GetPath("Bit.BlazorUI.Icons", "obj", "bit-assets"));
        var set = glyphs.IconSets.Single();
        Assert.AreEqual("fonts/FabMDL2.4.66.woff2", set.Font, "The icon set does not name the font its @font-face loads.");

        var rules = AssetComposer.ReadGlyphs(glyphs, set).Rules;
        var cmap = TrueTypeSubsetter.ReadCmap(Icons.Value.Tables["cmap"]);

        var codepoints = rules.Select(r => (r.Key, Codepoint: IconFontSubsetter.Codepoint(r.Value))).ToList();
        CollectionAssert.AreEqual(Array.Empty<string>(), codepoints.Where(c => c.Codepoint is null).Select(c => c.Key).ToList(), "These rules draw no codepoint.");
        CollectionAssert.AreEqual(NoGlyph, codepoints.Where(c => cmap.ContainsKey(c.Codepoint!.Value) is false).Select(c => c.Key).ToList(), "These rules draw a codepoint the font has no glyph for.");
        Assert.AreEqual(rules.Count, codepoints.Select(c => c.Codepoint).Distinct().Count(), "Two icons draw the same codepoint.");
    }

    [TestMethod]
    public void ACutShouldHoldTheGlyphsOfItsCharactersAndNothingElse()
    {
        var font = Icons.Value;
        var cmap = TrueTypeSubsetter.ReadCmap(font.Tables["cmap"]);

        var cut = Woff2.Read(Woff2.Write(TrueTypeSubsetter.Subset(font, [Delete, Add])));
        var cutCmap = TrueTypeSubsetter.ReadCmap(cut.Tables["cmap"]);

        // .notdef and the two glyphs, in the font's order, each the font's own and as wide.
        Assert.AreEqual(3, cut.Glyphs.Count);
        Assert.AreEqual(3, Big16(cut.Tables["maxp"], 4));
        CollectionAssert.AreEquivalent(new[] { Add, Delete }, cutCmap.Keys.ToList());
        foreach (var character in new[] { Add, Delete })
        {
            AssertSameGlyph(font.Glyphs[cmap[character]], cut.Glyphs[cutCmap[character]], $"U+{character:X4} is not drawn with the font's own glyph.");
            Assert.AreEqual(Advance(font, cmap[character]), Advance(cut, cutCmap[character]), $"U+{character:X4} is not as wide as in the font.");
        }
        AssertSameGlyph(font.Glyphs[0], cut.Glyphs[0], "The cut does not start with the font's .notdef.");

        // The tables indexing glyphs it does not rewrite are left out, and post keeps its header alone.
        CollectionAssert.IsSubsetOf(cut.Tables.Keys.ToList(), new[] { "OS/2", "cmap", "cvt ", "fpgm", "gasp", "head", "hhea", "hmtx", "maxp", "name", "post", "prep" });
        Assert.AreEqual(32, cut.Tables["post"].Length);
        Assert.AreEqual(0x0003, Big16(cut.Tables["post"], 0));
    }

    [TestMethod]
    public void ACutShouldBeMuchSmallerThanTheFont()
    {
        var font = File.ReadAllBytes(SourceFiles.GetPath("Bit.BlazorUI.Icons", "wwwroot", "fonts", "FabMDL2.4.66.woff2"));
        var cut = Woff2.Write(TrueTypeSubsetter.Subset(Icons.Value, [Add, Delete]));

        Assert.AreEqual("wOF2", Encoding.ASCII.GetString(cut, 0, 4));
        Assert.IsLessThan(font.Length / 50, cut.Length, $"A cut of two glyphs is {cut.Length:N0} of the font's {font.Length:N0} bytes.");
    }

    [TestMethod]
    public void ACharacterTheFontHasNoGlyphForShouldBeLeftOutOfTheCut()
    {
        // It draws nothing whether the font is cut or not, so it costs the cut nothing - as pyftsubset leaves it out too.
        var cut = TrueTypeSubsetter.Subset(Icons.Value, [Add, 'A']);

        CollectionAssert.AreEquivalent(new[] { Add }, TrueTypeSubsetter.ReadCmap(cut.Tables["cmap"]).Keys.ToList());
        Assert.AreEqual(2, cut.Glyphs.Count);
    }

    [TestMethod]
    public void ACompositeGlyphShouldBringTheGlyphsItIsBuiltOfRenumbered()
    {
        // .notdef, a composite of glyph 3 (A), a simple glyph (B) and the simple glyph the composite is built of.
        var simple = SimpleGlyph();
        var composite = new byte[16];
        Put16(composite, 0, 0xFFFF);
        Put16(composite, 10, 0x0002); // ARGS_ARE_XY_VALUES, byte arguments, the last component
        Put16(composite, 12, 3);
        var font = new TrueTypeFont(0x00010000, MinimalTables(4, ('A', 1), ('B', 2)), [[], composite, simple, simple]);

        var cut = TrueTypeSubsetter.Subset(font, ['A']);

        // Glyph 2 (B) is gone, so glyph 3 is glyph 2 now, and the composite says so.
        Assert.AreEqual(3, cut.Glyphs.Count);
        Assert.AreEqual(2, Big16(cut.Glyphs[1], 12), "The composite still names its component by the number it had in the font.");
        CollectionAssert.AreEqual(simple, cut.Glyphs[2]);
        Assert.AreEqual(1, TrueTypeSubsetter.ReadCmap(cut.Tables["cmap"])['A']);

        var back = Woff2.Read(Woff2.Write(cut));
        for (var i = 0; i < cut.Glyphs.Count; i++) AssertSameGlyph(cut.Glyphs[i], back.Glyphs[i], $"Glyph {i} changed on its way through the writer.");
    }

    [TestMethod]
    public void ACutShouldBeUsedAgainWithItsReasonsUntilWhatItIsCutWithChanges()
    {
        var package = PackageAssets.Load(SourceFiles.GetPath("Bit.BlazorUI.Icons", "obj", "bit-assets"));
        var set = package.IconSets.Single();
        var glyphs = AssetComposer.ReadGlyphs(package, set);
        var selection = new AssetSelection([package]);
        selection.Icons.Add("Add");

        var output = Path.Combine(Path.GetTempPath(), "bit-blazorui-icon-font-cut", Guid.NewGuid().ToString("N"));
        try
        {
            // A tools folder with nothing in it, so Node.js fails and .NET stands in.
            IconFontSubsetter.Result Cut(string? nodePath) => IconFontSubsetter.Subset(package, set, glyphs, selection, FontPath, IconFontSubsetter.Engine.Auto, output, Path.Combine(output, "no-tools"), nodePath);

            var first = Cut(nodePath: null);
            Assert.AreEqual(".NET", first.CutBy);
            Assert.IsFalse(first.Reused);
            Assert.AreEqual(1, first.Failures.Count);
            StringAssert.StartsWith(first.Failures[0], "Node.js: ");

            var again = Cut(nodePath: null);
            Assert.IsTrue(again.Reused, "The cut of the very same inputs was made again.");
            Assert.AreEqual(".NET", again.CutBy);
            CollectionAssert.AreEqual(first.Failures, again.Failures, "The cut used again does not say why Node.js failed, as the publish that made it did.");

            // Another node is another try of Node.js.
            Assert.IsFalse(Cut(Path.Combine(output, "node")).Reused, "The cut .NET stood in for was used again for another node.");
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [TestMethod]
    public void TheIconStylesheetShouldDrawWhatACutLeavesOutFromTheCoresOwnFont()
    {
        // Linked after the core stylesheet, the icons' .bit-icon rule draws every icon, the core components' built-in ones
        // too - and a cut of its font (an Explicit list) may hold none of theirs. The core's font, which has them all, is
        // the one they fall back to.
        var core = SourceFiles.Read("Bit.BlazorUI", "Styles", "fabric.mdl2.bit.blazoui.scss");
        var icons = SourceFiles.Read("Bit.BlazorUI.Icons", "Styles", "fabric.mdl2.icons.scss");

        CollectionAssert.AreEqual(new[] { FontFamilies(icons, "@font-face").Single(), FontFamilies(core, "@font-face").Single() }, FontFamilies(icons, ".bit-icon"),
                                  "The icons' .bit-icon rule does not draw from its own font, then the core's.");
    }

    [TestMethod]
    [DataRow("Bit.BlazorUI.Extras", "fabric.mdl2.bit.blazoui.extras.scss", "bit-icon-ex")]
    [DataRow("Bit.BlazorUI.AI", "fabric.mdl2.bit.blazoui.ai.scss", "bit-icon-ai")]
    public void APackagesGlyphSubsetShouldOutrankTheIconStylesheetLinkedAfterIt(string package, string file, string glyphClass)
    {
        // Linked last, the icons' .bit-icon rule is as important as a package's own and would win by its order alone,
        // drawing the package's glyphs from a font a publish may have cut down to the app's icons. The package's rule names
        // its class twice to outrank it, and draws from the package's own font.
        var stylesheet = SourceFiles.Read(package, "Styles", file);

        CollectionAssert.AreEqual(new[] { FontFamilies(stylesheet, "@font-face").Single() }, FontFamilies(stylesheet, $".{glyphClass}.{glyphClass}"),
                                  $"The .{glyphClass} rule does not outrank .bit-icon with the package's own font.");
    }

    [TestMethod]
    [DataRow(".bit-icon--Add::before{content:\"\\E710\"}", 0xE710)]
    [DataRow(".bit-icon--Add::before { content: \"\\e710 \" }", 0xE710)]
    [DataRow(".bit-icon--Add::before{content:\"\uE710\"}", 0xE710)]
    [DataRow(".x::before{content:\"\U0001F600\"}", 0x1F600)]
    [DataRow(".bit-icon--Add::before{color:red}", null)]
    public void ARulesCodepointShouldBeReadAsAStylesheetWritesIt(string rule, int? expected)
    {
        Assert.AreEqual(expected, IconFontSubsetter.Codepoint(rule));
    }

    [TestMethod]
    [DataRow("U+E710", 0xE710)]
    [DataRow("u+e710", 0xE710)]
    [DataRow("\\E710", 0xE710)]
    [DataRow("/E710", 0xE710)]
    [DataRow("0xE710", 0xE710)]
    [DataRow(" U+1F600 ", 0x1F600)]
    [DataRow("Add", null)]
    [DataRow("E710", null)]
    [DataRow("U+", null)]
    [DataRow("U+E710X", null)]
    public void ACodepointShouldBeNamedTheWaysACsprojWritesIt(string text, int? expected)
    {
        Assert.AreEqual(expected.HasValue, IconFontSubsetter.TryParseCodepoint(text, out var codepoint), text);
        if (expected.HasValue) Assert.AreEqual(expected.Value, codepoint);
    }

    // The families the first rule of a stylesheet for the selector lists, in order.
    private static List<string> FontFamilies(string stylesheet, string selector)
    {
        var match = Regex.Match(stylesheet, $@"{Regex.Escape(selector)}\s*\{{[^}}]*?font-family:\s*(?<families>[^;!}}]+)");
        Assert.IsTrue(match.Success, $"No {selector} rule with a font-family.");
        return [.. match.Groups["families"].Value.Split(',').Select(f => f.Trim().Trim('\'', '"'))];
    }

    // One point, on the curve at the origin: the header, its end point, no instructions, one flag (on curve, x and y the same).
    private static byte[] SimpleGlyph()
    {
        var glyph = new byte[15];
        Put16(glyph, 0, 1);
        glyph[14] = 0x31;
        return glyph;
    }

    // head, hhea, maxp, hmtx, post and a format 4 cmap mapping each character to its glyph, for a font of that many glyphs.
    private static Dictionary<string, byte[]> MinimalTables(int glyphs, params (char Character, int Glyph)[] mappings)
    {
        var hhea = new byte[36];
        Put16(hhea, 34, glyphs);

        var maxp = new byte[6];
        Put16(maxp, 0, 0x0000);
        Put16(maxp, 2, 0x5000);
        Put16(maxp, 4, glyphs);

        var hmtx = new byte[glyphs * 4];
        for (var i = 0; i < glyphs; i++) Put16(hmtx, i * 4, 500 + i);

        var post = new byte[32];
        Put16(post, 0, 0x0003);

        // One segment per character, and the closing 0xFFFF one.
        var segments = mappings.Select(m => (Start: (int)m.Character, Delta: (m.Glyph - m.Character) & 0xFFFF)).Append((Start: 0xFFFF, Delta: 1)).ToList();
        var format4 = new byte[16 + segments.Count * 8];
        Put16(format4, 0, 4);
        Put16(format4, 2, format4.Length);
        Put16(format4, 6, segments.Count * 2);
        for (var s = 0; s < segments.Count; s++)
        {
            Put16(format4, 14 + s * 2, segments[s].Start);
            Put16(format4, 16 + segments.Count * 2 + s * 2, segments[s].Start);
            Put16(format4, 16 + segments.Count * 4 + s * 2, segments[s].Delta);
        }
        var cmap = new byte[12 + format4.Length];
        Put16(cmap, 2, 1);
        Put16(cmap, 4, 3);
        Put16(cmap, 6, 1);
        cmap[11] = 12;
        format4.CopyTo(cmap, 12);

        return new(StringComparer.Ordinal) { ["head"] = new byte[54], ["hhea"] = hhea, ["maxp"] = maxp, ["hmtx"] = hmtx, ["post"] = post, ["cmap"] = cmap };
    }

    // A glyph read out of glyf and loca carries the padding that aligns the next one to four bytes.
    private static void AssertSameGlyph(byte[] expected, byte[] actual, string message)
    {
        var padding = actual.Length - expected.Length;
        Assert.IsTrue(padding is >= 0 and < 4 && actual.Take(expected.Length).SequenceEqual(expected) && actual.Skip(expected.Length).All(b => b == 0), message);
    }

    private static int Advance(TrueTypeFont font, int glyph)
    {
        var longMetrics = Big16(font.Tables["hhea"], 34);
        return Big16(font.Tables["hmtx"], Math.Min(glyph, longMetrics - 1) * 4);
    }

    private static int Big16(byte[] b, int i) => (b[i] << 8) | b[i + 1];

    private static void Put16(byte[] b, int i, int value)
    {
        b[i] = (byte)(value >> 8);
        b[i + 1] = (byte)value;
    }
}
