using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// The commands that edit a GFM table the caret is already sitting in: rows, columns and
/// column alignment. Each one reads the table around the caret, rewrites it, and lays the
/// result back out with every column padded to its widest cell, so the markdown source
/// stays readable in the textarea rather than degrading into ragged pipes.
/// </summary>
public static partial class BitMarkdownEditorCommands
{
    // A cell is at least this wide, because the delimiter row needs room for "---".
    private const int MinCellWidth = 3;

    /// <summary>
    /// The table the caret sits in, parsed into a grid: the header row, the body rows and the
    /// per column alignment, plus where the caret is inside it.
    /// </summary>
    private sealed class MarkdownTable
    {
        public int Start;
        public int End;
        public List<List<string>> Rows = [];
        public List<char> Aligns = [];
        public int Row;
        public int Column;

        /// <summary>
        /// Whether the cell the edit landed on is selected rather than merely reached. Walking
        /// from cell to cell selects what is there, so typing replaces it the way Tab through a
        /// grid is expected to behave; an edit that made a cell does not.
        /// </summary>
        public bool SelectCell;

        public int Columns => Rows.Count > 0 ? Rows[0].Count : 0;
    }

    private static BitMarkdownEditorEditResult TableEdit(string text, int start, int end, Func<MarkdownTable, bool> edit)
    {
        // A selection spanning several rows still edits the one the caret started in: every
        // one of these commands acts on a single row or column by definition.
        MarkdownTable? table = ReadTable(text, start);

        // No table under the caret, or an edit the table refused (the header row, the last
        // column): the document is left exactly as it was.
        if (table is null || edit(table) is false) return BitMarkdownEditorEditResult.NotHandled(text, start, end);

        string rendered = Render(table, out int caret, out int caretEnd);

        return new BitMarkdownEditorEditResult(true,
                                               text[..table.Start] + rendered + text[table.End..],
                                               table.Start + caret,
                                               table.Start + caretEnd);
    }

    // ---- reading ------------------------------------------------------------

    /// <summary>
    /// Reads the table around <paramref name="caret"/>, or null when the caret is not in one.
    /// A run of lines counts as a table only with a delimiter row right below its header, which
    /// is what tells a table apart from any other text that happens to contain a pipe.
    /// </summary>
    private static MarkdownTable? ReadTable(string text, int caret)
    {
        if (text.Length == 0) return null;

        int caretLineStart = LineStartIndex(text, caret);
        if (IsTableRow(text[caretLineStart..LineEndIndex(text, caret)]) is false) return null;

        int start = caretLineStart;
        while (start > 0)
        {
            int previousStart = LineStartIndex(text, start - 1);
            if (IsTableRow(text[previousStart..(start - 1)]) is false) break;
            start = previousStart;
        }

        int end = LineEndIndex(text, caret);
        while (end < text.Length)
        {
            int nextStart = end + 1;
            int nextEnd = LineEndIndex(text, nextStart);
            if (IsTableRow(text[nextStart..nextEnd]) is false) break;
            end = nextEnd;
        }

        string[] lines = text[start..end].Split('\n');
        if (lines.Length < 2 || IsDelimiterRow(lines[1]) is false) return null;

        var table = new MarkdownTable { Start = start, End = end };

        List<string> header = SplitRow(lines[0]);
        table.Rows.Add(header);
        table.Aligns = [.. SplitRow(lines[1]).Select(ReadAlignment)];

        for (int i = 2; i < lines.Length; i++)
        {
            table.Rows.Add(SplitRow(lines[i]));
        }

        // Every row is squared off to the header, so an edit never has to ask how long a row is.
        int columns = Math.Max(1, header.Count);
        foreach (var row in table.Rows)
        {
            while (row.Count < columns) row.Add(string.Empty);
            if (row.Count > columns) row.RemoveRange(columns, row.Count - columns);
        }
        while (table.Aligns.Count < columns) table.Aligns.Add('n');
        if (table.Aligns.Count > columns) table.Aligns.RemoveRange(columns, table.Aligns.Count - columns);

        int caretLine = 0;
        for (int i = start; i < caretLineStart; i++)
        {
            if (text[i] == '\n') caretLine++;
        }

        // The delimiter row is not a row anybody edits, so a caret on it acts on the header.
        table.Row = caretLine <= 1 ? 0 : caretLine - 1;
        table.Column = Math.Clamp(CellIndex(lines[caretLine], caret - caretLineStart), 0, columns - 1);

        return table;
    }

    private static bool IsTableRow(string line) => line.Contains('|', StringComparison.Ordinal) && line.Trim().Length > 0;

    private static bool IsDelimiterRow(string line)
    {
        List<string> cells = SplitRow(line);
        if (cells.Count == 0) return false;

        foreach (string cell in cells)
        {
            string c = cell.Trim();
            if (c.Length == 0) return false;

            int i = 0;
            if (c[i] == ':') i++;

            int dashes = 0;
            while (i < c.Length && c[i] == '-') { i++; dashes++; }
            if (dashes == 0) return false;

            if (i < c.Length && c[i] == ':') i++;
            if (i != c.Length) return false;
        }

        return true;
    }

    /// <summary>
    /// Splits a row on its unescaped pipes, dropping the optional leading and trailing one.
    /// </summary>
    private static List<string> SplitRow(string line)
    {
        string row = line.Trim();

        int from = row.StartsWith('|') ? 1 : 0;
        int to = row.Length;
        if (to > from && row[to - 1] == '|' && (to < 2 || row[to - 2] != '\\')) to--;

        List<string> cells = [];
        var cell = new StringBuilder();
        for (int i = from; i < to; i++)
        {
            if (row[i] == '\\' && i + 1 < to)
            {
                cell.Append(row[i]).Append(row[i + 1]);
                i++;
                continue;
            }

            if (row[i] == '|')
            {
                cells.Add(cell.ToString().Trim());
                cell.Clear();
                continue;
            }

            cell.Append(row[i]);
        }
        cells.Add(cell.ToString().Trim());

        return cells;
    }

    /// <summary>
    /// Which cell of <paramref name="line"/> the offset <paramref name="offset"/> falls in.
    /// </summary>
    private static int CellIndex(string line, int offset)
    {
        offset = Math.Clamp(offset, 0, line.Length);

        int leading = 0;
        while (leading < line.Length && char.IsWhiteSpace(line[leading])) leading++;

        int index = line.Length > leading && line[leading] == '|' ? -1 : 0;
        for (int i = 0; i < offset; i++)
        {
            if (line[i] == '\\') { i++; continue; }
            if (line[i] == '|') index++;
        }

        return Math.Max(0, index);
    }

    private static char ReadAlignment(string cell)
    {
        string c = cell.Trim();
        bool left = c.StartsWith(':');
        bool right = c.EndsWith(':') && c.Length > 1;

        return (left, right) switch
        {
            (true, true) => 'c',
            (true, false) => 'l',
            (false, true) => 'r',
            _ => 'n'
        };
    }

    // ---- writing ------------------------------------------------------------

    /// <summary>
    /// Lays the grid back out with every column padded to its widest cell, and reports where
    /// the caret goes: the start of the content of the cell the edit targeted.
    /// </summary>
    private static string Render(MarkdownTable table, out int caret, out int caretEnd)
    {
        int columns = table.Columns;
        int[] widths = new int[columns];

        for (int c = 0; c < columns; c++)
        {
            int width = MinCellWidth;
            foreach (var row in table.Rows)
            {
                width = Math.Max(width, row[c].Length);
            }
            widths[c] = width;
        }

        table.Row = Math.Clamp(table.Row, 0, table.Rows.Count - 1);
        table.Column = Math.Clamp(table.Column, 0, columns - 1);

        // A plain loop, not a LINQ lambda: the net8.0 trim analyzer (ILLink 8.0) never finishes its
        // dataflow over a lambda capturing `widths` next to the `ref caret` loop below, which hangs
        // every CI build (CI=true turns the trim analyzers on).
        var delimiters = new List<string>(table.Aligns.Count);
        for (int c = 0; c < table.Aligns.Count; c++)
        {
            delimiters.Add(DelimiterCell(table.Aligns[c], widths[c]));
        }

        var sb = new StringBuilder();
        caret = 0;
        caretEnd = 0;

        for (int r = 0; r < table.Rows.Count; r++)
        {
            AppendCells(sb, table.Rows[r], widths, ref caret, r == table.Row ? table.Column : -1, r);

            if (r == table.Row) caretEnd = caret + (table.SelectCell ? table.Rows[r][table.Column].Length : 0);

            // The delimiter row belongs to the table, not to the grid: it is written back out
            // right below the header, whether or not the table has any body rows yet.
            if (r == 0) AppendCells(sb, delimiters, widths, ref caret, -1, -1);

        }

        // The block the caret was read from ends at the end of its last LINE, so the text that
        // follows still opens with the newline that closed it: one more here would double it.
        if (sb.Length > 0 && sb[^1] == '\n') sb.Length--;

        return sb.ToString();
    }

    private static void AppendCells(StringBuilder sb, List<string> cells, int[] widths, ref int caret, int caretColumn, int row)
    {
        sb.Append('|');
        for (int c = 0; c < cells.Count; c++)
        {
            sb.Append(' ');
            if (c == caretColumn && row >= 0) caret = sb.Length;
            sb.Append(cells[c].PadRight(widths[c])).Append(" |");
        }
        sb.Append('\n');
    }

    private static string DelimiterCell(char align, int width)
    {
        int inner = Math.Max(MinCellWidth, width);

        return align switch
        {
            'l' => ":" + new string('-', inner - 1),
            'r' => new string('-', inner - 1) + ":",
            'c' => ":" + new string('-', Math.Max(1, inner - 2)) + ":",
            _ => new string('-', inner)
        };
    }

    // ---- the commands -------------------------------------------------------

    private static BitMarkdownEditorEditResult TableInsertRow(string text, int start, int end, bool below) =>
        TableEdit(text, start, end, table =>
        {
            // Nothing goes above the header - a GFM table without one is not a table - so
            // "above the header row" means the first body row.
            int at = below ? table.Row + 1 : Math.Max(1, table.Row);

            table.Rows.Insert(at, [.. Enumerable.Repeat(string.Empty, table.Columns)]);
            table.Row = at;
            table.Column = 0;
            return true;
        });

    private static BitMarkdownEditorEditResult TableDeleteRow(string text, int start, int end) =>
        TableEdit(text, start, end, table =>
        {
            // The header carries the column count and the delimiter row; taking it away would
            // stop the block being a table at all, so it stays.
            if (table.Row == 0) return false;

            table.Rows.RemoveAt(table.Row);
            table.Row = Math.Min(table.Row, table.Rows.Count - 1);
            return true;
        });

    private static BitMarkdownEditorEditResult TableInsertColumn(string text, int start, int end, bool after) =>
        TableEdit(text, start, end, table =>
        {
            int at = after ? table.Column + 1 : table.Column;

            foreach (var row in table.Rows) row.Insert(at, string.Empty);
            table.Aligns.Insert(at, 'n');

            table.Row = 0;
            table.Column = at;
            return true;
        });

    private static BitMarkdownEditorEditResult TableDeleteColumn(string text, int start, int end) =>
        TableEdit(text, start, end, table =>
        {
            // A table with no columns is not a table either.
            if (table.Columns <= 1) return false;

            int at = table.Column;
            foreach (var row in table.Rows) row.RemoveAt(at);
            table.Aligns.RemoveAt(at);

            table.Column = Math.Min(at, table.Columns - 1);
            return true;
        });

    /// <summary>
    /// Walks to the next (or previous) cell, selecting what is in it. Past the last cell of the
    /// last row a new row is added, which is how a table is filled in from the keyboard; before
    /// the first cell there is nowhere to go, so the keystroke is left to whatever wanted it.
    /// </summary>
    private static BitMarkdownEditorEditResult TableMoveCell(string text, int start, int end, bool forward) =>
        TableEdit(text, start, end, table =>
        {
            table.SelectCell = true;

            if (forward)
            {
                if (table.Column + 1 < table.Columns)
                {
                    table.Column++;
                    return true;
                }

                table.Column = 0;
                if (table.Row + 1 < table.Rows.Count)
                {
                    table.Row++;
                    return true;
                }

                table.Rows.Add([.. Enumerable.Repeat(string.Empty, table.Columns)]);
                table.Row = table.Rows.Count - 1;
                return true;
            }

            if (table.Column > 0)
            {
                table.Column--;
                return true;
            }

            if (table.Row == 0) return false;

            table.Row--;
            table.Column = table.Columns - 1;
            return true;
        });

    private static BitMarkdownEditorEditResult TableAlign(string text, int start, int end, char align) =>
        TableEdit(text, start, end, table =>
        {
            if (table.Aligns[table.Column] == align) return false;

            table.Aligns[table.Column] = align;
            return true;
        });
}
