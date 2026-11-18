namespace Bit.Bcrdt;

/// <summary>A single key's change within a transaction.</summary>
/// <param name="Action">Whether the key was added, updated or removed.</param>
/// <param name="OldValue">The value before the transaction, if there was one.</param>
/// <param name="NewValue">The value after the transaction, if there is one.</param>
public readonly record struct BcrdtKeyChange(BcrdtKeyChangeAction Action, object? OldValue, object? NewValue);
