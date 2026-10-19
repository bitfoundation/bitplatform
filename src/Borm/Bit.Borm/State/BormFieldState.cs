namespace Bit.Borm;

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
