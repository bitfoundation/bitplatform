namespace Bit.Bcrdt;

/// <summary>Describes a change to an undo or redo stack.</summary>
/// <param name="Item">The step that was added or updated.</param>
/// <param name="IsRedoStack">Whether the step landed on the redo stack rather than the undo stack.</param>
/// <param name="Origin">The origin of the transaction responsible.</param>
public readonly record struct BcrdtUndoStackEventArgs(BcrdtUndoStackItem Item, bool IsRedoStack, object? Origin);
