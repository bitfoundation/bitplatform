namespace Bit.Blow;

/// <summary>
/// A node's state in a run, handed to the canvas through <c>BlowCanvas.RunStates</c>. Shown on the node - a class, a
/// badge whose shape differs by status, a ring while running - and read out as part of its description.
/// </summary>
/// <param name="Status">Where the node is in the run.</param>
/// <param name="Message">What to say about it: an error, a result, how far along it is. Read out after the status.</param>
public readonly record struct BlowRunState(BlowRunStatus Status, string? Message = null);
