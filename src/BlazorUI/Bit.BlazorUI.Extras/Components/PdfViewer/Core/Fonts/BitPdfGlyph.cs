namespace Bit.BlazorUI;

/// <summary>
/// A decoded glyph from a show-text operand: its character code, the Unicode
/// text it maps to, its advance width in glyph-space (1/1000 em), and whether
/// it represents a single-byte space (for word-spacing).
/// </summary>
public readonly struct BitPdfGlyph
{
    public int Code { get; }
    public string Unicode { get; }
    public double Width1000 { get; }
    public bool IsSpace { get; }

    public BitPdfGlyph(int code, string unicode, double width1000, bool isSpace)
    {
        Code = code;
        Unicode = unicode;
        Width1000 = width1000;
        IsSpace = isSpace;
    }
}
