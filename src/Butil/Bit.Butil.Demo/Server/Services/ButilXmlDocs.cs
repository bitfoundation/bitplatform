using System.Text;
using System.Xml.Linq;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// Reads the XML documentation the Bit.Butil build emits next to its assembly and flattens each
/// entry into plain text: <c>&lt;see cref="T:Bit.Butil.ButilSubscription"/&gt;</c> becomes
/// <c>ButilSubscription</c>, <c>&lt;c&gt;</c> spans become their content, and paragraphs become
/// blank lines. That text is the same prose a developer reads in IntelliSense - and for a library
/// whose every member links to its MDN page and states its preconditions there, it is the most
/// accurate answer an MCP client can get about a member.
/// </summary>
public static partial class ButilXmlDocs
{
    private const char CodeMarker = (char)1;

    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load);
    private static readonly Lazy<FrozenDictionary<string, XElement>> _overloads = new(BuildOverloads);

    /// <summary>The flattened &lt;summary&gt; of a documented member, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary");

    /// <summary>The flattened &lt;remarks&gt; of a documented member, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks");

    private static string? GetSection(string documentationId, string section)
    {
        // Overloads are told apart by their parameter list; when building it did not produce an exact
        // hit (generics, modifiers), one overload's documentation still beats none. The fallback table
        // is keyed without the parameter list, so the id has to shed its own to match.
        if (_members.Value.TryGetValue(documentationId, out var member) is false &&
            _overloads.Value.TryGetValue(WithoutParameters(documentationId), out member) is false) return null;

        var element = member.Element(section);

        return element is null ? null : Flatten(element);
    }

    /// <summary>
    /// Every documented method by its id without the parameter list, so an inexact id still finds an
    /// overload without walking the whole table. Butil leans on optional parameters and overloads
    /// heavily (<c>Fetch</c>, <c>Crypto</c>), so this is the common case rather than the fallback.
    /// Which overload is decided by ordering the candidates - a FrozenDictionary enumerates in
    /// whatever order it hashed into, so taking one off it directly would answer differently per build.
    /// </summary>
    private static FrozenDictionary<string, XElement> BuildOverloads()
    {
        return _members.Value.Where(m => m.Key.Contains('(', StringComparison.Ordinal))
                             .OrderBy(m => m.Key, StringComparer.Ordinal)
                             .GroupBy(m => WithoutParameters(m.Key), StringComparer.Ordinal)
                             .ToFrozenDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
    }

    /// <summary>A documentation id up to its parameter list - the key <see cref="BuildOverloads"/> groups on.</summary>
    private static string WithoutParameters(string documentationId)
    {
        var parameters = documentationId.IndexOf('(', StringComparison.Ordinal);

        return parameters < 0 ? documentationId : documentationId[..parameters];
    }

    private static FrozenDictionary<string, XElement> Load()
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, $"{typeof(BitButil).Assembly.GetName().Name}.xml");

        if (File.Exists(path) is false) return FrozenDictionary<string, XElement>.Empty;

        try
        {
            var document = XDocument.Load(path);

            return document.Descendants("member")
                           .Where(m => m.Attribute("name") is not null)
                           .GroupBy(m => m.Attribute("name")!.Value, StringComparer.Ordinal)
                           .ToFrozenDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        }
        catch (Exception)
        {
            // Documentation is a nicety: a malformed or half-written file must not take the tools down.
            return FrozenDictionary<string, XElement>.Empty;
        }
    }

    private static string Flatten(XElement element)
    {
        var builder = new StringBuilder();
        var codeSamples = new List<string>();

        Write(element, builder, codeSamples);

        var text = builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);

        // The source wraps its prose across lines, so a single newline is just a space - while the
        // blank line a <para> emits is a real paragraph break and has to survive.
        text = WrappedLineRegex().Replace(text, " ");
        text = ParagraphBreakRegex().Replace(text, "\n\n");
        text = RepeatedSpaceRegex().Replace(text, " ");

        // A code sample only stood in as a placeholder while the prose was unwrapped: its own line
        // breaks and indentation are the sample, not source wrapping, and go back in verbatim.
        for (int i = 0; i < codeSamples.Count; i++)
        {
            text = text.Replace(CodePlaceholder(i), codeSamples[i], StringComparison.Ordinal);
        }

        return text.Trim();
    }

    /// <summary>
    /// Stands in for a code sample while the prose is unwrapped. U+0001 never occurs in
    /// documentation text, and none of the whitespace passes above can touch it.
    /// </summary>
    private static string CodePlaceholder(int index) => $"{CodeMarker}{index}{CodeMarker}";

    private static void Write(XNode node, StringBuilder builder, List<string> codeSamples)
    {
        switch (node)
        {
            case XText text:
                builder.Append(text.Value);
                return;

            case XElement element:
                switch (element.Name.LocalName)
                {
                    case "see" or "seealso":
                        builder.Append(Reference(element));
                        return;

                    case "paramref" or "typeparamref":
                        builder.Append(element.Attribute("name")?.Value);
                        return;

                    case "para" or "br":
                        builder.Append('\n');
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append('\n');
                        return;

                    case "code":
                        // Blank lines on both sides, so the sample is a block of its own rather than
                        // a sentence that happens to continue in code.
                        builder.Append("\n\n").Append(CodePlaceholder(codeSamples.Count)).Append("\n\n");
                        codeSamples.Add(element.Value.Trim());
                        return;

                    case "item":
                        // Butil's remarks explain a lot through <list> - a bullet has to survive the
                        // unwrapping pass below, and only a blank line does.
                        builder.Append("\n\n- ");
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append('\n').Append('\n');
                        return;

                    case "param" or "typeparam":
                        builder.Append('\n').Append(element.Attribute("name")?.Value).Append(": ");
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        return;

                    default:
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        return;
                }
        }
    }

    /// <summary>
    /// Turns a cref such as "M:Bit.Butil.Clipboard.WriteText(System.String)" into "Clipboard.WriteText",
    /// and a <c>see href</c> - a link out to the web rather than a documentation id - into a Markdown
    /// link, so the URL survives into the Markdown the tools answer with.
    /// </summary>
    private static string Reference(XElement element)
    {
        // Butil's remarks link to MDN this way constantly. The text of one is prose, not a
        // documentation id, so none of the trimming below applies to it.
        var href = element.Attribute("href")?.Value;
        if (string.IsNullOrWhiteSpace(href) is false)
        {
            var label = element.Value.Trim();

            // Most of these links are written with the URL as their own text, and a Markdown link
            // whose label IS its target is the same string twice - which every answer carrying that
            // member then pays for. An autolink is the same URL once, and unlike a bare one it is a
            // link in plain Markdown rather than only where bare URLs happen to be linkified.
            return label.Length == 0 || string.Equals(label, href, StringComparison.OrdinalIgnoreCase)
                ? $"<{href}>"
                : $"[{label}]({href})";
        }

        var target = element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? element.Value;

        if (string.IsNullOrEmpty(target)) return string.Empty;

        var kind = target.Length > 2 && target[1] == ':' ? target[0] : '\0';
        if (kind != '\0') target = target[2..];

        // A method cref carries its parameter list. Fully qualified argument types read as noise in
        // prose, and the '.'s inside them would otherwise be split on below - which would keep the
        // arguments and drop the owning type.
        var arguments = target.IndexOf('(', StringComparison.Ordinal);
        if (arguments > 0) target = target[..arguments];

        // Drop the arity marker of a generic type.
        var arity = target.IndexOf('`', StringComparison.Ordinal);
        if (arity > 0) target = target[..arity];

        var parts = target.Split('.');

        // A type reads best on its own; a member keeps the type it belongs to.
        var keep = kind is 'T' or 'N' ? 1 : 2;

        return parts.Length <= keep ? target : string.Join('.', parts[^keep..]);
    }

    [GeneratedRegex(@"(?<!\n)[ \t]*\n[ \t]*(?!\n)")]
    private static partial Regex WrappedLineRegex();

    [GeneratedRegex(@"[ \t]*\n[ \t\n]*")]
    private static partial Regex ParagraphBreakRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex RepeatedSpaceRegex();
}
