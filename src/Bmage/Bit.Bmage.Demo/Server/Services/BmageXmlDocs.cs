using System.Text;
using System.Xml.Linq;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Reads the XML documentation the three Bit.Bmage builds emit next to their assemblies -
/// Bit.Bmage.Core, Bit.Bmage and Bit.Bmage.Editor - and flattens each entry into plain text:
/// <c>&lt;see cref="T:Bit.Bmage.Pipeline.ImagePipeline"/&gt;</c> becomes <c>ImagePipeline</c>,
/// <c>&lt;c&gt;</c> spans become their content, and paragraphs become blank lines. That text is the
/// same prose a developer reads in IntelliSense, which makes it the most accurate answer an MCP
/// client can get about a member - there is no second copy to keep in sync.
/// </summary>
public static partial class BmageXmlDocs
{
    private const char CodeMarker = (char)1;

    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load);
    private static readonly Lazy<FrozenDictionary<string, XElement>> _overloads = new(BuildOverloads);

    /// <summary>The documentation files read, by assembly name - one per package.</summary>
    public static IEnumerable<string> AssemblyNames => BmageApiCatalog.Assemblies.Select(assembly => assembly.GetName().Name!);

    /// <summary>How many documented members were loaded, across all three files.</summary>
    public static int Count => _members.Value.Count;

    /// <summary>The flattened &lt;summary&gt; of a documented member, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary");

    /// <summary>The flattened &lt;remarks&gt; of a documented member, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks");

    private static string? GetSection(string documentationId, string section)
    {
        // Overloads are told apart by their parameter list; when building it did not produce an
        // exact hit (generics, modifiers), one overload's documentation still beats none.
        if (_members.Value.TryGetValue(documentationId, out var member) is false &&
            _overloads.Value.TryGetValue(documentationId, out member) is false) return null;

        // A member may carry several <remarks> blocks; ImagePipeline's optimiser does. Every one of
        // them is a caveat, so they are joined rather than all but the first being dropped.
        var elements = member.Elements(section).ToArray();

        if (elements.Length == 0) return null;

        var text = string.Join("\n\n", elements.Select(Flatten).Where(part => part.Length > 0));

        return text.Length == 0 ? null : text;
    }

    /// <summary>
    /// Every documented method by its id without the parameter list, so an inexact id still finds an
    /// overload without walking the whole table. Which overload is decided by ordering the
    /// candidates - a FrozenDictionary enumerates in whatever order it hashed into, so taking one
    /// off it directly would answer differently per build.
    /// </summary>
    private static FrozenDictionary<string, XElement> BuildOverloads()
    {
        return _members.Value.Where(member => member.Key.Contains('(', StringComparison.Ordinal))
                             .OrderBy(member => member.Key, StringComparer.Ordinal)
                             .GroupBy(member => member.Key[..member.Key.IndexOf('(', StringComparison.Ordinal)], StringComparer.Ordinal)
                             .ToFrozenDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, XElement> Load()
    {
        var members = new Dictionary<string, XElement>(StringComparer.Ordinal);

        // Three packages, three files. A member id is fully qualified, so the files cannot collide;
        // the first one wins anyway rather than letting a duplicate throw.
        foreach (var name in AssemblyNames)
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"{name}.xml");

            if (File.Exists(path) is false) continue;

            try
            {
                var document = XDocument.Load(path);

                foreach (var member in document.Descendants("member"))
                {
                    if (member.Attribute("name")?.Value is not { } id) continue;

                    members.TryAdd(id, member);
                }
            }
            catch (Exception)
            {
                // Documentation is a nicety: a malformed or half-written file must not take the tools
                // down, nor the other two files with it.
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

                    case "para":
                        // A blank line on either side: a single newline is how the source wraps its
                        // prose, and the unwrapping pass turns one of those back into a space.
                        builder.Append("\n\n");
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append("\n\n");
                        return;

                    case "item":
                        // A list item is a paragraph of its own; run together, the optimiser's list of
                        // rewrite rules reads as one sentence with no boundaries in it.
                        builder.Append("\n\n- ");
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append("\n\n");
                        return;

                    case "code":
                        // Blank lines on both sides, so the sample is a block of its own rather than
                        // a sentence that happens to continue in code.
                        builder.Append("\n\n").Append(CodePlaceholder(codeSamples.Count)).Append("\n\n");
                        codeSamples.Add(Dedent(element.Value));
                        return;

                    case "param" or "typeparam":
                        // Each parameter is a paragraph of its own, for the same reason - and the
                        // trailing break is what separates the last one from prose that follows it.
                        builder.Append("\n\n").Append(element.Attribute("name")?.Value).Append(": ");
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append("\n\n");
                        return;

                    default:
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        return;
                }
        }
    }

    /// <summary>
    /// A &lt;code&gt; sample keeps the indentation of the doc comment it was written in. The common
    /// part of it is the comment's, not the sample's, and is taken off every line.
    /// </summary>
    private static string Dedent(string code)
    {
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        var indent = lines.Where(line => line.Trim().Length > 0)
                          .Select(line => line.Length - line.TrimStart().Length)
                          .DefaultIfEmpty(0)
                          .Min();

        return string.Join('\n', lines.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart())).Trim('\n').TrimEnd();
    }

    /// <summary>Turns a cref such as "M:Bit.Bmage.Pipeline.ImagePipeline.Cover(...)" into "ImagePipeline.Cover".</summary>
    private static string Reference(XElement element)
    {
        var target = element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? element.Value;

        if (string.IsNullOrEmpty(target)) return string.Empty;

        var kind = target.Length > 2 && target[1] == ':' ? target[0] : '\0';
        if (kind != '\0') target = target[2..];

        // A method cref carries its parameter list. Fully qualified argument types read as noise in
        // prose, and the '.'s inside them would otherwise be split on below - which would keep the
        // arguments and drop the owning type.
        var arguments = target.IndexOf('(', StringComparison.Ordinal);
        if (arguments > 0) target = target[..arguments];

        // Drop the arity marker of a generic type or of a generic method, which spells its own with
        // two backticks; only the marker, since a member of a generic type carries it mid-cref.
        target = ArityRegex().Replace(target, string.Empty);

        var parts = target.Split('.');

        // A type reads best on its own; a member keeps the type it belongs to.
        var keep = kind is 'T' or 'N' ? 1 : 2;

        return parts.Length <= keep ? target : string.Join('.', parts[^keep..]);
    }

    [GeneratedRegex(@"(?<!\n)[ \t]*\n[ \t]*(?!\n)")]
    private static partial Regex WrappedLineRegex();

    [GeneratedRegex(@"[ \t]*\n[ \t\n]*")]
    private static partial Regex ParagraphBreakRegex();

    /// <summary>The arity marker in a documentation id: the "`1" of a generic type, the "``1" of a generic method.</summary>
    [GeneratedRegex(@"`{1,2}\d+")]
    private static partial Regex ArityRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex RepeatedSpaceRegex();
}
