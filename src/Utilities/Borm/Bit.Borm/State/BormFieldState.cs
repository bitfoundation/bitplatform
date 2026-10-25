namespace Bit.Borm;

/// <summary>A field's validity in one word. See <see cref="BormFieldState.Status"/>.</summary>
public enum BormFieldStatus
{
    /// <summary>No validation pass has judged the current value yet.</summary>
    Unvalidated,

    /// <summary>An asynchronous rule is checking it right now.</summary>
    Pending,

    /// <summary>Judged, and carries an error.</summary>
    Invalid,

    /// <summary>Judged, and nothing is wrong with it.</summary>
    Valid
}

/// <summary>
/// A snapshot of one field's interaction and validation state, returned by
/// <see cref="BormState.GetFieldState"/>.
/// </summary>
/// <param name="IsTouched">Whether the user has interacted with the field.</param>
/// <param name="IsDirty">Whether the value differs from the one the form opened with.</param>
/// <param name="IsInvalid">
/// Whether the field currently carries an error-severity message. Warnings do not make a field
/// invalid — they are advice, not a refusal.
/// </param>
/// <param name="Messages">Every message recorded against the field, in the order they were produced.</param>
/// <param name="IsValidating">
/// Whether an asynchronous rule is currently running for the field — a remote uniqueness check, say.
/// False for a field whose rules are all synchronous, which is almost all of them.
/// </param>
public readonly record struct BormFieldState(
    bool IsTouched,
    bool IsDirty,
    bool IsInvalid,
    IReadOnlyList<BormValidationMessage> Messages,
    bool IsValidating = false)
{
    /// <summary>
    /// Whether every rule of the field has judged its current value — a validation pass that reached it
    /// since it last changed. What tells "valid" from "nobody has looked yet": a field with no messages
    /// is both until this says which.
    /// </summary>
    public bool IsValidated { get; init; }

    /// <summary>
    /// The field's validity in one word: <see cref="BormFieldStatus.Pending"/> while an asynchronous rule
    /// is running, <see cref="BormFieldStatus.Invalid"/> with an error on it,
    /// <see cref="BormFieldStatus.Valid"/> once judged and found fine, and
    /// <see cref="BormFieldStatus.Unvalidated"/> before that — what a "✓ available" beside a username
    /// binds to without claiming it before the lookup has answered.
    /// </summary>
    public BormFieldStatus Status => IsValidating ? BormFieldStatus.Pending
        : IsInvalid ? BormFieldStatus.Invalid
        : IsValidated ? BormFieldStatus.Valid
        : BormFieldStatus.Unvalidated;

    /// <summary>The first error-severity message, or null when the field has none.</summary>
    public BormValidationMessage? Error
    {
        get
        {
            for (var i = 0; i < Messages.Count; i++)
                if (Messages[i].Severity == BormValidationSeverity.Error) return Messages[i];
            return null;
        }
    }
}
