using System.Globalization;
using System.Text;

namespace Bit.BevTools;

/// <summary>
/// Compares two render profiles of the same interaction, type by type, to tell what a change made render more often
/// or for longer. A profile is the <c>profile</c> section of a session export (<see cref="BevToolsSessionExporter.ToJson"/>):
/// for a reliable comparison, reset the profiler baseline, perform the interaction, export, in both recordings. Counts
/// are compared exactly by default; times only beyond <see cref="BevToolsProfileComparisonOptions"/>' thresholds, as they
/// vary between runs and machines. Types are matched by full name without assembly versions.
/// </summary>
/// <example>
/// In a test that drives an interaction (bUnit, Playwright):
/// <code>
/// var current = BevToolsSessionExporter.ToJson(session, new() { Sections = new HashSet&lt;string&gt; { "profile" } });
/// var result = BevToolsProfileComparison.Compare(File.ReadAllText("checkout.profile.json"), current);
/// Assert.IsFalse(result.HasRegressions, result.ToMarkdown());
/// </code>
/// </example>
public static class BevToolsProfileComparison
{
    /// <summary>Compares two session exports.</summary>
    /// <exception cref="FormatException">Either text is not a BevTools session export with a profile or components section.</exception>
    public static BevToolsProfileComparisonResult Compare(string baselineJson, string currentJson, BevToolsProfileComparisonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baselineJson);
        ArgumentNullException.ThrowIfNull(currentJson);
        return Compare(BevToolsRenderProfile.Parse(baselineJson), BevToolsRenderProfile.Parse(currentJson), options);
    }

    /// <summary>Compares a session export with what <paramref name="current"/> has recorded so far. Call on the session's dispatcher.</summary>
    /// <exception cref="FormatException"><paramref name="baselineJson"/> is not a BevTools session export with a profile or components section.</exception>
    public static BevToolsProfileComparisonResult Compare(string baselineJson, BevToolsSession current, BevToolsProfileComparisonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baselineJson);
        ArgumentNullException.ThrowIfNull(current);
        return Compare(BevToolsRenderProfile.Parse(baselineJson), BevToolsRenderProfile.Of(current), options);
    }

    internal static BevToolsProfileComparisonResult Compare(BevToolsRenderProfile baseline, BevToolsRenderProfile current, BevToolsProfileComparisonOptions? options)
    {
        options ??= new BevToolsProfileComparisonOptions();
        var before = baseline.Types.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.First());
        var after = current.Types.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.First());
        var changes = new List<BevToolsProfileTypeChange>();
        foreach (var type in before.Keys.Union(after.Keys))
        {
            before.TryGetValue(type, out var b);
            after.TryGetValue(type, out var a);
            var (kind, reason) = Classify(b, a, options, SameRenderMode(baseline, current));
            changes.Add(new BevToolsProfileTypeChange(
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
                reason)
            {
                BaselineAllocatedBytes = b?.AllocatedBytes,
                CurrentAllocatedBytes = a?.AllocatedBytes,
            });
        }

        var ordered = changes
            .OrderBy(c => c.Kind switch
            {
                BevToolsProfileChangeKind.Regressed => 0,
                BevToolsProfileChangeKind.Improved => 1,
                BevToolsProfileChangeKind.New => 2,
                BevToolsProfileChangeKind.Gone => 3,
                _ => 4,
            })
            .ThenByDescending(c => Math.Abs(c.CurrentRenders - c.BaselineRenders))
            .ThenByDescending(c => Math.Abs(c.CurrentRenderMs - c.BaselineRenderMs))
            .ToList();
        return new BevToolsProfileComparisonResult(ordered, Warnings(baseline, current), baseline.RecordedAt, current.RecordedAt);
    }

    // As Warnings decides it: two render modes run on different runtimes (a circuit's CoreCLR, the browser's Mono), whose
    // allocations differ for the same code.
    private static bool SameRenderMode(BevToolsRenderProfile baseline, BevToolsRenderProfile current) =>
        baseline.RenderMode is not { } before || current.RenderMode is not { } after || before == after;

    private static (BevToolsProfileChangeKind Kind, string? Reason) Classify(BevToolsProfileTypeTotals? baseline, BevToolsProfileTypeTotals? current, BevToolsProfileComparisonOptions options, bool sameRenderMode)
    {
        if (baseline is null)
        {
            return (BevToolsProfileChangeKind.New, current!.Renders + " render" + (current.Renders == 1 ? "" : "s") + ", " + BevToolsProfileComparisonResult.Ms(current.RenderMs));
        }

        if (current is null)
        {
            return (BevToolsProfileChangeKind.Gone, "rendered " + baseline.Renders + " time" + (baseline.Renders == 1 ? "" : "s") + " in the baseline");
        }

        var renderDelta = current.Renders - baseline.Renders;
        var rendersChanged = Math.Abs(renderDelta) >= options.MinRenderChange && Math.Abs(renderDelta) > baseline.Renders * options.RenderChangeRatio;
        var timeDelta = current.RenderMs - baseline.RenderMs;
        var timeChanged = Math.Abs(timeDelta) >= options.MinTimeChangeMs && Math.Abs(timeDelta) > baseline.RenderMs * options.TimeChangeRatio;
        var renders = "renders " + BevToolsProfileComparisonResult.Signed(renderDelta) + Percent(renderDelta, baseline.Renders);
        var time = "time " + (timeDelta >= 0 ? "+" : "-") + BevToolsProfileComparisonResult.Ms(Math.Abs(timeDelta)) + Percent(timeDelta, baseline.RenderMs);
        // Per render: what each render costs the garbage collector, which a change to the component's code moves and the
        // machine it ran on does not.
        var bytesBefore = baseline.AllocatedBytes is { } ab && baseline.Renders > 0 ? (double)ab / baseline.Renders : (double?)null;
        var bytesAfter = current.AllocatedBytes is { } aa && current.Renders > 0 ? (double)aa / current.Renders : (double?)null;
        var allocationDelta = bytesBefore is { } pb && bytesAfter is { } pa ? pa - pb : 0;
        var allocationsChanged = sameRenderMode && bytesBefore is not null && bytesAfter is not null
            && Math.Abs(allocationDelta) >= options.MinAllocationChangeBytes && Math.Abs(allocationDelta) > bytesBefore.Value * options.AllocationChangeRatio;
        var allocations = "allocated per render " + (allocationDelta >= 0 ? "+" : "-") + BevToolsProfileComparisonResult.Bytes(Math.Abs(allocationDelta)) + Percent(allocationDelta, bytesBefore ?? 0);
        var moreAllocations = allocationsChanged && allocationDelta > 0 ? ", " + allocations : "";
        if (rendersChanged && renderDelta > 0)
        {
            return (BevToolsProfileChangeKind.Regressed, renders + (timeChanged ? ", " + time : "") + moreAllocations);
        }

        if (timeChanged && timeDelta > 0)
        {
            return (BevToolsProfileChangeKind.Regressed, time + (renderDelta < 0 ? ", " + renders : "") + (current.Instances != baseline.Instances ? ", instances " + baseline.Instances + " → " + current.Instances : "") + moreAllocations);
        }

        if (allocationsChanged && allocationDelta > 0)
        {
            return (BevToolsProfileChangeKind.Regressed, allocations + (rendersChanged ? ", " + renders : ""));
        }

        // Not regressed, so renders did not rise beyond the thresholds and neither time nor allocations grew clearly.
        if ((rendersChanged && renderDelta < 0) || (timeChanged && timeDelta < 0) || (allocationsChanged && allocationDelta < 0))
        {
            return (BevToolsProfileChangeKind.Improved, string.Join(", ", new[] { rendersChanged ? renders : null, timeChanged ? time : null, allocationsChanged ? allocations : null }.Where(r => r is not null)));
        }

        return (BevToolsProfileChangeKind.Unchanged, null);
    }

    private static string Percent(double delta, double baseline) =>
        baseline <= 0 ? "" : " (" + (delta >= 0 ? "+" : "-") + Math.Abs(delta / baseline * 100).ToString("0", CultureInfo.InvariantCulture) + " %)";

    private static string MajorMinor(string runtime) =>
        System.Text.RegularExpressions.Regex.Match(runtime, @"\d+\.\d+") is { Success: true } match ? runtime[..match.Index] + match.Value : runtime;

    private static List<string> Warnings(BevToolsRenderProfile baseline, BevToolsRenderProfile current)
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
            warnings.Add("Recorded in different render modes (" + before + " → " + after + "): render counts are comparable, times and allocations are not.");
        }

        // A patch release does not change what renders cost; another major or minor version can.
        if (baseline.Runtime is { } runtimeBefore && current.Runtime is { } runtimeAfter && MajorMinor(runtimeBefore) != MajorMinor(runtimeAfter))
        {
            warnings.Add("Recorded on different runtimes (" + runtimeBefore + " → " + runtimeAfter + "): render counts are comparable, times and allocations are not.");
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
