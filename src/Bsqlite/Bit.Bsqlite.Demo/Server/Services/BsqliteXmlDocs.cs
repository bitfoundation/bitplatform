using System.Text;
using System.Xml.Linq;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Reads the XML documentation the Bit.Bsqlite builds emit next to their assemblies - the core, the
/// three storage packages and the testing kit - and flattens each entry into plain text:
/// <c>&lt;see cref="T:Bit.Bsqlite.Storage.IBsqliteStorageProvider"/&gt;</c> becomes
/// <c>IBsqliteStorageProvider</c>, <c>&lt;c&gt;</c> spans become their content, and paragraphs become
/// blank lines. That text is the same prose a developer reads in IntelliSense, which makes it the most
/// accurate answer an MCP client can get about a member - no second copy to keep in sync.
/// </summary>
public static partial class BsqliteXmlDocs
{
    private const char CodeMarker = (char)1;

    /// <summary>How far an <c>&lt;inheritdoc cref="..."/&gt;</c> chain is followed before giving up on it.</summary>
    private const int MaxInheritDepth = 4;

    private static readonly Lazy<FrozenDictionary<string, XElement>> _members = new(Load);
    private static readonly Lazy<FrozenDictionary<string, XElement>> _overloads = new(BuildOverloads);

    /// <summary>The documentation files read, by assembly name - one per package.</summary>
    public static IEnumerable<string> AssemblyNames => BsqliteApiCatalog.Assemblies.Select(assembly => assembly.GetName().Name!);

    /// <summary>How many documented members were loaded, across every file.</summary>
    public static int Count => _members.Value.Count;

    /// <summary>The flattened &lt;summary&gt; of a documented member, or null.</summary>
    public static string? GetSummary(string documentationId) => GetSection(documentationId, "summary", 0);

    /// <summary>The flattened &lt;remarks&gt; of a documented member, or null.</summary>
    public static string? GetRemarks(string documentationId) => GetSection(documentationId, "remarks", 0);

    private static string? GetSection(string documentationId, string section, int depth)
    {
        // Overloads are told apart by their parameter list; when building it did not produce an exact
        // hit (generics, modifiers), one overload's documentation still beats none.
        if (_members.Value.TryGetValue(documentationId, out var member) is false &&
            _overloads.Value.TryGetValue(documentationId, out member) is false) return null;

        // A member may carry several <remarks> blocks. Every one of them is a caveat, so they are joined
        // rather than all but the first being dropped.
        var elements = member.Elements(section).ToArray();

        if (elements.Length > 0)
        {
            var text = string.Join("\n\n", elements.Select(Flatten).Where(part => part.Length > 0));

            return text.Length == 0 ? null : text;
        }

        // The library's generic overloads say <inheritdoc cref="..."/> and nothing else: the compiler
        // shows the referenced member's text for them, and so does this. An <inheritdoc/> without a
        // cref is resolved by the API catalog, which knows the member's base and interfaces.
        if (depth < MaxInheritDepth && member.Element("inheritdoc")?.Attribute("cref")?.Value is { Length: > 0 } cref)
        {
            return GetSection(cref, section, depth + 1);
        }

        return null;
    }

    /// <summary>
    /// Every documented method by its id without the parameter list, so an inexact id still finds an
    /// overload without walking the whole table. Which overload is decided by ordering the candidates
    /// - a FrozenDictionary enumerates in whatever order it hashed into, so taking one off it
    /// directly would answer differently per build.
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

        // One file per package. A member id is fully qualified, so the files cannot collide; the first
        // one wins anyway rather than letting a duplicate throw.
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
                // down, nor the other files with it.
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
                        builder.Append('\n');
                        foreach (var child in element.Nodes()) Write(child, builder, codeSamples);
                        builder.Append('\n');
                        return;

                    case "code":
                        // Blank lines on both sides, so the sample is a block of its own rather than
                        // a sentence that happens to continue in code.
                        builder.Append("\n\n").Append(CodePlaceholder(codeSamples.Count)).Append("\n\n");
                        codeSamples.Add($"```\n{Dedent(element.Value)}\n```");
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
    /// A code sample without the indentation of the comment it was written in. The compiler keeps
    /// every line's leading spaces, so a sample in an indented doc comment arrives with its first line
    /// flush (it follows the &lt;code&gt; tag) and every other line pushed right by the comment's own
    /// indentation - which reads as a sample whose braces do not line up.
    /// </summary>
    private static string Dedent(string code)
    {
        var lines = code.ReplaceLineEndings("\n").Split('\n').ToList();

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0])) lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);

        if (lines.Count == 0) return string.Empty;

        // The first line is measured only when it is indented itself: one that follows <code> on the
        // same line starts at column 0 whatever the rest of the sample does.
        var measured = lines.Skip(char.IsWhiteSpace(lines[0], 0) ? 0 : 1).Where(line => string.IsNullOrWhiteSpace(line) is false).ToArray();
        var indent = measured.Length == 0 ? 0 : measured.Min(line => line.Length - line.TrimStart().Length);

        return string.Join('\n', lines.Select((line, index) =>
            index == 0 && char.IsWhiteSpace(line, 0) is false ? line : line.Length >= indent ? line[indent..].TrimEnd() : line.Trim()));
    }

    /// <summary>Turns a cref such as "P:Bit.Bsqlite.BsqliteStorageSelection.Candidates" into "BsqliteStorageSelection.Candidates".</summary>
    private static string Reference(XElement element)
    {
        var target = element.Attribute("cref")?.Value ?? element.Attribute("langword")?.Value ?? element.Value;

        if (string.IsNullOrEmpty(target)) return string.Empty;

        var kind = target.Length > 2 && target[1] == ':' ? target[0] : '\0';
        if (kind != '\0') target = target[2..];

        // A method cref carries its parameter list ("M:Bit.Bsqlite.Interop.BsqliteDatabase.OpenAsync(System.Threading.CancellationToken)").
        // Fully qualified argument types read as noise in prose, and the '.'s inside them would
        // otherwise be split on below - which would keep the arguments and drop the owning type.
        var arguments = target.IndexOf('(', StringComparison.Ordinal);
        if (arguments > 0) target = target[..arguments];

        // Drop the arity markers of generic types and methods ("IBsqliteLiveQuery`1.RefreshAsync") wherever
        // they are: cutting the text at the first one would drop the member a member reference is about.
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

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex RepeatedSpaceRegex();

    [GeneratedRegex(@"`+\d+")]
    private static partial Regex ArityRegex();
}
