namespace Bit.BlazorUI;

/// <summary>How much of a row an inline edit opens.</summary>
public enum BitDataGridEditMode
{
    /// <summary>The whole row opens at once, with Save/Cancel in the command column.</summary>
    Row = 0,

    /// <summary>One cell opens at a time: Enter, F2 or a double-click opens it; Enter, Tab (which opens the next) or moving the focus out of it commits; Escape cancels.</summary>
    Cell = 1
}
