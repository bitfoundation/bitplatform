using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Handles inline links <c>[text](url "title")</c> and images <c>![alt](url)</c>, plus the
/// three reference forms - full <c>[text][ref]</c>, collapsed <c>[text][]</c> and shortcut
/// <c>[text]</c> - whose destination comes from a link reference definition elsewhere in
/// the document.
/// </summary>
public sealed class BitMarkdownLinkInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '[', '!' };

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        string s = state.Text;
        int i = state.Pos;
        bool isImage = s[i] == '!';
        int bracket = isImage ? i + 1 : i;
        if (bracket >= s.Length || s[bracket] != '[') return false;

        int labelEnd = BitMarkdownLinkHelpers.FindLabelEnd(s, bracket);
        if (labelEnd < 0) return false;

        string label = s.Substring(bracket + 1, labelEnd - bracket - 1);

        int p = labelEnd + 1;
        if (p < s.Length && s[p] == '(')
            return TryParseInline(state, s, label, isImage, p);

        return TryParseReference(state, s, label, isImage, labelEnd);
    }

    private static bool TryParseInline(BitMarkdownInlineProcessor state, string s, string label, bool isImage, int openParen)
    {
        int q = openParen + 1;
        if (!ParseDestination(s, ref q, out string url, out string? title)) return false;
        if (q >= s.Length || s[q] != ')') return false;

        Emit(state, label, isImage, BitMarkdownEntities.Decode(url), title);
        state.Pos = q + 1;
        return true;
    }

    // Full, collapsed and shortcut references all resolve against a link reference
    // definition, which may appear anywhere in the document - including after this point.
    // Rather than guess, the parser emits an unresolved node that
    // BitMarkdownLinkReferenceAstProcessor rewrites once the whole document is parsed. It
    // only does so for a label the document actually defines (the pre-scan knows them all),
    // so ordinary bracketed prose in a document with no definitions is scanned untouched.
    private static bool TryParseReference(BitMarkdownInlineProcessor state, string s, string label, bool isImage, int labelEnd)
    {
        string reference;
        string suffix;
        int end;

        int p = labelEnd + 1;
        if (p < s.Length && s[p] == '[')
        {
            int referenceEnd = BitMarkdownLinkHelpers.FindLabelEnd(s, p);
            if (referenceEnd < 0) return false;

            string inner = s.Substring(p + 1, referenceEnd - p - 1);
            if (inner.Trim().Length == 0)
            {
                // Collapsed: "[text][]" reuses the text as the reference label.
                reference = label;
                suffix = "][]";
            }
            else
            {
                reference = inner;
                suffix = "][" + inner + "]";
            }
            end = referenceEnd;
        }
        else
        {
            // Shortcut: "[text]" is itself the reference label.
            reference = label;
            suffix = "]";
            end = labelEnd;
        }

        string normalized = BitMarkdownLinkHelpers.NormalizeLabel(reference);
        if (normalized.Length == 0 || normalized.Length > BitMarkdownLinkHelpers.MaxLabelLength) return false;
        if (state.HasReferenceLabel(normalized) is false) return false;

        var node = new BitMarkdownLinkReferenceNode
        {
            Label = normalized,
            IsImage = isImage,
            RawPrefix = isImage ? "![" : "[",
            RawSuffix = suffix
        };
        node.Children.AddRange(isImage ? state.ParseInlines(label) : RemoveNestedLinks(state.ParseInlines(label)));

        state.AppendNode(node);
        state.Pos = end + 1;
        return true;
    }

    private static void Emit(BitMarkdownInlineProcessor state, string label, bool isImage, string url, string? title)
    {
        if (isImage)
        {
            state.AppendNode(new BitMarkdownImageNode
            {
                Url = BitMarkdownUrlSanitizer.Sanitize(url, isImage: true),
                Title = title,
                Alt = BitMarkdownInlineHelpers.PlainText(state.ParseInlines(label))
            });
            return;
        }

        var link = new BitMarkdownLinkNode
        {
            Url = BitMarkdownUrlSanitizer.Sanitize(url, isImage: false),
            Title = title
        };
        // A link may not contain another link; unwrap any nested links so the
        // inner link's content survives as plain inline text instead.
        link.Children.AddRange(RemoveNestedLinks(state.ParseInlines(label)));
        state.AppendNode(link);
    }

    // Recursively replaces any nested link node with its (also unwrapped) children,
    // ensuring a link's label can never contain another link.
    private static List<BitMarkdownNode> RemoveNestedLinks(IEnumerable<BitMarkdownNode> nodes)
    {
        var result = new List<BitMarkdownNode>();
        foreach (var node in nodes)
        {
            if (node is BitMarkdownLinkNode nested)
            {
                result.AddRange(RemoveNestedLinks(nested.Children));
                continue;
            }
            foreach (var list in node.ChildLists)
            {
                var cleaned = RemoveNestedLinks(list);
                list.Clear();
                foreach (var child in cleaned) list.Add(child);
            }
            result.Add(node);
        }
        return result;
    }

    private static bool ParseDestination(string s, ref int i, out string url, out string? title)
    {
        url = string.Empty;
        title = null;
        int n = s.Length;
        while (i < n && (s[i] is ' ' or '\t' or '\n')) i++;

        var sb = new StringBuilder();
        if (i < n && s[i] == '<')
        {
            i++;
            while (i < n && s[i] != '>' && s[i] != '\n') sb.Append(s[i++]);
            if (i >= n || s[i] != '>') return false;
            i++;
        }
        else
        {
            int depth = 0;
            while (i < n)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < n) { sb.Append(s[i + 1]); i += 2; continue; }
                if (c is ' ' or '\t' or '\n') break;
                if (c == '(') depth++;
                else if (c == ')')
                {
                    if (depth == 0) break;
                    depth--;
                }
                sb.Append(c);
                i++;
            }
        }
        url = sb.ToString();

        while (i < n && (s[i] is ' ' or '\t' or '\n')) i++;
        if (i < n && (s[i] is '"' or '\'' or '('))
        {
            char closeCh = s[i] == '(' ? ')' : s[i];
            i++;
            var tb = new StringBuilder();
            while (i < n && s[i] != closeCh)
            {
                if (s[i] == '\\' && i + 1 < n) { tb.Append(s[i + 1]); i += 2; continue; }
                tb.Append(s[i++]);
            }
            if (i >= n) return false;
            i++;
            title = BitMarkdownEntities.Decode(tb.ToString());
            while (i < n && (s[i] is ' ' or '\t' or '\n')) i++;
        }

        return true;
    }
}
