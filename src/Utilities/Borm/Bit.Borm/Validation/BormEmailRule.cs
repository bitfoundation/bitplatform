using System.Text.RegularExpressions;

namespace Bit.Borm;

/// <summary>Validates an email address.</summary>
public sealed class BormEmailRule(string? message = null) : IBormValidationRule
{
    // Pragmatic email pattern; intentionally not RFC-perfect. Anchored and free of nested quantifiers,
    // so it cannot backtrack catastrophically on hostile input — and given the same match timeout as
    // every schema pattern regardless, since "not catastrophic" is still quadratic on a long enough
    // string. The end anchor is \z rather than $: in .NET `$` also matches before a final newline, so
    // "a@b.co\n" passed.
    private static readonly Regex Rx =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+\z", RegexOptions.Compiled | RegexOptions.CultureInvariant, BormRegex.MatchTimeout);

    /// <inheritdoc />
    public string Key => "email";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var s = ctx.Value as string;
        if (string.IsNullOrEmpty(s)) return new(BormRuleResult.Success());

        return BormRegex.IsMatch(Rx, s) switch
        {
            true => new(BormRuleResult.Success()),
            false => new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Email))),
            null => new(BormRuleResult.Fail(ctx.Message(BormMessageKeys.PatternTimeout)))
        };
    }
}
