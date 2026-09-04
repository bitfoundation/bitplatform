using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// Reads the XML documentation the bit BlazorUI build emits next to each of its assemblies and
/// flattens an entry into plain text: <c>&lt;see cref="T:Bit.BlazorUI.BitVariant"/&gt;</c> becomes
/// <c>BitVariant</c>, <c>&lt;c&gt;</c> spans become their content, and <c>&lt;para&gt;</c> becomes a
/// blank line. That text is the same prose a developer reads in IntelliSense, which makes it the
/// most accurate thing an MCP client can be told about a type it is about to use.
/// </summary>
public static partial class BlazorUIXmlDocs
{
    private const char CodeMarker = (char)1;

    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<FrozenDictionary<string, XElement>> _overloads = new(BuildOverloads, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>The flattened &lt;summary&gt; of a documented member, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary");

    /// <summary>The flattened &lt;remarks&gt; of a documented member, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks");

    /// <summary>The documentation id of a type, with the arity marker generics carry (<c>T:Bit.BlazorUI.BitDropdown`2</c>).</summary>
    public static string IdOf(Type type) => $"T:{(type.FullName ?? type.Name).Replace('+', '.')}";

    /// <summary>The documentation id of a property or field declared on <paramref name="declaringType"/>.</summary>
    public static string IdOf(Type declaringType, string memberName, bool isField = false)
    {
        var owner = (declaringType.FullName ?? declaringType.Name).Replace('+', '.');

        return $"{(isField ? 'F' : 'P')}:{owner}.{memberName}";
    }

    /// <summary>
    /// The summary of a property, following it up the inheritance chain. A component overrides very
    /// few of the parameters it inherits, so the documentation of one is nearly always written on
    /// the type that declared it rather than on the one being asked about.
    /// </summary>
    public static string? GetPropertySummary(Type owner, PropertyInfo property)
    {
        for (var type = owner; type is not null; type = type.BaseType)
        {
            var declaring = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            if (GetSummary(IdOf(declaring, property.Name)) is string summary) return summary;

            if (type == property.DeclaringType) break;
        }

        return property.DeclaringType is null || property.DeclaringType == owner
            ? null
            : GetSummary(IdOf(property.DeclaringType, property.Name));
    }

    private static string? GetSection(string documentationId, string section)
    {
        // Overloads are told apart by their parameter list; when an exact id did not hit (generics,
        // modifiers), one overload's documentation still beats none. The fallback table is keyed
        // without the parameter list, so the id has to shed its own to match.
        if (_members.Value.TryGetValue(documentationId, out var member) is false &&
            _overloads.Value.TryGetValue(WithoutParameters(documentationId), out member) is false) return null;

        var element = member.Element(section);

        return element is null ? null : Flatten(element);
    }

    private static FrozenDictionary<string, XElement> BuildOverloads()
    {
        // Ordered before grouping: a FrozenDictionary enumerates in whatever order it hashed into,
        // so taking "the first" overload off it directly would answer differently per build.
        return _members.Value.Where(m => m.Key.Contains('(', StringComparison.Ordinal))
                             .OrderBy(m => m.Key, StringComparer.Ordinal)
                             .GroupBy(m => WithoutParameters(m.Key), StringComparer.Ordinal)
                             .ToFrozenDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
    }

    private static string WithoutParameters(string documentationId)
    {
        var parameters = documentationId.IndexOf('(', StringComparison.Ordinal);

        return parameters < 0 ? documentationId : documentationId[..parameters];
    }

    /// <summary>
    /// Every documented member of every assembly the tools answer from, in one table. The ids are
    /// fully qualified, so the four files cannot collide and merging them costs nothing.
    /// </summary>
    private static FrozenDictionary<string, XElement> Load()
    {
        var members = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var assembly in BlazorUIAssemblies.All)
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

            if (File.Exists(path) is false) continue;

            try
            {
                foreach (var member in XDocument.Load(path).Descendants("member"))
                {
                    if (member.Attribute("name")?.Value is string name) members.TryAdd(name, member);
                }
            }
            catch (Exception)
            {
                // Documentation is a nicety: a malformed or half-written file must not take the
                // tools down, and the other three assemblies still have theirs.
            }
        }

        return members.ToFrozenDictionary(StringComparer.Ordinal);
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
        for (var i = 0; i < codeSamples.Count; i++)
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
                        builder.Append("\n\n").Append(CodePlaceholder(codeSamples.Count)).Append("\n\n");
                        codeSamples.Add(element.Value.Trim());
                        return;

                    case "item":
                        // A bullet has to survive the unwrapping pass above, and only a blank line does.
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
    /// Turns a cref such as "P:Bit.BlazorUI.BitButton.Variant" into "BitButton.Variant", and a
    /// <c>see href</c> - a link out to the web rather than a documentation id - into Markdown, so
    /// the URL survives into what the tools answer with.
    /// </summary>
    private static string Reference(XElement element)
    {
        var href = element.Attribute("href")?.Value;

        if (string.IsNullOrWhiteSpace(href) is false)
        {
            var label = element.Value.Trim();

            // A Markdown link whose label IS its target is the same string twice, which every
            // answer carrying that member then pays for. An autolink is the same URL once.
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
