using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Bheadless;

/// <summary>The result of an accessibility audit.</summary>
public sealed class BitBhA11yReport
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    internal BitBhA11yReport(IReadOnlyList<BitBhA11yViolation> violations, int elementCount)
    {
        Violations = violations;
        ElementCount = elementCount;
    }

    /// <summary>The violations found, most severe first.</summary>
    public IReadOnlyList<BitBhA11yViolation> Violations { get; }

    /// <summary>The number of elements inspected.</summary>
    public int ElementCount { get; }

    /// <summary>Whether no violations were found.</summary>
    public bool Passed => Violations.Count == 0;

    /// <summary>A human-readable summary.</summary>
    public override string ToString()
        => Passed
            ? $"No accessibility violations in {ElementCount} elements."
            : $"{Violations.Count} accessibility violation(s) in {ElementCount} elements:\n\n" + string.Join("\n\n", Violations);

    /// <summary>A Markdown table (for CI job summaries).</summary>
    public string ToMarkdown(string? title = null)
    {
        var sb = new StringBuilder();
        if (title is not null) sb.AppendLine($"### {title}").AppendLine();
        if (Passed)
        {
            sb.AppendLine($"✅ No accessibility violations ({ElementCount} elements checked).");
            return sb.ToString();
        }
        sb.AppendLine("| Impact | Rule | Element | Problem |").AppendLine("| --- | --- | --- | --- |");
        foreach (var v in Violations)
            sb.AppendLine($"| {v.Impact} | `{v.RuleId}` | `{v.Selector.Replace("|", "\\|")}` | {v.Message.Replace("|", "\\|")} |");
        return sb.ToString();
    }

    /// <summary>The violations as JSON (for tooling): <c>{ passed, elements, violations: [{ rule, impact, message, selector, html }] }</c>.</summary>
    public string ToJson()
    {
        // Built as a JSON DOM rather than serialised by reflection: stable property order, trimming-safe.
        var violations = new JsonArray();
        foreach (var v in Violations)
        {
            violations.Add(new JsonObject
            {
                ["rule"] = v.RuleId,
                ["impact"] = v.Impact.ToString().ToLowerInvariant(),
                ["message"] = v.Message,
                ["selector"] = v.Selector,
                ["html"] = v.Html,
            });
        }
        return new JsonObject { ["passed"] = Passed, ["elements"] = ElementCount, ["violations"] = violations }.ToJsonString(Indented);
    }
}
