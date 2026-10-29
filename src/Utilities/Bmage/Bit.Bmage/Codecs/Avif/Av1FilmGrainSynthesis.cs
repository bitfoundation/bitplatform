namespace Bit.Bmage.Codecs.Avif;

/// <summary>The reference film grain synthesis process (spec 7.18.3), applied to the output planes.</summary>
internal static class Av1FilmGrainSynthesis
{
    public static void Apply(Av1Image image, Av1FilmGrain g)
    {
        var bitDepth = image.BitDepth;
        var mono = image.MonoChrome;
        var subX = image.SubX;
        var subY = image.SubY;
        var numPlanes = image.Planes.Length;
        var w = image.Width;
        var h = image.Height;
        var random = g.GrainSeed;
        var grainCenter = 128 << (bitDepth - 8);
        var grainMin = -grainCenter;
        var grainMax = (256 << (bitDepth - 8)) - 1 - grainCenter;

        int Random(int bits)
        {
            var r = random;
            var bit = ((r >> 0) ^ (r >> 1) ^ (r >> 3) ^ (r >> 12)) & 1;
            r = (r >> 1) | (bit << 15);
            random = r;
            return (r >> (16 - bits)) & ((1 << bits) - 1);
        }

        var gaussian = Av1Tables.GaussianSequence;
        var lumaGrain = new int[73 * 82];
        var shift = 12 - bitDepth + g.GrainScaleShift;
        for (var y = 0; y < 73; y++)
            for (var x = 0; x < 82; x++)
                lumaGrain[y * 82 + x] = g.NumYPoints > 0 ? Round2(gaussian[Random(11)], shift) : 0;
        var arShift = g.ArCoeffShiftMinus6 + 6;
        var lag = g.ArCoeffLag;
        for (var y = 3; y < 73; y++)
        {
            for (var x = 3; x < 82 - 3; x++)
            {
                var s = 0;
                var pos = 0;
                for (var deltaRow = -lag; deltaRow <= 0; deltaRow++)
                {
                    for (var deltaCol = -lag; deltaCol <= lag; deltaCol++)
                    {
                        if (deltaRow == 0 && deltaCol == 0) break;
                        var c = g.ArCoeffsYPlus128[pos] - 128;
                        s += lumaGrain[(y + deltaRow) * 82 + x + deltaCol] * c;
                        pos++;
                    }
                }
                lumaGrain[y * 82 + x] = Math.Clamp(lumaGrain[y * 82 + x] + Round2(s, arShift), grainMin, grainMax);
            }
        }

        var chromaW = subX == 1 ? 44 : 82;
        var chromaH = subY == 1 ? 38 : 73;
        var cbGrain = new int[chromaH * chromaW];
        var crGrain = new int[chromaH * chromaW];
        if (!mono)
        {
            random = g.GrainSeed ^ 0xb524;
            for (var y = 0; y < chromaH; y++)
                for (var x = 0; x < chromaW; x++)
                    cbGrain[y * chromaW + x] = g.NumCbPoints > 0 || g.ChromaScalingFromLuma ? Round2(gaussian[Random(11)], shift) : 0;
            random = g.GrainSeed ^ 0x49d8;
            for (var y = 0; y < chromaH; y++)
                for (var x = 0; x < chromaW; x++)
                    crGrain[y * chromaW + x] = g.NumCrPoints > 0 || g.ChromaScalingFromLuma ? Round2(gaussian[Random(11)], shift) : 0;
            for (var y = 3; y < chromaH; y++)
            {
                for (var x = 3; x < chromaW - 3; x++)
                {
                    var s0 = 0;
                    var s1 = 0;
                    var pos = 0;
                    for (var deltaRow = -lag; deltaRow <= 0; deltaRow++)
                    {
                        for (var deltaCol = -lag; deltaCol <= lag; deltaCol++)
                        {
                            var c0 = g.ArCoeffsCbPlus128[pos] - 128;
                            var c1 = g.ArCoeffsCrPlus128[pos] - 128;
                            if (deltaRow == 0 && deltaCol == 0)
                            {
                                if (g.NumYPoints > 0)
                                {
                                    var luma = 0;
                                    var lumaX = ((x - 3) << subX) + 3;
                                    var lumaY = ((y - 3) << subY) + 3;
                                    for (var i = 0; i <= subY; i++)
                                        for (var j = 0; j <= subX; j++)
                                            luma += lumaGrain[(lumaY + i) * 82 + lumaX + j];
                                    luma = Round2(luma, subX + subY);
                                    s0 += luma * c0;
                                    s1 += luma * c1;
                                }
                                break;
                            }
                            s0 += cbGrain[(y + deltaRow) * chromaW + x + deltaCol] * c0;
                            s1 += crGrain[(y + deltaRow) * chromaW + x + deltaCol] * c1;
                            pos++;
                        }
                    }
                    cbGrain[y * chromaW + x] = Math.Clamp(cbGrain[y * chromaW + x] + Round2(s0, arShift), grainMin, grainMax);
                    crGrain[y * chromaW + x] = Math.Clamp(crGrain[y * chromaW + x] + Round2(s1, arShift), grainMin, grainMax);
                }
            }
        }

        // Scaling lookup tables.
        var scalingLut = new int[3, 256];
        for (var plane = 0; plane < numPlanes; plane++)
        {
            int numPoints;
            int[] xs, ys;
            if (plane == 0 || g.ChromaScalingFromLuma)
            {
                numPoints = g.NumYPoints;
                xs = g.PointYValue;
                ys = g.PointYScaling;
            }
            else if (plane == 1)
            {
                numPoints = g.NumCbPoints;
                xs = g.PointCbValue;
                ys = g.PointCbScaling;
            }
            else
            {
                numPoints = g.NumCrPoints;
                xs = g.PointCrValue;
                ys = g.PointCrScaling;
            }
            if (numPoints == 0) continue;
            for (var x = 0; x < xs[0]; x++) scalingLut[plane, x] = ys[0];
            for (var i = 0; i < numPoints - 1; i++)
            {
                var deltaY = ys[i + 1] - ys[i];
                var deltaX = xs[i + 1] - xs[i];
                if (deltaX <= 0) throw new ImageDecodeException("Invalid AV1 film grain scaling points.");
                var delta = deltaY * ((65536 + (deltaX >> 1)) / deltaX);
                for (var x = 0; x < deltaX; x++)
                    scalingLut[plane, xs[i] + x] = ys[i] + ((x * delta + 32768) >> 16);
            }
            for (var x = xs[numPoints - 1]; x < 256; x++) scalingLut[plane, x] = ys[numPoints - 1];
        }

        int ScaleLut(int plane, int index)
        {
            var sh = bitDepth - 8;
            var x = index >> sh;
            var rem = index - (x << sh);
            if (bitDepth == 8 || x == 255) return scalingLut[plane, x];
            var start = scalingLut[plane, x];
            var end = scalingLut[plane, x + 1];
            return start + Round2((end - start) * rem, sh);
        }

        // Noise stripes, 34 luma rows each, blended horizontally.
        var stripes = (h + 1) / 2 / 16 + 1;
        var lumaStripeW = w + 34 * 2;
        var noiseStripes = new int[numPlanes][];
        var stripeW = new int[numPlanes];
        var stripeH = new int[numPlanes];
        for (var plane = 0; plane < numPlanes; plane++)
        {
            var psx = plane > 0 ? subX : 0;
            var psy = plane > 0 ? subY : 0;
            stripeW[plane] = (lumaStripeW >> psx) + 34;
            stripeH[plane] = 34 >> psy;
            noiseStripes[plane] = new int[stripes * stripeH[plane] * stripeW[plane]];
        }
        var lumaNum = 0;
        for (var y = 0; y < (h + 1) / 2; y += 16)
        {
            random = g.GrainSeed;
            random ^= ((lumaNum * 37 + 178) & 255) << 8;
            random ^= (lumaNum * 173 + 105) & 255;
            for (var x = 0; x < (w + 1) / 2; x += 16)
            {
                var rand = Random(8);
                var offsetX = rand >> 4;
                var offsetY = rand & 15;
                for (var plane = 0; plane < numPlanes; plane++)
                {
                    var planeSubX = plane > 0 ? subX : 0;
                    var planeSubY = plane > 0 ? subY : 0;
                    var planeOffsetX = planeSubX != 0 ? 6 + offsetX : 9 + offsetX * 2;
                    var planeOffsetY = planeSubY != 0 ? 6 + offsetY : 9 + offsetY * 2;
                    var grain = plane == 0 ? lumaGrain : plane == 1 ? cbGrain : crGrain;
                    var grainW = plane == 0 ? 82 : chromaW;
                    var stripe = noiseStripes[plane];
                    var sw = stripeW[plane];
                    var baseIdx = lumaNum * stripeH[plane] * sw;
                    for (var i = 0; i < 34 >> planeSubY; i++)
                    {
                        for (var j = 0; j < 34 >> planeSubX; j++)
                        {
                            var gv = grain[(planeOffsetY + i) * grainW + planeOffsetX + j];
                            if (planeSubX == 0)
                            {
                                var idx = baseIdx + i * sw + x * 2 + j;
                                if (j < 2 && g.OverlapFlag && x > 0)
                                {
                                    var old = stripe[idx];
                                    gv = j == 0 ? old * 27 + gv * 17 : old * 17 + gv * 27;
                                    gv = Math.Clamp(Round2(gv, 5), grainMin, grainMax);
                                }
                                stripe[idx] = gv;
                            }
                            else
                            {
                                var idx = baseIdx + i * sw + x + j;
                                if (j == 0 && g.OverlapFlag && x > 0)
                                {
                                    var old = stripe[idx];
                                    gv = old * 23 + gv * 22;
                                    gv = Math.Clamp(Round2(gv, 5), grainMin, grainMax);
                                }
                                stripe[idx] = gv;
                            }
                        }
                    }
                }
            }
            lumaNum++;
        }

        int Noise(int plane, int y, int x)
        {
            var planeSubY = plane > 0 ? subY : 0;
            var num = y >> (5 - planeSubY);
            var i = y - (num << (5 - planeSubY));
            var sw = stripeW[plane];
            var sh = stripeH[plane];
            var stripe = noiseStripes[plane];
            var gv = stripe[num * sh * sw + i * sw + x];
            if (planeSubY == 0)
            {
                if (i < 2 && num > 0 && g.OverlapFlag)
                {
                    var old = stripe[(num - 1) * sh * sw + (i + 32) * sw + x];
                    gv = i == 0 ? old * 27 + gv * 17 : old * 17 + gv * 27;
                    gv = Math.Clamp(Round2(gv, 5), grainMin, grainMax);
                }
            }
            else if (i < 1 && num > 0 && g.OverlapFlag)
            {
                var old = stripe[(num - 1) * sh * sw + (i + 16) * sw + x];
                gv = old * 23 + gv * 22;
                gv = Math.Clamp(Round2(gv, 5), grainMin, grainMax);
            }
            return gv;
        }

        int minValue, maxLuma, maxChroma;
        if (g.ClipToRestrictedRange)
        {
            minValue = 16 << (bitDepth - 8);
            maxLuma = 235 << (bitDepth - 8);
            maxChroma = image.Seq.MatrixCoefficients == 0 ? maxLuma : 240 << (bitDepth - 8);
        }
        else
        {
            minValue = 0;
            maxLuma = (256 << (bitDepth - 8)) - 1;
            maxChroma = maxLuma;
        }
        var scalingShift = g.GrainScalingMinus8 + 8;
        var maxPixel = (1 << bitDepth) - 1;
        var outY = image.Planes[0];
        if (numPlanes > 1)
        {
            var outU = image.Planes[1];
            var outV = image.Planes[2];
            for (var y = 0; y < (h + subY) >> subY; y++)
            {
                for (var x = 0; x < (w + subX) >> subX; x++)
                {
                    var lumaX = x << subX;
                    var lumaY = y << subY;
                    var lumaNextX = Math.Min(lumaX + 1, w - 1);
                    var averageLuma = subX != 0
                        ? Round2(outY.Data[lumaY * outY.Stride + lumaX] + outY.Data[lumaY * outY.Stride + lumaNextX], 1)
                        : outY.Data[lumaY * outY.Stride + lumaX];
                    if (g.NumCbPoints > 0 || g.ChromaScalingFromLuma)
                    {
                        int orig = outU.Data[y * outU.Stride + x];
                        int merged;
                        if (g.ChromaScalingFromLuma)
                        {
                            merged = averageLuma;
                        }
                        else
                        {
                            var combined = averageLuma * (g.CbLumaMult - 128) + orig * (g.CbMult - 128);
                            merged = Math.Clamp((combined >> 6) + ((g.CbOffset - 256) << (bitDepth - 8)), 0, maxPixel);
                        }
                        var noise = Round2(ScaleLut(1, merged) * Noise(1, y, x), scalingShift);
                        outU.Data[y * outU.Stride + x] = (ushort)Math.Clamp(orig + noise, minValue, maxChroma);
                    }
                    if (g.NumCrPoints > 0 || g.ChromaScalingFromLuma)
                    {
                        int orig = outV.Data[y * outV.Stride + x];
                        int merged;
                        if (g.ChromaScalingFromLuma)
                        {
                            merged = averageLuma;
                        }
                        else
                        {
                            var combined = averageLuma * (g.CrLumaMult - 128) + orig * (g.CrMult - 128);
                            merged = Math.Clamp((combined >> 6) + ((g.CrOffset - 256) << (bitDepth - 8)), 0, maxPixel);
                        }
                        var noise = Round2(ScaleLut(2, merged) * Noise(2, y, x), scalingShift);
                        outV.Data[y * outV.Stride + x] = (ushort)Math.Clamp(orig + noise, minValue, maxChroma);
                    }
                }
            }
        }
        if (g.NumYPoints > 0)
        {
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    int orig = outY.Data[y * outY.Stride + x];
                    var noise = Round2(ScaleLut(0, orig) * Noise(0, y, x), scalingShift);
                    outY.Data[y * outY.Stride + x] = (ushort)Math.Clamp(orig + noise, minValue, maxLuma);
                }
            }
        }
    }

    private static int Round2(int x, int n) => n == 0 ? x : (x + (1 << (n - 1))) >> n;
}
