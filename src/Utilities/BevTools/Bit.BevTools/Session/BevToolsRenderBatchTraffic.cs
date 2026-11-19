namespace Bit.BevTools;

/// <summary>Render batches a circuit sent to the browser, and how long the browser took to apply them.</summary>
public sealed class BevToolsRenderBatchTraffic
{
    public int Count { get; set; }

    public long Bytes { get; set; }

    public long MaxBytes { get; set; }

    /// <summary>From a batch arriving to the browser acknowledging it: the DOM work on the browser's side, summed.</summary>
    public double ApplyMs { get; set; }

    public double MaxApplyMs { get; set; }

    /// <summary>Batches received and not acknowledged yet.</summary>
    public int Pending { get; set; }

    /// <summary>
    /// Batches that changed nothing but BevTools' own panel, told apart in the browser by where their DOM changes landed.
    /// They are not in the counts above; their bytes are in <see cref="BevToolsSocketStats.Own"/>.
    /// </summary>
    public int BevTools { get; set; }
}
