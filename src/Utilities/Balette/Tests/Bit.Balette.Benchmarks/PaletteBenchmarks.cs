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
/// One keystroke end to end, measured on everything whose cost grows with the registry. It repeats what the
/// palette component does on the root page in the default mode: snapshot the registered commands, keep the visible
/// top-level ones, parse the query once, match every candidate (keeping <see cref="BaletteCommand.AlwaysShow"/>
/// commands that miss) and rank the matches down to <see cref="BaletteOptions.MaxResults"/>. Grouping the ranked
/// matches into rows and rendering them is left out: it is bounded by MaxResults, whatever the registry size. This
/// mirrors private component code, so it has to follow changes to <c>Balette.Rebuild</c>.
/// </summary>
[MemoryDiagnoser]
public class KeystrokeBenchmarks
{
    private readonly BaletteOptions _options = new();
    private BaletteService _service = null!;
    private BaletteRanker _ranker = null!;
    private BaletteHistory _history = null!;

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
        _service = new BaletteService(_options);
        _service.Register(registry);
        _ranker = new BaletteRanker(_options);
        _history = Workloads.History(registry, _options);
    }

    /// <summary>Turning the current search text into the ranked matches the palette renders.</summary>
    [Benchmark]
    public int Keystroke()
    {
        // Snapshot, as RefreshSnapshot does before every rebuild.
        var all = AllCommands().ToList();
        var parentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BaletteCommand command in all)
        {
            if (command.Parent is not null)
            {
                parentIds.Add(command.Parent);
            }
        }

        string effectiveQuery = Query.Trim();
        bool hasQuery = effectiveQuery.Length > 0;
        IBaletteMatcher matcher = _options.Matcher;
        var query = BaletteQuery.Parse(effectiveQuery);

        // The root page's candidates, matched as MatchCandidates does.
        var registryMatches = new List<BaletteMatch>();
        foreach (BaletteCommand command in all.Where(c => c.Parent is null).Where(c => c.IsVisible?.Invoke() != false))
        {
            BaletteMatch? match = matcher.Match(command, query, _options.Weights);
            if (match is null)
            {
                if (command.AlwaysShow)
                {
                    registryMatches.Add(new BaletteMatch(command, double.NegativeInfinity, [], [], null));
                }

                continue;
            }

            registryMatches.Add(match);
        }

        // The registry matches are copied before async source results would be interleaved into them.
        var matches = new List<BaletteMatch>(registryMatches);
        List<BaletteMatch> ranked = _ranker.Rank(matches, hasQuery, id => _history.Frecency(id), _options.MaxResults);
        return ranked.Count + parentIds.Count;
    }

    private IEnumerable<BaletteCommand> AllCommands()
    {
        foreach (BaletteCommand command in _service.Commands)
        {
            yield return command;
        }
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
