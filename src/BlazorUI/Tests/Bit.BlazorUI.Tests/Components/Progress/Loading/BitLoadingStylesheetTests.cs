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
    public void BitLoadingParamsShouldHaveCorrectParamName()
    {
        IBitComponentParams @params = new BitLoadingParams();

        Assert.AreEqual($"{nameof(BitParams)}.BitLoading", @params.Name);
    }

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

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray());
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

            StringAssert.Contains(stylesheet, $"\n.bit-ldn-{loader} {{\n    --bit-ldn-stroke-authored:");
            StringAssert.Contains(stylesheet, "var(--bit-ldn-thickness)");
        }
    }

    [TestMethod]
    public void BitLoadingShouldKeepTheDrawingVisibleInForcedColors()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        StringAssert.Contains(@base, "@media (forced-colors: active)");
        StringAssert.Contains(@base, "--bit-ldn-color: CanvasText;");
        StringAssert.Contains(@base, "forced-color-adjust: none;");
    }

    [TestMethod]
    public void BitLoadingShouldSizeAnInlineLoaderWithItsTextUnlessASizeIsGiven()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        var unsized = @base.IndexOf("\n.bit-ldn-em {\n    --bit-ldn-sz: 1em;\n    --bit-ldn-lfs: 1em;\n}", StringComparison.Ordinal);

        // At equal specificity the later rule wins, so the 1em has to come after the root's own label size. It
        // is a class of its own, which a sized inline loader does not carry, so bit-ldn-inl must not set either.
        Assert.IsGreaterThan(@base.IndexOf("\n.bit-ldn {", StringComparison.Ordinal), unsized);
        StringAssert.DoesNotMatch(@base, new Regex(@"\.bit-ldn-inl \{[^}]*--bit-ldn-(sz|lfs)"));
    }

    [TestMethod]
    public void BitLoadingShouldSetTheLabelOfAnInlineLoaderInTheFontOfItsText()
    {
        var @base = ReadStylesheets().Single(s => s.Name == "BitLoading.scss").Content;

        StringAssert.Contains(@base, ".bit-ldn-inl > .bit-ldn-lbl {\n    font-family: inherit;\n}");
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

        StringAssert.Contains(stylesheet, $".bit-rtl .bit-ldn-{loader}-ccn {{");
        StringAssert.Contains(stylesheet, $"\n.bit-ldn-{loader}-ccn:dir(rtl) {{");
    }

    private static (string Name, string Content)[] ReadStylesheets()
    {
        var folder = SourceFiles.GetDirectory("Bit.BlazorUI", "Components", "Progress", "Loading");

        var files = Directory.GetFiles(folder, "*.scss", SearchOption.AllDirectories);

        Assert.HasCount(19, files);

        return [.. files.Select(f => (Path.GetFileName(f), SourceFiles.ReadFullPath(f)))];
    }
}
