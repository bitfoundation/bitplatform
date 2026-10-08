namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTextShimmer"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the shimmers of an application, or of one part of it, agree on: the look of the band,
/// its pace and its direction, the tag they render, and whether they move at all - a <see cref="Paused"/> or a
/// <see cref="Static"/> set once over a region is how a single "pause animations" switch reaches every shimmer in it.
/// The text, the content and its length are left out on purpose: they are what makes one shimmer the one it is.
/// </remarks>
public class BitTextShimmerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTextShimmer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTextShimmer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTextShimmer)}";



    public string Name => ParamName;



    /// <summary>
    /// Sweeps the band back and forth across the text instead of always in the same direction.
    /// </summary>
    public bool? Alternate { get; set; }

    /// <summary>
    /// The tilt of the band in degrees, measured from upright.
    /// </summary>
    public double? Angle { get; set; }

    /// <summary>
    /// The resting/dim color of the text.
    /// </summary>
    public string? BaseColor { get; set; }

    /// <summary>
    /// The general color of the band that sweeps across the text.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay before the first shimmer sweep starts in ms.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// The animation duration of one full shimmer sweep in ms.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// The bright highlight color that sweeps across the text. A Color set on the shimmer itself wins over it.
    /// </summary>
    public string? GradientColor { get; set; }

    /// <summary>
    /// The number of shimmer sweeps to play before the text comes to rest.
    /// </summary>
    public int? Iterations { get; set; }

    /// <summary>
    /// Holds the shimmer where it is instead of sweeping.
    /// </summary>
    public bool? Paused { get; set; }

    /// <summary>
    /// Holds the shimmer where it is while the pointer is over it or the focus is inside it.
    /// </summary>
    public bool? PauseOnHover { get; set; }

    /// <summary>
    /// An extra pause between two shimmer sweeps in ms.
    /// </summary>
    public int? RepeatDelay { get; set; }

    /// <summary>
    /// Sweeps the band against the reading direction.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The shimmer band width multiplier, applied to the character count of the text.
    /// </summary>
    public double? Spread { get; set; }

    /// <summary>
    /// An explicit CSS length for the spread of the band, which replaces the one computed from Spread and the character count.
    /// A Spread set on the shimmer itself wins over it.
    /// </summary>
    public string? SpreadLength { get; set; }

    /// <summary>
    /// Renders the text at rest in its base color, without the shimmer.
    /// </summary>
    public bool? Static { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTextShimmer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTextShimmer"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTextShimmer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTextShimmer"/>.
    /// </remarks>
    /// <param name="bitTextShimmer">
    /// The <see cref="BitTextShimmer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTextShimmer bitTextShimmer)
    {
        if (bitTextShimmer is null) return;

        UpdateBaseParameters(bitTextShimmer);

        if (Alternate.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Alternate), Alternate.Value, static t => t.Alternate, static (t, v) => t.Alternate = v);
        }

        if (Angle.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Angle), Angle.Value, static t => t.Angle, static (t, v) => t.Angle = v);
        }

        if (BaseColor.HasValue())
        {
            bitTextShimmer.TakeFromCascade(nameof(BaseColor), BaseColor, static t => t.BaseColor, static (t, v) => t.BaseColor = v);
        }

        if (Color.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (Delay.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Delay), Delay.Value, static t => t.Delay, static (t, v) => t.Delay = v);
        }

        if (Duration.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Duration), Duration.Value, static t => t.Duration, static (t, v) => t.Duration = v);
        }

        if (Element.HasValue())
        {
            bitTextShimmer.TakeFromCascade(nameof(Element), Element, static t => t.Element, static (t, v) => t.Element = v);
        }

        if (GradientColor.HasValue())
        {
            bitTextShimmer.TakeFromCascade(nameof(GradientColor), GradientColor, static t => t.GradientColor, static (t, v) => t.GradientColor = v);
        }

        if (Iterations.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Iterations), Iterations.Value, static t => t.Iterations, static (t, v) => t.Iterations = v);
        }

        if (Paused.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Paused), Paused.Value, static t => t.Paused, static (t, v) => t.Paused = v);
        }

        if (PauseOnHover.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(PauseOnHover), PauseOnHover.Value, static t => t.PauseOnHover, static (t, v) => t.PauseOnHover = v);
        }

        if (RepeatDelay.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(RepeatDelay), RepeatDelay.Value, static t => t.RepeatDelay, static (t, v) => t.RepeatDelay = v);
        }

        if (Reversed.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Reversed), Reversed.Value, static t => t.Reversed, static (t, v) => t.Reversed = v);
        }

        if (Spread.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Spread), Spread, static t => t.Spread, static (t, v) => t.Spread = v);
        }

        if (SpreadLength.HasValue())
        {
            bitTextShimmer.TakeFromCascade(nameof(SpreadLength), SpreadLength, static t => t.SpreadLength, static (t, v) => t.SpreadLength = v);
        }

        if (Static.HasValue)
        {
            bitTextShimmer.TakeFromCascade(nameof(Static), Static.Value, static t => t.Static, static (t, v) => t.Static = v);
        }
    }
}
