namespace Bit.Borm;

/// <summary>A single validation message tied to a field path.</summary>
/// <param name="FieldPath">The path of the field the message belongs to.</param>
/// <param name="Message">Human-readable message.</param>
/// <param name="Severity">Severity of the message.</param>
/// <remarks>
/// Two messages are equal when they say the same thing about the same field with the same severity —
/// what the user reads — whichever rule or source produced them. That is how the same complaint from
/// two validators is shown once, and why <see cref="Code"/> and <see cref="Source"/>, which describe
/// where a message came from rather than what it says, take no part in equality.
/// </remarks>
public sealed record BormValidationMessage(string FieldPath, string Message, BormValidationSeverity Severity = BormValidationSeverity.Error)
{
    /// <summary>
    /// What kind of rule reported it, when known: the rule's <see cref="IBormValidationRule.Key"/>
    /// (<c>required</c>, <c>maxLength</c>, <c>pattern</c>, …), <c>conversion</c> for an entry the model
    /// could not accept, or whatever code an external validator or server supplied. Stable across
    /// languages, so a test, an analytics event or a custom message component can key off it instead
    /// of the translated text.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>Where the message came from.</summary>
    public BormValidationSource Source { get; init; }

    public bool Equals(BormValidationMessage? other)
        => other is not null
           && string.Equals(FieldPath, other.FieldPath, StringComparison.Ordinal)
           && string.Equals(Message, other.Message, StringComparison.Ordinal)
           && Severity == other.Severity;

    public override int GetHashCode() => HashCode.Combine(FieldPath, Message, Severity);
}
