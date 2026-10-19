using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>
/// A 32-bit pixel with 8-bit red, green, blue and alpha channels stored in memory order R, G, B, A
/// (the layout used by the browser <c>ImageData</c> API). Alpha is straight (not premultiplied).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[DebuggerDisplay("R={R} G={G} B={B} A={A}")]
public readonly struct Rgba32 : IEquatable<Rgba32>
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public Rgba32(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    public static readonly Rgba32 Transparent = new(0, 0, 0, 0);
    public static readonly Rgba32 Black = new(0, 0, 0);
    public static readonly Rgba32 White = new(255, 255, 255);

    /// <summary>The pixel packed as 0xAABBGGRR (little-endian memory order R,G,B,A).</summary>
    public uint PackedValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)(R | (G << 8) | (B << 16) | (A << 24));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rgba32 FromPacked(uint value) => new((byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24));

    public static Rgba32 FromColor(Color color) => new(color.R, color.G, color.B, color.A);

    public Color ToColor() => Color.FromArgb(A, R, G, B);

    /// <summary>Creates a pixel from floating point channel values in the range [0,1]. Values are clamped.</summary>
    public static Rgba32 FromScaled(float r, float g, float b, float a = 1f)
        => new(ClampToByte(r * 255f), ClampToByte(g * 255f), ClampToByte(b * 255f), ClampToByte(a * 255f));

    /// <summary>Parses a CSS style hex colour: #RGB, #RGBA, #RRGGBB or #RRGGBBAA (leading '#' optional).</summary>
    public static Rgba32 ParseHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var s = hex.AsSpan().Trim();
        if (s.Length > 0 && s[0] == '#') s = s[1..];
        return s.Length switch
        {
            3 => new((byte)(H(s[0]) * 17), (byte)(H(s[1]) * 17), (byte)(H(s[2]) * 17)),
            4 => new((byte)(H(s[0]) * 17), (byte)(H(s[1]) * 17), (byte)(H(s[2]) * 17), (byte)(H(s[3]) * 17)),
            6 => new(H2(s, 0), H2(s, 2), H2(s, 4)),
            8 => new(H2(s, 0), H2(s, 2), H2(s, 4), H2(s, 6)),
            _ => throw new FormatException($"'{hex}' is not a valid hex colour."),
        };

        static byte H(char c) => (byte)(c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : c >= 'A' && c <= 'F' ? c - 'A' + 10 : throw new FormatException("Invalid hex digit."));
        static byte H2(ReadOnlySpan<char> s, int i) => (byte)((H(s[i]) << 4) | H(s[i + 1]));
    }

    /// <summary>Tries to parse a hex colour; returns false instead of throwing on malformed input.</summary>
    public static bool TryParseHex(string? hex, out Rgba32 value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        try { value = ParseHex(hex); return true; }
        catch (FormatException) { return false; }
    }

    public string ToHex(bool includeAlpha = true) => includeAlpha ? $"#{R:x2}{G:x2}{B:x2}{A:x2}" : $"#{R:x2}{G:x2}{B:x2}";

    /// <summary>CSS colour string, e.g. <c>rgba(255,0,0,0.5)</c>.</summary>
    public string ToCss() => A == 255 ? $"#{R:x2}{G:x2}{B:x2}" : $"rgba({R},{G},{B},{A / 255f:0.###})";

    public Rgba32 WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>Relative luminance (Rec. 601 weights) in the range [0,255].</summary>
    public byte Luminance => (byte)((R * 299 + G * 587 + B * 114 + 500) / 1000);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ClampToByte(float value)
    {
        // Rounds to nearest and clamps to [0,255]. NaN becomes 0.
        value += 0.5f;
        if (!(value > 0f)) return 0;
        if (value >= 255f) return 255;
        return (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ClampToByte(int value) => (byte)(value < 0 ? 0 : value > 255 ? 255 : value);

    public bool Equals(Rgba32 other) => PackedValue == other.PackedValue;
    public override bool Equals(object? obj) => obj is Rgba32 o && Equals(o);
    public override int GetHashCode() => (int)PackedValue;
    public static bool operator ==(Rgba32 left, Rgba32 right) => left.Equals(right);
    public static bool operator !=(Rgba32 left, Rgba32 right) => !left.Equals(right);
    public override string ToString() => ToHex();

    public static implicit operator Rgba32(Color color) => FromColor(color);
    public static implicit operator Color(Rgba32 pixel) => pixel.ToColor();
}
