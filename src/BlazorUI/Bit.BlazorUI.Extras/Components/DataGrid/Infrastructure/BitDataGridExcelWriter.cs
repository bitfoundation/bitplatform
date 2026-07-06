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
            AddEntry(zip, "xl/worksheets/sheet1.xml", BuildSheet(rows, columns));
        }
        return stream.ToArray();
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildSheet<TItem>(IReadOnlyList<TItem> rows, IReadOnlyList<BitDataGridColumn<TItem>> columns)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        sb.Append("<row>");
        foreach (var column in columns)
        {
            AppendInlineString(sb, column.DisplayTitle);
        }
        sb.Append("</row>");

        foreach (var item in rows)
        {
            sb.Append("<row>");
            foreach (var column in columns)
            {
                AppendCell(sb, column, item);
            }
            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static void AppendCell<TItem>(StringBuilder sb, BitDataGridColumn<TItem> column, TItem item)
    {
        var value = column.GetValue(item);
        switch (value)
        {
            case null:
                sb.Append("<c/>");
                break;
            case bool b:
                sb.Append("<c t=\"b\"><v>").Append(b ? '1' : '0').Append("</v></c>");
                break;
            // Native numeric cells keep their real value so spreadsheet math works on the export;
            // a column Format (e.g. "C2") is presentation-only and intentionally not applied here.
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                sb.Append("<c><v>")
                  .Append(Convert.ToString(value, CultureInfo.InvariantCulture))
                  .Append("</v></c>");
                break;
            default:
                AppendInlineString(sb, column.GetFormattedValue(item));
                break;
        }
    }

    private static void AppendInlineString(StringBuilder sb, string text)
    {
        sb.Append("<c t=\"inlineStr\"><is><t xml:space=\"preserve\">");
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                // Strip control characters that are invalid in XML 1.0 rather than emitting a broken file.
                case < ' ' when ch is not ('\t' or '\n' or '\r'): break;
                default: sb.Append(ch); break;
            }
        }
        sb.Append("</t></is></c>");
    }
}
