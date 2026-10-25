namespace Bit.Blow;

/// <summary>
/// How much history the canvas is holding, raised through <c>BlowCanvas.OnHistoryChanged</c>.
/// Enough to drive a pair of undo/redo buttons without reaching for the history itself.
/// </summary>
/// <param name="CanUndo">Whether there is an earlier state to go back to.</param>
/// <param name="CanRedo">Whether a state was undone and has not been superseded by a new change.</param>
/// <param name="UndoDepth">How many steps can be undone.</param>
/// <param name="RedoDepth">How many steps can be redone.</param>
public readonly record struct BlowHistoryState(bool CanUndo, bool CanRedo, int UndoDepth, int RedoDepth)
{
    /// <summary>What undoing would take back - <c>move</c>, <c>connect</c>, ... - or null; see <see cref="BlowHistory.UndoLabel"/>.</summary>
    public string? UndoLabel { get; init; }

    /// <summary>What redoing would bring back, or null; see <see cref="BlowHistory.RedoLabel"/>.</summary>
    public string? RedoLabel { get; init; }
}
