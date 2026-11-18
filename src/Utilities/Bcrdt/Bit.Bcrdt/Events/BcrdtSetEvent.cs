namespace Bit.Bcrdt;

/// <summary>What changed in a <see cref="BcrdtSet"/> during one transaction.</summary>
public sealed class BcrdtSetEvent(BcrdtSet target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The set that changed.</summary>
    public new BcrdtSet Target => (BcrdtSet)base.Target;

    /// <summary>The elements added by the transaction.</summary>
    public IEnumerable<object?> Added => Keys
        .Where(p => p.Value.Action is BcrdtKeyChangeAction.Add)
        .Select(p => p.Value.NewValue);

    /// <summary>The elements removed by the transaction.</summary>
    public IEnumerable<object?> Removed => Keys
        .Where(p => p.Value.Action is BcrdtKeyChangeAction.Delete)
        .Select(p => p.Value.OldValue);
}
