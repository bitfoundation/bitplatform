// Content-stream operators resolved to a compact dispatch code.

namespace Bit.BlazorUI;

/// <summary>
/// A content-stream operator keyword resolved to a compact integer code. The
/// renderer dispatches on this (a dense <c>switch</c> jump table) instead of
/// re-hashing and comparing the operator string on every operation. Resolution
/// runs once per interned <see cref="BitPdfCmd"/> keyword (a few dozen per
/// document), not once per occurrence — see <see cref="BitPdfCmd.OpCode"/>.
///
/// Members are grouped so two hot renderer predicates collapse to a single
/// range compare: the colour operators form one contiguous block
/// (<see cref="FillGray"/>..<see cref="StrokeColorN"/>) and the text operators
/// another (<see cref="BeginText"/>..<see cref="NextLineShowTextSpacing"/>).
/// Keep each block contiguous when adding members.
/// </summary>
internal enum BitPdfOpCode : byte
{
    Unknown = 0,

    // Graphics state.
    SaveState, RestoreState, ConcatMatrix, LineWidth, Dash, LineCap, LineJoin,
    MiterLimit, RenderingIntent, Flatness, ExtGState,

    // Colour — KEEP CONTIGUOUS (FillGray..StrokeColorN); the Type3 `d1` colour
    // lock is a single range compare over this block.
    FillGray, StrokeGray, FillRgb, StrokeRgb, FillCmyk, StrokeCmyk,
    FillColorSpace, StrokeColorSpace, FillColorN, StrokeColorN,

    // Path construction.
    MoveTo, LineTo, CurveTo, CurveToV, CurveToY, Rectangle, ClosePath,

    // Path painting.
    Stroke, CloseStroke, Fill, FillEvenOdd, FillStroke, FillStrokeEvenOdd,
    CloseFillStroke, CloseFillStrokeEvenOdd, EndPath, Clip, ClipEvenOdd,

    // Shadings, XObjects, inline images.
    Shading, XObject, InlineImage,

    // Type3 glyph metrics.
    Type3Width, Type3WidthBox,

    // Marked / optional content.
    BeginMarkedContentDict, BeginMarkedContent, EndMarkedContent,

    // Text — KEEP CONTIGUOUS (BeginText..NextLineShowTextSpacing); a pending
    // coalesced painted line may survive across exactly this block, tested as a
    // single range compare.
    BeginText, EndText, CharSpacing, WordSpacing, HorizScale, Leading, TextRise,
    RenderMode, SetFont, TextMove, TextMoveSetLeading, TextMatrix, TextNextLine,
    ShowText, ShowTextArray, NextLineShowText, NextLineShowTextSpacing,
}

internal static class BitPdfOpCodes
{
    /// <summary>
    /// Maps an operator keyword to its <see cref="BitPdfOpCode"/>. Called once
    /// per unique interned keyword, so the string switch cost is amortized away.
    /// </summary>
    internal static BitPdfOpCode Resolve(string op) => op switch
    {
        // Graphics state.
        "q" => BitPdfOpCode.SaveState,
        "Q" => BitPdfOpCode.RestoreState,
        "cm" => BitPdfOpCode.ConcatMatrix,
        "w" => BitPdfOpCode.LineWidth,
        "d" => BitPdfOpCode.Dash,
        "J" => BitPdfOpCode.LineCap,
        "j" => BitPdfOpCode.LineJoin,
        "M" => BitPdfOpCode.MiterLimit,
        "ri" => BitPdfOpCode.RenderingIntent,
        "i" => BitPdfOpCode.Flatness,
        "gs" => BitPdfOpCode.ExtGState,

        // Colour.
        "g" => BitPdfOpCode.FillGray,
        "G" => BitPdfOpCode.StrokeGray,
        "rg" => BitPdfOpCode.FillRgb,
        "RG" => BitPdfOpCode.StrokeRgb,
        "k" => BitPdfOpCode.FillCmyk,
        "K" => BitPdfOpCode.StrokeCmyk,
        "cs" => BitPdfOpCode.FillColorSpace,
        "CS" => BitPdfOpCode.StrokeColorSpace,
        "sc" or "scn" => BitPdfOpCode.FillColorN,
        "SC" or "SCN" => BitPdfOpCode.StrokeColorN,

        // Path construction.
        "m" => BitPdfOpCode.MoveTo,
        "l" => BitPdfOpCode.LineTo,
        "c" => BitPdfOpCode.CurveTo,
        "v" => BitPdfOpCode.CurveToV,
        "y" => BitPdfOpCode.CurveToY,
        "re" => BitPdfOpCode.Rectangle,
        "h" => BitPdfOpCode.ClosePath,

        // Path painting.
        "S" => BitPdfOpCode.Stroke,
        "s" => BitPdfOpCode.CloseStroke,
        "f" or "F" => BitPdfOpCode.Fill,
        "f*" => BitPdfOpCode.FillEvenOdd,
        "B" => BitPdfOpCode.FillStroke,
        "B*" => BitPdfOpCode.FillStrokeEvenOdd,
        "b" => BitPdfOpCode.CloseFillStroke,
        "b*" => BitPdfOpCode.CloseFillStrokeEvenOdd,
        "n" => BitPdfOpCode.EndPath,
        "W" => BitPdfOpCode.Clip,
        "W*" => BitPdfOpCode.ClipEvenOdd,

        // Shadings, XObjects, inline images.
        "sh" => BitPdfOpCode.Shading,
        "Do" => BitPdfOpCode.XObject,
        "INLINE_IMAGE" => BitPdfOpCode.InlineImage,

        // Type3 glyph metrics.
        "d0" => BitPdfOpCode.Type3Width,
        "d1" => BitPdfOpCode.Type3WidthBox,

        // Marked / optional content.
        "BDC" => BitPdfOpCode.BeginMarkedContentDict,
        "BMC" => BitPdfOpCode.BeginMarkedContent,
        "EMC" => BitPdfOpCode.EndMarkedContent,

        // Text.
        "BT" => BitPdfOpCode.BeginText,
        "ET" => BitPdfOpCode.EndText,
        "Tc" => BitPdfOpCode.CharSpacing,
        "Tw" => BitPdfOpCode.WordSpacing,
        "Tz" => BitPdfOpCode.HorizScale,
        "TL" => BitPdfOpCode.Leading,
        "Ts" => BitPdfOpCode.TextRise,
        "Tr" => BitPdfOpCode.RenderMode,
        "Tf" => BitPdfOpCode.SetFont,
        "Td" => BitPdfOpCode.TextMove,
        "TD" => BitPdfOpCode.TextMoveSetLeading,
        "Tm" => BitPdfOpCode.TextMatrix,
        "T*" => BitPdfOpCode.TextNextLine,
        "Tj" => BitPdfOpCode.ShowText,
        "TJ" => BitPdfOpCode.ShowTextArray,
        "'" => BitPdfOpCode.NextLineShowText,
        "\"" => BitPdfOpCode.NextLineShowTextSpacing,

        _ => BitPdfOpCode.Unknown,
    };
}
