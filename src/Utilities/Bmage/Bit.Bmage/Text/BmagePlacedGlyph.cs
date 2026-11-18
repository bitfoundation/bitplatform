namespace Bit.Bmage;

/// <summary>A glyph placed on a line: its font, and its origin in pixels from the line's start on the baseline (y down).</summary>
internal readonly record struct BmagePlacedGlyph(BmageFontFile Font, ushort Glyph, float X, float Y);
