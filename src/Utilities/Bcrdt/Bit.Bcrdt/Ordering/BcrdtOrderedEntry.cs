namespace Bit.Bcrdt;

/// <summary>One item of a <see cref="BcrdtOrderedCollection"/>, as it sorts.</summary>
/// <param name="Id">The item's key in <see cref="BcrdtOrderedCollection.Items"/>.</param>
/// <param name="Item">The item itself.</param>
/// <param name="OrderKey">
/// The item's order key, or <see langword="null"/> when it has none or a malformed one - such an item
/// sorts after every item with a valid key.
/// </param>
public readonly record struct BcrdtOrderedEntry(string Id, BcrdtMap Item, string? OrderKey);
