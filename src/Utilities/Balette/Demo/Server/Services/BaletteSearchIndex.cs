using Bit.Balette.Demo.Server.Dtos;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, every public
/// type and member, the setup guides, the stylesheet's variables, classes and hooks, the search lab
/// and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "a rename command that asks for the new name" is a guide section, a result type, a builder method
/// and a demo page all at once. Each hit therefore carries the exact follow-up tool call that returns
/// the full text, so one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BaletteSearchIndex
{
    /// <summary>
    /// The words a custom property's name abbreviates, so a question asked in words finds it: "active row background"
    /// finds <c>--bit-balette-active-bg</c>, "muted text colour" <c>--bit-balette-fg-muted</c>.
    /// </summary>
    private static string SpelledOut(string variable) => string.Join(' ', variable.Split('-', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part switch
        {
            "bg" => "background",
            "fg" => "foreground text colour color",
            "kbd" => "keyboard key cap",
            "z" => "z-index stacking layer",
            "mono" => "monospace font",
            "gap" => "spacing padding",
            "radius" => "corner rounded",
            "soft" => "tint",
            _ => null,
        })
        .Where(word => word is not null));

    /// <param name="Name">
    /// What the entry is called, when that is less than its title: a member's own name, without the
    /// name of the type it is a member of. The type's name says where the member lives, which every
    /// other member of the type shares - counted as the member's name, "command" would name every
    /// member of <c>BaletteCommandBuilder</c>, and every question with "command" in it would be
    /// answered with them. In the title it still counts, as any other word of a title does that is not a name.
    /// </param>
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight, string? Name = null)
    {
        /// <summary>
        /// The name split into words, camel-case humps included, so "GroupWhenSearching" is found by
        /// "group when searching" - and so a query word only counts as a title hit when it IS one of
        /// those words, rather than merely appearing inside one ("pin" inside "Spinner").
        /// </summary>
        public string[] TitleWords { get; } = SplitWords(Name ?? Title);

        /// <summary>
        /// The stems of <see cref="TitleWords"/>, which is what a term is matched against: "rank" has to
        /// find the section called "Search and ranking", "shortcut" the one about shortcuts, and nobody
        /// phrases a question in the number or tense the heading happens to use. Worked out once here
        /// rather than for every term against every entry of every search.
        /// </summary>
        public HashSet<string> TitleStems { get; } = [.. SplitWords(Name ?? Title).Select(Stem)];
    }

    /// <summary>
    /// A word of the query, and the stems it is matched by: its own, and that of the word Balette
    /// uses for the same thing if it has one in <see cref="_synonyms"/>.
    /// </summary>
    private readonly record struct Term(string Word, string[] Stems);

    /// <summary>How one term matched one entry.</summary>
    private readonly record struct Match(bool IsTitleWord, int InTitle, int InBoosted, int InBody)
    {
        public bool Found => IsTitleWord || InTitle + InBoosted + InBody > 0;

        public bool Named => IsTitleWord || InTitle + InBoosted > 0;

        // A term in a name is worth far more than the same term buried in prose: someone asking
        // for "GroupWhenSearching" wants the option, not the paragraphs that happen to mention it.
        public int Points => (IsTitleWord ? 12 : 0) + InTitle * 3 + InBoosted * 5 + Math.Min(InBody, MaxBodyScore);
    }

    private const int MaxTerms = 16;

    /// <summary>The most hits one search may return, whatever the caller asked for.</summary>
    private const int MaxHits = 50;

    private const int MaxSuggestions = 8;

    /// <summary>The weight of an entry that answers a question: a guide section, a type, a member.</summary>
    private const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a source file. They are examples, not answers, and their titles are paths
    /// whose segments ("Client", "Pages", "Server") are common words carrying no topic - without
    /// this, every question phrased with one of them is answered with a directory listing.
    /// </summary>
    private const int ExampleWeight = 4;

    /// <summary>
    /// The most an entry can earn from prose, however much of it there is. A long section mentions
    /// everything, so without a ceiling the biggest document in the corpus is the top hit for every
    /// query - and a name match, which is the far stronger signal, never gets ahead of it.
    /// </summary>
    private const int MaxBodyScore = 8;

    /// <summary>
    /// The most entries a term can be found in and still single out the one a question is about. Past
    /// it the term is common to a whole subsystem - "command" is in three hundred entries, every member
    /// of the builder and the prose of most sections - and the more entries it is in, the less it says
    /// about which of them is the answer. So its weight falls (<see cref="WeightOf"/>), and the rarer
    /// words of a question decide what it is about: "a command that asks for a name" is about asking.
    /// </summary>
    private const int DistinctiveSpread = 12;

    private static readonly Lazy<Entry[]> _entries = new(Build);

    /// <summary>
    /// The one- and two-letter words that name something here: the "bg" and "fg" of the colour variables,
    /// the "z" of the stacking one. The tokenizer drops a word that short unless it is one of these or
    /// holds a digit ("F2"), because the rest ("a", "in", "do") match everything.
    /// </summary>
    private static readonly Lazy<HashSet<string>> _shortNames = new(() =>
        new(_entries.Value.SelectMany(entry => entry.TitleWords).Where(word => word.Length <= 2), StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// The words every entry in this index carries because of what the index is about. They
    /// separate nothing, and they do worse than nothing: a term that matches an entry counts
    /// towards the "how many terms matched" multiplier, so left in, the longest documents win every
    /// query on the strength of a word that is in all of them.
    /// <para>
    /// Unlike a stop word they are dropped only when the query says something else as well, since
    /// "balette" and "blazor command palette" are what a caller reaches for first and are made of
    /// almost nothing but these.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> _ambientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "balette", "bit", "blazor", "library", "palette"
    };

    /// <summary>
    /// The words a caller uses for something Balette calls otherwise, and the word Balette uses, by
    /// <see cref="SynonymKey"/>. A query word finds what the library's word finds as well as what it
    /// finds itself: a command that "asks for" a name is one whose handler returns a prompt, and
    /// "hotkey" finds every entry about shortcuts.
    /// <para>
    /// Only a word that means nothing else here belongs in this table: "input" is the search box as much
    /// as a prompt's text field, "filter" the matcher as much as a mode, and "step" anything that comes
    /// in order, so they are searched for as themselves.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms = new (string Word, string Means)[]
    {
        ("ask", "prompt"),
        ("asks", "prompt"),
        ("hotkey", "shortcut"),
        ("keybinding", "shortcut"),
        ("alias", "keyword"),
        ("favorite", "pin"),
        ("favourite", "pin"),
        ("submenu", "nested"),
        ("remote", "async"),
    }.ToDictionary(synonym => SynonymKey(synonym.Word), synonym => Stem(synonym.Means), StringComparer.Ordinal);

    private static readonly HashSet<string> _metaSections = new(StringComparer.OrdinalIgnoreCase) { "Documentation", "MCP server" };

    /// <summary>The members every type has: <see cref="object"/>'s, overridden or generated.</summary>
    private static readonly HashSet<string> _objectMembers = new(StringComparer.Ordinal) { "Equals", "GetHashCode", "ToString" };

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // A pronoun names nothing - and "them" is the stem of "theme".
        "them", "they", "their", "these", "those", "whose",
        // The short ones that are also the hump of some member's name ("On" of OnClose, "Is" of
        // IsOpen), which would otherwise pass for a short name.
        "a", "an", "as", "at", "be", "by", "do", "go", "i", "if", "in", "is", "it", "me", "my", "no",
        "of", "on", "or", "so", "to", "up", "us", "we"
    };

    public static BaletteSearchResultDto Search(string? query, int limit)
    {
        var words = Tokenize(query);

        if (words.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than
            // returned as an empty list the agent would read as "Balette cannot do this".
            return new BaletteSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held nothing but filler words and words too short to search by, so there was " +
                          "nothing to rank by. Search for the thing itself - 'prompt', 'shortcut', 'async source' - " +
                          "rather than for a sentence."
            };
        }

        Term[] terms = [.. words.Select(ToTerm)];

        // Every term against every entry, once: how many entries a term is found in is its weight,
        // which every entry's score needs before any of them can be worked out.
        var matches = _entries.Value.Select(entry => (Entry: entry, Matches: terms.Select(term => MatchOf(entry, term)).ToArray())).ToArray();
        double[] weights = [.. terms.Select((_, i) => WeightOf(matches.Count(candidate => candidate.Matches[i].Found)))];

        // A snippet is cut around the rarest word of the query it holds, which is the one that says why
        // the hit is there: "asks" rather than "command" for "a command that asks for a name before it runs".
        string[] stems = [.. terms.Select((term, i) => (term.Stems, Weight: weights[i])).OrderByDescending(term => term.Weight).SelectMany(term => term.Stems)];

        var ranked = matches
            .Select(candidate => (candidate.Entry, Score: Score(candidate.Entry, candidate.Matches, weights)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // Two entries that would be read through the same call under the same name are one hit:
            // a method with two overloads is two members and one place to read about them, and the
            // second copy costs a hit out of the caller's budget to say nothing new. A call that takes
            // no argument returns the same text whichever entry led to it, so its name does not count.
            .DistinctBy(hit => hit.Entry.Tool.EndsWith("()", StringComparison.Ordinal) ? (string.Empty, hit.Entry.Tool) : (hit.Entry.Title, hit.Entry.Tool))
            .ToArray();

        var take = Math.Clamp(limit, 1, MaxHits);

        var hits = Cover(ranked, terms, take).Select(hit => new BaletteSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = Snippet(hit.Entry.Body, stems)
        }).ToArray();

        if (hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that
            // query one message ago, and what it was tokenized into changes nothing about a hit it
            // can read for itself. Both are worth saying only when there is nothing else to say.
            return new BaletteSearchResultDto
            {
                Hits = hits,
                HasMore = ranked.Length > hits.Length
            };
        }

        // A search that found nothing is where an agent is most likely to conclude the library has
        // no such feature and hand-roll one. Whatever came closest is named instead, so the next
        // call is a better query rather than a worse decision.
        var nearby = Suggest(words);

        return new BaletteSearchResultDto
        {
            Terms = words,
            Hits = [],
            Message = nearby.Length > 0
                ? $"Nothing matched {string.Join(" + ", words)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", words)}. Try one word rather than several, or call " +
                   "GetBaletteGuideSection or GetBaletteApi with no argument at all for the index of what " +
                   "there is. A concept Balette does not name is often the same idea under another word - a " +
                   "'command menu', 'quick pick' or 'spotlight' is the palette, a 'submenu' or 'step' is a nested page, " +
                   "'input' or 'text box' is a prompt, 'scope' or 'filter' is a mode, 'remote results' are an async " +
                   "source, 'hotkey' is a shortcut, 'aliases' are keywords, 'favorites' are pins.",
            DidYouMean = nearby.Length > 0 ? nearby : null
        };
    }

    /// <summary>A word of the query as it is matched: by its stem, and by its synonym's if it has one.</summary>
    private static Term ToTerm(string word) =>
        new(word, _synonyms.TryGetValue(SynonymKey(word), out var means) ? [Stem(word), means] : [Stem(word)]);

    /// <summary>
    /// The weight of a term found in <paramref name="spread"/> entries: whole up to
    /// <see cref="DistinctiveSpread"/>, and falling with the square root of the spread past it.
    /// In proportion it would fall so far that a common word could no longer settle anything between
    /// the entries a rare one found.
    /// </summary>
    private static double WeightOf(int spread) => spread > DistinctiveSpread ? Math.Sqrt((double)DistinctiveSpread / spread) : 1;

    /// <summary>
    /// The first <paramref name="take"/> hits, with every query word that names something kept in
    /// view. A request usually joins two things - "a toggle that keeps the palette open" is a toggle AND
    /// a result - and the one with more material behind it would otherwise fill every place, leaving the
    /// caller to conclude the other has no answer. So for each word that is the name of some entry yet
    /// names none of the top hits, the best entry it does name takes the place of the lowest-ranked hit
    /// that is not itself there for that reason - nor the only hit in view that another word names,
    /// which would only hand that word's place to this one. The order stays the ranking's.
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

    /// <summary>
    /// The titles that nearly matched: an entry whose name merely begins with one of the query's
    /// words, which the ranking itself ignores on purpose - a prefix is far too weak a signal to
    /// rank a hit by, and exactly strong enough to suggest a word to search for instead.
    /// </summary>
    private static string[] Suggest(string[] terms)
    {
        return [.. _entries.Value
            .Where(entry => terms.Any(term => term.Length >= 4
                                              && entry.TitleWords.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                                                                              // A one- or two-letter title word ("1", "UI") is the start of
                                                                              // half the dictionary, so it suggests nothing.
                                                                              || (word.Length >= 4 && term.StartsWith(word, StringComparison.OrdinalIgnoreCase)))))
            .Select(entry => entry.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)];
    }

    private static Match MatchOf(Entry entry, Term term)
    {
        var isTitleWord = Names(entry, term);

        // Text is counted by the singular, which a plural query word would otherwise never find
        // inside: "shortcuts" has to count the "Shortcut" of GlobalShortcut, and "pages" every "page".
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

        // Every term matching is the strongest signal a hit is the right one - and a term the entry is
        // named by counts twice, since every long section mentions "command", "open" and "query"
        // somewhere, and a multiplier fed only by prose hands the longest of them every query phrased in
        // plain words. The weight is what keeps a worked example from outranking the reference that
        // answers the question.
        return matched == 0 ? 0 : score * (matched + named) * entry.Weight;
    }

    /// <summary>
    /// A word without its plural, its -ing or -ed, and a final silent e: "ranks", "ranking" and
    /// "ranked" are all "rank", "registering" and "register" both "register". Only ever cut where at
    /// least three letters remain, so "css" or "used" keep the letters that make them a word. Prose is
    /// counted by the stem too, which as a substring also finds every longer form of it.
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

    /// <summary>
    /// A word as <see cref="_synonyms"/> is keyed: its stem, with the y back that an -ies plural loses,
    /// so that a plural reaches the synonym of its singular.
    /// </summary>
    private static string SynonymKey(string word)
    {
        var stem = Stem(word);

        return stem.EndsWith('i') ? $"{stem[..^1]}y" : stem;
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
    /// Where the term next starts a word of the text, or -1. Only the start of a word counts - the start
    /// of the text, a letter after a non-letter, or a capital after a lower-case letter, so "shortcut"
    /// counts in "GlobalShortcut" - because inside a word a term is a different word: every "Balette" type
    /// name contains "let", and counting it there turns "let me" into a search for the library's name.
    /// The end of the word stays open, so a stem still finds its longer forms ("rank" in "ranking") -
    /// except after a term of one or two letters, which has no longer forms and is the start of
    /// hundreds of other words: "k" begins "key" and "keyword". That one has to be the whole word.
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

        // The same word starts the snippet that was counted for the score, not "let" inside "Balette" -
        // and only failing that wherever a term is at all.
        var index = stems.Select(stem => IndexOfWord(body, stem, 0)).FirstOrDefault(at => at >= 0, -1);
        if (index < 0) index = stems.Select(stem => body.IndexOf(stem, StringComparison.OrdinalIgnoreCase)).FirstOrDefault(at => at >= 0, 0);

        // A snippet is a reason to make the follow-up call, not a substitute for making it: enough
        // of the surrounding text to tell the right hit from the wrong one, and no more. Every extra
        // character here is paid for by every hit of every search, most of which are not the one.
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

            // A capital after a lowercase letter starts a new word ("GroupWhenSearching" -> group when
            // searching), while a run of capitals stays together ("CSS", "MCP").
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
            // One- and two-letter words ("a", "in", "do") match everything and rank nothing, unless they
            // are a name or a number (_shortNames) - and the words a question is phrased with do worse
            // than nothing: "how do I open the palette FROM code" would otherwise score a section whose
            // heading merely contains "from".
            .Where(term => (term.Length > 2 || term.Any(char.IsDigit) || _shortNames.Value.Contains(term)) && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];

        return DropAmbientWords(terms);
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(512);

        foreach (var section in BaletteSourceCatalog.GuideSections)
        {
            // Two sections of the README are about the README rather than about the library: the
            // table of contents, which names every topic and so matches every query, and the one about
            // this MCP server, whose list of tools mentions search, setup, themes and the API in a
            // paragraph that answers none of them. Both stay readable through GetBaletteGuideSection;
            // they are only kept out of the ranking.
            if (_metaSections.Contains(section.Key)) continue;

            var body = BaletteSourceCatalog.GetGuideSection(section.Key) ?? string.Empty;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBaletteGuideSection(heading: \"{section.Key}\")", body, string.Empty));
        }

        foreach (var type in BaletteApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, null,
                $"GetBaletteApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BaletteApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: the overloads of Handler are several members
            // and one thing to know about, and all are read by the same call anyway. Their
            // documentation is searched as one body, so a term in any overload finds the member.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();
                var body = string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim();

                // What every object has says nothing about the type it is on, unless the type documents
                // what its own does - and split into words it answers questions it has nothing to do
                // with: to the search, GetHashCode is "code", and the top hit for anything done from code.
                if (body.Length == 0 && _objectMembers.Contains(member.Name)) continue;

                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBaletteApi(typeName: \"{type.Name}\")",
                    body,
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim(),
                    Name: member.Name));
            }
        }

        foreach (var mode in BaletteSetupGuide.RenderModes)
        {
            entries.Add(new Entry("Setup guide", $"Setup: {mode}", null,
                $"GetBaletteSetupGuide(renderMode: \"{mode}\")", BaletteSetupGuide.Summary(mode),
                $"setup install wiring register services stylesheet render mode {mode.Replace('-', ' ')}"));
        }

        entries.Add(new Entry("Search lab", "Inspect a search", null,
            "InspectBaletteSearch(query: \"rc\")",
            "Runs the library's own matcher and ranker over a query and a list of commands - this demo's registry by " +
            "default - and reports, per command, whether it matched and why not, its score, its rank and its row, and " +
            "the characters it highlights.",
            "inspect debug explain why score rank ranking order highlight match fuzzy operators negate exact prefix suffix frecency priority"));

        // The table as a whole, for the question that asks for the list itself ("theme variables",
        // "dark mode colours") rather than for one of its rows.
        entries.Add(new Entry("Theme variable", "Theme variable catalog", null,
            "GetBaletteThemeVariables()", BaletteThemeCatalog.Render(), "css theme variables colors colours dark light mode restyle unstyled"));

        foreach (var variable in BaletteThemeCatalog.Variables)
        {
            // A row is one property and its values. The rows share one parameterless call, so
            // however many of them match, a search spends one hit on them.
            var dark = variable.DarkValue is null ? null : $" (dark: {variable.DarkValue})";

            entries.Add(new Entry("Theme variable", variable.Name, variable.Group,
                "GetBaletteThemeVariables()", $"{variable.Name}: {variable.LightValue}{dark}", $"css {variable.Group} {SpelledOut(variable.Name)}"));
        }

        foreach (var hook in BaletteThemeCatalog.Hooks)
        {
            entries.Add(new Entry("Markup hook", $"data-balette=\"{hook.Value}\"", hook.Component,
                "GetBaletteThemeVariables()",
                $"The <{hook.Element}> {hook.Component} renders{(hook.Class is null ? null : $" with class {hook.Class}")}{(hook.States.Length == 0 ? null : $", carrying {string.Join(", ", hook.States)}")}.",
                "unstyled data attribute hook test selector"));
        }

        foreach (var rule in BaletteStylesheetClasses.Classes)
        {
            // The classes share one file, so however many of them match, a search spends one hit on them.
            entries.Add(new Entry("CSS class", rule.Selector, "bit-balette.css",
                $"GetBaletteSourceFile(path: \"{BaletteSourceCatalog.StylesheetPath}\")", rule.Description, "css class style stylesheet"));
        }

        foreach (var file in BaletteSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBaletteSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }

    /// <summary>
    /// The query without the words that are true of everything here - unless that is all it said,
    /// in which case they are all it can be searched by.
    /// </summary>
    private static string[] DropAmbientWords(string[] terms)
    {
        var meaningful = terms.Where(term => _ambientWords.Contains(term) is false).ToArray();

        return meaningful.Length > 0 ? meaningful : terms;
    }
}
