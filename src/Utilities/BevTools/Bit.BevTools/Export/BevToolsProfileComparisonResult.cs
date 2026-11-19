using System.Globalization;
using System.Text;

namespace Bit.BevTools;

/// <summary>Two recordings compared type by type, regressions first.</summary>
public sealed class BevToolsProfileComparisonResult
{
    internal BevToolsProfileComparisonResult(IReadOnlyList<BevToolsProfileTypeChange> changes, IReadOnlyList<string> warnings, DateTimeOffset? baselineRecordedAt, DateTimeOffset? currentRecordedAt)
    {
        Changes = changes;
        Warnings = warnings;
        BaselineRecordedAt = baselineRecordedAt;
        CurrentRecordedAt = currentRecordedAt;
    }

    /// <summary>Every type that rendered in either recording: regressions, improvements, new, gone, then unchanged.</summary>
    public IReadOnlyList<BevToolsProfileTypeChange> Changes { get; }

    /// <summary>Why the two may not be comparable: different runtimes or render modes, a window in one only, a truncated registry.</summary>
    public IReadOnlyList<string> Warnings { get; }

    public DateTimeOffset? BaselineRecordedAt { get; }

    public DateTimeOffset? CurrentRecordedAt { get; }

    public IEnumerable<BevToolsProfileTypeChange> Regressions => Changes.Where(c => c.Kind == BevToolsProfileChangeKind.Regressed);

    public bool HasRegressions => Changes.Any(c => c.Kind == BevToolsProfileChangeKind.Regressed);

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
        Section(sb, "Regressed", BevToolsProfileChangeKind.Regressed, maxRowsPerSection);
        Section(sb, "Improved", BevToolsProfileChangeKind.Improved, maxRowsPerSection);
        Section(sb, "New", BevToolsProfileChangeKind.New, maxRowsPerSection);
        Section(sb, "No longer rendered", BevToolsProfileChangeKind.Gone, maxRowsPerSection);
        foreach (var warning in Warnings)
        {
            sb.Append("> ").AppendLine(warning.ReplaceLineEndings(" "));
        }

        return sb.ToString();
    }

    private void Section(StringBuilder sb, string title, BevToolsProfileChangeKind kind, int maxRows)
    {
        var rows = Changes.Where(c => c.Kind == kind).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"#### {title} ({rows.Count})").AppendLine();
        sb.AppendLine("| Component | Instances | Renders | Render time | p95 | Max | Allocated per render | Why |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");
        foreach (var c in rows.Take(maxRows))
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| `{Cell(c.DisplayName)}` | {c.BaselineInstances} → {c.CurrentInstances} | {c.BaselineRenders} → {c.CurrentRenders} | {Ms(c.BaselineRenderMs)} → {Ms(c.CurrentRenderMs)} | {Ms(c.BaselineP95RenderMs)} → {Ms(c.CurrentP95RenderMs)} | {Ms(c.BaselineMaxRenderMs)} → {Ms(c.CurrentMaxRenderMs)} | {Bytes(c.BaselineBytesPerRender)} → {Bytes(c.CurrentBytesPerRender)} | {Cell(c.Reason ?? "")} |");
        }

        if (rows.Count > maxRows)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| … {rows.Count - maxRows} more | | | | | | | |");
        }

        sb.AppendLine();
    }

    // Names come from an export file: one line, no pipes or backticks, so a row stays a row.
    private static string Cell(string text) => text.ReplaceLineEndings(" ").Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "'", StringComparison.Ordinal);

    internal static string Ms(double ms) => ms.ToString(ms >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture) + " ms";

    private static string Ms(double? ms) => ms is { } value ? Ms(value) : "–";

    internal static string Bytes(double? bytes) => bytes is { } value ? BevToolsHttpMessageHandler.FormatBytes((long)Math.Round(value)) : "–";

    internal static string Signed(int value) => value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
}
