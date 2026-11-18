using System.Buffers;

namespace Bit.Bmage;

/// <summary>Alpha compositing helpers operating on straight-alpha RGBA.</summary>
public static partial class BmageCompositor
{
    /// <summary>Composites <paramref name="source"/> over <paramref name="destination"/> with an extra coverage/alpha factor.</summary>
    public static BmageRgba32 SourceOver(BmageRgba32 destination, BmageRgba32 source, float alpha)
    {
        var sa = source.A / 255f * Math.Clamp(alpha, 0f, 1f);
        if (sa <= 0f) return destination;
        var da = destination.A / 255f;
        var outA = sa + da * (1 - sa);
        if (outA <= 0f) return BmageRgba32.Transparent;
        var inv = 1f / outA;
        return new BmageRgba32(
            BmageRgba32.ClampToByte((source.R * sa + destination.R * da * (1 - sa)) * inv),
            BmageRgba32.ClampToByte((source.G * sa + destination.G * da * (1 - sa)) * inv),
            BmageRgba32.ClampToByte((source.B * sa + destination.B * da * (1 - sa)) * inv),
            BmageRgba32.ClampToByte(outA * 255f));
    }

    /// <summary>
    /// <see cref="SourceOver"/> with a fixed colour and factor, exactly as that computes it, for the fully covered
    /// pixels of a fill.
    /// </summary>
    internal sealed class SolidFill : IDisposable
    {
        private readonly BmageRgba32 _color;
        private readonly float _alpha;
        private readonly bool _replaces;
        // The red, green and blue tables one after another, rented from the pool.
        private byte[]? _tables;
        private byte _opaqueAlpha;

        private SolidFill(BmageRgba32 color, float alpha)
        {
            _color = color;
            _alpha = alpha;
            // SourceOver of an opaque colour at full strength is exactly the colour: its own weight is 1 and the
            // destination's 0, and each channel converts back to the same byte.
            _replaces = color.A == 255 && Math.Clamp(alpha, 0f, 1f) == 1f;
        }

        public static SolidFill Create(BmageRgba32 color, float alpha) => new(color, alpha);

        public BmageRgba32 Apply(BmageRgba32 destination)
        {
            if (_replaces) return _color;
            if (destination.A != 255) return SourceOver(destination, _color, _alpha);
            var tables = _tables ?? BuildOpaqueTables();
            return new BmageRgba32(tables[destination.R], tables[256 + destination.G], tables[512 + destination.B], _opaqueAlpha);
        }

        public void Dispose()
        {
            if (_tables is not null) ArrayPool<byte>.Shared.Return(_tables);
            _tables = null;
        }

        /// <summary>SourceOver over every opaque destination value, one channel at a time.</summary>
        private byte[] BuildOpaqueTables()
        {
            var tables = ArrayPool<byte>.Shared.Rent(3 * 256);
            for (var v = 0; v < 256; v++)
            {
                var result = SourceOver(new BmageRgba32((byte)v, (byte)v, (byte)v, 255), _color, _alpha);
                tables[v] = result.R; tables[256 + v] = result.G; tables[512 + v] = result.B;
                _opaqueAlpha = result.A;
            }
            return _tables = tables;
        }
    }

    /// <summary>Erases from the destination: reduces its alpha by the coverage.</summary>
    public static BmageRgba32 Erase(BmageRgba32 destination, float alpha)
    {
        var a = Math.Clamp(alpha, 0f, 1f);
        if (a <= 0f) return destination;
        return destination.WithAlpha(BmageRgba32.ClampToByte(destination.A * (1 - a)));
    }
}
