using System.Text.RegularExpressions;

namespace Bit.Borm;

/// <summary>
/// Enforces JSON Schema's <c>"format": "uuid"</c>. A rule of its own rather than a
/// <see cref="BormPatternRule"/>, because a document can say both — a UUID that must also match
/// the document's own <c>pattern</c> — and two pattern rules share one key, so the second replaced the
/// first.
/// </summary>
internal sealed class BormUuidRule(string? message = null) : IBormValidationRule
{
    private static readonly Regex? Uuid = BormRegex.Create(BormJsonSchemaImporter.UuidPattern);

    /// <inheritdoc />
    public string Key => "uuid";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        if (ctx.Value is not string { Length: > 0 } s) return new(BormRuleResult.Success());
        return BormRegex.IsMatch(Uuid, s) == false
            ? new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Uuid)))
            : new(BormRuleResult.Success());
    }
}
