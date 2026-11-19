namespace Bit.Bmage;

/// <summary>A shaped line: its glyphs from left to right, its width, and how far its fonts reach above and below the baseline.</summary>
internal sealed record BmageShapedLine(BmagePlacedGlyph[] Glyphs, float Width, float Ascent, float Descent);
