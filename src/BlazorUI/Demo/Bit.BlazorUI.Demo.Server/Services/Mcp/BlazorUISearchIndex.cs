using System.Text;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// One searchable index over everything this server knows: every component with its aliases and its
/// prose, every parameter of every component, every worked example, every public type and enum
/// value, and every chapter of the theming reference.
/// <para>
/// The name a task suggests is rarely the name the library chose - a "select" is
/// <c>BitDropdown</c>, a "toast" is <c>BitSnackBar</c>, a "skeleton" is <c>BitShimmer</c>, an
/// "expander" is <c>BitAccordion</c> - and with 110 components across five packages, working out
/// which corpus holds the answer is the actual task. This does it in one call, and every hit names
/// the exact follow-up call that returns its full text, so one search is enough to know what to ask
/// for next.
/// </para>
/// </summary>
public static class BlazorUISearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted)
    {
        /// <summary>
        /// The names this entry is also known by, whole - what someone arriving from another library
        /// types.
        /// <para>
        /// Scored above a title word, because an alias is not a term that happens to appear in a
        /// name: it is a stated mapping from the word a task uses to the word this library uses.
        /// Without it, "on off switch" ranks BitGridItem over BitToggle, because the alias counted
        /// as one more term buried in a boosted string.
        /// </para>
        /// </summary>
        public string[] AliasPhrases { get; init; } = [];

        /// <summary>
        /// The words of those aliases. A term that IS a whole alias is a stronger signal than one
        /// that is a word inside a longer one - "tabs" is exactly BitPivot's "Tab" and only half of
        /// BitNavBar's "TabPanel" - so the two are scored apart.
        /// </summary>
        public string[] AliasWords { get; init; } = [];

        /// <summary>
        /// How much this entry outranks another that matched the query as well.
        /// <para>
        /// A component's answer already contains its parameters, its own enums and the titles of
        /// its examples, so a query that matches both the component and one of its members is
        /// better served by the component: the member hit is a subset of what the component hit
        /// returns, and it costs the same call. Without this a query like "multi select chips"
        /// fills its whole result with three of BitDropdown's parameters and never names
        /// BitDropdown.
        /// </para>
        /// </summary>
        public int Weight { get; init; } = 1;

        /// <summary>
        /// The title split into words, camel-case humps included, so "SnackBar" is found by "snack
        /// bar" - and so a query word counts as a title hit only when it IS one of those words,
        /// rather than merely appearing inside one.
        /// </summary>
        public string[] TitleWords { get; } = [.. SplitWords(Title).Select(w => w.ToLowerInvariant())];

        /// <summary>
        /// The searchable text lowered once, when the index is built, so scoring scans it with
        /// ordinal comparisons rather than folding every character it walks past on every query.
        /// </summary>
        public string LoweredTitle { get; } = Title.ToLowerInvariant();

        /// <inheritdoc cref="LoweredTitle"/>
        public string LoweredBoosted { get; } = Boosted.ToLowerInvariant();

        /// <inheritdoc cref="LoweredTitle"/>
        public string LoweredBody { get; } = Body.ToLowerInvariant();
    }

    private const int MaxTerms = 16;

    // PublicationOnly rather than the default: the default mode caches the exception a failed build
    // threw and rethrows it for the lifetime of the process, so one transient failure would leave
    // the search permanently broken. Here a failed build is retried by the next caller.
    private static readonly Lazy<Entry[]> _entries = new(Build, LazyThreadSafetyMode.PublicationOnly);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // Every entry in this index is a bit BlazorUI component in a Blazor app - a term that
        // matches everything ranks nothing.
        "bit", "blazor", "blazorui", "component", "components", "razor"
    };

    /// <summary>
    /// Builds the index ahead of the first query. Walking 110 demo pages and five assemblies takes
    /// long enough to be noticed, and the caller who happens to be first is the one who would wait.
    /// </summary>
    public static void Warm()
    {
        BlazorUIComponentCatalog.Warm();
        BlazorUITypeCatalog.Warm();
        BlazorUIIconCatalog.Warm();

        _ = _entries.Value;
    }

    public static string Search(string? query, int limit)
    {
        var terms = Tokenize(query);

        if (terms.Length == 0)
        {
            return $"'{query}' carries no searchable term: words under three letters and words common to every entry here (\"the\", \"how\", \"bit\", \"blazor\", \"component\") are dropped before matching. Search for what the thing does - \"pick a date\", \"toast notification\", \"tabs\", \"virtualized table\".";
        }

        var hits = _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 50))
            .ToArray();

        if (hits.Length == 0)
        {
            return $"Nothing in bit BlazorUI matches '{query}'. Try the capability rather than the wording - \"select\" for a dropdown, \"toast\" for a snack bar, \"skeleton\" for a shimmer - or call `GetBitBlazorUIComponent` with no name for the whole catalog.";
        }

        var builder = new StringBuilder();

        builder.AppendLine($"# bit BlazorUI matches for '{query}'").AppendLine();

        foreach (var hit in hits)
        {
            builder.AppendLine($"## {hit.Entry.Title}{(hit.Entry.Context is null ? null : $" - {hit.Entry.Context}")}");
            builder.AppendLine($"{hit.Entry.Kind} · `{hit.Entry.Tool}`");

            var snippet = Snippet(hit.Entry.Body, terms);

            if (snippet.Length > 0) builder.AppendLine().AppendLine(snippet);

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static int Score(Entry entry, string[] terms)
    {
        var score = 0;
        var body = 0;
        var alias = 0;
        var matched = 0;

        foreach (var term in terms)
        {
            var aliasHit = entry.AliasPhrases.Any(phrase => Equivalent(phrase, term)) ? 25
                         : entry.AliasWords.Any(word => Equivalent(word, term)) ? 15
                         : 0;
            var isAlias = aliasHit > 0;
            var isTitleWord = entry.TitleWords.Any(word => Equivalent(word, term));
            var inTitle = isTitleWord ? 0 : Count(entry.LoweredTitle, term);
            var inBoosted = Count(entry.LoweredBoosted, term);
            var inBody = Count(entry.LoweredBody, term);

            if (isAlias is false && isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "Dropdown" wants the component, not the paragraphs that mention it. An alias is
            // worth more still - it is the library saying "this is what you call it".
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5;
            alias = Math.Max(alias, aliasHit);
            body += Math.Min(inBody, 3);
        }

        // The strongest alias hit, not the sum of them: an entry that lists five alternative names
        // is not five times more relevant to a query that happens to touch two of them. Without the
        // cap, "loading skeleton placeholder" ranks BitTextShimmer - whose nav entry lists both
        // "Skeleton" and "Loading" - above BitShimmer, which is the skeleton.
        score += alias;

        // A body here is a whole component description or a chapter of the theming guide, and a long
        // enough document mentions everything. Capping the prose as a whole rather than per term is
        // what lets full text be indexed at all: it can rank a hit and break a tie, but it can never
        // outweigh the entry whose NAME was asked for.
        score += Math.Min(body, 9);

        return matched == 0 ? 0 : score * matched * entry.Weight;
    }

    /// <summary>Same word, plural aside - "tags" has to find "Tag", and nobody phrases a question in the number the API happens to use.</summary>
    private static bool Equivalent(string word, string term)
    {
        return string.Equals(word, term, StringComparison.Ordinal)
            || (word.Length == term.Length + 1 && word[^1] == 's' && string.Equals(word[..^1], term, StringComparison.Ordinal))
            || (term.Length == word.Length + 1 && term[^1] == 's' && string.Equals(term[..^1], word, StringComparison.Ordinal));
    }

    private static int Count(string text, string term)
    {
        if (text.Length == 0) return 0;

        var count = 0;
        var index = text.IndexOf(term, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = text.IndexOf(term, index + term.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>The window of the original body around the first term that occurs in it, in the case it was written in.</summary>
    private static string Snippet(string body, string[] terms)
    {
        if (body.Length == 0) return string.Empty;

        var index = -1;

        foreach (var term in terms)
        {
            index = body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0) break;
        }

        if (index < 0) index = 0;

        var start = Math.Max(0, index - 80);
        var length = Math.Min(240, body.Length - start);
        var snippet = body.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return $"{(start > 0 ? "..." : null)}{snippet}{(start + length < body.Length ? "..." : null)}";
    }

    private static string[] SplitWords(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            // A capital after a lowercase letter starts a new word ("SnackBar" -> snack bar), while
            // a run of capitals stays together ("RTL", "SSR").
            if (char.IsUpper(c) && current.Length > 0 && char.IsLower(current[^1]))
            {
                words.Add(current.ToString());
                current.Clear();
            }

            current.Append(c);
        }

        if (current.Length > 0) words.Add(current.ToString());

        return [.. words];
    }

    private static string[] Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return [.. query.Split(['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'],
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Select(term => term.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            // Every term is counted in every entry's body, so the work is terms x corpus. No
            // question is phrased in more words than this, while a pasted file would scan for hours.
            .Take(MaxTerms)];
    }

    /// <summary>The names a nav entry lists a component under, split apart.</summary>
    private static string[] Aliases(string? aliases)
        => (aliases ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static Entry[] Build()
    {
        var entries = new List<Entry>(8192);

        // The base classes alongside the components: their parameters are the ones no component's
        // own table names, so a search for "debounce", "read only" or "ValueExpression" has nowhere
        // else to land.
        foreach (var component in BlazorUIComponentCatalog.Components.Concat(BlazorUIComponentCatalog.Bases))
        {
            var call = $"GetBitBlazorUIComponent(name: \"{component.Name}\")";

            entries.Add(new Entry("Component", $"{component.Name}{component.TypeParameters}", component.Category, call,
                $"{component.Description ?? component.Summary} {component.Notes}".Trim(),
                // The example titles ride along in the boosted text because they name the features
                // the component has - "Chips", "Virtualization", "Validation" - which no other field
                // of it does.
                $"{component.Package.PackageId} {string.Join(' ', component.Examples.Select(e => e.Title).Distinct())}")
            {
                Weight = 3,
                AliasPhrases = [.. Aliases(component.Aliases).Select(alias => new string([.. alias.Where(char.IsLetterOrDigit)]).ToLowerInvariant())],
                AliasWords = [.. Aliases(component.Aliases).SelectMany(SplitWords).Select(word => word.ToLowerInvariant())]
            });

            foreach (var parameter in component.Parameters)
            {
                entries.Add(new Entry("Parameter", $"{component.Name}.{parameter.Name}", component.Name, call,
                    parameter.Description ?? string.Empty, parameter.Type));
            }

            foreach (var member in component.PublicMembers)
            {
                entries.Add(new Entry("Public member", $"{component.Name}.{member.Name}", component.Name, call,
                    member.Description ?? string.Empty, member.Type));
            }

            foreach (var type in component.OwnTypes)
            {
                // The member names go in the body rather than in the boosted text: a class-styles bag
                // is thirty part names, and boosting each of them made "BitDatePickerClassStyles"
                // outrank BitDateRangePicker for "pick a date range". A body is capped as a whole,
                // so the names can still find the type without winning on volume.
                entries.Add(new Entry(type.IsEnum ? "Enum" : "Class", type.Name, component.Name, call,
                    $"{type.Description} {string.Join(' ', type.Members.Select(m => m.Name))}".Trim(), string.Empty));
            }

            foreach (var example in component.Examples)
            {
                entries.Add(new Entry("Example", $"{component.Name} · {example.Title}", example.Tab,
                    $"GetBitBlazorUIComponentExamples(name: \"{component.Name}\", example: \"{example.Title}\")",
                    example.Prose ?? string.Empty, string.Empty));
            }
        }

        foreach (var type in BlazorUITypeCatalog.LibraryWide)
        {
            entries.Add(new Entry(type.Kind[..1].ToUpperInvariant() + type.Kind[1..], type.Name, type.Package.Required ? null : type.Package.PackageId,
                $"GetBitBlazorUIType(typeName: \"{type.Name}\")",
                type.Summary ?? string.Empty,
                type.Clr.IsEnum ? string.Join(' ', Enum.GetNames(type.Clr)) : string.Empty));
        }

        entries.AddRange(ThemingChapters());

        foreach (var model in BlazorUISetupGuide.HostingModels)
        {
            entries.Add(new Entry("Setup guide", model, "Hosting model", $"GetBitBlazorUISetupGuide(hostingModel: \"{model}\")",
                "install package nuget register services AddBitBlazorUIServices stylesheet script tag imports namespace setup wiring", string.Empty));
        }

        entries.Add(new Entry("Icons", "BitIconName", $"{BlazorUIIconCatalog.Count:N0} glyphs", "FindBitBlazorUIIcons(query: \"...\")",
            "icon glyph symbol fabric mdl2 pictogram IconName", string.Empty));

        return [.. entries];
    }

    /// <summary>The theming reference's chapters, from the one list <see cref="BlazorUIThemingGuide.Chapters"/> holds.</summary>
    private static IEnumerable<Entry> ThemingChapters()
    {
        return BlazorUIThemingGuide.Chapters.Select(chapter => new Entry("Theming", chapter.Title, "Theming reference",
            $"GetBitBlazorUIThemingGuide(section: \"{chapter.Title}\")",
            $"{chapter.Description} theme token css variable design system dark light color preset", string.Empty));
    }
}
