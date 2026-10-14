using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// The limits the engine puts on structures that refer to themselves or nest without end - a
/// color space built over itself, a stitching function listing itself, a PostScript program
/// of nothing but braces, operands nested past the content parser's depth, a tiling pattern
/// filling with itself - and, beyond "it does not crash", exactly what each one answers in
/// place of the refused structure: the documented default (DeviceGray, a constant function,
/// an unsupported function, an empty operand), with everything around it still read.
/// </summary>
[TestClass]
public class BitPdfEngineLimitsTests
{
    /// <summary>A cross-reference with no indirect objects: every value is already direct.</summary>
    private sealed class NoRefs : IBitPdfXRef
    {
        public static readonly NoRefs Instance = new();
        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false) => null;
        public object? FetchIfRef(object? value, bool suppressEncryption = false) => value;
    }

    private static object Parse(string pdf)
        => new BitPdfParser(new BitPdfLexer(new BitPdfStream(Encoding.Latin1.GetBytes(pdf)))).GetObj();

    private static BitPdfDict Dict(string entries) => (BitPdfDict)Parse($"<< {entries} >>");

    private static BitPdfStream Stream(string dictEntries, string data)
        => new(Encoding.Latin1.GetBytes(data), dict: Dict(dictEntries));

    private static BitPdfColorSpace ColorSpace(string obj, string resourceEntries = "")
        => BitPdfColorSpace.Create(Parse(obj), NoRefs.Instance, Dict(resourceEntries));

    // ----- Color spaces -----

    [TestMethod]
    public void AnICCBasedSpaceWithNoStreamShouldBeRgb()
    {
        var space = ColorSpace("[/ICCBased]");

        Assert.AreEqual(3, space.Components);
        Assert.AreEqual(((byte)255, (byte)0, (byte)0), space.GetRgb([1, 0, 0]));
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(3, 3)]
    [DataRow(4, 4)]
    [DataRow(7, 3, DisplayName = "An unknown component count is read as RGB")]
    public void AnICCBasedSpaceShouldBeReadByItsComponentCount(int n, int expectedComponents)
    {
        var array = new List<object?> { BitPdfName.Get("ICCBased"), Stream($"/N {n}", "profile") };

        var space = BitPdfColorSpace.Create(array, NoRefs.Instance, null);

        Assert.AreEqual(expectedComponents, space.Components);
    }

    [TestMethod]
    public void AnIndexedSpaceOverItselfShouldFallBackToAGrayBase()
    {
        // /CS1 is Indexed over /CS1: the base is refused and stands in as DeviceGray, so the
        // two lookup bytes are read as two gray samples (index 1 is 0xFF, white).
        var space = ColorSpace("/CS1", "/ColorSpace << /CS1 [/Indexed /CS1 1 <00FF>] >>");

        Assert.AreEqual(1, space.Components);
        Assert.AreEqual(((byte)0, (byte)0, (byte)0), space.GetRgb([0]));
        Assert.AreEqual(((byte)255, (byte)255, (byte)255), space.GetRgb([1]));
    }

    [TestMethod]
    public void ASeparationWithItselfAsAlternateShouldTintAGrayBase()
    {
        var space = ColorSpace("/CS1",
            "/ColorSpace << /CS1 [/Separation /Spot /CS1 << /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >>] >>");

        Assert.AreEqual(1, space.Components);
        // The tint function maps the tint straight onto the one gray component.
        Assert.AreEqual(((byte)0, (byte)0, (byte)0), space.GetRgb([0]));
        Assert.AreEqual(((byte)255, (byte)255, (byte)255), space.GetRgb([1]));
    }

    [TestMethod]
    public void APatternSpaceOverItselfShouldBeRgb()
    {
        var space = ColorSpace("/CS1", "/ColorSpace << /CS1 [/Pattern /CS1] >>");

        Assert.AreEqual(3, space.Components);
    }

    [TestMethod]
    [DataRow("/DeviceRGB", "/CalRGB", 3, DisplayName = "A default naming the calibrated twin of the device space")]
    [DataRow("/DeviceCMYK", "/DeviceCMYK", 4, DisplayName = "A default naming the device space itself")]
    public void ADefaultSpaceThatLeadsBackToTheDeviceSpaceShouldLeaveTheDeviceSpace(string device, string @default, int components)
    {
        string key = device switch { "/DeviceRGB" => "DefaultRGB", "/DeviceGray" => "DefaultGray", _ => "DefaultCMYK" };

        var space = ColorSpace(device, $"/ColorSpace << /{key} {@default} >>");

        Assert.AreEqual(components, space.Components);
        Assert.AreSame(device switch { "/DeviceRGB" => BitPdfColorSpace.Rgb, "/DeviceGray" => BitPdfColorSpace.Gray, _ => BitPdfColorSpace.Cmyk }, space);
    }

    [TestMethod]
    public void ADefaultSpaceThatIsAnotherSpaceShouldRedirectTheDeviceSpace()
    {
        // The guard is against a default leading back to the same device space, not against a
        // redirection: /DefaultGray may be a Separation, and then selecting DeviceGray selects it.
        var space = ColorSpace("/DeviceGray",
            "/ColorSpace << /DefaultGray [/Separation /Spot /DeviceRGB << /FunctionType 2 /Domain [0 1] /C0 [0 0 0] /C1 [1 0 0] /N 1 >>] >>");

        Assert.AreEqual(1, space.Components);
        Assert.AreEqual(((byte)255, (byte)0, (byte)0), space.GetRgb([1]));
    }

    [TestMethod]
    public void AChainOfNamedSpacesDeeperThanTheLimitShouldStillResolveTheOuterOnes()
    {
        // CS0 is Indexed over CS1, CS1 over CS2, ... CS30 over DeviceRGB: a real document nests a
        // few levels, so the chain is cut at the depth limit and the rest stands in as gray - the
        // outer spaces are still Indexed, and nothing recursed until the stack ran out.
        var entries = new StringBuilder();
        for (int i = 0; i < 30; i++)
        {
            entries.Append($"/CS{i} [/Indexed /CS{i + 1} 0 <000000>] ");
        }
        entries.Append("/CS30 [/Indexed /DeviceRGB 0 <FF0000>]");

        var outer = ColorSpace("/CS0", $"/ColorSpace << {entries} >>");
        var inner = ColorSpace("/CS30", $"/ColorSpace << {entries} >>");

        Assert.AreEqual(1, outer.Components);
        Assert.AreEqual(1, inner.Components);
        Assert.AreEqual(((byte)255, (byte)0, (byte)0), inner.GetRgb([0]));
    }

    [TestMethod]
    [DataRow("42")]
    [DataRow("(DeviceRGB)")]
    [DataRow("null")]
    [DataRow("[]")]
    [DataRow("/NoSuchSpace")]
    public void SomethingThatIsNotAColorSpaceShouldBeGray(string obj)
    {
        Assert.AreSame(BitPdfColorSpace.Gray, ColorSpace(obj));
    }

    // ----- Functions -----

    private static BitPdfDocument WithFunctions(params string[] bodies)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 10 10] >>");
        int num = 8;
        foreach (string body in bodies)
        {
            file.Obj(num++, body);
        }
        file.XRefTable($"/Size {num} /Root 1 0 R", [0, 1, 2, 3, .. Enumerable.Range(8, bodies.Length)]);
        return BitPdfDocument.Load(file.ToArray());
    }

    [TestMethod]
    public void AStitchingFunctionListingItselfShouldStandInAConstantForItself()
    {
        var document = WithFunctions("<< /FunctionType 3 /Domain [0 1] /Functions [8 0 R] /Bounds [] /Encode [0 1] >>");

        var function = BitPdfFunction.Create(new BitPdfRef(8, 0), document.XRef);

        Assert.IsNotNull(function);
        // The one sub-function is the function itself, refused and replaced by the constant 0.
        CollectionAssert.AreEqual(new double[] { 0 }, function.Eval([0.25]));
        CollectionAssert.AreEqual(new double[] { 0 }, function.Eval([0.75]));
    }

    [TestMethod]
    public void AStitchingFunctionShouldKeepTheSubFunctionsThatAreNotItself()
    {
        // Two halves: the first is the function itself (refused), the second a real ramp.
        var document = WithFunctions(
            "<< /FunctionType 3 /Domain [0 1] /Functions [8 0 R 9 0 R] /Bounds [0.5] /Encode [0 1 0 1] >>",
            "<< /FunctionType 2 /Domain [0 1] /C0 [1] /C1 [0] /N 1 >>");

        var function = BitPdfFunction.Create(new BitPdfRef(8, 0), document.XRef)!;

        CollectionAssert.AreEqual(new double[] { 0 }, function.Eval([0.25]));
        // x = 0.75 is encoded to 0.5 of the second half: halfway down the ramp from 1 to 0.
        Assert.AreEqual(0.5, function.Eval([0.75])[0], 1e-9);
    }

    [TestMethod]
    public void AStitchingChainDeeperThanTheLimitShouldStillBuild()
    {
        // Thirty stitching functions each wrapping the next, ending in a ramp.
        string[] bodies = [.. Enumerable.Range(8, 30).Select(n =>
            $"<< /FunctionType 3 /Domain [0 1] /Functions [{n + 1} 0 R] /Bounds [] /Encode [0 1] >>"),
            "<< /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >>"];
        var document = WithFunctions(bodies);

        var function = BitPdfFunction.Create(new BitPdfRef(8, 0), document.XRef);

        Assert.IsNotNull(function);
        // Past the nesting limit the chain is cut off and stands in as 0, so the ramp at the end
        // is never reached from the outermost function.
        CollectionAssert.AreEqual(new double[] { 0 }, function.Eval([1]));
        // The innermost ones, within the limit of their own, still reach it.
        Assert.AreEqual(1, BitPdfFunction.Create(new BitPdfRef(37, 0), document.XRef)!.Eval([1])[0], 1e-9);
    }

    [TestMethod]
    public void APostScriptFunctionNestedPastTheLimitShouldBeUnsupported()
    {
        var function = BitPdfFunction.Create(Stream("/FunctionType 4 /Domain [0 1] /Range [0 1]", new string('{', 101)), NoRefs.Instance);

        Assert.IsNull(function);
    }

    [TestMethod]
    public void APostScriptFunctionWithinTheLimitShouldRun()
    {
        // Fifty nested blocks beside the program: the parser keeps them, and a bare block (one no if or ifelse
        // consumes) is skipped at run time.
        string program = "{ 2 mul " + new string('{', 49) + new string('}', 49) + " }";

        var function = BitPdfFunction.Create(Stream("/FunctionType 4 /Domain [0 1] /Range [0 1]", program), NoRefs.Instance);

        Assert.IsNotNull(function);
        Assert.AreEqual(0.5, function.Eval([0.25])[0], 1e-9);
    }

    [TestMethod]
    public void AnArrayOfFunctionsShouldSkipTheOnesItCannotBuildAndConcatenateTheRest()
    {
        var array = new List<object?>
        {
            Dict("/FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1"),
            Dict("/FunctionType 9"),
            Dict("/FunctionType 2 /Domain [0 1] /C0 [1] /C1 [0] /N 1"),
        };

        var function = BitPdfFunction.Create(array, NoRefs.Instance);

        Assert.IsNotNull(function);
        CollectionAssert.AreEqual(new double[] { 0.25, 0.75 }, function.Eval([0.25]));
        Assert.IsNull(BitPdfFunction.Create(new List<object?> { Dict("/FunctionType 9") }, NoRefs.Instance));
        Assert.IsNull(BitPdfFunction.Create(null, NoRefs.Instance));
    }

    // ----- Content operands -----

    [TestMethod]
    public void NestedOperandsWithinTheDepthLimitShouldBeKeptWhole()
    {
        var operations = new BitPdfContentParser(Encoding.ASCII.GetBytes("/Tag [[[1 2] 3] << /K [4] >>] BDC EMC")).Parse();

        Assert.AreEqual("BDC", operations[0].Operator);
        var outer = (List<object?>)operations[0].Operands[1]!;
        Assert.AreEqual(2, outer.Count);
        var middle = (List<object?>)outer[0]!;
        CollectionAssert.AreEqual(new List<object?> { 1d, 2d }, (List<object?>)middle[0]!);
        Assert.AreEqual(3d, middle[1]);
        CollectionAssert.AreEqual(new List<object?> { 4d }, (List<object?>)((BitPdfDict)outer[1]!).Get("K")!);
        Assert.AreEqual("EMC", operations[1].Operator);
    }

    [TestMethod]
    [DataRow("[", "]")]
    [DataRow("<< /A ", " >>")]
    public void OperandsNestedPastTheDepthLimitShouldBeReadAsEmptyAndTheRestStillParsed(string opener, string closer)
    {
        string content = "/Tag " + string.Concat(Enumerable.Repeat(opener, 600)) + " 7 " + string.Concat(Enumerable.Repeat(closer, 600)) + " BDC 0 0 1 1 re f";

        var operations = new BitPdfContentParser(Encoding.ASCII.GetBytes(content)).Parse();

        CollectionAssert.AreEqual(new[] { "BDC", "re", "f" }, operations.Select(o => o.Operator).ToArray());
        Assert.AreEqual(2, operations[0].Operands.Count);
        Assert.AreEqual(4, operations[1].Operands.Count);
        Assert.AreEqual(1d, operations[1].Operands[3]);
    }

    [TestMethod]
    public void OperandsNestedPastTheDepthLimitAndNeverClosedShouldEndTheStreamQuietly()
    {
        string content = "0 0 1 1 re f /Tag " + string.Concat(Enumerable.Repeat("[", 600));

        var operations = new BitPdfContentParser(Encoding.ASCII.GetBytes(content)).Parse();

        CollectionAssert.AreEqual(new[] { "re", "f" }, operations.Select(o => o.Operator).ToArray());
    }

    // ----- Tiling patterns -----

    // Renders a 200x200 page whose content is the given fills of the 2x2-cell tiling pattern /P1.
    private static string RenderTiledFills(string content)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Pattern << /P1 5 0 R >> >> /Contents 4 0 R >>");
        file.Stream(4, content);
        file.Stream(5, "1 0 0 rg 0 0 1 1 re f", "/Type /Pattern /PatternType 1 /PaintType 1 /TilingType 1 /BBox [0 0 2 2] /XStep 2 /YStep 2 /Resources << >>");
        file.XRefTable("/Size 6 /Root 1 0 R", [0, 1, 2, 3, 4, 5]);
        var document = BitPdfDocument.Load(file.ToArray());

        // A renderer is made for one render of one page, the way the viewer uses it.
        return new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render();
    }

    // Every painted fill is clipped to its path, and a tiled one clips each of its cells as well.
    private static int CountClips(string html) => System.Text.RegularExpressions.Regex.Matches(html, @"clip-path:path\(").Count;

    [TestMethod]
    public void AWholeRenderShouldStopTilingOnceItsBudgetIsSpent()
    {
        // A 120x120 fill of 2x2 cells (with the margin cell on every side) is 63x63 = 3969 tiles: within the
        // per-fill limit of 4000. Four such fills stay within the whole-render budget of 16000; the fifth would
        // pass it, so it is painted as one solid fill instead.
        const string fill = "/Pattern cs /P1 scn 0 0 120 120 re f ";

        int one = CountClips(RenderTiledFills(fill));
        Assert.IsTrue(one > 3969, $"A single fill was not tiled ({one} clips).");

        int five = CountClips(RenderTiledFills(string.Concat(Enumerable.Repeat(fill, 5))));

        Assert.AreEqual(4 * one + 1, five, "Four fills tile and the fifth degrades to a single solid fill.");
    }

    [TestMethod]
    public void AFillWhoseCellsWouldExceedTheRenderBudgetShouldDegradeToASolidFill()
    {
        // One cell per point over the whole 200x200 page: 40000 cells, past the per-fill limit.
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Pattern << /P1 5 0 R >> >> /Contents 4 0 R >>");
        file.Stream(4, "/Pattern cs /P1 scn 0 0 200 200 re f");
        file.Stream(5, "1 0 0 rg 0 0 0.5 0.5 re f", "/Type /Pattern /PatternType 1 /PaintType 1 /TilingType 1 /BBox [0 0 1 1] /XStep 1 /YStep 1 /Resources << >>");
        file.XRefTable("/Size 6 /Root 1 0 R", [0, 1, 2, 3, 4, 5]);
        var document = BitPdfDocument.Load(file.ToArray());

        string html = new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render();

        // A solid fill is one element clipped to the path; a tiled one would clip every one of its cells.
        Assert.AreEqual(1, CountClips(html), "The fill was tiled rather than degraded.");
        Assert.IsTrue(html.Length < 20000, $"A degraded fill is a handful of elements, not {html.Length} characters.");
    }
}
