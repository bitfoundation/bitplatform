namespace Bit.Bcrdt;

/// <summary>What changed in a <see cref="BcrdtCounter"/> during one transaction.</summary>
public sealed class BcrdtCounterEvent(BcrdtCounter target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The counter that changed.</summary>
    public new BcrdtCounter Target => (BcrdtCounter)base.Target;

    /// <summary>
    /// How much the total moved in this transaction, from every replica whose change it carried.
    /// </summary>
    public long Change
    {
        get
        {
            long change = 0;
            foreach (BcrdtKeyChange key in Keys.Values)
            {
                long before = key.OldValue as long? ?? 0;
                long after = key.NewValue as long? ?? 0;
                change = unchecked(change + (after - before));
            }
            return change;
        }
    }
}
