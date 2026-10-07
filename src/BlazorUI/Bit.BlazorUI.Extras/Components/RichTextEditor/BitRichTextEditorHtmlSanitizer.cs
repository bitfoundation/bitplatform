using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// The C# counterpart of the bridge's sanitize pass, for the one moment the bridge cannot run: the value the
/// editing surface is rendered with before the page is interactive (a prerender, or a static SSR page for good).
/// It applies the same rules - the policy's tag, attribute and URI-scheme allowlists, the CSS property allowlist,
/// the embed hosts, the hardening of a blank target - and drops the same elements with everything inside them.
/// </summary>
/// <remarks>
/// The bridge sanitizes the tree the browser's own parser builds; there is no such parser here, so the markup is
/// read by a tokenizer and a tree of its own is written back. That makes it stricter than the bridge, never looser:
/// <list type="bullet">
/// <item>Every name and value is decoded, checked and encoded again, so what the browser reads back is exactly what
/// was checked, and no text can turn into markup.</item>
/// <item>Only the tags of the default allowlist are written (those the policy allows, of them), since those are the
/// tags whose parsing rules it models. Any other tag a custom policy allows is unwrapped here, keeping its text, and
/// comes back when the bridge replaces the content on the first interactive render.</item>
/// <item>The output is well formed, and every element the browser would close on its own is closed explicitly first,
/// so the browser builds the very tree that was written - in particular nothing in a value can close an element
/// around the surface and spill the rest of the value into the page.</item>
/// </list>
/// </remarks>
internal static partial class BitRichTextEditorHtmlSanitizer
{
    // The longest value sanitized here (in chars, about a megabyte); a longer one is left to the bridge (see Sanitize).
    private const int MaxInputLength = 1024 * 1024;

    // How deep elements are written. A start tag past it is unwrapped - its text kept, the output still well formed -
    // which bounds every look down the open-element stack, and so keeps the work linear in the input. Content written
    // by the editor itself is nowhere near as deep, and browsers flatten a tree far shallower than the input asks for
    // too (past 512 levels).
    private const int MaxDepth = 128;

    // The tags this writer knows the parsing rules of: the default policy's allowlist.
    private static readonly HashSet<string> KnownTags = new(BitRichTextEditorSanitizationPolicy.Default.AllowedTags, StringComparer.Ordinal);

    // The embed hosts a policy naming none of its own gets, read once (Default builds a new policy on every read).
    private static readonly string[] DefaultIframeHosts = [.. BitRichTextEditorSanitizationPolicy.Default.AllowedIframeHosts ?? []];

    // The rules of the secure default policy, which an editor with no SanitizationPolicy of its own is sanitized with.
    // Rules are only ever read once built, so one instance serves every call.
    private static readonly Lazy<Rules> DefaultRules = new(() => new Rules(BitRichTextEditorSanitizationPolicy.Default));

    private static readonly HashSet<string> VoidTags =
    [
        "area", "base", "br", "col", "embed", "frame", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
    ];

    // The bridge's DROPPED_TAGS: never content, so removed with everything inside them rather than unwrapped. A
    // policy listing one of them keeps it on the bridge's side; here they always go (an iframe is the exception,
    // checked on its own, since the default policy allows it).
    private static readonly HashSet<string> DroppedTags =
    [
        "script", "style", "object", "embed", "link", "meta", "title", "head", "base", "form", "noscript", "template",
        "svg", "math", "frame", "frameset", "applet", "dialog", "noembed", "xmp", "plaintext"
    ];

    // Elements whose content the tokenizer reads as text up to their own end tag, not as markup.
    private static readonly HashSet<string> RawTextTags = ["script", "style", "xmp", "iframe", "noembed", "noframes", "noscript"];

    // ... of which these decode character references in it.
    private static readonly HashSet<string> EscapableRawTextTags = ["title", "textarea"];

    // The parser's "special" elements among the known tags: an end tag nobody opened stops at them, and a list item
    // looks no further than them for one to close.
    private static readonly HashSet<string> SpecialTags =
    [
        "blockquote", "br", "caption", "col", "colgroup", "div", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "iframe", "img",
        "li", "ol", "p", "pre", "source", "table", "tbody", "td", "tfoot", "th", "thead", "tr", "ul"
    ];

    private static readonly HashSet<string> FormattingTags = ["a", "b", "code", "em", "i", "s", "strike", "strong", "u"];

    private static readonly HashSet<string> HeadingTags = ["h1", "h2", "h3", "h4", "h5", "h6"];

    // Start tags that close an open paragraph first.
    private static readonly HashSet<string> ParagraphClosers = ["blockquote", "div", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "ol", "p", "pre", "ul"];

    // Where the parser's scope checks stop among the known tags.
    private static readonly HashSet<string> ScopeBoundaries = ["caption", "table", "td", "th"];

    private static readonly HashSet<string> TableStructureTags = ["caption", "col", "colgroup", "tbody", "td", "tfoot", "th", "thead", "tr"];

    private static readonly HashSet<string> RowGroupTags = ["tbody", "tfoot", "thead"];

    // The elements directly inside which only table structure goes: text and anything else there is carried out in
    // front of the table by the browser, so it is left out here.
    private static readonly HashSet<string> TableContextTags = ["colgroup", "table", "tbody", "tfoot", "thead", "tr"];

    // The bridge's ALLOWED_CSS_PROPS.
    private static readonly HashSet<string> AllowedCssProperties =
    [
        "color", "background-color", "background",
        "font-family", "font-size", "font-weight", "font-style", "font-variant",
        "text-align", "text-decoration", "text-decoration-line", "text-indent", "text-transform",
        "letter-spacing", "word-spacing", "line-height", "white-space", "direction", "unicode-bidi",
        "vertical-align", "list-style-type", "list-style-position",
        "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
        "width", "height", "max-width", "max-height", "min-width", "min-height",
        "border", "border-top", "border-right", "border-bottom", "border-left",
        "border-color", "border-style", "border-width", "border-radius", "border-collapse",
        "caption-side", "table-layout", "float", "clear", "display", "opacity"
    ];

    // The bridge's UNSAFE_CSS_VALUE.
    [GeneratedRegex(@"url\s*\(|image-set\s*\(|expression\s*\(|javascript\s*:|vbscript\s*:|behavior\s*:|-moz-binding|@import|\\", RegexOptions.IgnoreCase)]
    private static partial Regex UnsafeCssValue();

    // What the browser ignores in a URL's scheme, stripped before the scheme is read (the bridge's isAllowedUri).
    [GeneratedRegex("[\u0000- \u007F-\u009F​-‍﻿]")]
    private static partial Regex IgnoredUriCharacters();

    [GeneratedRegex("^([a-z][a-z0-9+.-]*):", RegexOptions.IgnoreCase)]
    private static partial Regex UriScheme();

    // An attribute name is written back only when it is a plain one, whatever the policy lists.
    [GeneratedRegex("^[a-z0-9_.:-]+$")]
    private static partial Regex PlainAttributeName();



    /// <summary>
    /// Sanitizes <paramref name="html"/> against <paramref name="policy"/> into markup safe to render into the editing
    /// surface as it is.
    /// </summary>
    /// <remarks>
    /// No <paramref name="policy"/> is the secure default, whose rules are read once rather than on every call.
    /// </remarks>
    public static string Sanitize(string? html, BitRichTextEditorSanitizationPolicy? policy)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // This runs on the server for every prerender and every static SSR request, so its cost is bounded
        // before it starts: of a value past MaxInputLength only its beginning is rendered here, up to the last tag
        // that starts within the limit, and the bridge fills in the whole of it the moment the page is
        // interactive. Rendering nothing instead would leave an empty surface, which the placeholder then claims
        // is a blank document - for good, on a static page. Below the limit the work is linear in the input.
        if (html.Length > MaxInputLength)
        {
            var cut = html.LastIndexOf('<', MaxInputLength - 1);
            if (cut <= 0)
            {
                cut = MaxInputLength;
                if (char.IsHighSurrogate(html[cut - 1])) cut--;
            }

            html = html[..cut];
        }

        var writer = new Writer(policy is null ? DefaultRules.Value : new Rules(policy));
        var s = html;
        var n = s.Length;
        var i = 0;

        while (i < n)
        {
            if (s[i] != '<')
            {
                var next = s.IndexOf('<', i);
                if (next < 0) next = n;
                writer.Text(Decode(s[i..next]));
                i = next;
                continue;
            }

            if (string.CompareOrdinal(s, i, "<!--", 0, 4) == 0)
            {
                i = SkipComment(s, i + 4);
                continue;
            }

            if (i + 1 < n && (s[i + 1] == '!' || s[i + 1] == '?'))
            {
                i = SkipBogusComment(s, i + 2);
                continue;
            }

            if (i + 1 < n && s[i + 1] == '/')
            {
                if (i + 2 < n && char.IsAsciiLetter(s[i + 2]))
                {
                    var end = ReadTag(s, i + 2, out i);
                    if (end is null) break; // a tag cut off by the end of the input is dropped by the browser too

                    writer.EndTag(end.Name);
                }
                else if (i + 2 < n && s[i + 2] == '>')
                {
                    i += 3;
                }
                else if (i + 2 >= n)
                {
                    writer.Text("</");
                    i = n;
                }
                else
                {
                    i = SkipBogusComment(s, i + 2);
                }
                continue;
            }

            if (i + 1 < n && char.IsAsciiLetter(s[i + 1]))
            {
                var tag = ReadTag(s, i + 1, out i);
                if (tag is null) break;

                // Everything after it is text inside it, which is dropped with it.
                if (tag.Name == "plaintext") break;

                if (RawTextTags.Contains(tag.Name) || EscapableRawTextTags.Contains(tag.Name))
                {
                    var content = ReadRawText(s, i, tag.Name, out i);
                    writer.RawTextElement(tag, EscapableRawTextTags.Contains(tag.Name) ? Decode(content) : content);
                }
                else
                {
                    writer.StartTag(tag);
                }
                continue;
            }

            writer.Text("<");
            i++;
        }

        return writer.Finish();
    }



    private sealed record Tag(string Name, List<KeyValuePair<string, string>> Attributes);

    private static bool IsHtmlWhitespace(char c) => c is ' ' or '\t' or '\n' or '\f' or '\r';

    private static string Decode(string value) => WebUtility.HtmlDecode(value).Replace('\0', '�');

    // "<!-->" and "<!--->" are empty comments; otherwise one runs to "-->" (or "--!>"), or to the end of the input.
    private static int SkipComment(string s, int i)
    {
        if (i < s.Length && s[i] == '>') return i + 1;
        if (string.CompareOrdinal(s, i, "->", 0, 2) == 0) return i + 2;

        // One forward pass for whichever end comes first: looking for each end on its own would read to the end of
        // the input for every comment that does not use it.
        var n = s.Length;
        while (true)
        {
            var dashes = s.IndexOf("--", i, StringComparison.Ordinal);
            if (dashes < 0) return n;
            if (dashes + 2 < n && s[dashes + 2] == '>') return dashes + 3;
            if (dashes + 3 < n && s[dashes + 2] == '!' && s[dashes + 3] == '>') return dashes + 4;
            i = dashes + 1;
        }
    }

    private static int SkipBogusComment(string s, int i)
    {
        var close = s.IndexOf('>', i);
        return close < 0 ? s.Length : close + 1;
    }

    // Reads a tag from its name on, the way the browser's tokenizer does; null when the input ends inside it.
    private static Tag? ReadTag(string s, int i, out int end)
    {
        var n = s.Length;
        var name = new StringBuilder();
        while (i < n && IsHtmlWhitespace(s[i]) is false && s[i] != '/' && s[i] != '>')
        {
            name.Append(s[i] == '\0' ? '�' : char.ToLowerInvariant(s[i]));
            i++;
        }

        var tag = new Tag(name.ToString(), []);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            while (i < n && (IsHtmlWhitespace(s[i]) || s[i] == '/')) i++;

            if (i >= n)
            {
                end = n;
                return null;
            }

            if (s[i] == '>')
            {
                end = i + 1;
                return tag;
            }

            // The first character is part of the name whatever it is, an '=' included.
            var nameStart = i++;
            while (i < n && IsHtmlWhitespace(s[i]) is false && s[i] != '/' && s[i] != '>' && s[i] != '=') i++;
            var attributeName = s[nameStart..i].ToLowerInvariant();

            while (i < n && IsHtmlWhitespace(s[i])) i++;

            var value = string.Empty;
            if (i < n && s[i] == '=')
            {
                i++;
                while (i < n && IsHtmlWhitespace(s[i])) i++;

                if (i < n && (s[i] == '"' || s[i] == '\''))
                {
                    var close = s.IndexOf(s[i], i + 1);
                    if (close < 0)
                    {
                        end = n;
                        return null;
                    }

                    value = s[(i + 1)..close];
                    i = close + 1;
                }
                else
                {
                    var valueStart = i;
                    while (i < n && IsHtmlWhitespace(s[i]) is false && s[i] != '>') i++;
                    value = s[valueStart..i];
                }
            }

            // A repeated attribute is ignored: the first one is the one the element gets.
            if (seen.Add(attributeName))
            {
                tag.Attributes.Add(new(attributeName, Decode(value)));
            }
        }
    }

    // The text of a raw-text element, up to its own end tag (or to the end of the input when there is none).
    private static string ReadRawText(string s, int i, string name, out int end)
    {
        var n = s.Length;
        var search = i;
        while (true)
        {
            var close = s.IndexOf("</" + name, search, StringComparison.OrdinalIgnoreCase);
            if (close < 0)
            {
                end = n;
                return s[i..];
            }

            var after = close + 2 + name.Length;
            if (after < n && (IsHtmlWhitespace(s[after]) || s[after] == '/' || s[after] == '>'))
            {
                ReadTag(s, close + 2, out end);
                return s[i..close];
            }

            search = close + 1;
        }
    }



    /// <summary>The policy, lowercased the way the bridge reads it.</summary>
    private sealed class Rules
    {
        private readonly HashSet<string> _tags;
        private readonly Dictionary<string, HashSet<string>> _attributes;
        private readonly HashSet<string> _schemes;
        private readonly HashSet<string> _iframeHosts;
        private readonly bool _allowDataImageUris;

        public Rules(BitRichTextEditorSanitizationPolicy policy)
        {
            // A set left null (or holding a null) in a hand-built policy allows nothing, as an empty one does, rather
            // than failing the render of the page around the editor.
            _tags = new(Lowered(policy.AllowedTags).Where(KnownTags.Contains), StringComparer.Ordinal);
            _attributes = (policy.AllowedAttributes ?? new Dictionary<string, ISet<string>>())
                                .Where(kv => kv.Key is not null)
                                .GroupBy(kv => kv.Key.ToLowerInvariant())
                                .ToDictionary(g => g.Key,
                                              g => new HashSet<string>(g.SelectMany(kv => Lowered(kv.Value)), StringComparer.Ordinal),
                                              StringComparer.Ordinal);
            _schemes = new(Lowered(policy.AllowedUriSchemes), StringComparer.Ordinal);
            // No hosts named is the built-in approved embed hosts, as on the bridge.
            _iframeHosts = new(Lowered(policy.AllowedIframeHosts ?? DefaultIframeHosts), StringComparer.Ordinal);
            _allowDataImageUris = policy.AllowDataImageUris;
        }

        private static IEnumerable<string> Lowered(IEnumerable<string>? values)
            => values is null ? [] : values.Where(v => v is not null).Select(v => v.ToLowerInvariant());

        public bool AllowsTag(string tag) => _tags.Contains(tag);

        public bool AllowsAttribute(string tag, string attribute)
            => (_attributes.TryGetValue(tag, out var own) && own.Contains(attribute))
            || (_attributes.TryGetValue("*", out var global) && global.Contains(attribute));

        /// <summary>
        /// The attributes the element keeps, in their order, or null when the element itself has to go (an iframe
        /// whose source is not an approved https embed).
        /// </summary>
        public List<KeyValuePair<string, string>>? FilterAttributes(Tag tag)
        {
            var kept = new List<KeyValuePair<string, string>>();
            foreach (var (name, value) in tag.Attributes)
            {
                if (name.StartsWith("on", StringComparison.Ordinal)) continue;
                if (PlainAttributeName().IsMatch(name) is false) continue;
                // A video's poster is an image the browser fetches like an img's src.
                if ((name == "href" || name == "src" || name == "poster")
                    && IsAllowedUri(value, isImage: (name == "src" && tag.Name == "img") || name == "poster") is false) continue;
                if (AllowsAttribute(tag.Name, name) is false) continue;

                var keptValue = value;
                if (name == "style")
                {
                    keptValue = SanitizeStyle(value);
                    if (keptValue.Length == 0) continue;
                }

                kept.Add(new(name, keptValue));
            }

            if (tag.Name == "iframe" && IsAllowedEmbedSrc(kept.Find(a => a.Key == "src").Value) is false) return null;

            // A target that opens another browsing context - not only "_blank" but any name, and any spelling of
            // "_blank" the browser still treats as one, such as one padded with spaces - hands the opened page
            // window.opener unless rel says noopener; when the policy does not allow a rel to say it, the target goes
            // instead.
            var target = kept.FindIndex(a => a.Key == "target");
            if (tag.Name == "a" && target >= 0 && OpensNewContext(kept[target].Value))
            {
                if (AllowsAttribute("a", "rel"))
                {
                    var rel = kept.FindIndex(a => a.Key == "rel");
                    if (rel >= 0)
                    {
                        kept[rel] = new("rel", "noopener noreferrer");
                    }
                    else
                    {
                        kept.Add(new("rel", "noopener noreferrer"));
                    }
                }
                else
                {
                    kept.RemoveAt(target);
                }
            }

            return kept;
        }

        // The bridge's opensNewContext: every target but the three that name the link's own browsing context or its
        // ancestors.
        private static bool OpensNewContext(string target)
            => target.Length > 0
            && string.Equals(target, "_self", StringComparison.OrdinalIgnoreCase) is false
            && string.Equals(target, "_parent", StringComparison.OrdinalIgnoreCase) is false
            && string.Equals(target, "_top", StringComparison.OrdinalIgnoreCase) is false;

        // The bridge's isAllowedUri.
        private bool IsAllowedUri(string url, bool isImage)
        {
            var trimmed = url.Trim();
            if (trimmed.Length == 0) return false;

            var candidate = IgnoredUriCharacters().Replace(trimmed, string.Empty);
            if (candidate.Length == 0) return false;
            if (candidate.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return false;
            if (candidate.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)) return false;

            var match = UriScheme().Match(candidate);
            if (match.Success is false)
            {
                // A relative URL, unless it is one of the forms browsers resolve against another host.
                return candidate.StartsWith("//", StringComparison.Ordinal) is false
                    && candidate.StartsWith('\\') is false
                    && candidate.StartsWith("/\\", StringComparison.Ordinal) is false;
            }

            var scheme = match.Groups[1].Value.ToLowerInvariant();
            if (scheme == "data")
            {
                return isImage && _allowDataImageUris && candidate.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);
            }

            return _schemes.Contains(scheme);
        }

        // The bridge's isAllowedEmbedSrc.
        private bool IsAllowedEmbedSrc(string? src)
        {
            var value = (src ?? string.Empty).Trim();
            if (value.Length == 0) return false;
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) is false) return false;
            if (uri.Scheme != Uri.UriSchemeHttps) return false;

            return _iframeHosts.Contains("*") || _iframeHosts.Contains(uri.Authority.ToLowerInvariant());
        }

        // The bridge's sanitizeStyle.
        private static string SanitizeStyle(string style)
        {
            var kept = new List<string>();
            foreach (var declaration in style.Split(';'))
            {
                var colon = declaration.IndexOf(':');
                if (colon <= 0) continue;

                var property = declaration[..colon].Trim().ToLowerInvariant();
                var value = declaration[(colon + 1)..].Trim();
                if (value.Length == 0) continue;
                if (AllowedCssProperties.Contains(property) is false) continue;
                if (UnsafeCssValue().IsMatch(value)) continue;

                kept.Add($"{property}: {value}");
            }

            return string.Join("; ", kept);
        }
    }



    /// <summary>
    /// Writes the sanitized tree, keeping the stack of the elements it has opened in step with the one the browser
    /// will build from the output.
    /// </summary>
    private sealed class Writer(Rules rules)
    {
        private readonly StringBuilder _output = new();
        private readonly List<string> _open = [];

        // How many of each name are open, and how many open elements a list item stops at (see MakeRoomFor): what
        // lets a look down the stack for something that is not there end before it starts.
        private readonly Dictionary<string, int> _openCounts = new(StringComparer.Ordinal);
        private int _listItemStops;

        // The element being dropped with its content, and how many of its own name are open inside it.
        private string? _skipping;
        private int _skipDepth;

        private string? Current => _open.Count == 0 ? null : _open[^1];

        private bool InTableContext => Current is { } current && TableContextTags.Contains(current);

        public void Text(string text)
        {
            if (_skipping is not null || text.Length == 0) return;

            // Only whitespace stays where it is directly inside table structure; the browser carries any other text
            // out in front of the table.
            if (InTableContext && text.All(IsHtmlWhitespace) is false) return;

            AppendText(text);
        }

        public void StartTag(Tag tag)
        {
            var name = tag.Name;

            if (_skipping is not null)
            {
                if (name == _skipping && VoidTags.Contains(name) is false) _skipDepth++;
                return;
            }

            if (DroppedTags.Contains(name))
            {
                if (VoidTags.Contains(name) is false)
                {
                    _skipping = name;
                    _skipDepth = 1;
                }
                return;
            }

            // A tag the policy does not allow is unwrapped: it goes, what is inside it stays.
            if (rules.AllowsTag(name) is false) return;

            var attributes = rules.FilterAttributes(tag);
            if (attributes is null) return;

            // Past the depth cap the element is unwrapped, before it can close anything (see MaxDepth).
            if (_open.Count >= MaxDepth && VoidTags.Contains(name) is false) return;

            if (MakeRoomFor(name) is false) return;

            AppendStartTag(name, attributes);
            if (VoidTags.Contains(name) is false)
            {
                Push(name);
            }
        }

        public void RawTextElement(Tag tag, string content)
        {
            if (_skipping is not null) return;

            // An embed keeps none of its raw text, which is never shown; one the policy or its host rules out goes.
            if (tag.Name == "iframe")
            {
                if (rules.AllowsTag("iframe") is false) return;

                var attributes = rules.FilterAttributes(tag);
                if (attributes is null || MakeRoomFor("iframe") is false) return;

                AppendStartTag("iframe", attributes);
                _output.Append("</iframe>");
                return;
            }

            // Not allowed, so unwrapped: what was inside it is text.
            if (tag.Name is "textarea" or "noframes")
            {
                Text(content);
            }
        }

        public void EndTag(string name)
        {
            if (_skipping is not null)
            {
                if (name == _skipping && --_skipDepth == 0) _skipping = null;
                return;
            }

            if (HeadingTags.Contains(name))
            {
                // Any heading's end tag ends whichever heading is open.
                if (HeadingTags.Any(IsOpen)) CloseInScope(HeadingTags.Contains);
                return;
            }

            // Every rule below closes an element of this name or nothing.
            if (IsOpen(name) is false) return;

            if (name == "table" || TableStructureTags.Contains(name))
            {
                for (var k = _open.Count - 1; k >= 0; k--)
                {
                    if (_open[k] == name)
                    {
                        CloseFrom(k);
                        return;
                    }
                    if (_open[k] == "table") return;
                }
                return;
            }

            if (name == "li")
            {
                for (var k = _open.Count - 1; k >= 0; k--)
                {
                    if (_open[k] == "li")
                    {
                        CloseFrom(k);
                        return;
                    }
                    if (ScopeBoundaries.Contains(_open[k]) || _open[k] is "ul" or "ol") return;
                }
                return;
            }

            if (SpecialTags.Contains(name) || FormattingTags.Contains(name))
            {
                CloseInScope(name);
                return;
            }

            // Any other end tag closes its element only when no special element stands in between.
            for (var k = _open.Count - 1; k >= 0; k--)
            {
                if (_open[k] == name)
                {
                    CloseFrom(k);
                    return;
                }
                if (SpecialTags.Contains(_open[k])) return;
            }
        }

        public string Finish()
        {
            CloseFrom(0);
            return _output.ToString();
        }

        /// <summary>
        /// Closes, explicitly, whatever the browser would close on its own as this start tag arrives, so the two
        /// stacks stay the same; false when the element cannot go where the writer is (it is then unwrapped).
        /// </summary>
        private bool MakeRoomFor(string name)
        {
            if (TableStructureTags.Contains(name))
            {
                if (IsOpen("table") is false) return false;

                var table = _open.LastIndexOf("table");

                var rowGroup = table + 1 < _open.Count && RowGroupTags.Contains(_open[table + 1]) ? table + 1 : table;
                switch (name)
                {
                    case "td" or "th":
                        var row = _open.LastIndexOf("tr");
                        if (row > table)
                        {
                            CloseAbove(row);
                        }
                        else
                        {
                            // A cell needs a row, which the browser would add on its own.
                            OpenRowGroup(table, rowGroup);
                            AppendStartTag("tr", []);
                            Push("tr");
                        }
                        break;
                    case "tr":
                        OpenRowGroup(table, rowGroup);
                        break;
                    case "col":
                        if (table + 1 < _open.Count && _open[table + 1] == "colgroup")
                        {
                            CloseAbove(table + 1);
                        }
                        else
                        {
                            // A column needs a group, which the browser would add on its own.
                            CloseAbove(table);
                            AppendStartTag("colgroup", []);
                            Push("colgroup");
                        }
                        break;
                    default:
                        CloseAbove(table);
                        break;
                }
                return true;
            }

            if (name == "table" && InTableContext)
            {
                // A table straight inside another's structure ends that one first.
                CloseFrom(_open.LastIndexOf("table"));
            }

            if (InTableContext) return false;

            if (name == "li")
            {
                // The browser closes the open list item, looking through anything but a special element for it - and
                // past the surface into the page when no list stands in the way, so one without a list is unwrapped.
                if (_listItemStops == 0) return false;

                var placed = false;
                for (var k = _open.Count - 1; k >= 0; k--)
                {
                    if (_open[k] == "li")
                    {
                        CloseFrom(k);
                        placed = true;
                        break;
                    }
                    if (SpecialTags.Contains(_open[k]) && _open[k] is not "div" and not "p")
                    {
                        placed = true;
                        break;
                    }
                }
                if (placed is false) return false;
            }

            if (name is "li" or "table" || ParagraphClosers.Contains(name))
            {
                CloseInScope("p");
            }

            if (HeadingTags.Contains(name) && Current is { } current && HeadingTags.Contains(current))
            {
                CloseFrom(_open.Count - 1);
            }

            if (name == "a")
            {
                // A link inside a link ends the outer one.
                CloseInScope("a");
            }

            return true;
        }

        // Leaves the table's row group open for a row to go into, writing the tbody the browser would add on its own
        // when the table has none open.
        private void OpenRowGroup(int table, int rowGroup)
        {
            CloseAbove(rowGroup);
            if (rowGroup == table)
            {
                AppendStartTag("tbody", []);
                Push("tbody");
            }
        }

        private bool IsOpen(string name) => _openCounts.GetValueOrDefault(name) > 0;

        private void Push(string name)
        {
            _open.Add(name);
            _openCounts[name] = _openCounts.GetValueOrDefault(name) + 1;
            if (StopsListItem(name)) _listItemStops++;
        }

        // What the browser's search for a list item to close stops at: an open list item, or a special element other
        // than a div or a paragraph (a list, above all).
        private static bool StopsListItem(string name) => SpecialTags.Contains(name) && name is not "div" and not "p";

        private void CloseInScope(string name)
        {
            if (IsOpen(name)) CloseInScope(open => open == name);
        }

        // Closes the nearest open element matching, unless a scope boundary (a table, a cell, a caption) is nearer.
        private void CloseInScope(Func<string, bool> match)
        {
            for (var k = _open.Count - 1; k >= 0; k--)
            {
                if (match(_open[k]))
                {
                    CloseFrom(k);
                    return;
                }
                if (ScopeBoundaries.Contains(_open[k])) return;
            }
        }

        // Closes the element at the index and everything opened inside it, innermost first.
        private void CloseFrom(int index)
        {
            while (_open.Count > index)
            {
                var name = _open[^1];
                _output.Append("</").Append(name).Append('>');
                _open.RemoveAt(_open.Count - 1);
                _openCounts[name]--;
                if (StopsListItem(name)) _listItemStops--;
            }
        }

        // Closes everything opened inside the element at the index, leaving it open.
        private void CloseAbove(int index) => CloseFrom(index + 1);

        private void AppendStartTag(string name, List<KeyValuePair<string, string>> attributes)
        {
            _output.Append('<').Append(name);
            foreach (var (attribute, value) in attributes)
            {
                _output.Append(' ').Append(attribute).Append("=\"");
                foreach (var c in value)
                {
                    _output.Append(c switch
                    {
                        '&' => "&amp;",
                        '"' => "&quot;",
                        '<' => "&lt;",
                        '>' => "&gt;",
                        ' ' => "&nbsp;",
                        _ => c.ToString()
                    });
                }
                _output.Append('"');
            }
            _output.Append('>');
        }

        private void AppendText(string text)
        {
            foreach (var c in text)
            {
                switch (c)
                {
                    case '&': _output.Append("&amp;"); break;
                    case '<': _output.Append("&lt;"); break;
                    case '>': _output.Append("&gt;"); break;
                    case ' ': _output.Append("&nbsp;"); break;
                    default: _output.Append(c); break;
                }
            }
        }
    }
}
