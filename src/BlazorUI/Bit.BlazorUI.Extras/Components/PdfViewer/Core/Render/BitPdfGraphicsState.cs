// Graphics-state tracking for content-stream rendering.


namespace Bit.BlazorUI;

/// <summary>
/// The mutable PDF graphics state: current transform, colors, line width and
/// the full text state. Pushed and popped by the <c>q</c> / <c>Q</c> operators.
/// </summary>
public sealed class BitPdfGraphicsState
{
    public BitPdfMatrix Ctm { get; set; } = BitPdfMatrix.Identity;

    public string FillColor { get; set; } = "rgb(0,0,0)";
    public string StrokeColor { get; set; } = "rgb(0,0,0)";
    public BitPdfColorSpace? FillColorSpace { get; set; }   // current fill space (cs)
    public BitPdfColorSpace? StrokeColorSpace { get; set; } // current stroke space (CS)
    public string? FillPattern { get; set; }   // /Pattern resource name for scn
    public string BlendMode { get; set; } = ""; // CSS mix-blend-mode, "" = normal
    public double FillAlpha { get; set; } = 1.0;
    public double StrokeAlpha { get; set; } = 1.0;
    public double LineWidth { get; set; } = 1.0;
    public double[]? DashArray { get; set; }    // device dash pattern (d operator)
    public double DashPhase { get; set; }
    public int LineCap { get; set; }            // 0 butt, 1 round, 2 square
    public int LineJoin { get; set; }           // 0 miter, 1 round, 2 bevel
    public double MiterLimit { get; set; } = 10.0; // PDF default 10 (SVG default is 4)

    // Text state.
    public BitPdfFont? Font { get; set; }
    public string? FontResourceName { get; set; }
    public double FontSize { get; set; }
    public double CharSpacing { get; set; }
    public double WordSpacing { get; set; }
    public double Leading { get; set; }
    public double HorizScale { get; set; } = 1.0; // Tz / 100
    public double TextRise { get; set; }
    public int RenderMode { get; set; }

    public BitPdfGraphicsState Clone() => new()
    {
        Ctm = Ctm,
        FillColor = FillColor,
        StrokeColor = StrokeColor,
        FillColorSpace = FillColorSpace,
        StrokeColorSpace = StrokeColorSpace,
        FillPattern = FillPattern,
        BlendMode = BlendMode,
        FillAlpha = FillAlpha,
        StrokeAlpha = StrokeAlpha,
        LineWidth = LineWidth,
        DashArray = DashArray,
        DashPhase = DashPhase,
        LineCap = LineCap,
        LineJoin = LineJoin,
        MiterLimit = MiterLimit,
        Font = Font,
        FontResourceName = FontResourceName,
        FontSize = FontSize,
        CharSpacing = CharSpacing,
        WordSpacing = WordSpacing,
        Leading = Leading,
        HorizScale = HorizScale,
        TextRise = TextRise,
        RenderMode = RenderMode,
    };
}
