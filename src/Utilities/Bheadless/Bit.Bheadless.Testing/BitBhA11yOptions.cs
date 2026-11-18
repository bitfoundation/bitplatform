using AngleSharp.Dom;

namespace Bit.Bheadless;

/// <summary>Options for <see cref="BitBhA11y.Audit(INode, BitBhA11yOptions?)"/>.</summary>
public sealed class BitBhA11yOptions
{
    /// <summary>Rules to skip (by id).</summary>
    public ISet<string> DisabledRules { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Only report violations at or above this impact. Default: <see cref="BitBhA11yImpact.Minor"/>.</summary>
    public BitBhA11yImpact MinimumImpact { get; set; } = BitBhA11yImpact.Minor;

    /// <summary>
    /// Whether the markup is a fragment of a page. When true, ID references
    /// to elements outside the fragment are not reported — they may live elsewhere
    /// on the page. Default: false.
    /// </summary>
    public bool IsFragment { get; set; }

    /// <summary>Disables a rule (fluent).</summary>
    public BitBhA11yOptions Without(params string[] ruleIds)
    {
        foreach (var id in ruleIds) DisabledRules.Add(id);
        return this;
    }
}
