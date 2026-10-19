using System.Text;
using System.Text.RegularExpressions;
using Bit.Bortable.Demo.Server.Dtos;

namespace Bit.Bortable.Demo.Server.Services;

/// <summary>
/// The CSS classes of the stylesheet Bit.Bortable ships - the container, the item wrapper and the
/// drag states - read out of the file itself, each with the element that carries it and the
/// parameter that replaces it.
/// <para>
/// The colours live in custom properties, which <see cref="BortableThemeCatalog"/> serves; the
/// classes are the other half, and the half a person reaches for first: "the dragged item should be
/// fainter", "the original should not disappear", "the selection needs a border". Four of them are
/// only defaults - <c>GhostClass</c>, <c>ChosenClass</c>, <c>SwapClass</c> and <c>SelectedClass</c> name
/// them - and a list that sets one of those parameters is no longer styled by the stylesheet's rule
/// for that state, which is exactly the surprise worth saying up front. The rest are always rendered
/// under their own names. Which is which is read off the component's parameter defaults, and what
/// each class is set on off the README's table of them, so neither is a list kept here.
/// </para>
/// </summary>
public static partial class BortableStylesheetClasses
{
    /// <summary>How the component is named where a parameter is qualified by it, e.g. "Bortable&lt;TItem&gt;.GhostClass".</summary>
    public const string ComponentName = "Bortable<TItem>";

    private static readonly Lazy<BortableStateClassDto[]> _classes = new(Build);
    private static readonly Lazy<IReadOnlyDictionary<string, string>> _parameterByClass = new(BuildParameterByClass);

    /// <summary>Every class of the stylesheet, once each, in the order the file first names it.</summary>
    public static BortableStateClassDto[] Classes => _classes.Value;

    /// <summary>The classes that are a parameter's default, mapped to that parameter: "bit-bortable-ghost" to "GhostClass".</summary>
    public static IReadOnlyDictionary<string, string> ParameterByClass => _parameterByClass.Value;

    /// <summary>One class by name - "bit-bortable-ghost", ".bit-bortable-ghost" or just "ghost" - or null.</summary>
    public static BortableStateClassDto? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var text = name.Trim().TrimStart('.');

        if (text.StartsWith("bit-bortable", StringComparison.OrdinalIgnoreCase) is false) text = $"bit-bortable-{text}";

        return Classes.FirstOrDefault(cssClass => string.Equals(cssClass.Name, text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>One class in full as Markdown, or null when the stylesheet has no class by that name.</summary>
    public static string? Render(string name)
    {
        var cssClass = Find(name);
        if (cssClass is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# `{cssClass.Name}`").AppendLine();
        builder.AppendLine("A class of the shipped stylesheet, `_content/Bit.Bortable/bit-bortable.css`.").AppendLine();

        if (cssClass.SetOn is not null) builder.AppendLine($"- **Set on** (the README): {cssClass.SetOn}");

        builder.AppendLine(cssClass.Parameter is null
            ? "- **Always rendered under this name** - no parameter replaces it. Restyle it, or override the custom properties its rules read."
            : $"- **Replaced by** `{cssClass.Parameter}`, whose default it is: a list that sets `{cssClass.Parameter}` renders its own class for this state, and the rules below no longer style that list.");

        if (cssClass.Comment is not null) builder.AppendLine($"- **From the stylesheet**: {cssClass.Comment}");

        builder.AppendLine().AppendLine("## Rules").AppendLine();

        foreach (var rule in cssClass.Rules) builder.AppendLine($"- `{rule}`");

        builder.AppendLine();

        if (cssClass.Properties.Length > 0)
        {
            builder.AppendLine("## Custom properties its rules read").AppendLine();

            foreach (var property in cssClass.Properties) builder.AppendLine($"- `{property}` - `GetBortableTheme(name: \"{property}\")`");

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>The library's classes a selector names, in order, without their dots.</summary>
    internal static IEnumerable<string> ClassesIn(string selector)
        => ClassRegex().Matches(selector).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal);

    private static BortableStateClassDto[] Build()
    {
        var rules = BortableThemeCatalog.Rules;
        var setOn = ReadmeSetOn();

        var names = rules.SelectMany(rule => ClassesIn(rule.Selector)).Distinct(StringComparer.Ordinal).ToArray();

        return [.. names.Select(name =>
        {
            var styling = rules.Where(rule => ClassesIn(rule.Selector).Contains(name, StringComparer.Ordinal)).ToArray();

            // A rule's comment is about the first class its selector names - the condition of
            // ":where(.bit-bortable-has-handle) > .bit-bortable-item", not the item every such rule ends in.
            var comment = string.Join(' ', styling.Where(rule => ClassesIn(rule.Selector).First() == name && rule.Comment is not null)
                                                  .Select(rule => rule.Comment)
                                                  .Distinct(StringComparer.Ordinal));

            return new BortableStateClassDto
            {
                Name = name,
                SetOn = setOn.GetValueOrDefault(name),
                Parameter = ParameterByClass.GetValueOrDefault(name),
                Rules = [.. styling.Select(rule => rule.ToString())],
                Comment = comment.Length == 0 ? null : comment,
                Properties = [.. styling.SelectMany(rule => rule.Declarations)
                                        .SelectMany(declaration => BortableThemeCatalog.VariablesIn(declaration.Value))
                                        .Distinct(StringComparer.Ordinal)]
            };
        })];
    }

    /// <summary>
    /// What the README's table of drag-state classes says each one is set on. The table is the
    /// library's own description of its markup, which the stylesheet's comments only half repeat.
    /// </summary>
    private static Dictionary<string, string> ReadmeSetOn()
    {
        var section = BortableSourceCatalog.GetGuideSection("Drag-state classes") ?? string.Empty;

        return ReadmeRowRegex().Matches(section)
                               .GroupBy(match => match.Groups["name"].Value, StringComparer.Ordinal)
                               .ToDictionary(group => group.Key, group => group.First().Groups["on"].Value.Trim(), StringComparer.Ordinal);
    }

    /// <summary>
    /// The string parameters of the component whose default is one of the library's classes - read off
    /// the API reference, so a new state parameter is found by the same reflection that documents it.
    /// </summary>
    private static IReadOnlyDictionary<string, string> BuildParameterByClass()
    {
        var details = BortableApiCatalog.GetTypeDetails(ComponentName);

        return (details?.Members ?? [])
            .Where(member => member.Kind == "Parameter" && member.Type == "string" && member.Default is not null)
            .Select(member => (Class: member.Default!.Trim('"'), Parameter: member.Name))
            .Where(pair => ClassRegex().IsMatch($".{pair.Class}") && ClassRegex().Match($".{pair.Class}").Length == pair.Class.Length + 1)
            .GroupBy(pair => pair.Class, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Parameter, StringComparer.Ordinal);
    }

    [GeneratedRegex(@"\.(?<name>bit-bortable(?:-[a-z0-9]+)*)(?![a-z0-9-])")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^\|\s*`(?<name>bit-bortable[a-z0-9-]*)`(?:\s*\(`\w+`\))?\s*\|\s*(?<on>[^|\n]+?)\s*\|\s*$", RegexOptions.Multiline)]
    private static partial Regex ReadmeRowRegex();
}
