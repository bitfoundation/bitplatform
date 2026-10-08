namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitSlider"/> component.
/// </summary>
/// <remarks>
/// Everything a group of sliders can share is here - the scale, the step, the marks, the colors, the sizes
/// and the accessible names of the two ends of a range. What each slider holds of its own is not: a value,
/// a default value, a name, the templates and the event callbacks stay on the markup of the slider itself.
/// </remarks>
public class BitSliderParams : BitInputBaseParams<double>, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSlider"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSlider value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSlider)}";



    public string Name => ParamName;



    /// <summary>
    /// A description of the slider for the benefit of screen readers, rendered into a visually hidden element
    /// that every thumb references through its <c>aria-describedby</c> attribute.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// A text description of the Slider number value for the benefit of screen readers, which becomes the
    /// <c>aria-valuetext</c> of every thumb.
    /// </summary>
    public Func<double, string>? AriaValueText { get; set; }

    /// <summary>
    /// If true, the slider automatically receives focus when the page renders.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the slider.
    /// </summary>
    public BitSliderClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the slider, applied to the filled part of the track and to the thumbs.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Lets the filled band between the two thumbs of a ranged slider be dragged bodily.
    /// </summary>
    public bool? DraggableTrack { get; set; }

    /// <summary>
    /// Builds the text of every label the slider draws from the value it stands for. Takes precedence over
    /// <see cref="ValueFormat"/>.
    /// </summary>
    public Func<double, string>? GetValueText { get; set; }

    /// <summary>
    /// Fills the track from the far end instead of from the near one.
    /// </summary>
    public bool? Inverted { get; set; }

    /// <summary>
    /// Attaches the origin of the filled part of the track to zero.
    /// </summary>
    public bool? IsOriginFromZero { get; set; }

    /// <summary>
    /// Gives the slider a second thumb, so what it selects is a range rather than a single value.
    /// </summary>
    public bool? IsRanged { get; set; }

    /// <summary>
    /// Stands the slider upright and fills it from the bottom up.
    /// </summary>
    public bool? IsVertical { get; set; }

    /// <summary>
    /// The distance the larger keyboard jumps cover: <c>Page Up</c> and <c>Page Down</c>, and an arrow key
    /// held with <c>Shift</c>.
    /// </summary>
    public double? LargeStep { get; set; }

    /// <summary>
    /// The accessible name of the lower thumb of a ranged slider.
    /// </summary>
    public string? LowerAriaLabel { get; set; }

    /// <summary>
    /// The marks drawn along the track of the slider, each optionally carrying a label under it.
    /// </summary>
    public IEnumerable<BitSliderMark>? Marks { get; set; }

    /// <summary>
    /// The interval between the generated marks, which setting is enough to ask for them.
    /// </summary>
    public double? MarkStep { get; set; }

    /// <summary>
    /// The max value of the slider.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// The largest distance the two thumbs of a ranged slider are allowed to be apart.
    /// </summary>
    public double? MaxRange { get; set; }

    /// <summary>
    /// The min value of the slider.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// The smallest distance the two thumbs of a ranged slider are allowed to be apart.
    /// </summary>
    public double? MinRange { get; set; }

    /// <summary>
    /// Leaves the track unfilled, so the slider is a bare rail with a thumb on it.
    /// </summary>
    public bool? NoFill { get; set; }

    /// <summary>
    /// Stops the two thumbs of a ranged slider from crossing over each other.
    /// </summary>
    public bool? NoSwap { get; set; }

    /// <summary>
    /// The value the filled part of the track grows out of, instead of the near end of the track.
    /// </summary>
    public double? Origin { get; set; }

    /// <summary>
    /// Lets a thumb of a ranged slider push the other one along instead of stopping against it.
    /// </summary>
    public bool? Pushable { get; set; }

    /// <summary>
    /// Restricts the values the user can pick to the marks alone.
    /// </summary>
    public bool? RestrictToMarks { get; set; }

    /// <summary>
    /// Writes each mark's own value underneath it, which setting is enough to ask for the marks as well.
    /// </summary>
    public bool? ShowMarkLabels { get; set; }

    /// <summary>
    /// Draws a mark at every <see cref="MarkStep"/> (or every <see cref="Step"/> when that is not set).
    /// </summary>
    public bool? ShowMarks { get; set; }

    /// <summary>
    /// Whether to show the value beside the slider.
    /// </summary>
    public bool? ShowValue { get; set; }

    /// <summary>
    /// Size of the slider, which scales its track, thumbs and labels together.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Additional HTML attributes for the slider box, the element the track and the inputs are laid inside of.
    /// </summary>
    public Dictionary<string, object>? SliderBoxHtmlAttributes { get; set; }

    /// <summary>
    /// The difference between the two adjacent values of the slider.
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the slider.
    /// </summary>
    public BitSliderClassStyles? Styles { get; set; }

    /// <summary>
    /// Decides when the floating label that rides along with the thumb is shown.
    /// </summary>
    public BitSliderThumbLabel? ThumbLabel { get; set; }

    /// <summary>
    /// The accessible name of the upper thumb of a ranged slider.
    /// </summary>
    public string? UpperAriaLabel { get; set; }

    /// <summary>
    /// Custom format for the displayed value of the slider, applied to the value labels, the mark labels and
    /// the floating thumb labels alike.
    /// </summary>
    public string? ValueFormat { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSlider"/> instance with any values that have been set
    /// on this object, if those properties have not already been set on the <see cref="BitSlider"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSlider"/> will
    /// be updated. This method does not overwrite existing values on <paramref name="bitSlider"/>.
    /// </remarks>
    /// <param name="bitSlider">
    /// The <see cref="BitSlider"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSlider bitSlider)
    {
        if (bitSlider is null) return;

        UpdateInputBaseParameters(bitSlider);

        if (AriaDescription.HasValue())
        {
            bitSlider.TakeFromCascade(nameof(AriaDescription), AriaDescription, static s => s.AriaDescription, static (s, v) => s.AriaDescription = v);
        }

        if (AriaValueText is not null)
        {
            bitSlider.TakeFromCascade(nameof(AriaValueText), AriaValueText, static s => s.AriaValueText, static (s, v) => s.AriaValueText = v);
        }

        if (AutoFocus.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static s => s.AutoFocus, static (s, v) => s.AutoFocus = v);
        }

        if (Classes is not null)
        {
            bitSlider.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Color.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Color), Color.Value, static s => s.Color, static (s, v) => s.Color = v);
        }

        if (DraggableTrack.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(DraggableTrack), DraggableTrack.Value, static s => s.DraggableTrack, static (s, v) => s.DraggableTrack = v);
        }

        if (GetValueText is not null)
        {
            bitSlider.TakeFromCascade(nameof(GetValueText), GetValueText, static s => s.GetValueText, static (s, v) => s.GetValueText = v);
        }

        if (Inverted.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Inverted), Inverted.Value, static s => s.Inverted, static (s, v) => s.Inverted = v);
        }

        if (IsOriginFromZero.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(IsOriginFromZero), IsOriginFromZero.Value, static s => s.IsOriginFromZero, static (s, v) => s.IsOriginFromZero = v);
        }

        if (IsRanged.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(IsRanged), IsRanged.Value, static s => s.IsRanged, static (s, v) => s.IsRanged = v);
        }

        if (IsVertical.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(IsVertical), IsVertical.Value, static s => s.IsVertical, static (s, v) => s.IsVertical = v);
        }

        if (LargeStep.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(LargeStep), LargeStep.Value, static s => s.LargeStep, static (s, v) => s.LargeStep = v);
        }

        if (LowerAriaLabel.HasValue())
        {
            bitSlider.TakeFromCascade(nameof(LowerAriaLabel), LowerAriaLabel, static s => s.LowerAriaLabel, static (s, v) => s.LowerAriaLabel = v);
        }

        if (Marks is not null)
        {
            bitSlider.TakeFromCascade(nameof(Marks), Marks, static s => s.Marks, static (s, v) => s.Marks = v);
        }

        if (MarkStep.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(MarkStep), MarkStep.Value, static s => s.MarkStep, static (s, v) => s.MarkStep = v);
        }

        if (Max.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Max), Max.Value, static s => s.Max, static (s, v) => s.Max = v);
        }

        if (MaxRange.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(MaxRange), MaxRange.Value, static s => s.MaxRange, static (s, v) => s.MaxRange = v);
        }

        if (Min.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Min), Min.Value, static s => s.Min, static (s, v) => s.Min = v);
        }

        if (MinRange.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(MinRange), MinRange.Value, static s => s.MinRange, static (s, v) => s.MinRange = v);
        }

        if (NoFill.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(NoFill), NoFill.Value, static s => s.NoFill, static (s, v) => s.NoFill = v);
        }

        if (NoSwap.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(NoSwap), NoSwap.Value, static s => s.NoSwap, static (s, v) => s.NoSwap = v);
        }

        if (Origin.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Origin), Origin.Value, static s => s.Origin, static (s, v) => s.Origin = v);
        }

        if (Pushable.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Pushable), Pushable.Value, static s => s.Pushable, static (s, v) => s.Pushable = v);
        }

        if (RestrictToMarks.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(RestrictToMarks), RestrictToMarks.Value, static s => s.RestrictToMarks, static (s, v) => s.RestrictToMarks = v);
        }

        if (ShowMarkLabels.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(ShowMarkLabels), ShowMarkLabels.Value, static s => s.ShowMarkLabels, static (s, v) => s.ShowMarkLabels = v);
        }

        if (ShowMarks.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(ShowMarks), ShowMarks.Value, static s => s.ShowMarks, static (s, v) => s.ShowMarks = v);
        }

        if (ShowValue.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(ShowValue), ShowValue.Value, static s => s.ShowValue, static (s, v) => s.ShowValue = v);
        }

        if (Size.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (SliderBoxHtmlAttributes is not null)
        {
            bitSlider.TakeFromCascade(nameof(SliderBoxHtmlAttributes), SliderBoxHtmlAttributes, static s => s.SliderBoxHtmlAttributes, static (s, v) => s.SliderBoxHtmlAttributes = v);
        }

        if (Step.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(Step), Step.Value, static s => s.Step, static (s, v) => s.Step = v);
        }

        if (Styles is not null)
        {
            bitSlider.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (ThumbLabel.HasValue)
        {
            bitSlider.TakeFromCascade(nameof(ThumbLabel), ThumbLabel.Value, static s => s.ThumbLabel, static (s, v) => s.ThumbLabel = v);
        }

        if (UpperAriaLabel.HasValue())
        {
            bitSlider.TakeFromCascade(nameof(UpperAriaLabel), UpperAriaLabel, static s => s.UpperAriaLabel, static (s, v) => s.UpperAriaLabel = v);
        }

        if (ValueFormat.HasValue())
        {
            bitSlider.TakeFromCascade(nameof(ValueFormat), ValueFormat, static s => s.ValueFormat, static (s, v) => s.ValueFormat = v);
        }
    }
}
