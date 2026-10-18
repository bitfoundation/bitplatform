using Bit.Bsqlite.Demo.Server.Dtos;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: every section of every document, every
/// public type and member of the five packages, the storage backends, the setup guides, the two labs
/// and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there - "keep
/// the user's data when OPFS appears" is a guide section, a resolver, a migration mode, a demo page and
/// a lab all at once. Each hit therefore carries the exact follow-up tool call that returns the full
/// text, so one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BsqliteSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "AllowNonPersistentFallback" is found
        /// by "non persistent fallback" - and so a query word only counts as a title hit when it IS one of
        /// those words, rather than merely appearing inside one ("lite" inside "Bsqlite").
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Title);
    }

    private const int MaxTerms = 16;

    /// <summary>The most hits one search may return, whatever the caller asked for.</summary>
    private const int MaxHits = 50;

    private const int MaxSuggestions = 8;

    /// <summary>The weight of an entry that answers a question: a document section, a type, a member, a guide.</summary>
    private const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a source file. They are examples, not answers, and their titles are paths whose
    /// segments ("Client", "Pages", "Server") are common words carrying no topic - without this, every
    /// question phrased with one of them is answered with a directory listing.
    /// </summary>
    private const int ExampleWeight = 4;

    /// <summary>
    /// The most an entry can earn from prose, however much of it there is. A long section mentions
    /// everything, so without a ceiling the biggest document in the corpus is the top hit for every
    /// query - and a name match, which is the far stronger signal, never gets ahead of it.
    /// </summary>
    private const int MaxBodyScore = 8;

    private static readonly Lazy<Entry[]> _entries = new(Build);

    /// <summary>
    /// The words every entry in this index carries because of what the index is about. They separate
    /// nothing, and they do worse than nothing: a term that matches an entry counts towards the "how
    /// many terms matched" multiplier, so left in, the longest documents win every query on the
    /// strength of a word that is in all of them.
    /// <para>
    /// Unlike a stop word they are dropped only when the query says something else as well, since
    /// "bsqlite" and "sqlite blazor library" are what a caller reaches for first and are made of
    /// nothing but these.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> _ambientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bsqlite", "bit", "blazor", "library", "sqlite"
    };

    /// <summary>
    /// The sections that are about a document rather than about the library: tables of contents, which
    /// name every topic and so match every query, link lists, the license, and the README's paragraph on
    /// this MCP server, which mentions searching, setup and storage while answering none of them. They
    /// stay readable through GetBsqliteDocument; they are only kept out of the ranking.
    /// </summary>
    private static readonly HashSet<(string Document, string Heading)> _metaSections =
    [
        (BsqliteSourceCatalog.ReadmeName, "Table of Contents"),
        (BsqliteSourceCatalog.ReadmeName, "Documentation"),
        (BsqliteSourceCatalog.ReadmeName, "MCP server (for AI agents)"),
        (BsqliteSourceCatalog.ReadmeName, "License"),
        ("Bit.Bsqlite", "Links"),
    ];

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto", "why", "off", "will"
    };

    public static BsqliteSearchResultDto Search(string? query, int limit)
    {
        var terms = Tokenize(query);

        if (terms.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than returned
            // as an empty list the agent would read as "Bsqlite cannot do this".
            return new BsqliteSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held no word longer than two letters that was not a filler word, so there was " +
                          "nothing to rank by. Search for the thing itself - 'opfs', 'live query', 'migration' - " +
                          "rather than for a sentence."
            };
        }

        var ranked = _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, terms)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // Two entries that would be read through the same call under the same name are one hit: a
            // method with two overloads is two members and one place to read about them. A call that takes
            // no argument returns the same text whichever entry led to it, so its name does not count.
            .DistinctBy(hit => hit.Entry.Tool.EndsWith("()", StringComparison.Ordinal) ? (string.Empty, hit.Entry.Tool) : (hit.Entry.Title, hit.Entry.Tool))
            .ToArray();

        var take = Math.Clamp(limit, 1, MaxHits);

        var hits = Cover(ranked, terms, take).Select(hit => new BsqliteSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = Snippet(hit.Entry, terms)
        }).ToArray();

        if (hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that query
            // one message ago. Both are worth saying only when there is nothing else to say.
            return new BsqliteSearchResultDto
            {
                Hits = hits,
                HasMore = ranked.Length > hits.Length
            };
        }

        // A search that found nothing is where an agent is most likely to conclude the library has no
        // such feature and hand-roll one. Whatever came closest is named instead, so the next call is a
        // better query rather than a worse decision.
        var nearby = Suggest(terms);

        return new BsqliteSearchResultDto
        {
            Terms = terms,
            Hits = [],
            Message = nearby.Length > 0
                ? $"Nothing matched {string.Join(" + ", terms)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", terms)}. Try one word rather than several, or call " +
                   "GetBsqliteDocument or GetBsqliteApi with no argument at all for the index of what there is. " +
                   "A concept Bsqlite does not name is often the same idea under another word - 'offline' or " +
                   "'local database' is the browser database itself, 'backend' or 'persistence' is a storage " +
                   "provider, 'sticky' is the binding store, 'reactive' or 'observable' is a live query, 'mock' " +
                   "or 'fake' is the in-process transport, 'backup' is export.",
            DidYouMean = nearby.Length > 0 ? nearby : null
        };
    }

    /// <summary>
    /// The first <paramref name="take"/> hits, with every query word that names something kept in view.
    /// A request usually joins two things - "migrate from indexeddb to opfs" is a migration AND two
    /// backends - and the one with more material behind it would otherwise fill every place, leaving the
    /// caller to conclude the other has no answer. So for each word that is the name of some entry yet
    /// names none of the top hits, the best entry it does name takes the place of the lowest-ranked hit
    /// that is not itself there for that reason. The order stays the ranking's.
    /// </summary>
    private static (Entry Entry, int Score)[] Cover((Entry Entry, int Score)[] ranked, string[] terms, int take)
    {
        var top = ranked.Take(take).ToList();
        if (top.Count < take || terms.Length < 2) return [.. top];

        var kept = new HashSet<Entry>();

        foreach (var term in terms)
        {
            if (top.Any(hit => hit.Entry.TitleWords.Any(word => Equivalent(word, term)))) continue;

            var best = ranked.Skip(take).FirstOrDefault(hit => hit.Entry.TitleWords.Any(word => Equivalent(word, term)));
            if (best.Entry is null) continue;

            var replace = top.FindLastIndex(hit => kept.Contains(hit.Entry) is false);
            if (replace < 0) break;

            top[replace] = best;
            kept.Add(best.Entry);
        }

        return [.. top.OrderByDescending(hit => hit.Score).ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// The titles that nearly matched: an entry whose name merely begins with one of the query's words,
    /// which the ranking itself ignores on purpose - a prefix is far too weak a signal to rank a hit by,
    /// and exactly strong enough to suggest a word to search for instead.
    /// </summary>
    private static string[] Suggest(string[] terms)
    {
        return [.. _entries.Value
            .Where(entry => terms.Any(term => term.Length >= 4
                                              && entry.TitleWords.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                                                                              // A one- or two-letter title word ("1", "Db") is the start of
                                                                              // half the dictionary, so it suggests nothing.
                                                                              || (word.Length >= 4 && term.StartsWith(word, StringComparison.OrdinalIgnoreCase)))))
            .Select(entry => entry.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)];
    }

    private static int Score(Entry entry, string[] terms)
    {
        var score = 0;
        var matched = 0;
        var named = 0;

        foreach (var term in terms)
        {
            var isTitleWord = entry.TitleWords.Any(word => Equivalent(word, term));

            // Text is counted by the singular, which a plural query word would otherwise never find inside:
            // "queries" has to count the "Query" of AsLiveQuery, and "backends" every "backend".
            var stem = Stem(term);
            var inTitle = isTitleWord ? 0 : Count(entry.Title, stem);
            var inBoosted = Count(entry.Boosted, stem);
            var inBody = Count(entry.Body, stem);

            if (isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;
            if (isTitleWord || inTitle + inBoosted > 0) named++;

            // A term in a name is worth far more than the same term buried in prose: someone asking for
            // "AllowNonPersistentFallback" wants the option, not the paragraphs that happen to mention it.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, MaxBodyScore);
        }

        // Every term matching is the strongest signal a hit is the right one - and a term the entry is named
        // by counts twice, since every long section mentions "database", "open" and "page" somewhere, and a
        // multiplier fed only by prose hands the longest of them every query phrased in plain words. The
        // weight is what keeps a worked example from outranking the reference that answers the question.
        return matched == 0 ? 0 : score * (matched + named) * entry.Weight;
    }

    /// <summary>
    /// Same word, whatever form it is in - "migrate" has to find the section called "Migration",
    /// "queries" the one about live queries, and nobody phrases a question in the number or tense the
    /// heading happens to use.
    /// </summary>
    private static bool Equivalent(string word, string term)
    {
        return string.Equals(word, term, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Stem(word), Stem(term), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A word without its plural, its -ing, -ed, -ion or -ies, and a final silent e: "migrates",
    /// "migrating", "migrated" and "migration" are all "migrat", "queries" and "query" both "quer". Only
    /// ever cut where at least three letters remain, so "wal" or "used" keep the letters that make them a
    /// word. Prose is counted by the stem too, which as a substring also finds every longer form of it.
    /// </summary>
    private static string Stem(string word)
    {
        var stem = word.ToLowerInvariant();

        if (stem.Length > 5 && stem.EndsWith("ies", StringComparison.Ordinal)) stem = $"{stem[..^3]}y";
        if (stem.Length > 4 && stem.EndsWith('s') && stem.EndsWith("ss", StringComparison.Ordinal) is false) stem = stem[..^1];
        if (stem.Length > 5 && stem.EndsWith("ing", StringComparison.Ordinal)) stem = stem[..^3];
        else if (stem.Length > 5 && stem.EndsWith("ion", StringComparison.Ordinal)) stem = stem[..^3];
        else if (stem.Length > 4 && stem.EndsWith("ed", StringComparison.Ordinal)) stem = stem[..^2];
        if (stem.Length > 3 && (stem.EndsWith('e') || stem.EndsWith('y'))) stem = stem[..^1];

        return stem;
    }

    /// <summary>
    /// How often a term starts a word of the text. Only the start of a word counts - the start of the
    /// text, a letter after a non-letter, or a capital after a lower-case letter, so "query" counts in
    /// "AsLiveQuery" - because inside a word a term is a different word: every "Bsqlite" type name
    /// contains "sqlite", and counting it there turns "sqlite version" into a search for the library's
    /// name. The end of the word stays open, so a stem still finds its longer forms ("migrat" in "migration").
    /// </summary>
    private static int Count(string text, string term)
    {
        if (text.Length == 0) return 0;

        var count = 0;
        var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);

        while (index >= 0)
        {
            if (StartsWord(text, index)) count++;
            index = text.IndexOf(term, index + term.Length, StringComparison.OrdinalIgnoreCase);
        }

        return count;
    }

    private static bool StartsWord(string text, int index)
    {
        if (index == 0) return true;

        var previous = text[index - 1];

        return char.IsLetterOrDigit(previous) is false || (char.IsUpper(text[index]) && char.IsLower(previous));
    }

    /// <summary>
    /// The text that shows why an entry was found: its prose, or - when only its boosted words matched, as
    /// a member found by its type is - those words, so the snippet never reads as a hit for nothing.
    /// </summary>
    private static string Snippet(Entry entry, string[] terms)
    {
        var stems = terms.Select(Stem).ToArray();

        bool Mentions(string text) => stems.Any(stem => Count(text, stem) > 0);

        return Snippet(Mentions(entry.Body) || Mentions(entry.Boosted) is false ? entry.Body : entry.Boosted, terms);
    }

    private static string Snippet(string body, string[] terms)
    {
        if (body.Length == 0) return string.Empty;

        var index = 0;

        // The same word starts the snippet that was counted for the score, not "sqlite" inside "Bsqlite".
        foreach (var term in terms.Select(Stem))
        {
            var at = body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            while (at >= 0 && StartsWord(body, at) is false) at = body.IndexOf(term, at + term.Length, StringComparison.OrdinalIgnoreCase);

            if (at >= 0)
            {
                index = at;
                break;
            }
        }

        // A snippet is a reason to make the follow-up call, not a substitute for making it: enough of the
        // surrounding text to tell the right hit from the wrong one, and no more. Every extra character
        // here is paid for by every hit of every search, most of which are not the one.
        var start = Math.Max(0, index - 60);
        var length = Math.Min(180, body.Length - start);
        var snippet = body.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return $"{(start > 0 ? "..." : null)}{snippet}{(start + length < body.Length ? "..." : null)}";
    }

    /// <summary>Splits a name or heading into words, breaking camel-case humps as well as punctuation.</summary>
    private static string[] SplitWords(string text)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            // A capital after a lowercase letter starts a new word ("AllowNonPersistentFallback" -> allow non
            // persistent fallback), while a run of capitals stays together ("OPFS", "WAL").
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
            // One- and two-letter words ("a", "in", "do") match everything and rank nothing - and the words a
            // question is phrased with do worse than nothing: "how do I keep data FROM indexeddb" would
            // otherwise score a section whose heading merely contains "from".
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question is
            // phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];

        return DropAmbientWords(terms);
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(1024);

        foreach (var document in BsqliteSourceCatalog.Documents)
        {
            // The document as a whole, for the question that asks for it by name ("the storage guide"). Its
            // body is what no section entry covers - the prose above its first section, which for a document
            // without any (the limits table) is all of it.
            entries.Add(new Entry("Document", document.Title, document.Name,
                $"GetBsqliteDocument(name: \"{document.Name}\")", BsqliteSourceCatalog.GetPreamble(document.Name) ?? document.Summary ?? string.Empty,
                $"{document.Name.Replace('-', ' ')} {document.Kind.ToLowerInvariant()}"));

            foreach (var section in BsqliteSourceCatalog.GetSections(document.Name))
            {
                if (_metaSections.Contains((document.Name, section.Heading))) continue;

                var body = BsqliteSourceCatalog.GetDocumentSection(document.Name, section.Key) ?? string.Empty;

                entries.Add(new Entry(document.Kind == "Package" ? "Package section" : "Guide section", section.Heading,
                    section.Parent is null ? document.Name : $"{document.Name} > {section.Parent}",
                    $"GetBsqliteDocument(name: \"{document.Name}\", section: \"{section.Key}\")", body, string.Empty));
            }
        }

        foreach (var type in BsqliteApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Namespace,
                $"GetBsqliteApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BsqliteApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // The conformance kit's tests are named as sentences ("ProbeAsync_ReportsUnavailability_RatherThanThrowing"),
            // so they match half the words of any question about storage while answering none of it. The kit is
            // found by its type, whose reference lists every rule; of its members only the ones a test class
            // overrides are indexed, since those are what a backend author has to write.
            var kit = type.Namespace.EndsWith(".ConformanceTests", StringComparison.Ordinal);

            // One entry per member NAME, not per member: two overloads of UseBitBsqlite are two members and
            // one thing to know about, and both are read by the same call anyway. Their documentation is
            // searched as one body, so a term in either overload finds the member. A constructor is named
            // after its type, which the type's own entry already is.
            foreach (var overloads in details.Members.Where(member => member.Kind != "Constructor" && (kit is false || member.Protected)).GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                // Overloads usually share their documentation; it is searched, and quoted in a snippet, once.
                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBsqliteApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim()).Distinct(StringComparer.Ordinal)).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim()));
            }
        }

        // The table as a whole, for the question that asks for the list itself ("which backends are there")
        // rather than for one of its rows.
        entries.Add(new Entry("Storage backend", "Storage backends", null,
            "GetBsqliteStorageProviders()", BsqliteStorageCatalog.RenderIndex(), "storage backend backends provider providers capabilities persistence"));

        foreach (var provider in BsqliteStorageCatalog.Providers)
        {
            entries.Add(new Entry("Storage backend", provider.TypeName, provider.Name,
                $"GetBsqliteStorageProviders(provider: \"{provider.Name}\")",
                $"{provider.Summary} {provider.Remarks} {string.Join(", ", provider.Capabilities.Select(capability => $"{capability.Name} {capability.Value}"))}",
                $"storage backend provider {provider.Name.Replace('-', ' ')} {provider.Package}"));
        }

        foreach (var scenario in BsqliteSetupGuide.Scenarios)
        {
            entries.Add(new Entry("Setup guide", $"Setup: {BsqliteSetupGuide.Title(scenario)}", scenario,
                $"GetBsqliteSetupGuide(scenario: \"{scenario}\")", BsqliteSetupGuide.Summary(scenario),
                $"setup install wiring register {scenario.Replace('-', ' ')}"));
        }

        entries.Add(new Entry("Storage lab", "Plan a storage selection", null,
            "PlanBsqliteStorageSelection()",
            "Runs the library's own storage selection - resolver, migrator and session factory - on a browser you describe: " +
            "which backends are registered, available and persistent, the preference order, AllowNonPersistentFallback, the " +
            "migration mode and an existing sticky binding; reports the backend chosen, every candidate's status and why, " +
            "the binding before and after, any automatic migration, and what the library logged.",
            "plan predict simulate selection fallback binding sticky migration migrate unavailable which backend chosen"));

        entries.Add(new Entry("SQL check", "Check SQL against the guards", null,
            "CheckBsqliteSql()",
            "Runs BsqliteFeatureGuards and BsqliteTableNames on SQL without executing it: which backends refuse it - WAL " +
            "everywhere, ATTACH on OPFS, PRAGMA page_size on IndexedDB and Cache Storage - with the guard's own message, " +
            "and which tables a live query over it watches.",
            "check validate guard guards refused rejected pragma wal attach page_size journal_mode tables live query sql"));

        foreach (var file in BsqliteSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBsqliteSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }

    /// <summary>
    /// The query without the words that are true of everything here - unless that is all it said, in
    /// which case they are all it can be searched by.
    /// </summary>
    private static string[] DropAmbientWords(string[] terms)
    {
        var meaningful = terms.Where(term => _ambientWords.Contains(term) is false).ToArray();

        return meaningful.Length > 0 ? meaningful : terms;
    }
}
