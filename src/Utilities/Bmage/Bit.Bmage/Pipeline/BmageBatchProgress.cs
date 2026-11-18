namespace Bit.Bmage;

/// <summary>Progress update from a batch run.</summary>
/// <param name="Completed">Images finished so far.</param>
/// <param name="Total">Images in the batch, or -1 when they come from a stream whose length is not known.</param>
/// <param name="Last">The image that just finished.</param>
public readonly record struct BmageBatchProgress(int Completed, int Total, BmageBatchItemResult Last)
{
    /// <summary>The share of the batch finished, or 0 when the total is not known.</summary>
    public double Fraction => Total < 0 ? 0 : Total == 0 ? 1 : Completed / (double)Total;
}
