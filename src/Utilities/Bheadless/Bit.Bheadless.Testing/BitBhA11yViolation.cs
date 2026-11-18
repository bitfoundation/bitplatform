using AngleSharp.Dom;

namespace Bit.Bheadless;

/// <summary>One accessibility problem found by <see cref="BitBhA11y.Audit(INode, BitBhA11yOptions?)"/>.</summary>
/// <param name="RuleId">The rule that failed (e.g. <c>aria-valid-attr-value</c>).</param>
/// <param name="Impact">The severity.</param>
/// <param name="Message">What is wrong and how to fix it.</param>
/// <param name="Selector">A CSS selector locating the element.</param>
/// <param name="Html">The element's opening tag.</param>
public sealed record BitBhA11yViolation(string RuleId, BitBhA11yImpact Impact, string Message, string Selector, string Html)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Impact}] {RuleId}: {Message}\n    at {Selector}\n    {Html}";
}
