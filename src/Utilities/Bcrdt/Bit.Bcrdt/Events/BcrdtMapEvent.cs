namespace Bit.Bcrdt;

/// <summary>What changed in a <see cref="BcrdtMap"/> during one transaction.</summary>
public sealed class BcrdtMapEvent(BcrdtMap target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The map that changed.</summary>
    public new BcrdtMap Target => (BcrdtMap)base.Target;
}
