namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitProgress"/> component.
/// </summary>
/// <remarks>
/// It carries what a group of indicators shares - the shape, the look, the scale they are read on and how they
/// speak - and leaves out what each one reports on its own: the <see cref="BitProgress.Percent"/>,
/// <see cref="BitProgress.Value"/> and <see cref="BitProgress.Buffer"/>, the texts naming and describing it, and
/// the templates.
/// </remarks>
public class BitProgressParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitProgress"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitProgress value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitProgress)}";



    public string Name => ParamName;



    /// <summary>
    /// Announces the progress to screen readers as it advances, once per <see cref="AnnounceStep"/> crossed.
    /// </summary>
    public bool? AnnounceProgress { get; set; }

    /// <summary>
    /// How far the progress has to advance, in percentage points, before it is announced again.
    /// </summary>
    public double? AnnounceStep { get; set; }

    /// <summary>
    /// The color of the bar itself, as any CSS color, replacing the palette of the <see cref="Color"/> role.
    /// </summary>
    public string? BarColor { get; set; }

    /// <summary>
    /// Draws the progress as a ring instead of as a bar.
    /// </summary>
    public bool? Circular { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitProgress.
    /// </summary>
    public BitProgressClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the BitProgress.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// How long, in milliseconds, the progress stays hidden after it is first rendered.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// The diameter of the circular progress in pixels.
    /// </summary>
    public int? Diameter { get; set; }

    /// <summary>
    /// Cuts a gap of this many degrees out of the circular progress, which turns the ring into a gauge.
    /// </summary>
    public double? GapDegree { get; set; }

    /// <summary>
    /// Where the <see cref="GapDegree"/> gap of the gauge sits.
    /// </summary>
    public BitProgressGapPosition? GapPosition { get; set; }

    /// <summary>
    /// Reports that something is running without saying how far along it is.
    /// </summary>
    public bool? Indeterminate { get; set; }

    /// <summary>
    /// How long a vertical bar is, as a CSS length.
    /// </summary>
    public string? Length { get; set; }

    /// <summary>
    /// The highest value of the range the Value is read against.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Reports the indicator as a meter - a measurement within a known range - rather than as a progress bar.
    /// </summary>
    public bool? Meter { get; set; }

    /// <summary>
    /// The lowest value of the range the Value is read against.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// The composite format string the percentage readout is written with.
    /// </summary>
    public string? PercentNumberFormat { get; set; }

    /// <summary>
    /// Where the percentage readout of a linear progress is placed.
    /// </summary>
    public BitProgressPercentPosition? PercentNumberPosition { get; set; }

    /// <summary>
    /// The multiplier applied to the thickness to size the circular progress.
    /// </summary>
    public int? Radius { get; set; }

    /// <summary>
    /// Fills the progress from the end of the container towards its start.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Rounds the ends of the bar, and the stroke cap of the ring.
    /// </summary>
    public bool? Rounded { get; set; }

    /// <summary>
    /// The gap between two segments, in pixels.
    /// </summary>
    public int? SegmentGap { get; set; }

    /// <summary>
    /// Cuts the linear bar into this many equal segments.
    /// </summary>
    public int? Segments { get; set; }

    /// <summary>
    /// Writes the percentage beside the bar, or in the middle of the ring.
    /// </summary>
    public bool? ShowPercentNumber { get; set; }

    /// <summary>
    /// The size of the BitProgress.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Paints diagonal stripes over the linear bar.
    /// </summary>
    public bool? Striped { get; set; }

    /// <summary>
    /// Animates the stripes of a striped bar so they travel along it.
    /// </summary>
    public bool? StripedAnimation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProgress.
    /// </summary>
    public BitProgressClassStyles? Styles { get; set; }

    /// <summary>
    /// How thick the indicator is drawn, in pixels.
    /// </summary>
    public int? Thickness { get; set; }

    /// <summary>
    /// The color of the unfilled part of the indicator, as any CSS color.
    /// </summary>
    public string? TrackColor { get; set; }

    /// <summary>
    /// Stands the linear bar on its end.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitProgress"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitProgress"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitProgress"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitProgress"/>.
    /// </remarks>
    /// <param name="bitProgress">
    /// The <see cref="BitProgress"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitProgress bitProgress)
    {
        if (bitProgress is null) return;

        UpdateBaseParameters(bitProgress);

        if (AnnounceProgress.HasValue && bitProgress.HasNotBeenSet(nameof(AnnounceProgress)))
        {
            bitProgress.AnnounceProgress = AnnounceProgress.Value;
        }

        if (AnnounceStep.HasValue && bitProgress.HasNotBeenSet(nameof(AnnounceStep)))
        {
            bitProgress.AnnounceStep = AnnounceStep.Value;
        }

        if (BarColor.HasValue() && bitProgress.HasNotBeenSet(nameof(BarColor)))
        {
            bitProgress.BarColor = BarColor;
        }

        if (Circular.HasValue && bitProgress.HasNotBeenSet(nameof(Circular)))
        {
            bitProgress.Circular = Circular.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (Classes is not null && bitProgress.HasNotBeenSet(nameof(Classes)))
        {
            bitProgress.Classes = Classes;

            bitProgress.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitProgress.HasNotBeenSet(nameof(Color)))
        {
            bitProgress.Color = Color.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (Delay.HasValue && bitProgress.HasNotBeenSet(nameof(Delay)))
        {
            bitProgress.Delay = Delay.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (Diameter.HasValue && bitProgress.HasNotBeenSet(nameof(Diameter)))
        {
            bitProgress.Diameter = Diameter.Value;
        }

        if (GapDegree.HasValue && bitProgress.HasNotBeenSet(nameof(GapDegree)))
        {
            bitProgress.GapDegree = GapDegree.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (GapPosition.HasValue && bitProgress.HasNotBeenSet(nameof(GapPosition)))
        {
            bitProgress.GapPosition = GapPosition.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (Indeterminate.HasValue && bitProgress.HasNotBeenSet(nameof(Indeterminate)))
        {
            bitProgress.Indeterminate = Indeterminate.Value;
        }

        if (Length.HasValue() && bitProgress.HasNotBeenSet(nameof(Length)))
        {
            bitProgress.Length = Length;
        }

        if (Max.HasValue && bitProgress.HasNotBeenSet(nameof(Max)))
        {
            bitProgress.Max = Max.Value;
        }

        if (Meter.HasValue && bitProgress.HasNotBeenSet(nameof(Meter)))
        {
            bitProgress.Meter = Meter.Value;
        }

        if (Min.HasValue && bitProgress.HasNotBeenSet(nameof(Min)))
        {
            bitProgress.Min = Min.Value;
        }

        if (PercentNumberFormat.HasValue() && bitProgress.HasNotBeenSet(nameof(PercentNumberFormat)))
        {
            bitProgress.PercentNumberFormat = PercentNumberFormat!;
        }

        if (PercentNumberPosition.HasValue && bitProgress.HasNotBeenSet(nameof(PercentNumberPosition)))
        {
            bitProgress.PercentNumberPosition = PercentNumberPosition.Value;
        }

        if (Radius.HasValue && bitProgress.HasNotBeenSet(nameof(Radius)))
        {
            bitProgress.Radius = Radius.Value;
        }

        if (Reversed.HasValue && bitProgress.HasNotBeenSet(nameof(Reversed)))
        {
            bitProgress.Reversed = Reversed.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (Rounded.HasValue && bitProgress.HasNotBeenSet(nameof(Rounded)))
        {
            bitProgress.Rounded = Rounded.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (SegmentGap.HasValue && bitProgress.HasNotBeenSet(nameof(SegmentGap)))
        {
            bitProgress.SegmentGap = SegmentGap.Value;
        }

        if (Segments.HasValue && bitProgress.HasNotBeenSet(nameof(Segments)))
        {
            bitProgress.Segments = Segments.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (ShowPercentNumber.HasValue && bitProgress.HasNotBeenSet(nameof(ShowPercentNumber)))
        {
            bitProgress.ShowPercentNumber = ShowPercentNumber.Value;
        }

        if (Size.HasValue && bitProgress.HasNotBeenSet(nameof(Size)))
        {
            bitProgress.Size = Size.Value;

            bitProgress.ClassBuilder.Reset();
        }

        if (Striped.HasValue && bitProgress.HasNotBeenSet(nameof(Striped)))
        {
            bitProgress.Striped = Striped.Value;
        }

        if (StripedAnimation.HasValue && bitProgress.HasNotBeenSet(nameof(StripedAnimation)))
        {
            bitProgress.StripedAnimation = StripedAnimation.Value;
        }

        if (Styles is not null && bitProgress.HasNotBeenSet(nameof(Styles)))
        {
            bitProgress.Styles = Styles;

            bitProgress.StyleBuilder.Reset();
        }

        if (Thickness.HasValue && bitProgress.HasNotBeenSet(nameof(Thickness)))
        {
            bitProgress.Thickness = Thickness.Value;
        }

        if (TrackColor.HasValue() && bitProgress.HasNotBeenSet(nameof(TrackColor)))
        {
            bitProgress.TrackColor = TrackColor;
        }

        if (Vertical.HasValue && bitProgress.HasNotBeenSet(nameof(Vertical)))
        {
            bitProgress.Vertical = Vertical.Value;

            bitProgress.ClassBuilder.Reset();
        }
    }
}
