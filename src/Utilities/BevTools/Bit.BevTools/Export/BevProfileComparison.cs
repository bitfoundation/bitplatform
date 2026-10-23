using System.Globalization;
using System.Text;

namespace Bit.BevTools;

/// <summary>When <see cref="BevProfileComparison"/> calls a change a regression or an improvement.</summary>
public sealed class BevProfileComparisonOptions
{
    /// <summary>A type renders this many times more (or fewer) at least. Default 1: the same interaction renders the same number of times.</summary>
    public int MinRenderChange { get; set; } = 1;

    /// <summary>
    /// …and by more than this fraction of the baseline's renders. Default 0, so counts are compared exactly; raise it
    /// (0.1 tolerates 100 → 110) for recordings made by hand, where a timer or a late response can add a render.
    /// </summary>
    public double RenderChangeRatio { get; set; }

    /// <summary>A type's total render time changes by this many milliseconds at least. Default 5.</summary>
    public double MinTimeChangeMs { get; set; } = 5;

    /// <summary>…and by more than this fraction of the baseline's time. Default 0.5: timings vary from run to run, counts do not.</summary>
    public double TimeChangeRatio { get; set; } = 0.5;
}

public enum BevProfileChangeKind
{
    /// <summary>Renders as often as in the baseline, and its time changed less than the thresholds.</summary>
    Unchanged,

    /// <summary>Renders more often, or takes clearly longer, than in the baseline.</summary>
    Regressed,

    /// <summary>Renders less often, or takes clearly less time, without rendering more often or taking clearly longer.</summary>
    Improved,

    /// <summary>Rendered in the current recording only.</summary>
    New,

    /// <summary>Rendered in the baseline only.</summary>
    Gone,
}

/// <summary>One component type in both recordings. Times are totals of BuildRenderTree in milliseconds.</summary>
public sealed record BevProfileTypeChange(
    string Type,
    string DisplayName,
    BevProfileChangeKind Kind,
    int BaselineInstances,
    int CurrentInstances,
    int BaselineRenders,
    int CurrentRenders,
    double BaselineRenderMs,
    double CurrentRenderMs,
    double BaselineMaxRenderMs,
    double CurrentMaxRenderMs,
    double? BaselineP95RenderMs,
    double? CurrentP95RenderMs,
    string? Reason);

/// <summary>Two recordings compared type by type, regressions first.</summary>
public sealed class BevProfileComparisonResult
{
    internal BevProfileComparisonResult(IReadOnlyList<BevProfileTypeChange> changes, IReadOnlyList<string> warnings, DateTimeOffset? baselineRecordedAt, DateTimeOffset? currentRecordedAt)
    {
        Changes = changes;
        Warnings = warnings;
        BaselineRecordedAt = baselineRecordedAt;
        CurrentRecordedAt = currentRecordedAt;
    }

    /// <summary>Every type that rendered in either recording: regressions, improvements, new, gone, then unchanged.</summary>
    public IReadOnlyList<BevProfileTypeChange> Changes { get; }

    /// <summary>Why the two may not be comparable: different runtimes or render modes, a window in one only, a truncated registry.</summary>
    public IReadOnlyList<string> Warnings { get; }

    public DateTimeOffset? BaselineRecordedAt { get; }

    public DateTimeOffset? CurrentRecordedAt { get; }

    public IEnumerable<BevProfileTypeChange> Regressions => Changes.Where(c => c.Kind == BevProfileChangeKind.Regressed);

    public bool HasRegressions => Changes.Any(c => c.Kind == BevProfileChangeKind.Regressed);

    public int BaselineRenders => Changes.Sum(c => c.BaselineRenders);

    public int CurrentRenders => Changes.Sum(c => c.CurrentRenders);

    public double BaselineRenderMs => Changes.Sum(c => c.BaselineRenderMs);

    public double CurrentRenderMs => Changes.Sum(c => c.CurrentRenderMs);

    /// <summary>A report for a pull request or a CI log: totals, then a table per kind of change, then the warnings.</summary>
    public string ToMarkdown(int maxRowsPerSection = 25)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### BevTools render profile comparison").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"Renders {BaselineRenders} → {CurrentRenders} ({Signed(CurrentRenders - BaselineRenders)}) · render time {Ms(BaselineRenderMs)} → {Ms(CurrentRenderMs)}");
        sb.AppendLine().AppendLine();
        sb.AppendLine(HasRegressions ? $"**{Regressions.Count()} regression{(Regressions.Count() == 1 ? "" : "s")}**" : "No regressions.").AppendLine();
        Section(sb, "Regressed", BevProfileChangeKind.Regressed, maxRowsPerSection);
        Section(sb, "Improved", BevProfileChangeKind.Improved, maxRowsPerSection);
        Section(sb, "New", BevProfileChangeKind.New, maxRowsPerSection);
        Section(sb, "No longer rendered", BevProfileChangeKind.Gone, maxRowsPerSection);
        foreach (var warning in Warnings)
        {
            sb.Append("> ").AppendLine(warning.ReplaceLineEndings(" "));
        }

        return sb.ToString();
    }

    private void Section(StringBuilder sb, string title, BevProfileChangeKind kind, int maxRows)
    {
        var rows = Changes.Where(c => c.Kind == kind).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"#### {title} ({rows.Count})").AppendLine();
        sb.AppendLine("| Component | Instances | Renders | Render time | p95 | Max | Why |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---|");
        foreach (var c in rows.Take(maxRows))
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| `{Cell(c.DisplayName)}` | {c.BaselineInstances} → {c.CurrentInstances} | {c.BaselineRenders} → {c.CurrentRenders} | {Ms(c.BaselineRenderMs)} → {Ms(c.CurrentRenderMs)} | {Ms(c.BaselineP95RenderMs)} → {Ms(c.CurrentP95RenderMs)} | {Ms(c.BaselineMaxRenderMs)} → {Ms(c.CurrentMaxRenderMs)} | {Cell(c.Reason ?? "")} |");
        }

        if (rows.Count > maxRows)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| … {rows.Count - maxRows} more | | | | | | |");
        }

        sb.AppendLine();
    }

    // Names come from an export file: one line, no pipes or backticks, so a row stays a row.
    private static string Cell(string text) => text.ReplaceLineEndings(" ").Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "'", StringComparison.Ordinal);

    internal static string Ms(double ms) => ms.ToString(ms >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture) + " ms";

    private static string Ms(double? ms) => ms is { } value ? Ms(value) : "–";

    internal static string Signed(int value) => value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Compares two render profiles of the same interaction, type by type, to tell what a change made render more often
/// or for longer. A profile is the <c>profile</c> section of a session export (<see cref="BevSessionExporter.ToJson"/>):
/// for a reliable comparison, reset the profiler baseline, perform the interaction, export, in both recordings. Counts
/// are compared exactly by default; times only beyond <see cref="BevProfileComparisonOptions"/>' thresholds, as they
/// vary between runs and machines. Types are matched by full name without assembly versions.
/// </summary>
/// <example>
/// In a test that drives an interaction (bUnit, Playwright):
/// <code>
/// var current = BevSessionExporter.ToJson(session, new() { Sections = new HashSet&lt;string&gt; { "profile" } });
/// var result = BevProfileComparison.Compare(File.ReadAllText("checkout.profile.json"), current);
/// Assert.IsFalse(result.HasRegressions, result.ToMarkdown());
/// </code>
/// </example>
public static class BevProfileComparison
{
    /// <summary>Compares two session exports.</summary>
    /// <exception cref="FormatException">Either text is not a BevTools session export with a profile or components section.</exception>
    public static BevProfileComparisonResult Compare(string baselineJson, string currentJson, BevProfileComparisonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baselineJson);
        ArgumentNullException.ThrowIfNull(currentJson);
        return Compare(BevRenderProfile.Parse(baselineJson), BevRenderProfile.Parse(currentJson), options);
    }

    /// <summary>Compares a session export with what <paramref name="current"/> has recorded so far. Call on the session's dispatcher.</summary>
    /// <exception cref="FormatException"><paramref name="baselineJson"/> is not a BevTools session export with a profile or components section.</exception>
    public static BevProfileComparisonResult Compare(string baselineJson, BevToolsSession current, BevProfileComparisonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baselineJson);
        ArgumentNullException.ThrowIfNull(current);
        return Compare(BevRenderProfile.Parse(baselineJson), BevRenderProfile.Of(current), options);
    }

    internal static BevProfileComparisonResult Compare(BevRenderProfile baseline, BevRenderProfile current, BevProfileComparisonOptions? options)
    {
        options ??= new BevProfileComparisonOptions();
        var before = baseline.Types.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.First());
        var after = current.Types.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.First());
        var changes = new List<BevProfileTypeChange>();
        foreach (var type in before.Keys.Union(after.Keys))
        {
            before.TryGetValue(type, out var b);
            after.TryGetValue(type, out var a);
            var (kind, reason) = Classify(b, a, options);
            changes.Add(new BevProfileTypeChange(
                type,
                (a ?? b)!.DisplayName,
                kind,
                b?.Instances ?? 0,
                a?.Instances ?? 0,
                b?.Renders ?? 0,
                a?.Renders ?? 0,
                b?.RenderMs ?? 0,
                a?.RenderMs ?? 0,
                b?.MaxRenderMs ?? 0,
                a?.MaxRenderMs ?? 0,
                b?.P95RenderMs,
                a?.P95RenderMs,
                reason));
        }

        var ordered = changes
            .OrderBy(c => c.Kind switch
            {
                BevProfileChangeKind.Regressed => 0,
                BevProfileChangeKind.Improved => 1,
                BevProfileChangeKind.New => 2,
                BevProfileChangeKind.Gone => 3,
                _ => 4,
            })
            .ThenByDescending(c => Math.Abs(c.CurrentRenders - c.BaselineRenders))
            .ThenByDescending(c => Math.Abs(c.CurrentRenderMs - c.BaselineRenderMs))
            .ToList();
        return new BevProfileComparisonResult(ordered, Warnings(baseline, current), baseline.RecordedAt, current.RecordedAt);
    }

    private static (BevProfileChangeKind Kind, string? Reason) Classify(BevProfileTypeTotals? baseline, BevProfileTypeTotals? current, BevProfileComparisonOptions options)
    {
        if (baseline is null)
        {
            return (BevProfileChangeKind.New, current!.Renders + " render" + (current.Renders == 1 ? "" : "s") + ", " + BevProfileComparisonResult.Ms(current.RenderMs));
        }

        if (current is null)
        {
            return (BevProfileChangeKind.Gone, "rendered " + baseline.Renders + " time" + (baseline.Renders == 1 ? "" : "s") + " in the baseline");
        }

        var renderDelta = current.Renders - baseline.Renders;
        var rendersChanged = Math.Abs(renderDelta) >= options.MinRenderChange && Math.Abs(renderDelta) > baseline.Renders * options.RenderChangeRatio;
        var timeDelta = current.RenderMs - baseline.RenderMs;
        var timeChanged = Math.Abs(timeDelta) >= options.MinTimeChangeMs && Math.Abs(timeDelta) > baseline.RenderMs * options.TimeChangeRatio;
        var renders = "renders " + BevProfileComparisonResult.Signed(renderDelta) + Percent(renderDelta, baseline.Renders);
        var time = "time " + (timeDelta >= 0 ? "+" : "-") + BevProfileComparisonResult.Ms(Math.Abs(timeDelta)) + Percent(timeDelta, baseline.RenderMs);
        if (rendersChanged && renderDelta > 0)
        {
            return (BevProfileChangeKind.Regressed, renders + (timeChanged ? ", " + time : ""));
        }

        if (timeChanged && timeDelta > 0)
        {
            return (BevProfileChangeKind.Regressed, time + (renderDelta < 0 ? ", " + renders : "") + (current.Instances != baseline.Instances ? ", instances " + baseline.Instances + " → " + current.Instances : ""));
        }

        // Not regressed, so renders did not rise beyond the thresholds and time did not grow clearly.
        if ((rendersChanged && renderDelta < 0) || (timeChanged && timeDelta < 0))
        {
            return (BevProfileChangeKind.Improved, string.Join(", ", new[] { rendersChanged ? renders : null, timeChanged ? time : null }.Where(r => r is not null)));
        }

        return (BevProfileChangeKind.Unchanged, null);
    }

    private static string Percent(double delta, double baseline) =>
        baseline <= 0 ? "" : " (" + (delta >= 0 ? "+" : "-") + Math.Abs(delta / baseline * 100).ToString("0", CultureInfo.InvariantCulture) + " %)";

    private static string MajorMinor(string runtime) =>
        System.Text.RegularExpressions.Regex.Match(runtime, @"\d+\.\d+") is { Success: true } match ? runtime[..match.Index] + match.Value : runtime;

    private static List<string> Warnings(BevRenderProfile baseline, BevRenderProfile current)
    {
        var warnings = new List<string>();
        if (baseline.SinceBaseline != current.SinceBaseline)
        {
            warnings.Add((baseline.SinceBaseline ? "The baseline counts since a profiler reset, the current recording since the session started" : "The current recording counts since a profiler reset, the baseline since the session started")
                + ": reset the profiler before the interaction in both, or in neither.");
        }

        if (baseline.Truncated || current.Truncated)
        {
            warnings.Add("The " + (baseline.Truncated && current.Truncated ? "two recordings" : baseline.Truncated ? "baseline" : "current recording")
                + " reached the component limit (MaxTrackedComponents): components created after it are missing.");
        }

        if (baseline.RenderMode is { } before && current.RenderMode is { } after && before != after)
        {
            warnings.Add("Recorded in different render modes (" + before + " → " + after + "): render counts are comparable, times are not.");
        }

        // A patch release does not change what renders cost; another major or minor version can.
        if (baseline.Runtime is { } runtimeBefore && current.Runtime is { } runtimeAfter && MajorMinor(runtimeBefore) != MajorMinor(runtimeAfter))
        {
            warnings.Add("Recorded on different runtimes (" + runtimeBefore + " → " + runtimeAfter + "): times are not comparable.");
        }

        if (baseline.FromComponents || current.FromComponents)
        {
            warnings.Add("The " + (baseline.FromComponents && current.FromComponents ? "two exports have" : baseline.FromComponents ? "baseline export has" : "current export has")
                + " no profile section (an older BevTools): it was read from the components section, which misses components let go before the export"
                + " and whose maxima are not limited to the reset window.");
        }

        return warnings;
    }
}
