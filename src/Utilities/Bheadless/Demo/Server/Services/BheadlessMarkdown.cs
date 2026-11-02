using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Bit.Bheadless.Demo.Client.Core.Shared;

namespace Bit.Bheadless.Demo.Server.Services;

/// <summary>
/// Renders what the tools answer with. Markdown rather than JSON: a parameter table names its columns
/// once instead of once per row, and it is the shape a model reads best.
/// </summary>
public static partial class BheadlessMarkdown
{
    /// <summary>
    /// The longest answer a tool gives. Long enough for the largest family (Menu: eighteen parts) with its
    /// first example, short enough that a couple of calls cannot crowd out a client's context window.
    /// What does not fit is cut with the call that returns the rest.
    /// </summary>
    public const int MaxAnswerLength = 30_000;

    private const string ComponentTool = "GetBheadlessComponent";
    private const string ExamplesTool = "GetBheadlessExamples";

    /// <summary>Every family grouped by area, with the line that says what it is for and the names of its parts.</summary>
    public static string Catalog()
    {
        var families = BheadlessComponentCatalog.Families;
        var builder = new StringBuilder();

        builder.AppendLine($"# bit Bheadless {BheadlessComponentCatalog.Version}: {families.Count(family => family.CssPage is not null)} component families, {families.Sum(family => family.Parts.Length)} parts")
               .AppendLine()
               .AppendLine($"Each family is the set of parts one demo page composes. `{ComponentTool}(names: \"<family or part>\")` returns a family's parts with their parameters and its examples; several names may be passed at once, separated by commas.")
               .AppendLine();

        var areaOrder = Enum.GetValues<DemoArea>().Select(DemoCatalog.AreaLabel).ToList();

        foreach (var area in families.GroupBy(family => family.Area)
                                     .OrderBy(area => areaOrder.IndexOf(area.Key) is var index and >= 0 ? index : int.MaxValue))
        {
            builder.AppendLine($"## {area.Key}").AppendLine();

            foreach (var family in area)
            {
                builder.Append($"- **{family.Label}** (`{family.Key}`)");
                if (family.Lead is not null) builder.Append($" - {family.Lead}");
                builder.AppendLine($" Parts: {string.Join(", ", family.Parts.Select(part => $"`{part.Name}`"))}.");
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>The full reference of one or more families: every part with its parameters, the types they take, and the examples.</summary>
    public static string Families(IReadOnlyList<(ComponentFamily Family, ComponentPart? Part)> asked)
    {
        var builder = new StringBuilder();
        var families = asked.Select(entry => entry.Family).Distinct().ToArray();
        var parts = families.SelectMany(family => family.Parts).ToArray();

        // A base a single part inherits from is that part's business; one several parts here share is
        // described once, below, instead of once per part.
        var sharedBases = parts.SelectMany(part => part.Parameters.Select(parameter => parameter.DeclaringType)
                                                                   .Where(declaring => declaring != part.Type)
                                                                   .Distinct()
                                                                   .Select(declaring => (Declaring: declaring, Part: part)))
                               .GroupBy(entry => entry.Declaring)
                               .Where(group => group.Count() > 1)
                               .Select(group => group.Key)
                               .ToHashSet();

        if (parts.Any(part => part.HasCommonParameters)) AppendCommonParameters(builder);

        foreach (var family in families)
        {
            builder.AppendLine($"# {family.Label} (`{family.Key}`, {family.Area})").AppendLine();

            if (family.Lead is not null) builder.AppendLine(family.Lead).AppendLine();

            if (family.CssPage is not null)
            {
                builder.AppendLine($"Demo pages: `{family.CssPage.Route}` (plain CSS){(family.TailwindPage is null ? "" : $", `{family.TailwindPage.Route}` (Tailwind)")}.").AppendLine();
            }

            foreach (var (_, part) in asked.Where(entry => entry.Family == family && entry.Part is not null))
            {
                builder.AppendLine($"_`{part!.Name}` is one of this family's parts; here is the whole family, since its parts only work composed._").AppendLine();
            }

            foreach (var part in family.Parts)
            {
                AppendPart(builder, part, sharedBases, heading: "##");
            }
        }

        foreach (var shared in sharedBases.OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            var inheritors = parts.Where(part => part.Parameters.Any(parameter => parameter.DeclaringType == shared)).ToArray();
            var rows = inheritors[0].Parameters.Where(parameter => parameter.DeclaringType == shared).ToArray();

            builder.AppendLine($"## Parameters of {BheadlessTypeNames.Of(shared)}").AppendLine()
                   .AppendLine($"Taken by {string.Join(", ", inheritors.Select(part => $"`{part.Name}`"))}.").AppendLine();

            AppendParameterTable(builder, rows);
        }

        AppendTypes(builder, parts, families);

        foreach (var family in families.Where(family => family.CssPage is not null))
        {
            AppendExampleIndex(builder, family);
        }

        return Bound(builder.ToString(), asked.Count > 1
            ? $"Ask for fewer names at a time - `{ComponentTool}(names: \"{asked[^1].Family.Key}\")` for the last one."
            : $"The examples are also available on their own: `{ExamplesTool}(name: \"{families[0].Key}\")`.");
    }

    /// <summary>A library type that is not a part: an enum with its values, a class with its members, or an abstract base with its parameters.</summary>
    public static string Type(Type type)
    {
        var builder = new StringBuilder();
        var name = BheadlessTypeNames.Of(type);
        var kind = type.IsEnum ? "enum" : type.IsInterface ? "interface" : type.IsValueType ? "struct"
                 : type.IsAbstract && type.IsSealed ? "static class"
                 : typeof(IComponent).IsAssignableFrom(type) ? "abstract component base"
                 : IsRecord(type) ? "record" : "class";

        builder.AppendLine($"# {name} ({kind})").AppendLine();

        if (typeof(IComponent).IsAssignableFrom(type) && type.IsAbstract && type.IsSealed is false)
        {
            var part = BheadlessComponentCatalog.BuildPart(type);
            var inheritors = BheadlessComponentCatalog.Parts.Where(candidate => BheadlessComponentCatalog.Inherits(candidate.Type, type)).ToArray();

            AppendPart(builder, part, sharedBases: [], heading: "##", title: false);

            if (inheritors.Length > 0)
            {
                builder.AppendLine($"Inherited by {string.Join(", ", inheritors.Select(inheritor => $"`{inheritor.Name}` ({BheadlessComponentCatalog.FamilyOf(inheritor).Label})"))}.");
            }

            return builder.ToString().TrimEnd();
        }

        AppendProse(builder, BheadlessXmlDocs.GetSummary(BheadlessXmlDocs.IdOf(type)));
        AppendProse(builder, BheadlessXmlDocs.GetRemarks(BheadlessXmlDocs.IdOf(type)));

        if (type.IsEnum)
        {
            builder.AppendLine("| Value | Description |").AppendLine("| --- | --- |");
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                builder.AppendLine($"| `{field.Name}` | {Cell(BheadlessXmlDocs.GetSummary(BheadlessXmlDocs.IdOf(field)))} |");
            }
            builder.AppendLine();
        }
        else
        {
            AppendMembers(builder, type);
        }

        var users = UsersOf(type);
        if (users.Length > 0)
        {
            builder.AppendLine($"Taken by {string.Join(", ", users)}.");
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>The examples of one family as the Razor (and CSS) the demo page runs, in page order.</summary>
    public static string Examples(ComponentFamily family, string? example, string? styling)
    {
        var tailwind = styling is not null && styling.Trim().StartsWith('t');
        var unknownStyling = styling is not null && Normalize(styling) is not ("" or "css" or "plaincss" or "plain" or "tailwind" or "tw" or "tailwindcss");

        if (unknownStyling)
        {
            return $"'{styling}' is not a styling the demo shows. Use 'css' (hand-written CSS, the default) or 'tailwind'.";
        }

        var page = tailwind ? family.TailwindPage : family.CssPage;

        if (page is null)
        {
            return family.CssPage is null
                ? $"No demo page shows the {family.Label} on their own. `{ComponentTool}(names: \"{family.Key}\")` lists them."
                : $"The {family.Label} family has no {(tailwind ? "Tailwind" : "CSS")} page.";
        }

        var examples = page.Examples;

        if (string.IsNullOrWhiteSpace(example) is false)
        {
            var key = Normalize(example);
            examples = [.. examples.Where(candidate => Normalize(candidate.Title) == key)];
            if (examples.Length == 0)
            {
                examples = [.. page.Examples.Where(candidate => Normalize(candidate.Title).Contains(key, StringComparison.Ordinal)
                                                             || (candidate.Description is not null && Normalize(candidate.Description).Contains(key, StringComparison.Ordinal)))];
            }

            if (examples.Length == 0)
            {
                return $"No example of {family.Label} is titled '{example}'. Its examples are: {string.Join(", ", page.Examples.Select(candidate => $"'{candidate.Title}'"))}.";
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine($"# {family.Label} examples - {(tailwind ? "Tailwind" : "plain CSS")} (`{page.Route}`)").AppendLine();

        if (tailwind)
        {
            // The utilities are Tailwind's own, but a class like data-state-open: only works with the
            // variant the demo declares for it - so the declarations these examples use go with them.
            var code = string.Join('\n', examples.Select(candidate => candidate.Razor));
            var variants = BheadlessDemoSource.TailwindVariants
                                                     .Where(variant => code.Contains($"{variant.Name}:", StringComparison.Ordinal))
                                                     .ToArray();

            if (variants.Length > 0)
            {
                builder.AppendLine("These examples use custom variants the demo declares in its Tailwind input CSS:")
                       .AppendLine()
                       .AppendLine("```css")
                       .AppendLine(string.Join('\n', variants.Select(variant => variant.Declaration)))
                       .AppendLine("```")
                       .AppendLine();
            }
        }

        var written = 0;
        foreach (var candidate in examples)
        {
            var block = ExampleBlock(candidate);

            if (written > 0 && builder.Length + block.Length > MaxAnswerLength)
            {
                var rest = examples.Skip(written).Select(remaining => $"'{remaining.Title}'");
                builder.AppendLine($"[{examples.Length - written} more examples did not fit: {string.Join(", ", rest)}. " +
                                   $"Call `{ExamplesTool}(name: \"{family.Key}\", example: \"<title>\"{(tailwind ? ", styling: \"tailwind\"" : "")})` for each.]");
                break;
            }

            builder.Append(block);
            written++;
        }

        return Bound(builder.ToString(), $"Call `{ExamplesTool}(name: \"{family.Key}\", example: \"<title>\")` for one example at a time.");
    }

    private static string ExampleBlock(DemoExample example)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"## {example.Title}").AppendLine();
        if (example.Description is not null) builder.AppendLine(example.Description).AppendLine();
        if (example.Razor is not null) builder.AppendLine("```razor").AppendLine(example.Razor).AppendLine("```").AppendLine();
        if (example.Css is not null) builder.AppendLine("```css").AppendLine(example.Css).AppendLine("```").AppendLine();

        return builder.ToString();
    }

    private static void AppendCommonParameters(StringBuilder builder)
    {
        builder.AppendLine("_Every part marked \"common parameters\" below derives from `BitBhComponentBase` and also takes these, which are not repeated per part:_")
               .AppendLine()
               .AppendLine("| Parameter | Type | Description |")
               .AppendLine("| --- | --- | --- |");

        foreach (var parameter in BheadlessComponentCatalog.CommonParameters)
        {
            var name = parameter.Name == nameof(BitBhComponentBase.AdditionalAttributes)
                ? "any other attribute (`class`, `style`, `data-*`, `aria-*`, `@onclick` ...)"
                : $"`{parameter.Name}`";

            builder.AppendLine($"| {name} | `{parameter.TypeName}` | {Cell(parameter.Summary)} |");
        }

        builder.AppendLine();
    }

    private static void AppendPart(StringBuilder builder, ComponentPart part, HashSet<Type> sharedBases, string heading, bool title = true)
    {
        if (title) builder.AppendLine($"{heading} {part.Name}").AppendLine();

        AppendProse(builder, part.Summary);
        AppendProse(builder, part.Remarks);

        var facts = new List<string>();

        if (part.Type.IsAbstract is false)
        {
            facts.Add(part.HasCommonParameters
                ? $"Renders {(part.Element is null ? "an element" : $"`<{part.Element}>`")} (another one with `As`); takes the common parameters."
                : "Derives from `ComponentBase` directly: it takes only the parameters below, none of the common ones.");
        }

        if (part.Bindable.Length > 0) facts.Add($"Two-way bindable: {string.Join(", ", part.Bindable.Select(bind => $"`{bind}`"))}.");
        if (part.Hooks.Length > 0) facts.Add($"Hooks its documentation names: {string.Join(", ", part.Hooks.Select(hook => $"`{hook}`"))}.");

        var inherited = part.Parameters.Select(parameter => parameter.DeclaringType)
                                       .Where(declaring => sharedBases.Contains(declaring) && declaring != part.Type)
                                       .Distinct()
                                       .ToArray();

        if (inherited.Length > 0)
        {
            facts.Add($"Also takes the parameters of {string.Join(" and ", inherited.Select(type => $"`{BheadlessTypeNames.Of(type)}`"))} (below).");
        }

        foreach (var fact in facts) builder.AppendLine($"- {fact}");
        if (facts.Count > 0) builder.AppendLine();

        var rows = part.Parameters.Where(parameter => inherited.Contains(parameter.DeclaringType) is false).ToArray();

        if (rows.Length > 0) AppendParameterTable(builder, rows);
    }

    private static void AppendParameterTable(StringBuilder builder, IEnumerable<ComponentParameter> rows)
    {
        builder.AppendLine("| Parameter | Type | Default | Description |")
               .AppendLine("| --- | --- | --- | --- |");

        foreach (var parameter in rows)
        {
            var name = parameter.Required ? $"`{parameter.Name}` (required)" : $"`{parameter.Name}`";
            var defaultValue = parameter.Default is null ? "-" : $"`{parameter.Default}`";

            builder.AppendLine($"| {name} | `{parameter.TypeName}` | {defaultValue} | {Cell(parameter.Summary)} |");
        }

        builder.AppendLine();
    }

    /// <summary>
    /// The library types the parts' parameters take. An enum is cheap and comes with its values; a type
    /// only these parts take (a render context) comes with its members; one shared across the library
    /// (BitBhAnchorOptions) is named with the call that documents it, rather than repeated in every
    /// family that takes it.
    /// </summary>
    private static void AppendTypes(StringBuilder builder, ComponentPart[] parts, ComponentFamily[] families)
    {
        var library = BheadlessComponentCatalog.Types.ToHashSet();

        var types = parts.SelectMany(part => part.Parameters)
                         .SelectMany(parameter => BheadlessTypeNames.Constituents(parameter.PropertyType))
                         .Where(library.Contains)
                         .Distinct()
                         .OrderBy(type => type.Name, StringComparer.Ordinal)
                         .ToArray();

        if (types.Length == 0) return;

        builder.AppendLine("## Types these parts take").AppendLine();

        foreach (var type in types)
        {
            var name = BheadlessTypeNames.Of(type);
            var summary = BheadlessComponentCatalog.FirstSentence(BheadlessXmlDocs.GetSummary(BheadlessXmlDocs.IdOf(type)));

            if (type.IsEnum)
            {
                var values = type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(field =>
                {
                    var doc = BheadlessComponentCatalog.FirstSentence(BheadlessXmlDocs.GetSummary(BheadlessXmlDocs.IdOf(field)));
                    return doc is null ? $"`{field.Name}`" : $"`{field.Name}` ({Cell(doc)})";
                });

                builder.AppendLine($"- `{name}` (enum){(summary is null ? "" : $" - {Cell(summary)}")} Values: {string.Join("; ", values)}.");
                continue;
            }

            var takenElsewhere = FamiliesTaking(type).Except(families).Any();

            if (takenElsewhere)
            {
                builder.AppendLine($"- `{name}`{(summary is null ? "" : $" - {Cell(summary)}")} Shared across the library: `{ComponentTool}(names: \"{BheadlessComponentCatalog.BareName(type)}\")` lists its members.");
                continue;
            }

            builder.AppendLine($"- `{name}`{(summary is null ? "" : $" - {Cell(summary)}")}");

            foreach (var property in PublicProperties(type))
            {
                var doc = BheadlessXmlDocs.GetPropertySummary(type, property);
                builder.AppendLine($"  - `{property.Name}` (`{BheadlessTypeNames.Of(property)}`){(doc is null ? "" : $" - {Cell(doc)}")}");
            }
        }

        builder.AppendLine();
    }

    private static void AppendExampleIndex(StringBuilder builder, ComponentFamily family)
    {
        var css = family.CssPage!.Examples;
        var tailwind = family.TailwindPage?.Examples ?? [];

        builder.AppendLine($"## Examples of {family.Label}").AppendLine();

        builder.AppendLine($"Plain CSS (`{family.CssPage.Route}`): {string.Join(", ", css.Select(example => $"'{example.Title}'"))}.");

        if (tailwind.Length > 0)
        {
            var same = tailwind.Select(example => example.Title).SequenceEqual(css.Select(example => example.Title));
            builder.AppendLine(same
                ? $"Tailwind (`{family.TailwindPage!.Route}`): the same examples styled with utility classes."
                : $"Tailwind (`{family.TailwindPage!.Route}`): {string.Join(", ", tailwind.Select(example => $"'{example.Title}'"))}.");
        }

        builder.AppendLine()
               .AppendLine($"`{ExamplesTool}(name: \"{family.Key}\", example: \"<title>\", styling: \"css\" or \"tailwind\")` returns any of them as the Razor (and CSS) the page runs. The first one:")
               .AppendLine();

        if (css.FirstOrDefault() is { } first)
        {
            builder.AppendLine($"### {first.Title}").AppendLine();
            if (first.Description is not null) builder.AppendLine(first.Description).AppendLine();
            if (first.Razor is not null) builder.AppendLine("```razor").AppendLine(first.Razor).AppendLine("```").AppendLine();
        }
    }

    private static void AppendMembers(StringBuilder builder, Type type)
    {
        var properties = PublicProperties(type).ToArray();

        if (properties.Length > 0)
        {
            builder.AppendLine("| Property | Type | Description |").AppendLine("| --- | --- | --- |");
            foreach (var property in properties)
            {
                builder.AppendLine($"| `{property.Name}` | `{BheadlessTypeNames.Of(property)}` | {Cell(BheadlessXmlDocs.GetPropertySummary(type, property))} |");
            }
            builder.AppendLine();
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                          .Where(method => method.IsSpecialName is false
                                        && method.DeclaringType != typeof(object)
                                        && method.IsDefined(typeof(CompilerGeneratedAttribute)) is false
                                        && method.Name is not ("Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "GetType" or "Dispose" or "DisposeAsync")
                                        && method.Name.StartsWith('<') is false)
                          .OrderBy(method => method.Name, StringComparer.Ordinal)
                          .ToArray();

        if (methods.Length > 0)
        {
            builder.AppendLine("| Method | Returns | Description |").AppendLine("| --- | --- | --- |");
            foreach (var method in methods)
            {
                var parameters = string.Join(", ", method.GetParameters().Select(parameter =>
                    $"{BheadlessTypeNames.Of(parameter.ParameterType)} {parameter.Name}{(parameter.HasDefaultValue ? " = ..." : "")}"));

                builder.AppendLine($"| `{(method.IsStatic ? "static " : "")}{method.Name}({parameters})` | `{BheadlessTypeNames.Of(method.ReturnType)}` | {Cell(BheadlessXmlDocs.GetSummary(BheadlessXmlDocs.IdOf(method)))} |");
            }
            builder.AppendLine();
        }
    }

    private static IEnumerable<PropertyInfo> PublicProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
               .Where(property => property.GetIndexParameters().Length == 0 && property.Name != "EqualityContract")
               .GroupBy(property => property.Name, StringComparer.Ordinal)
               .Select(group => group.First())
               .OrderBy(property => property.MetadataToken);

    private static readonly Lazy<Dictionary<Type, ComponentFamily[]>> _familiesByType = new(() =>
        BheadlessComponentCatalog.Families
            .SelectMany(family => family.Parts.SelectMany(part => part.Parameters)
                                              .SelectMany(parameter => BheadlessTypeNames.Constituents(parameter.PropertyType))
                                              .Distinct()
                                              .Select(type => (Type: type, Family: family)))
            .GroupBy(entry => entry.Type)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Family).Distinct().ToArray()),
        LazyThreadSafetyMode.PublicationOnly);

    private static ComponentFamily[] FamiliesTaking(Type type) => _familiesByType.Value.GetValueOrDefault(type) ?? [];

    /// <summary>The parameters that take a type, as "BitBhMenuItems.Anchor".</summary>
    private static string[] UsersOf(Type type)
        => [.. BheadlessComponentCatalog.Parts
               .SelectMany(part => part.Parameters
                                       .Where(parameter => BheadlessTypeNames.Constituents(parameter.PropertyType).Contains(type))
                                       .Select(parameter => $"`{part.TagName}.{parameter.Name}`"))
               .Distinct()
               .Take(40)];

    private static bool IsRecord(Type type) => type.GetMethod("<Clone>$") is not null;

    private static void AppendProse(StringBuilder builder, string? text)
    {
        if (string.IsNullOrWhiteSpace(text) is false) builder.AppendLine(text).AppendLine();
    }

    /// <summary>Prose fitted into one table cell: one line, its code samples inline, its pipes escaped.</summary>
    public static string Cell(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var cell = FenceRegex().Replace(text, match => $"`{match.Groups["code"].Value.Replace('\n', ' ').Trim()}`");

        return WhitespaceRegex().Replace(cell, " ").Replace("|", "\\|", StringComparison.Ordinal).Trim();
    }

    /// <summary>
    /// Bounds one answer. A cut that says only that it happened leaves the caller to guess whether the
    /// rest matters, so the notice carries the call that gets it.
    /// </summary>
    public static string Bound(string text, string howToContinue)
    {
        text = text.TrimEnd();

        if (text.Length <= MaxAnswerLength) return text;

        var cut = text.LastIndexOf('\n', MaxAnswerLength);
        var kept = text[..(cut > 0 ? cut : MaxAnswerLength)];

        // A cut inside a code fence would leave the rest of the answer reading as code.
        if (Regex.Matches(kept, "^```", RegexOptions.Multiline).Count % 2 == 1) kept += "\n```";

        return $"{kept}\n\n[cut at {MaxAnswerLength} characters. {howToContinue}]";
    }

    private static string Normalize(string text)
        => new([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

    [GeneratedRegex(@"```\w*\n(?<code>.*?)\n```", RegexOptions.Singleline)]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
