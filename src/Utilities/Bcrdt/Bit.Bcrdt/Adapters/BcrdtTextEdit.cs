namespace Bit.Bcrdt;

/// <summary>Describes the single edit that turns one string into another.</summary>
/// <param name="Index">Where the change starts.</param>
/// <param name="DeletedLength">How many characters were removed.</param>
/// <param name="Inserted">What was inserted in their place.</param>
public readonly record struct BcrdtTextEdit(int Index, int DeletedLength, string Inserted)
{
    /// <summary>Whether the two strings were already equal.</summary>
    public bool IsEmpty => DeletedLength == 0 && Inserted.Length == 0;
}
