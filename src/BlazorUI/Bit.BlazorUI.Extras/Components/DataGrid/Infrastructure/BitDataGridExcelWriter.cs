using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Minimal, dependency-free XLSX writer for the grid's Excel export. An .xlsx file is a ZIP package
/// of SpreadsheetML parts; this emits the smallest valid subset (workbook, one worksheet, styles,
/// package relationships) using inline strings, so no shared-string table is needed.
/// Numeric and boolean values are written as native cell types so spreadsheet formulas work on them;
/// everything else is written as the column's formatted display text.
/// The grid's rendered layout is mirrored with native workbook features: the header row is bold and
/// frozen (with the grid's leading frozen columns becoming the vertical freeze pane), ColSpan cells
/// become merged regions, and the grid's pixel column widths carry over.
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
        <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
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
        <bookViews><workbookView/></bookViews>
        <sheets><sheet name="Export" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private const string WorkbookRelsXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
        <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    // Minimal style sheet: style 0 is the default cell, style 1 the bold header. The two fills and
    // the empty border are mandatory filler (SpreadsheetML requires fill 0 = none and fill 1 = gray125).
    private const string StylesXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
        <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
        <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
        <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
        <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
        <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
        <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    /// <param name="rows">The export rows, already sorted/filtered.</param>
    /// <param name="columns">The exported (visible, field-bound) columns in display order.</param>
    /// <param name="frozenColumnCount">How many leading columns to freeze (Excel panes can only freeze
    /// a leading run, so this is the grid's count of consecutive Frozen columns from the start;
    /// FrozenEnd has no workbook equivalent).</param>
    /// <param name="columnPixelWidths">Per-column widths in px (same order as <paramref name="columns"/>),
    /// carried over as Excel column widths when provided.</param>
    /// <param name="style">The grid's sampled visual theme; when provided the workbook's style sheet
    /// carries the on-screen colors/fonts (header fill, striped rows, borders) instead of the plain
    /// bold-header default.</param>
    public static byte[] Write<TItem>(
        IReadOnlyList<TItem> rows,
        IReadOnlyList<BitDataGridColumn<TItem>> columns,
        int frozenColumnCount = 0,
        IReadOnlyList<double>? columnPixelWidths = null,
        BitDataGridExcelStyle? style = null)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml);
            AddEntry(zip, "_rels/.rels", PackageRelsXml);
            AddEntry(zip, "xl/workbook.xml", WorkbookXml);
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
            AddEntry(zip, "xl/styles.xml", style is null ? StylesXml : BuildStylesXml(style));

            // The worksheet is the only part that scales with the export, so stream it straight into
            // the (deflating) zip entry instead of assembling the whole XML in memory first - the
            // uncompressed sheet of a large export would otherwise dwarf the finished package.
            var sheet = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
            using var writer = new StreamWriter(sheet.Open(), new UTF8Encoding(false));
            WriteSheet(writer, rows, columns, frozenColumnCount, columnPixelWidths, styled: style is not null);
        }
        return stream.ToArray();
    }

    /// <summary>Builds the style sheet for a styled export. The cell-format (cellXfs) contract with
    /// the sheet writer: index 0 = data row (the implicit default for cells with no <c>s</c>
    /// attribute), 1 = header, 2 = alternating (striped) data row.</summary>
    private static string BuildStylesXml(BitDataGridExcelStyle style)
    {
        // "#rrggbb" (CSS) -> "FFRRGGBB" (SpreadsheetML ARGB); anything else falls back to default.
        static string? Argb(string? hex)
            => hex is { Length: 7 } && hex[0] == '#' ? "FF" + hex[1..].ToUpperInvariant() : null;

        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

        static void AppendFont(StringBuilder sb, bool bold, bool italic, string? argb)
        {
            sb.Append("<font>");
            if (bold) sb.Append("<b/>");
            if (italic) sb.Append("<i/>");
            sb.Append("<sz val=\"11\"/>");
            if (argb is not null) sb.Append($"<color rgb=\"{argb}\"/>");
            sb.Append("<name val=\"Calibri\"/></font>");
        }

        sb.Append("<fonts count=\"2\">");
        AppendFont(sb, style.RowBold, style.RowItalic, Argb(style.RowForeground));
        AppendFont(sb, style.HeaderBold, style.HeaderItalic, Argb(style.HeaderForeground));
        sb.Append("</fonts>");

        // Fills 0 (none) and 1 (gray125) are mandatory SpreadsheetML filler; real fills follow.
        var fills = new List<string> { "<fill><patternFill patternType=\"none\"/></fill>", "<fill><patternFill patternType=\"gray125\"/></fill>" };
        int AddFill(string? argb)
        {
            if (argb is null) return 0;
            fills.Add($"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"{argb}\"/><bgColor rgb=\"{argb}\"/></patternFill></fill>");
            return fills.Count - 1;
        }
        var rowFill = AddFill(Argb(style.RowBackground));
        var headerFill = AddFill(Argb(style.HeaderBackground));
        var stripeFill = AddFill(Argb(style.StripeBackground));
        if (stripeFill == 0) stripeFill = rowFill;
        sb.Append($"<fills count=\"{fills.Count}\">");
        foreach (var fill in fills) sb.Append(fill);
        sb.Append("</fills>");

        var borderId = 0;
        sb.Append("<borders count=\"").Append(Argb(style.BorderColor) is null ? 1 : 2).Append("\">");
        sb.Append("<border><left/><right/><top/><bottom/><diagonal/></border>");
        if (Argb(style.BorderColor) is { } borderColor)
        {
            borderId = 1;
            var edge = $" style=\"thin\"><color rgb=\"{borderColor}\"/></";
            sb.Append(style.VerticalBorders
                ? $"<border><left{edge}left><right{edge}right><top{edge}top><bottom{edge}bottom><diagonal/></border>"
                : $"<border><left/><right/><top/><bottom{edge}bottom><diagonal/></border>");
        }
        sb.Append("</borders>");

        sb.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");

        void AppendXf(int fontId, int fillId)
            => sb.Append($"<xf numFmtId=\"0\" fontId=\"{fontId}\" fillId=\"{fillId}\" borderId=\"{borderId}\" xfId=\"0\" applyFont=\"1\"")
                 .Append(fillId > 0 ? " applyFill=\"1\"" : "")
                 .Append(borderId > 0 ? " applyBorder=\"1\"" : "")
                 .Append("/>");

        sb.Append("<cellXfs count=\"3\">");
        AppendXf(0, rowFill);      // 0: data row (implicit default)
        AppendXf(1, headerFill);   // 1: header
        AppendXf(0, stripeFill);   // 2: striped data row
        sb.Append("</cellXfs>");

        sb.Append("<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>");
        sb.Append("</styleSheet>");
        return sb.ToString();
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static void WriteSheet<TItem>(
        TextWriter writer,
        IReadOnlyList<TItem> rows,
        IReadOnlyList<BitDataGridColumn<TItem>> columns,
        int frozenColumnCount,
        IReadOnlyList<double>? columnPixelWidths,
        bool styled)
    {
        writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

        WriteFreezePane(writer, Math.Clamp(frozenColumnCount, 0, columns.Count));
        WriteColumnWidths(writer, columns.Count, columnPixelWidths);

        writer.Write("<sheetData>");

        writer.Write("<row>");
        foreach (var column in columns)
        {
            WriteInlineString(writer, column.DisplayTitle, styleIndex: 1);
        }
        writer.Write("</row>");

        // Merged regions mirroring the grid's per-row ColSpan cells ("A3:B3" refs), collected while
        // the rows stream out and emitted after sheetData (the order the worksheet schema requires).
        List<string>? merges = null;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var item = rows[rowIndex];
            // Cell format 0 (the implicit default) is the data-row style; in a styled export the
            // alternating rows use format 2 (the stripe fill), matching the grid's nth-child(even)
            // striping (sheet data row 2 = data index 1).
            var cellStyle = styled && rowIndex % 2 == 1 ? 2 : 0;
            writer.Write("<row>");
            for (var colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                WriteCell(writer, columns[colIndex], item, cellStyle);

                // Mirror the rendered layout: a spanning cell covers its following neighbours, whose
                // own values/spans are skipped (a merged region keeps only its top-left cell's value).
                // Spans resolve within the exported columns, clamped like the grid's ResolveColSpan.
                var span = Math.Clamp(columns[colIndex].ColSpan?.Invoke(item) ?? 1, 1, columns.Count - colIndex);
                if (span > 1)
                {
                    (merges ??= []).Add($"{CellRef(colIndex, rowIndex + 2)}:{CellRef(colIndex + span - 1, rowIndex + 2)}");
                    for (var covered = 1; covered < span; covered++)
                    {
                        writer.Write(cellStyle > 0 ? $"<c s=\"{cellStyle}\"/>" : "<c/>");
                    }
                    colIndex += span - 1;
                }
            }
            writer.Write("</row>");
        }

        writer.Write("</sheetData>");

        if (merges is not null)
        {
            writer.Write($"<mergeCells count=\"{merges.Count}\">");
            foreach (var merge in merges)
            {
                writer.Write($"<mergeCell ref=\"{merge}\"/>");
            }
            writer.Write("</mergeCells>");
        }

        writer.Write("</worksheet>");
    }

    /// <summary>Freezes the header row plus the leading frozen columns, matching the grid where the
    /// header is always sticky and Frozen columns pin to the start edge.</summary>
    private static void WriteFreezePane(TextWriter writer, int frozenColumnCount)
    {
        var topLeft = CellRef(frozenColumnCount, 2);
        var xSplit = frozenColumnCount > 0 ? $" xSplit=\"{frozenColumnCount}\"" : "";
        var activePane = frozenColumnCount > 0 ? "bottomRight" : "bottomLeft";
        writer.Write("<sheetViews><sheetView workbookViewId=\"0\">");
        writer.Write($"<pane{xSplit} ySplit=\"1\" topLeftCell=\"{topLeft}\" activePane=\"{activePane}\" state=\"frozen\"/>");
        writer.Write("</sheetView></sheetViews>");
    }

    private static void WriteColumnWidths(TextWriter writer, int columnCount, IReadOnlyList<double>? pixelWidths)
    {
        if (pixelWidths is null || pixelWidths.Count == 0) return;

        writer.Write("<cols>");
        for (var i = 0; i < Math.Min(columnCount, pixelWidths.Count); i++)
        {
            // Excel column widths are measured in default-font digit widths (~7px each at 100%,
            // plus 5px of cell padding), so convert the grid's px width with the usual (px-5)/7.
            var width = Math.Max(1, (pixelWidths[i] - 5) / 7);
            writer.Write($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{width.ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        }
        writer.Write("</cols>");
    }

    /// <summary>An A1-style cell reference from a 0-based column index and a 1-based sheet row.</summary>
    private static string CellRef(int columnIndex, int row)
    {
        var letters = string.Empty;
        for (var i = columnIndex; i >= 0; i = i / 26 - 1)
        {
            letters = (char)('A' + i % 26) + letters;
        }
        return letters + row.ToString(CultureInfo.InvariantCulture);
    }

    private static void WriteCell<TItem>(TextWriter writer, BitDataGridColumn<TItem> column, TItem item, int styleIndex)
    {
        var s = styleIndex > 0 ? $" s=\"{styleIndex}\"" : "";
        var value = column.GetValue(item);
        switch (value)
        {
            case null:
                writer.Write($"<c{s}/>");
                break;
            case bool b:
                writer.Write($"<c t=\"b\"{s}><v>");
                writer.Write(b ? '1' : '0');
                writer.Write("</v></c>");
                break;
            // A non-finite float has no valid numeric-cell representation in SpreadsheetML (a <v> of
            // "NaN"/"Infinity" makes the file unreadable), so write its display text as an inline
            // string instead of a numeric cell.
            case float f when !float.IsFinite(f):
            case double d when !double.IsFinite(d):
                WriteInlineString(writer, column.GetFormattedValue(item), styleIndex);
                break;
            // Native numeric cells keep their real value so spreadsheet math works on the export;
            // a column Format (e.g. "C2") is presentation-only and intentionally not applied here.
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                writer.Write($"<c{s}><v>");
                writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture));
                writer.Write("</v></c>");
                break;
            default:
                WriteInlineString(writer, column.GetFormattedValue(item), styleIndex);
                break;
        }
    }

    private static void WriteInlineString(TextWriter writer, string text, int styleIndex = 0)
    {
        writer.Write(styleIndex > 0
            ? $"<c t=\"inlineStr\" s=\"{styleIndex}\"><is><t xml:space=\"preserve\">"
            : "<c t=\"inlineStr\"><is><t xml:space=\"preserve\">");
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
