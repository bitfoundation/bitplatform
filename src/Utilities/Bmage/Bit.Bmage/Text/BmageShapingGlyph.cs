namespace Bit.Bmage;

/// <summary>One glyph while text is shaped: what it came from, what applies to it, and where it goes.</summary>
internal struct BmageShapingGlyph
{
    public ushort Glyph;
    /// <summary>The index of the first code point the glyph stands for.</summary>
    public int Cluster;
    /// <summary>The features that apply to the glyph, one bit each; bit 0 is every global feature.</summary>
    public uint Mask;
    /// <summary>GDEF glyph class: 0 unknown, 1 base, 2 ligature, 3 mark, 4 component.</summary>
    public byte GlyphClass;
    public byte MarkAttachClass;
    /// <summary>For a ligature, and for a mark that sat between its components: the ligature it belongs to.</summary>
    public int LigatureId;
    /// <summary>For a mark: the component of <see cref="LigatureId"/> it followed, from 1; 0 for the last one.</summary>
    public byte LigatureComponent;
    /// <summary>For a ligature: how many characters it joins.</summary>
    public byte LigatureComponents;
    /// <summary>A default ignorable code point (ZWNJ, ZWJ, a bidi mark, a soft hyphen): neither drawn nor given any width.</summary>
    public bool Hidden;

    // Positions, in font units.
    public int XAdvance;
    public int XOffset;
    public int YOffset;
    /// <summary>The glyph a mark is attached to, or -1.</summary>
    public int AttachTo;
    public int AttachDx;
    public int AttachDy;
}
