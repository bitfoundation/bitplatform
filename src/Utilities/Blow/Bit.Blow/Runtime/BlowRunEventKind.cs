namespace Bit.Blow;

/// <summary>What happened, in a run's event stream (<see cref="BlowRunRequest.OnEvent"/>, <see cref="BlowRunResult.Events"/>).</summary>
public enum BlowRunEventKind
{
    /// <summary>The run began. Every node in it is queued.</summary>
    RunStarted,

    /// <summary>The node is waiting for its turn.</summary>
    NodeQueued,

    /// <summary>An attempt of the node started, or it went back to work after waiting.</summary>
    NodeStarted,

    /// <summary>The node said how far along it is (<see cref="BlowNodeRunContext.Report"/>).</summary>
    NodeProgress,

    /// <summary>The node is waiting for something outside the run (<see cref="BlowNodeRunContext.WaitAsync{T}"/>).</summary>
    NodeWaiting,

    /// <summary>An attempt failed and the node will be tried again after <see cref="BlowRunEvent.RetryDelay"/>.</summary>
    NodeRetrying,

    /// <summary>The node finished successfully.</summary>
    NodeSucceeded,

    /// <summary>The node failed, after every attempt its retry policy allowed.</summary>
    NodeFailed,

    /// <summary>The node was not run: a branch passed it by, something before it failed, or the run stopped first.</summary>
    NodeSkipped,

    /// <summary>The node was working when the run was cancelled.</summary>
    NodeCancelled,

    /// <summary>The node's result was taken from a checkpoint instead of running it again (<see cref="BlowRunRequest.Resume"/>).</summary>
    NodeRestored,

    /// <summary>A value went along the edge (<see cref="BlowRunEvent.EdgeId"/>) to the node after it.</summary>
    EdgeActivated,

    /// <summary>The run ended. <see cref="BlowRunEvent.Outcome"/> says how.</summary>
    RunFinished,
}
