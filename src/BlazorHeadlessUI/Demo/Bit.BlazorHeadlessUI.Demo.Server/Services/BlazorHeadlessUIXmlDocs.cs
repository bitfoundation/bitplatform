using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.BlazorHeadlessUI.Demo.Server.Services;

/// <summary>
/// Reads the XML documentation the Bit.BlazorHeadlessUI build emits next to its assembly and turns
/// an entry into Markdown: <c>&lt;see cref="T:Bit.BlazorHeadlessUI.BitBhMenuItem"/&gt;</c> becomes
/// <c>`BitBhMenuItem`</c>, <c>&lt;c&gt;</c> spans become code spans, a <c>&lt;list&gt;</c> becomes
/// bullets and a <c>&lt;code&gt;</c> sample a fenced block. That text is the same prose a developer
/// reads in IntelliSense, so there is no second copy of it to keep in sync.
/// <para>
/// The file is copied next to the server's assembly from the library's build output, and into a
/// publish by PublishReferencesDocumentationFiles (set in the csproj).
/// </para>
/// </summary>
public static partial class BlazorHeadlessUIXmlDocs
{
    private const char CodeMarker = (char)1;
    private const char BulletMarker = (char)2;

    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<FrozenDictionary<string, XElement>> _overloads = new(BuildOverloads, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Whether the documentation file was found; without it every answer still lists the API, only without prose.</summary>
    public static bool IsAvailable => _members.Value.Count > 0;

    /// <summary>The &lt;summary&gt; of a documented member as Markdown, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary");

    /// <summary>The &lt;remarks&gt; of a documented member as Markdown, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks");

    /// <summary>
    /// The full names of the types a member's summary and remarks point at with <c>&lt;see cref="T:..."/&gt;</c>,
    /// in the order they appear. A component's summary naming its parts is how a part that shares no
    /// name with its family is told where it belongs.
    /// </summary>
    public static string[] GetTypeReferences(string documentationId)
    {
        if (TryGetMember(documentationId, out var member) is false) return [];

        return [.. member.Elements()
                         .Where(element => element.Name.LocalName is "summary" or "remarks")
                         .SelectMany(element => element.Descendants())
                         .Where(element => element.Name.LocalName is "see" or "seealso")
                         .Select(element => element.Attribute("cref")?.Value)
                         .OfType<string>()
                         .Where(cref => cref.StartsWith("T:", StringComparison.Ordinal))
                         .Select(cref => cref[2..])
                         .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The documentation id of a type, with the arity marker generics carry (<c>T:Bit.BlazorHeadlessUI.BitBhListbox`1</c>).</summary>
    public static string IdOf(Type type) => $"T:{FullName(type)}";

    /// <summary>The documentation id of a property, an enum value or a method (without its parameter list).</summary>
    public static string IdOf(MemberInfo member) => member switch
    {
        PropertyInfo property => $"P:{FullName(property.DeclaringType!)}.{property.Name}",
        FieldInfo field => $"F:{FullName(field.DeclaringType!)}.{field.Name}",
        MethodInfo method => $"M:{FullName(method.DeclaringType!)}.{method.Name}",
        _ => $"T:{member.Name}",
    };

    /// <summary>
    /// The summary of a property, following it up the inheritance chain: a part overrides few of the
    /// parameters it inherits, so the documentation of one is nearly always written on the base type
    /// that declared it rather than on the part being asked about.
    /// </summary>
    public static string? GetPropertySummary(Type owner, PropertyInfo property)
    {
        for (var type = owner; type is not null && type != typeof(object); type = type.BaseType)
        {
            if (GetSummary($"P:{FullName(type)}.{property.Name}") is string summary) return summary;
        }

        return null;
    }

    private static string FullName(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        return (definition.FullName ?? $"{definition.Namespace}.{definition.Name}").Replace('+', '.');
    }

    private static string? GetSection(string documentationId, string section)
    {
        if (TryGetMember(documentationId, out var member) is false) return null;

        var element = member.Element(section);

        return element is null ? null : Flatten(element);
    }

    private static bool TryGetMember(string documentationId, out XElement member)
    {
        // A method's id carries its parameter list, which a caller building it from a MethodInfo does
        // not spell the way the compiler does; one overload's documentation beats none.
        return _members.Value.TryGetValue(documentationId, out member!) ||
               _overloads.Value.TryGetValue(documentationId, out member!);
    }

    private static FrozenDictionary<string, XElement> BuildOverloads()
    {
        // Ordered before grouping: a FrozenDictionary enumerates in whatever order it hashed into, so
        // taking "the first" overload off it directly would answer differently per build.
        return _members.Value.Where(m => m.Key.Contains('(', StringComparison.Ordinal))
                             .OrderBy(m => m.Key, StringComparer.Ordinal)
                             .GroupBy(m => m.Key[..m.Key.IndexOf('(', StringComparison.Ordinal)], StringComparer.Ordinal)
                             .ToFrozenDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, XElement> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{typeof(BitBhComponentBase).Assembly.GetName().Name}.xml");

        if (File.Exists(path) is false) return FrozenDictionary<string, XElement>.Empty;

        try
        {
            // PreserveWhitespace: the space in "<b>Hooks:</b> <c>data-state</c>" is a whitespace-only node,
            // which the default load drops, gluing the two words together.
            return XDocument.Load(path, LoadOptions.PreserveWhitespace)
                            .Descendants("member")
                            .Where(member => member.Attribute("name") is not null)
                            .GroupBy(member => member.Attribute("name")!.Value, StringComparer.Ordinal)
                            .ToFrozenDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }
        catch (Exception)
        {
            // Documentation is a nicety: a malformed file must not take the tools down.
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

        // List items were written as paragraphs so the unwrapping above could not merge them; as
        // Markdown they are consecutive bullets.
        text = text.Replace($"\n\n{BulletMarker}", "\n- ", StringComparison.Ordinal)
                   .Replace(BulletMarker.ToString(), "- ", StringComparison.Ordinal);

        // A code sample only stood in as a placeholder while the prose was unwrapped: its own line
        // breaks and indentation are the sample, not source wrapping, and go back in verbatim.
        for (var i = 0; i < codeSamples.Count; i++)
        {
            text = text.Replace($"{CodeMarker}{i}{CodeMarker}", codeSamples[i], StringComparison.Ordinal);
        }

        return text.Trim();
    }

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
                        var reference = Reference(element);
                        builder.Append(element.Attribute("langword") is null && element.Attribute("cref") is null
                                           ? reference
                                           : $"`{reference}`");
                        return;

                    case "paramref" or "typeparamref":
                        builder.Append('`').Append(element.Attribute("name")?.Value).Append('`');
                        return;

                    case "c":
                        builder.Append('`').Append(element.Value).Append('`');
                        return;

                    case "b" or "strong":
                        builder.Append("**");
                        WriteChildren(element, builder, codeSamples);
                        builder.Append("**");
                        return;

                    case "i" or "em":
                        builder.Append('_');
                        WriteChildren(element, builder, codeSamples);
                        builder.Append('_');
                        return;

                    case "br":
                        builder.Append("\n\n");
                        return;

                    case "para":
                        builder.Append("\n\n");
                        WriteChildren(element, builder, codeSamples);
                        builder.Append("\n\n");
                        return;

                    case "list":
                        WriteChildren(element, builder, codeSamples);
                        builder.Append("\n\n");
                        return;

                    case "item" or "listheader":
                        builder.Append("\n\n").Append(BulletMarker);
                        if (element.Element("term") is { } term)
                        {
                            builder.Append("**");
                            WriteChildren(term, builder, codeSamples);
                            builder.Append("** - ");
                            if (element.Element("description") is { } description) WriteChildren(description, builder, codeSamples);
                        }
                        else
                        {
                            WriteChildren(element, builder, codeSamples);
                        }
                        return;

                    case "code":
                        // A block of its own rather than a sentence that happens to continue in code.
                        var sample = Dedent(element.Value);
                        var language = sample.StartsWith('<') || sample.StartsWith('@') ? "razor" : "csharp";
                        builder.Append("\n\n").Append(CodeMarker).Append(codeSamples.Count).Append(CodeMarker).Append("\n\n");
                        codeSamples.Add($"```{language}\n{sample}\n```");
                        return;

                    case "example":
                        builder.Append("\n\n");
                        WriteChildren(element, builder, codeSamples);
                        builder.Append("\n\n");
                        return;

                    default:
                        WriteChildren(element, builder, codeSamples);
                        return;
                }
        }
    }

    private static void WriteChildren(XElement element, StringBuilder builder, List<string> codeSamples)
    {
        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
    }

    /// <summary>The sample's lines with the indentation they all share removed - the doc comment's, not the code's.</summary>
    private static string Dedent(string code)
    {
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        var start = 0;
        var end = lines.Length;
        while (start < end && string.IsNullOrWhiteSpace(lines[start])) start++;
        while (end > start && string.IsNullOrWhiteSpace(lines[end - 1])) end--;

        lines = lines[start..end];

        var indent = lines.Where(line => string.IsNullOrWhiteSpace(line) is false)
                          .Select(line => line.Length - line.TrimStart().Length)
                          .DefaultIfEmpty(0)
                          .Min();

        return string.Join('\n', lines.Select(line => line.Length >= indent ? line[indent..].TrimEnd() : line.Trim()));
    }

    /// <summary>Turns a cref such as "P:Bit.BlazorHeadlessUI.BitBhMenu.Open" into "BitBhMenu.Open".</summary>
    private static string Reference(XElement element)
    {
        var target = element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? element.Value;

        if (string.IsNullOrEmpty(target)) return string.Empty;

        var kind = target.Length > 2 && target[1] == ':' ? target[0] : '\0';
        if (kind != '\0') target = target[2..];

        // A method cref carries its parameter list; the '.'s inside fully qualified argument types
        // would otherwise be split on below.
        var arguments = target.IndexOf('(', StringComparison.Ordinal);
        if (arguments > 0) target = target[..arguments];

        // The arity marker of a generic type ("BitBhListbox`1") or method ("On``1"), wherever it sits.
        target = ArityRegex().Replace(target, string.Empty);

        var parts = target.Split('.');

        // A type reads best on its own; a member keeps the type it belongs to.
        var keep = kind is 'T' or 'N' ? 1 : 2;

        return parts.Length <= keep ? target : string.Join('.', parts[^keep..]);
    }

    /// <summary>A line break with text on both sides of it; a whitespace-only line is a paragraph break, not a wrap.</summary>
    [GeneratedRegex(@"(?<!\n[ \t]*)[ \t]*\n(?![ \t]*\n)[ \t]*")]
    private static partial Regex WrappedLineRegex();

    [GeneratedRegex(@"[ \t]*\n[ \t\n]*")]
    private static partial Regex ParagraphBreakRegex();

    [GeneratedRegex(@"`{1,2}\d+")]
    private static partial Regex ArityRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex RepeatedSpaceRegex();
}
