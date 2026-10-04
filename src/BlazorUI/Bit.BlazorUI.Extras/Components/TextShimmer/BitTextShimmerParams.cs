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

        // Every value below drives the class or the style of the root, and this runs on every render of every
        // shimmer under the BitParams - so a builder is only reset when the value differs from the one the
        // shimmer already holds.

        if (Alternate.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Alternate)) && bitTextShimmer.Alternate != Alternate.Value)
        {
            bitTextShimmer.Alternate = Alternate.Value;

            bitTextShimmer.ClassBuilder.Reset();
        }

        // Nullable.Equals rather than != for the two doubles, since a NaN is never != to itself and would reset the
        // builder on every render.
        if (Angle.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Angle)) && Nullable.Equals(bitTextShimmer.Angle, Angle) is false)
        {
            bitTextShimmer.Angle = Angle.Value;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (BaseColor.HasValue() && bitTextShimmer.HasNotBeenSet(nameof(BaseColor)) && bitTextShimmer.BaseColor != BaseColor)
        {
            bitTextShimmer.BaseColor = BaseColor;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Color.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Color)) && bitTextShimmer.Color != Color)
        {
            bitTextShimmer.Color = Color.Value;

            bitTextShimmer.ClassBuilder.Reset();
        }

        if (Delay.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Delay)) && bitTextShimmer.Delay != Delay)
        {
            bitTextShimmer.Delay = Delay.Value;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Duration.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Duration)) && bitTextShimmer.Duration != Duration)
        {
            bitTextShimmer.Duration = Duration.Value;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Element.HasValue() && bitTextShimmer.HasNotBeenSet(nameof(Element)))
        {
            bitTextShimmer.Element = Element;
        }

        if (GradientColor.HasValue() && bitTextShimmer.HasNotBeenSet(nameof(GradientColor)) && bitTextShimmer.GradientColor != GradientColor)
        {
            bitTextShimmer.GradientColor = GradientColor;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Iterations.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Iterations)) && bitTextShimmer.Iterations != Iterations)
        {
            bitTextShimmer.Iterations = Iterations.Value;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Paused.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Paused)) && bitTextShimmer.Paused != Paused.Value)
        {
            bitTextShimmer.Paused = Paused.Value;

            bitTextShimmer.ClassBuilder.Reset();
        }

        if (PauseOnHover.HasValue && bitTextShimmer.HasNotBeenSet(nameof(PauseOnHover)) && bitTextShimmer.PauseOnHover != PauseOnHover.Value)
        {
            bitTextShimmer.PauseOnHover = PauseOnHover.Value;

            bitTextShimmer.ClassBuilder.Reset();
        }

        if (RepeatDelay.HasValue && bitTextShimmer.HasNotBeenSet(nameof(RepeatDelay)) && bitTextShimmer.RepeatDelay != RepeatDelay)
        {
            bitTextShimmer.RepeatDelay = RepeatDelay.Value;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Reversed.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Reversed)) && bitTextShimmer.Reversed != Reversed.Value)
        {
            bitTextShimmer.Reversed = Reversed.Value;

            bitTextShimmer.ClassBuilder.Reset();
        }

        if (Spread.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Spread)) && Nullable.Equals(bitTextShimmer.Spread, Spread) is false)
        {
            bitTextShimmer.Spread = Spread;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (SpreadLength.HasValue() && bitTextShimmer.HasNotBeenSet(nameof(SpreadLength)) && bitTextShimmer.SpreadLength != SpreadLength)
        {
            bitTextShimmer.SpreadLength = SpreadLength;

            bitTextShimmer.StyleBuilder.Reset();
        }

        if (Static.HasValue && bitTextShimmer.HasNotBeenSet(nameof(Static)) && bitTextShimmer.Static != Static.Value)
        {
            bitTextShimmer.Static = Static.Value;

            bitTextShimmer.ClassBuilder.Reset();
        }
    }
}
