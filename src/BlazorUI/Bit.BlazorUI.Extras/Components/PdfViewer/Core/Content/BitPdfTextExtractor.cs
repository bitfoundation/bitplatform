// Plain-text extraction from a page's content stream, without emitting HTML.
// Used for search indexing and the public text-extraction API.

using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Extracts the visible text of a page by replaying its content stream and
/// decoding show-text operators through each selected font. Positioning is
/// approximated with simple space/newline heuristics — enough for search and
/// copy, not a layout-faithful reconstruction.
/// </summary>
public static class BitPdfTextExtractor
{
    public static string Extract(BitPdfPage page, IBitPdfXRef xref)
    {
        var sb = new StringBuilder();
        var fontCache = new Dictionary<object, BitPdfFont>();

        void Run(byte[] content, BitPdfDict? resources, int depth)
        {
            if (depth > 8)
            {
                return;
            }
            List<BitPdfOperation> ops;
            try
            {
                ops = new BitPdfContentParser(content).Parse();
            }
            catch
            {
                return;
            }

            BitPdfFont? font = null;
            foreach (var op in ops)
            {
                switch (op.Code)
                {
                    case BitPdfOpCode.SetFont:
                        if (op.Operands.Count >= 1 && op.Operands[0] is BitPdfName fn)
                        {
                            font = ResolveFont(fn.Value, resources, xref, fontCache);
                        }
                        break;
                    case BitPdfOpCode.ShowText:
                        AppendShow(sb, font, op.Operands.Count > 0 ? op.Operands[0] : null);
                        break;
                    case BitPdfOpCode.ShowTextArray:
                        if (op.Operands.Count > 0 && op.Operands[0] is List<object?> arr)
                        {
                            foreach (var item in arr)
                            {
                                if (item is BitPdfString)
                                {
                                    AppendShow(sb, font, item);
                                }
                                else if (item is double adj && adj < -100)
                                {
                                    sb.Append(' '); // a large negative adjustment is a word gap
                                }
                            }
                        }
                        break;
                    case BitPdfOpCode.NextLineShowText:
                    case BitPdfOpCode.NextLineShowTextSpacing:
                        // Both move to the next line then show the string (the last
                        // operand); the spacing variant's word/char spacing operands
                        // don't affect extracted text.
                        sb.Append('\n');
                        AppendShow(sb, font, op.Operands.Count > 0 ? op.Operands[^1] : null);
                        break;
                    case BitPdfOpCode.TextMove:
                    case BitPdfOpCode.TextMoveSetLeading:
                        // A vertical move implies a new line; a purely horizontal move a space.
                        if (Math.Abs(op.Num(1)) > 0.01)
                        {
                            sb.Append('\n');
                        }
                        else if (op.Num(0) > 0)
                        {
                            sb.Append(' ');
                        }
                        break;
                    case BitPdfOpCode.TextNextLine:
                        sb.Append('\n');
                        break;
                    case BitPdfOpCode.XObject:
                        // Recurse into a form XObject so its text is captured too.
                        if (op.Operands.Count > 0 && op.Operands[0] is BitPdfName xn
                            && resources?.Get("XObject") is BitPdfDict xobjs
                            && xobjs.Get(xn.Value) is BitPdfStream xs && xs.Dict is not null
                            && (xs.Dict.Get("Subtype") as BitPdfName)?.Value == "Form")
                        {
                            try
                            {
                                Run(BitPdfStreamDecoder.Decode(xs), xs.Dict.Get("Resources") as BitPdfDict ?? resources, depth + 1);
                            }
                            catch
                            {
                                // ignore malformed form content
                            }
                        }
                        break;
                }
            }
        }

        Run(page.GetContentBytes(), page.Resources, 0);
        return sb.ToString();
    }

    private static void AppendShow(StringBuilder sb, BitPdfFont? font, object? operand)
    {
        if (operand is not BitPdfString s || font is null)
        {
            return;
        }
        foreach (var g in font.Decode(s.Bytes))
        {
            sb.Append(g.Unicode);
        }
    }

    private static BitPdfFont? ResolveFont(string name, BitPdfDict? resources, IBitPdfXRef xref, Dictionary<object, BitPdfFont> cache)
    {
        if (resources?.Get("Font") is not BitPdfDict fonts)
        {
            return null;
        }
        object? raw = fonts.GetRaw(name);
        object key = raw is BitPdfRef r ? r : xref.FetchIfRef(raw) ?? name;
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }
        if (xref.FetchIfRef(raw) is BitPdfDict fontDict)
        {
            var font = BitPdfFont.Create(fontDict, xref);
            cache[key] = font;
            return font;
        }
        return null;
    }
}
