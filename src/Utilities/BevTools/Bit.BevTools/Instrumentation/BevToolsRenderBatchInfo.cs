namespace Bit.BevTools;

/// <summary>Bookkeeping for one render batch (one pass of the renderer's queue, ending in one diff sent to the browser).</summary>
public sealed class BevToolsRenderBatchInfo
{
    /// <summary>The number shown as "Render batch #N"; 0 for a batch that is only bookkept (see <see cref="IsTracked"/>).</summary>
    public long Id { get; internal set; }

    /// <summary>
    /// False for a batch kept only so diff measurements stay aligned with the renderer's batches: one in which only
    /// BevTools' own components rendered, or that ran while capture was paused. It has no timeline row; it absorbs
    /// its own measurement, which would otherwise be attached to an application batch.
    /// </summary>
    public bool IsTracked { get; internal set; }

    /// <summary>Every render seen in this batch, BevTools' own and paused ones included; compared with the renderer's diff count to detect a new pass.</summary>
    internal int ObservedRenders { get; set; }

    /// <summary>Position among every batch the session has seen, bookkept ones included, so "the next batch" can be named before it starts.</summary>
    internal long Sequence { get; init; }

    public long EventId { get; set; }

    public long StartTicks { get; init; }

    public int Count { get; set; }

    public double TotalMs { get; set; }

    public double? DiffMs { get; set; }

    public int? DiffSize { get; set; }

    /// <summary>Instance ids rendered in this batch; used to attribute a render to an ancestor that rendered first.</summary>
    internal HashSet<long> RenderedInstanceIds { get; } = [];

    public long? TriggerEventId { get; set; }

    /// <summary>True once no further renders may be attached (the renderer moved on, or the diff was measured).</summary>
    public bool Closed { get; set; }
}
