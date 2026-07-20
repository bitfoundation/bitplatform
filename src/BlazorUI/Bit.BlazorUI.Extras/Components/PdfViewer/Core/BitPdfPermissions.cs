namespace Bit.BlazorUI;

/// <summary>
/// User access permissions derived from an encrypted document's <c>/P</c> value
/// (PDF 32000-1 Table 22). For an unencrypted document every permission is
/// granted. Bit numbers are 1-based as in the specification.
/// </summary>
public readonly struct BitPdfPermissions
{
    private readonly int _p;
    private readonly bool _encrypted;

    /// <summary>Creates a permission set from the raw <c>/P</c> integer.</summary>
    public BitPdfPermissions(int p, bool encrypted)
    {
        _p = p;
        _encrypted = encrypted;
    }

    /// <summary>The raw <c>/P</c> bit field (all bits set when unencrypted).</summary>
    public int RawValue => _encrypted ? _p : -1;

    // An unencrypted document grants everything; otherwise consult the bit.
    private bool Bit(int oneBased) => !_encrypted || (_p & (1 << (oneBased - 1))) != 0;

    /// <summary>Print the document (bit 3).</summary>
    public bool CanPrint => Bit(3);

    /// <summary>Modify the contents (bit 4).</summary>
    public bool CanModify => Bit(4);

    /// <summary>Copy or extract text and graphics (bit 5).</summary>
    public bool CanCopy => Bit(5);

    /// <summary>Add or modify annotations and fill form fields (bit 6).</summary>
    public bool CanAnnotate => Bit(6);

    /// <summary>Fill existing interactive form fields (bit 9).</summary>
    public bool CanFillForms => Bit(9);

    /// <summary>Extract text and graphics for accessibility (bit 10).</summary>
    public bool CanExtractForAccessibility => Bit(10);

    /// <summary>Assemble the document: insert, rotate, delete pages (bit 11).</summary>
    public bool CanAssemble => Bit(11);

    /// <summary>Print at high resolution (bit 12).</summary>
    public bool CanPrintHighQuality => Bit(12);
}
