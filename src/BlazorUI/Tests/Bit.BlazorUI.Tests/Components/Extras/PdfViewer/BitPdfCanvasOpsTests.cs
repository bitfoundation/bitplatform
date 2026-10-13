using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Pins the exact canvas display list (<see cref="BitPdfHtmlRenderer.CanvasOpsJson"/> with
/// <see cref="BitPdfHtmlRenderer.EmitCanvasOps"/>) the viewer's JavaScript replays: the op
/// codes, the argument order, the rounding, the color strings and the JSON escaping. The
/// replay side (<c>paintCanvasPages</c>) reads these arrays positionally, so a serializer or
/// emitter change that alters any of it breaks the canvas silently - these tests make it loud.
/// </summary>
[TestClass]
public class BitPdfCanvasOpsTests
{
    private static string CanvasOps(string content, string extraResources = "")
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> {extraResources} >> /Contents 4 0 R >>");
        file.Stream(4, content);
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.XRefTable("/Size 6 /Root 1 0 R", [0, 1, 2, 3, 4, 5]);
        var document = BitPdfDocument.Load(file.ToArray());

        var renderer = new BitPdfHtmlRenderer(document.Pages[0], document.XRef) { EmitCanvasOps = true };
        string html = renderer.Render();
        StringAssert.Contains(html, "<canvas data-bit-pdv-canvas width=\"200\" height=\"200\"");
        Assert.IsNotNull(renderer.CanvasOpsJson);
        return renderer.CanvasOpsJson!;
    }

    [TestMethod]
    public void AFillShouldEmitItsExactOp()
    {
        // ["f", path in device space (y flipped), evenOdd, fill color, alpha, blend mode]
        string json = CanvasOps("1 0 0 rg 10 20 50 30 re f");

        Assert.AreEqual("""[["f","M10 180 L60 180 L60 150 L10 150 Z",false,"rgb(255,0,0)",1,""]]""", json);
    }

    [TestMethod]
    public void AnEvenOddFillWithAlphaAndABlendModeShouldEmitThem()
    {
        string json = CanvasOps("/GS1 gs 0 0.5 1 rg 0 0 10 10 re f*",
            "/ExtGState << /GS1 << /ca 0.25 /BM /Multiply >> >>");

        Assert.AreEqual("""[["f","M0 200 L10 200 L10 190 L0 190 Z",true,"rgb(0,128,255)",0.25,"multiply"]]""", json);
    }

    [TestMethod]
    public void ADashedStrokeShouldEmitItsExactOp()
    {
        // ["s", path, stroke color, device width, cap, join, miter limit, dashes (rounded to two places),
        //  dash phase, alpha, blend mode]
        string json = CanvasOps("0 0 1 RG 2 w [3 1.255] 0.5 d 1 J 2 j 10 100 m 150 100 l 150 50 l S");

        Assert.AreEqual("""[["s","M10 100 L150 100 L150 150","rgb(0,0,255)",2,1,2,10,[3,1.25],0.5,1,""]]""", json);
    }

    [TestMethod]
    public void ATextRunShouldEmitItsExactOpWithJsonEscaping()
    {
        // ["t", text, font height, font family, bold, italic, a, b, c, d, e, f, fill color, stroke color,
        //  stroke width, target advance width, alpha, blend mode, letter spacing, word spacing].
        // The <, &, >, " and ' are escaped by the serializer's default encoder, and c is a negative
        // zero; the replay side parses JSON, so neither shows there, but a change of encoder does here.
        string json = CanvasOps("0.2 0.4 0.6 rg BT /F1 12 Tf 20 150 Td (A<&>\"') Tj ET");

        Assert.AreEqual("""[["t","A\u003C\u0026\u003E\u0022\u0027",12,"sans-serif",false,false,1,0,-0,1,20,50,"rgb(51,102,153)",null,0,36.58,1,"",0,0]]""", json);
    }

    [TestMethod]
    public void AnAxialShadingShouldEmitItsExactOp()
    {
        // The clip opens a "g" group that "G" closes; the shading is ["sh", 2, [x0, y0, x1, y1], stops,
        // alpha, blend mode, bbox path], with a transparent guard stop at the end Extend leaves off.
        string json = CanvasOps("q 0 0 100 100 re W n /Sh1 sh Q",
            "/Shading << /Sh1 << /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 100 0] " +
            "/Function << /FunctionType 2 /Domain [0 1] /C0 [1 0 0] /C1 [0 0 1] /N 1 >> /Extend [true false] >> >>");

        Assert.AreEqual("""[["g","M0 200 L100 200 L100 100 L0 100 Z",false],["sh",2,[0,200,100,200],[[0.001,"rgb(255,0,0)"],[0.0444,"rgb(244,0,11)"],[0.0878,"rgb(233,0,22)"],[0.1312,"rgb(222,0,33)"],[0.1746,"rgb(211,0,44)"],[0.218,"rgb(200,0,55)"],[0.2613,"rgb(188,0,67)"],[0.3047,"rgb(177,0,78)"],[0.3481,"rgb(166,0,89)"],[0.3915,"rgb(155,0,100)"],[0.4349,"rgb(144,0,111)"],[0.4783,"rgb(133,0,122)"],[0.5217,"rgb(122,0,133)"],[0.5651,"rgb(111,0,144)"],[0.6085,"rgb(100,0,155)"],[0.6519,"rgb(89,0,166)"],[0.6953,"rgb(78,0,177)"],[0.7387,"rgb(67,0,188)"],[0.782,"rgb(55,0,200)"],[0.8254,"rgb(44,0,211)"],[0.8688,"rgb(33,0,222)"],[0.9122,"rgb(22,0,233)"],[0.9556,"rgb(11,0,244)"],[0.999,"rgb(0,0,255)"],[1,"rgba(0,0,255,0)"]],1,"",null],["G"]]""", json);
    }

    [TestMethod]
    public void ARadialShadingShouldEmitItsExactOp()
    {
        // ["sh", 3, [cx, cy, r0, r1], stops, alpha, blend mode, bbox path]
        string json = CanvasOps("/Sh1 sh",
            "/Shading << /Sh1 << /ShadingType 3 /ColorSpace /DeviceGray /Coords [100 100 0 100 100 50] " +
            "/Function << /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >> >> >>");

        Assert.AreEqual("""[["sh",3,[100,100,0,50],[[0,"rgb(0,0,0)"],[0.0435,"rgb(11,11,11)"],[0.087,"rgb(22,22,22)"],[0.1304,"rgb(33,33,33)"],[0.1739,"rgb(44,44,44)"],[0.2174,"rgb(55,55,55)"],[0.2609,"rgb(67,67,67)"],[0.3043,"rgb(78,78,78)"],[0.3478,"rgb(89,89,89)"],[0.3913,"rgb(100,100,100)"],[0.4348,"rgb(111,111,111)"],[0.4783,"rgb(122,122,122)"],[0.5217,"rgb(133,133,133)"],[0.5652,"rgb(144,144,144)"],[0.6087,"rgb(155,155,155)"],[0.6522,"rgb(166,166,166)"],[0.6957,"rgb(177,177,177)"],[0.7391,"rgb(188,188,188)"],[0.7826,"rgb(200,200,200)"],[0.8261,"rgb(211,211,211)"],[0.8696,"rgb(222,222,222)"],[0.913,"rgb(233,233,233)"],[0.9565,"rgb(244,244,244)"],[1,"rgb(255,255,255)"]],1,"",null]]""", json);
    }

    [TestMethod]
    public void TheOpsShouldReplayAsPositionalArrays()
    {
        // Whatever the values, every op is a JSON array whose first element is its code.
        string json = CanvasOps("1 0 0 rg 0 0 5 5 re f 0 0 0 RG 0 0 m 5 5 l S BT /F1 9 Tf 1 1 Td (x) Tj ET");

        using var parsed = JsonDocument.Parse(json);
        var codes = new StringBuilder();
        foreach (var op in parsed.RootElement.EnumerateArray())
        {
            Assert.AreEqual(JsonValueKind.Array, op.ValueKind);
            codes.Append(op[0].GetString()).Append(' ');
        }
        Assert.AreEqual("f s t ", codes.ToString());
    }

    [TestMethod]
    public void HtmlModeShouldEmitNoCanvasOps()
    {
        var document = BitPdfDocument.Load(BitPdfEngineTestFile.SinglePage(BitPdfEngineTestFile.ShowText("html")));
        var renderer = new BitPdfHtmlRenderer(document.Pages[0], document.XRef);

        string html = renderer.Render();

        Assert.IsNull(renderer.CanvasOpsJson);
        Assert.IsFalse(html.Contains("<canvas"));
    }

    [TestMethod]
    public void TheCanvasRendererShouldHandBackTheSameOps()
    {
        var document = BitPdfDocument.Load(BitPdfEngineTestFile.SinglePage(Encoding.ASCII.GetBytes("1 0 0 rg 10 20 50 30 re f")));

        var result = new BitPdfCanvasRenderer(document.Pages[0], document.XRef).Render();

        Assert.AreEqual("""[["f","M10 180 L60 180 L60 150 L10 150 Z",false,"rgb(255,0,0)",1,""]]""", result.OpsJson);
        Assert.AreEqual(200, result.Width);
        Assert.AreEqual(200, result.Height);
    }
}
