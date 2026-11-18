namespace Bit.Bcrdt;

/// <summary>
/// What a plain-text control should be updated to, and what it must still contain for the update to
/// be safe.
/// </summary>
/// <param name="Value">The new value to show.</param>
/// <param name="Expected">
/// The value the control is believed to hold right now. If it holds anything else the user has typed
/// since this was produced; the update must be skipped and retried after that input is processed.
/// </param>
/// <param name="SelectionStart">The selection to restore, in <paramref name="Value"/> coordinates.</param>
/// <param name="SelectionEnd">The end of the selection to restore.</param>
/// <param name="Sequence">Identifies the document state <paramref name="Value"/> was taken from.</param>
public sealed record BcrdtTextControlUpdate(
    string Value,
    string Expected,
    int SelectionStart,
    int SelectionEnd,
    long Sequence)
{
    /// <summary>
    /// Identifies this update among the ones the binding has produced, increasing from one. A control
    /// remembers the identifier of the last update it applied and reports it back, so the binding can
    /// tell whether an update whose confirmation was lost was applied - see
    /// <see cref="BcrdtTextInputBinding.Resolve"/>.
    /// </summary>
    public long Id { get; init; }

    internal BcrdtTextProjection Projection { get; init; } = null!;
}
