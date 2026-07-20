// AcroForm field extraction. Exposes the interactive form fields (/AcroForm
// /Fields) as a flat list of name/type/value so a consumer can build form UI or
// read submitted data. This does not yet render widgets as live inputs.

namespace Bit.BlazorUI;

/// <summary>A single interactive form field.</summary>
public sealed class BitPdfFormField
{
    /// <summary>The fully-qualified field name (parent names joined with '.').</summary>
    public required string Name { get; init; }

    /// <summary>The field type: "Tx" (text), "Btn" (button/checkbox), "Ch"
    /// (choice), "Sig" (signature), or "" when unspecified.</summary>
    public required string Type { get; init; }

    /// <summary>The current field value (<c>/V</c>) as text, when present.</summary>
    public string? Value { get; init; }
}
