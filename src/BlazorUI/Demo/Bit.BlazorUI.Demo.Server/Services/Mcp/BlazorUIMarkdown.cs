using System.Text;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// Renders what the catalogs hold as Markdown, which is what every tool on this server answers with.
/// <para>
/// A parameter table as JSON repeats the four field names on every one of a component's sixty rows;
/// as a Markdown table it names them once. Over a library of 110 components that is most of what an
/// answer would have cost, spent on nothing a model needed - and the table is also the shape it
/// reads best. The same reasoning goes for the listings: they exist to be chosen from, and a row
/// per choice beats an object per choice.
/// </para>
/// </summary>
public static class BlazorUIMarkdown
{
    public const string SiteUrl = "https://blazorui.bitplatform.dev";

    /// <summary>
    /// The cap on one answer. Big enough for the largest component's full API, small enough that a
    /// tool call cannot spend a client's whole context window in one go; what is cut is always
    /// named, with the call that returns it.
    /// </summary>
    public const int MaxLength = 60_000;

    /// <summary>Every documented component in one table - the answer when no component was named.</summary>
    public static string ComponentCatalog()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# bit BlazorUI components ({BlazorUIComponentCatalog.Components.Length})").AppendLine();
        builder.AppendLine("Pass a name to `GetBitBlazorUIComponent` for the full API of one, or to")
               .AppendLine("`GetBitBlazorUIComponentExamples` for its working code. \"Also known as\" lists the names")
               .AppendLine("other libraries use for the same thing, and resolves in both tools.").AppendLine();

        foreach (var group in BlazorUIComponentCatalog.Components.GroupBy(c => c.Category))
        {
            var package = group.Select(c => c.Package.PackageId).Distinct().ToArray();

            builder.AppendLine($"## {group.Key}{(package.Length == 1 && package[0] != BlazorUIAssemblies.Core.PackageId ? $" ({package[0]})" : null)}").AppendLine();
            builder.AppendLine("| Component | Package | Also known as | Summary |");
            builder.AppendLine("| --- | --- | --- | --- |");

            foreach (var component in group)
            {
                builder.AppendLine($"| `{component.Name}{component.TypeParameters}` | {component.Package.PackageId} | {Cell(component.Aliases)} | {Cell(component.Summary)} |");
            }

            builder.AppendLine();
        }

        builder.AppendLine("Every one of them also takes the parameters of `BitComponentBase` - `Class`, `Style`, `Id`,")
               .AppendLine("`IsEnabled`, `Dir`, `Visibility`, `HtmlAttributes` and the rest. Those are documented once, by")
               .AppendLine("`GetBitBlazorUIComponent(name: \"BitComponentBase\")`, rather than repeated on each component.");

        return builder.ToString();
    }

    /// <summary>The full reference of one component.</summary>
    public static string Component(BlazorUIComponent component)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {component.Name}{component.TypeParameters}").AppendLine();

        builder.Append(component.Category).Append(" · ").Append(component.Package.PackageId);
        if (component.Url.StartsWith("/components", StringComparison.Ordinal)) builder.Append(" · ").Append(SiteUrl).Append(component.Url);
        if (component.SourceUrl is not null) builder.Append(" · source: ").Append(component.SourceUrl);
        builder.AppendLine().AppendLine();

        if (string.IsNullOrWhiteSpace(component.Aliases) is false)
        {
            builder.AppendLine($"Also known as: {component.Aliases}.").AppendLine();
        }

        builder.AppendLine(component.Description ?? component.Summary).AppendLine();

        // The note nearly every Extras component carries says to install the package, which the
        // block below says with the package name, the registration call and the tags. Two sentences
        // about the same thing, one of them vague, is the redundancy this server exists to avoid.
        if (string.IsNullOrWhiteSpace(component.Notes) is false &&
            component.Notes.Contains("install the", StringComparison.OrdinalIgnoreCase) is false)
        {
            builder.AppendLine($"**Note:** {component.Notes}").AppendLine();
        }

        // Stated on the component rather than left to the setup guide: a component from a package
        // the app has not referenced is the failure this line prevents, and it is the one an agent
        // reads at exactly the moment it decides to use it.
        if (component.Package.Required is false)
        {
            builder.Append($"Ships in `{component.Package.PackageId}`: add the package, ")
                   .Append(component.Package.Registration is null ? "and " : $"call `{component.Package.Registration}`, and ")
                   .AppendLine($"add `{component.Package.Stylesheet}`{(component.Package.Script is null ? null : $" and `{component.Package.Script}`")} to the host page. `GetBitBlazorUISetupGuide` has the exact tags.")
                   .AppendLine();
        }

        AppendMembers(builder, component.IsComponent ? "Parameters" : "Members", component.Parameters);
        AppendMembers(builder, "Public members", component.PublicMembers);

        foreach (var type in component.OwnTypes)
        {
            builder.AppendLine($"## {type.Name} ({(type.IsEnum ? "enum" : "class")})").AppendLine();

            if (string.IsNullOrWhiteSpace(type.Description) is false) builder.AppendLine(type.Description).AppendLine();

            if (type.IsEnum) AppendEnumRows(builder, type.Members);
            else AppendMemberRows(builder, type.Members);

            builder.AppendLine();
        }

        if (component.SharedEnums.Count > 0)
        {
            builder.AppendLine("## Library enums used here").AppendLine();
            builder.AppendLine("Named with their values only - these are shared across the library and documented in full")
                   .AppendLine("by `GetBitBlazorUIType`.").AppendLine();

            foreach (var type in component.SharedEnums)
            {
                builder.AppendLine($"- `{type.Name}`: {string.Join(", ", type.Members.Select(m => m.Name))}");
            }

            builder.AppendLine();
        }

        if (component.Examples.Count > 0)
        {
            builder.AppendLine("## Worked examples").AppendLine();
            builder.AppendLine($"`GetBitBlazorUIComponentExamples(name: \"{component.Name}\")` returns the Razor and C# for:").AppendLine();

            var tabs = component.Examples.GroupBy(e => e.Tab).ToArray();

            // A multi-API component is three views of one feature set, so its tabs carry the same
            // sections by design. Printing that list once and naming the tabs beside it says the
            // same thing as printing it three times, and says the part that differs out loud.
            var identical = tabs.Length > 1 && tabs.Skip(1).All(t => t.Select(e => e.Title).SequenceEqual(tabs[0].Select(e => e.Title)));

            if (identical)
            {
                builder.AppendLine($"{string.Join(", ", tabs[0].Select(e => e.Title))}.").AppendLine();
                builder.AppendLine($"The same sections appear under each of its {tabs.Length} API tabs - {string.Join(", ", tabs.Select(t => t.Key))} - which differ only in how the items are supplied. Pass a tab name as `example` to get one tab's code.");
            }
            else
            {
                foreach (var tab in tabs)
                {
                    builder.AppendLine($"{(tab.Key is null ? null : $"**{tab.Key} tab** - ")}{string.Join(", ", tab.Select(e => e.Title))}.");
                }
            }

            builder.AppendLine();
        }

        if (component.InheritsBase)
        {
            builder.AppendLine($"Inherits the `BitComponentBase` parameters (`Class`, `Style`, `Id`, `IsEnabled`, `Dir`, `Visibility`, `HtmlAttributes`, `AriaLabel`, `TabIndex`, `ForceAnimation`) - `GetBitBlazorUIComponent(name: \"BitComponentBase\")` documents them.");
        }

        return Truncate(builder.ToString());
    }

    /// <summary>The worked examples of one component, optionally narrowed to the sections a filter names.</summary>
    public static string Examples(BlazorUIComponent component, string? filter)
    {
        var tabs = component.Examples.Select(e => e.Tab).Where(t => t is not null).Distinct().ToArray();

        // A filter that names a tab exactly means that tab and not the sections whose titles happen
        // to contain the word: "Option" is one of BitDropdown's three APIs and also a word in
        // "Search options", and the tab is what a caller who typed a tab name asked for.
        //
        // With no filter and more than one tab, only the first is answered. The tabs of a multi-API
        // component carry the same sections in a different API by design, so returning all of them
        // is the same code three times over - and three times over is more than one answer holds,
        // so the caller would get one tab and a truncation notice anyway.
        IReadOnlyList<DemoExampleSource> matches = string.IsNullOrWhiteSpace(filter)
            ? tabs.Length > 1
                ? [.. component.Examples.Where(e => e.Tab == tabs[0])]
                : component.Examples
            : component.Examples.Any(e => string.Equals(e.Tab, filter, StringComparison.OrdinalIgnoreCase))
                ? [.. component.Examples.Where(e => string.Equals(e.Tab, filter, StringComparison.OrdinalIgnoreCase))]
                : [.. component.Examples.Where(e => e.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                                                 || e.Tab?.Contains(filter, StringComparison.OrdinalIgnoreCase) is true)];

        if (matches.Count == 0)
        {
            return component.Examples.Count == 0
                ? $"{component.Name} has no worked examples on the documentation site. Its full API is `GetBitBlazorUIComponent(name: \"{component.Name}\")`."
                : $"{component.Name} has no example matching '{filter}'. Its sections are: {string.Join(", ", component.Examples.Select(e => e.Title))}.";
        }

        // One instance per owner rather than one per sample: the samples are instance fields, and a
        // page with forty of them would otherwise be constructed forty times.
        var samples = matches.Select(e => e.Owner).Distinct().Where(t => t is not null)
                             .ToDictionary(t => t!, t => DemoTables.Samples(t!));

        var builder = new StringBuilder();

        builder.AppendLine($"# {component.Name} examples").AppendLine();

        if (string.IsNullOrWhiteSpace(filter) && tabs.Length > 1)
        {
            builder.AppendLine($"{component.Name} is a multi-API component: the same sections appear under each of its {tabs.Length} tabs - {string.Join(", ", tabs)} - differing only in how the items are supplied. This is the **{tabs[0]}** tab; pass another tab name as `example` for its version of the same code.").AppendLine();
        }

        var written = 0;

        foreach (var example in matches)
        {
            var section = new StringBuilder();

            section.AppendLine($"## {(example.Tab is null ? null : $"{example.Tab} · ")}{example.Title}").AppendLine();

            if (example.Prose is not null) section.AppendLine(example.Prose).AppendLine();

            var owned = example.Owner is null ? [] : samples[example.Owner];

            AppendFence(section, owned, example.RazorField, "razor");
            AppendFence(section, owned, example.CsharpField, "csharp");

            // Stopped at the cap rather than cut mid-sample: half a code block is not a smaller
            // answer, it is a wrong one. What is left is named with the call that returns it.
            if (builder.Length + section.Length > MaxLength && written > 0)
            {
                var remaining = matches.Skip(written).Select(e => e.Tab is null ? e.Title : $"{e.Tab} · {e.Title}").Distinct();

                builder.AppendLine($"Stopped here to stay within one answer. Also available, one at a time via `GetBitBlazorUIComponentExamples(name: \"{component.Name}\", example: \"...\")`: {string.Join(", ", remaining)}.");

                return builder.ToString();
            }

            builder.Append(section);
            written++;
        }

        return builder.ToString();
    }

    /// <summary>The library-wide types, grouped by kind - the answer when no type was named.</summary>
    public static string TypeCatalog()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# bit BlazorUI types").AppendLine();
        builder.AppendLine("Pass a name to `GetBitBlazorUIType` for one of these in full. A type named after a")
               .AppendLine("component (its class-styles bag, its item class) is left out here: `GetBitBlazorUIComponent`")
               .AppendLine("documents it for that component, where it is read in context.").AppendLine();

        foreach (var group in BlazorUITypeCatalog.Listed.GroupBy(t => t.Kind).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var type in group)
            {
                var summary = FirstSentence(type.Summary);

                builder.AppendLine($"- `{type.Name}`{(type.Package.Required ? null : $" ({type.Package.PackageId})")}{(summary is null ? null : $" - {summary}")}");
            }

            builder.AppendLine();
        }

        // The classes and sub-components no component's API names are left out rather than listed:
        // they are the internals of a component that documents them in context, and they would be
        // a third of this listing spent on names nothing points at. Every one still resolves.
        if (BlazorUITypeCatalog.Hidden > 0)
        {
            builder.AppendLine($"A further {BlazorUITypeCatalog.Hidden} public classes and sub-components are not listed here: no component's parameter or member names them, so they are reached from the component that uses them. `GetBitBlazorUIType` still resolves each of them by name, and `SearchBitBlazorUI` finds them.");
        }

        return Truncate(builder.ToString());
    }

    /// <summary>The full reference of one type.</summary>
    public static string Type(BlazorUIType type)
    {
        var builder = new StringBuilder();
        var clr = type.Clr;

        var generics = clr.IsGenericTypeDefinition ? $"<{string.Join(", ", clr.GetGenericArguments().Select(a => a.Name))}>" : null;

        builder.AppendLine($"# {type.Name}{generics}").AppendLine();
        builder.AppendLine($"{type.Kind} · {type.Package.PackageId} · `{clr.Namespace}`").AppendLine();

        if (type.Summary is not null) builder.AppendLine(type.Summary).AppendLine();

        if (BlazorUIXmlDocs.GetRemarks(BlazorUIXmlDocs.IdOf(clr)) is string remarks) builder.AppendLine(remarks).AppendLine();

        if (BlazorUIComponentCatalog.Find(type.Name) is { } component && component.Name == type.Name)
        {
            builder.AppendLine($"Documented in full, with its parameters and worked examples, by `GetBitBlazorUIComponent(name: \"{type.Name}\")`.");

            return builder.ToString();
        }

        BlazorUIReflection.AppendMembers(builder, clr);

        return Truncate(builder.ToString());
    }

    /// <summary>A table of parameters or members, or nothing at all when there are none.</summary>
    private static void AppendMembers(StringBuilder builder, string heading, IReadOnlyList<ComponentMember> members)
    {
        if (members.Count == 0) return;

        builder.AppendLine($"## {heading}").AppendLine();
        AppendMemberRows(builder, members);
        builder.AppendLine();
    }

    private static void AppendMemberRows(StringBuilder builder, IReadOnlyList<ComponentMember> members)
    {
        var defaults = members.Any(m => string.IsNullOrWhiteSpace(m.Default) is false);

        builder.AppendLine(defaults ? "| Name | Type | Default | Description |" : "| Name | Type | Description |");
        builder.AppendLine(defaults ? "| --- | --- | --- | --- |" : "| --- | --- | --- |");

        foreach (var member in members)
        {
            builder.Append($"| `{member.Name}` | `{Cell(member.Type)}` | ");
            if (defaults) builder.Append($"{Code(member.Default)} | ");
            builder.AppendLine($"{Cell(member.Description)} |");
        }
    }

    private static void AppendEnumRows(StringBuilder builder, IReadOnlyList<ComponentMember> values)
    {
        builder.AppendLine("| Name | Value | Description |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var value in values)
        {
            builder.AppendLine($"| `{value.Name}` | {Cell(value.Default)} | {Cell(value.Description)} |");
        }
    }

    private static void AppendFence(StringBuilder builder, Dictionary<string, string> samples, string? field, string language)
    {
        if (field is null || samples.TryGetValue(field, out var code) is false) return;

        code = code.Trim();

        if (code.Length == 0) return;

        builder.AppendLine($"```{language}").AppendLine(code).AppendLine("```").AppendLine();
    }

    /// <summary>
    /// The lead sentence of a summary - what a listing has room for. A full stop that ends an
    /// abbreviation ("e.g.", "i.e.") is not the end of a sentence, and cutting there leaves a row
    /// that stops mid-thought, so a stop is only taken once a few words have gone past it.
    /// </summary>
    private static string? FirstSentence(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return null;

        var text = Cell(summary);

        for (var stop = text.IndexOf(". ", StringComparison.Ordinal); stop > 0; stop = text.IndexOf(". ", stop + 1, StringComparison.Ordinal))
        {
            // The word the stop ends, which an abbreviation makes one or two letters long.
            var word = text.LastIndexOf(' ', stop);

            if (stop - word > 3) return text[..(stop + 1)];
        }

        return text;
    }

    private static string Code(string? text) => string.IsNullOrWhiteSpace(text) ? string.Empty : $"`{Cell(text)}`";

    /// <summary>
    /// One cell's text, made safe to sit between two pipes. A pipe inside a cell ends the cell and a
    /// line break ends the row, so either arriving in a description would shift every column after
    /// it - which a reader cannot see has happened.
    /// </summary>
    private static string Cell(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text.Replace('\r', ' ')
                   .Replace('\n', ' ')
                   .Replace("|", @"\|", StringComparison.Ordinal)
                   .Trim();
    }

    public static string Truncate(string text)
        => text.Length <= MaxLength ? text : $"{text[..MaxLength]}\n\n[Cut off at {MaxLength:N0} characters.]";
}
