using System.Text;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// One searchable index over everything this server knows: every component with its aliases and its
/// prose, every parameter of every component, every worked example, every public type and enum
/// value, and every chapter of the theming reference.
/// <para>
/// The name a task suggests is rarely the name the library chose - a "select" is
/// <c>BitDropdown</c>, a "toast" is <c>BitSnackBar</c>, a "skeleton" is <c>BitShimmer</c>, an
/// "expander" is <c>BitAccordion</c> - and with 150 components across six packages, working out
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
        /// A component's own name as one word, without the Bit prefix every name here shares -
        /// "searchbox" for BitSearchBox - for a query that spells it in the words it is made of,
        /// as it may spell an alias (see <see cref="SpelledNameSpan"/>), and for a query that is
        /// nothing but the name, which puts the component first.
        /// </summary>
        public string? NamePhrase { get; init; }

        /// <summary>
        /// The last word of a component's name of two words or more - "chat" for BitAiChat, "picker"
        /// for BitDatePicker - the word that says what the component is, where the ones before it say
        /// which one. A term that is the head of a name counts as a title word twice over.
        /// <para>
        /// A title word alone is worth less than a word of an alias, so for "chat", BitAiPromptInput,
        /// which lists "ChatInput", ranked above BitAiChat, whose own name says it is one. Only the
        /// last word counts: BitAiChatMessage is a message, and a ChatInput an input. A name of one
        /// word has no head apart from the whole name, which a query spells (see SpelledNameSpan).
        /// </para>
        /// </summary>
        public string? NameHead { get; init; }

        /// <summary>
        /// The words a component's nav entry tags it with, lowered: what it does and what it is used
        /// for - "Eyedropper", "Hex" for BitColorPicker, "LLM", "GPT" for BitAiChat - which nothing
        /// else here says, so without them those found nothing at all. The site's search boxes rank by
        /// them too.
        /// <para>
        /// Scored as one occurrence in the boosted text, however many of the keywords hold the term: a
        /// list of tags names a thing once, and BitChart's "Bar chart" and "Error bars" ranked it
        /// above BitHeader, which is the AppBar, for "app bar".
        /// </para>
        /// </summary>
        public string LoweredKeywords { get; init; } = string.Empty;

        /// <summary>
        /// How much this entry outranks another that matched the query as well.
        /// <para>
        /// A component's answer already contains its parameters, its own enums and the titles of
        /// its examples, so a query that matches both the component and one of its members is
        /// better served by the component: the member hit is a subset of what the component hit
        /// returns. Without this a query like "multi select chips"
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

    // The words of a query a name is looked for across (see Runs): more than a question is phrased
    // in, and a bound on the runs a pasted file would make of it.
    private const int MaxWords = 64;

    // PublicationOnly rather than the default: the default mode caches the exception a failed build
    // threw and rethrows it for the lifetime of the process, so one transient failure would leave
    // the search permanently broken. Here a failed build is retried by the next caller.
    private static readonly Lazy<Entry[]> _entries = new(Build, LazyThreadSafetyMode.PublicationOnly);

    // The longest name an entry goes by, plus the plural "s" Equivalent forgives: a run of words
    // longer than this spells no name, and nor does a longer run from the same word (see Runs).
    private static readonly Lazy<int> _longestName = new(() => _entries.Value.Max(entry => Math.Max(entry.NamePhrase?.Length ?? 0,
                                                                                                     entry.AliasPhrases.Length == 0 ? 0 : entry.AliasPhrases.Max(phrase => phrase.Length))) + 1,
                                                          LazyThreadSafetyMode.PublicationOnly);

    private static readonly char[] _separators = ['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'];

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
    /// Builds the index ahead of the first query. Walking 150 demo pages and six assemblies takes
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
        var words = Words(query);
        var terms = Tokenize(words);

        if (terms.Length == 0)
        {
            return $"'{query}' carries no searchable term: words under three letters and words common to every entry here (\"the\", \"how\", \"bit\", \"blazor\", \"component\") are dropped before matching. Search for what the thing does - \"pick a date\", \"toast notification\", \"tabs\", \"virtualized table\".";
        }

        var runs = Runs(words);

        // The whole query as one word, read the way a component's name is kept (see NamePhrase).
        var named = BlazorUISuggest.Normalize(query);

        var hits = _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, terms, runs)))
            .Where(hit => hit.Score > 0)
            // The component a query names outright leads, whatever outscores it: someone typing "chart"
            // wants BitChart, though BitChartLegacy's example titles say "chart" more often, and
            // "accordion" is BitAccordion, though BitAccordionList lists the word as an alias of its
            // own. The site's search box ranks a name above everything else the same way.
            .OrderByDescending(hit => hit.Entry.NamePhrase is { } name && Equivalent(name, named))
            .ThenByDescending(hit => hit.Score)
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

    private static int Score(Entry entry, string[] terms, (string Text, int Words)[] runs)
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
            var isNameHead = entry.NameHead is { } head && Equivalent(head, term);
            var inTitle = isTitleWord ? 0 : Count(entry.LoweredTitle, term);
            var inBoosted = Count(entry.LoweredBoosted, term) + (entry.LoweredKeywords.Contains(term, StringComparison.Ordinal) ? 1 : 0);
            var inBody = Count(entry.LoweredBody, term);

            if (isAlias is false && isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "Dropdown" wants the component, not the paragraphs that mention it. An alias is
            // worth more still - it is the library saying "this is what you call it" - and the word of a
            // name that says what the component is, more than both (see NameHead).
            score += (isTitleWord ? 12 : 0) + (isNameHead ? 12 : 0) + inTitle * 3 + inBoosted * 5;
            alias = Math.Max(alias, aliasHit);
            body += Math.Min(inBody, 3);
        }

        // The strongest alias hit, not the sum of them: an entry that lists five alternative names
        // is not five times more relevant to a query that happens to touch two of them. Without the
        // cap, "loading skeleton placeholder" ranks BitTextShimmer - whose nav entry lists both
        // "Skeleton" and "Loading" - above BitShimmer, which is the skeleton.
        // Words of the query that spell one of its names, its own included, are that name named once
        // per word, so a whole-alias hit for each (see SpelledNameSpan).
        score += Math.Max(alias, 25 * SpelledNameSpan(entry, runs));

        // A body here is a whole component description or a chapter of the theming guide, and a long
        // enough document mentions everything. Capping the prose as a whole rather than per term is
        // what lets full text be indexed at all: it can rank a hit and break a tie, but it can never
        // outweigh the entry whose NAME was asked for.
        score += Math.Min(body, 9);

        return matched == 0 ? 0 : score * matched * entry.Weight;
    }

    /// <summary>
    /// How many words of the query the longest run spelling one of the entry's names covers - its
    /// own or an alias: two for "app bar", which is BitHeader's "AppBar", and for "search box", one
    /// for "searchbox" - or 0 when no run does.
    /// <para>
    /// A query writes a multiword name the way a sentence would, a word at a time, and scored a word
    /// at a time each is only a word of the name, which other entries match as well or better: for
    /// "app bar", BitNavBar has "bar" in its name and in its own "TabBar", BitChart has bars in its
    /// example titles, and both ranked above BitHeader; for "search box", BitDropdown's examples
    /// ("SearchBox", "Search options", "ComboBox") ranked it above BitSearchBox. So the run counts as
    /// the whole name once for every word it spans, which puts the component it names ahead of the
    /// ones that only share its words.
    /// </para>
    /// <para>
    /// A run of one word counts too. For an alias that changes nothing, since a word that is a whole
    /// alias already scores as one; but a component's own name typed as one word is none of its title
    /// words ("searchbox" is not "search" or "box"), and BitDropdown's "SearchBox" example outranked it.
    /// </para>
    /// </summary>
    private static int SpelledNameSpan(Entry entry, (string Text, int Words)[] runs)
    {
        if (entry.NamePhrase is null && entry.AliasPhrases.Length == 0) return 0;

        var longest = 0;

        foreach (var run in runs)
        {
            if (run.Words > longest && IsNameOf(entry, run.Text)) longest = run.Words;
        }

        return longest;
    }

    /// <summary>Whether a word is one of the names the entry goes by, its own or an alias.</summary>
    private static bool IsNameOf(Entry entry, string word)
    {
        if (entry.NamePhrase is { } name && Equivalent(name, word)) return true;

        foreach (var alias in entry.AliasPhrases)
        {
            if (Equivalent(alias, word)) return true;
        }

        return false;
    }

    /// <summary>
    /// Every run of consecutive words of the query, joined into one, with the number of words it
    /// takes - "app", "appbar" and "bar" for "app bar" - the ones a name may be spelled in.
    /// <para>
    /// Read off the words as written rather than off the terms: a name is spelled with the short and
    /// common words the terms leave out as often as not - "ai chat" is BitAiChat, which "chat" alone
    /// ranked below BitAiPromptInput, and "md viewer" is BitMarkdownViewer's MdViewer - and two words
    /// with another left out between them are not adjacent.
    /// </para>
    /// </summary>
    private static (string Text, int Words)[] Runs(string[] words)
    {
        var longest = _longestName.Value;
        var count = Math.Min(words.Length, MaxWords);
        var runs = new List<(string Text, int Words)>();

        for (var start = 0; start < count; start++)
        {
            var text = string.Empty;

            for (var end = start; end < count; end++)
            {
                text += words[end];

                if (text.Length > longest) break;

                runs.Add((text, end - start + 1));
            }
        }

        return [.. runs];
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

    /// <summary>The words of a query as written, in order and lower-cased.</summary>
    private static string[] Words(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return [.. query.Split(_separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(word => word.ToLowerInvariant())];
    }

    /// <summary>The words of a query that are scored: the ones of three letters or more, less the stop words, once each.</summary>
    private static string[] Tokenize(string[] words)
    {
        return [.. words
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
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
                LoweredKeywords = component.Keywords?.ToLowerInvariant() ?? string.Empty,
                NamePhrase = BlazorUISuggest.Normalize(component.Name),
                // Its first word is the Bit prefix every name here carries.
                NameHead = SplitWords(component.Name) is { Length: > 2 } words ? words[^1].ToLowerInvariant() : null,
                AliasPhrases = [.. Aliases(component.Aliases).Select(alias => new string([.. alias.Where(char.IsLetterOrDigit)]).ToLowerInvariant())],
                AliasWords = [.. Aliases(component.Aliases).SelectMany(SplitWords).Select(word => word.ToLowerInvariant())]
            });

            // A hit below the component is answered with its own rows rather than with the whole
            // reference: the section argument narrows to exactly the name the hit is about, which on
            // the largest components is the difference between one table row and fifty thousand
            // characters to search through.
            string Narrowed(string section) => $"GetBitBlazorUIComponent(name: \"{component.Name}\", section: \"{section}\")";

            // One entry per name rather than per overload, titled by the name alone: a signature's
            // parameter names, types and defaults are not what the member is called, and as title words
            // they made every method taking a `title` or a `value` a title hit for those words - once
            // per overload. The section a hit names is matched on the same bare name. (A service's
            // methods are in the table a component's parameters are in.)
            IEnumerable<Entry> ByName(string kind, IReadOnlyList<ComponentMember> members)
                => members.GroupBy(m => BlazorUIComponentCatalog.Bare(m.Name), StringComparer.Ordinal)
                          .Select(overloads => new Entry(kind, $"{component.Name}.{overloads.Key}", component.Name, Narrowed(overloads.Key),
                              string.Join(' ', overloads.Select(m => m.Description).Where(d => string.IsNullOrWhiteSpace(d) is false).Distinct(StringComparer.Ordinal)),
                              string.Join(' ', overloads.Select(m => m.Type).Distinct(StringComparer.Ordinal))));

            entries.AddRange(ByName(component.IsComponent ? "Parameter" : "Member", component.Parameters));
            entries.AddRange(ByName("Public member", component.PublicMembers));

            // The custom properties are the half of a component's look that no parameter reaches, so
            // a search for "action button hover background" has to land on one rather than on the
            // nearest enum. The default rides in the boosted text the way a parameter's type does:
            // it is what names the global token the variable falls back to.
            foreach (var variable in component.CssVariables)
            {
                entries.Add(new Entry("CSS variable", variable.Name, component.Name, Narrowed(variable.Name),
                    variable.Description ?? string.Empty, variable.Default ?? string.Empty));
            }

            foreach (var type in component.OwnTypes)
            {
                // The member names go in the body rather than in the boosted text: a class-styles bag
                // is thirty part names, and boosting each of them made "BitDatePickerClassStyles"
                // outrank BitDateRangePicker for "pick a date range". A body is capped as a whole,
                // so the names can still find the type without winning on volume.
                entries.Add(new Entry(type.IsEnum ? "Enum" : "Class", type.Name, component.Name, Narrowed(type.Name),
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

        // A name one package adds to another package's type is written on the type it extends and
        // stored nowhere near it, so a search for "material dark" has to be told about it here or
        // it lands on the theming chapter that merely mentions it. Indexed under the name it is
        // read by - BitThemePresets.MaterialDark - which is also what a query is likely to type.
        foreach (var group in BlazorUIExtensionMembers.All)
        {
            foreach (var member in group.Members)
            {
                entries.Add(new Entry("Extension member", $"{group.ReceiverName}.{member.Name}", group.Package.PackageId,
                    $"GetBitBlazorUIType(typeName: \"{group.ReceiverName}\")",
                    member.Summary ?? string.Empty, member.Value ?? string.Empty));
            }
        }

        entries.AddRange(ThemingChapters());

        foreach (var model in BlazorUISetupGuide.HostingModels)
        {
            // The words only a setup question is asked in, as aliases: a guide is titled by its hosting
            // model rather than by what it answers, and its keywords as prose are capped like any body.
            // Without them, the components whose descriptions mention what the app registered with
            // AddBitBlazorUIAIServices outrank the guide for "register services stylesheet script".
            // The generic words - package, services, script, tag - stay prose, or "tag" would put four
            // setup guides between BitTag and its own parameters.
            entries.Add(new Entry("Setup guide", model, "Hosting model", $"GetBitBlazorUISetupGuide(hostingModel: \"{model}\")",
                "install package nuget register services AddBitBlazorUIServices stylesheet script tag imports namespace setup wiring", string.Empty)
            {
                AliasPhrases = ["install", "nuget", "register", "addbitblazoruiservices", "stylesheet", "setup", "wiring"]
            });
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
