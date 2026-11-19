using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>Executes a <see cref="BmagePointOperation"/> in a single pass over an image.</summary>
/// <remarks>
/// <para>
/// When every stage maps each channel from that channel alone (brightness, contrast, exposure, temperature, tint,
/// invert, opacity, gamma, levels, posterize, curves), the whole operation is a function of one byte per channel. It
/// is then run once over the 256 possible values, through the very same code, and the image is mapped through the
/// resulting tables: the pixels are identical and the float work disappears.
/// </para>
/// <para>
/// When only the first stages of a fused run qualify, their result for each input byte is tabulated the same way and
/// replaces loading the pixel and running those stages. Trailing curves are tabulated too: a curve starts by
/// quantising its input exactly as storing a pixel does, so storing the row and mapping the bytes through their
/// tables gives the pixels running them would. Only the stages in between run in floating point.
/// </para>
/// </remarks>
internal static class BmagePointOperationExecutor
{
    public static BmageImageBuffer Run(BmageImageBuffer source, BmageOperationContext context, BmagePointOperation operation)
    {
        ArgumentNullException.ThrowIfNull(source);
        var output = context.CanMutateSource ? source : context.Allocate(source.Width, source.Height, clear: false);
        if (!ReferenceEquals(output, source)) output.Metadata = source.Metadata;
        try
        {
            using var processor = new RowProcessor(operation);
            var width = source.Width;
            var height = source.Height;
            // The tables are only read once built, so every band shares them; each band has its own row. A table
            // lookup is cheap, so a band of those needs four times the rows to be worth handing out.
            BmageParallelRows.For(processor.Context(context), 0, height, processor.UsesTablesOnly ? width / 4 : width, (start, end, band) =>
            {
                var rowArray = processor.UsesTablesOnly ? null : ArrayPool<Vector4>.Shared.Rent(width);
                try
                {
                    var row = rowArray.AsSpan(0, rowArray is null ? 0 : width);
                    var check = processor.UsesTablesOnly ? 63 : 15;
                    for (var y = start; y < end; y++)
                    {
                        if (((y - start) & check) == 0)
                        {
                            band.ThrowIfCancellationRequested();
                            band.ReportProgress(y / (double)height);
                        }
                        processor.Process(source.GetRow(y), output.GetRow(y), row);
                    }
                }
                finally
                {
                    if (rowArray is not null) ArrayPool<Vector4>.Shared.Return(rowArray);
                }
            });
            context.ReportProgress(1);
            return output;
        }
        catch
        {
            if (!ReferenceEquals(output, source)) output.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Applies a point operation to one row at a time, producing exactly the pixels <see cref="Run"/> does for that row.
    /// Built once per run, with whatever tables the operation allows; safe to use from several threads at once when the
    /// operation <see cref="BmagePointOperation.CanProcessRowsConcurrently">can process rows concurrently</see>, each with its
    /// own scratch row.
    /// </summary>
    internal sealed class RowProcessor : IDisposable
    {
        private readonly BmagePointOperation _operation;
        private readonly BmageFusedPointOperation? _fused;
        private readonly int _prefix;
        private readonly int _suffix;
        private readonly ByteTables? _tables;
        private readonly float[][]? _prefixTables;
        private readonly ByteTables? _suffixTables;

        public RowProcessor(BmagePointOperation operation)
        {
            _operation = operation;
            if (operation.IsChannelSeparable)
            {
                _tables = Tabulate(operation, 0);
                return;
            }
            _fused = operation as BmageFusedPointOperation;
            _prefix = _fused?.SeparablePrefixLength ?? 0;
            // Only reached with at least one stage that mixes channels, so the closing curves never overlap the prefix.
            _suffix = _fused?.CurveSuffixStart ?? 1;
            if (_prefix > 0) _prefixTables = BuildPrefixTables(_fused!, _prefix);
            if (_fused is not null && _suffix < _fused.Stages.Count) _suffixTables = Tabulate(_fused, _suffix);
        }

        /// <summary>True when every channel goes through a table, so <see cref="Process"/> needs no scratch row.</summary>
        public bool UsesTablesOnly => _tables is not null;

        /// <summary>The context to run under: one thread unless the operation allows several.</summary>
        public BmageOperationContext Context(BmageOperationContext context)
            => UsesTablesOnly || _operation.CanProcessRowsConcurrently ? context : context.WithParallelism(1);

        /// <summary>
        /// Maps <paramref name="source"/> into <paramref name="target"/>, which may be the same row. <paramref name="scratch"/>
        /// must hold at least a row's worth of values unless <see cref="UsesTablesOnly"/>.
        /// </summary>
        public void Process(ReadOnlySpan<BmageRgba32> source, Span<BmageRgba32> target, Span<Vector4> scratch)
        {
            if (_tables is not null)
            {
                MapThroughTables(source, target, _tables);
                return;
            }
            var row = scratch[..source.Length];
            if (_fused is null)
            {
                BmagePixelRows.Load(source, row);
                _operation.ProcessRow(row);
                BmagePixelRows.Store(row, target);
                return;
            }
            if (_prefixTables is null) BmagePixelRows.Load(source, row);
            else LoadThroughTables(source, row, _prefixTables);
            // Storing clamps exactly as the stage boundary would, so the last stage's clamp is skipped.
            _fused.ProcessRowRange(_prefix, _suffix, row, clampLast: false);
            BmagePixelRows.Store(row, target);
            if (_suffixTables is not null) MapThroughTables(target, target, _suffixTables);
        }

        public void Dispose()
        {
            _tables?.Dispose();
            if (_prefixTables is not null) foreach (var table in _prefixTables) ArrayPool<float>.Shared.Return(table);
            _suffixTables?.Dispose();
        }
    }

    /// <summary>
    /// What the operation, from stage <paramref name="start"/> on when it is fused, makes of every byte value, each
    /// entry shifted to its channel's place in a packed pixel.
    /// </summary>
    private static ByteTables Tabulate(BmagePointOperation operation, int start)
    {
        // The operation applied to every byte value at once: entry i of the probe row holds i in every channel.
        var probeArray = RentProbe();
        var bytesArray = ArrayPool<BmageRgba32>.Shared.Rent(256);
        try
        {
            var probe = probeArray.AsSpan(0, 256);
            if (operation is BmageFusedPointOperation fused) fused.ProcessRowRange(start, fused.Stages.Count, probe);
            else operation.ProcessRow(probe);
            var bytes = bytesArray.AsSpan(0, 256);
            BmagePixelRows.Store(probe, bytes);
            var tables = new ByteTables();
            for (var i = 0; i < 256; i++)
            {
                var p = bytes[i];
                tables.Red[i] = p.R;
                tables.Green[i] = (uint)p.G << 8;
                tables.Blue[i] = (uint)p.B << 16;
                tables.Alpha[i] = (uint)p.A << 24;
                tables.AlphaChanges |= p.A != i;
            }
            return tables;
        }
        finally
        {
            ArrayPool<Vector4>.Shared.Return(probeArray);
            ArrayPool<BmageRgba32>.Shared.Return(bytesArray);
        }
    }

    private static void MapThroughTables(ReadOnlySpan<BmageRgba32> source, Span<BmageRgba32> destination, ByteTables tables)
    {
        uint[] red = tables.Red, green = tables.Green, blue = tables.Blue, alpha = tables.Alpha;
        if (!BitConverter.IsLittleEndian)
        {
            for (var x = 0; x < source.Length; x++)
            {
                var p = source[x];
                destination[x] = new BmageRgba32((byte)red[p.R], (byte)(green[p.G] >> 8), (byte)(blue[p.B] >> 16), (byte)(alpha[p.A] >> 24));
            }
            return;
        }
        var from = MemoryMarshal.Cast<BmageRgba32, uint>(source);
        var to = MemoryMarshal.Cast<BmageRgba32, uint>(destination);
        if (tables.AlphaChanges)
        {
            for (var x = 0; x < from.Length; x++)
            {
                var v = from[x];
                to[x] = red[v & 0xFF] | green[(v >> 8) & 0xFF] | blue[(v >> 16) & 0xFF] | alpha[v >> 24];
            }
        }
        else
        {
            for (var x = 0; x < from.Length; x++)
            {
                var v = from[x];
                to[x] = red[v & 0xFF] | green[(v >> 8) & 0xFF] | blue[(v >> 16) & 0xFF] | (v & 0xFF000000);
            }
        }
    }

    /// <summary>Four 256-entry tables, rented from the pool and returned on disposal; entries past 255 are unused.</summary>
    private sealed class ByteTables : IDisposable
    {
        public readonly uint[] Red = Rent(), Green = Rent(), Blue = Rent(), Alpha = Rent();
        public bool AlphaChanges;

        private static uint[] Rent() => ArrayPool<uint>.Shared.Rent(256);

        public void Dispose()
        {
            ArrayPool<uint>.Shared.Return(Red);
            ArrayPool<uint>.Shared.Return(Green);
            ArrayPool<uint>.Shared.Return(Blue);
            ArrayPool<uint>.Shared.Return(Alpha);
        }
    }

    /// <summary>
    /// The values the first <paramref name="count"/> stages produce for each input byte, clamped as the stage after them
    /// sees them, one table per channel, rented from the pool.
    /// </summary>
    private static float[][] BuildPrefixTables(BmageFusedPointOperation fused, int count)
    {
        var probeArray = RentProbe();
        try
        {
            var probe = probeArray.AsSpan(0, 256);
            fused.ProcessRowRange(0, count, probe);
            var pool = ArrayPool<float>.Shared;
            var tables = new[] { pool.Rent(256), pool.Rent(256), pool.Rent(256), pool.Rent(256) };
            for (var i = 0; i < 256; i++)
            {
                tables[0][i] = probe[i].X;
                tables[1][i] = probe[i].Y;
                tables[2][i] = probe[i].Z;
                tables[3][i] = probe[i].W;
            }
            return tables;
        }
        finally
        {
            ArrayPool<Vector4>.Shared.Return(probeArray);
        }
    }

    /// <summary>
    /// Every byte value, loaded exactly as an image row is: entry i holds i in all four channels. Rented from the pool,
    /// with the values in the first 256 entries.
    /// </summary>
    private static Vector4[] RentProbe()
    {
        var bytes = ArrayPool<BmageRgba32>.Shared.Rent(256);
        var values = ArrayPool<Vector4>.Shared.Rent(256);
        for (var i = 0; i < 256; i++) bytes[i] = new BmageRgba32((byte)i, (byte)i, (byte)i, (byte)i);
        BmagePixelRows.Load(bytes.AsSpan(0, 256), values.AsSpan(0, 256));
        ArrayPool<BmageRgba32>.Shared.Return(bytes);
        return values;
    }

    private static void LoadThroughTables(ReadOnlySpan<BmageRgba32> source, Span<Vector4> destination, float[][] tables)
    {
        var red = tables[0]; var green = tables[1]; var blue = tables[2]; var alpha = tables[3];
        for (var i = 0; i < source.Length; i++)
        {
            var p = source[i];
            destination[i] = new Vector4(red[p.R], green[p.G], blue[p.B], alpha[p.A]);
        }
    }
}
