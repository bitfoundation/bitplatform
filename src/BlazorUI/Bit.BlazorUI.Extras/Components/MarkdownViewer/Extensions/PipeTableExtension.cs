using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

public enum ColumnAlignment { None, Left, Center, Right }

/// <summary>A GitHub-flavored pipe table.</summary>
public sealed class TableNode : MarkdownNode
{
    public List<List<MarkdownNode>> Header { get; } = new();
    public List<ColumnAlignment> Alignments { get; } = new();
    public List<List<List<MarkdownNode>>> Rows { get; } = new();

    public override IEnumerable<IList<MarkdownNode>> ChildLists
    {
        get
        {
            foreach (var cell in Header) yield return cell;
            foreach (var row in Rows)
                foreach (var cell in row)
                    yield return cell;
        }
    }
}

/// <summary>Parses GitHub-style pipe tables (a header row followed by a delimiter row).</summary>
public sealed partial class PipeTableBlockParser : BlockParser
{
    // Tables sit just before the paragraph fallback.
    public override int Order => 65;

    [GeneratedRegex(@"^\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?$")]
    private static partial Regex DelimiterRow();

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var lines = state.Lines;
        int i = state.Line;
        if (i + 1 >= lines.Count) return false;
        if (!lines[i].Contains('|') || !IsDelimiter(lines[i + 1])) return false;

        var header = SplitRow(lines[i]);
        var delims = SplitRow(lines[i + 1]);
        if (header.Count == 0 || delims.Count != header.Count) return false;

        var alignments = new List<ColumnAlignment>();
        foreach (var d in delims)
        {
            string t = d.Trim();
            if (!Regex.IsMatch(t, @"^:?-+:?$")) return false;
            bool l = t.StartsWith(':'), r = t.EndsWith(':');
            alignments.Add((l, r) switch
            {
                (true, true) => ColumnAlignment.Center,
                (true, false) => ColumnAlignment.Left,
                (false, true) => ColumnAlignment.Right,
                _ => ColumnAlignment.None
            });
        }

        var table = new TableNode();
        table.Alignments.AddRange(alignments);
        foreach (var cell in header)
            table.Header.Add(state.ParseInlines(cell.Trim()));

        int j = i + 2;
        while (j < lines.Count && !BlockProcessor.IsBlank(lines[j]) && lines[j].Contains('|'))
        {
            var cells = SplitRow(lines[j]);
            var row = new List<List<MarkdownNode>>();
            for (int c = 0; c < header.Count; c++)
                row.Add(state.ParseInlines(c < cells.Count ? cells[c].Trim() : string.Empty));
            table.Rows.Add(row);
            j++;
        }

        output.Add(table);
        state.Line = j;
        return true;
    }

    private static bool IsDelimiter(string line)
    {
        string s = line.Trim();
        return s.Contains('-') && DelimiterRow().IsMatch(s);
    }

    private static List<string> SplitRow(string line)
    {
        string s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|') && !s.EndsWith("\\|")) s = s[..^1];

        var cells = new List<string>();
        var sb = new StringBuilder();
        int backtickRun = 0; // length of the backtick run that opened the current code span; 0 when outside.
        for (int i = 0; i < s.Length; i++)
        {
            // Handle backslash escapes as pairs so backslash parity is respected:
            // e.g. in "\\|" the first backslash escapes the second, leaving the pipe
            // as a real cell delimiter, whereas in "\|" the pipe is escaped.
            if (s[i] == '\\' && i + 1 < s.Length && backtickRun == 0)
            {
                char next = s[i + 1];
                if (next == '|') sb.Append('|');                            // escaped pipe -> literal '|'
                else if (next == '`') { sb.Append('\\'); sb.Append('`'); }  // preserve escape so inline parsing handles it
                else { sb.Append('\\'); sb.Append(next); }                  // consume the pair (e.g. "\\")
                i++;
            }
            else if (s[i] == '`')
            {
                int runStart = i;
                while (i + 1 < s.Length && s[i + 1] == '`') i++;
                int runLength = i - runStart + 1;
                if (backtickRun == 0) backtickRun = runLength;
                else if (runLength == backtickRun) backtickRun = 0;
                sb.Append(s, runStart, runLength);
            }
            else if (s[i] == '|' && backtickRun == 0) { cells.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(s[i]);
        }
        cells.Add(sb.ToString());
        return cells;
    }
}

/// <summary>Renders <see cref="TableNode"/> as an HTML table with column alignment.</summary>
public sealed class TableRenderer : NodeRenderer
{
    public override bool Accept(MarkdownNode node) => node is TableNode;

    public override void Write(MarkdownRenderer r, RenderTreeBuilder b, MarkdownNode node)
    {
        var table = (TableNode)node;
        b.OpenElement(r.NextSeq(), "table");

        b.OpenElement(r.NextSeq(), "thead");
        b.OpenElement(r.NextSeq(), "tr");
        for (int c = 0; c < table.Header.Count; c++)
        {
            b.OpenElement(r.NextSeq(), "th");
            AddAlignment(r, b, table, c);
            r.WriteNodes(b, table.Header[c]);
            b.CloseElement();
        }
        b.CloseElement();
        b.CloseElement();

        b.OpenElement(r.NextSeq(), "tbody");
        foreach (var row in table.Rows)
        {
            b.OpenElement(r.NextSeq(), "tr");
            for (int c = 0; c < row.Count; c++)
            {
                b.OpenElement(r.NextSeq(), "td");
                AddAlignment(r, b, table, c);
                r.WriteNodes(b, row[c]);
                b.CloseElement();
            }
            b.CloseElement();
        }
        b.CloseElement();

        b.CloseElement();
    }

    private static void AddAlignment(MarkdownRenderer r, RenderTreeBuilder b, TableNode table, int col)
    {
        if (col >= table.Alignments.Count) return;
        string? align = table.Alignments[col] switch
        {
            ColumnAlignment.Left => "left",
            ColumnAlignment.Center => "center",
            ColumnAlignment.Right => "right",
            _ => null
        };
        if (align is not null)
            b.AddAttribute(r.NextSeq(), "style", $"text-align:{align}");
    }
}

/// <summary>Enables GitHub-style pipe tables.</summary>
public sealed class PipeTableExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new PipeTableBlockParser());
        builder.Renderers.Add(new TableRenderer());
    }
}
