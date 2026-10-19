namespace Bit.Borm;

/// <summary>A validation rule applied to a field.</summary>
public interface IBormValidationRule
{
    /// <summary>Validates the field. Synchronous rules can return a completed task.</summary>
    ValueTask<BormRuleResult> ValidateAsync(BormValidationContext context);

    /// <summary>
    /// True if the rule performs asynchronous work (e.g. remote calls). Used to decide whether
    /// to run the rule on every keystroke or defer it to blur/submit.
    /// </summary>
    bool IsAsync => false;

    /// <summary>
    /// Optional identity for the kind of rule (e.g. <c>required</c>, <c>maxLength</c>). Rules that expose a
    /// key replace an existing rule with the same key when added through
    /// <see cref="BormFieldDefinition.AddValidator"/>, so a schema generated from DataAnnotations and
    /// then refined with the fluent builder never reports the same problem twice.
    /// Rules without a key always accumulate.
    /// </summary>
    string? Key => null;
}
