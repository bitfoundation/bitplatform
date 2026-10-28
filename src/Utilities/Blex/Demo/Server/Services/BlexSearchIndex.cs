using System.Text;
using Bit.Blex.Demo.Server.Dtos;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, the docs pages,
/// every public type and member, the generator's diagnostics and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there - "how do
/// I cancel a running request?" is a guide section, a docs page, an attribute property and a demo store
/// all at once. Each hit therefore carries the exact follow-up tool call that returns the full text, so
/// one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BlexSearchIndex
{
    /// <param name="Name">
    /// What the entry is called, when that is less than its title: a member's own name, without the name of
    /// the type it is a member of. Every member of <c>BlexInfiniteQuery&lt;TPage, TParam&gt;</c> shares that
    /// type's name, and counted as theirs, "query" or "page" would name all of them at once.
    /// </param>
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight, string? Name = null)
    {
        /// <summary>
        /// The name split into words, camel-case humps included, so "DebounceMilliseconds" is found by
        /// "debounce" - and a query word only counts as a title hit when it IS one of those words.
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Name ?? Title);

        /// <summary>
        /// The stems of <see cref="TitleWords"/>, which is what a term is matched against: "effect" has to
        /// find the page called "Effects", "persisting" the option called Persist. Worked out once here
        /// rather than for every term against every entry of every search.
        /// </summary>
        public HashSet<string> TitleStems { get; } = [.. SplitWords(Name ?? Title).Select(Stem)];
    }

    /// <summary>A word of the query, and the stem it is matched by.</summary>
    private readonly record struct Term(string Word, string[] Stems);

    /// <summary>How one term matched one entry.</summary>
    private readonly record struct Match(bool IsTitleWord, int InTitle, int InBoosted, int InBody)
    {
        public bool Found => IsTitleWord || InTitle + InBoosted + InBody > 0;

        public bool Named => IsTitleWord || InTitle + InBoosted > 0;

        // A term in a name is worth far more than the same term buried in prose.
        public int Points => (IsTitleWord ? 12 : 0) + InTitle * 3 + InBoosted * 5 + Math.Min(InBody, MaxBodyScore);
    }

    private const int MaxTerms = 16;

    /// <summary>The most hits one search may return, whatever the caller asked for.</summary>
    private const int MaxHits = 50;

    private const int MaxSuggestions = 8;

    /// <summary>The weight of an entry that answers a question: a guide section, a page, a member, a diagnostic.</summary>
    private const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a source file. They are examples, not answers, and their titles are paths whose
    /// segments ("Client", "Stores") are common words carrying no topic.
    /// </summary>
    private const int ExampleWeight = 4;

    /// <summary>The most an entry can earn from prose, so the biggest document is not the top hit for every query.</summary>
    private const int MaxBodyScore = 8;

    /// <summary>
    /// The most entries a term can be found in and still single out the one a question is about. Past it
    /// the term is common to a whole subsystem - "value" is in a hundred entries, "page" in every member of
    /// the paged queries - and the more entries it is in, the less it says about which of them is the
    /// answer. So its weight falls (<see cref="WeightOf"/>), and the rarer words of a question decide what
    /// it is about: "a counter whose value survives a page reload" is about the reload.
    /// </summary>
    private const int DistinctiveSpread = 12;

    private static readonly Lazy<Entry[]> _entries = new(Build);

    /// <summary>
    /// The one- and two-letter words that name something here. The tokenizer drops a word that short
    /// unless it is one of these or holds a digit, because the rest ("a", "in", "do") match everything.
    /// </summary>
    private static readonly Lazy<HashSet<string>> _shortNames = new(() =>
        new(_entries.Value.SelectMany(entry => entry.TitleWords).Where(word => word.Length <= 2), StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// The words every entry here carries because of what the index is about. They separate nothing, so
    /// they are dropped - unless they are all the query said.
    /// </summary>
    private static readonly HashSet<string> _ambientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "blex", "bit", "blazor", "state", "management"
    };

    /// <summary>The members every type has: <see cref="object"/>'s, overridden or generated.</summary>
    private static readonly HashSet<string> _objectMembers = new(StringComparer.Ordinal) { "Equals", "GetHashCode", "ToString" };

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // A pronoun names nothing.
        "them", "they", "their", "these", "those", "whose",
        // The short ones that are also the hump of some member's name ("On" of OnError, "Is" of
        // IsLoading), which would otherwise pass for a short name.
        "a", "an", "as", "at", "be", "by", "do", "go", "i", "if", "in", "is", "it", "me", "my", "no",
        "of", "on", "or", "so", "to", "up", "us", "we"
    };

    /// <summary>
    /// The words a caller from another state library - or one who only says what the app has to do - uses
    /// for a Blex concept. They are matched as extra vocabulary of the entries that answer them, so
    /// "reducer" finds actions, "selector" finds the subscription APIs and a value that "survives a reload"
    /// finds persistence.
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Actions"] = "reducer reducers dispatch mutation mutate creator createaction typed strict",
        ["Effects"] = "async side effect thunk saga fetch http loading",
        ["Computed state"] = "derived getter memoized memo selector",
        ["Components & selectors"] = "rerender re-render subscribe subscription selector usestore useselector observer observable rx",
        ["Scoped stores"] = "instance instances componentstore component-scoped provider context multiple dialog tab",
        ["Persistence"] = "localstorage sessionstorage save reload survive rehydrate hydrate",
        ["Undo & redo"] = "history undo redo",
        ["DevTools & time travel"] = "redux devtools timetravel debugger dispatcher",
        ["Testing"] = "test unit deterministic fake clock timeprovider replay",
        ["Entity adapter"] = "normalized collection crud list",
        ["Middleware"] = "interceptor logger logging veto",
    };

    public static BlexSearchResultDto Search(string? query, int limit)
    {
        var words = Tokenize(query);

        if (words.Length == 0)
        {
            return new BlexSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held nothing but filler words and words too short to search by, so there was " +
                          "nothing to rank by. Search for the thing itself - 'effect cancel', 'persist', 'selector' - " +
                          "rather than for a sentence."
            };
        }

        Term[] terms = [.. words.Select(ToTerm)];

        // Every term against every entry, once: how many entries a term is found in is its weight,
        // which every entry's score needs before any of them can be worked out.
        var matches = _entries.Value.Select(entry => (Entry: entry, Matches: terms.Select(term => MatchOf(entry, term)).ToArray())).ToArray();
        double[] weights = [.. terms.Select((_, i) => WeightOf(matches.Count(candidate => candidate.Matches[i].Found)))];

        // A snippet is cut around the rarest word of the query it holds, which is the one that says why
        // the hit is there: "reload" rather than "value" for "a counter whose value survives a page reload".
        string[] stems = [.. terms.Select((term, i) => (term.Stems, Weight: weights[i])).OrderByDescending(term => term.Weight).SelectMany(term => term.Stems)];

        var ranked = matches
            .Select(candidate => (candidate.Entry, Score: Score(candidate.Entry, candidate.Matches, weights)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // Two entries read through the same call under the same name are one hit: a method with two
            // overloads is two members and one place to read about them.
            .DistinctBy(hit => hit.Entry.Tool.EndsWith("()", StringComparison.Ordinal) ? (string.Empty, hit.Entry.Tool) : (hit.Entry.Title, hit.Entry.Tool))
            .ToArray();

        var take = Math.Clamp(limit, 1, MaxHits);

        var hits = Cover(ranked, terms, take).Select(hit => new BlexSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = Snippet(hit.Entry.Body, stems)
        }).ToArray();

        if (hits.Length > 0)
        {
            return new BlexSearchResultDto
            {
                Hits = hits,
                HasMore = ranked.Length > hits.Length
            };
        }

        // A search that found nothing is where an agent is most likely to conclude the library has no
        // such feature and hand-roll one. Whatever came closest is named instead.
        var nearby = Suggest(words);

        return new BlexSearchResultDto
        {
            Terms = words,
            Hits = [],
            Message = nearby.Length > 0
                ? $"Nothing matched {string.Join(" + ", words)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", words)}. Try one word rather than several, or call " +
                   "GetBlexGuideSection, GetBlexDocsPage or GetBlexApi with no argument at all for the index of what " +
                   "there is. A concept Blex does not name is often the same idea under another word - a 'reducer' is an " +
                   "action, a 'thunk' is an effect, a 'getter' is a computed value.",
            DidYouMean = nearby.Length > 0 ? nearby : null
        };
    }

    /// <summary>A word of the query as it is matched: by its stem.</summary>
    private static Term ToTerm(string word) => new(word, [Stem(word)]);

    /// <summary>
    /// The weight of a term found in <paramref name="spread"/> entries: whole up to
    /// <see cref="DistinctiveSpread"/>, and falling with the square root of the spread past it - in
    /// proportion it would fall so far that a common word could no longer settle anything between the
    /// entries a rare one found.
    /// </summary>
    private static double WeightOf(int spread) => spread > DistinctiveSpread ? Math.Sqrt((double)DistinctiveSpread / spread) : 1;

    /// <summary>
    /// The first <paramref name="take"/> hits, with every query word that names something kept in view. A
    /// request usually joins two things - "persist the undo history" is persistence AND undo - and the one
    /// with more material behind it would otherwise fill every place. So for each word that is the name of
    /// some entry yet names none of the top hits, the best entry it does name takes the place of the
    /// lowest-ranked hit that is not itself there for that reason - nor the only hit in view that another
    /// word names. The order stays the ranking's.
    /// </summary>
    private static (Entry Entry, double Score)[] Cover((Entry Entry, double Score)[] ranked, Term[] terms, int take)
    {
        var top = ranked.Take(take).ToList();
        if (top.Count < take || terms.Length < 2) return [.. top];

        var kept = new HashSet<Entry>();

        foreach (var term in terms)
        {
            if (top.Any(hit => Names(hit.Entry, term))) continue;

            var best = ranked.Skip(take).FirstOrDefault(hit => Names(hit.Entry, term));
            if (best.Entry is null) continue;

            // The first hit is never given up: it is the one a caller reads, and the ranking put it there.
            var replace = top.FindLastIndex(top.Count - 1, top.Count - 1, hit => kept.Contains(hit.Entry) is false
                                                   && terms.Any(other => Names(hit.Entry, other) && top.Count(next => Names(next.Entry, other)) == 1) is false);
            if (replace < 0) continue;

            top[replace] = best;
            kept.Add(best.Entry);
        }

        return [.. top.OrderByDescending(hit => hit.Score).ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)];
    }

    private static bool Names(Entry entry, Term term) => term.Stems.Any(entry.TitleStems.Contains);

    private static string[] Suggest(string[] terms)
    {
        return [.. _entries.Value
            .Where(entry => terms.Any(term => term.Length >= 4
                                              && entry.TitleWords.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                                                                              || term.StartsWith(word, StringComparison.OrdinalIgnoreCase))))
            .Select(entry => entry.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)];
    }

    private static Match MatchOf(Entry entry, Term term)
    {
        var isTitleWord = Names(entry, term);

        // Text is counted by the stem, which a plural query word would otherwise never find inside:
        // "effects" has to count the "Effect" of BlexEffect.
        return new Match(isTitleWord,
            isTitleWord ? 0 : Count(entry.Title, term.Stems),
            Count(entry.Boosted, term.Stems),
            Count(entry.Body, term.Stems));
    }

    private static double Score(Entry entry, Match[] matches, double[] weights)
    {
        var score = 0d;
        var matched = 0d;
        var named = 0d;

        for (var i = 0; i < matches.Length; i++)
        {
            if (matches[i].Found is false) continue;

            matched += weights[i];
            if (matches[i].Named) named += weights[i];

            score += weights[i] * matches[i].Points;
        }

        // Every term matching is the strongest signal a hit is the right one - and a term the entry is named
        // by counts twice, since every long section mentions "store", "state" and "action" somewhere.
        return matched == 0 ? 0 : score * (matched + named) * entry.Weight;
    }

    /// <summary>
    /// A word without its plural, its -ing or -ed, and a final silent e: "effects" is "effect", "persisting"
    /// and "persisted" both "persist". Only ever cut where at least three letters remain. Prose is counted
    /// by the stem too, which as a word start also finds every longer form of it.
    /// </summary>
    private static string Stem(string word)
    {
        var stem = word.ToLowerInvariant();

        if (stem.Length > 4 && stem.EndsWith('s') && stem.EndsWith("ss", StringComparison.Ordinal) is false) stem = stem[..^1];
        if (stem.Length > 5 && stem.EndsWith("ing", StringComparison.Ordinal)) stem = stem[..^3];
        else if (stem.Length > 4 && stem.EndsWith("ed", StringComparison.Ordinal)) stem = stem[..^2];
        if (stem.Length > 3 && stem.EndsWith('e')) stem = stem[..^1];

        return stem;
    }

    private static int Count(string text, string[] stems) => stems.Sum(stem => Count(text, stem));

    /// <summary>How often a term is a word of the text, as <see cref="IndexOfWord"/> finds one.</summary>
    private static int Count(string text, string term)
    {
        var count = 0;

        for (var at = IndexOfWord(text, term, 0); at >= 0; at = IndexOfWord(text, term, at + term.Length)) count++;

        return count;
    }

    /// <summary>
    /// Where the term next starts a word of the text, or -1. Only the start of a word counts - the start of
    /// the text, a letter after a non-letter, or a capital after a lower-case letter, so "storage" counts in
    /// "AddBitBlexLocalStoragePersistence" - because inside a word a term is a different word: "page" is
    /// inside every "TPage", and counting it there answers a question about pages with the paged queries.
    /// The end of the word stays open, so a stem still finds its longer forms ("persist" in "persisted") -
    /// except after a term of one or two letters, which has to be the whole word.
    /// </summary>
    private static int IndexOfWord(string text, string term, int from)
    {
        var at = text.IndexOf(term, from, StringComparison.OrdinalIgnoreCase);

        while (at >= 0 && (StartsWord(text, at) is false || (term.Length <= 2 && EndsWord(text, at + term.Length) is false)))
        {
            at = text.IndexOf(term, at + term.Length, StringComparison.OrdinalIgnoreCase);
        }

        return at;
    }

    private static bool StartsWord(string text, int index)
    {
        if (index == 0) return true;

        var previous = text[index - 1];

        return char.IsLetterOrDigit(previous) is false || (char.IsUpper(text[index]) && char.IsLower(previous));
    }

    private static bool EndsWord(string text, int end)
    {
        if (end == text.Length) return true;

        var next = text[end];

        return char.IsLetterOrDigit(next) is false || (char.IsUpper(next) && char.IsLower(text[end - 1]));
    }

    private static string Snippet(string body, string[] stems)
    {
        if (body.Length == 0) return string.Empty;

        // The same word starts the snippet that was counted for the score - and only failing that wherever
        // a term is at all.
        var index = stems.Select(stem => IndexOfWord(body, stem, 0)).FirstOrDefault(at => at >= 0, -1);
        if (index < 0) index = stems.Select(stem => body.IndexOf(stem, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(at => at >= 0, 0);

        // Enough of the surrounding text to tell the right hit from the wrong one, and no more.
        var start = Math.Max(0, index - 60);
        var length = Math.Min(180, body.Length - start);
        var snippet = body.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return $"{(start > 0 ? "..." : null)}{snippet}{(start + length < body.Length ? "..." : null)}";
    }

    /// <summary>Splits a name or heading into words, breaking camel-case humps as well as punctuation.</summary>
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

        string[] terms = [.. query.Split(['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'],
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => (term.Length > 2 || term.Any(char.IsDigit) || _shortNames.Value.Contains(term)) && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTerms)];

        var meaningful = terms.Where(term => _ambientWords.Contains(term) is false).ToArray();

        return meaningful.Length > 0 ? meaningful : terms;
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(512);

        foreach (var section in BlexSourceCatalog.GuideSections)
        {
            var body = BlexSourceCatalog.GetGuideSection(section.Heading) ?? string.Empty;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBlexGuideSection(heading: \"{section.Heading}\")", body, _synonyms.GetValueOrDefault(section.Heading) ?? string.Empty));
        }

        foreach (var page in DocsPageRenderer.Pages)
        {
            entries.Add(new Entry("Docs page", page.Title, page.Section,
                $"GetBlexDocsPage(slug: \"{page.Slug}\")", page.Summary, _synonyms.GetValueOrDefault(page.Title) ?? string.Empty));
        }

        foreach (var type in BlexApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Package,
                $"GetBlexApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BlexApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: two overloads are one thing to know about, and
            // both are read by the same call anyway.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();
                var body = string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim();

                // What every object has says nothing about the type it is on, unless the type documents
                // what its own does - and split into words it answers questions it has nothing to do
                // with: to the search, GetHashCode is "code", and the top hit for anything done from code.
                if (body.Length == 0 && _objectMembers.Contains(member.Name)) continue;

                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBlexApi(typeName: \"{type.Name}\")",
                    body,
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name));
            }
        }

        foreach (var diagnostic in BlexDiagnosticsReference.Entries)
        {
            entries.Add(new Entry("Diagnostic", $"{diagnostic.Id} {diagnostic.Title}", diagnostic.Severity,
                $"GetBlexDiagnostics(code: \"{diagnostic.Id}\")",
                $"{diagnostic.MessageFormat} {diagnostic.Doc?.Cause}".Trim(),
                $"{diagnostic.Id} build error compile generator"));
        }

        foreach (var file in BlexSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBlexSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }
}
