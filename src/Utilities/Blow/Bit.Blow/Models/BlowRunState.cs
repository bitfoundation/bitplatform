namespace Bit.Blow;

/// <summary>Where a node is in a run of the graph - a workflow executing, a pipeline building, a state machine stepping.</summary>
public enum BlowRunStatus
{
    /// <summary>Not part of the run, or the run has not reached it. Nothing is shown.</summary>
    Idle,

    /// <summary>Waiting for its turn.</summary>
    Queued,

    /// <summary>Working now.</summary>
    Running,

    /// <summary>Finished successfully.</summary>
    Succeeded,

    /// <summary>Finished with an error.</summary>
    Failed,

    /// <summary>Not run, because something it depends on did not succeed or a condition passed it by.</summary>
    Skipped,
}

/// <summary>
/// A node's state in a run, handed to the canvas through <c>BlowCanvas.RunStates</c>. Shown on the node - a class, a
/// badge whose shape differs by status, a ring while running - and read out as part of its description.
/// </summary>
/// <param name="Status">Where the node is in the run.</param>
/// <param name="Message">What to say about it: an error, a result, how far along it is. Read out after the status.</param>
public readonly record struct BlowRunState(BlowRunStatus Status, string? Message = null);
