using System.Text;

namespace Bit.Bmotion.Demos.Shared;

/// <summary>
/// A small, dependency-free Razor/C# syntax highlighter for the demo code snippets.
/// Produces HTML with <c>&lt;span class="tok-*"&gt;</c> token wrappers (already HTML-encoded)
/// and wraps the lines that belong to a Bmotion component tag (<c>&lt;Bmotion&gt;</c>,
/// <c>&lt;BmotionAnimatePresence&gt;</c>, <c>&lt;BmotionConfig&gt;</c>) in a <c>hl-line</c> span so they
/// stand out among the surrounding markup.
/// It is intentionally pragmatic - it covers the constructs used by the demo snippets rather
/// than being a fully spec-compliant Razor lexer.
/// </summary>
public static class SyntaxHighlighter
{
    /// <summary>Highlights Razor/C# source and returns HTML markup safe to render verbatim.</summary>
    public static string Highlight(string code)
    {
        try
        {
            code = code.Replace("\r\n", "\n").Replace("\r", "\n");
            return new Lexer(code).Run();
        }
        catch { return System.Net.WebUtility.HtmlEncode(code); }
    }

    private sealed class LineBuf
    {
        public readonly StringBuilder Sb = new();
        public bool Hl;
    }

    private sealed class Lexer
    {
        private readonly string s;
        private readonly int n;
        private int i;

        private readonly List<LineBuf> lines = new();
        private LineBuf cur;
        private bool tagHl; // currently emitting a Bmotion component tag

        public Lexer(string code)
        {
            s = code; n = code.Length;
            cur = new LineBuf();
            lines.Add(cur);
        }

        public string Run()
        {
            Markup(false);

            var outSb = new StringBuilder();
            for (int k = 0; k < lines.Count; k++)
            {
                var ln = lines[k];
                // Each line is rendered as its own block-level span. Empty lines get a
                // zero-width space so they keep their height. Newlines are intentionally
                // NOT emitted: the block layout provides the line breaks, and a literal
                // '\n' next to a display:block span would render as an extra blank line.
                var cls = ln.Hl ? "code-line hl-line" : "code-line";
                outSb.Append("<span class=\"").Append(cls).Append("\">");
                if (ln.Sb.Length == 0) outSb.Append("\u200b");
                else outSb.Append(ln.Sb);
                outSb.Append("</span>");
            }
            return outSb.ToString();
        }

        // ── Vocabularies ──────────────────────────────────────────────────────
        private static readonly HashSet<string> BmotionTags = new(StringComparer.Ordinal)
        {
            "Bmotion", "BmotionAnimatePresence", "BmotionConfig",
        };
        private static readonly HashSet<string> Keywords = new()
        {
            "abstract","as","async","await","base","bool","break","byte","case","catch","char","class",
            "const","continue","decimal","default","do","double","else","enum","false","finally","float",
            "for","foreach","get","if","in","init","int","interface","internal","is","lock","long","new",
            "namespace","null","object","out","override","params","private","protected","public","readonly",
            "record","ref","return","sealed","set","short","static","string","struct","switch","this","throw",
            "true","try","typeof","using","var","virtual","void","while","yield",
        };
        private static readonly HashSet<string> SingleLineDirectives = new()
        {
            "using","inject","implements","page","namespace","inherits","layout","attribute","model",
            "rendermode","preservewhitespace","typeparam","addTagHelper",
        };
        private static readonly HashSet<string> BodyDirectives = new() { "code", "functions" };
        private static readonly HashSet<string> BlockKeywords = new()
        {
            "if","else","for","foreach","while","switch","do","lock",
        };

        // ── Output helpers ────────────────────────────────────────────────────
        private void NewLine() { cur = new LineBuf(); lines.Add(cur); }

        private void RawHtml(string h) { cur.Sb.Append(h); if (tagHl) cur.Hl = true; }

        private void Out(char c)
        {
            if (c == '\n') { NewLine(); return; }
            if (c == '\r') return;
            switch (c)
            {
                case '&': cur.Sb.Append("&amp;"); break;
                case '<': cur.Sb.Append("&lt;"); break;
                case '>': cur.Sb.Append("&gt;"); break;
                default:  cur.Sb.Append(c); break;
            }
            if (tagHl) cur.Hl = true;
        }

        private void Plain(char c) => Out(c);
        private void Plain(string t) { foreach (var c in t) Out(c); }

        private void Span(string t, string cls)
        {
            int start = 0;
            for (int k = 0; k < t.Length; k++)
            {
                if (t[k] == '\n')
                {
                    SpanSeg(t.Substring(start, k - start), cls);
                    NewLine();
                    start = k + 1;
                }
            }
            SpanSeg(t.Substring(start), cls);
        }

        private void SpanSeg(string seg, string cls)
        {
            if (seg.Length == 0) return;
            RawHtml("<span class=\"" + cls + "\">");
            Plain(seg);
            RawHtml("</span>");
        }

        private bool Match(string m) => i + m.Length <= n && string.CompareOrdinal(s, i, m, 0, m.Length) == 0;
        private void SkipWs() { while (i < n && char.IsWhiteSpace(s[i])) { Plain(s[i]); i++; } }

        private string ReadIdent()
        {
            int st = i;
            while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
            return s.Substring(st, i - st);
        }

        // ── Markup mode ───────────────────────────────────────────────────────
        private void Markup(bool untilCloseBrace)
        {
            while (i < n)
            {
                char c = s[i];
                if (untilCloseBrace && c == '}') { Plain('}'); i++; return; }
                if (c == '@')
                {
                    if (i + 1 < n && s[i + 1] == '*') RazorComment();
                    else RazorAt();
                    continue;
                }
                if (c == '<')
                {
                    if (Match("<!--")) { HtmlComment(); continue; }
                    if (i + 1 < n && (char.IsLetter(s[i + 1]) || s[i + 1] == '/')) { Tag(); continue; }
                    Plain('<'); i++; continue;
                }
                Plain(c); i++;
            }
        }

        private void MarkupGroup() { Plain('{'); i++; Markup(true); }

        private void RazorComment()
        {
            int st = i; i += 2;
            while (i < n && !(s[i] == '*' && i + 1 < n && s[i + 1] == '@')) i++;
            if (i < n) i += 2;
            Span(s.Substring(st, i - st), "tok-com");
        }

        private void HtmlComment()
        {
            int st = i; i += 4;
            while (i < n && !Match("-->")) i++;
            if (i < n) i += 3;
            Span(s.Substring(st, i - st), "tok-com");
        }

        private void Tag()
        {
            RawHtml("<span class=\"tok-tag\">");
            Out(s[i]); i++;                       // '<'
            if (i < n && s[i] == '/') { Out('/'); i++; }
            int st = i;
            while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '-' || s[i] == ':' || s[i] == '_')) { Out(s[i]); i++; }
            string name = s.Substring(st, i - st);
            RawHtml("</span>");

            bool bm = BmotionTags.Contains(name);
            if (bm) { tagHl = true; cur.Hl = true; }

            while (i < n)
            {
                char c = s[i];
                if (c == '>') { Span(">", "tok-tag"); i++; if (bm) tagHl = false; return; }
                if (c == '/' && i + 1 < n && s[i + 1] == '>') { Span("/>", "tok-tag"); i += 2; if (bm) tagHl = false; return; }
                if (char.IsWhiteSpace(c)) { Plain(c); i++; continue; }
                if (c == '@')
                {
                    i++;
                    string w = ReadIdent();
                    Span("@" + w, "tok-attr");
                    continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int as_ = i;
                    while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '-' || s[i] == ':' || s[i] == '_')) i++;
                    Span(s.Substring(as_, i - as_), "tok-attr");
                    continue;
                }
                if (c == '=') { Plain('='); i++; AttrValue(); continue; }
                Plain(c); i++;
            }
            if (bm) tagHl = false;
        }

        private void AttrValue()
        {
            while (i < n && char.IsWhiteSpace(s[i])) { Plain(s[i]); i++; }
            if (i >= n) return;
            char c = s[i];
            if (c == '"' || c == '\'')
            {
                char q = c;
                Span(q.ToString(), "tok-str"); i++;
                var buf = new StringBuilder();
                void Flush() { if (buf.Length > 0) { Span(buf.ToString(), "tok-str"); buf.Clear(); } }
                while (i < n)
                {
                    char d = s[i];
                    if (d == q) { Flush(); Span(q.ToString(), "tok-str"); i++; return; }
                    if (d == '@')
                    {
                        Flush();
                        if (i + 1 < n && s[i + 1] == '*') RazorComment();
                        else RazorAt();
                        continue;
                    }
                    buf.Append(d); i++;
                }
                Flush();
            }
            else
            {
                int st = i;
                while (i < n && !char.IsWhiteSpace(s[i]) && s[i] != '>' && s[i] != '/') i++;
                if (i > st) Span(s.Substring(st, i - st), "tok-str");
            }
        }

        // ── Razor transition ──────────────────────────────────────────────────
        private void RazorAt()
        {
            i++; // consume '@'
            if (i >= n) { Span("@", "tok-raz"); return; }
            char c = s[i];
            if (c == '(' || c == '{') { Span("@", "tok-raz"); CSharpGroup(); return; }
            if (char.IsLetter(c) || c == '_')
            {
                string w = ReadIdent();
                if (SingleLineDirectives.Contains(w)) { Span("@" + w, "tok-key"); DirectiveLine(); return; }
                if (BodyDirectives.Contains(w)) { Span("@" + w, "tok-key"); SkipWs(); if (i < n && s[i] == '{') CSharpGroup(); return; }
                if (BlockKeywords.Contains(w))
                {
                    Span("@" + w, "tok-key");
                    SkipWs();
                    if (i < n && s[i] == '(') CSharpGroup();
                    SkipWs();
                    if (i < n && s[i] == '{') MarkupGroup();
                    return;
                }
                Span("@", "tok-raz"); EmitIdentCS(w); MemberTail(); return;
            }
            Span("@", "tok-raz");
        }

        private void MemberTail()
        {
            while (i < n)
            {
                char c = s[i];
                if (c == '.' && i + 1 < n && (char.IsLetter(s[i + 1]) || s[i + 1] == '_'))
                {
                    Plain('.'); i++; EmitIdentCS(ReadIdent()); continue;
                }
                if (c == '(' || c == '[') { CSharpGroup(); continue; }
                break;
            }
        }

        private void DirectiveLine()
        {
            while (i < n && s[i] != '\n')
            {
                char c = s[i];
                if (char.IsLetter(c) || c == '_') { EmitIdentCS(ReadIdent()); continue; }
                if (char.IsDigit(c)) { Number(); continue; }
                Plain(c); i++;
            }
        }

        // ── C# mode ─────────────────────────────────────────────────────────────
        private void CSharpGroup()
        {
            Plain(s[i]); i++; // opening bracket
            int depth = 1;
            while (i < n && depth > 0)
            {
                char c = s[i];
                if (c == '"' || c == '\'') { CSharpString(c); continue; }
                if (c == '$' && i + 1 < n && s[i + 1] == '"') { InterpString(); continue; }
                if (c == '@' && i + 1 < n && s[i + 1] == '"') { VerbatimString(); continue; }
                if (c == '/' && i + 1 < n && s[i + 1] == '/') { LineComment(); continue; }
                if (c == '/' && i + 1 < n && s[i + 1] == '*') { BlockComment(); continue; }
                if (c == '(' || c == '{' || c == '[') { depth++; Plain(c); i++; continue; }
                if (c == ')' || c == '}' || c == ']') { depth--; Plain(c); i++; continue; }
                if (char.IsLetter(c) || c == '_') { EmitIdentCS(ReadIdent()); continue; }
                if (char.IsDigit(c)) { Number(); continue; }
                Plain(c); i++;
            }
        }

        private void EmitIdentCS(string w)
        {
            if (w.Length == 0) return;
            if (Keywords.Contains(w)) Span(w, "tok-key");
            else if (char.IsUpper(w[0])) Span(w, "tok-type");
            else Plain(w);
        }

        private void Number()
        {
            int st = i;
            while (i < n && char.IsDigit(s[i])) i++;
            if (i < n && s[i] == '.' && i + 1 < n && char.IsDigit(s[i + 1]))
            {
                i++;
                while (i < n && char.IsDigit(s[i])) i++;
            }
            if (i < n && "fFdDmMlLuU".IndexOf(s[i]) >= 0) i++;
            Span(s.Substring(st, i - st), "tok-num");
        }

        private void CSharpString(char q)
        {
            int st = i; i++;
            while (i < n)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < n) { i += 2; continue; }
                if (c == q) { i++; break; }
                i++;
            }
            Span(s.Substring(st, i - st), "tok-str");
        }

        private void InterpString()
        {
            int st = i; i += 2; // $"
            int brace = 0;
            while (i < n)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < n) { i += 2; continue; }
                if (c == '{') { if (i + 1 < n && s[i + 1] == '{') { i += 2; continue; } brace++; i++; continue; }
                if (c == '}') { if (i + 1 < n && s[i + 1] == '}') { i += 2; continue; } if (brace > 0) brace--; i++; continue; }
                if (c == '"' && brace == 0) { i++; break; }
                if (c == '"' && brace > 0)
                {
                    i++;
                    while (i < n) { char d = s[i]; if (d == '\\' && i + 1 < n) { i += 2; continue; } if (d == '"') { i++; break; } i++; }
                    continue;
                }
                i++;
            }
            Span(s.Substring(st, i - st), "tok-str");
        }

        private void VerbatimString()
        {
            int st = i; i += 2; // @"
            while (i < n)
            {
                char c = s[i];
                if (c == '"') { if (i + 1 < n && s[i + 1] == '"') { i += 2; continue; } i++; break; }
                i++;
            }
            Span(s.Substring(st, i - st), "tok-str");
        }

        private void LineComment()
        {
            int st = i;
            while (i < n && s[i] != '\n') i++;
            Span(s.Substring(st, i - st), "tok-com");
        }

        private void BlockComment()
        {
            int st = i; i += 2;
            while (i < n && !(s[i] == '*' && i + 1 < n && s[i + 1] == '/')) i++;
            if (i < n) i += 2;
            Span(s.Substring(st, i - st), "tok-com");
        }
    }
}
