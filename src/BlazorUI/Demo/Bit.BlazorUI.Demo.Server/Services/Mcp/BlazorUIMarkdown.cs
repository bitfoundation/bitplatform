using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Bit.BlazorUI.Demo.Client.Core.Models;

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
               .AppendLine("`Disabled`, `Dir`, `Visibility`, `HtmlAttributes` and the rest; the inputs add those of")
               .AppendLine("`BitInputBase` - `Value` and its `@bind-Value`, `Required`, `ReadOnly`, `OnChange`, and what an")
               .AppendLine("EditForm validates them by - and the ones that are typed into add `BitTextInputBase`. Each set is")
               .AppendLine("documented once, by `GetBitBlazorUIComponent` under its own name, rather than repeated on each")
               .AppendLine("component that carries it; a component's own answer names which of them it has.");

        return builder.ToString();
    }

    /// <summary>
    /// How long a component's reference may run before it opens by naming its sections.
    /// <para>
    /// A client that caps what one tool call may return - Claude Code among them - saves a longer
    /// answer to a file and shows the model only its first lines, so the start of a long answer is
    /// the one part certain to be read. A line there naming the sections, and the argument that
    /// returns one of them, turns a grep through a file into a second call that answers exactly; on
    /// a short answer it is only a line that was not needed.
    /// </para>
    /// </summary>
    private const int LongAnswer = 20_000;

    /// <summary>
    /// The reference of one component - whole, or narrowed by <paramref name="section"/> to the
    /// sections and the rows it names.
    /// </summary>
    public static string Component(BlazorUIComponent component, string? section = null)
    {
        var sections = Sections(component);

        if (string.IsNullOrWhiteSpace(section) is false) return Narrowed(component, sections, section);

        var title = $"# {component.Name}{component.TypeParameters}";
        var header = Header(component);
        var body = string.Concat(sections.Select(s => s.Text));

        var builder = new StringBuilder();

        builder.AppendLine(title).AppendLine();

        if (title.Length + header.Length + body.Length > LongAnswer)
        {
            builder.AppendLine($"A long reference: `GetBitBlazorUIComponent(name: \"{component.Name}\", section: \"...\")` returns one part of it - {string.Join(", ", sections.Select(s => $"`{s.Name}`"))} - or the rows of one parameter, member or CSS variable by its name; several, separated by commas.")
                   .AppendLine();
        }

        builder.Append(header).Append(body);

        return Truncate(builder.ToString());
    }

    /// <summary>
    /// One part of a component's reference: its name as a caller passes it in <c>section</c>, and its text
    /// from the <c>##</c> heading on.
    /// </summary>
    private sealed record ComponentSection(string Name, string Text);

    /// <summary>
    /// A table one row of which a <c>section</c> can name: the section it is printed in, its heading and the
    /// paragraph under it there, and how its rows are written - given the rows picked and the whole table,
    /// whose columns they keep - so a row asked for alone reads exactly as it does in the whole.
    /// </summary>
    private sealed record RowSource(string Section, string Heading, string? Lead, IReadOnlyList<ComponentMember> Rows, Action<StringBuilder, IReadOnlyList<ComponentMember>, IReadOnlyList<ComponentMember>> Write);

    /// <summary>What a component's answer says before its first section - who it is, where it ships and what it needs.</summary>
    private static string Header(BlazorUIComponent component)
    {
        var builder = new StringBuilder();

        AppendConstraints(builder, component);

        builder.Append(component.Category).Append(" · ").Append(component.Package.PackageId);
        if (component.Url.StartsWith("/components", StringComparison.Ordinal)) builder.Append(" · ").Append(SiteUrl).Append(component.Url);
        if (component.SourceUrl is not null) builder.Append(" · source: ").Append(component.SourceUrl);
        builder.AppendLine().AppendLine();

        if (string.IsNullOrWhiteSpace(component.Aliases) is false)
        {
            builder.AppendLine($"Also known as: {component.Aliases}.").AppendLine();
        }

        builder.AppendLine(component.Description ?? component.Summary).AppendLine();

        // A family documented on one page answers with its abstract base, which is not a tag anyone can write:
        // the tags are the concrete components deriving from it, read off the assembly when the catalog is built.
        if (component.FamilyMembers.Count > 0)
        {
            builder.AppendLine($"A family of {component.FamilyMembers.Count} components sharing the API below - write one of them as the tag: {string.Join(", ", component.FamilyMembers.Select(m => $"`{m}`"))}.")
                   .AppendLine();
        }

        // The note nearly every Extras component carries says to install the package, which the
        // block below says with the package name, the registration call and the tags. Two sentences
        // about the same thing, one of them vague, is the redundancy this server exists to avoid.
        if (string.IsNullOrWhiteSpace(component.Notes) is false &&
            component.Notes.Contains("install the", StringComparison.OrdinalIgnoreCase) is false)
        {
            builder.AppendLine($"**Note:** {component.Notes}").AppendLine();
        }

        AppendPackage(builder, component);

        return builder.ToString();
    }

    /// <summary>
    /// The constraints of a generic component, which decide what its type arguments may be and are
    /// written nowhere else: a TItem in a parameter table does not say it has to be a reference type
    /// with a parameterless constructor, and the compiler is where that is otherwise found out.
    /// </summary>
    private static void AppendConstraints(StringBuilder builder, BlazorUIComponent component)
    {
        if (component.ComponentType is not null && BlazorUITypeNames.ConstraintsOf(component.ComponentType) is string constraints)
        {
            builder.AppendLine($"`{constraints}`").AppendLine();
        }
    }

    /// <summary>
    /// What an optional package needs wired up. Stated on the component rather than left to the setup
    /// guide: a component from a package the app has not referenced is the failure this line prevents,
    /// and it is the one an agent reads at exactly the moment it decides to use it.
    /// </summary>
    private static void AppendPackage(StringBuilder builder, BlazorUIComponent component)
    {
        if (component.Package.Required) return;

        builder.Append($"Ships in `{component.Package.PackageId}`: add the package, ")
               .Append(component.Package.Registration is null ? "and " : $"call `{component.Package.Registration}`, and ")
               .AppendLine($"add `{component.Package.Stylesheet}`{(component.Package.Script is null ? null : $" and `{component.Package.Script}`")} to the host page. `GetBitBlazorUISetupGuide` has the exact tags.")
               .AppendLine();
    }

    /// <summary>
    /// The sections of a component's answer, in the order they are answered in, each written on its own
    /// so a <c>section</c> can return it alone. A section with nothing to say is left out rather than
    /// answered as an empty heading.
    /// </summary>
    private static List<ComponentSection> Sections(BlazorUIComponent component)
    {
        List<ComponentSection> sections = [];

        void Add(string name, Action<StringBuilder> write)
        {
            var builder = new StringBuilder();

            write(builder);

            if (builder.Length > 0) sections.Add(new(name, builder.ToString()));
        }

        var parameters = component.IsComponent ? "Parameters" : "Members";

        // The bindable line names parameters, so it is answered with them: a caller asking for the
        // parameters alone is the one who needs to know which of them take @bind-.
        Add(parameters, builder =>
        {
            AppendMembers(builder, parameters, component.Parameters);
            AppendBindable(builder, component);
        });

        Add("Public members", builder => AppendMembers(builder, "Public members", component.PublicMembers));
        Add("Inherited parameters", builder => AppendInherited(builder, component));
        Add("Cascading parameters", builder => AppendCascadingParams(builder, component));
        Add("CSS variables", builder => AppendCssVariables(builder, component));

        foreach (var type in component.OwnTypes)
        {
            Add(type.Name, builder =>
            {
                builder.AppendLine($"## {type.Name} ({Kind(type)})").AppendLine();

                if (string.IsNullOrWhiteSpace(type.Description) is false) builder.AppendLine(type.Description).AppendLine();

                if (type.IsEnum) AppendEnumRows(builder, type.Members);
                else AppendMemberRows(builder, type.Members);

                builder.AppendLine();
            });
        }

        Add("Library types used here", builder => AppendReferencedTypes(builder, component));
        Add("Worked examples", builder => AppendExamples(builder, component));

        return sections;
    }

    /// <summary>
    /// The parts of a component's answer a <c>section</c> names: a section by its heading, or the rows of a
    /// parameter, member, CSS variable or own type's member by its name - several, separated by commas
    /// outside a signature's own parameter list, so a signature copied from an answer names one member.
    /// <para>
    /// A name is matched exactly before it is matched as part of one, so <c>Parameters</c> is the table
    /// rather than BitParams' parameter of that name, and <c>Zoom</c> is BitMap's parameter rather than
    /// every member with the word in it. A name that matches nothing exactly is then read as part of a
    /// heading (<c>css</c>, <c>examples</c>) and of a member's name at once, which is what makes
    /// <c>Zoom</c> on a component with no such parameter answer with SetZoom, ZoomIn and ZoomOut. An own
    /// type's heading is read there without the component's name every one of them opens with, so
    /// <c>marker</c> on BitMap is BitMapMarker and the members about markers, and <c>date</c> on
    /// BitDatePicker is the members about dates rather than every BitDatePicker* type.
    /// </para>
    /// <para>
    /// Whatever is picked, the answer opens with what no part of it can be read without: the constraints
    /// a generic component's type arguments must meet and the package it ships in with what that package
    /// needs wired up.
    /// </para>
    /// </summary>
    private static string Narrowed(BlazorUIComponent component, List<ComponentSection> sections, string section)
    {
        var sources = RowSources(component);
        var whole = new HashSet<string>(StringComparer.Ordinal);
        var rows = new HashSet<ComponentMember>(ReferenceEqualityComparer.Instance);
        var pointers = new List<string>();
        var missed = new List<string>();

        var terms = BlazorUIComponentCatalog.SplitTopLevel(section).Select(term => term.Trim()).Where(term => term.Length > 0);

        foreach (var term in terms)
        {
            var key = Key(term);

            if (key.Length == 0)
            {
                missed.Add(term);
                continue;
            }

            if (Pick(sections.Where(s => Key(s.Name) == key).Select(s => s.Name), whole) ||
                Pick(sources.SelectMany(s => s.Rows).Where(row => Key(row.Name) == key), rows)) continue;

            if (InheritedFrom(component, key) is string pointer)
            {
                pointers.Add(pointer);
                continue;
            }

            // Both rather than the first that matches: a part of a name can be a heading's and a member's alike.
            var inHeadings = Pick(sections.Where(s => HeadingKey(component, s.Name).Contains(key, StringComparison.Ordinal)).Select(s => s.Name), whole);
            var inRows = Pick(sources.SelectMany(s => s.Rows).Where(row => Key(row.Name).Contains(key, StringComparison.Ordinal)), rows);

            if (inHeadings || inRows) continue;

            missed.Add(term);
        }

        // Nothing but commas names nothing, which is a request for the whole rather than a miss.
        if (whole.Count == 0 && rows.Count == 0 && pointers.Count == 0 && missed.Count == 0) return Component(component);

        var builder = new StringBuilder();

        builder.AppendLine($"# {component.Name}{component.TypeParameters}").AppendLine();

        AppendConstraints(builder, component);
        AppendPackage(builder, component);

        // In the order of the whole answer, and with the rows two names picked out of one table written
        // as one table: a reader asking for PanTo and SetZoom is asking about one table, not two.
        foreach (var part in sections)
        {
            if (whole.Contains(part.Name))
            {
                builder.Append(part.Text);
                continue;
            }

            if (sources.FirstOrDefault(s => s.Section == part.Name) is not { } source) continue;

            var picked = source.Rows.Where(rows.Contains).ToArray();

            if (picked.Length == 0) continue;

            builder.AppendLine($"## {source.Heading}").AppendLine();

            if (string.IsNullOrWhiteSpace(source.Lead) is false) builder.AppendLine(source.Lead).AppendLine();

            source.Write(builder, picked, source.Rows);
            builder.AppendLine();
        }

        foreach (var pointer in pointers.Distinct(StringComparer.Ordinal)) builder.AppendLine(pointer).AppendLine();

        if (missed.Count > 0)
        {
            var names = sources.SelectMany(s => s.Rows).Select(row => BlazorUIComponentCatalog.Bare(row.Name)).ToArray();
            var nearest = missed.SelectMany(term => BlazorUISuggest.Closest(term, names)).Distinct(StringComparer.Ordinal).ToArray();

            builder.AppendLine($"{component.Name} has no section, parameter or member called {string.Join(", ", missed.Select(m => $"'{m}'"))}. Its sections are {string.Join(", ", sections.Select(s => $"`{s.Name}`"))}{(nearest.Length == 0 ? null : $"; the nearest names are {string.Join(", ", nearest.Select(n => $"`{n}`"))}")}. Without `section`, `GetBitBlazorUIComponent(name: \"{component.Name}\")` returns the whole reference.");
        }
        else
        {
            builder.AppendLine($"Part of the reference: `GetBitBlazorUIComponent(name: \"{component.Name}\")` returns the whole of it.");
        }

        return Truncate(builder.ToString());
    }

    /// <summary>
    /// A heading as a part of a name is matched against: an own type's without the component's name it opens
    /// with - or the name of the component a service shows, which its types are named after - since every
    /// own type shares that and a term that is part of it would otherwise match all of them.
    /// </summary>
    private static string HeadingKey(BlazorUIComponent component, string heading)
    {
        var key = Key(heading);

        foreach (var owner in (string?[])[component.Name, component.ShownComponentType?.Name])
        {
            if (owner is null) continue;

            var prefix = Key(owner);

            if (key.Length > prefix.Length && key.StartsWith(prefix, StringComparison.Ordinal)) return key[prefix.Length..];
        }

        return key;
    }

    /// <summary>Adds what a term matched to what the answer carries, and says whether it matched anything.</summary>
    private static bool Pick<T>(IEnumerable<T> matches, HashSet<T> picked)
    {
        var any = false;

        foreach (var match in matches)
        {
            picked.Add(match);
            any = true;
        }

        return any;
    }

    /// <summary>The tables of a component's answer whose rows a <c>section</c> can name one by one, each with the section it is printed in.</summary>
    private static RowSource[] RowSources(BlazorUIComponent component)
    {
        var parameters = component.IsComponent ? "Parameters" : "Members";

        return
        [
            new(parameters, parameters, null, component.Parameters, AppendMemberRows),
            new("Public members", "Public members", null, component.PublicMembers, AppendMemberRows),
            new("CSS variables", "CSS variables", CssVariablesLead, component.CssVariables, (builder, rows, _) => AppendCssVariableRows(builder, rows)),
            .. component.OwnTypes.Select(type => new RowSource(type.Name, $"{type.Name} ({Kind(type)})", type.Description, type.Members,
                                                               type.IsEnum ? (builder, rows, _) => AppendEnumRows(builder, rows) : AppendMemberRows))
        ];
    }

    /// <summary>
    /// Where a parameter or member the component takes from one of its bases is documented, for a
    /// <c>section</c> that names one: <c>Value</c> and <c>FocusAsync</c> on BitTextField are not in its own
    /// tables, and the answer that says so has to say where they are instead.
    /// </summary>
    private static string? InheritedFrom(BlazorUIComponent component, string key)
    {
        foreach (var @base in component.Inherited)
        {
            var members = BlazorUIComponentCatalog.Bases.FirstOrDefault(b => b.Name == @base.Lookup)?.PublicMembers.Select(m => BlazorUIComponentCatalog.Bare(m.Name)) ?? [];

            if (@base.Parameters.Concat(members).FirstOrDefault(name => Key(name) == key) is string name)
            {
                return $"`{name}` is inherited from `{@base.Name}`: `GetBitBlazorUIComponent(name: \"{@base.Lookup}\", section: \"{name}\")` describes it.";
            }
        }

        return null;
    }

    /// <summary>
    /// A name as a <c>section</c> is matched by: without the parameter list or the type arguments a
    /// signature writes after it, and then without case, punctuation or the <c>Bit</c> prefix, so
    /// <c>PanTo</c>, <c>panTo(center)</c> and <c>BitDropdownItem&lt;TValue&gt;</c> all find what they name.
    /// </summary>
    private static string Key(string name)
    {
        var text = new string([.. BlazorUIComponentCatalog.Bare(name).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

        return text.StartsWith("bit", StringComparison.Ordinal) ? text[3..] : text;
    }

    /// <summary>The titles of a component's worked examples, and the call that returns their code.</summary>
    private static void AppendExamples(StringBuilder builder, BlazorUIComponent component)
    {
        if (component.Examples.Count > 0)
        {
            builder.AppendLine("## Worked examples").AppendLine();
            builder.AppendLine($"`GetBitBlazorUIComponentExamples(name: \"{component.Name}\")` returns the Razor and C# for:").AppendLine();

            var tabs = component.Examples.GroupBy(e => e.Tab).ToArray();

            // A multi-API component is three views of one feature set, so its tabs carry the same
            // sections by design. Printing that list once and naming the tabs beside it says the
            // same thing as printing it three times, and says the part that differs out loud.
            var identical = IsMultiApi(component.Examples);

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
    }

    /// <summary>The worked examples of one component, optionally narrowed to the sections a filter names.</summary>
    public static string Examples(BlazorUIComponent component, string? filter)
    {
        var tabs = component.Examples.Select(e => e.Tab).Where(t => t is not null).Distinct().ToArray();
        var multiApi = IsMultiApi(component.Examples);

        // A filter that names a tab exactly means that tab and not the sections whose titles happen
        // to contain the word: "Option" is one of BitDropdown's three APIs and also a word in
        // "Search options", and the tab is what a caller who typed a tab name asked for.
        //
        // With no filter, a multi-API component answers with its first tab only. Its tabs carry the
        // same sections in a different API by design, so returning all of them is the same code
        // three times over - and three times over is more than one answer holds, so the caller would
        // get one tab and a truncation notice anyway. Tabs that are different features (BitChart's
        // Line, Bar, Legend, ...) are each something the first one does not show, so they are all
        // answered, in page order, up to the cap.
        IReadOnlyList<DemoExampleSource> matches = string.IsNullOrWhiteSpace(filter)
            ? multiApi
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

        if (string.IsNullOrWhiteSpace(filter) && multiApi)
        {
            builder.AppendLine($"{component.Name} is a multi-API component: the same sections appear under each of its {tabs.Length} tabs - {string.Join(", ", tabs)} - differing only in how the items are supplied. This is the **{tabs[0]}** tab; pass another tab name as `example` for its version of the same code.").AppendLine();
        }

        var written = 0;

        foreach (var example in matches)
        {
            var section = new StringBuilder();

            section.AppendLine($"## {(example.Tab is null ? null : $"{example.Tab} · ")}{example.Title}").AppendLine();

            if (example.Prose is not null) section.AppendLine(example.Prose).AppendLine();

            var owned = example.Owner is null ? DemoSamples.Empty : samples[example.Owner];

            AppendFence(section, owned.Code, example.RazorField, "razor");
            AppendFence(section, owned.Code, example.CsharpField, "csharp");
            AppendCodeFiles(section, owned.Files, example.CodeFilesField);

            var remaining = matches.Skip(written).Select(e => e.Tab is null ? e.Title : $"{e.Tab} · {e.Title}").Distinct();

            var notice = $"Stopped here to stay within one answer. Also available, one at a time via `GetBitBlazorUIComponentExamples(name: \"{component.Name}\", example: \"...\")`: {string.Join(", ", remaining)}.";

            // Stopped at the cap rather than cut mid-sample: half a code block is not a smaller
            // answer, it is a wrong one. The notice is counted in the cap it keeps the answer under
            // - a page with enough sections to need it has enough of them to make it long, and a
            // notice cut in half names neither what was left out nor the call that returns it.
            if (builder.Length + section.Length + notice.Length > MaxLength && written > 0)
            {
                builder.AppendLine(notice);

                return Truncate(builder.ToString());
            }

            builder.Append(section);
            written++;
        }

        return Truncate(builder.ToString());
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

        // An extension container is a name nobody writes: what reflection finds on it is the
        // compiler's implementation of members that are written on another type entirely. So it is
        // answered with that type, and with the members under the names they are actually read by.
        if (type.IsExtensionContainer)
        {
            foreach (var group in BlazorUIExtensionMembers.All.Where(g => g.Container == clr))
            {
                builder.AppendLine($"Read off `{group.ReceiverName}` rather than off this type: `{group.ReceiverName}.{group.Members[0].Name}`, which `GetBitBlazorUIType(typeName: \"{group.ReceiverName}\")` answers with in full.").AppendLine();

                BlazorUIReflection.AppendExtensionMembers(builder, group.Members);
            }

            return Truncate(builder.ToString());
        }

        BlazorUIReflection.AppendMembers(builder, clr, type.Name);

        return Truncate(builder.ToString());
    }

    /// <summary>
    /// Whether a page's tabs are views of one feature set - a multi-API component, whose every tab carries the same
    /// sections - rather than a page split into a tab per feature, whose tabs each hold something the others do not.
    /// </summary>
    private static bool IsMultiApi(IReadOnlyList<DemoExampleSource> examples)
    {
        var tabs = examples.GroupBy(e => e.Tab).ToArray();

        return tabs.Length > 1 && tabs.Skip(1).All(t => t.Select(e => e.Title).SequenceEqual(tabs[0].Select(e => e.Title)));
    }

    /// <summary>
    /// Every other type this answer's tables name, with its members but without their prose.
    /// <para>
    /// Two kinds end up here. The shared ones - <c>BitColor</c>, <c>BitVariant</c>,
    /// <c>BitIconInfo</c> - appear on nearly every component, so repeating their descriptions 110
    /// times is the redundancy this server exists to avoid. The rest are the types a signature
    /// names that nothing above documents: a <c>BitDropdownItemsProviderRequest</c> read off an
    /// <c>ItemsProvider</c> parameter is a name an agent has no other way to resolve, since a type
    /// belonging to one component is deliberately left out of the type listing.
    /// </para>
    /// <para>
    /// Both are answered the same way, because a reader wants the same thing of them: enough to
    /// recognise the type here, and the call that returns it in full.
    /// </para>
    /// </summary>
    private static void AppendReferencedTypes(StringBuilder builder, BlazorUIComponent component)
    {
        var documented = component.OwnTypes.Select(t => t.Name)
            .Concat(component.SharedTypes.Select(t => t.Name))
            .Concat(component.Inherited.Select(b => b.Name))
            .Append(component.Name)
            .Select(name => name.Split('<')[0])
            .ToHashSet(StringComparer.Ordinal);

        // A method's row is typed by what it returns; the types it takes are in its signature.
        var texts = component.Parameters.Concat(component.PublicMembers)
            .Concat(component.OwnTypes.SelectMany(t => t.Members))
            .SelectMany(m => BlazorUIComponentCatalog.ParameterTypes(m.Name).Append(m.Type));

        var extra = BlazorUITypeCatalog.Referenced(texts)
            .Where(type => documented.Contains(type.Name) is false)
            .Select(type => $"- `{type.Name}` ({type.Kind}): {Members(type)}".TrimEnd(':', ' '));

        var shared = component.SharedTypes.Select(type => $"- `{type.Name}` ({Kind(type)}): {string.Join(", ", type.Members.Select(m => m.Name))}");

        var lines = shared.Concat(extra).ToArray();

        if (lines.Length == 0) return;

        builder.AppendLine("## Library types used here").AppendLine();
        builder.AppendLine("Named with their members only. `GetBitBlazorUIType` documents each in full.").AppendLine();

        foreach (var line in lines) builder.AppendLine(line);

        builder.AppendLine();
    }

    /// <summary>
    /// As much of a type as a one-line listing has room for: an enum's values, a class's property
    /// names, and - for a delegate, which has neither - the signature it is called with, since that
    /// is the whole of what it is and it names the types it hands over and takes back.
    /// </summary>
    private static string Members(BlazorUIType type)
    {
        if (type.Clr.IsEnum) return string.Join(", ", Enum.GetNames(type.Clr));

        if (typeof(Delegate).IsAssignableFrom(type.Clr) && type.Clr.GetMethod("Invoke") is { } invoke)
        {
            return $"{BlazorUITypeNames.Of(invoke.ReturnType)}({string.Join(", ", invoke.GetParameters().Select(p => BlazorUITypeNames.Of(p.ParameterType)))})";
        }

        return string.Join(", ", type.Clr
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p => p.Name));
    }

    /// <summary>
    /// The parameters this component inherits, named but not described.
    /// <para>
    /// The same three tables hold for every component that has them, so their prose is documented
    /// once and pointed at rather than printed 110 times. The names themselves are printed, though:
    /// a reader who cannot see that <c>Value</c> is up there has no reason to suspect it exists,
    /// and the whole point of the inherited half is that it is not visible on the component.
    /// </para>
    /// </summary>
    private static void AppendInherited(StringBuilder builder, BlazorUIComponent component)
    {
        if (component.Inherited.Count == 0) return;

        builder.AppendLine("## Inherited parameters").AppendLine();

        foreach (var @base in component.Inherited)
        {
            builder.AppendLine($"- `{@base.Name}` - {string.Join(", ", @base.Parameters.Select(p => $"`{p}`"))}. `GetBitBlazorUIComponent(name: \"{@base.Lookup}\")` describes them.");
        }

        builder.AppendLine();
    }

    /// <summary>
    /// The parameters that are two-way bindable, which is the one thing about a parameter that its
    /// row cannot say: a component declares the binding by carrying a <c>XChanged</c> callback
    /// beside <c>X</c>, and the pair is two rows a reader has to notice belong together. Named here
    /// in the form they are written in, since <c>@bind-Value</c> is what goes in the markup.
    /// </summary>
    private static void AppendBindable(StringBuilder builder, BlazorUIComponent component)
    {
        var names = component.Parameters.Select(p => p.Name)
                             .Concat(component.Inherited.SelectMany(b => b.Parameters))
                             .ToHashSet(StringComparer.Ordinal);

        var bindable = names.Where(n => n.EndsWith("Changed", StringComparison.Ordinal) is false && names.Contains($"{n}Changed"))
                            .OrderBy(n => n, StringComparer.Ordinal)
                            .ToArray();

        if (bindable.Length == 0) return;

        builder.AppendLine($"Two-way bindable: {string.Join(", ", bindable.Select(n => $"`@bind-{n}`"))}.").AppendLine();
    }

    /// <summary>
    /// The parameters object an ancestor can set this component's defaults with, which is written
    /// nowhere in the parameter table: the table is what markup sets on one instance, and this is
    /// the same set again as nullables, provided to a whole subtree at once.
    /// <para>
    /// Named rather than tabulated. Its members are the parameters printed above, and the
    /// inherited ones, with a <c>?</c> on them, so a second table of sixty rows would say what the
    /// first one already said; what a reader cannot get from the names is how the two meet - per
    /// parameter, and as a default the instance's own markup still beats.
    /// </para>
    /// </summary>
    private static void AppendCascadingParams(StringBuilder builder, BlazorUIComponent component)
    {
        if (component.CascadingParams is null) return;

        // Written the way it is written in Razor rather than as reflection names it: a params
        // class of a generic component is generic too, and `BitDropdownParams`2` neither compiles
        // in the snippet below nor resolves as the typeName the same line tells the caller to pass.
        var name = BlazorUITypeNames.Of(component.CascadingParams);

        var carried = component.CascadingParams
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        var baseName = component.CascadingParams.BaseType is { } baseType && baseType != typeof(object)
            ? BlazorUITypeNames.Of(baseType)
            : null;

        // Read off the compiled component rather than off its table: the parameters a params object
        // leaves out are the ones a reader would otherwise assume are there, and the type is what
        // has the last word on which exist. Inherited ones are included - they are exactly the set
        // in question, since a params object derives from BitComponentBaseParams and so carries the
        // BitComponentBase half of them and nothing of what an input base adds.
        var missing = component.ComponentType is null ? [] : component.ComponentType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(ParameterAttribute)) && p.IsDefined(typeof(ObsoleteAttribute)) is false && carried.Contains(p.Name) is false)
            .Select(p => p.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var inherits = baseName is null ? string.Empty : $" - its own and the `{baseName}` ones alike";

        builder.AppendLine("## Cascading parameters").AppendLine();

        // A service has no markup of its own to put a BitParams around: the component it shows is what takes the
        // cascade, from around the container that renders it - and what one showing is given beats it.
        if (component.ShownComponentType is { } shown)
        {
            var shownName = BlazorUITypeNames.Of(shown);

            builder.AppendLine($"Every `{shownName}` this service shows takes the defaults of `{name}` from a `BitParams` around the container that renders it: `<BitParams Parameters=\"@(new[] {{ new {name} {{ /* the shared ones */ }} }})\">` wrapping the container in the layout. They are defaults rather than overrides: what the container's own parameters and what one showing is given win over them, parameter by parameter - and the same `BitParams` reaches every `{shownName}` written in markup below it too. `GetBitBlazorUIType(typeName: \"{name}\")` lists its members; `GetBitBlazorUIComponent(name: \"BitParams\")` is the component that provides them.").AppendLine();

            return;
        }

        builder.AppendLine($"`{name}` carries {carried.Count} of this component's parameters again as nullables{inherits} - and a `BitParams` provides one to a whole subtree: `<BitParams Parameters=\"@(new[] {{ new {name} {{ /* the shared ones */ }} }})\">`. Every `{component.Name}` below it takes each parameter it did not write for itself from there - a default rather than an override, parameter by parameter, so one instance steps out of the group it is in by writing that one parameter and nothing else, and a nested `BitParams` likewise replaces only the parameters its own `{name}` sets. `GetBitBlazorUIType(typeName: \"{name}\")` lists its members; `GetBitBlazorUIComponent(name: \"BitParams\")` is the component that provides them.").AppendLine();

        if (missing.Length > 0)
        {
            builder.AppendLine($"Not on it, so they are written on the instance itself: {string.Join(", ", missing.Select(n => $"`{n}`"))}.").AppendLine();
        }
    }

    /// <summary>
    /// What a type named beside a component is - a class it takes an instance of, an enum one of
    /// its parameters is, or a component of its own that goes inside its markup. The third is the
    /// one worth telling apart: a <c>BitDropdownOption</c> that reads as a class is one an agent
    /// will try to construct rather than write as a tag.
    /// </summary>
    private static string Kind(ComponentSubType type)
    {
        if (type.IsEnum) return "enum";

        // An interface reads as a class it could construct too, so it is named for what it is.
        return BlazorUITypeCatalog.Find(type.Name)?.Kind is { } kind and ("component" or "interface") ? kind : "class";
    }

    /// <summary>A table of parameters or members, or nothing at all when there are none.</summary>
    private static void AppendMembers(StringBuilder builder, string heading, IReadOnlyList<ComponentMember> members)
    {
        if (members.Count == 0) return;

        builder.AppendLine($"## {heading}").AppendLine();
        AppendMemberRows(builder, members);
        builder.AppendLine();
    }

    /// <summary>
    /// The public custom properties the component reads off its root. Stated with how they are set
    /// rather than only listed: the one thing an agent needs beyond the names is that they inherit,
    /// so a `:root` rule, an ancestor's style and an instance's `Style` are all valid places for one - and an
    /// instance's `Style` reaches the instances nested in it as well, so what is meant for one alone is a parameter or
    /// the real CSS property on its `Style` / `Styles`.
    /// </summary>
    private static void AppendCssVariables(StringBuilder builder, BlazorUIComponent component)
    {
        // A service has no root of its own: the variables are those of the component it shows, pointed at rather
        // than tabulated a second time - with where a service's modals take a Style from, which that page cannot say.
        if (component.CssVariables.Count == 0 &&
            component.ShownComponentType is { } shown &&
            BlazorUIComponentCatalog.Find(shown.Name) is { CssVariables.Count: > 0 } shownComponent)
        {
            builder.AppendLine("## CSS variables").AppendLine();
            builder.AppendLine($"Every `{shownComponent.Name}` this service shows reads the {shownComponent.CssVariables.Count} public CSS variables `GetBitBlazorUIComponent(name: \"{shownComponent.Name}\")` lists (`{shownComponent.CssVariables[0].Name}`, ...). They inherit: set them on the `Style` of the parameters one showing is given to restyle that one and every one opened inside it, on the `Style` of the container's parameters to restyle every one it renders, or on `:root` to restyle every `{shownComponent.Name}` in the app.").AppendLine();

            return;
        }

        if (component.CssVariables.Count == 0) return;

        builder.AppendLine("## CSS variables").AppendLine();
        builder.AppendLine(CssVariablesLead).AppendLine();
        AppendCssVariableRows(builder, component.CssVariables);
        builder.AppendLine();
    }

    private const string CssVariablesLead = "Read off the root with a fallback and never declared by the component, so they inherit: set one on `:root` (or a `[bit-theme]` block) to restyle every instance, on an ancestor to restyle the ones inside it, or on the `Style` of one instance to restyle it and every instance nested inside it, which inherits the value too - a parameter changes one alone, as does the real CSS property on its `Style` / `Styles` when that property does not inherit (`opacity`, `border`, `padding`); an inherited one (`color`, `font-family`) reaches the nested instances that do not set it themselves. They restyle the default, never a choice: a parameter set on the instance (its `Color`, `Size`, `Shape`, ...) wins over the variable restyling what it sets, so a variable only shows on the instances that leave that parameter unset.";

    private static void AppendCssVariableRows(StringBuilder builder, IReadOnlyList<ComponentMember> variables)
    {
        builder.AppendLine("| Variable | Default | Description |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var variable in variables)
        {
            builder.AppendLine($"| `{variable.Name}` | {Cell(variable.Default)} | {Cell(variable.Description)} |");
        }
    }

    /// <summary>
    /// Rows of a parameter or member table, with the columns of <paramref name="table"/> - the whole table
    /// they are part of, when only some of its rows are written - so a row reads the same either way. A
    /// name is a signature for a method, and a signature can hold a pipe (<c>A | B</c>, a <c>[Flags]</c>
    /// default), so it is escaped like any other cell.
    /// </summary>
    private static void AppendMemberRows(StringBuilder builder, IReadOnlyList<ComponentMember> members, IReadOnlyList<ComponentMember>? table = null)
    {
        var defaults = (table ?? members).Any(m => string.IsNullOrWhiteSpace(m.Default) is false);

        builder.AppendLine(defaults ? "| Name | Type | Default | Description |" : "| Name | Type | Description |");
        builder.AppendLine(defaults ? "| --- | --- | --- | --- |" : "| --- | --- | --- |");

        foreach (var member in members)
        {
            builder.Append($"| `{Cell(member.Name)}` | `{Cell(member.Type)}` | ");
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

    private static void AppendFence(StringBuilder builder, IReadOnlyDictionary<string, string> samples, string? field, string language)
    {
        if (field is null || samples.TryGetValue(field, out var code) is false) return;

        code = code.Trim();

        if (code.Length == 0) return;

        builder.AppendLine($"```{language}").AppendLine(code).AppendLine("```").AppendLine();
    }

    /// <summary>
    /// The rest of an example's source, for the few whose feature is not one file - the stylesheet
    /// beside the markup, the code-behind beside both. Each fence is named with the file it came
    /// from, because the name is what says where the code is meant to go, and it is the same name
    /// the site prints on the tab over it.
    /// </summary>
    private static void AppendCodeFiles(StringBuilder builder, IReadOnlyDictionary<string, IReadOnlyList<DemoCodeFile>> files, string? field)
    {
        if (field is null || files.TryGetValue(field, out var sources) is false) return;

        foreach (var file in sources)
        {
            var code = file.Code?.Trim();

            if (string.IsNullOrEmpty(code)) continue;

            if (string.IsNullOrWhiteSpace(file.Name) is false) builder.AppendLine($"`{file.Name}`:").AppendLine();

            builder.AppendLine($"```{file.EffectiveLanguage}").AppendLine(code).AppendLine("```").AppendLine();
        }
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
