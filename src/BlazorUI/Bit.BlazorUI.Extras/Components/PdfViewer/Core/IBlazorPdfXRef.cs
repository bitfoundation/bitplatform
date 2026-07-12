// Core PDF object primitives: names, commands, references and dictionaries.

using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// Resolves indirect references (<see cref="BitPdfRef"/>) into concrete PDF objects.
/// Implemented by the cross-reference table reader. Defined here so that
/// <see cref="BitPdfDict"/> can resolve references lazily.
/// </summary>
public interface IBitPdfXRef
{
    /// <summary>Fetch the object a reference points to.</summary>
    object? Fetch(BitPdfRef reference, bool suppressEncryption = false);

    /// <summary>Resolve <paramref name="value"/> if it is a <see cref="BitPdfRef"/>, otherwise return it unchanged.</summary>
    object? FetchIfRef(object? value, bool suppressEncryption = false);
}
