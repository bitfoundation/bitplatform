namespace Bit.Bcrdt;

/// <summary>How a map key changed within a transaction.</summary>
public enum BcrdtKeyChangeAction
{
    /// <summary>The key did not exist before and does now.</summary>
    Add,
    /// <summary>The key existed before and has a different value now.</summary>
    Update,
    /// <summary>The key existed before and does not now.</summary>
    Delete,
}
