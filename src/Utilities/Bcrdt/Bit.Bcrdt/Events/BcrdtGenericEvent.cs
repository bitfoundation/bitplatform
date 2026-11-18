namespace Bit.Bcrdt;

/// <summary>
/// What changed in a type that has no more specific event, such as a root whose kind is not yet known.
/// </summary>
public sealed class BcrdtGenericEvent(BcrdtAbstractType target, BcrdtTransaction transaction)
    : BcrdtEvent(target, transaction);
