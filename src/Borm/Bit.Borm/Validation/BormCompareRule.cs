namespace Bit.Borm;

/// <summary>
/// Requires the field to equal the value at another path — the "confirm password" / "confirm email"
/// rule. The other path is resolved against the whole form, so it works across nested objects.
/// </summary>
public sealed class BormCompareRule(string otherPath, string? otherLabel = null, string? message = null)
    : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "compare";

    /// <summary>The path of the field this one must match.</summary>
    public string OtherPath => otherPath;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var other = ctx.Data.GetValue(otherPath);
        var mine = ctx.Value;

        // An empty value is left to the required rule, so the two errors do not stack.
        if (mine is null || (mine is string s && s.Length == 0))
            return new(BormRuleResult.Success());

        return new(AreEqual(mine, other)
            ? BormRuleResult.Success()
            : BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Compare, otherLabel ?? otherPath)));
    }

    /// <summary>
    /// Text is compared exactly, numbers by value. <see cref="object.Equals(object, object)"/> alone
    /// called an <c>int</c> 5 and a <c>long</c> 5 different — which is what a typed PIN confirmed
    /// against a dictionary-backed one (always stored as <c>long</c>) looked like.
    /// </summary>
    private static bool AreEqual(object mine, object? other)
    {
        if (mine is string a && other is string b) return string.Equals(a, b, StringComparison.Ordinal);

        if (BormNumber.IsNumber(mine) && BormNumber.IsNumber(other))
            return string.Equals(BormNumber.Canonical(mine), BormNumber.Canonical(other), StringComparison.Ordinal);

        return Equals(mine, other);
    }
}
