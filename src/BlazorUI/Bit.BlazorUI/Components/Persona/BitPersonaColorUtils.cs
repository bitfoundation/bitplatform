namespace Bit.BlazorUI;

public static class BitPersonaColorUtils
{
    static readonly BitPersonaInitialsColor[] _colorSwatchesLookup = new BitPersonaInitialsColor[]
    {
        BitPersonaInitialsColor.LightBlue,
        BitPersonaInitialsColor.Blue,
        BitPersonaInitialsColor.DarkBlue,
        BitPersonaInitialsColor.Teal,
        BitPersonaInitialsColor.Green,
        BitPersonaInitialsColor.DarkGreen,
        BitPersonaInitialsColor.LightPink,
        BitPersonaInitialsColor.Pink,
        BitPersonaInitialsColor.Magenta,
        BitPersonaInitialsColor.Purple,
        BitPersonaInitialsColor.Orange,
        BitPersonaInitialsColor.LightRed,
        BitPersonaInitialsColor.DarkRed,
        BitPersonaInitialsColor.Violet,
        BitPersonaInitialsColor.Gold,
        BitPersonaInitialsColor.Burgundy,
        BitPersonaInitialsColor.WarmGray,
        BitPersonaInitialsColor.Cyan,
        BitPersonaInitialsColor.Rust,
        BitPersonaInitialsColor.CoolGray
    };
    public static BitPersonaInitialsColor GetInitialsColorFromName(string? displayName)
    {
        BitPersonaInitialsColor color = BitPersonaInitialsColor.Blue;
        if (string.IsNullOrWhiteSpace(displayName))
            return color;

        int hashCode = 0;
        for (int iLen = displayName.Length - 1; iLen >= 0; iLen--)
        {
            char ch = displayName[iLen];
            int shift = iLen % 8;
            hashCode ^= (ch << shift) + (ch >> (8 - shift));
        }

        color = _colorSwatchesLookup[hashCode % _colorSwatchesLookup.Length];

        return color;
    }

    public static string GetPersonaColorHexCode(BitPersonaInitialsColor personaInitialsColor)
    {
        return personaInitialsColor switch
        {
            BitPersonaInitialsColor.LightBlue => "#4F6BED",
            BitPersonaInitialsColor.Blue => "#0078D4",
            BitPersonaInitialsColor.DarkBlue => "#004E8C",
            BitPersonaInitialsColor.Teal => "#038387",
            BitPersonaInitialsColor.LightGreen or BitPersonaInitialsColor.Green => "#498205",
            BitPersonaInitialsColor.DarkGreen => "#0B6A0B",
            BitPersonaInitialsColor.LightPink => "#C239B3",
            BitPersonaInitialsColor.Pink => "#E3008C",
            BitPersonaInitialsColor.Magenta => "#881798",
            BitPersonaInitialsColor.Purple => "#5C2E91",
            BitPersonaInitialsColor.Orange => "#CA5010",
            BitPersonaInitialsColor.Red => "#EE1111",
            BitPersonaInitialsColor.LightRed => "#D13438",
            BitPersonaInitialsColor.DarkRed => "#A4262C",
            BitPersonaInitialsColor.Transparent => "transparent",
            BitPersonaInitialsColor.Violet => "#8764B8",
            BitPersonaInitialsColor.Gold => "#986F0B",
            BitPersonaInitialsColor.Burgundy => "#750B1C",
            BitPersonaInitialsColor.WarmGray => "#7A7574",
            BitPersonaInitialsColor.Cyan => "#005B70",
            BitPersonaInitialsColor.Rust => "#8E562E",
            BitPersonaInitialsColor.CoolGray => "#69797E",
            BitPersonaInitialsColor.Black => "#1D1D1D",
            BitPersonaInitialsColor.Gray => "#393939",
            _ => "#0078D4",
        };
    }
}
