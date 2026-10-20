using Bit.Borm.Demo.Server.Dtos;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// One searchable index over everything this MCP server knows: the reference guide, every public
/// type and member, the setup guides, the theme's custom properties, the stylesheet's classes, the
/// schema lab and the demo's pages and source files.
/// <para>
/// Without it an agent has to guess which corpus holds the answer and what it is called there -
/// "load a dropdown's options from a service when another field changes" is a guide section, a
/// builder method, a delegate and a demo page all at once. Each hit therefore carries the exact
/// follow-up tool call that returns the full text, so one search is enough to know what to ask for next.
/// </para>
/// </summary>
public static class BormSearchIndex
{
    private sealed record Entry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted, int Weight = ReferenceWeight, bool SharesAnswer = false, string? Name = null)
    {
        /// <summary>
        /// The title split into words, camel-case humps included, so "ClearOnHide" is found by
        /// "clear on hide" - and so a query word only counts as a title hit when it IS one of those
        /// words, rather than merely appearing inside one ("tag" inside "Stage"). Two neighbouring
        /// words also count joined, because a hump is not always where a person breaks the word:
        /// "MultiSelect" has to be found by "multiselect", "IdPrefix" by "idprefix". The words every
        /// title carries ("Borm" in each type, "bit" and "borm" in each theme property) are left out -
        /// a title word that every title has picks out nothing. <see cref="Name"/>, when given, is what
        /// the words are read from instead of the title.
        /// </summary>
        public string[] TitleWords { get; } = TitleWordsOf(Name ?? Title);
    }

    private const int MaxTerms = 16;

    /// <summary>The most hits one search may return, whatever the caller asked for.</summary>
    private const int MaxHits = 50;

    private const int MaxSuggestions = 8;

    /// <summary>The weight of an entry that answers a question: a type, a member, a theme property, a setup guide.</summary>
    private const int ReferenceWeight = 10;

    /// <summary>
    /// The weight of a guide section: a little above a member, because between two equally good
    /// matches the section is the one written to explain - "cascading options" is answered by the
    /// section on them, not by OptionsDependencies, which is where the builder records them.
    /// </summary>
    private const int GuideWeight = 12;

    /// <summary>
    /// The weight of a demo page or a source file. They are worked examples, not answers, and their
    /// paths are made of segments ("Demo", "Client", "Pages") that carry no topic - without this,
    /// every question phrased with one of them is answered with a directory listing.
    /// </summary>
    private const int ExampleWeight = 4;

    /// <summary>
    /// The most a term can earn from the words an entry is boosted by. A signature is boosted so a
    /// member is found by its types, but a signature also repeats them - the condition overloads name
    /// BormConditionOperator in every one - and counted in full it outranks the guide section that
    /// answers the question.
    /// </summary>
    private const int MaxBoostedCount = 2;

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
    /// query on the strength of a word that is in all of them. "Form" is one of them - every page is
    /// about one - while "field", which names what the question is about, is not.
    /// <para>
    /// Unlike a stop word they are dropped only when the query says something else as well, since
    /// "borm" and "blazor form library" are what a caller reaches for first and are made of nothing
    /// but these.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> _ambientWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "borm", "bit", "blazor", "library", "form"
    };

    /// <summary>
    /// The sections of the README that are about the README, the demo or this server rather than about
    /// the library: the table of contents names every topic and so matches every query, the demo's
    /// tour lists every page by route (and the pages are entries of their own), and the list of this
    /// server's tools mentions schemas, setup and themes in a paragraph that answers none of them. All
    /// stay readable through GetBormGuideSection; they are only kept out of the ranking.
    /// </summary>
    private static readonly HashSet<string> _metaSections = new(StringComparer.OrdinalIgnoreCase) { "Documentation", "The demo", "MCP server" };

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "the", "and", "for", "with", "from", "that", "this", "what", "when", "where", "which",
        "does", "did", "are", "was", "you", "your", "than", "then", "its", "but", "any", "some",
        "please", "help", "about", "into", "way", "make", "want", "need", "would", "should", "could",
        "there", "here", "have", "has", "get", "got", "let", "one", "two", "per", "via", "onto",
        "only", "another", "other", "not", "don't", "doesn't", "can", "use", "using"
    };

    /// <summary>
    /// The words people use for what Borm calls something else. A question is phrased in the asker's
    /// vocabulary - a field "shows", a box is "ticked", a list is a "dropdown", changes are "unsaved" -
    /// and the library's answer is written in its own: VisibleWhen, IsTrue, Select, IsFormDirty. Each of
    /// these words is searched by the library's word as well, so the answer is not lost to a synonym.
    /// </summary>
    private static readonly Dictionary<string, string[]> _synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["show"] = ["visible"],
        ["shows"] = ["visible"],
        ["shown"] = ["visible"],
        ["hide"] = ["visible"],
        ["hides"] = ["visible"],
        ["appear"] = ["visible"],
        ["appears"] = ["visible"],
        ["ticked"] = ["istrue"],
        ["checked"] = ["istrue"],
        ["dropdown"] = ["select"],
        ["dropdowns"] = ["select"],
        ["repeater"] = ["array"],
        ["repeating"] = ["array"],
        ["derived"] = ["computed"],
        ["unsaved"] = ["dirty"],
        ["safe"] = ["untrusted"],
        ["safely"] = ["untrusted"],
        ["ids"] = ["idprefix"],
        ["dark"] = ["theme"],
        ["colour"] = ["theme"],
        ["colours"] = ["theme"],
        ["color"] = ["theme"],
        ["colors"] = ["theme"],
        ["translate"] = ["localization"],
        ["multistep"] = ["wizard"],
    };

    public static BormSearchResultDto Search(string? query, int limit)
    {
        var terms = Tokenize(query);

        if (terms.Length == 0)
        {
            // Nothing was left to search by, which is a different failure from "searched and found
            // nothing" - and the fix is on the caller's side, so it has to be said rather than
            // returned as an empty list the agent would read as "Borm cannot do this".
            return new BormSearchResultDto
            {
                Terms = [],
                Hits = [],
                Message = "The query held no word longer than two letters that was not a filler word, so there was " +
                          "nothing to rank by. Search for the thing itself - 'wizard', 'repeater', 'cascading options', " +
                          "'json schema' - rather than for a sentence."
            };
        }

        var searched = Expand(terms);

        var ranked = _entries.Value
            .Select(entry => (Entry: entry, Score: Score(entry, searched)))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Entry.Title, StringComparer.OrdinalIgnoreCase)
            // Two entries that would be read through the same call under the same name are one hit:
            // a method with two overloads is two members and one place to read about them, and the
            // second copy costs a hit out of the caller's budget to say nothing new. A call that takes
            // no argument returns the same text whichever entry led to it, and so does a call the
            // entries share by design - every class of the stylesheet is read by reading the stylesheet.
            .DistinctBy(hit => hit.Entry.SharesAnswer || hit.Entry.Tool.EndsWith("()", StringComparison.Ordinal) ? (string.Empty, hit.Entry.Tool) : (hit.Entry.Title, hit.Entry.Tool))
            .ToArray();

        var take = Math.Clamp(limit, 1, MaxHits);

        var hits = Cover(ranked, terms, take).Select(hit => new BormSearchHitDto
        {
            Kind = hit.Entry.Kind,
            Title = hit.Entry.Title,
            Context = hit.Entry.Context,
            Tool = hit.Entry.Tool,
            Snippet = Snippet(hit.Entry.Body, searched)
        }).ToArray();

        if (hits.Length > 0)
        {
            // No echo of the query and no list of the terms it was reduced to: the caller sent that
            // query one message ago, and what it was tokenized into changes nothing about a hit it
            // can read for itself. Both are worth saying only when there is nothing else to say.
            return new BormSearchResultDto
            {
                Hits = hits,
                HasMore = ranked.Length > hits.Length
            };
        }

        // A search that found nothing is where an agent is most likely to conclude the library has
        // no such feature and hand-roll one. Whatever came closest is named instead, so the next
        // call is a better query rather than a worse decision.
        var nearby = Suggest(terms);

        return new BormSearchResultDto
        {
            Terms = terms,
            Hits = [],
            Message = nearby.Length > 0
                ? $"Nothing matched {string.Join(" + ", terms)}. The closest names are listed in didYouMean - " +
                   "search for one of those, or for a single word out of this query."
                : $"Nothing matched {string.Join(" + ", terms)}. Try one word rather than several, or call " +
                   "GetBormGuideSection or GetBormApi with no argument at all for the index of what " +
                   "there is. A concept Borm does not name is often the same idea under another word - " +
                   "a 'schema' or 'form model' is a BormDefinition, a 'control' or 'input' is a field " +
                   "(BormFieldDefinition), 'show/hide' is VisibleWhen, a 'repeater' or 'list' is an Array field, " +
                   "'dependent dropdowns' are OptionsFrom with dependsOn, a 'derived' value is Computed, a 'multi-step' " +
                   "form is a wizard of Steps, and 'form state' is BormState.",
            DidYouMean = nearby.Length > 0 ? nearby : null
        };
    }

    /// <summary>
    /// The first <paramref name="take"/> hits, with every query word that names something kept in
    /// view. A request usually joins two things - "a repeater with a computed total" is arrays AND
    /// computed values - and the one with more material behind it would otherwise fill every place,
    /// leaving the caller to conclude the other has no answer. So for each word that is the name of
    /// some entry yet names none of the top hits, the best entry it does name takes the place of the
    /// lowest-ranked hit that is not itself there for that reason. The order stays the ranking's.
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
    /// The titles that nearly matched: an entry whose name merely begins with one of the query's
    /// words, which the ranking itself ignores on purpose - a prefix is far too weak a signal to
    /// rank a hit by, and exactly strong enough to suggest a word to search for instead.
    /// </summary>
    private static string[] Suggest(string[] terms)
    {
        return [.. _entries.Value
            .Where(entry => terms.Any(term => term.Length >= 4
                                              && entry.TitleWords.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                                                                              // A one- or two-letter title word ("Id", "Of") is the start of
                                                                              // half the dictionary, so it suggests nothing.
                                                                              || (word.Length >= 4 && term.StartsWith(word, StringComparison.OrdinalIgnoreCase)))))
            .Select(entry => entry.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)];
    }

    /// <summary>
    /// The query's terms, each followed by the library's word for it where people tend to use another.
    /// A synonym is searched exactly like a word of the query, but it never replaces the word itself:
    /// "hidden" is still the word a hidden-field type is named by.
    /// </summary>
    private static string[] Expand(string[] terms)
    {
        return [.. terms.SelectMany(term => _synonyms.TryGetValue(term, out var more) ? [term, .. more] : new[] { term })
                        .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static int Score(Entry entry, string[] terms)
    {
        var score = 0;
        var matched = 0;
        var named = 0;

        foreach (var term in terms)
        {
            var isTitleWord = entry.TitleWords.Any(word => Equivalent(word, term));

            // Text is counted by the singular, which a plural query word would otherwise never find
            // inside: "options" has to count the "Option" of OptionsFrom, and "fields" every "field".
            var stem = Stem(term);
            var inTitle = isTitleWord ? 0 : Count(entry.Title, stem);
            var inBoosted = Math.Min(Count(entry.Boosted, stem), MaxBoostedCount);
            var inBody = Count(entry.Body, stem);

            if (isTitleWord is false && inTitle + inBoosted + inBody == 0) continue;

            matched++;
            if (isTitleWord || inTitle + inBoosted > 0) named++;

            // A term in a name is worth far more than the same term buried in prose: someone asking
            // for "ClearOnHide" wants the method, not the paragraphs that happen to mention it.
            score += (isTitleWord ? 12 : 0) + (inTitle * 3) + (inBoosted * 5) + Math.Min(inBody, MaxBodyScore);
        }

        // Every term matching is the strongest signal a hit is the right one - and a term the entry is
        // named by counts twice, since every long section mentions "field", "value" and "rule"
        // somewhere, and a multiplier fed only by prose hands the longest of them every query phrased
        // in plain words. The weight is what keeps a worked example from outranking the reference that
        // answers the question.
        return matched == 0 ? 0 : score * (matched + named) * entry.Weight;
    }

    /// <summary>
    /// Same word, whatever form it is in - "validate" has to find the section called "Validation",
    /// "cascade" the one about cascading options, and nobody phrases a question in the number or tense
    /// the heading happens to use.
    /// </summary>
    private static bool Equivalent(string word, string term)
    {
        return string.Equals(word, term, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Stem(word), Stem(term), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A word without its plural, its -ing or -ed, and a final silent e: "rules", "ruling" and "ruled"
    /// are all "rul", "cascading" and "cascade" both "cascad". Only ever cut where at least three
    /// letters remain, so "css" or "used" keep the letters that make them a word. Prose is counted by
    /// the stem too, which as a substring also finds every longer form of it.
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
    /// text, a letter after a non-letter, or a capital after a lower-case letter, so "hide" counts in
    /// "ClearOnHide" - because inside a word a term is a different word: every "Borm" type name contains
    /// "orm", and counting it there turns a question about an ORM into a search for the library's name.
    /// The end of the word stays open, so a stem still finds its longer forms ("cascad" in "cascading").
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

        // The same word starts the snippet that was counted for the score, not "orm" inside "Borm".
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

    private static string[] TitleWordsOf(string title)
    {
        var words = SplitWords(title).Where(word => _ambientWords.Contains(word) is false).ToArray();

        return [.. words, .. words.Zip(words.Skip(1), (first, second) => first + second)];
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

            // A capital after a lowercase letter starts a new word ("ClearOnHide" -> clear on hide),
            // while a run of capitals stays together ("JSON", "UI").
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
            // One- and two-letter words ("a", "in", "do") match everything and rank nothing - and
            // the words a question is phrased with do worse than nothing: "how do I import a schema
            // FROM a user" would otherwise score a section whose heading merely contains "from".
            .Where(term => term.Length > 2 && _stopWords.Contains(term) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            // Every term is counted in every entry's body, so the work is terms x corpus. No question
            // is phrased in more words than this, while a pasted file as a query would scan for hours.
            .Take(MaxTerms)];

        return DropAmbientWords(terms);
    }

    private static Entry[] Build()
    {
        var entries = new List<Entry>(2048);

        foreach (var section in BormSourceCatalog.GuideSections)
        {
            if (_metaSections.Contains(section.Key)) continue;

            var body = BormSourceCatalog.GetGuideSection(section.Key) ?? string.Empty;

            // A sub-section is named by its parent as well as its own heading: "Untrusted documents" is the
            // section on importing a user's schema because it sits under "JSON Schema", and "json schema
            // from a user" has to find it there rather than lose it to every member with both words in it.
            // And a section says what it is about in the sentence that opens it - "Options can be loaded on
            // demand and refreshed when another field changes" - which counts like the words of a name.
            entries.Add(new Entry("Guide section", section.Heading, section.Parent,
                $"GetBormGuideSection(heading: \"{section.Key}\")", body, Opening(body), GuideWeight,
                Name: section.Parent is null ? null : $"{section.Parent} {section.Heading}"));
        }

        foreach (var type in BormApiCatalog.Types)
        {
            entries.Add(new Entry($"API {type.Kind.ToLowerInvariant()}", type.Name, type.Package == "Bit.Borm" ? null : type.Package,
                $"GetBormApi(typeName: \"{type.Name}\")", type.Summary ?? string.Empty, string.Empty));

            var details = BormApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            // One entry per member NAME, not per member: the three VisibleWhen overloads are three members
            // and one thing to know about, and all of them are read by the same call anyway. Their
            // documentation is searched as one body, so a term in any overload finds the member.
            foreach (var overloads in details.Members.GroupBy(member => member.Name, StringComparer.Ordinal))
            {
                var member = overloads.First();

                entries.Add(new Entry($"API {member.Kind.ToLowerInvariant()}", $"{type.Name}.{member.Name}", type.Name,
                    $"GetBormApi(typeName: \"{type.Name}\")",
                    string.Join(' ', overloads.Select(o => $"{o.Summary} {o.Remarks}".Trim())).Trim(),
                    string.Join(' ', overloads.Select(o => $"{o.Type} {o.Signature}".Trim())).Trim()));
            }
        }

        foreach (var model in BormSetupGuide.HostingModels)
        {
            entries.Add(new Entry("Setup guide", $"Setup: {model}", null,
                $"GetBormSetupGuide(hostingModel: \"{model}\")", BormSetupGuide.Summary(model),
                $"setup install wiring AddBorm stylesheet hosting model {model.Replace('-', ' ')}", SharesAnswer: true));
        }

        // The checklist every setup guide opens with is where the failures are explained - a missing
        // AddBorm, an unstyled form, a page that never becomes interactive - so it is searchable on its
        // own. It is read through the guide of the demo's own hosting model, and is the same hit as that
        // guide rather than a second one.
        entries.Add(new Entry("Setup guide", "Setup checklist", null,
            "GetBormSetupGuide(hostingModel: \"web-app\")", BormSetupGuide.Checklist,
            "setup checklist AddBorm stylesheet link registered service interactive unstyled", SharesAnswer: true));

        // The table as a whole, for the question that asks for the theme itself ("dark mode colours")
        // rather than for one of its rows.
        entries.Add(new Entry("Theme", "Theme property catalog", null,
            "GetBormTheme()",
            "Every --bit-borm-* custom property of the shipped stylesheet with its light and dark value and what it is used for; the dark " +
            "theme declares the same properties again under prefers-color-scheme: dark and data-theme=\"dark\", so it is restyled there.",
            "css theme colors colours palette dark mode light mode restyle variables custom properties"));

        foreach (var property in BormThemeCatalog.Properties)
        {
            var dark = property.DarkValue is null ? null : $" (dark: {property.DarkValue})";
            var paints = string.Join(", ", property.PaintedBy.Select(rule => rule[..rule.IndexOf(" {", StringComparison.Ordinal)]).Distinct(StringComparer.Ordinal));

            // What it paints stays in the prose: the selectors are full of "field" and "input", and as
            // boosted words they would make a colour the answer to every question about a field.
            entries.Add(new Entry("Theme property", property.Name, "Theme",
                $"GetBormTheme(property: \"{property.Name}\")",
                $"{property.Name}: {property.Value}{dark}. {property.Purpose} Paints {(paints.Length == 0 ? "nothing directly" : paints)}. {property.Comment}".Trim(),
                "css theme color colour"));
        }

        entries.Add(new Entry("Schema lab", "Inspect a JSON Schema", null,
            "InspectBormSchema()",
            "Runs a JSON Schema document through BormJsonSchemaImporter.TryImport and BormDefinition.Validate() and reports the form it " +
            "becomes - every field with its path, type, label, rules, conditions, options and wizard step - what the schema checks find, " +
            "what the importer silently drops (unknown x- extensions, unreadable conditions, dangling $refs, patterns .NET cannot compile), " +
            "and the document BormJsonSchemaExporter.Export gives back, imported again to check the round trip; called with no schema it " +
            "returns a sample schema.",
            "inspect validate check debug json schema import export round trip sample diagnostics x-extensions"));

        foreach (var rule in BormStylesheetClasses.Classes)
        {
            // The classes share one file, so however many of them match, a search spends one hit on them.
            entries.Add(new Entry("CSS class", rule.Selector, "bit-borm.css",
                $"GetBormSourceFile(path: \"{BormSourceCatalog.StylesheetPath}\")", rule.Description, "css class style stylesheet", SharesAnswer: true));
        }

        // A demo page is filed under the heading it shows a person - "Per-row conditions", not
        // "PerRowConditionsDemo.razor" - and its route, which is how the guide refers to it.
        foreach (var file in BormSourceCatalog.SourceFiles)
        {
            if (file.Kind == "Page" && file.Title is not null)
            {
                entries.Add(new Entry("Demo page", file.Title, file.Route,
                    $"GetBormSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty,
                    (file.Route ?? string.Empty).Replace('/', ' ').Replace('-', ' '), ExampleWeight));

                continue;
            }

            entries.Add(new Entry("Source file", file.Path, file.Kind,
                $"GetBormSourceFile(path: \"{file.Path}\")", file.Description ?? string.Empty, string.Empty, ExampleWeight));
        }

        return [.. entries];
    }

    /// <summary>
    /// The first paragraph of a guide section under its heading: the topic sentence the section is
    /// written around. A code block, a table or a list opening the section is not that sentence - a
    /// list of highlights names every topic of the guide, which is the opposite of naming one.
    /// </summary>
    private static string Opening(string section)
    {
        var lines = section.Split('\n').Skip(1).SkipWhile(string.IsNullOrWhiteSpace).ToArray();

        if (lines.Length == 0 || lines[0].StartsWith("```", StringComparison.Ordinal) || lines[0].StartsWith('|') || lines[0].StartsWith('#') ||
            lines[0].StartsWith("- ", StringComparison.Ordinal) || lines[0].StartsWith("* ", StringComparison.Ordinal)) return string.Empty;

        return string.Join(' ', lines.TakeWhile(line => string.IsNullOrWhiteSpace(line) is false && line.StartsWith("```", StringComparison.Ordinal) is false));
    }

    /// <summary>
    /// The query without the words that are true of everything here - unless that is all it said,
    /// in which case they are all it can be searched by.
    /// </summary>
    private static string[] DropAmbientWords(string[] terms)
    {
        // By the stem, so "forms" is as ambient as "form".
        var meaningful = terms.Where(term => _ambientWords.Contains(term) is false && _ambientWords.Contains(Stem(term)) is false).ToArray();

        return meaningful.Length > 0 ? meaningful : terms;
    }
}
