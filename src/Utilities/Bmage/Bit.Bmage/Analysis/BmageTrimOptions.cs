namespace Bit.Bmage;

/// <summary>How <see cref="BmageContentBounds.Find"/> tells content from background.</summary>
public sealed record BmageTrimOptions
{
    /// <summary>Transparent or top-left-colour borders, within 4% per channel.</summary>
    public static BmageTrimOptions Default { get; } = new();

    /// <summary>What counts as background.</summary>
    public BmageTrimMode Mode { get; init; }

    /// <summary>The background colour for <see cref="BmageTrimMode.Color"/>, or null to take the top left pixel's.</summary>
    public BmageRgba32? Color { get; init; }

    /// <summary>
    /// How far a channel may differ from the background and still count as background, in [0,1] of the channel's range.
    /// A little tolerance absorbs JPEG noise in a "white" border.
    /// </summary>
    public float Tolerance
    {
        get;
        init => field = float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : throw new ArgumentOutOfRangeException(nameof(Tolerance), "The tolerance must be a finite number.");
    } = 0.04f;

    /// <summary>Pixels of background kept around the content on every side, as far as the image reaches.</summary>
    public int Padding
    {
        get;
        init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Padding), "The padding cannot be negative.");
    }
}
