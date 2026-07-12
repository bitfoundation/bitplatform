// Core PDF object primitives: names, commands, references and dictionaries.

using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// Singleton sentinel objects used throughout parsing.
/// </summary>
public static class BitPdfPrimitives
{
    /// <summary>The PDF <c>null</c> object (distinct from a missing key / CLR null).</summary>
    public static readonly object Null = new NullObject();

    /// <summary>End-of-file / end-of-stream marker emitted by the lexer.</summary>
    public static readonly object EOF = new EofObject();

    /// <summary>Marks a circular indirect reference encountered during fetch.</summary>
    public static readonly object CircularRef = new CircularRefObject();

    /// <summary>
    /// Reads a numeric value, transparently resolving an indirect reference
    /// through <paramref name="xref"/>. Numbers in PDF arrays (/Coords, /Domain,
    /// /C0, /Matrix, rectangles, /W…) may be given indirectly; use this instead
    /// of a bare <c>is double</c> check so those cases don't silently read 0.
    /// </summary>
    public static double ResolveNumber(IBitPdfXRef? xref, object? obj, double fallback = 0)
    {
        object? v = xref is not null ? xref.FetchIfRef(obj) : obj;
        return v is double d ? d : fallback;
    }

    public static bool IsName(object? obj) => obj is BitPdfName;
    public static bool IsName(object? obj, string value) => obj is BitPdfName n && n.Value == value;
    public static bool IsCmd(object? obj) => obj is BitPdfCmd;
    public static bool IsCmd(object? obj, string value) => obj is BitPdfCmd c && c.Value == value;
    public static bool IsDict(object? obj) => obj is BitPdfDict;
    public static bool IsDict(object? obj, string type)
        => obj is BitPdfDict d && IsName(d.Get("Type"), type);

    /// <summary>Clears the interned <see cref="BitPdfName"/> and <see cref="BitPdfCmd"/> caches.</summary>
    public static void ClearPrimitiveCaches()
    {
        BitPdfName.ClearCache();
        BitPdfCmd.ClearCache();
    }

    private sealed class NullObject { public override string ToString() => "null"; }
    private sealed class EofObject { public override string ToString() => "EOF"; }
    private sealed class CircularRefObject { public override string ToString() => "<circular>"; }
}
