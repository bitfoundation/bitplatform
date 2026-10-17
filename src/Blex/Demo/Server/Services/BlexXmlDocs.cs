using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// Reads the XML documentation the Bit.Blex builds emit next to their assemblies and flattens each
/// entry into plain text: <c>&lt;see cref="T:Bit.Blex.BlexManager"/&gt;</c> becomes
/// <c>BlexManager</c>, <c>&lt;c&gt;</c> spans become their content, and paragraphs become blank
/// lines. That text is the same prose a developer reads in IntelliSense, which makes it the most
/// accurate answer an MCP client can get about a member - no second copy to keep in sync.
/// </summary>
public static partial class BlexXmlDocs
{
    private const char CodeMarker = (char)1;

    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load);
    private static readonly Lazy<FrozenDictionary<string, XElement>> _overloads = new(BuildOverloads);

    /// <summary>The flattened &lt;summary&gt; of a documented member, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary");

    /// <summary>The flattened &lt;remarks&gt; of a documented member, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks");

    /// <summary>How many members are documented across every Bit.Blex assembly - zero means the XML files are missing.</summary>
    public static int Count => _members.Value.Count;

    private static string? GetSection(string documentationId, string section)
    {
        // Overloads are told apart by their parameter list; when building it did not produce an exact
        // hit (generics, modifiers), one overload's documentation still beats none.
        if (_members.Value.TryGetValue(documentationId, out var member) is false &&
            _overloads.Value.TryGetValue(documentationId, out member) is false) return null;

        var element = member.Element(section);

        return element is null ? null : Flatten(element);
    }

    /// <summary>
    /// Every documented method by its id without the parameter list, so an inexact id still finds an
    /// overload. The candidates are ordered first so the same overload wins on every build.
    /// </summary>
    private static FrozenDictionary<string, XElement> BuildOverloads()
    {
        return _members.Value.Where(m => m.Key.Contains('(', StringComparison.Ordinal))
                             .OrderBy(m => m.Key, StringComparer.Ordinal)
                             .GroupBy(m => m.Key[..m.Key.IndexOf('(', StringComparison.Ordinal)], StringComparer.Ordinal)
                             .ToFrozenDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, XElement> Load()
    {
        var members = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var assembly in BlexApiCatalog.Assemblies)
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

            if (File.Exists(path) is false) continue;

            try
            {
                foreach (var member in XDocument.Load(path).Descendants("member"))
                {
                    var name = member.Attribute("name")?.Value;
                    if (name is not null) members.TryAdd(name, member);
                }
            }
            catch (Exception)
            {
                // Documentation is a nicety: a malformed or half-written file must not take the tools down.
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
        for (int i = 0; i < codeSamples.Count; i++)
        {
            text = text.Replace(CodePlaceholder(i), codeSamples[i], StringComparison.Ordinal);
        }

        return text.Trim();
    }

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

                    case "para":
                        builder.Append('\n');
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append('\n');
                        return;

                    case "code":
                        builder.Append("\n\n").Append(CodePlaceholder(codeSamples.Count)).Append("\n\n");
                        codeSamples.Add(element.Value.Trim());
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

    /// <summary>Turns a cref such as "P:Bit.Blex.BlexOptions.DevToolsName" into "BlexOptions.DevToolsName".</summary>
    private static string Reference(XElement element)
    {
        var target = element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? element.Value;

        if (string.IsNullOrEmpty(target)) return string.Empty;

        var kind = target.Length > 2 && target[1] == ':' ? target[0] : '\0';
        if (kind != '\0') target = target[2..];

        // A method cref carries its parameter list; fully qualified argument types read as noise in
        // prose, and the '.'s inside them would otherwise be split on below.
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
