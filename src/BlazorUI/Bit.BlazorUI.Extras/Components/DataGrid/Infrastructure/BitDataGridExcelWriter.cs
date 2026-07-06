using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Minimal, dependency-free XLSX writer for the grid's Excel export. An .xlsx file is a ZIP package
/// of SpreadsheetML parts; this emits the smallest valid subset (workbook, one worksheet, package
/// relationships) using inline strings, so no shared-string table or style sheet is needed.
/// Numeric and boolean values are written as native cell types so spreadsheet formulas work on them;
/// everything else is written as the column's formatted display text.
/// </summary>
internal static class BitDataGridExcelWriter
{
    private const string ContentTypesXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
        <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
        <Default Extension="xml" ContentType="application/xml"/>
        <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
        <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
        </Types>
        """;

    private const string PackageRelsXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string WorkbookXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
        <sheets><sheet name="Export" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private const string WorkbookRelsXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
        </Relationships>
        """;

    public static byte[] Write<TItem>(IReadOnlyList<TItem> rows, IReadOnlyList<BitDataGridColumn<TItem>> columns)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml);
            AddEntry(zip, "_rels/.rels", PackageRelsXml);
            AddEntry(zip, "xl/workbook.xml", WorkbookXml);
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);

            // The worksheet is the only part that scales with the export, so stream it straight into
            // the (deflating) zip entry instead of assembling the whole XML in memory first — the
            // uncompressed sheet of a large export would otherwise dwarf the finished package.
            var sheet = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
            using var writer = new StreamWriter(sheet.Open(), new UTF8Encoding(false));
            WriteSheet(writer, rows, columns);
        }
        return stream.ToArray();
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static void WriteSheet<TItem>(TextWriter writer, IReadOnlyList<TItem> rows, IReadOnlyList<BitDataGridColumn<TItem>> columns)
    {
        writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        writer.Write("<row>");
        foreach (var column in columns)
        {
            WriteInlineString(writer, column.DisplayTitle);
        }
        writer.Write("</row>");

        foreach (var item in rows)
        {
            writer.Write("<row>");
            foreach (var column in columns)
            {
                WriteCell(writer, column, item);
            }
            writer.Write("</row>");
        }

        writer.Write("</sheetData></worksheet>");
    }

    private static void WriteCell<TItem>(TextWriter writer, BitDataGridColumn<TItem> column, TItem item)
    {
        var value = column.GetValue(item);
        switch (value)
        {
            case null:
                writer.Write("<c/>");
                break;
            case bool b:
                writer.Write("<c t=\"b\"><v>");
                writer.Write(b ? '1' : '0');
                writer.Write("</v></c>");
                break;
            // A non-finite float has no valid numeric-cell representation in SpreadsheetML (a <v> of
            // "NaN"/"Infinity" makes the file unreadable), so write its display text as an inline
            // string instead of a numeric cell.
            case float f when !float.IsFinite(f):
            case double d when !double.IsFinite(d):
                WriteInlineString(writer, column.GetFormattedValue(item));
                break;
            // Native numeric cells keep their real value so spreadsheet math works on the export;
            // a column Format (e.g. "C2") is presentation-only and intentionally not applied here.
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                writer.Write("<c><v>");
                writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture));
                writer.Write("</v></c>");
                break;
            default:
                WriteInlineString(writer, column.GetFormattedValue(item));
                break;
        }
    }

    private static void WriteInlineString(TextWriter writer, string text)
    {
        writer.Write("<c t=\"inlineStr\"><is><t xml:space=\"preserve\">");
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '&': writer.Write("&amp;"); break;
                case '<': writer.Write("&lt;"); break;
                case '>': writer.Write("&gt;"); break;
                // Strip control characters that are invalid in XML 1.0 rather than emitting a broken file.
                case < ' ' when ch is not ('\t' or '\n' or '\r'): break;
                default: writer.Write(ch); break;
            }
        }
        writer.Write("</t></is></c>");
    }
}
