using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A text label, optionally with a background box.</summary>
public sealed record BmageTextAnnotation : BmageAnnotation
{
    public required string Text { get; init; }
    public required Vector2 Position { get; init; }
    public BmageTextStyle Style { get; init; } = new();

    /// <summary>Background colour drawn behind the text, or null for none.</summary>
    public BmageRgba32? BackgroundColor { get; init; }

    /// <summary>Padding around the text when a background is drawn.</summary>
    public float BackgroundPadding { get; init; } = 6f;

    /// <summary>Corner radius of the background box.</summary>
    public float BackgroundCornerRadius { get; init; } = 4f;

    /// <summary>Cached measured size, set by the editor after measuring. Used for bounds before the first render.</summary>
    public SizeF? MeasuredSize { get; init; }

    public override string Kind => "Text";

    public override RectangleF GetBounds()
    {
        var size = MeasuredSize ?? new SizeF(Math.Max(1, Text.Length) * Style.FontSize * 0.55f, Style.FontSize * Style.LineHeight * (1 + Text.Count(c => c == '\n')));
        var x = Style.Alignment switch
        {
            BmageTextAlignment.Center => Position.X - size.Width / 2,
            BmageTextAlignment.Right => Position.X - size.Width,
            _ => Position.X,
        };
        var y = Style.Baseline switch
        {
            BmageTextBaseline.Top => Position.Y,
            BmageTextBaseline.Middle => Position.Y - size.Height / 2,
            BmageTextBaseline.Bottom => Position.Y - size.Height,
            _ => Position.Y - Style.FontSize * 0.8f,
        };
        var r = new RectangleF(x, y, size.Width, size.Height);
        if (BackgroundColor is not null) r.Inflate(BackgroundPadding, BackgroundPadding);
        return r;
    }

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        if (string.IsNullOrEmpty(Text) || context.TextRasterizer is not { } rasterizer) return;
        if (BackgroundColor is { } background)
        {
            var metrics = rasterizer.Measure(Text, Style);
            var box = MeasuredBounds(metrics);
            box.Inflate(BackgroundPadding, BackgroundPadding);
            canvas.FillRoundedRectangle(box, BackgroundCornerRadius, background);
        }
        canvas.DrawText(Text, Position, Style, rasterizer);
    }

    /// <summary>Bounds computed from actual measured metrics.</summary>
    public RectangleF MeasuredBounds(BmageTextMetrics metrics)
    {
        var x = Style.Alignment switch
        {
            BmageTextAlignment.Center => Position.X - metrics.Width / 2,
            BmageTextAlignment.Right => Position.X - metrics.Width,
            _ => Position.X,
        };
        var y = Style.Baseline switch
        {
            BmageTextBaseline.Top => Position.Y,
            BmageTextBaseline.Middle => Position.Y - metrics.Height / 2,
            BmageTextBaseline.Bottom => Position.Y - metrics.Height,
            _ => Position.Y - metrics.Ascent,
        };
        return new RectangleF(x, y, metrics.Width, metrics.Height);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Position = Position + new Vector2(dx, dy) };

    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Position = Position * (float)scale,
        Style = Style.ForScale(scale),
        BackgroundPadding = (float)(BackgroundPadding * scale),
        BackgroundCornerRadius = (float)(BackgroundCornerRadius * scale),
        MeasuredSize = MeasuredSize is { } s ? new SizeF((float)(s.Width * scale), (float)(s.Height * scale)) : null,
    };
}
