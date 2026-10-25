using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;

namespace Bit.Bmage;

/// <summary>
/// Converts rows between 8-bit pixels and the [0,1] <see cref="Vector4"/> form every mixing operation works in.
/// </summary>
/// <remarks>
/// <para>
/// These loops run once per pixel in almost every operation, and on WebAssembly's interpreter the conversions cost far
/// more than the arithmetic between them: four <c>(byte)</c> casts per pixel take three times as long as converting to
/// <see cref="int"/> and packing the channels into one <see cref="uint"/>, WebAssembly's own saturating conversion and
/// byte swizzle are eight times faster, and widening the bytes with <see cref="Vector128"/> is three times faster than
/// four integer to float conversions. AOT compiled WebAssembly gains about four times on stores as well.
/// </para>
/// <para>
/// Every method produces exactly the values of the per-channel expressions in its summary, so switching an operation to
/// them never changes a pixel.
/// </para>
/// </remarks>
internal static class PixelRows
{
    public const float Inv255 = 1f / 255f;

    // Lane-wise SIMD and the uint view of a pixel both assume R is the lowest byte of the packed value.
    private static bool UseVectors => BitConverter.IsLittleEndian && Vector128.IsHardwareAccelerated;

    /// <summary><c>new Vector4(p.R, p.G, p.B, p.A) * (1f / 255f)</c> for every pixel.</summary>
    public static void Load(ReadOnlySpan<Rgba32> source, Span<Vector4> destination)
    {
        var length = Math.Min(source.Length, destination.Length);
        if (UseVectors)
        {
            var packed = MemoryMarshal.Cast<Rgba32, uint>(source);
            var scale = Vector128.Create(Inv255);
            ref var d = ref Unsafe.As<Vector4, Vector128<float>>(ref MemoryMarshal.GetReference(destination));
            for (var i = 0; i < length; i++)
                Unsafe.Add(ref d, i) = Widen(packed[i]) * scale;
            return;
        }
        for (var i = 0; i < length; i++)
        {
            var p = source[i];
            destination[i] = new Vector4(p.R, p.G, p.B, p.A) * Inv255;
        }
    }

    /// <summary>
    /// Premultiplied alpha: with <c>a = p.A * (1f / 255f)</c>, <c>new Vector4(p.R * (1f / 255f) * a, p.G * (1f / 255f) * a,
    /// p.B * (1f / 255f) * a, a)</c> for every pixel.
    /// </summary>
    public static void LoadPremultiplied(ReadOnlySpan<Rgba32> source, Span<Vector4> destination)
    {
        var length = Math.Min(source.Length, destination.Length);
        if (UseVectors)
        {
            var packed = MemoryMarshal.Cast<Rgba32, uint>(source);
            var scale = Vector128.Create(Inv255);
            ref var d = ref MemoryMarshal.GetReference(destination);
            for (var i = 0; i < length; i++)
            {
                var v = (Widen(packed[i]) * scale).AsVector4();
                var a = v.W;
                // Multiplying alpha by exactly 1 leaves it as it is.
                Unsafe.Add(ref d, i) = v * new Vector4(a, a, a, 1f);
            }
            return;
        }
        for (var i = 0; i < length; i++)
        {
            var p = source[i];
            var a = p.A * Inv255;
            destination[i] = new Vector4(p.R * Inv255 * a, p.G * Inv255 * a, p.B * Inv255 * a, a);
        }
    }

    /// <summary>
    /// With <c>v = Vector4.Clamp(p, Vector4.Zero, Vector4.One) * 255f + new Vector4(0.5f)</c>,
    /// <c>new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W)</c> for every value.
    /// </summary>
    public static void Store(ReadOnlySpan<Vector4> source, Span<Rgba32> destination)
    {
        var length = Math.Min(source.Length, destination.Length);
        var scale = new Vector4(255f);
        var half = new Vector4(0.5f);
        if (BitConverter.IsLittleEndian)
        {
            var packed = MemoryMarshal.Cast<Rgba32, uint>(destination);
            if (PackedSimd.IsSupported)
            {
                for (var i = 0; i < length; i++)
                    packed[i] = PackBrowser(Vector4.Clamp(source[i], Vector4.Zero, Vector4.One) * scale + half);
                return;
            }
            for (var i = 0; i < length; i++)
                packed[i] = Pack(Vector4.Clamp(source[i], Vector4.Zero, Vector4.One) * scale + half);
            return;
        }
        for (var i = 0; i < length; i++)
        {
            var v = Vector4.Clamp(source[i], Vector4.Zero, Vector4.One) * scale + half;
            destination[i] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
        }
    }

    /// <summary>
    /// Undoes premultiplication and stores: a value whose alpha is above 1e-5 has its colour divided by alpha (through
    /// <c>1f / a</c>), anything more transparent becomes transparent black, and the result is stored as by
    /// <see cref="Store"/>.
    /// </summary>
    public static void StoreUnpremultiplied(ReadOnlySpan<Vector4> source, Span<Rgba32> destination)
    {
        var length = Math.Min(source.Length, destination.Length);
        var scale = new Vector4(255f);
        var half = new Vector4(0.5f);
        if (BitConverter.IsLittleEndian)
        {
            var packed = MemoryMarshal.Cast<Rgba32, uint>(destination);
            if (PackedSimd.IsSupported)
            {
                for (var i = 0; i < length; i++)
                    packed[i] = PackBrowser(Vector4.Clamp(Unpremultiply(source[i]), Vector4.Zero, Vector4.One) * scale + half);
                return;
            }
            for (var i = 0; i < length; i++)
                packed[i] = Pack(Vector4.Clamp(Unpremultiply(source[i]), Vector4.Zero, Vector4.One) * scale + half);
            return;
        }
        for (var i = 0; i < length; i++)
        {
            var v = Vector4.Clamp(Unpremultiply(source[i]), Vector4.Zero, Vector4.One) * scale + half;
            destination[i] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Unpremultiply(Vector4 p)
    {
        var a = p.W;
        if (!(a > 1e-5f)) return Vector4.Zero;
        var invA = 1f / a;
        return new Vector4(p.X * invA, p.Y * invA, p.Z * invA, a);
    }

    /// <summary>
    /// Packs channels already scaled to [0.5, 255.5] the way <c>(byte)</c> casts would: truncation to an integer that
    /// fits a byte, so converting to <see cref="int"/> first changes nothing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack(Vector4 v)
        => (uint)(int)v.X | (uint)(int)v.Y << 8 | (uint)(int)v.Z << 16 | (uint)(int)v.W << 24;

    /// <summary>
    /// <see cref="Pack"/> through WebAssembly SIMD: a saturating truncation, which equals the casts' truncation for
    /// channels in [0.5, 255.5], then a swizzle gathering the low byte of each lane. Only called when
    /// <see cref="PackedSimd.IsSupported"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint PackBrowser(Vector4 v)
        => PackedSimd.Swizzle(PackedSimd.ConvertToInt32Saturate(v.AsVector128()).AsByte(), LowBytes).AsUInt32().ToScalar();

    private static readonly Vector128<byte> LowBytes = Vector128.Create((byte)0, 4, 8, 12, 0, 4, 8, 12, 0, 4, 8, 12, 0, 4, 8, 12);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> Widen(uint packed)
        => Vector128.ConvertToSingle(Vector128.WidenLower(Vector128.WidenLower(Vector128.CreateScalarUnsafe(packed).AsByte())).AsInt32());
}
