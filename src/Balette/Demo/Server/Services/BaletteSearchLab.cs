using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Globalization;
using Microsoft.JSInterop;
using Bit.Balette.Demo.Server.Dtos;
using Bit.Balette.Demo.Client.Services;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// Runs Bit.Balette's own matcher and ranker over a query and a list of commands, so "why does this
/// command rank there, and why does that one not show up at all?" is answered by the code that decides
/// it at runtime rather than by a reading of the scoring rules.
/// <para>
/// Matching goes through the public <see cref="BaletteMatcherExtensions.Match"/> and ranking through
/// <see cref="BaletteRanker.Rank(IEnumerable{BaletteMatch}, bool, Func{string, double})"/> - the two
/// calls the palette makes on every keystroke - over the candidates the palette would consider: the
/// visible commands without a parent, plus the AlwaysShow ones that did not match, at minus infinity.
/// What the palette does after that is component markup rather than public API - keep the best
/// MaxResults, then lay them out under their group headings - so those two steps are written out here,
/// the same way <c>Balette.Rebuild</c> takes them, and labelled as the palette's layout.
/// </para>
/// <para>
/// Every explanation of a single command is the library's answer too: the term that failed is found by
/// matching each term on its own, and the field that earned a term's score by matching the term against
/// that field alone and comparing the weighted scores - no second copy of the scoring rules is consulted.
/// </para>
/// </summary>
public static class BaletteSearchLab
{
    /// <summary>The most commands one inspection reads. A registry in the thousands is a performance question, not a ranking one.</summary>
    public const int MaxCommands = 500;

    /// <summary>The most commands one answer reports on.</summary>
    public const int MaxLimit = 200;

    /// <summary>The matchers this tool can run, as the demo's own search lab offers them.</summary>
    public static readonly string[] Matchers = ["fuzzy", "fuzzy-ignore-case", "substring"];

    private sealed record Registry(BaletteOptions Options, BaletteCommand[] Commands);

    private static readonly Lazy<Registry> _demo = new(BuildDemoRegistry);

    private static readonly BaletteMatchWeights _titleOnly = new() { Title = 1 };

    private static readonly IReadOnlyDictionary<string, int> _priorityLevels =
        typeof(BaletteCommandPriority).GetFields(BindingFlags.Public | BindingFlags.Static)
                                      .Where(field => field.IsLiteral && field.FieldType == typeof(int))
                                      .ToDictionary(field => field.Name, field => (int)field.GetRawConstantValue()!, StringComparer.OrdinalIgnoreCase);

    /// <summary>The root commands of this demo's app-wide registry, in registry order.</summary>
    public static IReadOnlyList<BaletteCommand> DemoRegistry => _demo.Value.Commands;

    /// <summary>The options this demo registers its palette with - its group order, its modes.</summary>
    public static BaletteOptions DemoOptions => _demo.Value.Options;

    public static BaletteSearchInspectionDto Inspect(string? query, string? commands, string? matcher, bool groupWhenSearching, int limit)
    {
        query ??= string.Empty;

        var notes = new List<string>();

        BaletteOptions options;
        List<BaletteCommand> candidates;
        string source;

        if (string.IsNullOrWhiteSpace(commands))
        {
            options = _demo.Value.Options;
            candidates = [.. _demo.Value.Commands];
            source = $"this demo's registry: the {candidates.Count} root commands Layout/MainLayout.razor registers from Services/DemoCommands.cs - the candidates the demo's own Search and ranking page runs its lab over";

            query = ApplyMode(query, options, candidates, notes);
        }
        else
        {
            options = new BaletteOptions();
            candidates = ParseCommands(commands, notes);
            source = $"the {candidates.Count} commands passed in, ranked with the default BaletteOptions";
        }

        var (matcherName, matcherInstance) = ResolveMatcher(matcher, notes);
        var weights = options.Weights;

        // What the palette calls a query: the text without surrounding spaces. Only then does it decide
        // whether there is one - a query of spaces is the empty query, ordered by priority alone.
        var hasQuery = query.Trim().Length > 0;
        var terms = Split(query).Select(raw => DescribeTerm(raw, matcherInstance)).ToArray();

        var hidden = candidates.Where(command => command.IsVisible?.Invoke() == false).ToList();
        var visible = candidates.Where(command => command.IsVisible?.Invoke() != false).ToList();

        var matches = new List<BaletteMatch>(visible.Count);
        var unmatched = new List<BaletteCommand>();

        foreach (var command in visible)
        {
            if (matcherInstance.Match(command, query, weights) is { } match) matches.Add(match);
            else if (command.AlwaysShow) matches.Add(new BaletteMatch(command, double.NegativeInfinity, [], [], null));
            else unmatched.Add(command);
        }

        // No history is kept here, so frecency is 0 for every command - exactly what a first visit sees.
        var ranked = new BaletteRanker(options).Rank(matches, hasQuery, _ => 0);
        var listed = ranked.Take(options.MaxResults).ToList();
        var rows = Layout(listed, hasQuery, groupWhenSearching, options);

        var order = Positions(ranked);
        var row = Positions(rows);

        var results = new List<BaletteInspectedCommandDto>(candidates.Count);

        foreach (var match in rows.Concat(ranked.Skip(options.MaxResults)))
        {
            results.Add(Report(match, terms, matcherInstance, weights, order[match.Command], row.GetValueOrDefault(match.Command)));
        }

        foreach (var command in unmatched) results.Add(Unmatched(command, terms, matcherInstance, weights));

        foreach (var command in hidden)
        {
            results.Add(new BaletteInspectedCommandDto
            {
                Title = command.Title,
                Subtitle = command.Subtitle,
                Group = command.Group,
                Id = command.Id,
                Matched = false,
                Reason = "Hidden: its IsVisible predicate returned false, so the palette does not search it at all.",
                Priority = command.Priority
            });
        }

        var take = Math.Clamp(limit, 1, MaxLimit);

        Explain(notes, terms, hasQuery, groupWhenSearching, options, matcherName, ranked, listed, rows, demo: string.IsNullOrWhiteSpace(commands));

        return new BaletteSearchInspectionDto
        {
            Terms = terms,
            Matcher = matcherName,
            Commands = source,
            Ranking = hasQuery
                ? FormattableString.Invariant($"rank = score + priority x {options.PriorityWeight} + ln(1 + frecency) x {options.FrecencyWeight}; frecency is 0 here")
                : "rank = priority x 1000 + frecency (the empty query); frecency is 0 here",
            Searched = visible.Count,
            Matched = matches.Count(match => double.IsNegativeInfinity(match.Score) is false),
            Results = [.. results.Take(take)],
            HasMore = results.Count > take,
            Notes = [.. notes]
        };
    }

    /// <summary>
    /// The palette's layout of the ranked list: under group headings - ungrouped commands first, then on
    /// the empty query the groups in <see cref="BaletteOptions.GroupOrder"/>, then by their best rank, then
    /// by name - or, while searching with GroupWhenSearching off, the ranking itself. Inside a group the
    /// ranking's order is kept. This is the step <c>Balette.Rebuild</c> takes after BaletteRanker.Rank.
    /// </summary>
    private static List<BaletteMatch> Layout(List<BaletteMatch> ranked, bool hasQuery, bool groupWhenSearching, BaletteOptions options)
    {
        if (hasQuery && groupWhenSearching is false) return ranked;

        return [.. ranked
            .GroupBy(match => match.Command.Group ?? string.Empty)
            .Select(group => new { Name = group.Key, Items = group.ToList(), Best = group.Max(match => match.Rank), Order = GroupOrderIndex(options, group.Key) })
            .OrderBy(group => group.Name.Length == 0 ? 0 : 1)
            .ThenBy(group => hasQuery ? 0 : group.Order)
            .ThenByDescending(group => group.Best)
            .ThenBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase)
            .SelectMany(group => group.Items)];
    }

    /// <summary>
    /// Each command's 1-based place in a list. Keyed by the instance rather than by id: two commands
    /// passed in with the same title share a derived id, and are still two rows.
    /// </summary>
    private static Dictionary<BaletteCommand, int> Positions(IEnumerable<BaletteMatch> matches)
    {
        var positions = new Dictionary<BaletteCommand, int>(ReferenceEqualityComparer.Instance);

        foreach (var match in matches) positions.TryAdd(match.Command, positions.Count + 1);

        return positions;
    }

    private static int GroupOrderIndex(BaletteOptions options, string group)
    {
        var index = options.GroupOrder.FindIndex(name => string.Equals(name, group, StringComparison.OrdinalIgnoreCase));

        return index < 0 ? int.MaxValue : index;
    }

    private static BaletteInspectedCommandDto Report(BaletteMatch match, BaletteQueryTermDto[] terms, IBaletteMatcher matcher, BaletteMatchWeights weights, int order, int row)
    {
        var command = match.Command;

        if (double.IsNegativeInfinity(match.Score))
        {
            return new BaletteInspectedCommandDto
            {
                Title = command.Title,
                Subtitle = command.Subtitle,
                Group = command.Group,
                Id = command.Id,
                Matched = false,
                Reason = $"AlwaysShow: it is listed whatever the query, after every match. {Why(command, terms, matcher, weights)}",
                AlwaysShown = true,
                Order = order,
                Row = row == 0 ? null : row,
                Priority = command.Priority
            };
        }

        var scores = terms.Length == 0 ? [] : terms.Select(term => TermScore(command, term, matcher, weights)).ToArray();
        var fields = scores.Where(score => score.Score is not null).Select(score => score.Field!).Distinct(StringComparer.Ordinal).ToArray();

        return new BaletteInspectedCommandDto
        {
            Title = command.Title,
            Subtitle = command.Subtitle,
            Group = command.Group,
            Id = command.Id,
            Matched = true,
            Score = Math.Round(match.Score, 3),
            Rank = Math.Round(match.Rank, 3),
            Order = order,
            Row = row == 0 ? null : row,
            Priority = command.Priority,
            MatchedOn = fields.Length == 0 ? null : string.Join(" + ", fields),
            MatchedKeyword = match.MatchedKeyword,
            TitlePositions = match.TitlePositions.Length == 0 ? null : match.TitlePositions,
            SubtitlePositions = match.SubtitlePositions.Length == 0 ? null : match.SubtitlePositions,
            HighlightedTitle = match.TitlePositions.Length == 0 ? null : Highlight(command.Title, match.TitlePositions),
            HighlightedSubtitle = match.SubtitlePositions.Length == 0 || command.Subtitle is null ? null : Highlight(command.Subtitle, match.SubtitlePositions),
            // One term says no more than MatchedOn and Score already do.
            Terms = scores.Length > 1 ? scores : null
        };
    }

    private static BaletteInspectedCommandDto Unmatched(BaletteCommand command, BaletteQueryTermDto[] terms, IBaletteMatcher matcher, BaletteMatchWeights weights)
    {
        return new BaletteInspectedCommandDto
        {
            Title = command.Title,
            Subtitle = command.Subtitle,
            Group = command.Group,
            Id = command.Id,
            Matched = false,
            Reason = Why(command, terms, matcher, weights),
            Priority = command.Priority
        };
    }

    /// <summary>
    /// Why a command did not match: every term is matched on its own - the query is an AND of its
    /// terms, so the ones that fail alone are exactly the ones that fail it - and each failure is put
    /// in the words of the operator it was written with.
    /// </summary>
    private static string Why(BaletteCommand command, BaletteQueryTermDto[] terms, IBaletteMatcher matcher, BaletteMatchWeights weights)
    {
        var reasons = new List<string>();

        foreach (var term in terms)
        {
            if (matcher.Match(command, term.Text, weights) is not null) continue;

            reasons.Add(term.Operator switch
            {
                "negate" => $"'{term.Text}' excludes it: '{term.Term}' occurs in its {string.Join(" and ", NegatedFields(command, term.Term))}.",
                "exact" => $"'{term.Text}': no field contains '{term.Term}' as one unbroken run.",
                "prefix" => $"'{term.Text}': no field starts with '{term.Term}'.",
                "suffix" => $"'{term.Text}': no field ends with '{term.Term}'.",
                _ when term.CaseSensitive && matcher.Match(command, term.Text.ToLowerInvariant(), weights) is not null =>
                    $"'{term.Text}' has a capital letter, so it is matched case-sensitively (smart case) - and only '{term.Text.ToLowerInvariant()}' occurs, in another case.",
                "substring" => $"'{term.Text}' occurs in none of its fields as one unbroken run.",
                _ => $"The letters of '{term.Text}' do not occur in that order in its title, keywords, subtitle, group, description or tags."
            });
        }

        return reasons.Count == 0 ? "It matches no term of the query." : string.Join(" ", reasons);
    }

    /// <summary>The fields a negated term looks in - the title, the subtitle, the group and the keywords, ignoring case - that hold it.</summary>
    private static IEnumerable<string> NegatedFields(BaletteCommand command, string term)
    {
        const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

        if (command.Title.Contains(term, IgnoreCase)) yield return "title";
        if (command.Subtitle?.Contains(term, IgnoreCase) == true) yield return "subtitle";
        if (command.Group?.Contains(term, IgnoreCase) == true) yield return "group";
        if (command.Keywords.FirstOrDefault(keyword => keyword.Contains(term, IgnoreCase)) is { } keyword) yield return $"keyword '{keyword}'";
    }

    /// <summary>
    /// What one term scored against a command, and in which field. The library keeps the best weighted
    /// field and reports only the title and subtitle positions and the keyword, so for a term won by the
    /// group, the description or a tag, each of those is matched alone and the one whose weighted score
    /// is the term's score is the field - in the library's own order, title first, which is the order its
    /// ties go in.
    /// </summary>
    private static BaletteTermScoreDto TermScore(BaletteCommand command, BaletteQueryTermDto term, IBaletteMatcher matcher, BaletteMatchWeights weights)
    {
        if (term.Operator == "negate") return new BaletteTermScoreDto { Term = term.Text, Field = "not present" };

        var match = matcher.Match(command, term.Text, weights);
        if (match is null) return new BaletteTermScoreDto { Term = term.Text };

        string field;

        if (match.TitlePositions.Length > 0) field = "title";
        else if (match.MatchedKeyword is { } keyword) field = $"keyword '{keyword}'";
        else if (match.SubtitlePositions.Length > 0) field = "subtitle";
        else
        {
            (string Name, string? Text, double Weight)[] others =
            [
                ("group", command.Group, weights.Group),
                ("description", command.Description, weights.Description),
                .. command.Tags.Select(tag => ($"tag '{tag}'", (string?)tag, weights.Tag))
            ];

            field = others.FirstOrDefault(other => other.Text is { Length: > 0 }
                                                   && matcher.Match(new BaletteCommand { Title = other.Text }, term.Text, _titleOnly) is { } alone
                                                   && Math.Abs((alone.Score * other.Weight) - match.Score) < 1e-9).Name
                    ?? "group, description or tag";
        }

        return new BaletteTermScoreDto { Term = term.Text, Score = Math.Round(match.Score, 3), Field = field };
    }

    /// <summary>The text with every run of highlighted characters in brackets: "[G]o to [S]ettings".</summary>
    public static string Highlight(string text, int[] positions)
    {
        var marked = new HashSet<int>(positions.Where(position => position >= 0 && position < text.Length));
        var builder = new StringBuilder(text.Length + (2 * marked.Count));

        for (var i = 0; i < text.Length; i++)
        {
            var on = marked.Contains(i);

            if (on && (i == 0 || marked.Contains(i - 1) is false)) builder.Append('[');
            builder.Append(text[i]);
            if (on && (i == text.Length - 1 || marked.Contains(i + 1) is false)) builder.Append(']');
        }

        return builder.ToString();
    }

    /// <summary>The query's terms, split exactly where the palette splits them: on spaces.</summary>
    private static string[] Split(string query) => query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// One term with the operator its first or last character gives it - the syntax the guide documents
    /// (<c>'exact</c>, <c>^prefix</c>, <c>suffix$</c>, <c>!negate</c>; a lone operator character is a
    /// plain term) - and whether it matches case-sensitively.
    /// </summary>
    private static BaletteQueryTermDto DescribeTerm(string raw, IBaletteMatcher matcher)
    {
        var plain = matcher is BaletteSubstringMatcher ? "substring" : "fuzzy";

        var (term, op) = raw.Length > 1 ? raw[0] switch
        {
            '!' => (raw[1..], "negate"),
            '\'' => (raw[1..], "exact"),
            '^' => (raw[1..], "prefix"),
            _ when raw[^1] == '$' => (raw[..^1], "suffix"),
            _ => (raw, plain)
        } : (raw, plain);

        return new BaletteQueryTermDto
        {
            Text = raw,
            Term = term,
            Operator = op,
            CaseSensitive = op == "fuzzy" && matcher is BaletteFuzzyMatcher { IgnoreCase: false } && term.Any(char.IsUpper)
        };
    }

    private static (string Name, IBaletteMatcher Matcher) ResolveMatcher(string? matcher, List<string> notes)
    {
        switch ((matcher ?? "fuzzy").Trim().ToLowerInvariant())
        {
            case "" or "fuzzy" or "default" or "smart-case":
                return ("fuzzy", BaletteFuzzyMatcher.Default);

            case "fuzzy-ignore-case" or "ignore-case" or "ignorecase":
                return ("fuzzy-ignore-case", new BaletteFuzzyMatcher { IgnoreCase = true });

            case "substring":
                return ("substring", BaletteSubstringMatcher.Default);

            default:
                notes.Add($"'{matcher}' is not a matcher this tool runs, so the default fuzzy one was used. Use one of: {string.Join(", ", Matchers)}.");
                return ("fuzzy", BaletteFuzzyMatcher.Default);
        }
    }

    /// <summary>
    /// The palette's modes, for the demo's registry: a query that starts with a mode's prefix searches
    /// that mode - only the commands its Tags and Groups admit, none at all when it hides the registry -
    /// with the rest of the query. The longest prefix wins, as it does in the palette, so ">>" and ">"
    /// can coexist. The mode's own sources are asynchronous calls this tool does not make.
    /// </summary>
    private static string ApplyMode(string query, BaletteOptions options, List<BaletteCommand> candidates, List<string> notes)
    {
        var mode = options.Modes.OrderByDescending(m => m.Prefix.Length).FirstOrDefault(m => query.StartsWith(m.Prefix, StringComparison.Ordinal));
        if (mode is null) return query;

        candidates.RemoveAll(command => mode.HideRegistryCommands
                                        || (mode.Tags is { Count: > 0 } && command.Tags.Any(tag => mode.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)) is false)
                                        || (mode.Groups is { Count: > 0 } && (command.Group is null || mode.Groups.Contains(command.Group, StringComparer.OrdinalIgnoreCase) is false)));

        var scope = mode.HideRegistryCommands
            ? "it hides the registry and lists only its sources"
            : string.Join(" and ", new[]
            {
                mode.Tags is { Count: > 0 } ? $"only commands tagged {string.Join(" or ", mode.Tags.Select(tag => $"'{tag}'"))}" : null,
                mode.Groups is { Count: > 0 } ? $"only the {string.Join(" and ", mode.Groups.Select(group => $"'{group}'"))} group" : null
            }.Where(part => part is not null));

        var sources = mode.Sources.Count > 0 ? $" Its sources ({string.Join(", ", mode.Sources.Select(source => source.Name))}) answer asynchronously and are not run here." : string.Empty;

        notes.Add($"'{mode.Prefix}' is the prefix of this demo's '{mode.Name}' mode, so the query is '{query[mode.Prefix.Length..].Trim()}' within that mode: {(scope.Length > 0 ? scope : "every command")}.{sources}");

        return query[mode.Prefix.Length..];
    }

    /// <summary>
    /// The commands passed in: a JSON array of objects (or of plain titles), or one title per line. Only
    /// what a search reads is taken - the fields it matches, the priority it ranks by, AlwaysShow, the id.
    /// </summary>
    private static List<BaletteCommand> ParseCommands(string text, List<string> notes)
    {
        var trimmed = text.Trim();
        var commands = new List<BaletteCommand>();

        if (trimmed.StartsWith('[') is false)
        {
            foreach (var line in trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                commands.Add(BaletteCmd.New(line).Build());
            }

            return Capped(commands, notes);
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(trimmed, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException exception)
        {
            notes.Add($"commands starts with '[' but is not valid JSON ({exception.Message}), so nothing was searched. Pass a JSON array of {{ \"title\", \"subtitle\", \"keywords\", \"group\", \"description\", \"tags\", \"priority\" }} objects, or one title per line.");
            return commands;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                notes.Add("commands is not a JSON array, so nothing was searched.");
                return commands;
            }

            var ignored = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;

            foreach (var element in document.RootElement.EnumerateArray())
            {
                index++;

                if (element.ValueKind == JsonValueKind.String)
                {
                    if (string.IsNullOrWhiteSpace(element.GetString()) is false) commands.Add(BaletteCmd.New(element.GetString()!.Trim()).Build());
                    continue;
                }

                if (element.ValueKind != JsonValueKind.Object || Text(element, "title") is not { Length: > 0 } title)
                {
                    notes.Add($"Element {index} of commands has no title and was skipped.");
                    continue;
                }

                var builder = BaletteCmd.New(title)
                    .Subtitle(Text(element, "subtitle"))
                    .Description(Text(element, "description"))
                    .Group(Text(element, "group"))
                    .Keywords([.. List(element, "keywords")])
                    .Tags([.. List(element, "tags")])
                    .Priority(Priority(element, index, notes));

                if (Text(element, "id") is { Length: > 0 } id) builder.Id(id);
                if (Property(element, "alwaysShow") is { ValueKind: JsonValueKind.True }) builder.AlwaysShow();

                foreach (var property in element.EnumerateObject())
                {
                    if (_knownFields.Contains(property.Name) is false) ignored.Add(property.Name);
                }

                commands.Add(builder.Build());
            }

            if (ignored.Count > 0) notes.Add($"Fields a search does not read were ignored: {string.Join(", ", ignored)}.");
        }

        return Capped(commands, notes);
    }

    private static readonly HashSet<string> _knownFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "subtitle", "description", "group", "keywords", "tags", "priority", "id", "alwaysShow"
    };

    private static List<BaletteCommand> Capped(List<BaletteCommand> commands, List<string> notes)
    {
        if (commands.Count <= MaxCommands) return commands;

        notes.Add($"Only the first {MaxCommands} of the {commands.Count} commands were searched.");

        return commands[..MaxCommands];
    }

    private static JsonElement? Property(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }

        return null;
    }

    private static string? Text(JsonElement element, string name) => Property(element, name) switch
    {
        { ValueKind: JsonValueKind.String } value => value.GetString(),
        { ValueKind: JsonValueKind.Number } value => value.GetRawText(),
        _ => null
    };

    /// <summary>An array of strings, or one string split on commas and spaces the way BaletteItem's Keywords parameter is.</summary>
    private static IEnumerable<string> List(JsonElement element, string name) => Property(element, name) switch
    {
        { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).Where(item => item.Length > 0),
        { ValueKind: JsonValueKind.String } value => value.GetString()!.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        _ => []
    };

    /// <summary>A number, or the name of a BaletteCommandPriority level ("High").</summary>
    private static int Priority(JsonElement element, int index, List<string> notes)
    {
        switch (Property(element, "priority"))
        {
            case null:
                return BaletteCommandPriority.Normal;

            case { ValueKind: JsonValueKind.Number } number when number.TryGetInt32(out var value):
                return value;

            case { ValueKind: JsonValueKind.String } named when _priorityLevels.TryGetValue(named.GetString()!.Replace("BaletteCommandPriority.", string.Empty, StringComparison.Ordinal), out var level):
                return level;

            default:
                notes.Add($"The priority of element {index} is neither a whole number nor one of {string.Join(", ", _priorityLevels.Keys)}, so it was left at Normal (0).");
                return BaletteCommandPriority.Normal;
        }
    }

    private static void Explain(List<string> notes, BaletteQueryTermDto[] terms, bool hasQuery, bool groupWhenSearching, BaletteOptions options,
                                string matcherName, List<BaletteMatch> ranked, List<BaletteMatch> listed, List<BaletteMatch> rows, bool demo)
    {
        var weights = options.Weights;

        notes.Add(matcherName switch
        {
            "fuzzy-ignore-case" => "Matcher: new BaletteFuzzyMatcher { IgnoreCase = true } - fzf's algorithm (a term matches when its characters occur in order; word starts, camel humps and consecutive runs score most, gaps cost), with case never counting.",
            "substring" => "Matcher: BaletteSubstringMatcher.Default - a term has to occur as one unbroken run, ignoring case: no typo tolerance, and a match at the very start scores a bonus.",
            _ => "Matcher: BaletteFuzzyMatcher.Default, the palette's default - fzf's algorithm: a term matches when its characters occur in order; word starts, camel humps and consecutive runs score most, gaps cost, and a shorter text or an earlier match wins a tie. Smart case: a term in lower case ignores case, one with a capital letter only matches the same case."
        });

        if (hasQuery is false)
        {
            notes.Add($"The query is empty, so nothing is matched: every command scores 0 and rank = priority x 1000 + frecency - the priority decides. {(options.GroupOrder.Count > 0 ? $"The palette lists them under their groups: ungrouped first, then {string.Join(", ", options.GroupOrder)} (options.GroupOrder), then the other groups by their best rank." : "The palette lists them under their groups: ungrouped first, then the groups by their best rank, then by name.")}");

            if (demo && options.ShowModeHints && options.Modes.Count > 0)
            {
                notes.Add($"On the empty query the palette also lists this demo's {options.Modes.Count} modes ({string.Join(", ", options.Modes.Select(mode => $"'{mode.Prefix}' {mode.Name}"))}) under \"Modes\", at the lowest priority; they are not commands and are left out here.");
            }
        }
        else
        {
            var matching = terms.Where(term => term.Operator != "negate").ToArray();

            if (matching.Length > 1)
            {
                notes.Add($"{matching.Length} terms: every one has to match (AND), each against whichever field scores it best, and the command's score is the sum of the terms' scores - Terms shows what each one earned where.");
            }

            notes.Add(FormattableString.Invariant($"Field weights (BaletteOptions.Weights): title {weights.Title}, keywords {weights.Keyword}, subtitle {weights.Subtitle}, group {weights.Group}, description {weights.Description}, tags {weights.Tag} - a term's score in a field is multiplied by the field's weight and the best field counts, so the same hit scores more in the title than in a keyword. Only title and subtitle hits are highlighted; a keyword hit is shown under the title."));

            notes.Add(FormattableString.Invariant($"rank = score + priority x {options.PriorityWeight} + ln(1 + frecency) x {options.FrecencyWeight}: the match dominates, a priority of {BaletteCommandPriority.High} (High) is worth {BaletteCommandPriority.High * options.PriorityWeight} points, and in a running palette a command used often and recently climbs by its frecency - which is 0 for every command here, since this server keeps no history."));

            foreach (var term in terms)
            {
                switch (term.Operator)
                {
                    case "negate":
                        notes.Add($"'{term.Text}' is a negation: it drops every command whose title, subtitle, group or keywords contain '{term.Term}', ignoring case - its description and tags are not looked at - and adds nothing to the score of the rest.");
                        break;

                    case "exact":
                        notes.Add($"'{term.Text}' is exact: '{term.Term}' has to occur as one unbroken run in a field, ignoring case, and it scores like a perfect consecutive run.");
                        break;

                    case "prefix":
                        notes.Add($"'{term.Text}' is a prefix: a field has to start with '{term.Term}', ignoring case.");
                        break;

                    case "suffix":
                        notes.Add($"'{term.Text}' is a suffix: a field has to end with '{term.Term}', ignoring case.");
                        break;

                    case "fuzzy" when term.CaseSensitive:
                        notes.Add($"'{term.Text}' has a capital letter, so it matches case-sensitively (smart case): type it in lower case to ignore case, or run the fuzzy-ignore-case matcher.");
                        break;
                }
            }

            notes.Add(groupWhenSearching
                ? "GroupWhenSearching is on (the default): the palette lists the ranked commands under their group headings - ungrouped first, then the group holding the best match - so Row can differ from Order. Pass groupWhenSearching=false for one flat ranked list, where Row is Order."
                : "GroupWhenSearching is off: the palette lists one flat ranked list, so Row is Order.");
        }

        if (ranked.Count > listed.Count)
        {
            notes.Add($"Only the best {options.MaxResults} (BaletteOptions.MaxResults) are listed; the {ranked.Count - listed.Count} ranked after them have no Row.");
        }

        if (rows.FirstOrDefault(match => double.IsNegativeInfinity(match.Score)) is { } always)
        {
            notes.Add($"'{always.Command.Title}' is AlwaysShow: it is listed whatever the query, after every match - in its group's place when grouping, last otherwise.");
        }

        if (rows.FirstOrDefault(match => match.Command.Disabled) is { } disabled)
        {
            notes.Add($"'{disabled.Command.Title}' is disabled: it is listed and ranked like any other command, but Enter shows its DisabledReason instead of running it.");
        }

        if (hasQuery && ranked.Count(match => double.IsNegativeInfinity(match.Score) is false) == 0)
        {
            notes.Add("Nothing matched. The palette shows its empty state (and any AlwaysShow command) - try fewer letters, a lower-case term, or a keyword: SearchBalette(query: \"keywords\") explains where a command's aliases go.");
        }
    }

    /// <summary>
    /// The demo's own registry, built the way the demo builds it: its AddDemoServices registration, its
    /// DemoState, the commands its layout registers through BaletteCommands - registered with the real
    /// BaletteService, which flattens nested pages into the registry - and the root ones of those, the
    /// candidates of the palette's root page. Nothing here needs a browser: DemoState only reaches for
    /// JavaScript when a command runs, and no command runs here.
    /// </summary>
    private static Registry BuildDemoRegistry()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IJSRuntime>(NoBrowser.Instance);
        services.AddDemoServices();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var state = scope.ServiceProvider.GetRequiredService<DemoState>();
        var palette = scope.ServiceProvider.GetRequiredService<IBaletteService>();

        using var registration = palette.Register(DemoCommands.Global(state));

        return new Registry(scope.ServiceProvider.GetRequiredService<BaletteOptions>(), [.. palette.Commands.Where(command => command.Parent is null)]);
    }

    /// <summary>The JavaScript runtime of a server that has no browser: DemoState never calls it, because no command is run here.</summary>
    private sealed class NoBrowser : IJSRuntime
    {
        public static readonly NoBrowser Instance = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException("The MCP server inspects commands; it never runs them, so there is no browser to call.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException("The MCP server inspects commands; it never runs them, so there is no browser to call.");
    }
}
