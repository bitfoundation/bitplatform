using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// Reads the XML documentation the Bit.Brouter build emits next to its assembly and flattens each
/// entry into plain text: <c>&lt;see cref="T:Bit.Brouter.IBrouter"/&gt;</c> becomes
/// <c>IBrouter</c>, <c>&lt;c&gt;</c> spans become their content, and paragraphs become blank
/// lines. That text is the same prose a developer reads in IntelliSense, which makes it the most
/// accurate answer an MCP client can get about a member - no second copy to keep in sync.
/// </summary>
public static partial class BrouterXmlDocs
{
    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load);

    /// <summary>The flattened &lt;summary&gt; of a documented member, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary");

    /// <summary>The flattened &lt;remarks&gt; of a documented member, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks");

    private static string? GetSection(string documentationId, string section)
    {
        if (_members.Value.TryGetValue(documentationId, out var member) is false)
        {
            // Overloads are told apart by their parameter list; when building it did not produce an
            // exact hit (generics, modifiers), the first overload's documentation still beats none.
            var prefix = $"{documentationId}(";
            member = _members.Value.FirstOrDefault(m => m.Key.StartsWith(prefix, StringComparison.Ordinal)).Value;

            if (member is null) return null;
        }

        var element = member.Element(section);

        return element is null ? null : Flatten(element);
    }

    private static FrozenDictionary<string, XElement> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{typeof(BrouterLink).Assembly.GetName().Name}.xml");

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

        Write(element, builder);

        var text = builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);

        // The source wraps its prose across lines, so a single newline is just a space - while the
        // blank line a <para> emits is a real paragraph break and has to survive.
        text = WrappedLineRegex().Replace(text, " ");
        text = ParagraphBreakRegex().Replace(text, "\n\n");
        text = RepeatedSpaceRegex().Replace(text, " ");

        return text.Trim();
    }

    private static void Write(XNode node, StringBuilder builder)
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

                    case "para":
                        builder.Append('\n');
                        foreach (var child in element.Nodes()) Write(child, builder);
                        builder.Append('\n');
                        return;

                    case "code":
                        builder.Append('\n').Append(element.Value.Trim()).Append('\n');
                        return;

                    case "param" or "typeparam":
                        builder.Append('\n').Append(element.Attribute("name")?.Value).Append(": ");
                        foreach (var child in element.Nodes()) Write(child, builder);
                        return;

                    default:
                        foreach (var child in element.Nodes()) Write(child, builder);
                        return;
                }
        }
    }

    /// <summary>Turns a cref such as "T:Bit.Brouter.BrouterOptions.ScrollBehavior" into "BrouterOptions.ScrollBehavior".</summary>
    private static string Reference(XElement element)
    {
        var target = element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? element.Value;

        if (string.IsNullOrEmpty(target)) return string.Empty;

        var kind = target.Length > 2 && target[1] == ':' ? target[0] : '\0';
        if (kind != '\0') target = target[2..];

        // Drop the arity marker of a generic type ("BrouterTypeRouteConstraint`1").
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
