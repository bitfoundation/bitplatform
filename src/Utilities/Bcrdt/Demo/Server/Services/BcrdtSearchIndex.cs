using Bit.Bcrdt.Demo.Server.Dtos;

namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, every public
/// type and member of its ten packages, the setup guides, the two labs and the demo's source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "keep both edits to a description field" is a guide section, a map method, a text type and a
/// demo page all at once. Each hit therefore carries the exact follow-up tool call that returns
/// the full text, so one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static partial class BcrdtSearchIndex
{
    /// <param name="NamesFromBody">
    /// Whether <paramref name="Boosted"/> holds names taken out of <paramref name="Body"/> - a guide section's
    /// lead - rather than a signature of the entry's own. A term is then a name of the entry once, however many
    /// of those names share it: a section that opens on BcrdtText, BcrdtTextArea and BcrdtTextSynchronizer is
    /// not three times as much about "text". And the mentions those names are in the body are not counted a
    /// second time as prose.
    /// </param>
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight, bool NamesFromBody = false)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "PresenceTimeout" is found by
        /// "presence timeout" - and so a query word only counts as a title hit when it IS one of
        /// those words, rather than merely appearing inside one ("set" inside "Offset"). Two neighbouring
        /// humps of one name are a word as well, since that is how they are often written: "textarea" is
        /// the TextArea of BcrdtTextArea, not a word inside it.
        /// </summary>
        public string[] TitleWords { get; } = [.. SplitWords(Title), .. Compounds(Title)];
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

    private static readonly Lazy<Entry[]> _entries = new(Build);

    /// <summary>
    /// The words every entry in this index carries because of what the index is about. They
    /// separate nothing, and they do worse than nothing: a term that matches an entry counts
    /// towards the "how many terms matched" multiplier, so left in, the longest documents win every
    /// query on the strength of a word that is in all of them.
    /// <para>
    /// Unlike a stop word they are dropped only when the query says something else as well, since
    /// "bcrdt" and "blazor crdt library" are what a caller reaches for first and are made of almost
    /// nothing but these.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> _ambientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bcrdt", "bit", "blazor", "library", "crdt", "crdts"
    };

    private static readonly HashSet<string> _metaSections = new(StringComparer.OrdinalIgnoreCase) { "Contents", "MCP server" };

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto"
    };

    /// <summary>
    /// The words a question is phrased with that are also the first word of a name: "can" is filler in
    /// "a field two people can edit", where counted in prose it matches every section that says what the
    /// library can do - and it is the only word that tells CanUndo, CanRedo and CanMergeWith from Undo, Redo
    /// and MergeWith. So such a word counts where it is a word of an entry's name, and nowhere else.
    /// </summary>
    private static readonly HashSet<string> _nameOnlyWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "can", "will", "may", "might", "must"
    };

    public static BcrdtSearchResultDto Search(string? query, int limit)
    {
        var terms = Tokenize(query);

        if (terms.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than
            // returned as an empty list the agent would read as "Bcrdt cannot do this".
            return new BcrdtSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held no word longer than two letters that was not a filler word, so there was " +
                          "nothing to rank by. Search for the thing itself - 'presence', 'offline', 'undo', 'map conflict' - " +
                          "rather than for a sentence."
            };
        }

        var ranked = _entries.Value
            .Select(entry => Score(entry, terms))
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

        // A snippet starts at a word the entry was ranked by in its text, which a name-only word never is.
        string[] proseTerms = [.. terms.Where(term => _nameOnlyWords.Contains(term) is false)];

        var hits = Cover(ranked, terms, take).Select(hit => new BcrdtSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = Snippet(hit.Entry.Body, proseTerms)
        }).ToArray();

        if (hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that
            // query one message ago, and what it was tokenized into changes nothing about a hit it
            // can read for itself. Both are worth saying only when there is nothing else to say.
            return new BcrdtSearchResultDto
            {
                Hits = hits,
                HasMore = ranked.Length > hits.Length
            };
        }

        // A search that found nothing is where an agent is most likely to conclude the library has
        // no such feature and hand-roll one. Whatever came closest is named instead, so the next
        // call is a better query rather than a worse decision.
        var nearby = Suggest(terms);

        return new BcrdtSearchResultDto
        {
            Terms = terms,
            Hits = [],
            Message = nearby.Length > 0
                ? $"Nothing matched {string.Join(" + ", terms)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", terms)}. Try one word rather than several, or call " +
                   "GetBcrdtGuideSection or GetBcrdtApi with no argument at all for the index of what " +
                   "there is. A concept Bcrdt does not name is often the same idea under another word - a " +
                   "'Y.Doc' is a BcrdtDocument, 'Y.Text', 'Y.Map' and 'Y.Array' are BcrdtText, BcrdtMap and BcrdtArray, " +
                   "'cursors' or 'who is online' are presence (awareness), a 'conflict' is what a merge decides, " +
                   "'history' or 'versions' are snapshots, a 'provider' or 'connection' is a transport, a 'server' " +
                   "is a relay, and 'local storage' or 'IndexedDB' is a document store.",
            DidYouMean = nearby.Length > 0 ? nearby : null
        };
    }

    /// <summary>
    /// The first <paramref name="take"/> hits, with every query word that names something kept in
    /// view. A request usually joins two things - "presence while offline" is presence AND reconnection -
    /// and the one with more material behind it would otherwise fill every place, leaving the
    /// caller to conclude the other has no answer. So for each word that is the name of some entry yet
    /// is said by none of the top hits - neither in a name nor in a text - the best entry it does name takes
    /// the place of the lowest-ranked hit that is not itself there for that reason. A word one of the top
    /// hits already explains is in view, and swapping any hit for an entry that merely has the word in its
    /// name loses an answer to gain nothing: "url" would trade a section on the hub's absolute URL for an
    /// avatar's URL. A name-only word ("can") is never a topic of its own to keep in view. The order stays
    /// the ranking's.
    /// </summary>
    private static Hit[] Cover(Hit[] ranked, string[] terms, int take)
    {
        var top = ranked.Take(take).ToList();
        if (top.Count < take || terms.Length < 2) return [.. top];

        var kept = new HashSet<Entry>();

        for (var i = 0; i < terms.Length; i++)
        {
            var term = terms[i];
            var bit = 1 << i;

            if (_nameOnlyWords.Contains(term) || top.Any(hit => (hit.Matched & bit) != 0)) continue;

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

    /// <summary>
    /// An entry with its score, and which of the query's terms it matched - bit <c>i</c> for <c>terms[i]</c>,
    /// in its name or in its text - so whether a term is in view among the top hits is known without counting it again.
    /// </summary>
    private readonly record struct Hit(Entry Entry, int Score, int Matched);

    private static Hit Score(Entry entry, string[] terms)
    {
        var score = 0;
        var matched = 0;
        var named = 0;
        var mask = 0;
        var topical = false;

        for (var i = 0; i < terms.Length; i++)
        {
            var term = terms[i];
            var isTitleWord = entry.TitleWords.Any(word => Equivalent(word, term));
            var nameOnly = _nameOnlyWords.Contains(term);

            if (nameOnly && isTitleWord is false) continue;

            // Text is counted by the singular, which a plural query word would otherwise never find
            // inside: "transports" has to count the "Transport" of IBcrdtTransport, and "replicas" every "replica".
            var stem = Stem(term);
            var inTitle = isTitleWord || nameOnly ? 0 : Count(entry.Title, stem);
            var inBoosted = nameOnly ? 0 : Count(entry.Boosted, stem);
            var inBody = nameOnly ? 0 : Count(entry.Body, stem);

            if (entry.NamesFromBody)
            {
                inBody = Math.Max(0, inBody - inBoosted);
                inBoosted = Math.Min(inBoosted, 1);
            }

            if (isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            topical |= nameOnly is false;
            mask |= 1 << i;
            matched++;
            if (isTitleWord || inTitle + inBoosted > 0) named++;

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "PresenceTimeout" wants the option, not the paragraphs that happen to mention it.
            score += (isTitleWord ? 12 : 0) + inTitle * 3 + inBoosted * 5 + Math.Min(inBody, MaxBodyScore);
        }

        // Every term matching is the strongest signal a hit is the right one - and a term the entry is
        // named by counts twice, since every long section mentions "command", "open" and "query"
        // somewhere, and a multiplier fed only by prose hands the longest of them every query phrased in
        // plain words. The weight is what keeps a worked example from outranking the reference that
        // answers the question.
        // A name-only word is not a topic: "can redo" is about redoing, and CanMergeWith is not an answer
        // to it for sharing the "Can". Only a query of nothing else is searched by it alone.
        if (topical is false && terms.Any(term => _nameOnlyWords.Contains(term) is false)) return new Hit(entry, 0, 0);

        return new Hit(entry, matched == 0 ? 0 : score * (matched + named) * entry.Weight, mask);
    }

    /// <summary>
    /// Same word, whatever form it is in - "persist" has to find the section called "Persistence",
    /// "transport" the one about transports, and nobody phrases a question in the number or tense the
    /// heading happens to use.
    /// </summary>
    private static bool Equivalent(string word, string term)
    {
        return string.Equals(word, term, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Stem(word), Stem(term), StringComparison.OrdinalIgnoreCase);
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
    /// How often a term starts a word of the text. Only the start of a word counts - the start of the
    /// text, a letter after a non-letter, or a capital after a lower-case letter, so "transport" counts
    /// in "IBcrdtTransport" - because inside a word a term is a different word: every "offset" contains
    /// "set", and counting it there turns a question about BcrdtSet into one about every index.
    /// The end of the word stays open, so a stem still finds its longer forms ("persist" in "persistence").
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

        // The same word starts the snippet that was counted for the score, not "set" inside "offset".
        foreach (var term in terms)
        {
            var at = body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            while (at >= 0 && StartsWord(body, at) is false) at = body.IndexOf(term, at + term.Length, StringComparison.OrdinalIgnoreCase);

            if (at >= 0)
            {
                index = at;
                break;
            }
        }

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

            // A capital after a lowercase letter starts a new word ("PresenceTimeout" -> presence
            // timeout), while a run of capitals stays together ("CRDT", "MCP").
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

    /// <summary>Every two neighbouring camel-case humps of one name, joined: "BcrdtTextArea" gives "BcrdtText" and "TextArea".</summary>
    private static IEnumerable<string> Compounds(string text)
    {
        foreach (var name in text.Split(text.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            var humps = SplitWords(name);

            for (var i = 0; i + 1 < humps.Length; i++) yield return humps[i] + humps[i + 1];
        }
    }

    private static string[] Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        string[] terms = [.. query.Split(['.', ',', ';', ':', '?', '!', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '<', '>', '-', '_', ' ', '\t', '\n', '\r'],
                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // One- and two-letter words ("a", "in", "do") match everything and rank nothing - and
            // the words a question is phrased with do worse than nothing: "how do I open the document
            // FROM code" would otherwise score a section whose heading merely contains "from".
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];

        return DropAmbientWords(terms);
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(512);

        // Every name the API answers to - a type, or a member of one - which is what tells a code span
        // naming the API from one naming a file ("Program.cs"), a setting or a value.
        var apiNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in BcrdtApiCatalog.Types)
        {
            apiNames.Add(BareName(type.Name));

            foreach (var member in BcrdtApiCatalog.GetTypeDetails(type.Name)?.Members ?? [])
            {
                if (member.Kind != "Constructor") apiNames.Add(BareName(member.Name));
            }
        }

        foreach (var (section, ownText) in BcrdtSourceCatalog.GuideSectionsWithOwnText())
        {
            // Two sections of the README are about the README rather than about the library: the
            // table of contents, which names every topic and so matches every query, and the one about
            // this MCP server, whose list of tools mentions search, setup, merges and the API in a
            // paragraph that answers none of them. Both stay readable through GetBcrdtGuideSection;
            // they are only kept out of the ranking.
            if (_metaSections.Contains(section.Key)) continue;

            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBcrdtGuideSection(heading: \"{section.Key}\")", ownText, NamesIn(ownText, apiNames), NamesFromBody: true));
        }

        foreach (var type in BcrdtApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Package,
                $"GetBcrdtApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BcrdtApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: the overloads of Track are several members
            // and one thing to know about, and all are read by the same call anyway. Their
            // documentation is searched as one body, so a term in any overload finds the member.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                // A constructor is named the way it is called - "new BcrdtSyncProvider" - not as a member of itself.
                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", member.Kind == "Constructor" ? member.Name : $"{type.Name}.{member.Name}", type.Name,
                    $"GetBcrdtApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim()));
            }
        }

        foreach (var model in BcrdtSetupGuide.HostingModels)
        {
            entries.Add(new Entry("Setup guide", $"Setup: {model}", null,
                $"GetBcrdtSetupGuide(hostingModel: \"{model}\")", BcrdtSetupGuide.Summary(model),
                $"setup install wiring register services hosting model {model.Replace('-', ' ')}"));
        }

        entries.Add(new Entry("Lab", "Inspect a merge", null,
            "InspectBcrdtMerge()",
            "Runs the real engine on concurrent edits made by two to four replicas - text, map, array, set and counter operations - " +
            "exchanges their updates in every delivery order, and reports each replica's state before the merge, the state " +
            "they converge to, whether they converged, every update's size, and what the merge decided: one value wins for a " +
            "map key, both insertions survive in a text, every change to a counter is summed.",
            "inspect merge conflict concurrent concurrently edits wins winner both survive converge convergence decide " +
            "simultaneous offline replicas last writer counter increment decrement sum"));

        entries.Add(new Entry("Lab", "Inspect an update", null,
            "InspectBcrdtUpdate()",
            "Decodes an update or a sync-protocol message with the library's own decoders and reports its kind, the blocks " +
            "per client with their clock ranges and content, the delete set, the state vector it implies and its sizes - or " +
            "the BcrdtFormatException a malformed payload is rejected with.",
            "inspect decode update message bytes base64 wire format blocks delete set state vector size malformed " +
            "BcrdtFormatException SyncStep1 SyncStep2 handshake"));

        foreach (var file in BcrdtSourceCatalog.SourceFiles)
        {
            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBcrdtSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }

    /// <summary>
    /// The API a guide section's opening paragraph names - <c>`BcrdtText`</c>, <c>`BcrdtMap`</c> - which a
    /// section is named by the way a member is named by its signature. Without them a heading is the only
    /// name a section has, so a question about a "text field" ranked every member with "Text" in its name
    /// above the section on binding text inputs to a <c>BcrdtText</c>.
    /// <para>
    /// Only the opening paragraph, and only its prose, because that is where a section says what it is
    /// about; further down it names whatever it touches. A section that lists the packages, or the changes
    /// since the last release, names half the API in passing - in a table or a list, often from its first
    /// line or right under it - and would then be named by every question. And only a span the API
    /// answers to: <c>`Program.cs`</c> or <c>`appsettings.json`</c> in a lead name a file, not the topic.
    /// </para>
    /// </summary>
    private static string NamesIn(string body, HashSet<string> apiNames)
    {
        var lines = body.Split('\n')
            .Skip(1) // the heading
            .SkipWhile(string.IsNullOrWhiteSpace)
            .ToArray();

        // A section that opens on a table, a list, a quote, a picture or code says nothing about itself first.
        if (lines.Length == 0 || NotProseRegex().IsMatch(lines[0])) return string.Empty;

        var lead = string.Join(' ', lines.TakeWhile(line => string.IsNullOrWhiteSpace(line) is false && NotProseRegex().IsMatch(line) is false));

        return string.Join(' ', CodeSpanRegex().Matches(lead)
            .Select(match => match.Groups["name"].Value)
            .Where(name => name.Split('.').Any(apiNames.Contains))
            .Distinct(StringComparer.Ordinal));
    }

    /// <summary>A type or member name without its type parameters: "BcrdtListBinding&lt;T&gt;" is "BcrdtListBinding".</summary>
    private static string BareName(string name)
    {
        var generic = name.IndexOf('<');

        return generic < 0 ? name : name[..generic];
    }

    /// <summary>
    /// An inline code span holding a name rather than code: a type or a member, qualified or not,
    /// optionally generic or called - <c>`BcrdtText`</c>, <c>`Sync.LastSyncedAt`</c>, <c>`ConnectAsync()`</c>,
    /// <c>`GetBcrdtCollaborationService(...)`</c>, <c>`BcrdtListBinding&lt;T&gt;`</c>. A span starting lower-case
    /// is code (<c>`null`</c>) unless it is qualified (<c>`document.Text("body")`</c>), and a fenced block never
    /// matches because its fence is three backticks. Whether the name is the API's is for the caller to check.
    /// </summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"(?<!`)`(?<name>(?:[A-Z]\w*|[a-z]\w*(?=\.))(?:\.\w+)*)(?:<[\w, ]+>)?(?:\([^`]*\))?`(?!`)")]
    private static partial System.Text.RegularExpressions.Regex CodeSpanRegex();

    /// <summary>
    /// A Markdown line that is not a line of a paragraph: a table row, a bullet or numbered list item, a
    /// quote, a heading, HTML, an image, a code fence or an indented code block.
    /// </summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"^(?: {4}|\t|\s*(?:[|>#<]|[*+-]\s|\d+[.)]\s|!\[|```|~~~))")]
    private static partial System.Text.RegularExpressions.Regex NotProseRegex();

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
