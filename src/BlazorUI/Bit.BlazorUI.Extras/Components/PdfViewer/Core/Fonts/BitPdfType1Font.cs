// Type1 (PostScript) font-program parsing and charstring interpretation.
// Embedded Type1 fonts (/FontFile) can't be loaded by a browser directly, so we
// parse the program, interpret each glyph's Type1 charstring into an absolute
// outline, and hand the outlines to CffFontWriter to build an OpenType/CFF font.
//
// Hints are intentionally dropped: unhinted outlines render with the correct
// shape (only rasterization crispness at tiny sizes is affected), which lets us
// skip the intricate hint-replacement machinery.

namespace Bit.BlazorUI;

internal sealed class BitPdfType1Font
{
    public double[] FontMatrix { get; private set; } = [0.001, 0, 0, 0.001, 0, 0];

    /// <summary>Built-in encoding: code (0-255) → glyph name.</summary>
    public string[] Encoding { get; } = new string[256];

    /// <summary>Glyph name → raw (decrypted) Type1 charstring.</summary>
    public Dictionary<string, byte[]> CharStrings { get; } = new();

    private readonly List<byte[]> _subrs = new();
    private int _lenIV = 4;

    /// <summary>Parses a Type1 font program, or returns null if it isn't one.</summary>
    public static BitPdfType1Font? Parse(byte[] data)
    {
        try
        {
            byte[] flat = IsPfb(data) ? DePfb(data) : data;
            int eexec = IndexOf(flat, "eexec"u8);
            if (eexec < 0)
            {
                return null;
            }

            var font = new BitPdfType1Font();
            byte[] clear = flat[..eexec];
            font.ParseClear(clear);

            int p = eexec + 5;
            while (p < flat.Length && (flat[p] is 0x20 or 0x0A or 0x0D or 0x09))
            {
                p++;
            }
            byte[] enc = flat[p..];
            byte[] priv = DecryptEexec(enc);
            font.ParsePrivate(priv);

            return font.CharStrings.Count > 0 ? font : null;
        }
        catch
        {
            return null;
        }
    }

    // ----- clear-text section: /FontMatrix and /Encoding -----

    private void ParseClear(byte[] clear)
    {
        string text = System.Text.Encoding.Latin1.GetString(clear);

        int fm = text.IndexOf("/FontMatrix", StringComparison.Ordinal);
        if (fm >= 0)
        {
            int lb = text.IndexOf('[', fm);
            int rb = lb >= 0 ? text.IndexOf(']', lb) : -1;
            if (lb >= 0 && rb > lb)
            {
                var nums = text[(lb + 1)..rb].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length >= 6)
                {
                    var m = new double[6];
                    bool ok = true;
                    for (int i = 0; i < 6; i++)
                    {
                        ok &= double.TryParse(nums[i], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out m[i]);
                    }
                    if (ok)
                    {
                        FontMatrix = m;
                    }
                }
            }
        }

        if (text.Contains("/Encoding StandardEncoding def", StringComparison.Ordinal))
        {
            Array.Copy(BitPdfEncodings.Standard, Encoding, 256);
        }
        else
        {
            // Custom encoding: entries look like "dup <code> /<name> put".
            int idx = 0;
            while ((idx = text.IndexOf("dup ", idx, StringComparison.Ordinal)) >= 0)
            {
                idx += 4;
                int sp = text.IndexOf(' ', idx);
                int slash = text.IndexOf('/', idx);
                if (sp < 0 || slash < 0 || slash < sp)
                {
                    continue;
                }
                if (!int.TryParse(text[idx..sp], out int code) || code is < 0 or > 255)
                {
                    continue;
                }
                int nameStart = slash + 1;
                int nameEnd = nameStart;
                while (nameEnd < text.Length && !char.IsWhiteSpace(text[nameEnd]))
                {
                    nameEnd++;
                }
                Encoding[code] = text[nameStart..nameEnd];
            }
        }
    }

    // ----- private section: /lenIV, /Subrs, /CharStrings -----

    private void ParsePrivate(byte[] priv)
    {
        int li = IndexOf(priv, "/lenIV"u8);
        if (li >= 0)
        {
            int q = li + 6;
            while (q < priv.Length && priv[q] == ' ')
            {
                q++;
            }
            int start = q;
            while (q < priv.Length && priv[q] is >= (byte)'0' and <= (byte)'9')
            {
                q++;
            }
            if (q > start && int.TryParse(System.Text.Encoding.Latin1.GetString(priv, start, q - start), out int lv))
            {
                _lenIV = lv;
            }
        }

        ParseSubrs(priv);
        ParseCharStrings(priv);
    }

    private void ParseSubrs(byte[] priv)
    {
        int s = IndexOf(priv, "/Subrs"u8);
        if (s < 0)
        {
            return;
        }
        // Entries: "dup <index> <len> RD <bytes> NP".
        int p = s;
        while (true)
        {
            int dup = IndexOf(priv, "dup "u8, p);
            if (dup < 0)
            {
                break;
            }
            // Stop once we reach CharStrings.
            int cs = IndexOf(priv, "/CharStrings"u8, s);
            if (cs >= 0 && dup > cs)
            {
                break;
            }
            int q = dup + 4;
            int index = ReadInt(priv, ref q);
            SkipSpace(priv, ref q);
            int len = ReadInt(priv, ref q);
            q = SkipToBinaryStart(priv, q); // past RD/-| and one space
            if (q < 0 || q + len > priv.Length)
            {
                p = dup + 4;
                continue;
            }
            byte[] raw = DecryptCharstring(priv, q, len);
            while (_subrs.Count <= index)
            {
                _subrs.Add([]);
            }
            _subrs[index] = raw;
            p = q + len;
        }
    }

    private void ParseCharStrings(byte[] priv)
    {
        int cs = IndexOf(priv, "/CharStrings"u8);
        if (cs < 0)
        {
            return;
        }
        int p = cs;
        while (true)
        {
            // Entries: "/<name> <len> RD <bytes> ND".
            int slash = IndexOf(priv, "/"u8, p + 1);
            if (slash < 0)
            {
                break;
            }
            int q = slash + 1;
            int nameStart = q;
            while (q < priv.Length && !IsWhite(priv[q]))
            {
                q++;
            }
            string name = System.Text.Encoding.Latin1.GetString(priv, nameStart, q - nameStart);
            SkipSpace(priv, ref q);
            if (q >= priv.Length || priv[q] is < (byte)'0' or > (byte)'9')
            {
                p = slash + 1;
                continue;
            }
            int len = ReadInt(priv, ref q);
            int bin = SkipToBinaryStart(priv, q);
            if (bin < 0 || bin + len > priv.Length)
            {
                p = slash + 1;
                continue;
            }
            CharStrings[name] = DecryptCharstring(priv, bin, len);
            p = bin + len;
        }
    }

    // ----- charstring interpretation -----

    /// <summary>Interprets a glyph's charstring into an absolute outline.</summary>
    public BitPdfGlyphOutline? BuildOutline(string glyphName)
    {
        if (!CharStrings.TryGetValue(glyphName, out var cs))
        {
            return null;
        }
        var ctx = new InterpContext();
        try
        {
            Run(cs, ctx);
        }
        catch
        {
            return null;
        }
        if (ctx.Open)
        {
            ctx.Result.Segments.Add(new BitPdfPathSeg { Op = BitPdfPathSeg.Kind.Close });
        }
        ctx.Result.AdvanceWidth = ctx.Width;
        return ctx.Result;
    }

    private sealed class InterpContext
    {
        public readonly BitPdfGlyphOutline Result = new();
        public readonly List<double> Stack = new();
        public readonly List<double> PostScriptStack = new();
        public double X, Y;         // current point (absolute)
        public double Sbx;          // left side bearing
        public double Width;
        public bool Open;           // a contour is in progress
        public int FlexPointCount = -1; // >=0 while collecting flex points
        public readonly List<double> FlexPts = new();
        public bool Done;
    }

    private void Run(byte[] cs, InterpContext ctx)
    {
        int i = 0;
        while (i < cs.Length && !ctx.Done)
        {
            int v = cs[i++];
            if (v >= 32)
            {
                double num;
                if (v <= 246)
                {
                    num = v - 139;
                }
                else if (v <= 250)
                {
                    num = (v - 247) * 256 + cs[i++] + 108;
                }
                else if (v <= 254)
                {
                    num = -(v - 251) * 256 - cs[i++] - 108;
                }
                else
                {
                    num = (cs[i] << 24) | (cs[i + 1] << 16) | (cs[i + 2] << 8) | cs[i + 3];
                    i += 4;
                }
                ctx.Stack.Add(num);
                continue;
            }
            if (!Operator(v, cs, ref i, ctx))
            {
                return; // return/endchar
            }
        }
    }

    private bool Operator(int op, byte[] cs, ref int i, InterpContext ctx)
    {
        var st = ctx.Stack;
        switch (op)
        {
            case 13: // hsbw: sbx wx
                if (st.Count >= 2)
                {
                    ctx.Sbx = st[0];
                    ctx.Width = st[1];
                    ctx.X = ctx.Sbx;
                    ctx.Y = 0;
                }
                st.Clear();
                break;
            case 9: // closepath
                if (ctx.Open)
                {
                    ctx.Result.Segments.Add(new BitPdfPathSeg { Op = BitPdfPathSeg.Kind.Close });
                    ctx.Open = false;
                }
                st.Clear();
                break;
            case 21: // rmoveto: dx dy
                MoveOrFlex(ctx, Get(st, 0), Get(st, 1));
                st.Clear();
                break;
            case 22: // hmoveto: dx
                MoveOrFlex(ctx, Get(st, 0), 0);
                st.Clear();
                break;
            case 4: // vmoveto: dy
                MoveOrFlex(ctx, 0, Get(st, 0));
                st.Clear();
                break;
            case 5: // rlineto: dx dy
                LineTo(ctx, ctx.X + Get(st, 0), ctx.Y + Get(st, 1));
                st.Clear();
                break;
            case 6: // hlineto: dx
                LineTo(ctx, ctx.X + Get(st, 0), ctx.Y);
                st.Clear();
                break;
            case 7: // vlineto: dy
                LineTo(ctx, ctx.X, ctx.Y + Get(st, 0));
                st.Clear();
                break;
            case 8: // rrcurveto: dx1 dy1 dx2 dy2 dx3 dy3
                CurveRel(ctx, Get(st, 0), Get(st, 1), Get(st, 2), Get(st, 3), Get(st, 4), Get(st, 5));
                st.Clear();
                break;
            case 30: // vhcurveto: dy1 dx2 dy2 dx3
                CurveRel(ctx, 0, Get(st, 0), Get(st, 1), Get(st, 2), Get(st, 3), 0);
                st.Clear();
                break;
            case 31: // hvcurveto: dx1 dx2 dy2 dy3
                CurveRel(ctx, Get(st, 0), 0, Get(st, 1), Get(st, 2), 0, Get(st, 3));
                st.Clear();
                break;
            case 1: // hstem
            case 3: // vstem
                st.Clear(); // hints dropped
                break;
            case 10: // callsubr
                if (st.Count >= 1)
                {
                    int idx = (int)st[^1];
                    st.RemoveAt(st.Count - 1);
                    if (idx >= 0 && idx < _subrs.Count)
                    {
                        Run(_subrs[idx], ctx);
                    }
                }
                break;
            case 11: // return
                return false;
            case 14: // endchar
                ctx.Done = true;
                return false;
            case 12: // escape
                return Escape(cs[i++], ctx);
            default:
                st.Clear();
                break;
        }
        return true;
    }

    private bool Escape(int op, InterpContext ctx)
    {
        var st = ctx.Stack;
        switch (op)
        {
            case 12: // div
                if (st.Count >= 2)
                {
                    double b = st[^1], a = st[^2];
                    st.RemoveRange(st.Count - 2, 2);
                    st.Add(b != 0 ? a / b : 0);
                }
                break;
            case 16: // callothersubr
                CallOtherSubr(ctx);
                break;
            case 17: // pop
                ctx.Stack.Add(ctx.PostScriptStack.Count > 0
                    ? Pop(ctx.PostScriptStack)
                    : 0);
                break;
            case 6: // seac (accented char) — not composed; ignore base for now
            case 7: // sbw
            case 0: // dotsection
            case 1: // vstem3
            case 2: // hstem3
                st.Clear();
                break;
            case 33: // setcurrentpoint
                if (st.Count >= 2)
                {
                    ctx.X = st[0];
                    ctx.Y = st[1];
                }
                st.Clear();
                break;
            default:
                st.Clear();
                break;
        }
        return true;
    }

    // OtherSubrs 0-3 implement flex and hint replacement via a stack protocol.
    private void CallOtherSubr(InterpContext ctx)
    {
        var st = ctx.Stack;
        if (st.Count < 2)
        {
            st.Clear();
            return;
        }
        int othersubr = (int)st[^1];
        int nargs = (int)st[^2];
        st.RemoveRange(st.Count - 2, 2);

        var args = new List<double>();
        for (int k = 0; k < nargs && st.Count > 0; k++)
        {
            args.Insert(0, Pop(st));
        }

        switch (othersubr)
        {
            case 1: // flex begin
                ctx.FlexPointCount = 0;
                ctx.FlexPts.Clear();
                break;
            case 2: // flex point (collected via the following rmovetos)
                break;
            case 0: // flex end: emit two curves from the 7 collected reference points
                EmitFlex(ctx);
                ctx.FlexPointCount = -1;
                // othersubr 0 leaves the final x,y on the PS stack for two pops.
                ctx.PostScriptStack.Add(ctx.Y);
                ctx.PostScriptStack.Add(ctx.X);
                break;
            case 3: // hint replacement: leaves the subr# on the PS stack
                ctx.PostScriptStack.Add(args.Count > 0 ? args[0] : 3);
                break;
            default:
                // Unknown othersubr: echo args back for subsequent pops.
                for (int k = args.Count - 1; k >= 0; k--)
                {
                    ctx.PostScriptStack.Add(args[k]);
                }
                break;
        }
    }

    private void MoveOrFlex(InterpContext ctx, double dx, double dy)
    {
        ctx.X += dx;
        ctx.Y += dy;
        if (ctx.FlexPointCount >= 0)
        {
            // During flex, rmovetos collect reference points instead of moving.
            ctx.FlexPts.Add(ctx.X);
            ctx.FlexPts.Add(ctx.Y);
            ctx.FlexPointCount++;
            return;
        }
        if (ctx.Open)
        {
            ctx.Result.Segments.Add(new BitPdfPathSeg { Op = BitPdfPathSeg.Kind.Close });
        }
        ctx.Result.Segments.Add(new BitPdfPathSeg { Op = BitPdfPathSeg.Kind.Move, X1 = ctx.X, Y1 = ctx.Y });
        ctx.Open = true;
    }

    private void EmitFlex(InterpContext ctx)
    {
        // FlexPts holds 7 points: [reference, c1,c2,c3, c4,c5,end] (14 doubles).
        if (ctx.FlexPts.Count < 14)
        {
            return;
        }
        var p = ctx.FlexPts;
        // Points 1..3 form the first curve, 4..6 the second (point 0 is the
        // flex reference and is skipped).
        ctx.Result.Segments.Add(new BitPdfPathSeg
        {
            Op = BitPdfPathSeg.Kind.Curve,
            X1 = p[2], Y1 = p[3], X2 = p[4], Y2 = p[5], X3 = p[6], Y3 = p[7],
        });
        ctx.Result.Segments.Add(new BitPdfPathSeg
        {
            Op = BitPdfPathSeg.Kind.Curve,
            X1 = p[8], Y1 = p[9], X2 = p[10], Y2 = p[11], X3 = p[12], Y3 = p[13],
        });
        ctx.X = p[12];
        ctx.Y = p[13];
    }

    private static void LineTo(InterpContext ctx, double x, double y)
    {
        ctx.X = x;
        ctx.Y = y;
        ctx.Result.Segments.Add(new BitPdfPathSeg { Op = BitPdfPathSeg.Kind.Line, X1 = x, Y1 = y });
    }

    private static void CurveRel(InterpContext ctx, double dx1, double dy1, double dx2, double dy2, double dx3, double dy3)
    {
        double x1 = ctx.X + dx1, y1 = ctx.Y + dy1;
        double x2 = x1 + dx2, y2 = y1 + dy2;
        double x3 = x2 + dx3, y3 = y2 + dy3;
        ctx.X = x3;
        ctx.Y = y3;
        ctx.Result.Segments.Add(new BitPdfPathSeg
        {
            Op = BitPdfPathSeg.Kind.Curve, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, X3 = x3, Y3 = y3,
        });
    }

    private static double Get(List<double> st, int i) => i < st.Count ? st[i] : 0;
    private static double Pop(List<double> st)
    {
        if (st.Count == 0)
        {
            return 0;
        }
        double v = st[^1];
        st.RemoveAt(st.Count - 1);
        return v;
    }

    // ----- decryption + byte helpers -----

    private byte[] DecryptCharstring(byte[] data, int start, int len)
    {
        var outBytes = Decrypt(data, start, len, 4330);
        return _lenIV > 0 && outBytes.Length >= _lenIV ? outBytes[_lenIV..] : outBytes;
    }

    private static byte[] DecryptEexec(byte[] enc)
    {
        // eexec may be binary or ASCII-hex; detect hex.
        byte[] bin = LooksHex(enc) ? HexDecode(enc) : enc;
        byte[] dec = Decrypt(bin, 0, bin.Length, 55665);
        return dec.Length >= 4 ? dec[4..] : dec;
    }

    private static byte[] Decrypt(byte[] data, int start, int len, ushort r0)
    {
        const ushort c1 = 52845, c2 = 22719;
        ushort r = r0;
        var outBytes = new byte[len];
        for (int i = 0; i < len; i++)
        {
            byte cipher = data[start + i];
            outBytes[i] = (byte)(cipher ^ (r >> 8));
            r = (ushort)((cipher + r) * c1 + c2);
        }
        return outBytes;
    }

    private static bool LooksHex(byte[] d)
    {
        int n = 0;
        for (int i = 0; i < d.Length && n < 4; i++)
        {
            byte b = d[i];
            if (IsWhite(b))
            {
                continue;
            }
            bool hex = b is >= (byte)'0' and <= (byte)'9' or >= (byte)'a' and <= (byte)'f' or >= (byte)'A' and <= (byte)'F';
            if (!hex)
            {
                return false;
            }
            n++;
        }
        return n > 0;
    }

    private static byte[] HexDecode(byte[] d)
    {
        var outBytes = new List<byte>(d.Length / 2);
        int hi = -1;
        foreach (byte b in d)
        {
            int v = b switch
            {
                >= (byte)'0' and <= (byte)'9' => b - '0',
                >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
                >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
                _ => -1,
            };
            if (v < 0)
            {
                continue;
            }
            if (hi < 0)
            {
                hi = v;
            }
            else
            {
                outBytes.Add((byte)((hi << 4) | v));
                hi = -1;
            }
        }
        return outBytes.ToArray();
    }

    private static bool IsPfb(byte[] d) => d.Length > 6 && d[0] == 0x80 && d[1] == 0x01;

    private static byte[] DePfb(byte[] d)
    {
        var outBytes = new List<byte>(d.Length);
        int p = 0;
        while (p + 6 <= d.Length && d[p] == 0x80)
        {
            int type = d[p + 1];
            if (type == 3)
            {
                break;
            }
            int len = d[p + 2] | (d[p + 3] << 8) | (d[p + 4] << 16) | (d[p + 5] << 24);
            p += 6;
            if (p + len > d.Length)
            {
                break;
            }
            for (int i = 0; i < len; i++)
            {
                outBytes.Add(d[p + i]);
            }
            p += len;
        }
        return outBytes.ToArray();
    }

    private static int IndexOf(byte[] hay, ReadOnlySpan<byte> needle, int from = 0)
    {
        for (int i = Math.Max(0, from); i + needle.Length <= hay.Length; i++)
        {
            bool ok = true;
            for (int k = 0; k < needle.Length; k++)
            {
                if (hay[i + k] != needle[k])
                {
                    ok = false;
                    break;
                }
            }
            if (ok)
            {
                return i;
            }
        }
        return -1;
    }

    private static bool IsWhite(byte b) => b is 0x20 or 0x09 or 0x0A or 0x0C or 0x0D or 0x00;
    private static void SkipSpace(byte[] d, ref int q)
    {
        while (q < d.Length && IsWhite(d[q]))
        {
            q++;
        }
    }

    private static int ReadInt(byte[] d, ref int q)
    {
        SkipSpace(d, ref q);
        bool neg = q < d.Length && d[q] == (byte)'-';
        if (neg)
        {
            q++;
        }
        int v = 0;
        while (q < d.Length && d[q] is >= (byte)'0' and <= (byte)'9')
        {
            v = v * 10 + (d[q] - '0');
            q++;
        }
        return neg ? -v : v;
    }

    // Skips the "RD"/"-|" read operator and the single following space, returning
    // the index of the first binary byte (or -1 on failure).
    private static int SkipToBinaryStart(byte[] d, int q)
    {
        SkipSpace(d, ref q);
        // The operator is a token (RD or -|), then exactly one space precedes data.
        while (q < d.Length && !IsWhite(d[q]))
        {
            q++;
        }
        if (q < d.Length && IsWhite(d[q]))
        {
            q++; // the single separator space
            return q;
        }
        return -1;
    }
}
