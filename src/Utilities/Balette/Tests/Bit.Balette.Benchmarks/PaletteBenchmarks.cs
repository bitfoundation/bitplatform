using BenchmarkDotNet.Attributes;

namespace Bit.Balette.Benchmarks;

/// <summary>
/// The fuzzy matcher on a single field. It runs once per searchable field of every command on every keystroke, so a
/// registry of 2,000 commands calls it thousands of times per keystroke, and on WebAssembly it runs interpreted. It
/// reuses per-thread buffers for its score matrices, so a hit allocates only the positions it returns and a miss
/// allocates nothing.
/// </summary>
[MemoryDiagnoser]
public class FuzzyMatcherBenchmarks
{
    private const string Title = "View: Toggle Primary Side Bar Visibility";

    private readonly BaletteFuzzyMatcher _matcher = BaletteFuzzyMatcher.Default;
    private string _description = null!;
    private string _article = null!;

    [GlobalSetup]
    public void Setup()
    {
        _description = Workloads.RichCommand().Description!;
        _article = Workloads.LongText(2_000);
    }

    /// <summary>Two letters at word starts of a short title: the textbook palette query ("gs" for Go to Settings).</summary>
    [Benchmark]
    public double ShortHit() => _matcher.Match("gs", "Go to Settings").Score;

    /// <summary>A few keystrokes into a word in the middle of a typical title.</summary>
    [Benchmark]
    public double TypicalHit() => _matcher.Match("sett", "Preferences: Open Settings (JSON)").Score;

    /// <summary>An acronym spread over a 40-character title, where every letter is a word start reached across a gap.</summary>
    [Benchmark]
    public double AcronymHit() => _matcher.Match("tpsb", Title).Score;

    /// <summary>A 20-letter query against the same title: the score matrix grows with query length times text length.</summary>
    [Benchmark]
    public double LongQueryHit() => _matcher.Match("toggleprimarysidebar", Title).Score;

    /// <summary>An upper-case query, which switches smart case to case-sensitive matching.</summary>
    [Benchmark]
    public double SmartCaseHit() => _matcher.Match("PSB", Title).Score;

    /// <summary>Non-ASCII text, which leaves the ASCII fast path of case folding for <c>char.ToLowerInvariant</c>.</summary>
    [Benchmark]
    public double NonAsciiHit() => _matcher.Match("über", "Übersicht öffnen").Score;

    /// <summary>A word inside a 107-character description, the longest field most commands have.</summary>
    [Benchmark]
    public double DescriptionHit() => _matcher.Match("hosts", _description).Score;

    /// <summary>
    /// A query against 2,000 characters of prose. The matrices then exceed the 16K cells the per-thread buffers keep,
    /// so this call allocates its own: the price of putting whole articles into a searchable field.
    /// </summary>
    [Benchmark]
    public double OversizedFieldHit() => _matcher.Match("rollback", _article).Score;

    /// <summary>
    /// A query that is not a subsequence of the title. This is rejected after case folding and one linear scan,
    /// without building the score matrices, and it is what most fields cost once a query is a few letters long.
    /// </summary>
    [Benchmark]
    public bool Reject() => _matcher.Match("qjz", Title).Matched;
}

/// <summary>
/// Matching a whole command: every term of the query is scored against the title, keywords, subtitle, group,
/// description and tags, and the best weighted field wins; the operator terms use an ordinal comparison instead of
/// the fuzzy matcher. The palette parses the query once per keystroke and reuses it for every command, which is what
/// <see cref="Match"/> measures; <see cref="MatchParsingEachCall"/> is the public string overload, which parses the
/// query on every call.
/// </summary>
[MemoryDiagnoser]
public class CommandMatchBenchmarks
{
    private readonly IBaletteMatcher _matcher = BaletteFuzzyMatcher.Default;
    private readonly BaletteMatchWeights _weights = new();
    private BaletteCommand _command = null!;
    private BaletteQuery _query = null!;

    /// <summary>
    /// The query. "tog" hits the title; "explr" misses the title and hits a keyword and the description; "tog side vis"
    /// is three terms that all have to match; the next three use the exact, prefix and suffix operators; the two
    /// negations keep and drop the command; "qjz" matches no field at all.
    /// </summary>
    [Params("tog", "explr", "tog side vis", "'side", "^tog", "bility$", "side !debug", "side !explorer", "qjz")]
    public string Query { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _command = Workloads.RichCommand();
        _query = BaletteQuery.Parse(Query);
    }

    /// <summary>Matching with a query parsed once, as the palette does for every command of a keystroke.</summary>
    [Benchmark(Baseline = true)]
    public double Match() => _matcher.Match(_command, _query, _weights)?.Score ?? double.NaN;

    /// <summary>Matching through the public string overload, which splits and parses the query on every call.</summary>
    [Benchmark]
    public double MatchParsingEachCall() => _matcher.Match(_command, Query, _weights)?.Score ?? double.NaN;
}

/// <summary>
/// Ranking the matches of one keystroke: frecency is looked up for every match, then matches are ordered by rank,
/// title and original position. The palette renders at most <see cref="BaletteOptions.MaxResults"/> rows (150 by
/// default), so it asks the ranker for the best 150 and only those are sorted and copied; <see cref="RankAll"/> is
/// the public overload, which sorts and copies every match.
/// </summary>
[MemoryDiagnoser]
public class RankerBenchmarks
{
    private readonly BaletteOptions _options = new();
    private BaletteRanker _ranker = null!;
    private List<BaletteMatch> _matches = null!;
    private List<BaletteMatch> _unscored = null!;
    private Func<string, double> _frecency = null!;

    /// <summary>
    /// Matches to rank. A first keystroke matches most of a registry, so these are also roughly the registry sizes
    /// at which ranking matters.
    /// </summary>
    [Params(200, 2_000, 20_000)]
    public int Matches { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _ranker = new BaletteRanker(_options);

        // The matches of a first keystroke, scored by the real matcher, and the same commands as an empty query
        // lists them. A loaded history supplies frecency, as it does in the palette.
        _matches = Workloads.Matches(Matches, "s");
        var commands = _matches.Select(m => m.Command).ToList();
        _unscored = Workloads.Unscored(commands);
        BaletteHistory history = Workloads.History(commands, _options);
        _frecency = history.Frecency;
    }

    /// <summary>Ranking and sorting every match, the public overload.</summary>
    [Benchmark(Baseline = true)]
    public int RankAll() => _ranker.Rank(_matches, hasQuery: true, _frecency).Count;

    /// <summary>Keeping only the best 150, which is what the palette asks for on every keystroke.</summary>
    [Benchmark]
    public int RankTop150() => _ranker.Rank(_matches, hasQuery: true, _frecency, _options.MaxResults).Count;

    /// <summary>
    /// The empty query, when the palette has just opened: every command is unscored, so ranks come from priority and
    /// frecency alone and most of them tie, which sends the comparison to the culture-aware title comparer.
    /// </summary>
    [Benchmark]
    public int RankEmptyQueryTop150() => _ranker.Rank(_unscored, hasQuery: false, _frecency, _options.MaxResults).Count;
}

/// <summary>
/// One keystroke end to end, as <c>Balette.Rebuild</c> runs it: parse the query once, match the root page's candidates
/// (every visible command without a parent), match the nested commands the root finds too
/// (<see cref="BaletteOptions.SearchNestedCommands"/>), and rank the matches down to <see cref="BaletteOptions.MaxResults"/>.
/// The snapshot of the registry is taken once, outside the keystroke, as the palette now does: it re-reads the registry
/// only when it changed (see <see cref="SnapshotBenchmarks"/>). Grouping the ranked matches into rows and rendering
/// them is left out: it is bounded by MaxResults, whatever the registry size. This mirrors private component code, so it
/// has to follow changes to <c>Balette.Rebuild</c>.
/// </summary>
[MemoryDiagnoser]
public class KeystrokeBenchmarks
{
    private readonly BaletteOptions _options = new();
    private BaletteRanker _ranker = null!;
    private BaletteHistory _history = null!;
    private List<BaletteCommand> _all = null!;
    private Dictionary<string, BaletteCommand> _byId = null!;

    /// <summary>Commands in the registry.</summary>
    [Params(200, 2_000, 20_000)]
    public int Commands { get; set; }

    /// <summary>
    /// The text in the search box. Empty is the palette just opened; "s" is the first keystroke, which matches most of
    /// the registry; "sett" is a few keystrokes in; "open sett" is two terms; "qjz" matches nothing and is rejected
    /// field by field.
    /// </summary>
    [Params("", "s", "sett", "open sett", "qjz")]
    public string Query { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        BaletteCommand[] registry = Workloads.Registry(Commands);
        var service = new BaletteService(_options);
        service.Register(registry);
        _ranker = new BaletteRanker(_options);
        _history = Workloads.History(registry, _options);
        _all = [.. service.Commands];
        _byId = _all.ToDictionary(c => c.Id, StringComparer.Ordinal);
    }

    /// <summary>Turning the current search text into the ranked matches the palette renders.</summary>
    [Benchmark]
    public int Keystroke() => KeystrokeMirror.Run(_all, _byId, Query, _options, _ranker, _history);
}

/// <summary>
/// The same keystroke over a registry where a quarter of the commands are nested, two levels deep - pages such as
/// "Theme" or "Language" with their options - so the nested pass (an ancestor chain per command, one more field to
/// match) is measured on real work rather than skipped.
/// </summary>
[MemoryDiagnoser]
public class NestedKeystrokeBenchmarks
{
    private readonly BaletteOptions _options = new();
    private BaletteRanker _ranker = null!;
    private BaletteHistory _history = null!;
    private List<BaletteCommand> _all = null!;
    private Dictionary<string, BaletteCommand> _byId = null!;

    /// <summary>Commands in the registry, nested ones included.</summary>
    [Params(2_000, 20_000)]
    public int Commands { get; set; }

    /// <summary>A first keystroke, a word, a word with an ancestor's title, and a query that matches nothing.</summary>
    [Params("s", "sett", "area sett", "qjz")]
    public string Query { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        BaletteCommand[] flat = Workloads.Registry(Commands);
        var nestedCount = Commands / 4;
        var roots = flat.Skip(nestedCount).ToList();

        // Pages of ten options each, every fifth page one level deeper under the page before it.
        var pages = new List<BaletteCommand>();
        for (int i = 0, page = 0; i < nestedCount; i += 10, page++)
        {
            pages.Add(BaletteCmd.New($"Area {page}").Group("Areas").Id($"area-{page}").Children(flat.Skip(i).Take(10)).Build());
        }

        for (var i = 4; i < pages.Count; i += 5)
        {
            pages[i] = pages[i].With(b => b.Parent(pages[i - 1].Id));
        }

        var service = new BaletteService(_options);
        service.Register([.. roots, .. pages]);
        _ranker = new BaletteRanker(_options);
        _history = Workloads.History(roots, _options);
        _all = [.. service.Commands];
        _byId = _all.ToDictionary(c => c.Id, StringComparer.Ordinal);
    }

    /// <summary>The keystroke, nested pass included.</summary>
    [Benchmark]
    public int Keystroke() => KeystrokeMirror.Run(_all, _byId, Query, _options, _ranker, _history);
}

/// <summary>
/// The snapshot the palette takes of the registry: once when it opens on a changed registry, and after every change of
/// it - no longer on every keystroke. This is the work the keystroke no longer does.
/// </summary>
[MemoryDiagnoser]
public class SnapshotBenchmarks
{
    private BaletteService _service = null!;

    /// <summary>Commands in the registry.</summary>
    [Params(200, 2_000, 20_000)]
    public int Commands { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _service = new BaletteService(new BaletteOptions());
        _service.Register(Workloads.Registry(Commands));
    }

    /// <summary>One command per id, the parents that have pages, and the lookup by id, as RefreshSnapshot builds them.</summary>
    [Benchmark]
    public int Snapshot()
    {
        var byId = new Dictionary<string, BaletteCommand>(StringComparer.Ordinal);
        var all = _service.Commands.Where(c => byId.TryAdd(c.Id, c)).ToList();
        var parentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BaletteCommand command in all)
        {
            if (command.Parent is not null)
            {
                parentIds.Add(command.Parent);
            }
        }

        return all.Count + parentIds.Count;
    }
}

/// <summary>What <c>Balette.Rebuild</c> and <c>MatchCandidates</c> do per keystroke, for the benchmarks above.</summary>
internal static class KeystrokeMirror
{
    public static int Run(List<BaletteCommand> all, Dictionary<string, BaletteCommand> byId, string text, BaletteOptions options, BaletteRanker ranker, BaletteHistory history)
    {
        string effectiveQuery = text.Trim();
        bool hasQuery = effectiveQuery.Length > 0;
        IBaletteMatcher matcher = options.Matcher;
        var query = BaletteQuery.Parse(effectiveQuery);

        // The root page's candidates.
        var matches = new List<BaletteMatch>();
        foreach (BaletteCommand command in all)
        {
            if (command.Parent is not null || command.IsVisible?.Invoke() == false)
            {
                continue;
            }

            BaletteMatch? match = matcher.Match(command, query, options.Weights);
            if (match is null)
            {
                if (command.AlwaysShow)
                {
                    matches.Add(new BaletteMatch(command, double.NegativeInfinity, [], [], null));
                }

                continue;
            }

            matches.Add(match);
        }

        // Nested commands, with their ancestors' titles as one more field (ChainOf, memoized per keystroke).
        if (hasQuery && options.SearchNestedCommands)
        {
            var chains = new Dictionary<string, (string[] Titles, string Text)?>(StringComparer.Ordinal);
            foreach (BaletteCommand command in all)
            {
                if (command.Parent is null || command.IsVisible?.Invoke() == false || Chain(command.Parent, byId, chains) is not { } chain)
                {
                    continue;
                }

                if (matcher.Match(command, query, options.Weights, chain.Text) is { } match)
                {
                    matches.Add(match with { Score = match.Score - chain.Titles.Length, Path = chain.Titles });
                }
            }
        }

        List<BaletteMatch> ranked = ranker.Rank(matches, hasQuery, id => history.Frecency(id), options.MaxResults);
        return ranked.Count;
    }

    private static (string[] Titles, string Text)? Chain(string id, Dictionary<string, BaletteCommand> byId, Dictionary<string, (string[] Titles, string Text)?> memo)
    {
        if (memo.TryGetValue(id, out var known))
        {
            return known;
        }

        (string[] Titles, string Text)? chain = null;
        if (byId.TryGetValue(id, out var command) && !command.Disabled && !command.MultiSelectChildren && command.IsVisible?.Invoke() != false)
        {
            chain = command.Parent is null
                ? ([command.Title], command.Title)
                : Chain(command.Parent, byId, memo) is { } parent ? ([.. parent.Titles, command.Title], parent.Text + " " + command.Title) : null;
        }

        memo[id] = chain;
        return chain;
    }
}

/// <summary>
/// Parsing and normalizing keyboard shortcuts. Parsing runs when a command is registered (the string conversion in
/// an initializer, or the builder), and the normalized form is computed for every command with a shortcut each time
/// a global shortcut fires, so both should stay negligible next to the search path.
/// </summary>
[MemoryDiagnoser]
public class ShortcutBenchmarks
{
    private readonly BaletteKeyboardShortcut _combo = BaletteKeyboardShortcut.Parse("Ctrl+Shift+P");

    /// <summary>A single chord with the platform modifier, the palette's own toggle.</summary>
    [Benchmark]
    public int ParseChord() => BaletteKeyboardShortcut.Parse("Mod+K").Chords.Count;

    /// <summary>A chord with two modifiers.</summary>
    [Benchmark]
    public int ParseCombo() => BaletteKeyboardShortcut.Parse("Ctrl+Shift+P").Chords.Count;

    /// <summary>A two-chord sequence.</summary>
    [Benchmark]
    public int ParseSequence() => BaletteKeyboardShortcut.Parse("G I").Chords.Count;

    /// <summary>The canonical form a pressed shortcut is compared against.</summary>
    [Benchmark]
    public int Normalize() => _combo.Normalized.Length;
}
