using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bit.Bmage.Codecs.Avif;

/// <summary>The constrained directional enhancement filter (spec 7.15).</summary>
internal static class Av1Cdef
{
    /// <summary>Returns the filtered planes (CdefFrame), or null when no 8x8 block is filtered and CdefFrame equals CurrFrame.</summary>
    public static Av1Plane[]? Apply(Av1Frame f, CancellationToken cancellationToken)
    {
        var hdr = f.Hdr;
        var anyStrength = false;
        for (var i = 0; i < 1 << hdr.CdefBits; i++)
            if (hdr.CdefYPriStrength[i] != 0 || hdr.CdefYSecStrength[i] != 0 || hdr.CdefUvPriStrength[i] != 0 || hdr.CdefUvSecStrength[i] != 0)
                anyStrength = true;
        if (!anyStrength || f.CdefIdx.AsSpan().IndexOfAnyExcept((sbyte)-1) < 0) return null;

        var cdef = new Av1Plane[f.NumPlanes];
        try
        {
            for (var p = 0; p < f.NumPlanes; p++) cdef[p] = f.Planes[p].Clone();
            for (var r = 0; r < f.MiRows; r += 2)
            {
                if ((r & 15) == 0) cancellationToken.ThrowIfCancellationRequested();
                for (var c = 0; c < f.MiCols; c += 2)
                {
                    int idx = f.CdefIdx[(r >> 4) * f.CdefStride + (c >> 4)];
                    if (idx == -1) continue;
                    Block(f, cdef, r, c, idx);
                }
            }
            return cdef;
        }
        catch
        {
            foreach (var plane in cdef) plane?.Dispose();
            throw;
        }
    }

    private static void Block(Av1Frame f, Av1Plane[] cdef, int r, int c, int idx)
    {
        var hdr = f.Hdr;
        var mi = r * f.MiCols + c;
        var skip = f.Skips[mi] != 0 && f.Skips[mi + f.MiCols] != 0 && f.Skips[mi + 1] != 0 && f.Skips[mi + f.MiCols + 1] != 0;
        if (skip) return;
        var coeffShift = f.BitDepth - 8;
        var (yDir, var) = Direction(f, r, c);
        var priStr = hdr.CdefYPriStrength[idx] << coeffShift;
        var secStr = hdr.CdefYSecStrength[idx] << coeffShift;
        var dir = priStr == 0 ? 0 : yDir;
        var varStr = (var >> 6) != 0 ? Math.Min(BitOperations.Log2((uint)(var >> 6)), 12) : 0;
        priStr = var != 0 ? (priStr * (4 + varStr) + 8) >> 4 : 0;
        var damping = hdr.CdefDamping + coeffShift;
        Filter(f, cdef, 0, r, c, priStr, secStr, damping, dir);
        if (f.NumPlanes == 1) return;
        priStr = hdr.CdefUvPriStrength[idx] << coeffShift;
        secStr = hdr.CdefUvSecStrength[idx] << coeffShift;
        dir = priStr == 0 ? 0 : Av1Tables.CdefUvDir[(f.SubX * 2 + f.SubY) * 8 + yDir];
        damping = hdr.CdefDamping + coeffShift - 1;
        Filter(f, cdef, 1, r, c, priStr, secStr, damping, dir);
        Filter(f, cdef, 2, r, c, priStr, secStr, damping, dir);
    }

    private static (int Dir, int Var) Direction(Av1Frame f, int r, int c)
    {
        Span<int> cost = stackalloc int[8];
        Span<int> partial = stackalloc int[8 * 15];
        var luma = f.Planes[0];
        var x0 = c << 2;
        var y0 = r << 2;
        var shift = f.BitDepth - 8;
        for (var i = 0; i < 8; i++)
        {
            var row = (y0 + i) * luma.Stride + x0;
            for (var j = 0; j < 8; j++)
            {
                var x = (luma.Data[row + j] >> shift) - 128;
                partial[0 * 15 + i + j] += x;
                partial[1 * 15 + i + j / 2] += x;
                partial[2 * 15 + i] += x;
                partial[3 * 15 + 3 + i - j / 2] += x;
                partial[4 * 15 + 7 + i - j] += x;
                partial[5 * 15 + 3 - i / 2 + j] += x;
                partial[6 * 15 + j] += x;
                partial[7 * 15 + i / 2 + j] += x;
            }
        }
        var div = Av1Tables.DivTable;
        for (var i = 0; i < 8; i++)
        {
            cost[2] += partial[2 * 15 + i] * partial[2 * 15 + i];
            cost[6] += partial[6 * 15 + i] * partial[6 * 15 + i];
        }
        cost[2] *= div[8];
        cost[6] *= div[8];
        for (var i = 0; i < 7; i++)
        {
            cost[0] += (partial[i] * partial[i] + partial[14 - i] * partial[14 - i]) * div[i + 1];
            cost[4] += (partial[4 * 15 + i] * partial[4 * 15 + i] + partial[4 * 15 + 14 - i] * partial[4 * 15 + 14 - i]) * div[i + 1];
        }
        cost[0] += partial[7] * partial[7] * div[8];
        cost[4] += partial[4 * 15 + 7] * partial[4 * 15 + 7] * div[8];
        for (var i = 1; i < 8; i += 2)
        {
            for (var j = 0; j < 5; j++) cost[i] += partial[i * 15 + 3 + j] * partial[i * 15 + 3 + j];
            cost[i] *= div[8];
            for (var j = 0; j < 3; j++)
                cost[i] += (partial[i * 15 + j] * partial[i * 15 + j] + partial[i * 15 + 10 - j] * partial[i * 15 + 10 - j]) * div[2 * j + 2];
        }
        var bestCost = 0;
        var yDir = 0;
        for (var i = 0; i < 8; i++)
        {
            if (cost[i] > bestCost)
            {
                bestCost = cost[i];
                yDir = i;
            }
        }
        return (yDir, (bestCost - cost[(yDir + 4) & 7]) >> 10);
    }

    /// <summary>constrain( diff, threshold, damping ), given the damping shift Max( 0, damping - FloorLog2( threshold ) ).</summary>
    private static int Constrain(int diff, int threshold, int dampingShift)
    {
        if (threshold == 0) return 0;
        var abs = diff < 0 ? -diff : diff;
        var v = threshold - (abs >> dampingShift);
        v = v < 0 ? 0 : v > abs ? abs : v;
        return diff < 0 ? -v : v;
    }

    private static void Filter(Av1Frame f, Av1Plane[] cdef, int plane, int r, int c, int priStr, int secStr, int damping, int dir)
    {
        var coeffShift = f.BitDepth - 8;
        var subX = plane > 0 ? f.SubX : 0;
        var subY = plane > 0 ? f.SubY : 0;
        var x0 = (c * 4) >> subX;
        var y0 = (r * 4) >> subY;
        var w = 8 >> subX;
        var h = 8 >> subY;
        var src = f.Planes[plane];
        var dst = cdef[plane];
        var stride = src.Stride;
        var maxY = (f.MiRows * 4) >> subY;
        var maxX = (f.MiCols * 4) >> subX;
        var dirs = Av1Tables.CdefDirections;
        var priTaps = Av1Tables.CdefPriTaps;
        var tapSet = ((priStr >> coeffShift) & 1) * 2;
        // constrain() with its damping shift worked out once per block rather than per tap.
        var priShift = priStr == 0 ? 0 : Math.Max(0, damping - BitOperations.Log2((uint)priStr));
        var secShift = secStr == 0 ? 0 : Math.Max(0, damping - BitOperations.Log2((uint)secStr));
        Span<int> off = stackalloc int[6];
        for (var k = 0; k < 2; k++)
        {
            var d0 = (dir * 2 + k) * 2;
            var d1 = (((dir + 2) & 7) * 2 + k) * 2;
            var d2 = (((dir - 2) & 7) * 2 + k) * 2;
            off[k * 3] = dirs[d0] * stride + dirs[d0 + 1];
            off[k * 3 + 1] = dirs[d1] * stride + dirs[d1 + 1];
            off[k * 3 + 2] = dirs[d2] * stride + dirs[d2 + 1];
        }
        int pri0 = priTaps[tapSet], pri1 = priTaps[tapSet + 1];
        var data = src.Data;
        var output = dst.Data;
        // Taps reach at most two samples away; blocks that far from the frame edges need no availability checks.
        if (x0 >= 2 && y0 >= 2 && x0 + w + 2 <= maxX && y0 + h + 2 <= maxY)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                FilterInteriorVector(data, output, y0 * stride + x0, stride, w, h, off, priStr, priShift, secStr, secShift, pri0, pri1);
                return;
            }
            for (var i = 0; i < h; i++)
            {
                var pos = (y0 + i) * stride + x0;
                for (var j = 0; j < w; j++, pos++)
                {
                    int x = data[pos];
                    var max = x;
                    var min = x;
                    var sum = 0;
                    for (var k = 0; k < 2; k++)
                    {
                        var priTap = k == 0 ? pri0 : pri1;
                        var secTap = k == 0 ? 2 : 1;
                        var o = off[k * 3];
                        int a = data[pos + o], b = data[pos - o];
                        sum += priTap * (Constrain(a - x, priStr, priShift) + Constrain(b - x, priStr, priShift));
                        max = Math.Max(max, Math.Max(a, b));
                        min = Math.Min(min, Math.Min(a, b));
                        o = off[k * 3 + 1];
                        a = data[pos + o];
                        b = data[pos - o];
                        var o2 = off[k * 3 + 2];
                        int e = data[pos + o2], g = data[pos - o2];
                        sum += secTap * (Constrain(a - x, secStr, secShift) + Constrain(b - x, secStr, secShift)
                            + Constrain(e - x, secStr, secShift) + Constrain(g - x, secStr, secShift));
                        max = Math.Max(Math.Max(max, Math.Max(a, b)), Math.Max(e, g));
                        min = Math.Min(Math.Min(min, Math.Min(a, b)), Math.Min(e, g));
                    }
                    output[pos] = (ushort)Math.Clamp(x + ((8 + sum - (sum < 0 ? 1 : 0)) >> 4), min, max);
                }
            }
            return;
        }
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                var sum = 0;
                var y = y0 + i;
                var xx = x0 + j;
                int x = data[y * stride + xx];
                var max = x;
                var min = x;
                for (var k = 0; k < 2; k++)
                {
                    var priTap = k == 0 ? pri0 : pri1;
                    var secTap = k == 0 ? 2 : 1;
                    for (var sign = -1; sign <= 1; sign += 2)
                    {
                        for (var t = 0; t < 3; t++)
                        {
                            var di = t == 0 ? (dir * 2 + k) * 2 : t == 1 ? (((dir + 2) & 7) * 2 + k) * 2 : (((dir - 2) & 7) * 2 + k) * 2;
                            var py = y + sign * dirs[di];
                            var px = xx + sign * dirs[di + 1];
                            if (py < 0 || py >= maxY || px < 0 || px >= maxX) continue;
                            int v = data[py * stride + px];
                            sum += t == 0 ? priTap * Constrain(v - x, priStr, priShift) : secTap * Constrain(v - x, secStr, secShift);
                            if (v > max) max = v;
                            if (v < min) min = v;
                        }
                    }
                }
                output[y * stride + xx] = (ushort)Math.Clamp(x + ((8 + sum - (sum < 0 ? 1 : 0)) >> 4), min, max);
            }
        }
    }

    /// <summary>
    /// The interior filter eight samples at a time: a row of a luma block, or two rows of a subsampled chroma one. Values
    /// stay well inside 16 bits (taps times strengths sum to a few thousand at 12 bits). With only one of the two
    /// strengths set the result never leaves the taps' range, so only the combined case needs the min/max clip.
    /// </summary>
    private static void FilterInteriorVector(ushort[] data, ushort[] output, int start, int stride, int w, int h, Span<int> off,
        int priStr, int priShift, int secStr, int secShift, int pri0, int pri1)
    {
        ref var src = ref MemoryMarshal.GetArrayDataReference(data);
        ref var dst = ref MemoryMarshal.GetArrayDataReference(output);
        var both = priStr != 0 && secStr != 0;
        var priT = Vector128.Create((short)priStr);
        var secT = Vector128.Create((short)secStr);
        var priTap0 = Vector128.Create((short)pri0);
        var priTap1 = Vector128.Create((short)pri1);
        var eight = Vector128.Create((short)8);
        var rows = w == 8 ? 1 : 2;
        for (var i = 0; i < h; i += rows)
        {
            var pos = start + i * stride;
            var x = Load(ref src, pos, w, stride);
            var sum = Vector128<short>.Zero;
            var max = x;
            var min = x;
            for (var k = 0; k < 2; k++)
            {
                if (priStr != 0 || both)
                {
                    var o = off[k * 3];
                    var a = Load(ref src, pos + o, w, stride);
                    var b = Load(ref src, pos - o, w, stride);
                    var t = Constrain(a - x, priT, priShift) + Constrain(b - x, priT, priShift);
                    sum += t * (k == 0 ? priTap0 : priTap1);
                    if (both)
                    {
                        max = Vector128.Max(max, Vector128.Max(a, b));
                        min = Vector128.Min(min, Vector128.Min(a, b));
                    }
                }
                if (secStr != 0)
                {
                    var o1 = off[k * 3 + 1];
                    var o2 = off[k * 3 + 2];
                    var a = Load(ref src, pos + o1, w, stride);
                    var b = Load(ref src, pos - o1, w, stride);
                    var c = Load(ref src, pos + o2, w, stride);
                    var d = Load(ref src, pos - o2, w, stride);
                    var t = Constrain(a - x, secT, secShift) + Constrain(b - x, secT, secShift) + Constrain(c - x, secT, secShift) + Constrain(d - x, secT, secShift);
                    sum += k == 0 ? t + t : t;
                    if (both)
                    {
                        max = Vector128.Max(Vector128.Max(max, Vector128.Max(a, b)), Vector128.Max(c, d));
                        min = Vector128.Min(Vector128.Min(min, Vector128.Min(a, b)), Vector128.Min(c, d));
                    }
                }
            }
            // (8 + sum - (sum < 0)) >> 4: the comparison mask is -1 where sum is negative.
            var result = x + Vector128.ShiftRightArithmetic(eight + sum + Vector128.LessThan(sum, Vector128<short>.Zero), 4);
            if (both) result = Vector128.Min(Vector128.Max(result, min), max);
            Store(ref dst, pos, w, stride, result);
        }
    }

    private static Vector128<short> Constrain(Vector128<short> diff, Vector128<short> threshold, int dampingShift)
    {
        var abs = Vector128.Abs(diff);
        var v = Vector128.Min(Vector128.Max(threshold - Vector128.ShiftRightArithmetic(abs, dampingShift), Vector128<short>.Zero), abs);
        return Vector128.ConditionalSelect(Vector128.LessThan(diff, Vector128<short>.Zero), -v, v);
    }

    private static Vector128<short> Load(ref ushort src, int pos, int w, int stride)
    {
        if (w == 8) return Vector128.LoadUnsafe(ref src, (nuint)pos).AsInt16();
        return Vector128.Create(Vector64.LoadUnsafe(ref src, (nuint)pos), Vector64.LoadUnsafe(ref src, (nuint)(pos + stride))).AsInt16();
    }

    private static void Store(ref ushort dst, int pos, int w, int stride, Vector128<short> value)
    {
        var v = value.AsUInt16();
        if (w == 8)
        {
            v.StoreUnsafe(ref dst, (nuint)pos);
            return;
        }
        v.GetLower().StoreUnsafe(ref dst, (nuint)pos);
        v.GetUpper().StoreUnsafe(ref dst, (nuint)(pos + stride));
    }
}
