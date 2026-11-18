namespace Bit.Blow;

/// <summary>What to run and how to watch it.</summary>
public sealed class BlowRunRequest
{
    /// <summary>The run's id, given to every event and to <see cref="BlowNodeRunContext.IdempotencyKey"/>. A new one when null, or the checkpoint's when resuming.</summary>
    public string? RunId { get; init; }

    /// <summary>A value for the run as a whole - what a trigger received - read by handlers through <see cref="BlowNodeRunContext.RunInput"/>.</summary>
    public object? Input { get; init; }

    /// <summary>
    /// Which nodes take part. Null takes every node; group frames, notes and other nodes that do nothing are left out
    /// with a filter. Edges to or from a node left out are ignored.
    /// </summary>
    public Func<BlowNode, bool>? Include { get; init; }

    /// <summary>
    /// Runs only these nodes and what depends on them; the nodes before them are not run, and their outputs are taken
    /// from <see cref="Resume"/> when it has them. The start nodes run with whatever reached them - nothing, without a
    /// checkpoint. Null runs everything. Pair it with the previous run's checkpoint to
    /// run again from a node.
    /// </summary>
    public IReadOnlyCollection<string>? StartAt { get; init; }

    /// <summary>
    /// A previous run's checkpoint (<see cref="BlowRunResult.ToCheckpoint"/>): nodes that succeeded there are not run
    /// again, their outputs are used as they were. The run keeps the checkpoint's id unless <see cref="RunId"/> is set.
    /// </summary>
    public BlowRunCheckpoint? Resume { get; init; }

    /// <summary>Checked before the run starts; an error refuses it. Null checks only what the runner needs: no cycles, a handler per node.</summary>
    public BlowGraphValidator? Validator { get; init; }

    /// <summary>
    /// Called for every event, in order, on the context the run was started on (the renderer's, in a Blazor component).
    /// It must not throw: an exception stops the run and is thrown from <see cref="BlowRunner.RunAsync"/>.
    /// </summary>
    public Action<BlowRunEvent>? OnEvent { get; init; }

    /// <summary>Pauses and resumes the run while it is going (<see cref="BlowRunControl"/>).</summary>
    public BlowRunControl? Control { get; init; }

    /// <summary>Services for handlers, through <see cref="BlowNodeRunContext.Services"/>.</summary>
    public IServiceProvider? Services { get; init; }
}
