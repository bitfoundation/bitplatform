// Image decoding: the raster cases needed to produce displayable pixels.

using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;

namespace Bit.BlazorUI;

/// <summary>
/// Decodes an image XObject (or inline image) into a browser-displayable data
/// URI. JPEG (DCTDecode) streams are passed through directly; other images are
/// decoded to RGBA and PNG-encoded.
/// </summary>
internal static class BitPdfImage
{
    public static string? BuildDataUri(BitPdfStream stream, IBitPdfXRef xref, BitPdfDict? resources, (byte R, byte G, byte B) fillColor, bool inline = false)
    {
        BitPdfDict dict = stream.Dict!;
        int width = GetInt(dict, "Width", "W");
        int height = GetInt(dict, "Height", "H");
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var filterNames = GetFilterNames(dict);
        if (filterNames.Contains("JPXDecode"))
        {
            return null; // JPEG2000 not supported by browsers
        }
        if (filterNames.Contains("JBIG2Decode"))
        {
            // No JBIG2 decoder yet: skip rather than read the compressed bytes as
            // 1-bpc pixels (which renders as noise). A real decoder is Phase 6.
            return null;
        }

        bool isJpeg = filterNames.Contains("DCTDecode") || filterNames.Contains("DCT");

        byte[] data;
        try
        {
            data = BitPdfStreamDecoder.Decode(stream, inline);
        }
        catch
        {
            return null;
        }

        if (isJpeg)
        {
            // Browsers decode plain RGB/YCbCr JPEG correctly, so pass those through.
            // But CMYK JPEGs render wrong in browsers, and /SMask, /Mask and a
            // non-default /Decode all need the pixels in hand — decode in C# then.
            BitPdfColorSpace jcs = BitPdfColorSpace.Create(dict.Get("ColorSpace", "CS"), xref, resources);
            bool needsCSharp = jcs.Components == 4
                || dict.Get("SMask") is BitPdfStream
                || xref.FetchIfRef(dict.Get("Mask")) is not null
                || dict.Get("Decode", "D") is List<object?>;

            if (needsCSharp && BitPdfJpegDecoder.Decode(data) is { } jpeg)
            {
                byte[]? jrgba = JpegToRgba(jpeg, dict, xref);
                if (jrgba is not null)
                {
                    ApplySoftMask(dict, xref, resources, jpeg.Width, jpeg.Height, jrgba);
                    if (xref.FetchIfRef(dict.Get("Mask")) is BitPdfStream jstencil)
                    {
                        ApplyStencilMask(jstencil, xref, jpeg.Width, jpeg.Height, jrgba);
                    }
                    byte[] jpng = BitPdfPngEncoder.EncodeRgba(jpeg.Width, jpeg.Height, jrgba);
                    return "data:image/png;base64," + Convert.ToBase64String(jpng);
                }
            }

            // StreamDecoder passes DCT data through, so `data` is the JPEG stream.
            return "data:image/jpeg;base64," + Convert.ToBase64String(data);
        }

        if (filterNames.Contains("CCITTFaxDecode") || filterNames.Contains("CCF"))
        {
            try
            {
                data = BitPdfCcittFaxDecoder.Decode(data, ReadCcittParams(dict, xref, width, height));
            }
            catch
            {
                return null;
            }
        }

        bool imageMask = IsTrue(dict.Get("ImageMask", "IM"));
        int bpc = imageMask ? 1 : GetInt(dict, "BitsPerComponent", "BPC");
        if (bpc <= 0)
        {
            bpc = 8;
        }

        // Compute the RGBA size in `long` so a hostile Width×Height cannot wrap a
        // 32-bit multiply into a small allocation, and reject absurd images
        // outside a generous pixel budget (~268 M px). Wrap the pixel-producing
        // work in try/catch so one bad image is skipped, not fatal to the page.
        long pixels = (long)width * height;
        if (pixels > 268_435_456L)
        {
            return null;
        }

        try
        {
            var rgba = new byte[pixels * 4];

            if (imageMask)
            {
                DecodeImageMask(data, width, height, dict, fillColor, rgba);
            }
            else
            {
                BitPdfColorSpace cs = BitPdfColorSpace.Create(dict.Get("ColorSpace", "CS"), xref, resources);
                double[]? decode = ReadDecodeArray(dict.Get("Decode", "D"), xref);
                // /Mask as an array is colour-key masking (per-component sample
                // ranges become transparent); as a stream it is a stencil mask.
                object? maskObj = xref.FetchIfRef(dict.Get("Mask"));
                int[]? colorKey = maskObj is List<object?> ck ? ReadColorKey(ck, cs.Components) : null;
                DecodeColorImage(data, width, height, bpc, cs, rgba, decode, colorKey);
                ApplySoftMask(dict, xref, resources, width, height, rgba);
                if (maskObj is BitPdfStream stencil)
                {
                    ApplyStencilMask(stencil, xref, width, height, rgba);
                }
            }

            byte[] png = BitPdfPngEncoder.EncodeRgba(width, height, rgba);
            return "data:image/png;base64," + Convert.ToBase64String(png);
        }
        catch
        {
            return null;
        }
    }

    private static void DecodeImageMask(byte[] data, int width, int height, BitPdfDict dict,
        (byte R, byte G, byte B) fill, byte[] rgba)
    {
        // Default Decode [0 1]: sample 0 paints, 1 is transparent.
        bool invert = false;
        if (dict.Get("Decode", "D") is List<object?> dec && dec.Count >= 2 && dec[0] is double d0 && d0 == 1)
        {
            invert = true;
        }

        int rowBytes = (width + 7) / 8;
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * rowBytes;
            for (int x = 0; x < width; x++)
            {
                int bytePos = rowStart + (x >> 3);
                int bit = bytePos < data.Length ? (data[bytePos] >> (7 - (x & 7))) & 1 : 1;
                bool paint = invert ? bit == 1 : bit == 0;
                int p = (y * width + x) * 4;
                rgba[p] = fill.R;
                rgba[p + 1] = fill.G;
                rgba[p + 2] = fill.B;
                rgba[p + 3] = (byte)(paint ? 255 : 0);
            }
        }
    }

    private static void DecodeColorImage(byte[] data, int width, int height, int bpc,
        BitPdfColorSpace cs, byte[] rgba, double[]? decode, int[]? colorKey = null)
    {
        int nComps = cs.Components;
        // 64-bit product: width alone can approach the pixel budget (~268 M) while
        // nComps * bpc pushes the bit count past int.MaxValue for a malicious image,
        // silently wrapping rowBytes negative. The checked narrowing turns a genuinely
        // oversized row into a catchable overflow (the caller skips the bad image).
        int rowBytes = checked((int)(((long)width * nComps * bpc + 7) / 8));

        // Fast path A — single-component spaces at <=8 bpc. Every output pixel is a
        // pure function of one sample value, of which there are at most 256, so bake
        // an RGBA lookup table once (one GetRgb per distinct sample) and collapse the
        // per-pixel work to a single table read + 32-bit store. This removes the
        // per-pixel virtual GetRgb dispatch for the Gray / Indexed / 1-colorant
        // Separation cases — and for Indexed, lifts its per-pixel palette scaling and
        // double[] allocation out of the hot loop entirely.
        if (nComps == 1 && bpc <= 8)
        {
            uint[] lut = BuildSampleLut(cs, bpc, decode, colorKey);
            DecodeViaSampleLut(data, width, height, bpc, rowBytes, lut, rgba);
            return;
        }

        // Fast path B — 8-bpc DeviceRGB (ICCBased-N3 / CalRGB resolve to the same
        // singleton) with no /Decode or colour-key mask is already packed RGB bytes.
        // The scalar path round-trips each sample through /255 then *255 and a virtual
        // call; instead widen RGB->RGBA directly, SIMD-accelerated where available.
        if (bpc == 8 && decode is null && colorKey is null && ReferenceEquals(cs, BitPdfColorSpace.Rgb))
        {
            DecodeDeviceRgb8(data, width, height, rgba);
            return;
        }

        DecodeColorImageGeneric(data, width, height, bpc, nComps, cs, rgba, decode, colorKey, rowBytes);
    }

    // The general per-component path: reads each sample, applies /Decode or colour-key
    // masking, and converts through the colour space. Handles the multi-component
    // (RGB-with-Decode, CMYK, Lab, DeviceN) and high-bit-depth cases the fast paths
    // above deliberately skip.
    private static void DecodeColorImageGeneric(byte[] data, int width, int height, int bpc,
        int nComps, BitPdfColorSpace cs, byte[] rgba, double[]? decode, int[]? colorKey, int rowBytes)
    {
        bool indexed = cs is BitPdfIndexedColorSpace;
        double maxVal = (1 << bpc) - 1;
        var comps = new double[nComps];

        // A /Decode entry must supply a [min max] pair per component to be usable.
        bool hasDecode = decode is not null && decode.Length >= nComps * 2;
        bool hasColorKey = colorKey is not null && colorKey.Length >= nComps * 2;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * rowBytes;
            int bitPos = 0;
            for (int x = 0; x < width; x++)
            {
                bool masked = hasColorKey;
                for (int c = 0; c < nComps; c++)
                {
                    int sample = ReadBits(data, rowStart, bitPos, bpc);
                    bitPos += bpc;
                    // Colour-key masking compares the raw sample against the
                    // per-component range; a pixel inside every range is masked.
                    if (hasColorKey && (sample < colorKey![c * 2] || sample > colorKey[c * 2 + 1]))
                    {
                        masked = false;
                    }
                    if (hasDecode)
                    {
                        // Map the raw sample into [Dmin, Dmax] (PDF §8.9.5.2). For
                        // indexed spaces this yields an index; for others, a value
                        // in the base space's component range.
                        double dmin = decode![c * 2];
                        double dmax = decode[c * 2 + 1];
                        comps[c] = dmin + sample * (dmax - dmin) / maxVal;
                    }
                    else
                    {
                        comps[c] = indexed ? sample : sample / maxVal;
                    }
                }
                var (r, g, b) = cs.GetRgb(comps);
                int p = (y * width + x) * 4;
                rgba[p] = r;
                rgba[p + 1] = g;
                rgba[p + 2] = b;
                rgba[p + 3] = (byte)(masked ? 0 : 255);
            }
        }
    }

    // Bakes an RGBA-per-sample lookup for a single-component space (bpc <= 8, so at
    // most 256 entries). Mirrors the general path's /Decode, indexed and colour-key
    // handling exactly, evaluated once per sample value instead of once per pixel.
    // Each entry is packed little-endian (R | G<<8 | B<<16 | A<<24) so it can be
    // stored to the RGBA buffer as one 32-bit word.
    private static uint[] BuildSampleLut(BitPdfColorSpace cs, int bpc, double[]? decode, int[]? colorKey)
    {
        int count = 1 << bpc;
        double maxVal = count - 1;
        bool indexed = cs is BitPdfIndexedColorSpace;
        bool hasDecode = decode is not null && decode.Length >= 2;
        bool hasColorKey = colorKey is not null && colorKey.Length >= 2;
        var comps = new double[1];
        var lut = new uint[count];

        for (int s = 0; s < count; s++)
        {
            if (hasDecode)
            {
                comps[0] = decode![0] + s * (decode[1] - decode[0]) / maxVal;
            }
            else
            {
                comps[0] = indexed ? s : s / maxVal;
            }
            var (r, g, b) = cs.GetRgb(comps);
            int a = hasColorKey && s >= colorKey![0] && s <= colorKey[1] ? 0 : 255;
            lut[s] = (uint)(r | (g << 8) | (b << 16) | (a << 24));
        }
        return lut;
    }

    private static void DecodeViaSampleLut(byte[] data, int width, int height, int bpc,
        int rowBytes, uint[] lut, byte[] rgba)
    {
        // The packed LUT words are little-endian RGBA, so on a little-endian runtime
        // (every target here: Blazor WASM, x86, ARM) the fast path is a straight
        // 32-bit store per pixel over the buffer reinterpreted as uint.
        if (BitConverter.IsLittleEndian)
        {
            Span<uint> dst = MemoryMarshal.Cast<byte, uint>(rgba.AsSpan());
            int srcLen = data.Length;
            if (bpc == 8)
            {
                for (int y = 0; y < height; y++)
                {
                    int rowStart = y * rowBytes;
                    int o = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = rowStart + x;
                        // Out-of-range samples read as 0, matching ReadBits' bounds guard.
                        dst[o + x] = idx < srcLen ? lut[data[idx]] : lut[0];
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    int rowStart = y * rowBytes;
                    int bitPos = 0;
                    int o = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        dst[o + x] = lut[ReadBits(data, rowStart, bitPos, bpc)];
                        bitPos += bpc;
                    }
                }
            }
            return;
        }

        // Big-endian fallback: unpack each LUT word into bytes explicitly.
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * rowBytes;
            int bitPos = 0;
            for (int x = 0; x < width; x++)
            {
                uint px = lut[ReadBits(data, rowStart, bitPos, bpc)];
                bitPos += bpc;
                int p = (y * width + x) * 4;
                rgba[p] = (byte)px;
                rgba[p + 1] = (byte)(px >> 8);
                rgba[p + 2] = (byte)(px >> 16);
                rgba[p + 3] = (byte)(px >> 24);
            }
        }
    }

    // RGB (3 src bytes) -> RGBA (4 dst bytes) expansion mask: each pixel's R,G,B move
    // into the low three lanes of its output word; the 0x80 index zeroes the alpha
    // lane, which the alpha vector then fills with 0xFF. Four pixels per 128-bit op.
    private static readonly Vector128<byte> RgbToRgbaShuffle = Vector128.Create(
        (byte)0, 1, 2, 0x80, 3, 4, 5, 0x80, 6, 7, 8, 0x80, 9, 10, 11, 0x80);
    private static readonly Vector128<byte> RgbaAlpha = Vector128.Create(
        (byte)0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255);

    private static void DecodeDeviceRgb8(byte[] data, int width, int height, byte[] rgba)
    {
        // rowBytes == width*3 for 8-bpc RGB (no row padding), so the samples are one
        // contiguous run and rows can be ignored entirely.
        int totalPixels = width * height;
        int srcLen = data.Length;
        int i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            // Each iteration loads 16 bytes (needs the full 16 in bounds), shuffles the
            // first 12 into 4 RGBA pixels, OR-s in opaque alpha, and stores 16 bytes.
            for (; i + 4 <= totalPixels && i * 3 + 16 <= srcLen; i += 4)
            {
                Vector128<byte> src = Vector128.LoadUnsafe(ref data[i * 3]);
                Vector128<byte> pixels = Vector128.Shuffle(src, RgbToRgbaShuffle) | RgbaAlpha;
                pixels.StoreUnsafe(ref rgba[i * 4]);
            }
        }

        // Scalar tail — also the whole loop on non-accelerated runtimes and for any
        // pixels whose source bytes run past a truncated stream (read as 0).
        for (; i < totalPixels; i++)
        {
            int s = i * 3;
            int p = i * 4;
            rgba[p] = s < srcLen ? data[s] : (byte)0;
            rgba[p + 1] = s + 1 < srcLen ? data[s + 1] : (byte)0;
            rgba[p + 2] = s + 2 < srcLen ? data[s + 2] : (byte)0;
            rgba[p + 3] = 255;
        }
    }

    private static void ApplySoftMask(BitPdfDict dict, IBitPdfXRef xref, BitPdfDict? resources, int width, int height, byte[] rgba)
    {
        if (dict.Get("SMask") is not BitPdfStream smask || smask.Dict is null)
        {
            return;
        }
        int mw = GetInt(smask.Dict, "Width", "W");
        int mh = GetInt(smask.Dict, "Height", "H");
        int mbpc = GetInt(smask.Dict, "BitsPerComponent", "BPC");
        if (mw <= 0 || mh <= 0 || mbpc <= 0)
        {
            return;
        }

        // The SMask's own bytes need the same image decoding as a base image. We
        // don't yet decode DCT/JPX masks, so applying the raw compressed bytes as
        // alpha would paint noise — leave the image fully opaque instead.
        var maskFilters = GetFilterNames(smask.Dict);
        if (maskFilters.Contains("DCTDecode") || maskFilters.Contains("DCT")
            || maskFilters.Contains("JPXDecode") || maskFilters.Contains("JBIG2Decode"))
        {
            return;
        }

        byte[] mdata;
        try
        {
            mdata = BitPdfStreamDecoder.Decode(smask);
        }
        catch
        {
            return;
        }

        // Honor the mask's own /Decode [1 0] (inverted alpha).
        bool invert = ReadDecodeArray(smask.Dict.Get("Decode", "D"), xref) is { Length: >= 2 } md && md[0] > md[1];

        // /Matte: the base image's colour has been pre-blended against this matte
        // colour, so it must be un-premultiplied as alpha is applied (PDF 32000-1
        // §11.6.5.3). The matte is in the base image's colour space.
        (byte R, byte G, byte B)? matte = null;
        if (smask.Dict.Get("Matte") is List<object?> matteArr && matteArr.Count > 0)
        {
            var cs = BitPdfColorSpace.Create(dict.Get("ColorSpace", "CS"), xref, resources);
            var comps = new double[matteArr.Count];
            for (int i = 0; i < matteArr.Count; i++)
            {
                comps[i] = xref.FetchIfRef(matteArr[i]) is double d ? d : 0;
            }
            if (comps.Length == cs.Components)
            {
                matte = cs.GetRgb(comps);
            }
        }

        double maxVal = (1 << mbpc) - 1;
        int rowBytes = (mw * mbpc + 7) / 8;

        // Horizontal scale is fixed per column, so precompute the source x for each
        // destination x once instead of dividing per pixel. Only worth (and safe) to
        // precompute for reasonably wide rows: a pathological width (up to the pixel
        // budget when height is tiny) would make the width-sized map a ~1 GB allocation
        // on top of the RGBA buffer, so above the threshold each mx is computed inline.
        bool scaleX = mw != width;
        int[]? xMap = scaleX && width <= ScaleMapMaxWidth ? BuildScaleMap(width, mw) : null;

        // The alpha byte is a pure function of the mask sample (sample -> /maxVal ->
        // optional invert -> round to 0..255). For the usual bit depths (<=16) bake
        // that whole chain into a lookup so the hot loop drops its per-pixel divide,
        // round and clamp. The matte path additionally needs the fractional alpha,
        // which stays cheap to derive from the same sample.
        byte[]? alphaLut = mbpc <= 16 ? BuildAlphaLut(mbpc, invert) : null;

        if (matte is null)
        {
            for (int y = 0; y < height; y++)
            {
                // Widen before multiplying by rowBytes: a mask with a huge declared
                // width makes rowBytes large enough that (srcRow * rowBytes) overflows
                // int and wraps negative, which ReadBits would turn into a data[<0]
                // out-of-range read. Clamp past the buffer so ReadBits just yields 0.
                long srcRow = mh == height ? y : (long)y * mh / height;
                long rowStartL = srcRow * rowBytes;
                int rowStart = rowStartL >= mdata.Length ? mdata.Length : (int)rowStartL;
                int o = y * width;
                for (int x = 0; x < width; x++)
                {
                    int mx = xMap is not null ? xMap[x] : (scaleX ? (int)((long)x * mw / width) : x);
                    int sample = ReadBits(mdata, rowStart, mx * mbpc, mbpc);
                    rgba[(o + x) * 4 + 3] = alphaLut is not null
                        ? alphaLut[sample]
                        : AlphaByte(sample / maxVal, invert);
                }
            }
            return;
        }

        // /Matte: the base colour was pre-blended against this matte, so un-premultiply
        // each channel as alpha is applied (PDF 32000-1 §11.6.5.3). Rare, so kept off
        // the fast path above; still uses the precomputed scale map and alpha LUT.
        var m = matte.Value;
        for (int y = 0; y < height; y++)
        {
            // Widen before multiplying by rowBytes so a huge declared mask width can't
            // overflow int to a negative rowStart (see the matte-free path above).
            long srcRow = mh == height ? y : (long)y * mh / height;
            long rowStartL = srcRow * rowBytes;
            int rowStart = rowStartL >= mdata.Length ? mdata.Length : (int)rowStartL;
            for (int x = 0; x < width; x++)
            {
                int mx = xMap is not null ? xMap[x] : (scaleX ? (int)((long)x * mw / width) : x);
                int sample = ReadBits(mdata, rowStart, mx * mbpc, mbpc);
                double alpha = sample / maxVal;
                if (invert)
                {
                    alpha = 1 - alpha;
                }
                int p = (y * width + x) * 4;
                if (alpha > 0)
                {
                    rgba[p] = Unmatte(rgba[p], m.R, alpha);
                    rgba[p + 1] = Unmatte(rgba[p + 1], m.G, alpha);
                    rgba[p + 2] = Unmatte(rgba[p + 2], m.B, alpha);
                }
                rgba[p + 3] = alphaLut is not null
                    ? alphaLut[sample]
                    : (byte)Math.Clamp((int)Math.Round(alpha * 255), 0, 255);
            }
        }
    }

    // Above this destination width the per-column source-x map (4 bytes/entry) is not
    // precomputed — each mx is derived inline instead — so a pathologically wide image
    // cannot force a huge array allocation. 65536 keeps the fast path for any real image.
    private const int ScaleMapMaxWidth = 1 << 16;

    // Maps a destination coordinate to its nearest source coordinate for a mask that
    // differs in size from the base image (matches the inline `i * srcLen / dstLen`).
    private static int[] BuildScaleMap(int dstLen, int srcLen)
    {
        var map = new int[dstLen];
        for (int i = 0; i < dstLen; i++)
        {
            map[i] = (int)((long)i * srcLen / dstLen); // 64-bit product: huge claimed sizes must not overflow
        }
        return map;
    }

    // sample (0..2^bpc-1) -> 8-bit alpha, honouring an inverted /Decode [1 0].
    private static byte[] BuildAlphaLut(int bpc, bool invert)
    {
        int count = 1 << bpc;
        double maxVal = count - 1;
        var lut = new byte[count];
        for (int s = 0; s < count; s++)
        {
            lut[s] = AlphaByte(s / maxVal, invert);
        }
        return lut;
    }

    private static byte AlphaByte(double alpha, bool invert)
    {
        if (invert)
        {
            alpha = 1 - alpha;
        }
        return (byte)Math.Clamp((int)Math.Round(alpha * 255), 0, 255);
    }

    // Recovers a colour component pre-blended against a matte: c = m + (c' - m)/a.
    private static byte Unmatte(byte premultiplied, byte matte, double alpha)
        => (byte)Math.Clamp((int)Math.Round(matte + (premultiplied - matte) / alpha), 0, 255);

    /// <summary>Applies an explicit stencil-mask stream (1-bpc): masked samples
    /// become fully transparent (PDF 32000-1 §8.9.6.3).</summary>
    private static void ApplyStencilMask(BitPdfStream mask, IBitPdfXRef xref, int width, int height, byte[] rgba)
    {
        if (mask.Dict is null)
        {
            return;
        }
        int mw = GetInt(mask.Dict, "Width", "W");
        int mh = GetInt(mask.Dict, "Height", "H");
        if (mw <= 0 || mh <= 0)
        {
            return;
        }

        var filters = GetFilterNames(mask.Dict);
        byte[] mdata;
        try
        {
            mdata = BitPdfStreamDecoder.Decode(mask);
            if (filters.Contains("CCITTFaxDecode") || filters.Contains("CCF"))
            {
                mdata = BitPdfCcittFaxDecoder.Decode(mdata, ReadCcittParams(mask.Dict, xref, mw, mh));
            }
        }
        catch
        {
            return;
        }

        // Default /Decode [0 1]: a 1 bit marks a masked (unpainted) pixel.
        bool maskOnOne = !(ReadRawDecode(mask.Dict) is { Length: >= 2 } d && d[0] == 1);
        int rowBytes = (mw + 7) / 8;
        for (int y = 0; y < height; y++)
        {
            int my = mh == height ? y : y * mh / height;
            int rowStart = my * rowBytes;
            for (int x = 0; x < width; x++)
            {
                int mx = mw == width ? x : x * mw / width;
                int bytePos = rowStart + (mx >> 3);
                int bit = bytePos < mdata.Length ? (mdata[bytePos] >> (7 - (mx & 7))) & 1 : 0;
                bool masked = maskOnOne ? bit == 1 : bit == 0;
                if (masked)
                {
                    rgba[(y * width + x) * 4 + 3] = 0;
                }
            }
        }
    }

    private static double[]? ReadRawDecode(BitPdfDict dict)
    {
        if (dict.Get("Decode", "D") is not List<object?> arr || arr.Count < 2)
        {
            return null;
        }
        var r = new double[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            r[i] = arr[i] is double d ? d : 0;
        }
        return r;
    }

    private static int[]? ReadColorKey(List<object?> arr, int nComps)
    {
        if (arr.Count < nComps * 2)
        {
            return null;
        }
        var key = new int[nComps * 2];
        for (int i = 0; i < key.Length; i++)
        {
            if (arr[i] is not double d)
            {
                return null;
            }
            key[i] = (int)d;
        }
        return key;
    }

    private static int ReadBits(byte[] data, int rowStart, int bitPos, int bpc)
    {
        if (bpc == 8)
        {
            int idx = rowStart + (bitPos >> 3);
            return idx < data.Length ? data[idx] : 0;
        }
        int value = 0;
        for (int i = 0; i < bpc; i++)
        {
            int absBit = bitPos + i;
            int bytePos = rowStart + (absBit >> 3);
            int bit = bytePos < data.Length ? (data[bytePos] >> (7 - (absBit & 7))) & 1 : 0;
            value = (value << 1) | bit;
        }
        return value;
    }

    private static List<string> GetFilterNames(BitPdfDict dict)
    {
        var names = new List<string>();
        object? filter = dict.Get("Filter", "F");
        if (filter is BitPdfName n)
        {
            names.Add(n.Value);
        }
        else if (filter is List<object?> arr)
        {
            foreach (var item in arr)
            {
                if (item is BitPdfName name)
                {
                    names.Add(name.Value);
                }
            }
        }
        return names;
    }

    private static BitPdfCcittParams ReadCcittParams(BitPdfDict dict, IBitPdfXRef xref, int width, int height)
    {
        // /DecodeParms may be a single dictionary or an array (one per filter).
        BitPdfDict? parms = null;
        object? dp = dict.Get("DecodeParms", "DP");
        if (dp is BitPdfDict d)
        {
            parms = d;
        }
        else if (dp is List<object?> arr)
        {
            foreach (var item in arr)
            {
                if (xref.FetchIfRef(item) is BitPdfDict candidate && candidate.Has("K"))
                {
                    parms = candidate;
                    break;
                }
                parms ??= xref.FetchIfRef(item) as BitPdfDict;
            }
        }

        int GetI(string key, int fallback) => parms?.Get(key) is double v ? (int)v : fallback;
        bool GetB(string key) => parms?.Get(key) is bool b && b;

        return new BitPdfCcittParams
        {
            K = GetI("K", 0),
            Columns = GetI("Columns", width > 0 ? width : 1728),
            Rows = GetI("Rows", height),
            BlackIs1 = GetB("BlackIs1"),
            EncodedByteAlign = GetB("EncodedByteAlign"),
            EndOfBlock = parms?.Get("EndOfBlock") is not bool eob || eob,
        };
    }

    private static byte[]? JpegToRgba(BitPdfJpegImage jpeg, BitPdfDict dict, IBitPdfXRef xref)
    {
        int w = jpeg.Width, h = jpeg.Height, nc = jpeg.Components;
        long total = (long)w * h * 4;
        if (total <= 0 || total > int.MaxValue)
        {
            return null;
        }
        var rgba = new byte[total];
        double[]? decode = ReadDecodeArray(dict.Get("Decode", "D"), xref);
        bool hasDecode = decode is not null && decode.Length >= nc * 2;
        var comps = new double[nc];

        for (int i = 0; i < w * h; i++)
        {
            for (int c = 0; c < nc; c++)
            {
                double s = jpeg.Data[i * nc + c] / 255.0;
                if (hasDecode)
                {
                    s = decode![c * 2] + s * (decode[c * 2 + 1] - decode[c * 2]);
                }
                comps[c] = s;
            }

            (byte r, byte g, byte b) = nc switch
            {
                1 => (Norm(comps[0]), Norm(comps[0]), Norm(comps[0])),
                3 => (Norm(comps[0]), Norm(comps[1]), Norm(comps[2])),
                4 => BitPdfColorSpace.CmykToRgb(comps[0], comps[1], comps[2], comps[3]),
                _ => (Norm(comps.Length > 0 ? comps[0] : 0), (byte)0, (byte)0),
            };
            int p = i * 4;
            rgba[p] = r;
            rgba[p + 1] = g;
            rgba[p + 2] = b;
            rgba[p + 3] = 255;
        }
        return rgba;
    }

    private static byte Norm(double v) => (byte)Math.Clamp((int)Math.Round(v * 255), 0, 255);

    private static double[]? ReadDecodeArray(object? value, IBitPdfXRef xref)
    {
        if (xref.FetchIfRef(value) is not List<object?> arr || arr.Count < 2)
        {
            return null;
        }
        var result = new double[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            if (xref.FetchIfRef(arr[i]) is not double d)
            {
                return null;
            }
            result[i] = d;
        }
        return result;
    }

    private static int GetInt(BitPdfDict dict, string key1, string key2)
        => dict.Get(key1, key2) is double d ? (int)d : 0;

    private static bool IsTrue(object? value) => value is bool b && b;
}
