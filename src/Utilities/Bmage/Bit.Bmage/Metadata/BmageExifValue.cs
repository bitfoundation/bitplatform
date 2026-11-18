using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage;

/// <summary>A single EXIF field with its raw little-endian value bytes.</summary>
public sealed class BmageExifValue
{
    public BmageExifValue(BmageExifType type, int count, ReadOnlyMemory<byte> data)
    {
        Type = type;
        Count = count;
        Data = data;
    }

    public BmageExifType Type { get; }

    /// <summary>Number of components (characters for ASCII).</summary>
    public int Count { get; }

    /// <summary>Raw value bytes normalised to little-endian byte order.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>Size in bytes of one component of the given type.</summary>
    public static int SizeOf(BmageExifType type) => type switch
    {
        BmageExifType.Byte or BmageExifType.Ascii or BmageExifType.SByte or BmageExifType.Undefined => 1,
        BmageExifType.Short or BmageExifType.SShort => 2,
        BmageExifType.Long or BmageExifType.SLong or BmageExifType.Float => 4,
        BmageExifType.Rational or BmageExifType.SRational or BmageExifType.Double => 8,
        _ => 1,
    };

    /// <summary>
    /// Creates a text field. EXIF names this type ASCII, but cameras and editors have written UTF-8 into these fields
    /// for years, so UTF-8 is what this library writes and reads. Restricting to ASCII would silently mangle a
    /// copyright symbol or a name with an accent, which is exactly the data a preserve policy exists to protect.
    /// </summary>
    public static BmageExifValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value + "\0");
        return new BmageExifValue(BmageExifType.Ascii, bytes.Length, bytes);
    }

    public static BmageExifValue FromShort(params ushort[] values)
    {
        var bytes = new byte[values.Length * 2];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), values[i]);
        return new BmageExifValue(BmageExifType.Short, values.Length, bytes);
    }

    public static BmageExifValue FromLong(params uint[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), values[i]);
        return new BmageExifValue(BmageExifType.Long, values.Length, bytes);
    }

    public static BmageExifValue FromRational(params (uint Numerator, uint Denominator)[] values)
    {
        var bytes = new byte[values.Length * 8];
        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 8), values[i].Numerator);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 8 + 4), values[i].Denominator);
        }
        return new BmageExifValue(BmageExifType.Rational, values.Length, bytes);
    }

    public static BmageExifValue FromUndefined(ReadOnlyMemory<byte> bytes) => new(BmageExifType.Undefined, bytes.Length, bytes);

    /// <summary>Reads the value as a string (ASCII fields; trailing NULs trimmed).</summary>
    public string? GetString()
    {
        if (Type is not (BmageExifType.Ascii or BmageExifType.Undefined or BmageExifType.Byte)) return null;
        var span = Data.Span;
        var end = span.Length;
        while (end > 0 && span[end - 1] == 0) end--;
        return Encoding.UTF8.GetString(span[..end]).Trim();
    }

    /// <summary>Reads the component at <paramref name="index"/> as a double (integers, rationals and floats).</summary>
    public double? GetDouble(int index = 0)
    {
        if (index < 0 || index >= Count) return null;
        var s = Data.Span;
        var sz = SizeOf(Type);
        if ((index + 1) * sz > s.Length) return null;
        var v = s.Slice(index * sz, sz);
        return Type switch
        {
            BmageExifType.Byte or BmageExifType.Undefined => v[0],
            BmageExifType.SByte => (sbyte)v[0],
            BmageExifType.Short => BinaryPrimitives.ReadUInt16LittleEndian(v),
            BmageExifType.SShort => BinaryPrimitives.ReadInt16LittleEndian(v),
            BmageExifType.Long => BinaryPrimitives.ReadUInt32LittleEndian(v),
            BmageExifType.SLong => BinaryPrimitives.ReadInt32LittleEndian(v),
            BmageExifType.Rational => Ratio(BinaryPrimitives.ReadUInt32LittleEndian(v), BinaryPrimitives.ReadUInt32LittleEndian(v[4..])),
            BmageExifType.SRational => Ratio(BinaryPrimitives.ReadInt32LittleEndian(v), BinaryPrimitives.ReadInt32LittleEndian(v[4..])),
            BmageExifType.Float => BinaryPrimitives.ReadSingleLittleEndian(v),
            BmageExifType.Double => BinaryPrimitives.ReadDoubleLittleEndian(v),
            _ => null,
        };

        static double? Ratio(double n, double d) => d == 0 ? (n == 0 ? 0 : null) : n / d;
    }

    /// <summary>Reads the first component as an integer.</summary>
    public int? GetInt32(int index = 0) => GetDouble(index) is { } d && !double.IsNaN(d) ? (int)d : null;

    public override string ToString() => Type == BmageExifType.Ascii ? GetString() ?? "" : Count == 1 ? GetDouble()?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "" : $"{Type}[{Count}]";
}
