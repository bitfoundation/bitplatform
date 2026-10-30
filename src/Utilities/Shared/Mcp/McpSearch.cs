using System.Text;

namespace Bit.Utilities.Mcp;

/// <summary>
/// The ranking behind a utility's Search tool: one search over everything its MCP server knows - the reference
/// guide, every public type and member, the demo's source files and whatever else the server indexes.
/// <para>
/// A server builds the <see cref="McpSearchEntry"/> list and words the answer; how a query becomes terms, how
/// an entry is scored against them and which hits are kept in view is all here, linked into every server that
/// uses it, so a fix to how a question is read is made once.
/// </para>
/// </summary>
internal sealed class McpSearch
{
    /// <summary>The weight of an entry that answers a question: a guide section, a type, a member, a diagnostic.</summary>
    public const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a source file. They are examples, not answers, and their titles are paths whose segments
    /// ("Client", "Pages", "Server") are common words carrying no topic - without this, every question phrased
    /// with one of them is answered with a directory listing.
    /// </summary>
    public const int ExampleWeight = 4;

    /// <summary>
    /// The most terms a query is searched by. Every term is counted in every entry's body, so the work is terms
    /// x corpus: no question is phrased in more words than this, while a pasted file as a query would scan for hours.
    /// </summary>
    private const int MaxTerms = 16;

    /// <summary>The most hits one search may return, whatever the caller asked for.</summary>
    private const int MaxHits = 50;

    private const int MaxSuggestions = 8;

    /// <summary>
    /// The most an entry can earn from prose, however much of it there is. A long section mentions everything,
    /// so without a ceiling the biggest document in the corpus is the top hit for every query - and a name
    /// match, which is the far stronger signal, never gets ahead of it.
    /// </summary>
    private const int MaxBodyScore = 8;

    /// <summary>
    /// The most places a term can be found in and still single out the one a question is about. Past it the
    /// term is common to a whole subsystem - "command" is in every member of a builder and the prose of most
    /// sections - and the more places it is in, the less it says about which of them is the answer. So its
    /// weight falls (<see cref="WeightOf"/>), and the rarer words of a question decide what it is about: "a
    /// counter whose value survives a page reload" is about the reload.
    /// </summary>
    private const int DistinctiveSpread = 12;

    /// <summary>Where a query is split into words. Not at an apostrophe: <see cref="Uncontracted"/> reads those.</summary>
    private static readonly char[] _separators =
        ['.', ',', ';', ':', '?', '!', '"', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'];

    /// <summary>
    /// The words a question is phrased with. They do worse than match everything: "how do I open the palette
    /// FROM code" would otherwise score a section whose heading merely contains "from", and "why does my effect
    /// not cancel" the one called "Why".
    /// </summary>
    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which", "why", "who", "whom",
        "does", "did", "are", "was", "were", "had", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        // A pronoun names nothing - and "them" is the stem of "theme".
        "them", "they", "their", "these", "those", "whose",
        // The short ones that are also the hump of some member's name ("On" of OnClose, "Is" of IsOpen), which
        // would otherwise pass for a short name.
        "a", "an", "as", "at", "be", "by", "do", "go", "i", "if", "in", "is", "it", "me", "my", "no",
        "of", "on", "or", "so", "to", "up", "us", "we"
    };

    private readonly Lazy<Indexed[]> _entries;

    /// <summary>
    /// The one- and two-letter words that name something here: the "bg" of a colour variable, the "z" of a
    /// stacking one. A query word that short is dropped unless it is one of these (<see cref="IsSearchable"/>),
    /// because the rest ("a", "in", "do") match everything.
    /// </summary>
    private readonly Lazy<HashSet<string>> _shortNames;

    /// <summary>
    /// The words every entry carries because of what the index is about. They separate nothing, and they do
    /// worse than nothing: a term that matches an entry counts towards the "how many terms matched" multiplier,
    /// so left in, the longest documents win every query on the strength of a word that is in all of them.
    /// Unlike a stop word they are dropped only when the query says something else as well, since the library's
    /// name is what a caller reaches for first.
    /// </summary>
    private readonly HashSet<string> _ambientWords;

    /// <summary>
    /// The words a caller uses for something the library calls otherwise, by the stem of each form of the
    /// word, and the word the library uses. A query word finds what the library's word finds as well as what it
    /// finds itself: "hotkey" finds every entry about shortcuts.
    /// </summary>
    private readonly Dictionary<string, string> _synonyms = new(StringComparer.Ordinal);

    /// <param name="build">Every entry of the index. Called once, on the first search.</param>
    /// <param name="ambientWords">The words true of everything in the index: the library's name, and what it is.</param>
    /// <param name="synonyms">
    /// Words a caller uses, and the word the library uses for the same thing. Only a word that means nothing else
    /// in the library belongs here. Its plural reaches the synonym too.
    /// </param>
    public McpSearch(Func<IEnumerable<McpSearchEntry>> build, IEnumerable<string> ambientWords, IEnumerable<(string Word, string Means)>? synonyms = null)
    {
        _entries = new(() => [.. build().Select(entry => new Indexed(entry))]);

        // A number that short ("3" of "3. Start it") names nothing: it only matches the number of a numbered heading.
        _shortNames = new(() => new(_entries.Value.SelectMany(entry => entry.TitleWords).Where(word => word.Length <= 2 && word.Any(char.IsLetter)),
                                    StringComparer.OrdinalIgnoreCase));

        _ambientWords = new(ambientWords, StringComparer.OrdinalIgnoreCase);

        // Keyed by the stem of the plural too: a word and its plural do not always share a stem ("alias" and
        // "aliases"), and a caller asks in either.
        foreach (var (word, means) in synonyms ?? [])
        {
            _synonyms[StemOf(word)] = means;
            _synonyms[StemOf(PluralOf(word))] = means;
        }
    }

    /// <summary>
    /// The hits for <paramref name="query"/>, best first, or - when there are none - the titles that nearly
    /// matched. A query left with no word to search by has no <see cref="McpSearchResult.Terms"/>.
    /// </summary>
    public McpSearchResult Search(string? query, int limit)
    {
        var words = Tokenize(query);

        if (words.Length == 0) return new McpSearchResult([], [], false, []);

        Term[] terms = [.. words.Select(ToTerm)];

        // Every term against every entry, once: how many places a term is found in is its weight, which every
        // entry's score needs before any of them can be worked out. Places, not entries: the rows read through
        // one call are one hit however many of them match, so they are one place too.
        var matches = _entries.Value.Select(entry => (Entry: entry, Matches: terms.Select(term => MatchOf(entry, term)).ToArray())).ToArray();
        double[] weights = [.. terms.Select((_, i) => WeightOf(matches.Where(candidate => candidate.Matches[i].Found).Select(candidate => ReadKey(candidate.Entry)).Distinct().Count()))];

        // A snippet is cut around the rarest word of the query it holds, which is the one that says why the hit
        // is there: "reload" rather than "value" for "a counter whose value survives a page reload".
        Stem[] stems = [.. terms.Select((term, i) => (term.Stems, Weight: weights[i])).OrderByDescending(term => term.Weight).SelectMany(term => term.Stems)];

        var ranked = matches
            .Select(candidate => (candidate.Entry, Score: Score(candidate.Entry, candidate.Matches, weights)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // Two entries read through the same call are one hit: a method with two overloads is two members and
            // one place to read about them, and the second copy costs a hit out of the caller's budget to say
            // nothing new.
            .DistinctBy(hit => ReadKey(hit.Entry))
            .ToArray();

        McpSearchHit[] hits = [.. Cover(ranked, terms, Math.Clamp(limit, 1, MaxHits)).Select(hit => new McpSearchHit(hit.Entry.Entry, Snippet(hit.Entry.Entry.Body, stems)))];

        // A search that found nothing is where an agent is most likely to conclude the library has no such
        // feature and hand-roll one. Whatever came closest is named instead, so the next call is a better query
        // rather than a worse decision.
        return new McpSearchResult(words, hits, ranked.Length > hits.Length, hits.Length > 0 ? [] : Suggest(words));
    }

    /// <summary>
    /// What an entry is read through: its call, and its title unless the call takes no argument and so answers
    /// the same text whichever entry led to it.
    /// </summary>
    private static (string Title, string Tool) ReadKey(Indexed entry)
    {
        var tool = entry.Entry.Tool;

        return tool.EndsWith("()", StringComparison.Ordinal) ? (string.Empty, tool) : (entry.Entry.Title, tool);
    }

    /// <summary>A word of the query as it is matched: by its stem, and by its synonym's if it has one.</summary>
    private Term ToTerm(string word)
    {
        var stem = ToStem(word);

        return new Term(_synonyms.TryGetValue(stem.Text, out var means) ? [stem, ToStem(means)] : [stem]);
    }

    /// <summary>
    /// The weight of a term found in <paramref name="spread"/> places: whole up to <see cref="DistinctiveSpread"/>,
    /// and falling with the square root of the spread past it. In proportion it would fall so far that a common
    /// word could no longer settle anything between the entries a rare one found.
    /// </summary>
    private static double WeightOf(int spread)
    {
        return spread > DistinctiveSpread ? Math.Sqrt((double)DistinctiveSpread / spread) : 1;
    }

    /// <summary>
    /// The first <paramref name="take"/> hits, with every query word that names something kept in view. A request
    /// usually joins two things - "persist the undo history" is persistence AND undo - and the one with more
    /// material behind it would otherwise fill every place, leaving the caller to conclude the other has no
    /// answer. So for each word that is the name of some entry yet names none of the top hits, the best entry it
    /// does name takes the place of the lowest-ranked hit that is not itself there for that reason - nor the only
    /// hit in view that another word names, which would only hand that word's place to this one. The order stays
    /// the ranking's.
    /// </summary>
    private static (Indexed Entry, double Score)[] Cover((Indexed Entry, double Score)[] ranked, Term[] terms, int take)
    {
        var top = ranked.Take(take).ToList();
        if (top.Count < take || terms.Length < 2) return [.. top];

        var kept = new HashSet<Indexed>();

        foreach (var term in terms)
        {
            if (top.Any(hit => hit.Entry.IsNamedBy(term))) continue;

            var best = ranked.Skip(take).FirstOrDefault(hit => hit.Entry.IsNamedBy(term));
            if (best.Entry is null) continue;

            // The first hit is never given up: it is the one a caller reads, and the ranking put it there.
            var replace = top.FindLastIndex(top.Count - 1, top.Count - 1, hit => kept.Contains(hit.Entry) is false
                                                   && terms.Any(other => hit.Entry.IsNamedBy(other) && top.Count(next => next.Entry.IsNamedBy(other)) == 1) is false);
            if (replace < 0) continue;

            top[replace] = best;
            kept.Add(best.Entry);
        }

        return [.. top.OrderByDescending(hit => hit.Score).ThenBy(hit => hit.Entry.Entry.Title, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// The titles that nearly matched: an entry whose name merely begins with one of the query's words, which the
    /// ranking itself ignores on purpose - a prefix is far too weak a signal to rank a hit by, and exactly strong
    /// enough to suggest a word to search for instead.
    /// </summary>
    private string[] Suggest(string[] words)
    {
        return [.. _entries.Value
            .Where(entry => words.Any(word => word.Length >= 4
                                              && entry.TitleWords.Any(title => title.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                                                               // A short title word ("UI") is the start of half the
                                                                               // dictionary, so it suggests nothing.
                                                                               || (title.Length >= 4 && word.StartsWith(title, StringComparison.OrdinalIgnoreCase)))))
            .Select(entry => entry.Entry.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)];
    }

    private static Match MatchOf(Indexed entry, Term term)
    {
        var isTitleWord = entry.IsNamedBy(term);

        // Text is counted by the stem, which a plural or a tense would otherwise never find inside: "effects" has
        // to count the "Effect" of BlexEffect, and "waits" every "wait" and "waiting".
        return new Match(isTitleWord,
            isTitleWord ? 0 : Count(entry.Entry.Title, term.Stems),
            // A list of names, where a word said twice - "load loads loaded", or a type in every overload's
            // signature - is not twice as much the entry's name.
            Math.Min(Count(entry.Entry.Boosted, term.Stems), 1),
            Count(entry.Entry.Body, term.Stems));
    }

    private static double Score(Indexed entry, Match[] matches, double[] weights)
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

        // Every term matching is the strongest signal a hit is the right one - and a term the entry is named by
        // counts twice, since every long section mentions the library's everyday words somewhere, and a multiplier
        // fed only by prose hands the longest of them every query phrased in plain words. The weight is what keeps
        // a worked example from outranking the reference that answers the question.
        return matched == 0 ? 0 : score * (matched + named) * entry.Entry.Weight;
    }

    /// <summary>The stem a word is matched by, and whether a longer word that merely starts with it is found by it too.</summary>
    private static Stem ToStem(string word)
    {
        var stem = StemOf(word);

        // A stem of one or two letters is the start of hundreds of other words ("k" begins "key" and "keyword"),
        // and one cut down to four letters or fewer may have stopped short of a word of its own: "stat" of "state"
        // starts "status" and "static". Either finds only the words with that same stem - "states", "stated".
        return new Stem(stem, stem.Length > 2 && (stem.Length > 4 || stem.Length == word.Length));
    }

    /// <summary>
    /// A word without its plural, its -ing or -ed, and a final silent e: "effects" is "effect", "queries"
    /// "query", "indexes" "index", "persisting" and "persisted" both "persist", "saving" and "save" both "sav".
    /// Only ever cut where at least three letters remain, so "keys" is "key" while "css" stays "css" - and only
    /// where the ending inflects the word: the "s" of "status" and "axis", the "ed" of "speed" and the "ing" of
    /// "string", which would leave no vowel, are the word's own.
    /// </summary>
    private static string StemOf(string word)
    {
        var stem = word.ToLowerInvariant();

        if (stem.Length > 4 && stem.EndsWith("ies", StringComparison.Ordinal)) stem = $"{stem[..^3]}y";
        else if (stem.Length > 4 && (stem.EndsWith("sses", StringComparison.Ordinal) || stem.EndsWith("xes", StringComparison.Ordinal)
                                     || stem.EndsWith("ches", StringComparison.Ordinal) || stem.EndsWith("shes", StringComparison.Ordinal))) stem = stem[..^2];
        else if (stem.Length > 3 && stem.EndsWith('s') && stem.EndsWith("ss", StringComparison.Ordinal) is false
                 && stem.EndsWith("us", StringComparison.Ordinal) is false && stem.EndsWith("is", StringComparison.Ordinal) is false) stem = stem[..^1];

        if (stem.Length > 5 && stem.EndsWith("ing", StringComparison.Ordinal) && HasVowel(stem[..^3])) stem = Undoubled(stem[..^3]);
        else if (stem.Length > 4 && stem.EndsWith("ed", StringComparison.Ordinal) && stem.EndsWith("eed", StringComparison.Ordinal) is false && HasVowel(stem[..^2])) stem = Undoubled(stem[..^2]);
        else if (stem.Length > 3 && stem.EndsWith('e') && stem.EndsWith("ee", StringComparison.Ordinal) is false) stem = stem[..^1];

        return stem;
    }

    private static bool HasVowel(string text)
    {
        return text.AsSpan().IndexOfAny("aeiouy") >= 0;
    }

    /// <summary>
    /// A stem without the consonant -ing and -ed double: "dragging" is "drag", "padded" "pad". Not an l, s or z,
    /// which "scroll", "press" and "buzz" double on their own.
    /// </summary>
    private static string Undoubled(string stem)
    {
        return stem.Length > 3 && stem[^1] == stem[^2] && "aeiouylsz".Contains(stem[^1]) is false ? stem[..^1] : stem;
    }

    /// <summary>The plural of a word, as <see cref="StemOf"/> takes one apart.</summary>
    private static string PluralOf(string word)
    {
        if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith("ch", StringComparison.Ordinal) || word.EndsWith("sh", StringComparison.Ordinal)) return $"{word}es";

        return word.Length > 1 && word.EndsWith('y') && "aeiou".Contains(word[^2]) is false ? $"{word[..^1]}ies" : $"{word}s";
    }

    private static int Count(string text, Stem[] stems)
    {
        return stems.Sum(stem => Count(text, stem));
    }

    /// <summary>How often a stem is a word of the text, as <see cref="IndexOfWord"/> finds one.</summary>
    private static int Count(string text, Stem stem)
    {
        var count = 0;

        for (var at = IndexOfWord(text, stem, 0); at >= 0; at = IndexOfWord(text, stem, at + stem.Text.Length)) count++;

        return count;
    }

    /// <summary>
    /// Where the stem next starts a word of the text, or -1. Only the start of a word counts - the start of the
    /// text, a letter after a non-letter, or a capital after a lower-case letter, so "shortcut" counts in
    /// "GlobalShortcut" - because inside a word a stem is a different word: "page" is inside every "TPage", and
    /// "let" inside every "Balette". The end of the word stays open for an <see cref="Stem.Open"/> stem, so it
    /// still finds the longer forms of its word ("persist" in "persistence"); any other has to be the stem of the
    /// whole word.
    /// </summary>
    private static int IndexOfWord(string text, Stem stem, int from)
    {
        var at = text.IndexOf(stem.Text, from, StringComparison.OrdinalIgnoreCase);

        while (at >= 0 && IsWordAt(text, at, stem) is false)
        {
            at = text.IndexOf(stem.Text, at + stem.Text.Length, StringComparison.OrdinalIgnoreCase);
        }

        return at;
    }

    private static bool IsWordAt(string text, int at, Stem stem)
    {
        if (StartsWord(text, at) is false) return false;
        if (stem.Open) return true;

        var end = at + stem.Text.Length;
        while (EndsWord(text, end) is false) end++;

        return StemOf(text[at..end]) == stem.Text;
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

    private static string Snippet(string body, Stem[] stems)
    {
        if (body.Length == 0) return string.Empty;

        // The same word starts the snippet that was counted for the score. When the body holds none of them -
        // the hit is there for its name - the snippet starts where the body does: anywhere else would be wherever
        // the letters of a term happen to sit inside some other word.
        var index = stems.Select(stem => IndexOfWord(body, stem, 0)).FirstOrDefault(at => at >= 0, 0);

        // A snippet is a reason to make the follow-up call, not a substitute for making it: enough of the
        // surrounding text to tell the right hit from the wrong one, and no more. Every extra character here is
        // paid for by every hit of every search, most of which are not the one.
        var start = Math.Max(0, index - 60);
        var length = Math.Min(180, body.Length - start);
        var snippet = body.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ').Trim();

        return $"{(start > 0 ? "..." : null)}{snippet}{(start + length < body.Length ? "..." : null)}";
    }

    /// <summary>
    /// Splits a name or heading into words, breaking camel-case humps as well as punctuation. What follows an
    /// apostrophe is a contraction or a possessive, not a word: "the library's" is the library, and an "s" of it
    /// would make the "s" of every "it's" a name to search by.
    /// </summary>
    private static string[] SplitWords(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        var contraction = false;

        foreach (var c in text)
        {
            if ((c is '\'' or '’') && current.Length > 0)
            {
                contraction = true;
                continue;
            }

            if (contraction && char.IsLetter(c)) continue;
            contraction = false;

            if (char.IsLetterOrDigit(c) is false)
            {
                if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                continue;
            }

            // A capital after a lowercase letter starts a new word ("GroupWhenSearching" -> group when
            // searching), while a run of capitals stays together ("CSS", "OPFS").
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

    /// <summary>
    /// A name without its type parameters: <c>Subscribe&lt;T&gt;</c> is called Subscribe. "T" and "TPage" name
    /// no topic, and as a one-letter word of a name "T" would make the "t" of every "don't" a name to search by.
    /// </summary>
    private static string WithoutTypeParameters(string name)
    {
        if (name.Contains('<', StringComparison.Ordinal) is false) return name;

        var builder = new StringBuilder(name.Length);
        var depth = 0;

        foreach (var c in name)
        {
            if (c == '<') depth++;
            else if (c == '>' && depth > 0) depth--;
            else if (depth == 0) builder.Append(c);
        }

        return builder.ToString();
    }

    private string[] Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        string[] terms = [.. query.Replace('’', '\'').Split(_separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uncontracted)
            .Where(IsSearchable)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTerms)];

        var meaningful = terms.Where(term => _ambientWords.Contains(term) is false).ToArray();

        return meaningful.Length > 0 ? meaningful : terms;
    }

    /// <summary>
    /// A word of the query as it is searched by: without the quotes around it, a possessive without its "'s" -
    /// "the tour's target" is the tour - and a negation not at all. "can't" and "doesn't" say nothing about what
    /// is wanted, and split at the apostrophe they would be searched for "t".
    /// </summary>
    private static string Uncontracted(string word)
    {
        var unquoted = word.Trim('\'');

        var apostrophe = unquoted.IndexOf('\'', StringComparison.Ordinal);
        if (apostrophe < 0) return unquoted;

        return unquoted.EndsWith("n't", StringComparison.OrdinalIgnoreCase) ? string.Empty : unquoted[..apostrophe];
    }

    /// <summary>
    /// Whether a word of the query is one to search by. One- and two-letter words ("a", "in", "do") match
    /// everything and rank nothing, unless they are a name here or a key made of a letter and a digit ("F2").
    /// </summary>
    private bool IsSearchable(string word)
    {
        if (word.Length == 0 || _stopWords.Contains(word)) return false;

        return word.Length > 2 || _shortNames.Value.Contains(word) || (word.Any(char.IsDigit) && word.Any(char.IsLetter));
    }

    /// <summary>An entry as it is searched: with its name taken apart into the words a term is matched against.</summary>
    private sealed class Indexed
    {
        public Indexed(McpSearchEntry entry)
        {
            Entry = entry;
            TitleWords = [.. SplitWords(WithoutTypeParameters(entry.Name ?? entry.Title)), .. SplitWords(entry.Aliases ?? string.Empty)];
            TitleStems = [.. TitleWords.Select(StemOf)];
        }

        public McpSearchEntry Entry { get; }

        /// <summary>
        /// The name split into words, camel-case humps included, so "GroupWhenSearching" is found by "group when
        /// searching" - and so a query word only counts as a title hit when it IS one of those words, rather than
        /// merely appearing inside one ("pin" inside "Spinner").
        /// </summary>
        public string[] TitleWords { get; }

        /// <summary>
        /// The stems of <see cref="TitleWords"/>, which is what a term is matched against: "effect" has to find the
        /// page called "Effects", and nobody phrases a question in the number or tense the heading happens to use.
        /// Worked out once here rather than for every term against every entry of every search.
        /// </summary>
        public HashSet<string> TitleStems { get; }

        public bool IsNamedBy(Term term)
        {
            return term.Stems.Any(stem => TitleStems.Contains(stem.Text));
        }
    }

    /// <summary>
    /// What a word is matched by: its stem, and whether a longer word that merely starts with the stem counts
    /// too - <see cref="ToStem"/> says when it does.
    /// </summary>
    private readonly record struct Stem(string Text, bool Open);

    /// <summary>A word of the query, by the stems it is matched by: its own, and its synonym's if it has one.</summary>
    private readonly record struct Term(Stem[] Stems);

    /// <summary>How one term matched one entry.</summary>
    private readonly record struct Match(bool IsTitleWord, int InTitle, int InBoosted, int InBody)
    {
        public bool Found => IsTitleWord || InTitle + InBoosted + InBody > 0;

        public bool Named => IsTitleWord || InTitle + InBoosted > 0;

        // A term in a name is worth far more than the same term buried in prose: someone asking for
        // "GroupWhenSearching" wants the option, not the paragraphs that happen to mention it.
        public int Points => (IsTitleWord ? 12 : 0) + InTitle * 3 + InBoosted * 5 + Math.Min(InBody, MaxBodyScore);
    }
}

/// <summary>What a search found.</summary>
/// <param name="Terms">The words the query was searched by. Empty when it held nothing but filler words.</param>
/// <param name="Hits">The hits, best first.</param>
/// <param name="HasMore">Whether the ranking was cut short at the limit, so a narrower query would show more.</param>
/// <param name="DidYouMean">When nothing matched, the titles that nearly did.</param>
internal sealed record McpSearchResult(string[] Terms, McpSearchHit[] Hits, bool HasMore, string[] DidYouMean);

/// <summary>One hit, and the part of its body that says why it is one.</summary>
internal sealed record McpSearchHit(McpSearchEntry Entry, string Snippet);
