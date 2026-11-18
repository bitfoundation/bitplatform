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

    /// <summary>Started, and now waiting for something outside the run: an approval, an event, a person.</summary>
    Waiting,

    /// <summary>Stopped part-way, because the run was cancelled while it worked.</summary>
    Cancelled,
}
