namespace Bit.Bcrdt;

/// <summary>What changed in a <see cref="BcrdtArray"/> during one transaction.</summary>
public sealed class BcrdtArrayEvent(BcrdtArray target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The array that changed.</summary>
    public new BcrdtArray Target => (BcrdtArray)base.Target;
}
