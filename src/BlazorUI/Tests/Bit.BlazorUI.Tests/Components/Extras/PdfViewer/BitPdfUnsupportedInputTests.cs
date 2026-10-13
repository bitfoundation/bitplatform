using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// What the engine and the viewer do with input they cannot open: unsupported security
/// handlers, missing or wrong passwords, files that are not PDFs, and truncated or damaged
/// files. The documented contract is a typed exception from <see cref="BitPdfDocument.Load(byte[], string?)"/>
/// (<see cref="BitPdfFormatException"/>, <see cref="BitPdfPasswordException"/>,
/// <see cref="BitPdfUnsupportedEncryptionException"/>) and, in the viewer, <c>OnError</c> with
/// an error shown in place of the pages.
/// </summary>
[TestClass]
public class BitPdfUnsupportedInputTests : BunitTestContext
{
    private static byte[] WithEncryptDictionary(string encryptBody)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>");
        file.Stream(4, "BT ET");
        file.Obj(5, encryptBody);
        file.XRefTable("/Size 6 /Root 1 0 R /Encrypt 5 0 R /ID [<00112233445566778899AABBCCDDEEFF> <00112233445566778899AABBCCDDEEFF>]", [0, 1, 2, 3, 4, 5]);
        return file.ToArray();
    }

    private static readonly string Zeros32 = new('0', 64);

    // ----- The engine -----

    [TestMethod]
    [DataRow("<< /Filter /Adobe.PubSec /V 4 /R 4 /SubFilter /adbe.pkcs7.s5 >>", DisplayName = "Public-key handler")]
    [DataRow("<< /Filter /Standard /V 7 /R 7 /O <{0}> /U <{0}> /P -1 >>", DisplayName = "Unknown standard revision")]
    [DataRow("<< /V 2 /R 3 /O <{0}> /U <{0}> /P -1 >>", DisplayName = "No /Filter")]
    public void AnUnsupportedSecurityHandlerShouldThrowItsTypedException(string encrypt)
    {
        byte[] bytes = WithEncryptDictionary(string.Format(encrypt, Zeros32));

        Assert.ThrowsExactly<BitPdfUnsupportedEncryptionException>(() => BitPdfDocument.Load(bytes));
    }

    [TestMethod]
    public void AMissingPasswordShouldThrowAPasswordExceptionThatSaysNoneWasGiven()
    {
        var ex = Assert.ThrowsExactly<BitPdfPasswordException>(() => BitPdfDocument.Load(TestPdf.Encrypted("hunter2")));

        Assert.IsFalse(ex.WasProvided);
    }

    [TestMethod]
    public void AWrongPasswordShouldThrowAPasswordExceptionThatSaysOneWasGiven()
    {
        var ex = Assert.ThrowsExactly<BitPdfPasswordException>(() => BitPdfDocument.Load(TestPdf.Encrypted("hunter2"), "letmein"));

        Assert.IsTrue(ex.WasProvided);
    }

    [TestMethod]
    public void TheRightPasswordShouldOpenTheDocumentAndExposeItsPermissions()
    {
        var document = BitPdfDocument.Load(TestPdf.Encrypted("hunter2", permissions: -4 & ~(1 << 2)), "hunter2");

        Assert.IsTrue(document.IsEncrypted);
        Assert.AreEqual(1, document.PageCount);
        Assert.IsFalse(document.Permissions.CanPrint);
        Assert.IsTrue(document.Permissions.CanCopy);
        // The content stream decrypts to "BT ET".
        Assert.AreEqual("BT ET", Encoding.Latin1.GetString(document.Pages[0].GetContentBytes()));
    }

    [TestMethod]
    [DataRow("", DisplayName = "Empty")]
    [DataRow("not a pdf", DisplayName = "Plain text")]
    [DataRow("%PDF-1.7\n", DisplayName = "Header only")]
    [DataRow("%PDF-1.7\n1 0 obj\n<< /Type /Pages >>\nendobj\ntrailer\n<< /Size 2 >>\n%%EOF", DisplayName = "No catalog")]
    public void InputThatIsNotADocumentShouldThrowAFormatException(string text)
    {
        Assert.ThrowsExactly<BitPdfFormatException>(() => BitPdfDocument.Load(Encoding.Latin1.GetBytes(text)));
    }

    [TestMethod]
    public void NullBytesShouldThrowAnArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitPdfDocument.Load((byte[])null!));
    }

    /// <summary>
    /// Opens <paramref name="bytes"/> and touches everything the viewer reads from a document.
    /// Returns null when it all worked, or the exception when one escaped that is not one of
    /// the engine's documented, typed ones.
    /// </summary>
    private static Exception? OpenEverything(byte[] bytes)
    {
        try
        {
            var document = BitPdfDocument.Load(bytes);
            _ = document.Metadata;
            _ = document.Outline;
            _ = document.PageLabels;
            _ = document.Attachments;
            _ = document.Layers;
            foreach (var page in document.Pages)
            {
                _ = new BitPdfHtmlRenderer(page, document.XRef).Render();
                _ = new BitPdfHtmlRenderer(page, document.XRef) { EmitCanvasOps = true }.Render();
                _ = page.ExtractText();
            }
            return null;
        }
        catch (BitPdfFormatException)
        {
            return null;
        }
        catch (BitPdfPasswordException)
        {
            return null;
        }
        catch (BitPdfUnsupportedEncryptionException)
        {
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [TestMethod]
    public void EveryTruncationOfADocumentShouldOpenOrFailWithATypedException()
    {
        byte[] full = TestPdf.RichDocument();
        var failures = new List<string>();

        for (int length = 0; length <= full.Length; length += Math.Max(1, full.Length / 400))
        {
            if (OpenEverything(full[..length]) is { } ex)
            {
                failures.Add($"{length}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Take(10)));
    }

    [TestMethod]
    public void RandomByteDamageShouldOpenOrFailWithATypedException()
    {
        byte[] original = TestPdf.RichDocument();
        var random = new Random(20261013);
        var failures = new List<string>();

        for (int round = 0; round < 300; round++)
        {
            byte[] damaged = (byte[])original.Clone();
            int hits = random.Next(1, 6);
            for (int h = 0; h < hits; h++)
            {
                damaged[random.Next(damaged.Length)] = (byte)random.Next(256);
            }
            if (OpenEverything(damaged) is { } ex)
            {
                failures.Add($"round {round}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("\n", failures.Take(10)));
    }

    // ----- Hostile content -----

    /// <summary>
    /// A one-page document drawing <paramref name="content"/> with <paramref name="resources"/>
    /// added to the page's resources; <paramref name="extraObjects"/> become objects 8, 9, ...
    /// (a body starting with <c>S:</c> is a stream: <c>S:dictEntries|data</c>). Objects 6 and 7
    /// are an outline whose only item is its own next sibling and first child.
    /// </summary>
    private static byte[] Hostile(string content, string resources, params string[] extraObjects)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R /Outlines 6 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R 3 0 R] /Count 2 >>");
        file.Obj(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> {resources} >> /Contents 4 0 R >>");
        file.Stream(4, content);
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.Obj(6, "<< /Type /Outlines /First 7 0 R /Last 7 0 R >>");
        file.Obj(7, "<< /Title (loop) /Parent 6 0 R /Next 7 0 R /First 7 0 R /Dest [3 0 R /Fit] >>");
        int num = 8;
        foreach (string body in extraObjects)
        {
            if (body.StartsWith("S:", StringComparison.Ordinal))
            {
                string[] parts = body[2..].Split('|', 2);
                file.Stream(num, parts[1], parts[0]);
            }
            else
            {
                file.Obj(num, body);
            }
            num++;
        }
        file.XRefTable($"/Size {num} /Root 1 0 R", Enumerable.Range(0, num));
        return file.ToArray();
    }

    private static readonly Dictionary<string, Func<byte[]>> HostileCases = new()
    {
        ["form that draws itself"] = () => Hostile("/Fm1 Do", "/XObject << /Fm1 8 0 R >>",
            "S:/Type /XObject /Subtype /Form /BBox [0 0 10 10] /Resources << /XObject << /Fm1 8 0 R >> >>|/Fm1 Do 0 0 1 1 re f"),
        ["type3 glyph that shows itself"] = () => Hostile("BT /F3 12 Tf 0 0 Td (A) Tj ET", "/Font << /F1 5 0 R /F3 8 0 R >>",
            "<< /Type /Font /Subtype /Type3 /FontBBox [0 0 1 1] /FontMatrix [1 0 0 1 0 0] /CharProcs << /A 9 0 R >> " +
                "/Encoding << /Differences [65 /A] >> /FirstChar 65 /LastChar 65 /Widths [1] /Resources << /Font << /F3 8 0 R >> >> >>",
            "S:|BT /F3 12 Tf (A) Tj ET"),
        ["stream whose length is itself"] = () => Hostile("/Fm1 Do", "/XObject << /Fm1 8 0 R >>",
            "S:/Type /XObject /Subtype /Form /BBox [0 0 1 1] /Length 8 0 R|0 0 1 1 re f"),
        ["tiling pattern with a vanishing step"] = () => Hostile("/Pattern cs /P1 scn 0 0 200 200 re f", "/Pattern << /P1 8 0 R >>",
            "S:/Type /Pattern /PatternType 1 /PaintType 1 /TilingType 1 /BBox [0 0 0.0001 0.0001] /XStep 0.0001 /YStep 0.0001 /Resources << >>|0 0 1 1 re f"),
        ["tiling pattern with a zero step"] = () => Hostile("/Pattern cs /P1 scn 0 0 200 200 re f", "/Pattern << /P1 8 0 R >>",
            "S:/Type /Pattern /PatternType 1 /PaintType 1 /TilingType 1 /BBox [0 0 1 1] /XStep 0 /YStep 0 /Resources << >>|0 0 1 1 re f"),
        ["200000 unbalanced saves"] = () => Hostile(string.Concat(Enumerable.Repeat("q ", 200000)) + "0 0 1 1 re f", ""),
        ["100000 unclosed marked-content sections"] = () => Hostile(string.Concat(Enumerable.Repeat("/OC /X BDC ", 100000)) + "0 0 1 1 re f", ""),
        ["100000 nested arrays in a resource dictionary"] = () => Hostile("", "/Foo " + new string('[', 100000)),
        ["a 2-gigapixel image"] = () => Hostile("q 100 0 0 100 0 0 cm /Im1 Do Q", "/XObject << /Im1 8 0 R >>",
            "S:/Type /XObject /Subtype /Image /Width 2000000000 /Height 2 /ColorSpace /DeviceRGB /BitsPerComponent 8|abc"),
        ["an image with a negative width"] = () => Hostile("q 100 0 0 100 0 0 cm /Im1 Do Q", "/XObject << /Im1 8 0 R >>",
            "S:/Type /XObject /Subtype /Image /Width -5 /Height 3 /ColorSpace /DeviceGray /BitsPerComponent 16|abc"),
        ["an indexed image with a short palette"] = () => Hostile("q 100 0 0 100 0 0 cm /Im1 Do Q", "/XObject << /Im1 8 0 R >>",
            "S:/Type /XObject /Subtype /Image /Width 3 /Height 1 /ColorSpace [/Indexed /DeviceRGB 255 <00>] /BitsPerComponent 8|\u00ff\u00ff\u00ff"),
        ["a sampled function claiming 2 billion samples"] = () => Hostile("/Sh1 sh",
            "/Shading << /Sh1 << /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 1 0] /Function 8 0 R >> >>",
            "S:/FunctionType 0 /Domain [0 1] /Range [0 1 0 1 0 1] /Size [2000000000] /BitsPerSample 32|abc"),
        ["a PostScript function with hostile stack operators"] = () => Hostile("/Sh1 sh",
            "/Shading << /Sh1 << /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 1 0] /Function 8 0 R >> >>",
            "S:/FunctionType 4 /Domain [0 1] /Range [0 1 0 1 0 1]|{ 1 2 3 1000000000 -2000000000 roll 5 -1 roll 0 index 99 index 1 0 div 2147483647 copy }"),
        ["a CID font with a broken /W and an overflowing ToUnicode"] = () => Hostile("BT /F0 12 Tf 0 0 Td <0001FFFF> Tj ET", "/Font << /F1 5 0 R /F0 8 0 R >>",
            "<< /Type /Font /Subtype /Type0 /BaseFont /X /Encoding /Identity-H /DescendantFonts [9 0 R] /ToUnicode 10 0 R >>",
            "<< /Type /Font /Subtype /CIDFontType2 /W [0 [1e400] 1 65535 -5 1 2 3 4 5 6] /DW -1 >>",
            "S:|1 begincodespacerange <0000> <FFFF> endcodespacerange 2 beginbfrange <0000> <FFFF> <0041> <0001> <0001> [<D800> <DFFF0041>] endbfrange"),
        ["a simple font whose FirstChar is near int.MaxValue"] = () => Hostile("BT /F2 12 Tf 0 0 Td (AB) Tj ET", "/Font << /F1 5 0 R /F2 8 0 R >>",
            "<< /Type /Font /Subtype /TrueType /BaseFont /Arial /FirstChar 2147483600 /LastChar 2147483647 /Widths [1 2 3] >>"),
        ["a dash pattern of 1e-30"] = () => Hostile("[1e-30 1e-30] 0 d 0 0 m 200 200 l S", ""),
        // Ten levels of stitching functions, each listing the next one ten times: no cycle,
        // but 10^10 sub-functions if every reference were built out on its own.
        ["stitching functions fanned out ten times at every level"] = () => Hostile("/Sh1 sh",
            "/Shading << /Sh1 << /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 100 0] /Function 8 0 R >> >>",
            [.. Enumerable.Range(8, 10).Select(n =>
                $"<< /FunctionType 3 /Domain [0 1] /Functions [{string.Concat(Enumerable.Repeat($"{n + 1} 0 R ", 10))}] " +
                "/Bounds [0.1 0.2 0.3 0.4 0.5 0.6 0.7 0.8 0.9] /Encode [0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1 0 1] >>"),
            "<< /FunctionType 2 /Domain [0 1] /C0 [1 0 0] /C1 [0 0 1] /N 1 >>"]),
        // A /DefaultRGB that leads back to DeviceRGB through a calibrated name, and an
        // Indexed space over DeviceRGB.
        ["a default color space that leads back to itself"] = () => Hostile("/DeviceRGB cs 1 0 0 sc 0 0 9 9 re f /CS1 cs 0 sc 0 0 5 5 re f",
            "/ColorSpace << /DefaultRGB /CalRGB /DefaultGray [/Indexed /DeviceGray 1 <00FF>] /CS1 [/Indexed /DeviceRGB 0 <FF0000>] >>"),
    };

    public static IEnumerable<object[]> HostileCaseNames => HostileCases.Keys.Select(k => new object[] { k });

    [TestMethod]
    [DynamicData(nameof(HostileCaseNames))]
    public void HostileContentShouldRenderOrFailWithATypedException(string name)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Exception? ex = OpenEverything(HostileCases[name]());

        Assert.IsNull(ex, ex?.ToString());
        Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(10), $"Took {watch.Elapsed}.");
    }

    // The next ones used to overflow the stack, which no catch can stop: a regression takes
    // the whole test host down with it (exit 0xC00000FD) rather than failing one test.

    // BitPdfContentParser.ReadArray / ReadDict recursed once per nesting level with no depth
    // limit; they now stop at BitPdfParser's MaxDepth and skip what is deeper.
    [TestMethod]
    [DataRow("[")]
    [DataRow("<< /A ")]
    public void DeeplyNestedContentOperandsShouldNotOverflowTheStack(string opener)
    {
        byte[] bytes = Hostile("/Tag " + string.Concat(Enumerable.Repeat(opener, 100000)) + " BDC EMC 0 0 1 1 re f", "");

        Assert.IsNull(OpenEverything(bytes));
    }

    // BitPdfColorSpace.Create and the Indexed / Separation builders recursed into a base or
    // alternate space with no cycle guard; they now resolve within a BitPdfResolveScope.
    [TestMethod]
    [DataRow("/CS1", DisplayName = "A named space that is its own name")]
    [DataRow("8 0 R|[/Indexed 8 0 R 1 <0000>]", DisplayName = "Indexed over itself")]
    [DataRow("8 0 R|[/Separation /Spot 8 0 R << /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >>]", DisplayName = "Separation with itself as alternate")]
    [DataRow("8 0 R|[/Pattern 8 0 R]", DisplayName = "Pattern over itself")]
    public void ASelfReferencingColorSpaceShouldNotOverflowTheStack(string definition)
    {
        string[] parts = definition.Split('|');
        byte[] bytes = parts.Length == 1
            ? Hostile("/CS1 cs 0.5 scn 0 0 1 1 re f", $"/ColorSpace << /CS1 {parts[0]} >>")
            : Hostile("/CS1 cs 0.5 scn 0 0 1 1 re f", $"/ColorSpace << /CS1 {parts[0]} >>", parts[1]);

        Assert.IsNull(OpenEverything(bytes));
    }

    // BitPdfFunction.Create and BitPdfStitchingFunction built sub-functions with no cycle
    // guard; they now resolve within a BitPdfResolveScope.
    [TestMethod]
    public void ASelfReferencingStitchingFunctionShouldNotOverflowTheStack()
    {
        byte[] bytes = Hostile("/Sh1 sh", "/Shading << /Sh1 << /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 1 0] /Function 8 0 R >> >>",
            "<< /FunctionType 3 /Domain [0 1] /Functions [8 0 R] /Bounds [] /Encode [0 1] >>");

        Assert.IsNull(OpenEverything(bytes));
    }

    // BitPdfPostScriptFunction.ParseBlock recursed once per '{' with no depth limit; a
    // program nested past its limit is now an unsupported function.
    [TestMethod]
    public void ADeeplyNestedPostScriptFunctionShouldNotOverflowTheStack()
    {
        byte[] bytes = Hostile("/Sh1 sh", "/Shading << /Sh1 << /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 1 0] /Function 8 0 R >> >>",
            "S:/FunctionType 4 /Domain [0 1] /Range [0 1 0 1 0 1]|" + new string('{', 50000));

        Assert.IsNull(OpenEverything(bytes));
    }

    // A tiling pattern whose cell selects the same pattern again multiplied the cell count at
    // each of MaxFormDepth levels (MaxTiles capped one level, not the product) and ran ~50 s
    // into an OutOfMemoryException; the product of nested cells is now bounded too.
    [TestMethod]
    public void ASelfReferencingTilingPatternShouldRenderInBoundedTime()
    {
        byte[] bytes = Hostile("/Pattern cs /P1 scn 0 0 50 50 re f", "/Pattern << /P1 8 0 R >>",
            "S:/Type /Pattern /PatternType 1 /PaintType 1 /TilingType 1 /BBox [0 0 2 2] /XStep 2 /YStep 2 " +
                "/Resources << /Pattern << /P1 8 0 R >> >>|/Pattern cs /P1 scn 0 0 2 2 re f");
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Assert.IsNull(OpenEverything(bytes));
        Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(10), $"Took {watch.Elapsed}.");
    }

    // A CTM or font size that overflows to Infinity under a text run put a non-finite number
    // into the canvas display list, which JSON cannot write: the serializer threw and the
    // whole page failed in canvas mode while HTML mode rendered it. Such an op is now dropped.
    [TestMethod]
    [DataRow("1e308 0 0 1e308 0 0 cm 1e308 0 0 1e308 0 0 cm BT /F1 12 Tf (x) Tj ET")]
    [DataRow("BT /F1 1e30 Tf 0 0 Td (Hi) Tj ET BT /F1 -1e308 Tf 1e308 1e308 Td (x) Tj ET")]
    public void ANonFiniteCoordinateShouldNotFailTheCanvasRender(string content)
    {
        var document = BitPdfDocument.Load(BitPdfEngineTestFile.SinglePage(Encoding.Latin1.GetBytes(content)));
        _ = new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render();

        var renderer = new BitPdfHtmlRenderer(document.Pages[0], document.XRef) { EmitCanvasOps = true };
        _ = renderer.Render();

        Assert.IsNotNull(renderer.CanvasOpsJson);
    }

    // ----- The viewer -----

    [TestMethod]
    public void TheViewerShouldReportAnUnsupportedSecurityHandlerThroughOnError()
    {
        string? error = null;
        byte[] bytes = WithEncryptDictionary("<< /Filter /Adobe.PubSec /V 4 /R 4 >>");

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(bytes, "certificate.pdf"));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => error = e));
        });

        component.WaitForAssertion(() => Assert.IsNotNull(error));
        Assert.AreEqual(0, component.Instance.PageCount);
        // No password dialog: a password cannot open this file.
        Assert.AreEqual(0, component.FindAll(".bit-pdv-password").Count);
        var alert = component.Find(".bit-pdv-error[role=alert]");
        StringAssert.Contains(alert.TextContent, error!);
    }

    [TestMethod]
    public void TheViewerShouldReportATruncatedFileThroughOnError()
    {
        string? error = null;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<"), "cut.pdf"));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => error = e));
        });

        component.WaitForAssertion(() => Assert.IsNotNull(error));
        Assert.AreEqual(0, component.Instance.PageCount);
        Assert.AreEqual(1, component.FindAll(".bit-pdv-error").Count);
    }

    [TestMethod]
    public void TheViewerShouldShowEveryPageOfADocumentWithACorruptStream()
    {
        string? error = null;
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 7 0 R >> >> /Contents 5 0 R >>");
        file.Obj(4, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 7 0 R >> >> /Contents 6 0 R >>");
        file.Stream(5, "\u0001\u0002 definitely not zlib", "/Filter /FlateDecode");
        file.Stream(6, BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.ShowText("Still readable")), "/Filter /FlateDecode");
        file.Obj(7, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.XRefTable("/Size 8 /Root 1 0 R", Enumerable.Range(0, 8));

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(file.ToArray(), "corrupt.pdf"));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => error = e));
        });

        component.WaitForPages(2);
        Assert.IsNull(error);
        StringAssert.Contains(component.Instance.ExtractText(), "Still readable");
    }
}
