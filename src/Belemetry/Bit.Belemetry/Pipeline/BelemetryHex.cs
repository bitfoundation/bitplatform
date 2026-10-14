using System.Security.Cryptography;

namespace Bit.Belemetry;

/// <summary>Lowercase-hex helpers for trace and span ids.</summary>
internal static class BelemetryHex
{
    /// <summary>Decodes <paramref name="hex"/> into <paramref name="destination"/>; false when it is not valid hex of the right length.</summary>
    public static bool TryDecode(string? hex, Span<byte> destination)
    {
        if (hex is null || hex.Length != destination.Length * 2) return false;

        for (var i = 0; i < destination.Length; i++)
        {
            var hi = FromHexChar(hex[i * 2]);
            var lo = FromHexChar(hex[(i * 2) + 1]);
            if (hi < 0 || lo < 0) return false;
            destination[i] = (byte)((hi << 4) | lo);
        }

        return true;
    }

    /// <summary>Whether <paramref name="hex"/> is a valid, non-zero id of <paramref name="byteLength"/> bytes.</summary>
    public static bool IsValidId(string? hex, int byteLength)
    {
        if (hex is null || hex.Length != byteLength * 2) return false;

        var nonZero = false;
        foreach (var c in hex)
        {
            var v = FromHexChar(c);
            if (v < 0) return false;
            if (v != 0) nonZero = true;
        }

        return nonZero;
    }

    /// <summary>A random 16-byte trace id as 32 lowercase hex characters.</summary>
    public static string NewTraceId() => NewId(16);

    /// <summary>A random 8-byte span id as 16 lowercase hex characters.</summary>
    public static string NewSpanId() => NewId(8);

    private static string NewId(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int FromHexChar(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };
    }
}
