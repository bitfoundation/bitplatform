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
    public BitPlacement? GapPlacement { get; set; }

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
    /// The corners of the linear track and bar, and the stroke cap of the ring.
    /// </summary>
    public BitShape? Shape { get; set; }

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
    /// This method does not overwrite existing values on <paramref name="bitProgress"/>. What it supplies is recorded on
    /// the component, which puts back the value it replaced once this object stops supplying one.
    /// </remarks>
    /// <param name="bitProgress">
    /// The <see cref="BitProgress"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitProgress bitProgress)
    {
        if (bitProgress is null) return;

        UpdateBaseParameters(bitProgress);

        if (AnnounceProgress.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(AnnounceProgress), AnnounceProgress.Value, static p => p.AnnounceProgress, static (p, v) => p.AnnounceProgress = v);
        }

        if (AnnounceStep.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(AnnounceStep), AnnounceStep.Value, static p => p.AnnounceStep, static (p, v) => p.AnnounceStep = v);
        }

        if (BarColor.HasValue())
        {
            bitProgress.TakeFromCascade(nameof(BarColor), BarColor, static p => p.BarColor, static (p, v) => p.BarColor = v);
        }

        if (Circular.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Circular), Circular.Value, static p => p.Circular, static (p, v) => p.Circular = v);
        }

        if (Classes is not null)
        {
            bitProgress.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (Color.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Color), Color, static p => p.Color, static (p, v) => p.Color = v);
        }

        if (Delay.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Delay), Delay.Value, static p => p.Delay, static (p, v) => p.Delay = v);
        }

        if (Diameter.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Diameter), Diameter, static p => p.Diameter, static (p, v) => p.Diameter = v);
        }

        if (GapDegree.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(GapDegree), GapDegree.Value, static p => p.GapDegree, static (p, v) => p.GapDegree = v);
        }

        if (GapPlacement.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(GapPlacement), GapPlacement.Value, static p => p.GapPlacement, static (p, v) => p.GapPlacement = v);
        }

        if (Indeterminate.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Indeterminate), Indeterminate.Value, static p => p.Indeterminate, static (p, v) => p.Indeterminate = v);
        }

        if (Length.HasValue())
        {
            bitProgress.TakeFromCascade(nameof(Length), Length, static p => p.Length, static (p, v) => p.Length = v);
        }

        if (Max.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Max), Max.Value, static p => p.Max, static (p, v) => p.Max = v);
        }

        if (Meter.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Meter), Meter.Value, static p => p.Meter, static (p, v) => p.Meter = v);
        }

        if (Min.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Min), Min.Value, static p => p.Min, static (p, v) => p.Min = v);
        }

        if (PercentNumberFormat.HasValue())
        {
            bitProgress.TakeFromCascade(nameof(PercentNumberFormat), PercentNumberFormat!, static p => p.PercentNumberFormat, static (p, v) => p.PercentNumberFormat = v);
        }

        if (PercentNumberPosition.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(PercentNumberPosition), PercentNumberPosition.Value, static p => p.PercentNumberPosition, static (p, v) => p.PercentNumberPosition = v);
        }

        if (Radius.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Radius), Radius.Value, static p => p.Radius, static (p, v) => p.Radius = v);
        }

        if (Reversed.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Reversed), Reversed.Value, static p => p.Reversed, static (p, v) => p.Reversed = v);
        }

        if (Rounded.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Rounded), Rounded.Value, static p => p.Rounded, static (p, v) => p.Rounded = v);
        }

        if (SegmentGap.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(SegmentGap), SegmentGap.Value, static p => p.SegmentGap, static (p, v) => p.SegmentGap = v);
        }

        if (Segments.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Segments), Segments, static p => p.Segments, static (p, v) => p.Segments = v);
        }

        if (Shape.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Shape), Shape, static p => p.Shape, static (p, v) => p.Shape = v);
        }

        if (ShowPercentNumber.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(ShowPercentNumber), ShowPercentNumber.Value, static p => p.ShowPercentNumber, static (p, v) => p.ShowPercentNumber = v);
        }

        if (Size.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Size), Size, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Striped.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Striped), Striped.Value, static p => p.Striped, static (p, v) => p.Striped = v);
        }

        if (StripedAnimation.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(StripedAnimation), StripedAnimation.Value, static p => p.StripedAnimation, static (p, v) => p.StripedAnimation = v);
        }

        if (Styles is not null)
        {
            bitProgress.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (Thickness.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Thickness), Thickness, static p => p.Thickness, static (p, v) => p.Thickness = v);
        }

        if (TrackColor.HasValue())
        {
            bitProgress.TakeFromCascade(nameof(TrackColor), TrackColor, static p => p.TrackColor, static (p, v) => p.TrackColor = v);
        }

        if (Vertical.HasValue)
        {
            bitProgress.TakeFromCascade(nameof(Vertical), Vertical.Value, static p => p.Vertical, static (p, v) => p.Vertical = v);
        }
    }
}
