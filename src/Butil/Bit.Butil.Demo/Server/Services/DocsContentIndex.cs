using System.Net;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Bit.Butil.Demo.Client.Docs;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// The corpus behind the site's own search box: every documentation page broken into the pieces a
/// reader actually looks for - the page, each of its sections, and each row of its API reference.
/// <para>
/// The site used to search nothing but the nav taxonomy - ninety-seven titles and ninety-seven
/// one-line summaries - which finds a page only for someone who already knows what it is called.
/// The words a reader types are in the pages: "unsanitized" is a sentence on the Clipboard page,
/// keeping the screen awake is a section on WakeLock, <c>ReadText</c> is a row of a table. This
/// indexes that text, and anchors each hit at the section it came from rather than at the top of
/// the page.
/// </para>
/// <para>
/// The text is read out of the pages' own embedded source (see <see cref="ButilSourceCatalog"/>),
/// the way <see cref="ButilSearchIndex"/> reads it for the MCP tools, so nothing here is a second
/// copy of the documentation that could drift from what the page renders. It is built once, served
/// to the browser as one JSON document, and searched in the browser: a docs corpus this size
/// compresses to less than a photograph, and searching it locally is what keeps the result list
/// following the keystrokes.
/// </para>
/// </summary>
public static partial class DocsContentIndex
{
    /// <summary>The anchor <c>Shared/ApiTable.razor</c> gives its section.</summary>
    private const string ApiTableAnchor = "api-section";

    // PublicationOnly for the same reason ButilSearchIndex uses it: a failed build is retried by the
    // next caller rather than cached and rethrown for the lifetime of the process.
    private static readonly Lazy<Payload> _payload = new(BuildPayload, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Web defaults, named here rather than left to the controller: the client deserializes with
    /// <c>HttpClient.GetFromJsonAsync</c>, whose defaults are these, and a mismatch between the two
    /// ends is a corpus that arrives with every field null.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The index as it goes over the wire, built once: the JSON, the same JSON gzipped for the
    /// browsers that ask for it - this is prose, so it compresses to a fraction of itself - and the
    /// ETag that lets a returning visitor revalidate instead of downloading it again.
    /// </summary>
    /// <param name="ETag">Quoted, ready to be written into the header.</param>
    public sealed record Payload(byte[] Json, byte[] Gzip, string ETag);

    /// <inheritdoc cref="Payload"/>
    public static Payload Wire => _payload.Value;

    /// <summary>The entries as objects, for anything that wants them instead of the bytes.</summary>
    public static DocsSearchEntry[] Entries => Build();

    /// <summary>
    /// Builds the index ahead of the first visitor who opens the search box, since it parses every
    /// page on the site. Startup has the time to spare; the person typing does not.
    /// </summary>
    public static void Warm() => _ = _payload.Value;

    private static Payload BuildPayload()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new DocsSearchIndex(Build()), _jsonOptions);

        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(json);
        }

        return new Payload(json, buffer.ToArray(), $"\"{Convert.ToHexString(SHA256.HashData(json))[..16]}\"");
    }

    private static DocsSearchEntry[] Build()
    {
        var entries = new List<DocsSearchEntry>(1024);

        foreach (var group in DocsNav.Groups)
        {
            foreach (var link in group.Links)
            {
                var source = ButilSourceCatalog.GetSourceFile($"Demo/Client/Pages/{link.PageType.Name}.razor") ?? string.Empty;
                var markup = Markup(source);

                AddPage(entries, group, link, markup);
                AddSections(entries, group, link, markup);
                AddApiMembers(entries, group, link, markup);
            }
        }

        return [.. entries];
    }

    /// <summary>
    /// The page itself. Its body is what belongs to no section - the lead, the callouts, whatever
    /// prose the page writes around them - rather than the whole page: a page entry that contained
    /// everything its sections contain would match every query one of them matches, and then push
    /// the section that actually answers it out of the list.
    /// </summary>
    private static void AddPage(List<DocsSearchEntry> entries, DocGroup group, DocLink link, string markup)
    {
        var header = Elements(markup, "PageHeader").FirstOrDefault();
        var lead = Attribute(header.Attributes, "Lead");
        var inject = Attribute(header.Attributes, "InjectAs");

        // Only the titles: a callout's body is a text node, so it is already in what Outside keeps.
        var callouts = Elements(markup, "Callout").Select(callout => Attribute(callout.Attributes, "Title"));

        entries.Add(new DocsSearchEntry(
            Title: link.Title,
            Page: null,
            Url: $"/{link.Url}",
            Group: group.Title,
            Keywords: Join(link.Url.Replace('-', ' '), string.Join(' ', link.TypeNames()), inject, link.Support.Label()),
            // The lead is the page's own sentence about itself and is written to be read; the
            // taxonomy's summary stands in for the few pages that have no header.
            Summary: string.IsNullOrWhiteSpace(lead) ? link.Summary : lead,
            Body: Join(link.Summary, string.Join('\n', callouts), Text(Outside(markup)))));
    }

    /// <summary>
    /// One entry per <c>DemoSection</c>, anchored at the id the section gives itself, so a hit opens
    /// the page scrolled to the paragraph it matched rather than at the top of it.
    /// </summary>
    private static void AddSections(List<DocsSearchEntry> entries, DocGroup group, DocLink link, string markup)
    {
        foreach (var section in Elements(markup, "DemoSection"))
        {
            var title = Attribute(section.Attributes, "Title");
            if (string.IsNullOrWhiteSpace(title)) continue;

            entries.Add(new DocsSearchEntry(
                Title: title,
                Page: link.Title,
                Url: $"/{link.Url}#{Slug(title)}",
                Group: group.Title,
                Keywords: Join(Attribute(section.Attributes, "Api"), string.Join(' ', link.TypeNames())),
                Summary: Attribute(section.Attributes, "Description") ?? string.Empty,
                // The code sample and the live demo's own labels: someone who remembers a call they
                // saw on this site remembers how it was written, not which heading it was under.
                Body: Join(Attribute(section.Attributes, "Code"), Text(section.Inner))));
        }
    }

    /// <summary>
    /// One entry per row of the page's API reference table - the exact names and signatures, which
    /// is what someone types when they know what they want and only need to be shown where it is.
    /// </summary>
    private static void AddApiMembers(List<DocsSearchEntry> entries, DocGroup group, DocLink link, string markup)
    {
        foreach (var member in Elements(markup, "ApiMember"))
        {
            var name = Attribute(member.Attributes, "Name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            entries.Add(new DocsSearchEntry(
                Title: name,
                Page: link.Title,
                Url: $"/{link.Url}#{ApiTableAnchor}",
                Group: group.Title,
                Keywords: Join(Attribute(member.Attributes, "Signature"), string.Join(' ', link.TypeNames())),
                Summary: Attribute(member.Attributes, "Description") ?? string.Empty,
                Body: string.Empty));
        }
    }

    private static string Join(params string?[] parts)
        => string.Join(' ', parts.Where(part => string.IsNullOrWhiteSpace(part) is false)).Trim();

    /// <summary>The anchor a section gives itself - kept in step with <c>Shared/DemoSection.razor</c>.</summary>
    private static string Slug(string title) => string.Join('-',
        string.Concat(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-'))
              .Split('-', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The markup half of a page: everything before its <c>@code</c> block, with the Razor comments
    /// dropped.
    /// <para>
    /// The code-behind is the demo's plumbing - field names, event handlers, the JSON it logs to its
    /// console - and none of it is what a reader is searching for; the comments are notes to whoever
    /// maintains the page. Indexing either only makes every page match more queries, which is the
    /// same as making the search worse. The MCP server indexes the full source for agents, which is
    /// a different reader with a different question - see <see cref="ButilSearchIndex"/>.
    /// </para>
    /// </summary>
    private static string Markup(string source)
    {
        var withoutComments = RazorCommentRegex().Replace(source, " ");
        var code = IndexOfCodeBlock(withoutComments);

        return code < 0 ? withoutComments : withoutComments[..code];
    }

    /// <summary>
    /// Where a page's own <c>@code</c> block starts - skipping the raw string literals on the way,
    /// because half the samples on this site are components and a component sample has a
    /// <c>@code</c> block of its own, at the start of a line, inside a quoted attribute. Stopping at
    /// the first one that looks right would cut a page off at its first code sample and hide
    /// everything below it, which is most of the page.
    /// </summary>
    private static int IndexOfCodeBlock(string source)
    {
        const string code = "@code";

        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == '"' && i + 2 < source.Length && source[i + 1] == '"' && source[i + 2] == '"')
            {
                var terminator = source.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                if (terminator < 0) return -1;

                i = terminator + 2;
                continue;
            }

            if (source[i] != '@' || source.AsSpan(i).StartsWith(code, StringComparison.Ordinal) is false) continue;

            // The real one opens a block at the start of a line; "@code" mentioned mid-sentence in
            // a paragraph about Razor is prose.
            var lineStart = i == 0 ? 0 : source.LastIndexOf('\n', i - 1) + 1;
            if (source.AsSpan(lineStart, i - lineStart).IsWhiteSpace() is false) continue;

            var brace = i + code.Length;
            while (brace < source.Length && char.IsWhiteSpace(source[brace])) brace++;

            if (brace < source.Length && source[brace] == '{') return i;
        }

        return -1;
    }

    /// <summary>
    /// A page's markup with its sections and its API table cut out, so the page entry keeps the
    /// prose that is only on the page - a paragraph between two sections, the note under the header -
    /// without swallowing the text that already has an entry of its own.
    /// </summary>
    private static string Outside(string markup)
    {
        var kept = new StringBuilder(markup.Length);
        var cut = 0;

        foreach (var element in Elements(markup, "DemoSection").Concat(Elements(markup, "ApiTable")).OrderBy(e => e.Start))
        {
            // The two lists are each in reading order but their concatenation is not sorted by the
            // time the spans overlap - they never nest, so an element that starts before the cut has
            // already been removed.
            if (element.Start < cut) continue;

            kept.Append(markup[cut..element.Start]);
            cut = element.End;
        }

        return kept.Append(markup[cut..]).ToString();
    }

    /// <param name="Attributes">The text of the opening tag, from the end of the tag name to its end.</param>
    /// <param name="Inner">What the element wraps, or empty when it is self-closing.</param>
    /// <param name="Start">Where the element begins in the markup it was found in.</param>
    /// <param name="End">Where it ends, exclusive.</param>
    private readonly record struct Element(string Attributes, string Inner, int Start, int End);

    /// <summary>
    /// Every occurrence of one component in a page's markup.
    /// <para>
    /// A regex cannot do this: an attribute of these components is as likely to be a raw string
    /// holding a whole code sample - quotes, angle brackets and all - as it is to be a word, and
    /// that sample would end the tag early for any pattern that looks for the next <c>&gt;</c>.
    /// Scanning is what can tell a delimiter from the same character inside a string.
    /// </para>
    /// </summary>
    private static List<Element> Elements(string markup, string name)
    {
        var elements = new List<Element>();
        var open = $"<{name}";
        var close = $"</{name}>";

        var index = markup.IndexOf(open, StringComparison.Ordinal);

        while (index >= 0)
        {
            // "<Callout" must not match "<CalloutHeading": the tag name ends where the name ends.
            var after = index + open.Length;
            if (after < markup.Length && (char.IsLetterOrDigit(markup[after]) || markup[after] == '_'))
            {
                index = markup.IndexOf(open, after, StringComparison.Ordinal);
                continue;
            }

            var end = EndOfOpenTag(markup, after, out var selfClosing);
            if (end < 0) break;

            var attributes = markup[after..end];
            var inner = string.Empty;
            var next = end + 1;

            if (selfClosing is false)
            {
                var closing = markup.IndexOf(close, next, StringComparison.Ordinal);
                if (closing >= 0)
                {
                    inner = markup[next..closing];
                    next = closing + close.Length;
                }
            }

            elements.Add(new Element(attributes, inner, index, next));

            index = markup.IndexOf(open, next, StringComparison.Ordinal);
        }

        return elements;
    }

    /// <summary>
    /// The index of the <c>&gt;</c> that closes an opening tag, skipping over anything quoted.
    /// Returns -1 when the tag is never closed, which would mean the page did not compile.
    /// </summary>
    private static int EndOfOpenTag(string markup, int start, out bool selfClosing)
    {
        selfClosing = false;

        for (var i = start; i < markup.Length; i++)
        {
            var c = markup[i];

            // A C# raw string literal: three quotes to three quotes, and everything between them -
            // single quotes and '>' included - is text.
            if (c == '"' && i + 2 < markup.Length && markup[i + 1] == '"' && markup[i + 2] == '"')
            {
                var terminator = markup.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                if (terminator < 0) return -1;

                i = terminator + 2;
                continue;
            }

            if (c == '"')
            {
                var terminator = markup.IndexOf('"', i + 1);
                if (terminator < 0) return -1;

                i = terminator;
                continue;
            }

            if (c != '>') continue;

            selfClosing = markup[start..i].TrimEnd().EndsWith('/');

            return i;
        }

        return -1;
    }

    /// <summary>
    /// The value of one attribute of an opening tag. Both spellings of a Razor attribute are
    /// accepted - the plain <c>Title="..."</c> and the expression <c>Code=@("...")</c>, whose string
    /// is as often a raw one holding a multi-line sample.
    /// </summary>
    private static string? Attribute(string? attributes, string name)
    {
        if (string.IsNullOrEmpty(attributes)) return null;

        var match = AttributeRegex(name).Match(attributes);
        if (match.Success is false) return null;

        var value = match.Groups["raw"].Success ? match.Groups["raw"].Value
                  : match.Groups["expression"].Success ? match.Groups["expression"].Value
                  : match.Groups["plain"].Value;

        return WebUtility.HtmlDecode(value).Trim();
    }

    /// <summary>
    /// One attribute by name. Built per call rather than declared with <c>[GeneratedRegex]</c>
    /// because the name is the pattern; the cache the Regex class keeps is what stops that from
    /// costing a compile each time.
    /// </summary>
    private static Regex AttributeRegex(string name) => new(
        // Four quotes delimit it: the pattern itself contains the three that delimit a raw string,
        // because a raw string is one of the three ways a page writes an attribute value.
        $$""""\b{{Regex.Escape(name)}}\s*=\s*(?:@\(\s*"""(?<raw>[\s\S]*?)"""\s*\)|@\(\s*"(?<expression>[^"]*)"\s*\)|"(?<plain>[^"]*)")"""",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// The words inside a fragment of markup: its text nodes and nothing else.
    /// <para>
    /// The tags around them are the page's plumbing - element names, CSS classes, event bindings -
    /// and a corpus that contains them matches "div" and "button" on every page. What is left is
    /// what a visitor can actually see: the labels of the demo's controls, its headings, its prose.
    /// </para>
    /// </summary>
    private static string Text(string markup)
    {
        if (string.IsNullOrWhiteSpace(markup)) return string.Empty;

        var text = new StringBuilder(markup.Length);
        var depth = 0;

        foreach (var c in markup)
        {
            if (c == '<') { depth++; continue; }
            if (c == '>') { if (depth > 0) depth--; text.Append(' '); continue; }

            if (depth == 0) text.Append(c);
        }

        // Razor expressions - @_writeText, @onclick - are identifiers, not words anyone searches for.
        var words = WebUtility.HtmlDecode(text.ToString())
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.StartsWith('@') is false);

        return string.Join(' ', words);
    }

    /// <summary>A Razor comment: <c>@* ... *@</c>, across as many lines as it likes.</summary>
    [GeneratedRegex(@"@\*[\s\S]*?\*@")]
    private static partial Regex RazorCommentRegex();
}
