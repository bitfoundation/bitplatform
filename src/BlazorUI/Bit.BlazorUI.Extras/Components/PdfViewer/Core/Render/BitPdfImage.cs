// Image decoding: the raster cases needed to produce displayable pixels.


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
        bool indexed = cs is BitPdfIndexedColorSpace;
        double maxVal = (1 << bpc) - 1;
        int rowBits = width * nComps * bpc;
        int rowBytes = (rowBits + 7) / 8;
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
        for (int y = 0; y < height; y++)
        {
            int my = mh == height ? y : y * mh / height;
            int rowStart = my * rowBytes;
            for (int x = 0; x < width; x++)
            {
                int mx = mw == width ? x : x * mw / width;
                int sample = ReadBits(mdata, rowStart, mx * mbpc, mbpc);
                double alpha = sample / maxVal;
                if (invert)
                {
                    alpha = 1 - alpha;
                }
                int p = (y * width + x) * 4;
                if (matte is { } m && alpha > 0)
                {
                    rgba[p] = Unmatte(rgba[p], m.R, alpha);
                    rgba[p + 1] = Unmatte(rgba[p + 1], m.G, alpha);
                    rgba[p + 2] = Unmatte(rgba[p + 2], m.B, alpha);
                }
                rgba[p + 3] = (byte)Math.Clamp((int)Math.Round(alpha * 255), 0, 255);
            }
        }
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
