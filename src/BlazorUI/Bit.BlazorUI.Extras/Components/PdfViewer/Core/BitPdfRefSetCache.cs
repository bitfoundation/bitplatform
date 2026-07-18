// Core PDF object primitives: names, commands, references and dictionaries.

using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>A cache keyed by <see cref="BitPdfRef"/>.</summary>
public sealed class BitPdfRefSetCache<TValue>
{
    private readonly Dictionary<BitPdfRef, TValue> _map = new();

    public int Size => _map.Count;
    public bool Has(BitPdfRef reference) => _map.ContainsKey(reference);
    public TValue? Get(BitPdfRef reference) => _map.TryGetValue(reference, out var v) ? v : default;
    public void Put(BitPdfRef reference, TValue value) => _map[reference] = value;
    public void Clear() => _map.Clear();
}
