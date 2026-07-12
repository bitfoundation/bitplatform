// Core PDF object primitives: names, commands, references and dictionaries.

using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>A set of <see cref="BitPdfRef"/>s, used for cycle detection while walking the object graph.</summary>
public sealed class BitPdfRefSet
{
    private readonly HashSet<BitPdfRef> _set;

    public BitPdfRefSet(BitPdfRefSet? parent = null)
        => _set = parent is null ? new HashSet<BitPdfRef>() : new HashSet<BitPdfRef>(parent._set);

    public bool Has(BitPdfRef reference) => _set.Contains(reference);
    public void Put(BitPdfRef reference) => _set.Add(reference);
    public void Remove(BitPdfRef reference) => _set.Remove(reference);
}
