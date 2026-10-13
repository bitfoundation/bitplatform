using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Cross-reference resolution (<see cref="BitPdfXRef"/>): classic tables with several
/// subsections, PDF 1.5 cross-reference and object streams, incremental updates, hybrid
/// files, and the recovery scan a broken table falls back to.
/// </summary>
[TestClass]
public class BitPdfXRefTests
{
    private const string FontBody = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";
    private const string PageBody = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>";

    private static string PageText(BitPdfDocument document, int index = 0) => document.Pages[index].ExtractText().Trim();

    private static BitPdfEngineTestFile Base(string text, out int[] objects)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, PageBody);
        file.Stream(4, BitPdfEngineTestFile.ShowText(text));
        file.Obj(5, FontBody);
        file.Obj(6, "<< /Title (Original title) >>");
        objects = [0, 1, 2, 3, 4, 5, 6];
        return file;
    }

    /// <summary>An object stream holding <paramref name="bodies"/> (object number, body), in order.</summary>
    private static (string Data, int First) ObjectStream(params (int Num, string Body)[] bodies)
    {
        var header = new StringBuilder();
        var body = new StringBuilder();
        foreach (var (num, text) in bodies)
        {
            header.Append($"{num} {body.Length} ");
            body.Append(text).Append(' ');
        }
        return (header.ToString() + body, header.Length);
    }

    // ----- Classic tables -----

    [TestMethod]
    public void AClassicTableWithSeveralSubsectionsShouldResolveEveryObject()
    {
        var file = Base("Classic", out _);
        file.Obj(10, "(ten)");
        file.Obj(11, "[1 2 3]");
        file.XRefTable("/Size 12 /Root 1 0 R /Info 6 0 R", [0, 1, 2, 3, 4, 5, 6, 10, 11]);
        byte[] bytes = file.ToArray();
        StringAssert.Contains(Encoding.Latin1.GetString(bytes), "xref\n0 7\n");
        StringAssert.Contains(Encoding.Latin1.GetString(bytes), "\n10 2\n");

        var document = BitPdfDocument.Load(bytes);

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual("Classic", PageText(document));
        Assert.AreEqual("ten", ((BitPdfString)document.XRef.Fetch(new BitPdfRef(10, 0))!).AsText());
        Assert.AreEqual(3, ((List<object?>)document.XRef.Fetch(new BitPdfRef(11, 0))!).Count);
        Assert.AreEqual("Original title", document.Metadata.Title);
        Assert.AreEqual("1.7", document.Version);
    }

    [TestMethod]
    public void AFreeEntryShouldFetchAsNullEvenWhenItsObjectIsStillInTheFile()
    {
        var file = Base("Free", out _);
        file.Obj(7, "(deleted but still present)");
        file.XRefTable("/Size 8 /Root 1 0 R", [0, 1, 2, 3, 4, 5, 6], freeEntries: [7]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(7, 0)));
        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
    }

    [TestMethod]
    public void AReferenceToAnObjectNoSectionListsShouldFetchAsNull()
    {
        var file = Base("Missing", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R", objects);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(42, 0)));
    }

    [TestMethod]
    public void AnXRefTableWithCrLfAndSpaceCrEntriesShouldParse()
    {
        // Entries may end in " \r\n", "\r\n" or " \n"; all are 20 bytes with the two-byte EOL forms.
        var file = Base("Line endings", out int[] objects);
        int xref = file.Position;
        file.Raw("xref\r\n0 7\r\n0000000000 65535 f\r\n");
        foreach (int num in objects.Skip(1))
        {
            file.Raw($"{file.Offsets[num]:D10} 00000 n\r\n");
        }
        file.Raw($"trailer\r\n<< /Size 7 /Root 1 0 R >>\r\nstartxref\r\n{xref}\r\n%%EOF\r\n");

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual("Line endings", PageText(document));
    }

    // ----- Cross-reference streams (PDF 1.5) -----

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AnXRefStreamWithAnObjectStreamShouldResolveCompressedObjects(bool compress)
    {
        var file = new BitPdfEngineTestFile("%PDF-1.5\n");
        file.Stream(4, BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.ShowText("From an object stream")), "/Filter /FlateDecode");
        var (data, first) = ObjectStream(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, PageBody),
            (5, FontBody),
            (6, "<< /Title (Compressed info) >>"));
        file.Stream(7, BitPdfEngineTestFile.Zlib(Encoding.Latin1.GetBytes(data)), $"/Type /ObjStm /N 5 /First {first} /Filter /FlateDecode");
        file.XRefStream(8, "/Root 1 0 R /Info 6 0 R",
        [
            (0, 0, 0, 65535),
            (1, 2, 7, 0),
            (2, 2, 7, 1),
            (3, 2, 7, 2),
            (4, 1, file.Offsets[4], 0),
            (5, 2, 7, 3),
            (6, 2, 7, 4),
            (7, 1, file.Offsets[7], 0),
        ], compress);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual("1.5", document.Version);
        Assert.AreEqual(1, document.PageCount);
        Assert.AreEqual("From an object stream", PageText(document));
        Assert.AreEqual("Compressed info", document.Metadata.Title);
        Assert.IsTrue(BitPdfPrimitives.IsName(((BitPdfDict)document.XRef.Fetch(new BitPdfRef(5, 0))!).Get("BaseFont"), "Helvetica"));
    }

    [TestMethod]
    public void AnXRefStreamWithNonContiguousIndexSubsectionsShouldResolveEachOne()
    {
        var file = new BitPdfEngineTestFile("%PDF-1.5\n");
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, PageBody);
        file.Stream(4, BitPdfEngineTestFile.ShowText("Sparse index"));
        file.Obj(5, FontBody);
        file.Obj(20, "(twenty)");
        file.Obj(21, "(twenty-one)");
        file.XRefStream(30, "/Root 1 0 R",
            new[] { 1, 2, 3, 4, 5, 20, 21 }.Select(n => (n, 1, file.Offsets[n], 0)).Prepend((0, 0, 0, 65535)), compress: true);
        StringAssert.Contains(Encoding.Latin1.GetString(file.ToArray()), "/Index [0 6 20 2 30 1]");

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual("Sparse index", PageText(document));
        Assert.AreEqual("twenty-one", ((BitPdfString)document.XRef.Fetch(new BitPdfRef(21, 0))!).AsText());
    }

    // ----- Incremental updates -----

    [TestMethod]
    public void AnIncrementalUpdateShouldReplaceTheObjectsItRedefines()
    {
        var file = Base("Original", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", objects);
        var original = BitPdfDocument.Load(file.ToArray());
        Assert.AreEqual("Original", PageText(original));

        file.Stream(4, BitPdfEngineTestFile.ShowText("Updated"));
        file.Obj(6, "<< /Title (Updated title) >>");
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", [4, 6]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual(1, document.PageCount);
        Assert.AreEqual("Updated", PageText(document));
        Assert.AreEqual("Updated title", document.Metadata.Title);
    }

    [TestMethod]
    public void AnIncrementalUpdateShouldDeleteAnObjectItFrees()
    {
        var file = Base("Delete", out int[] objects);
        file.Obj(7, "(soon gone)");
        file.XRefTable("/Size 8 /Root 1 0 R", [.. objects, 7]);
        Assert.IsNotNull(BitPdfDocument.Load(file.ToArray()).XRef.Fetch(new BitPdfRef(7, 0)));

        file.XRefTable("/Size 8 /Root 1 0 R", [], freeEntries: [7]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(7, 0)));
        Assert.AreEqual("Delete", PageText(document));
    }

    [TestMethod]
    public void ThreeChainedUpdatesShouldResolveEachObjectFromItsNewestSection()
    {
        var file = Base("v1", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", objects);
        file.Stream(4, BitPdfEngineTestFile.ShowText("v2"));
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", [4]);
        file.Obj(6, "<< /Title (Title from v3) >>");
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", [6]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual("v2", PageText(document));
        Assert.AreEqual("Title from v3", document.Metadata.Title);
    }

    [TestMethod]
    public void AnXRefStreamUpdateOverAClassicTableShouldWin()
    {
        var file = Base("Classic base", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", objects);
        file.Stream(4, BitPdfEngineTestFile.ShowText("Stream update"));
        file.XRefStream(9, "/Root 1 0 R /Info 6 0 R", [(4, 1, file.Offsets[4], 0)]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
        Assert.AreEqual("Stream update", PageText(document));
        Assert.AreEqual("Original title", document.Metadata.Title);
    }

    [TestMethod]
    public void AnUpdateThatAddsAPageShouldExtendThePageTree()
    {
        var file = Base("First", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R", objects);
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R 7 0 R] /Count 2 >>");
        file.Obj(7, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 100] /Resources << /Font << /F1 5 0 R >> >> /Contents 8 0 R >>");
        file.Stream(8, BitPdfEngineTestFile.ShowText("Second"));
        file.XRefTable("/Size 9 /Root 1 0 R", [2, 7, 8]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(2, document.PageCount);
        Assert.AreEqual("Second", PageText(document, 1));
        Assert.AreEqual(300, document.Pages[1].Width);
    }

    [TestMethod]
    public void AHybridFileShouldReadCompressedObjectsFromItsXRefStm()
    {
        // A hybrid-reference file (PDF 32000-1 §7.5.8.4): the classic table lists object 6 as
        // free for a pre-1.5 reader; the /XRefStm stream gives its real home in object stream 7.
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, PageBody);
        file.Stream(4, BitPdfEngineTestFile.ShowText("Hybrid"));
        file.Obj(5, FontBody);
        var (data, first) = ObjectStream((6, "<< /Title (Only in the object stream) >>"));
        file.Stream(7, data, $"/Type /ObjStm /N 1 /First {first}");
        int xrefStm = file.XRefStream(8, "", [(6, 2, 7, 0)], withPrev: false);
        file.XRefTable($"/Size 9 /Root 1 0 R /Info 6 0 R /XRefStm {xrefStm}", [0, 1, 2, 3, 4, 5, 7, 8], freeEntries: [6], withPrev: false);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual("Hybrid", PageText(document));
        Assert.AreEqual("Only in the object stream", document.Metadata.Title);
    }

    [TestMethod]
    public void APrevChainThatLoopsShouldStillLoad()
    {
        // The first section's /Prev points forward at the update, whose /Prev points back at
        // the first: A -> B -> A. The placeholder is patched once B's offset is known.
        const string placeholder = "9999999999";
        var file = Base("Loop", out int[] objects);
        file.XRefTable($"/Size 7 /Root 1 0 R /Prev {placeholder}", objects, withPrev: false);
        file.Stream(4, BitPdfEngineTestFile.ShowText("Loop update"));
        int second = file.XRefTable("/Size 7 /Root 1 0 R", [4]);
        string text = Encoding.Latin1.GetString(file.ToArray()).Replace(placeholder, $"{second:D10}");

        var document = BitPdfDocument.Load(Encoding.Latin1.GetBytes(text));

        Assert.AreEqual("Loop update", PageText(document));
        Assert.AreEqual(0, document.Warnings.Count, string.Join("; ", document.Warnings));
    }

    // ----- Recovery -----

    [TestMethod]
    public void AStartXRefPastTheEndOfTheFileShouldFallBackToReconstruction()
    {
        var file = Base("Recovered", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", objects);
        string text = Encoding.Latin1.GetString(file.ToArray());
        int at = text.LastIndexOf("startxref\n", StringComparison.Ordinal) + "startxref\n".Length;
        byte[] broken = Encoding.Latin1.GetBytes(text[..at] + "999999" + text[text.IndexOf('\n', at)..]);

        var document = BitPdfDocument.Load(broken);

        Assert.IsTrue(document.Warnings.Count > 0);
        Assert.AreEqual("Recovered", PageText(document));
        Assert.AreEqual("Original title", document.Metadata.Title);
    }

    [TestMethod]
    public void ShiftedObjectOffsetsShouldBeCorrectedByAScan()
    {
        var file = Base("Shifted", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R /Info 6 0 R", objects);
        byte[] bytes = file.ToArray();
        // Insert a comment line after the header: every recorded object offset (and the
        // startxref value) is now 12 bytes short.
        byte[] comment = Encoding.ASCII.GetBytes("%inserted!!\n");
        int headerEnd = Array.IndexOf(bytes, (byte)'\n') + 1;
        byte[] shifted = [.. bytes[..headerEnd], .. comment, .. bytes[headerEnd..]];

        var document = BitPdfDocument.Load(shifted);

        Assert.IsTrue(document.Warnings.Count > 0);
        Assert.AreEqual(1, document.PageCount);
        Assert.AreEqual("Shifted", PageText(document));
    }

    [TestMethod]
    public void AFileWithNoXRefAndNoTrailerShouldFindItsCatalogByScanning()
    {
        var file = Base("No trailer", out _);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.IsTrue(document.Warnings.Any(w => w.Contains("catalog", StringComparison.OrdinalIgnoreCase)), string.Join("; ", document.Warnings));
        Assert.AreEqual("No trailer", PageText(document));
    }

    [TestMethod]
    public void ReconstructionShouldPreferTheLaterDefinitionOfAnObject()
    {
        var file = Base("Before", out int[] objects);
        file.XRefTable("/Size 7 /Root 1 0 R", objects);
        file.Stream(4, BitPdfEngineTestFile.ShowText("After"));
        file.XRefTable("/Size 7 /Root 1 0 R", [4]);
        // Wreck the newest startxref so the whole file has to be scanned.
        string text = Encoding.Latin1.GetString(file.ToArray());
        int at = text.LastIndexOf("startxref\n", StringComparison.Ordinal) + "startxref\n".Length;
        byte[] broken = Encoding.Latin1.GetBytes(text[..at] + "0" + text[text.IndexOf('\n', at)..]);

        var document = BitPdfDocument.Load(broken);

        Assert.AreEqual("After", PageText(document));
    }

    [TestMethod]
    public void AnObjectStreamShouldBeIndexedByTheRecoveryScan()
    {
        var file = new BitPdfEngineTestFile("%PDF-1.5\n");
        file.Stream(4, BitPdfEngineTestFile.ShowText("Scanned object stream"));
        var (data, first) = ObjectStream(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, PageBody),
            (5, FontBody));
        file.Stream(7, BitPdfEngineTestFile.Zlib(Encoding.Latin1.GetBytes(data)), $"/Type /ObjStm /N 4 /First {first} /Filter /FlateDecode");
        file.Raw("trailer\n<< /Size 8 /Root 1 0 R >>\nstartxref\n12345\n%%EOF\n");

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.IsTrue(document.Warnings.Count > 0);
        Assert.AreEqual("Scanned object stream", PageText(document));
    }

    // When an entry's offset holds another object and the rebuild scan finds no such object,
    // FetchUncompressed used to parse at the stale offset anyway and hand back the OTHER
    // object under this reference; it now answers null.
    [TestMethod]
    public void AnEntryPointingAtAnotherObjectShouldNotReturnThatObject()
    {
        var file = Base("Wrong target", out int[] objects);
        // Object 9 does not exist; its entry points at object 5 (the font dictionary).
        file.Offsets[9] = file.Offsets[5];
        file.XRefTable("/Size 10 /Root 1 0 R", [.. objects, 9]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(9, 0)));
    }

    // Only a hybrid file's /XRefStm may fill in an entry a newer section left free. Any older
    // xref stream used to as well, so an object an update deleted through an xref stream came
    // back from the stream it was deleted from.
    [TestMethod]
    public void AnObjectDeletedByAnXRefStreamUpdateShouldStayDeleted()
    {
        var file = new BitPdfEngineTestFile("%PDF-1.5\n");
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, PageBody);
        file.Stream(4, BitPdfEngineTestFile.ShowText("Base"));
        file.Obj(5, FontBody);
        file.Obj(6, "(deleted later)");
        file.XRefStream(7, "/Root 1 0 R", new[] { 1, 2, 3, 4, 5, 6 }.Select(n => (n, 1, file.Offsets[n], 0)).Prepend((0, 0, 0, 65535)));
        file.XRefStream(8, "/Root 1 0 R", [(6, 0, 0, 1)]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual("Base", PageText(document));
        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(6, 0)));
    }

    // The /XRefStm of a hybrid section fills in the entries ITS OWN classic table left free. An
    // older hybrid section reached through /Prev used to fill in any free entry, so an object the
    // newest table had deleted came back from the older section's stream.
    [TestMethod]
    public void AnObjectDeletedByAnUpdateShouldStayDeletedPastAnOlderHybridSection()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, PageBody);
        file.Stream(4, BitPdfEngineTestFile.ShowText("Hybrid"));
        file.Obj(5, FontBody);
        var (data, first) = ObjectStream((6, "<< /Title (Deleted by the update) >>"));
        file.Stream(7, data, $"/Type /ObjStm /N 1 /First {first}");
        int xrefStm = file.XRefStream(8, "", [(6, 2, 7, 0)], withPrev: false);
        file.XRefTable($"/Size 9 /Root 1 0 R /Info 6 0 R /XRefStm {xrefStm}", [0, 1, 2, 3, 4, 5, 7, 8], freeEntries: [6], withPrev: false);
        // The update deletes object 6; its /Prev leads to the hybrid section above.
        file.XRefTable("/Size 9 /Root 1 0 R", [], freeEntries: [6]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual("Hybrid", PageText(document));
        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(6, 0)));
        Assert.IsNull(document.Metadata.Title);
    }
}
