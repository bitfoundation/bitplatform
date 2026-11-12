using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

/// <summary>
/// Pins what a bUnit render cannot see about the stylesheets of the loading components: that the public
/// --bit-Loading-* variables are only ever read, and that every drawing is laid out in CSS from the one size
/// they resolve, which is what lets --bit-Loading-size resize a loader whole.
/// </summary>
[TestClass]
public class BitLoadingStylesheetTests
{
    private static readonly string[] StrokedLoaders = ["rng", "dur", "rpl", "xbx", "spn"];



    [TestMethod]
    public void BitLoadingShouldNeverDeclareAPublicVariable()
    {
        // A declaration would stop a value set on :root or on an ancestor from inheriting down to the loader.
        var declaration = new Regex(@"^\s*--bit-Loading-[\w-]+\s*:", RegexOptions.Multiline);

        foreach (var (name, stylesheet) in ReadStylesheets())
        {
            Assert.IsFalse(declaration.IsMatch(stylesheet), $"{name}: {declaration.Match(stylesheet).Value}");
        }
    }

    [TestMethod]
    public void BitLoadingShouldDocumentEveryPublicVariableItReads()
    {
        var all = ReadStylesheets().ToArray();
        var @base = all.Single(s => s.Name == "BitLoading.scss").Content;

        var documented = Regex.Matches(@base, @"^//\s+(--bit-Loading-[\w-]+)", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToHashSet();

        var read = all.SelectMany(s => Regex.Matches(s.Content, @"var\((--bit-Loading-[\w-]+)").Select(m => m.Groups[1].Value))
                      .ToHashSet();

        Assert.AreSequenceEqual(documented.Order().ToArray(), read.Order().ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public void BitLoadingShouldLayOutEveryDrawingFromTheResolvedSize()
    {
        // The offsets used to be written into the style attribute in pixels, computed from the Size parameter, so
        // a size set in CSS resized the box while the drawing inside it stayed where it was.
        var pixelOffset = new Regex(@"var\(--bit-ldn-[a-z]{3}-\d+f?\)");

        foreach (var (name, stylesheet) in ReadStylesheets())
        {
            Assert.IsFalse(pixelOffset.IsMatch(stylesheet), $"{name}: {pixelOffset.Match(stylesheet).Value}");
        }
    }

    [TestMethod]
    public void BitLoadingShouldGiveEveryStrokedLoaderTheWidthItWasDrawnAt()
    {
        var all = ReadStylesheets().ToArray();

        foreach (var loader in StrokedLoaders)
        {
            var stylesheet = all.Single(s => s.Content.Contains($".bit-ldn-{loader}-ccn {{")).Content;

            Assert.Contains($"\n.bit-ldn-{loader} {{\n    --bit-ldn-stroke-authored:", stylesheet);
            Assert.Contains("var(--bit-ldn-thickness)", stylesheet);
        }
    }

    [TestMethod]
    public void BitLoadingShouldKeepTheDrawingVisibleInForcedColors()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        Assert.Contains("@media (forced-colors: active)", @base);
        Assert.Contains("--bit-ldn-color: CanvasText;", @base);
        Assert.Contains("forced-color-adjust: none;", @base);
    }

    [TestMethod]
    public void BitLoadingShouldSizeAnInlineLoaderWithItsTextUnlessASizeIsGiven()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        var unsized = @base.IndexOf("\n.bit-ldn-em {\n    --bit-ldn-sz-def: 1em;\n    --bit-ldn-lfs-def: 1em;\n}", StringComparison.Ordinal);

        // At equal specificity the later rule wins, so the 1em has to come after the root's own resets. It is a class
        // of its own, which a sized inline loader does not carry, so bit-ldn-inl must not set either. It is the default
        // of an unsized loader rather than a size it was given, so it is read after the public variables.
        Assert.IsGreaterThan(@base.IndexOf("\n.bit-ldn {", StringComparison.Ordinal), unsized);
        Assert.DoesNotMatchRegex(new Regex(@"\.bit-ldn-inl \{[^}]*--bit-ldn-(sz|lfs)"), @base);
        Assert.Contains("--bit-ldn-size: var(--bit-ldn-sz, var(--bit-Loading-size, var(--bit-ldn-sz-def, 64px)));", @base);
        Assert.Contains("font-size: var(--bit-ldn-lfs, var(--bit-Loading-label-font-size, var(--bit-ldn-lfs-def, #{$tg-fs-sm})));", @base);
    }

    [TestMethod]
    public void BitLoadingShouldLetAParameterWinOverItsPublicVariable()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        // Color/CustomColor, Size/CustomSize, Thickness and Speed publish these only while they are set (inline, or
        // through a size class), so they are read before the variable, which only restyles the default.
        Assert.Contains("--bit-ldn-color: var(--bit-ldn-clr, var(--bit-Loading-color, #{$clr-pri}));", @base);
        Assert.Contains("--bit-ldn-size: var(--bit-ldn-sz, var(--bit-Loading-size, ", @base);
        Assert.Contains("--bit-ldn-thickness: var(--bit-ldn-stroke, var(--bit-Loading-thickness, var(--bit-ldn-stroke-authored)));", @base);
        Assert.Contains("calc(#{$mot-loop-factor} / var(--bit-ldn-spd, var(--bit-Loading-speed, 1)))", @base);
        Assert.Contains("font-size: var(--bit-ldn-lfs, var(--bit-Loading-label-font-size, ", @base);

        // A custom size scales the label from the size it was given, which is a choice as well.
        Assert.Contains("\n.bit-ldn-csz {\n    --bit-ldn-lfs: clamp(", @base);

        foreach (var (name, stylesheet) in ReadStylesheets())
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Loading-[a-z-]+, var\(--bit-ldn-(?!sz-def|lfs-def|stroke-authored)"), $"{name}: a public variable is read before the parameter it restyles the default of.");
        }
    }

    [TestMethod]
    public void BitLoadingShouldPublishItsParametersOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content, "\n.bit-ldn {");

        // A loader can sit in the label of another one, which must not inherit the outer loader's color, size, stroke
        // or speed: each root starts the values the parameters publish out unset, and the classes - declared further
        // down at the same weight - and the style attribute still win on the root that carries them.
        foreach (var property in new[] { "--bit-ldn-clr", "--bit-ldn-sz", "--bit-ldn-lfs", "--bit-ldn-stroke", "--bit-ldn-spd", "--bit-ldn-sz-def", "--bit-ldn-lfs-def" })
        {
            Assert.Contains($"{property}: initial;", root);
        }
    }

    [TestMethod]
    public void BitLoadingShouldSetTheLabelOfAnInlineLoaderInTheFontOfItsText()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        Assert.Contains(".bit-ldn-inl > .bit-ldn-lbl {\n    font-family: inherit;\n}", @base);
    }

    [TestMethod]
    public void BitLoadingShouldBeMovingFromTheFrameItAppears()
    {
        // A positive delay holds a part still until its turn, so a loader that appears reads as stuck - for
        // longer still under reduced motion, which stretches every delay with the durations. The stagger
        // between the parts is written as a negative delay instead, which keeps the same phase offsets.
        var positiveDelay = new Regex(@"animation-delay:\s*calc\(0?\.?[1-9][\d.]*s|animation:[^;]*?\ds\s*\*[^;]*?\)\)\s+calc\(\d");

        foreach (var (name, stylesheet) in ReadStylesheets())
        {
            Assert.IsFalse(positiveDelay.IsMatch(stylesheet), $"{name}: {positiveDelay.Match(stylesheet).Value}");
        }
    }

    [TestMethod,
        DataRow("BitEllipsisLoading.scss", "elp"),
        DataRow("BitRollingSquareLoading.scss", "rsq")]
    public void BitLoadingShouldMirrorTheTravellingLoadersForAnInheritedRightToLeftDirection(string file, string loader)
    {
        var stylesheet = ReadStylesheets().Single(s => s.Name == file).Content;

        // The class Dir renders is read on the drawing's parent only, so a loader nested in one that reads the other way
        // keeps its own direction.
        Assert.Contains($"\n.bit-ldn-{loader}-ccn:is(:dir(rtl), .bit-ldn-{loader}.bit-rtl > *) {{", stylesheet);
        Assert.DoesNotContain($".bit-rtl .bit-ldn-{loader}-ccn", stylesheet, "The class must not reach the drawing of a nested loader.");
    }

    private static (string Name, string Content)[] ReadStylesheets()
    {
        var folder = SourceFiles.GetDirectory("Bit.BlazorUI", "Components", "Progress", "Loading");

        var files = Directory.GetFiles(folder, "*.scss", SearchOption.AllDirectories);

        Assert.HasCount(19, files);

        return [.. files.Select(f => (Path.GetFileName(f), SourceFiles.ReadStylesheet(f)))];
    }
}
