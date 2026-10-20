using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A text annotation. Text is laid out by the host (browser fonts) so the element stores an explicit
/// <see cref="Size"/>; when the host measures the text it updates the size, otherwise an estimate is used.
/// </summary>
public sealed record BinkTextElement : BinkElement
{
    /// <summary>Maximum accepted text length, a defense against oversized documents.</summary>
    public const int MaxLength = 100_000;

    private readonly string _text = string.Empty;

    /// <summary>Creates a text element at a position.</summary>
    public BinkTextElement(Vector2 position, string text)
    {
        Position = position;
        Text = text;
    }

    /// <summary>Top-left of the text box in local coordinates.</summary>
    public Vector2 Position { get; init; }

    /// <summary>The text; line breaks start new lines.</summary>
    public string Text
    {
        get => _text;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > MaxLength) throw new ArgumentException($"Text exceeds the maximum length of {MaxLength} characters.", nameof(value));
            _text = value;
        }
    }

    /// <summary>Font and color.</summary>
    public BinkTextStyle Style { get; init; } = BinkTextStyle.Default;

    /// <summary>Opacity 0..1.</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Measured or estimated size of the text box in local units. Null means "estimate".</summary>
    public Vector2? Size { get; init; }

    /// <summary>Optional background color behind the text (e.g. for sticky notes).</summary>
    public BinkColor? Background { get; init; }

    /// <summary>Padding between background and text.</summary>
    public float Padding { get; init; } = 4f;

    /// <inheritdoc />
    public override string TypeName => "Text";

    /// <summary>The size used for bounds: measured when available, otherwise estimated.</summary>
    public Vector2 EffectiveSize => Size ?? EstimateSize();

    /// <inheritdoc />
    public override BinkRect LocalBounds
    {
        get
        {
            Vector2 size = EffectiveSize;
            Vector2 origin = Position;
            if (Style.Align == BinkTextAlign.Center) origin.X -= size.X / 2f;
            else if (Style.Align == BinkTextAlign.Right) origin.X -= size.X;
            return new BinkRect(origin.X - Padding, origin.Y - Padding, size.X + 2 * Padding, size.Y + 2 * Padding);
        }
    }

    /// <summary>
    /// Estimates the text size from the font size using an average glyph width. Hosts that can measure text should
    /// set <see cref="Size"/> instead.
    /// </summary>
    public Vector2 EstimateSize()
    {
        const float averageGlyphWidth = 0.55f;
        int lines = 1;
        int longest = 0;
        int current = 0;
        foreach (char c in _text)
        {
            if (c == '\n') { lines++; longest = Math.Max(longest, current); current = 0; }
            else if (c != '\r') current++;
        }
        longest = Math.Max(longest, current);
        float width = MathF.Max(longest * Style.FontSize * averageGlyphWidth, Style.FontSize * 0.5f);
        float height = lines * Style.FontSize * Style.LineHeight;
        return new Vector2(width, height);
    }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        if (Background is { } bg && !bg.IsTransparent)
        {
            context.FillPath(new BinkPathBuilder(6).AddRect(LocalBounds).ToGeometry(), new BinkFillPaint(bg, Opacity));
        }
        if (_text.Length == 0) return;
        context.DrawText(_text, Position, Style, Opacity);
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => LocalBounds.Inflate(tolerance).Contains(localPoint);

    /// <inheritdoc />
    public override string GetAccessibleDescription() => Description ?? (string.IsNullOrWhiteSpace(_text) ? "Empty text" : "Text: " + _text);
}
