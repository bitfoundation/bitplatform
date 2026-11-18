namespace Bit.Bcrdt;

/// <summary>A position resolved against the document as it stands right now.</summary>
/// <param name="Type">The sequence the position lies in.</param>
/// <param name="Index">The character or element index.</param>
/// <param name="Association">Which side of the position new text attaches to.</param>
public readonly record struct BcrdtAbsolutePosition(BcrdtAbstractType Type, int Index, int Association);
