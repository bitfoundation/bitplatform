using System.Numerics;

namespace Bit.Bmage.Codecs.WebP;

/// <summary>
/// Encoder for the WebP lossless bitstream. Images with at most 256 colours are coded through a palette, packing several
/// pixels into one when there are few; others go through the subtract-green and predictor transforms. Pixels are then
/// coded with LZ77 back references, a colour cache sized to the image, and prefix codes built from the statistics.
/// </summary>
internal static class Vp8LEncoder
{
    private const int MaxLength = 4096;
    private const int MinMatch = 3;
    private const int MaxChain = 48;
    private const int HashBits = 18;
    private const int PredictorBits = 4;
    private const int MaxCodeLength = 15;

    /// <summary>Encodes a 'VP8L' chunk payload: the signature, the header and the image stream.</summary>
    public static byte[] Encode(ImageBuffer image, bool hasAlpha, CancellationToken ct)
    {
        var width = image.Width;
        var height = image.Height;
        var argb = new uint[(long)width * height];
        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var p = row[x];
                argb[y * width + x] = (uint)(p.A << 24 | p.R << 16 | p.G << 8 | p.B);
            }
        }
        var bw = new BitWriter(Math.Max(1024, argb.Length));
        bw.Write(0x2F, 8);
        bw.Write((uint)(width - 1), 14);
        bw.Write((uint)(height - 1), 14);
        bw.Write(hasAlpha ? 1u : 0u, 1);
        bw.Write(0, 3);
        EncodeImageStream(bw, argb, width, height, isAlpha: false, ct);
        return bw.Finish();
    }

    /// <summary>Encodes alpha values as the headerless image stream of an 'ALPH' chunk, the values in the green channel.</summary>
    public static byte[] EncodeAlpha(ReadOnlySpan<byte> alpha, int width, int height, CancellationToken ct)
    {
        var argb = new uint[(long)width * height];
        for (var i = 0; i < argb.Length; i++) argb[i] = (uint)alpha[i] << 8;
        var bw = new BitWriter(Math.Max(256, argb.Length / 8));
        EncodeImageStream(bw, argb, width, height, isAlpha: true, ct);
        return bw.Finish();
    }

    private static void EncodeImageStream(BitWriter bw, uint[] argb, int width, int height, bool isAlpha, CancellationToken ct)
    {
        var palette = FindPalette(argb, isAlpha ? 16 : 256);
        if (palette is not null)
        {
            var (packed, packedWidth) = ApplyPalette(bw, argb, width, height, palette, ct);
            bw.Write(0, 1); // no more transforms
            WriteEntropyCodedImage(bw, packed, packedWidth, height, isMain: true, ct);
            return;
        }

        if (!isAlpha)
        {
            // Subtract green, then predict: the decoder undoes them in the opposite order.
            bw.Write(1, 1);
            bw.Write(2, 2);
            SubtractGreen(argb);
            bw.Write(1, 1);
            bw.Write(0, 2);
            bw.Write(PredictorBits - 2, 3);
            var modes = ApplyPredictor(argb, width, height, ct);
            WriteEntropyCodedImage(bw, modes, DivRoundUp(width, PredictorBits), DivRoundUp(height, PredictorBits), isMain: false, ct);
        }
        bw.Write(0, 1);
        WriteEntropyCodedImage(bw, argb, width, height, isMain: true, ct);
    }

    private static int DivRoundUp(int value, int bits) => (value + (1 << bits) - 1) >> bits;

    // ------------------------------------------------------------------ palette

    private static uint[]? FindPalette(uint[] argb, int limit)
    {
        var colors = new HashSet<uint>();
        var last = ~argb[0];
        foreach (var c in argb)
        {
            if (c == last) continue;
            last = c;
            if (colors.Add(c) && colors.Count > limit) return null;
        }
        var palette = colors.ToArray();
        Array.Sort(palette);
        return palette;
    }

    private static (uint[] Packed, int Width) ApplyPalette(BitWriter bw, uint[] argb, int width, int height, uint[] palette, CancellationToken ct)
    {
        bw.Write(1, 1);
        bw.Write(3, 2);
        bw.Write((uint)(palette.Length - 1), 8);
        // The table is coded as differences between neighbouring entries.
        var deltas = new uint[palette.Length];
        deltas[0] = palette[0];
        for (var i = 1; i < palette.Length; i++) deltas[i] = Vp8LDecoder.SubtractPixels(palette[i], palette[i - 1]);
        WriteEntropyCodedImage(bw, deltas, palette.Length, 1, isMain: false, ct);

        var index = new Dictionary<uint, int>(palette.Length);
        for (var i = 0; i < palette.Length; i++) index[palette[i]] = i;
        var bits = palette.Length > 16 ? 0 : palette.Length > 4 ? 1 : palette.Length > 2 ? 2 : 3;
        var packedWidth = DivRoundUp(width, bits);
        var packed = new uint[(long)packedWidth * height];
        var bitsPerPixel = 8 >> bits;
        var perPixel = 1 << bits;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = index[argb[y * width + x]];
                packed[y * packedWidth + x / perPixel] |= (uint)(i << ((x % perPixel) * bitsPerPixel)) << 8;
            }
        }
        for (var i = 0; i < packed.Length; i++) packed[i] |= 0xFF000000u;
        return (packed, packedWidth);
    }

    // ------------------------------------------------------------------ transforms

    private static void SubtractGreen(uint[] argb)
    {
        for (var i = 0; i < argb.Length; i++)
        {
            var p = argb[i];
            var green = (p >> 8) & 255;
            argb[i] = (p & 0xFF00FF00u) | (((p | 0xFF00FF00u) - (green << 16 | green)) & 0x00FF00FFu);
        }
    }

    /// <summary>
    /// Chooses, for every block, the predictor whose residuals have the least entropy, and replaces the pixels with their
    /// residuals. Returns the image of chosen modes, in the green channel.
    /// </summary>
    private static uint[] ApplyPredictor(uint[] argb, int width, int height, CancellationToken ct)
    {
        var blocksWide = DivRoundUp(width, PredictorBits);
        var blocksHigh = DivRoundUp(height, PredictorBits);
        var modes = new uint[blocksWide * blocksHigh];
        var size = 1 << PredictorBits;
        var histogram = new int[4 * 256];
        for (var by = 0; by < blocksHigh; by++)
        {
            ct.ThrowIfCancellationRequested();
            for (var bx = 0; bx < blocksWide; bx++)
            {
                var bestMode = 0;
                var bestCost = double.MaxValue;
                for (var mode = 0; mode < 14; mode++)
                {
                    Array.Clear(histogram);
                    for (var y = by * size; y < Math.Min(height, by * size + size); y++)
                        for (var x = bx * size; x < Math.Min(width, bx * size + size); x++)
                        {
                            var residual = Vp8LDecoder.SubtractPixels(argb[y * width + x], Predict(argb, width, x, y, mode));
                            histogram[residual >> 24]++;
                            histogram[256 + ((residual >> 16) & 255)]++;
                            histogram[512 + ((residual >> 8) & 255)]++;
                            histogram[768 + (residual & 255)]++;
                        }
                    var cost = 0.0;
                    for (var c = 0; c < 4; c++) cost += ShannonBits(histogram.AsSpan(c * 256, 256));
                    if (cost < bestCost) { bestCost = cost; bestMode = mode; }
                }
                modes[by * blocksWide + bx] = 0xFF000000u | (uint)(bestMode << 8);
            }
        }
        // Residuals depend on the original neighbours, so they are produced back to front.
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = width - 1; x >= 0; x--)
            {
                var mode = (int)((modes[(y >> PredictorBits) * blocksWide + (x >> PredictorBits)] >> 8) & 15);
                var i = y * width + x;
                argb[i] = Vp8LDecoder.SubtractPixels(argb[i], Predict(argb, width, x, y, mode));
            }
        }
        return modes;
    }

    /// <summary>The prediction a decoder makes for (x, y), including the rules for the first row and column.</summary>
    private static uint Predict(uint[] argb, int width, int x, int y, int mode)
    {
        var i = y * width + x;
        if (y == 0) return x == 0 ? 0xFF000000u : argb[i - 1];
        if (x == 0) return argb[i - width];
        return Vp8LDecoder.Predict(mode, argb[i - 1], argb[i - width], argb[i - width + 1], argb[i - width - 1]);
    }

    private static double ShannonBits(ReadOnlySpan<int> histogram)
    {
        long total = 0;
        foreach (var c in histogram) total += c;
        if (total == 0) return 0;
        var bits = 0.0;
        var log2Total = Math.Log2(total);
        foreach (var c in histogram) if (c > 0) bits += c * (log2Total - Math.Log2(c));
        return bits;
    }

    // ------------------------------------------------------------------ entropy coding

    /// <summary>
    /// A back reference, copying <see cref="Length"/> pixels from <see cref="Distance"/> back, after
    /// <see cref="Literals"/> pixels coded as themselves. Literals are not stored, which keeps a photo, mostly literals,
    /// from needing a token per pixel; those after the last reference run to the end of the image.
    /// </summary>
    private readonly record struct Token(int Literals, int Length, int Distance);

    private static void WriteEntropyCodedImage(BitWriter bw, uint[] pixels, int width, int height, bool isMain, CancellationToken ct)
    {
        var tokens = FindBackReferences(pixels, width, ct);
        var cacheBits = ChooseCacheBits(tokens, pixels);
        bw.Write(cacheBits > 0 ? 1u : 0u, 1);
        if (cacheBits > 0) bw.Write((uint)cacheBits, 4);
        if (isMain) bw.Write(0, 1); // one set of prefix codes for the whole image

        var cacheSize = cacheBits > 0 ? 1 << cacheBits : 0;
        var histograms = new[] { new int[280 + cacheSize], new int[256], new int[256], new int[256], new int[40] };
        var distanceCodes = new DistanceCodes(width);
        Walk(tokens, pixels, cacheBits, distanceCodes, histograms, null, null);

        var codes = new PrefixCode[5];
        for (var i = 0; i < 5; i++)
        {
            codes[i] = PrefixCode.Build(histograms[i], MaxCodeLength);
            codes[i].Write(bw);
        }
        Walk(tokens, pixels, cacheBits, distanceCodes, null, codes, bw);
    }

    /// <summary>Counts the symbols of the token stream (when <paramref name="histograms"/> is given) or writes them.</summary>
    private static void Walk(List<Token> tokens, uint[] pixels, int cacheBits, DistanceCodes distances, int[][]? histograms, PrefixCode[]? codes, BitWriter? bw)
    {
        var cache = cacheBits > 0 ? new uint[1 << cacheBits] : null;
        var shift = 32 - cacheBits;
        var pos = 0;
        foreach (var t in tokens)
        {
            Literals(pos, pos + t.Literals);
            pos += t.Literals;
            var (lengthPrefix, lengthBits, lengthExtra) = PrefixEncode(t.Length);
            Symbol(0, 256 + lengthPrefix);
            bw?.Write((uint)lengthExtra, lengthBits);
            var (distPrefix, distBits, distExtra) = PrefixEncode(distances.ToCode(t.Distance));
            Symbol(4, distPrefix);
            bw?.Write((uint)distExtra, distBits);
            if (cache is not null)
                for (var i = 0; i < t.Length; i++) { var c = pixels[pos + i]; cache[(0x1e35a7bdu * c) >> shift] = c; }
            pos += t.Length;
        }
        Literals(pos, pixels.Length);

        void Literals(int from, int to)
        {
            for (var i = from; i < to; i++)
            {
                var p = pixels[i];
                if (cache is not null)
                {
                    var key = (int)((0x1e35a7bdu * p) >> shift);
                    if (cache[key] == p)
                    {
                        Symbol(0, 280 + key);
                        continue;
                    }
                    cache[key] = p;
                }
                Symbol(0, (int)((p >> 8) & 255));
                Symbol(1, (int)((p >> 16) & 255));
                Symbol(2, (int)(p & 255));
                Symbol(3, (int)(p >> 24));
            }
        }

        void Symbol(int code, int symbol)
        {
            if (histograms is not null) histograms[code][symbol]++;
            else codes![code].WriteSymbol(bw!, symbol);
        }
    }

    /// <summary>The inverse of the decoder's prefix coding of lengths and distances: (prefix, extra bit count, extra bits).</summary>
    private static (int Prefix, int Bits, int Extra) PrefixEncode(int value)
    {
        if (value <= 4) return (value - 1, 0, 0);
        var v = value - 1;
        var high = 31 - BitOperations.LeadingZeroCount((uint)v);
        var second = (v >> (high - 1)) & 1;
        var bits = high - 1;
        return (2 * high + second, bits, v & ((1 << bits) - 1));
    }

    /// <summary>Greedy LZ77 over whole pixels with hash chains; the previous pixel and the one above are always tried.</summary>
    private static List<Token> FindBackReferences(uint[] pixels, int width, CancellationToken ct)
    {
        var total = pixels.Length;
        var tokens = new List<Token>();
        if (total < MinMatch + 1) return tokens;
        // No more heads than positions, so a palette or a transform's sub-image does not fill a table sized for a photo.
        var hashBits = Math.Min(HashBits, BitOperations.Log2((uint)total - 1) + 1);
        var head = new int[1 << hashBits];
        Array.Fill(head, -1);
        var chain = new int[total];
        var maxDistance = (1 << 20) - 120;
        var pos = 0;
        var literals = 0;
        while (pos < total)
        {
            if ((pos & 0xFFFF) == 0) ct.ThrowIfCancellationRequested();
            var bestLength = 0;
            var bestDistance = 0;
            var limit = Math.Min(MaxLength, total - pos);
            if (limit >= MinMatch)
            {
                Try(1);
                if (width < pos) Try(width);
                if (pos + 1 < total)
                {
                    var candidate = head[Hash(pixels[pos], pixels[pos + 1], hashBits)];
                    for (var steps = 0; candidate >= 0 && steps < MaxChain && bestLength < limit; steps++)
                    {
                        var distance = pos - candidate;
                        if (distance > maxDistance) break;
                        Try(distance);
                        candidate = chain[candidate];
                    }
                }
            }

            var advance = bestLength >= MinMatch ? bestLength : 1;
            if (bestLength >= MinMatch)
            {
                tokens.Add(new Token(literals, bestLength, bestDistance));
                literals = 0;
            }
            else literals++;
            for (var i = 0; i < advance; i++, pos++)
            {
                if (pos + 1 >= total) continue;
                var h = Hash(pixels[pos], pixels[pos + 1], hashBits);
                chain[pos] = head[h];
                head[h] = pos;
            }

            void Try(int distance)
            {
                if (distance > pos || distance <= 0) return;
                var source = pos - distance;
                // A candidate can only be longer if it also matches where the best one so far stopped.
                if (bestLength > 0 && bestLength < limit && pixels[source + bestLength] != pixels[pos + bestLength]) return;
                var length = 0;
                while (length < limit && pixels[source + length] == pixels[pos + length]) length++;
                if (length > bestLength) { bestLength = length; bestDistance = distance; }
            }
        }
        return tokens;
    }

    private static int Hash(uint a, uint b, int bits) => (int)((((a * 0x9E3779B1u) ^ (b * 0x85EBCA6Bu)) * 0xC2B2AE35u) >> (32 - bits));

    /// <summary>Picks the colour cache size that minimises the estimated size of the coded literals.</summary>
    private static int ChooseCacheBits(List<Token> tokens, uint[] pixels)
    {
        var best = 0;
        var bestBits = double.MaxValue;
        foreach (var bits in (ReadOnlySpan<int>)[0, 3, 5, 7, 9, 10, 11])
        {
            var size = bits > 0 ? 1 << bits : 0;
            var histograms = new[] { new int[280 + size], new int[256], new int[256], new int[256], new int[40] };
            // The distances do not depend on the cache, so any mapping counts them the same.
            Walk(tokens, pixels, bits, NoDistances, histograms, null, null);
            var estimate = 0.0;
            for (var h = 0; h < 4; h++) estimate += ShannonBits(histograms[h]) + histograms[h].Length / 4.0; // a rough price for the code itself
            if (estimate < bestBits) { bestBits = estimate; best = bits; }
        }
        return best;
    }

    private static readonly DistanceCodes NoDistances = new(1);

    /// <summary>Maps pixel distances to the format's distance codes, which give short codes to nearby 2D neighbours.</summary>
    private sealed class DistanceCodes
    {
        private readonly int[] _shortCodes;

        public DistanceCodes(int width)
        {
            _shortCodes = new int[8 * width + 9];
            // Smallest code last, so it wins where several name the same distance. The decoder clamps distances below
            // one to one; an encoder has no reason to rely on that.
            for (var code = 120; code >= 1; code--)
            {
                var packed = Vp8LDecoder.DistanceMapEntry(code);
                var distance = (packed >> 4) * width + 8 - (packed & 15);
                if (distance >= 1 && distance < _shortCodes.Length) _shortCodes[distance] = code;
            }
        }

        public int ToCode(int distance) => distance < _shortCodes.Length && _shortCodes[distance] != 0 ? _shortCodes[distance] : distance + 120;
    }

    // ------------------------------------------------------------------ prefix codes

    /// <summary>A canonical prefix code built from a histogram, and its encoding in the bitstream.</summary>
    private sealed class PrefixCode
    {
        private static ReadOnlySpan<byte> CodeLengthCodeOrder => [17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

        private readonly byte[] _lengths;
        private readonly int[] _codes; // bit reversed, ready to be written least significant bit first
        private readonly int _symbolCount;
        private readonly int _firstSymbol;
        private readonly int _secondSymbol;

        private PrefixCode(byte[] lengths, int symbolCount, int firstSymbol, int secondSymbol)
        {
            _lengths = lengths;
            _symbolCount = symbolCount;
            _firstSymbol = firstSymbol;
            _secondSymbol = secondSymbol;
            _codes = CanonicalCodes(lengths);
        }

        public static PrefixCode Build(int[] histogram, int maxLength)
        {
            var count = 0;
            int first = -1, second = -1;
            for (var s = 0; s < histogram.Length; s++)
            {
                if (histogram[s] == 0) continue;
                if (count == 0) first = s; else if (count == 1) second = s;
                count++;
            }
            var lengths = new byte[histogram.Length];
            if (count <= 1)
            {
                // A single symbol costs no bits; an empty code is written as the single symbol 0.
                lengths[Math.Max(first, 0)] = 1;
                return new PrefixCode(lengths, 1, Math.Max(first, 0), -1);
            }
            BuildLengths(histogram, maxLength, lengths);
            return new PrefixCode(lengths, count, first, second);
        }

        public void WriteSymbol(BitWriter bw, int symbol)
        {
            if (_symbolCount <= 1) return;
            bw.Write((uint)_codes[symbol], _lengths[symbol]);
        }

        public void Write(BitWriter bw)
        {
            if (_symbolCount <= 2 && _firstSymbol < 256 && _secondSymbol < 256)
            {
                // The simple form: one or two symbols below 256.
                bw.Write(1, 1);
                bw.Write((uint)(_symbolCount - 1), 1);
                if (_firstSymbol <= 1)
                {
                    bw.Write(0, 1);
                    bw.Write((uint)_firstSymbol, 1);
                }
                else
                {
                    bw.Write(1, 1);
                    bw.Write((uint)_firstSymbol, 8);
                }
                if (_symbolCount == 2) bw.Write((uint)_secondSymbol, 8);
                return;
            }

            bw.Write(0, 1);
            var tokens = RunLengthEncode(_lengths);
            var histogram = new int[19];
            foreach (var (code, _) in tokens) histogram[code]++;
            var lengthCode = Build(histogram, 7);
            var numCodes = 19;
            while (numCodes > 4 && lengthCode._lengths[CodeLengthCodeOrder[numCodes - 1]] == 0) numCodes--;
            bw.Write((uint)(numCodes - 4), 4);
            for (var i = 0; i < numCodes; i++) bw.Write(lengthCode._lengths[CodeLengthCodeOrder[i]], 3);
            bw.Write(0, 1); // the code covers the whole alphabet
            foreach (var (code, extra) in tokens)
            {
                lengthCode.WriteSymbol(bw, code);
                switch (code)
                {
                    case 16: bw.Write((uint)extra, 2); break;
                    case 17: bw.Write((uint)extra, 3); break;
                    case 18: bw.Write((uint)extra, 7); break;
                }
            }
        }

        /// <summary>Code lengths as the format codes them: literal lengths, repeats of the previous one, and runs of zeros.</summary>
        private static List<(int Code, int Extra)> RunLengthEncode(byte[] lengths)
        {
            var tokens = new List<(int, int)>();
            var i = 0;
            while (i < lengths.Length)
            {
                var value = lengths[i];
                var run = 1;
                while (i + run < lengths.Length && lengths[i + run] == value) run++;
                i += run;
                if (value == 0)
                {
                    while (run > 0)
                    {
                        if (run >= 11) { var n = Math.Min(run, 138); tokens.Add((18, n - 11)); run -= n; }
                        else if (run >= 3) { tokens.Add((17, run - 3)); run = 0; }
                        else { tokens.Add((0, 0)); run--; }
                    }
                    continue;
                }
                tokens.Add((value, 0));
                run--;
                while (run > 0)
                {
                    if (run >= 3) { var n = Math.Min(run, 6); tokens.Add((16, n - 3)); run -= n; }
                    else { tokens.Add((value, 0)); run--; }
                }
            }
            return tokens;
        }

        /// <summary>
        /// Huffman code lengths limited to <paramref name="maxLength"/>: when the optimal tree is too deep, the rarest
        /// counts are raised step by step, which flattens it at a small cost.
        /// </summary>
        private static void BuildLengths(int[] histogram, int maxLength, byte[] lengths)
        {
            for (var floor = 1; ; floor *= 2)
            {
                var queue = new PriorityQueue<int, long>();
                var nodes = new List<(long Weight, int Left, int Right)>();
                for (var s = 0; s < histogram.Length; s++)
                {
                    if (histogram[s] == 0) continue;
                    var w = Math.Max(histogram[s], floor);
                    nodes.Add((w, -1 - s, 0));
                    // Ties break on insertion order, which keeps the result deterministic.
                    queue.Enqueue(nodes.Count - 1, (long)w << 20 | (uint)(nodes.Count - 1));
                }
                while (queue.Count > 1)
                {
                    queue.TryDequeue(out var a, out _);
                    queue.TryDequeue(out var b, out _);
                    nodes.Add((nodes[a].Weight + nodes[b].Weight, a, b));
                    queue.Enqueue(nodes.Count - 1, (nodes[^1].Weight << 20) | (uint)(nodes.Count - 1));
                }
                Array.Clear(lengths);
                var deepest = 0;
                var stack = new Stack<(int Node, int Depth)>();
                stack.Push((nodes.Count - 1, 0));
                while (stack.Count > 0)
                {
                    var (node, depth) = stack.Pop();
                    var n = nodes[node];
                    if (n.Left < 0)
                    {
                        lengths[-1 - n.Left] = (byte)depth;
                        deepest = Math.Max(deepest, depth);
                        continue;
                    }
                    stack.Push((n.Left, depth + 1));
                    stack.Push((n.Right, depth + 1));
                }
                if (deepest <= maxLength) return;
            }
        }

        private static int[] CanonicalCodes(byte[] lengths)
        {
            Span<int> count = stackalloc int[MaxCodeLength + 1];
            foreach (var l in lengths) if (l > 0) count[l]++;
            Span<int> next = stackalloc int[MaxCodeLength + 1];
            var code = 0;
            for (var len = 1; len <= MaxCodeLength; len++)
            {
                code = (code + count[len - 1]) << 1;
                next[len] = code;
            }
            var codes = new int[lengths.Length];
            for (var s = 0; s < lengths.Length; s++)
            {
                int len = lengths[s];
                if (len == 0) continue;
                var c = next[len]++;
                var reversed = 0;
                for (var i = 0; i < len; i++) { reversed = (reversed << 1) | (c & 1); c >>= 1; }
                codes[s] = reversed;
            }
            return codes;
        }
    }

    /// <summary>Writes bits least significant first.</summary>
    internal sealed class BitWriter(int capacity)
    {
        private byte[] _buffer = new byte[capacity];
        private int _length;
        private ulong _bits;
        private int _used;

        public void Write(uint value, int count)
        {
            if (count == 0) return;
            _bits |= (ulong)(value & (uint)((1UL << count) - 1)) << _used;
            _used += count;
            while (_used >= 8)
            {
                if (_length == _buffer.Length) Array.Resize(ref _buffer, _buffer.Length * 2);
                _buffer[_length++] = (byte)_bits;
                _bits >>= 8;
                _used -= 8;
            }
        }

        public byte[] Finish()
        {
            if (_used > 0) Write(0, 8 - _used);
            return _buffer.AsSpan(0, _length).ToArray();
        }
    }
}
