namespace Bit.Bheadless;

/// <summary>Information about a completed reorder.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
public sealed record BitBhReorderEventArgs<TItem>(TItem Item, int OldIndex, int NewIndex);
