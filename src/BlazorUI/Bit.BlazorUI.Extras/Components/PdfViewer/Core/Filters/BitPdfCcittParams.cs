// C# implementation of the CCITTFaxDecode filter (ITU-T T.4 / T.6), following
// the standard specification. The Huffman code tables below are factual values
// from the ITU recommendations.

namespace Bit.BlazorUI;

/// <summary>Parameters from a CCITTFaxDecode <c>/DecodeParms</c> dictionary.</summary>
internal sealed class BitPdfCcittParams
{
    public int K { get; init; }
    public int Columns { get; init; } = 1728;
    public int Rows { get; init; }
    public bool BlackIs1 { get; init; }
    public bool EncodedByteAlign { get; init; }
    public bool EndOfBlock { get; init; } = true;
}
