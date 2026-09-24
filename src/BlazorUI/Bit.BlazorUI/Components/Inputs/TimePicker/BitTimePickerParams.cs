using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTimePicker"/> component.
/// </summary>
/// <remarks>
/// What a cascade of these carries is the shared configuration of the time pickers of a form or a page -
/// the clock format, the steps, the selectable range, the look. What stays on the instance is what belongs
/// to one field alone: its value and the callbacks reporting a change of it, the open state of its callout,
/// and the rejection it is currently showing (<see cref="BitTimePicker.Invalid"/> and its message), which
/// no two fields ever share.
/// </remarks>
public class BitTimePickerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTimePicker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTimePicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTimePicker)}";



    public string Name => ParamName;



    /// <inheritdoc cref="BitTimePicker.AllowTextInput"/>
    public bool? AllowTextInput { get; set; }

    /// <inheritdoc cref="BitTimePicker.AllowedHours"/>
    public Func<int, bool>? AllowedHours { get; set; }

    /// <inheritdoc cref="BitTimePicker.AllowedMinutes"/>
    public Func<int, bool>? AllowedMinutes { get; set; }

    /// <inheritdoc cref="BitTimePicker.AllowedSeconds"/>
    public Func<int, bool>? AllowedSeconds { get; set; }

    /// <inheritdoc cref="BitTimePicker.AriaDescription"/>
    public string? AriaDescription { get; set; }

    /// <inheritdoc cref="BitTimePicker.AutoFocus"/>
    public bool? AutoFocus { get; set; }

    /// <inheritdoc cref="BitTimePicker.CalloutAriaLabel"/>
    public string? CalloutAriaLabel { get; set; }

    /// <inheritdoc cref="BitTimePicker.CalloutFooterTemplate"/>
    public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <inheritdoc cref="BitTimePicker.CalloutHeaderTemplate"/>
    public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <inheritdoc cref="BitTimePicker.CalloutHtmlAttributes"/>
    public Dictionary<string, object>? CalloutHtmlAttributes { get; set; }

    /// <inheritdoc cref="BitTimePicker.Classes"/>
    public BitTimePickerClassStyles? Classes { get; set; }

    /// <inheritdoc cref="BitTimePicker.ClearButtonIcon"/>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.ClearButtonIconName"/>
    public string? ClearButtonIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.ClearButtonText"/>
    public string? ClearButtonText { get; set; }

    /// <inheritdoc cref="BitTimePicker.ClearButtonTitle"/>
    public string? ClearButtonTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.CloseButtonIcon"/>
    public BitIconInfo? CloseButtonIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.CloseButtonIconName"/>
    public string? CloseButtonIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.CloseButtonTitle"/>
    public string? CloseButtonTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.Color"/>
    public BitColor? Color { get; set; }

    /// <inheritdoc cref="BitTimePicker.ContinuousSpinDelay"/>
    public int? ContinuousSpinDelay { get; set; }

    /// <inheritdoc cref="BitTimePicker.ContinuousSpinInterval"/>
    public int? ContinuousSpinInterval { get; set; }

    /// <inheritdoc cref="BitTimePicker.Culture"/>
    public CultureInfo? Culture { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseHourIcon"/>
    public BitIconInfo? DecreaseHourIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseHourIconName"/>
    public string? DecreaseHourIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseHourTitle"/>
    public string? DecreaseHourTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseMinuteIcon"/>
    public BitIconInfo? DecreaseMinuteIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseMinuteIconName"/>
    public string? DecreaseMinuteIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseMinuteTitle"/>
    public string? DecreaseMinuteTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseSecondIcon"/>
    public BitIconInfo? DecreaseSecondIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseSecondIconName"/>
    public string? DecreaseSecondIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.DecreaseSecondTitle"/>
    public string? DecreaseSecondTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.Description"/>
    public string? Description { get; set; }

    /// <inheritdoc cref="BitTimePicker.DescriptionTemplate"/>
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <inheritdoc cref="BitTimePicker.DisallowedTimeErrorMessage"/>
    public string? DisallowedTimeErrorMessage { get; set; }

    /// <inheritdoc cref="BitTimePicker.DisableFuture"/>
    public bool? DisableFuture { get; set; }

    /// <inheritdoc cref="BitTimePicker.DisablePast"/>
    public bool? DisablePast { get; set; }

    /// <inheritdoc cref="BitTimePicker.DropDirection"/>
    public BitDropDirection? DropDirection { get; set; }

    /// <inheritdoc cref="BitTimePicker.HasBorder"/>
    public bool? HasBorder { get; set; }

    /// <inheritdoc cref="BitTimePicker.HourInputAriaLabel"/>
    public string? HourInputAriaLabel { get; set; }

    /// <inheritdoc cref="BitTimePicker.HourStep"/>
    public int? HourStep { get; set; }

    /// <inheritdoc cref="BitTimePicker.Icon"/>
    public BitIconInfo? Icon { get; set; }

    /// <inheritdoc cref="BitTimePicker.IconName"/>
    public string? IconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.IconLocation"/>
    public BitIconLocation? IconLocation { get; set; }

    /// <inheritdoc cref="BitTimePicker.IconTemplate"/>
    public RenderFragment? IconTemplate { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseHourIcon"/>
    public BitIconInfo? IncreaseHourIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseHourIconName"/>
    public string? IncreaseHourIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseHourTitle"/>
    public string? IncreaseHourTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseMinuteIcon"/>
    public BitIconInfo? IncreaseMinuteIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseMinuteIconName"/>
    public string? IncreaseMinuteIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseMinuteTitle"/>
    public string? IncreaseMinuteTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseSecondIcon"/>
    public BitIconInfo? IncreaseSecondIcon { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseSecondIconName"/>
    public string? IncreaseSecondIconName { get; set; }

    /// <inheritdoc cref="BitTimePicker.IncreaseSecondTitle"/>
    public string? IncreaseSecondTitle { get; set; }

    /// <inheritdoc cref="BitTimePicker.InvalidErrorMessage"/>
    public string? InvalidErrorMessage { get; set; }

    /// <inheritdoc cref="BitTimePicker.Label"/>
    public string? Label { get; set; }

    /// <inheritdoc cref="BitTimePicker.LabelTemplate"/>
    public RenderFragment? LabelTemplate { get; set; }

    /// <inheritdoc cref="BitTimePicker.MaxTime"/>
    public TimeSpan? MaxTime { get; set; }

    /// <inheritdoc cref="BitTimePicker.MinTime"/>
    public TimeSpan? MinTime { get; set; }

    /// <inheritdoc cref="BitTimePicker.MinuteInputAriaLabel"/>
    public string? MinuteInputAriaLabel { get; set; }

    /// <inheritdoc cref="BitTimePicker.MinuteStep"/>
    public int? MinuteStep { get; set; }

    /// <inheritdoc cref="BitTimePicker.NowButtonText"/>
    public string? NowButtonText { get; set; }

    /// <inheritdoc cref="BitTimePicker.OutOfRangeErrorMessage"/>
    public string? OutOfRangeErrorMessage { get; set; }

    /// <inheritdoc cref="BitTimePicker.Placeholder"/>
    public string? Placeholder { get; set; }

    /// <inheritdoc cref="BitTimePicker.Responsive"/>
    public bool? Responsive { get; set; }

    /// <inheritdoc cref="BitTimePicker.SecondInputAriaLabel"/>
    public string? SecondInputAriaLabel { get; set; }

    /// <inheritdoc cref="BitTimePicker.SecondStep"/>
    public int? SecondStep { get; set; }

    /// <inheritdoc cref="BitTimePicker.ShowClearButton"/>
    public bool? ShowClearButton { get; set; }

    /// <inheritdoc cref="BitTimePicker.ShowCloseButton"/>
    public bool? ShowCloseButton { get; set; }

    /// <inheritdoc cref="BitTimePicker.ShowInputClearButton"/>
    public bool? ShowInputClearButton { get; set; }

    /// <inheritdoc cref="BitTimePicker.ShowNowButton"/>
    public bool? ShowNowButton { get; set; }

    /// <inheritdoc cref="BitTimePicker.ShowSeconds"/>
    public bool? ShowSeconds { get; set; }

    /// <inheritdoc cref="BitTimePicker.Size"/>
    public BitSize? Size { get; set; }

    /// <inheritdoc cref="BitTimePicker.Standalone"/>
    public bool? Standalone { get; set; }

    /// <inheritdoc cref="BitTimePicker.StartingValue"/>
    public TimeSpan? StartingValue { get; set; }

    /// <inheritdoc cref="BitTimePicker.Styles"/>
    public BitTimePickerClassStyles? Styles { get; set; }

    /// <inheritdoc cref="BitTimePicker.TimeFormat"/>
    public BitTimeFormat? TimeFormat { get; set; }

    /// <inheritdoc cref="BitTimePicker.Underlined"/>
    public bool? Underlined { get; set; }

    /// <inheritdoc cref="BitTimePicker.ValueFormat"/>
    public string? ValueFormat { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitTimePicker"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTimePicker"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTimePicker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTimePicker"/>.
    /// </remarks>
    /// <param name="bitTimePicker">
    /// The <see cref="BitTimePicker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitTimePicker bitTimePicker)
    {
        if (bitTimePicker is null) return;

        UpdateBaseParameters(bitTimePicker);

        if (AllowTextInput.HasValue && bitTimePicker.HasNotBeenSet(nameof(AllowTextInput)))
        {
            bitTimePicker.AllowTextInput = AllowTextInput.Value;
        }

        if (AllowedHours is not null && bitTimePicker.HasNotBeenSet(nameof(AllowedHours)))
        {
            bitTimePicker.AllowedHours = AllowedHours;
        }

        if (AllowedMinutes is not null && bitTimePicker.HasNotBeenSet(nameof(AllowedMinutes)))
        {
            bitTimePicker.AllowedMinutes = AllowedMinutes;
        }

        if (AllowedSeconds is not null && bitTimePicker.HasNotBeenSet(nameof(AllowedSeconds)))
        {
            bitTimePicker.AllowedSeconds = AllowedSeconds;
        }

        if (AriaDescription.HasValue() && bitTimePicker.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitTimePicker.AriaDescription = AriaDescription;
        }

        if (AutoFocus.HasValue && bitTimePicker.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitTimePicker.AutoFocus = AutoFocus.Value;
        }

        if (CalloutAriaLabel.HasValue() && bitTimePicker.HasNotBeenSet(nameof(CalloutAriaLabel)))
        {
            bitTimePicker.CalloutAriaLabel = CalloutAriaLabel!;
        }

        if (CalloutFooterTemplate is not null && bitTimePicker.HasNotBeenSet(nameof(CalloutFooterTemplate)))
        {
            bitTimePicker.CalloutFooterTemplate = CalloutFooterTemplate;
        }

        if (CalloutHeaderTemplate is not null && bitTimePicker.HasNotBeenSet(nameof(CalloutHeaderTemplate)))
        {
            bitTimePicker.CalloutHeaderTemplate = CalloutHeaderTemplate;
        }

        if (CalloutHtmlAttributes is not null)
        {
            foreach (var attr in CalloutHtmlAttributes)
            {
                if (bitTimePicker.CalloutHtmlAttributes.ContainsKey(attr.Key)) continue;

                bitTimePicker.CalloutHtmlAttributes[attr.Key] = attr.Value;
            }
        }

        if (Classes is not null && bitTimePicker.HasNotBeenSet(nameof(Classes)))
        {
            bitTimePicker.Classes = Classes;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (ClearButtonIcon is not null && bitTimePicker.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitTimePicker.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitTimePicker.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearButtonText.HasValue() && bitTimePicker.HasNotBeenSet(nameof(ClearButtonText)))
        {
            bitTimePicker.ClearButtonText = ClearButtonText!;
        }

        if (ClearButtonTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(ClearButtonTitle)))
        {
            bitTimePicker.ClearButtonTitle = ClearButtonTitle!;
        }

        if (CloseButtonIcon is not null && bitTimePicker.HasNotBeenSet(nameof(CloseButtonIcon)))
        {
            bitTimePicker.CloseButtonIcon = CloseButtonIcon;
        }

        if (CloseButtonIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(CloseButtonIconName)))
        {
            bitTimePicker.CloseButtonIconName = CloseButtonIconName;
        }

        if (CloseButtonTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitTimePicker.CloseButtonTitle = CloseButtonTitle!;
        }

        if (Color.HasValue && bitTimePicker.HasNotBeenSet(nameof(Color)))
        {
            bitTimePicker.Color = Color.Value;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (ContinuousSpinDelay.HasValue && bitTimePicker.HasNotBeenSet(nameof(ContinuousSpinDelay)))
        {
            bitTimePicker.ContinuousSpinDelay = ContinuousSpinDelay.Value;
        }

        if (ContinuousSpinInterval.HasValue && bitTimePicker.HasNotBeenSet(nameof(ContinuousSpinInterval)))
        {
            bitTimePicker.ContinuousSpinInterval = ContinuousSpinInterval.Value;
        }

        // The culture decides how the value is written, how a typed one is read, and which way the picker
        // lays itself out, and the component reads all three off it in a hook that has already run by the
        // time this cascade reaches it - so the hook is run once more below.
        var recomputeCulture = false;

        if (Culture is not null && bitTimePicker.HasNotBeenSet(nameof(Culture)))
        {
            bitTimePicker.Culture = Culture;

            recomputeCulture = true;
        }

        if (DecreaseHourIcon is not null && bitTimePicker.HasNotBeenSet(nameof(DecreaseHourIcon)))
        {
            bitTimePicker.DecreaseHourIcon = DecreaseHourIcon;
        }

        if (DecreaseHourIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DecreaseHourIconName)))
        {
            bitTimePicker.DecreaseHourIconName = DecreaseHourIconName;
        }

        if (DecreaseHourTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DecreaseHourTitle)))
        {
            bitTimePicker.DecreaseHourTitle = DecreaseHourTitle!;
        }

        if (DecreaseMinuteIcon is not null && bitTimePicker.HasNotBeenSet(nameof(DecreaseMinuteIcon)))
        {
            bitTimePicker.DecreaseMinuteIcon = DecreaseMinuteIcon;
        }

        if (DecreaseMinuteIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DecreaseMinuteIconName)))
        {
            bitTimePicker.DecreaseMinuteIconName = DecreaseMinuteIconName;
        }

        if (DecreaseMinuteTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DecreaseMinuteTitle)))
        {
            bitTimePicker.DecreaseMinuteTitle = DecreaseMinuteTitle!;
        }

        if (DecreaseSecondIcon is not null && bitTimePicker.HasNotBeenSet(nameof(DecreaseSecondIcon)))
        {
            bitTimePicker.DecreaseSecondIcon = DecreaseSecondIcon;
        }

        if (DecreaseSecondIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DecreaseSecondIconName)))
        {
            bitTimePicker.DecreaseSecondIconName = DecreaseSecondIconName;
        }

        if (DecreaseSecondTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DecreaseSecondTitle)))
        {
            bitTimePicker.DecreaseSecondTitle = DecreaseSecondTitle!;
        }

        if (Description.HasValue() && bitTimePicker.HasNotBeenSet(nameof(Description)))
        {
            bitTimePicker.Description = Description;
        }

        if (DescriptionTemplate is not null && bitTimePicker.HasNotBeenSet(nameof(DescriptionTemplate)))
        {
            bitTimePicker.DescriptionTemplate = DescriptionTemplate;
        }

        if (DisallowedTimeErrorMessage.HasValue() && bitTimePicker.HasNotBeenSet(nameof(DisallowedTimeErrorMessage)))
        {
            bitTimePicker.DisallowedTimeErrorMessage = DisallowedTimeErrorMessage;
        }

        if (DisableFuture.HasValue && bitTimePicker.HasNotBeenSet(nameof(DisableFuture)))
        {
            bitTimePicker.DisableFuture = DisableFuture.Value;
        }

        if (DisablePast.HasValue && bitTimePicker.HasNotBeenSet(nameof(DisablePast)))
        {
            bitTimePicker.DisablePast = DisablePast.Value;
        }

        if (DropDirection.HasValue && bitTimePicker.HasNotBeenSet(nameof(DropDirection)))
        {
            bitTimePicker.DropDirection = DropDirection.Value;
        }

        if (HasBorder.HasValue && bitTimePicker.HasNotBeenSet(nameof(HasBorder)))
        {
            bitTimePicker.HasBorder = HasBorder.Value;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (HourInputAriaLabel.HasValue() && bitTimePicker.HasNotBeenSet(nameof(HourInputAriaLabel)))
        {
            bitTimePicker.HourInputAriaLabel = HourInputAriaLabel!;
        }

        if (HourStep.HasValue && bitTimePicker.HasNotBeenSet(nameof(HourStep)))
        {
            bitTimePicker.HourStep = HourStep.Value;
        }

        if (Icon is not null && bitTimePicker.HasNotBeenSet(nameof(Icon)))
        {
            bitTimePicker.Icon = Icon;
        }

        if (IconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IconName)))
        {
            bitTimePicker.IconName = IconName;
        }

        if (IconLocation.HasValue && bitTimePicker.HasNotBeenSet(nameof(IconLocation)))
        {
            bitTimePicker.IconLocation = IconLocation.Value;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (IconTemplate is not null && bitTimePicker.HasNotBeenSet(nameof(IconTemplate)))
        {
            bitTimePicker.IconTemplate = IconTemplate;
        }

        if (IncreaseHourIcon is not null && bitTimePicker.HasNotBeenSet(nameof(IncreaseHourIcon)))
        {
            bitTimePicker.IncreaseHourIcon = IncreaseHourIcon;
        }

        if (IncreaseHourIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IncreaseHourIconName)))
        {
            bitTimePicker.IncreaseHourIconName = IncreaseHourIconName;
        }

        if (IncreaseHourTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IncreaseHourTitle)))
        {
            bitTimePicker.IncreaseHourTitle = IncreaseHourTitle!;
        }

        if (IncreaseMinuteIcon is not null && bitTimePicker.HasNotBeenSet(nameof(IncreaseMinuteIcon)))
        {
            bitTimePicker.IncreaseMinuteIcon = IncreaseMinuteIcon;
        }

        if (IncreaseMinuteIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IncreaseMinuteIconName)))
        {
            bitTimePicker.IncreaseMinuteIconName = IncreaseMinuteIconName;
        }

        if (IncreaseMinuteTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IncreaseMinuteTitle)))
        {
            bitTimePicker.IncreaseMinuteTitle = IncreaseMinuteTitle!;
        }

        if (IncreaseSecondIcon is not null && bitTimePicker.HasNotBeenSet(nameof(IncreaseSecondIcon)))
        {
            bitTimePicker.IncreaseSecondIcon = IncreaseSecondIcon;
        }

        if (IncreaseSecondIconName.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IncreaseSecondIconName)))
        {
            bitTimePicker.IncreaseSecondIconName = IncreaseSecondIconName;
        }

        if (IncreaseSecondTitle.HasValue() && bitTimePicker.HasNotBeenSet(nameof(IncreaseSecondTitle)))
        {
            bitTimePicker.IncreaseSecondTitle = IncreaseSecondTitle!;
        }

        if (InvalidErrorMessage.HasValue() && bitTimePicker.HasNotBeenSet(nameof(InvalidErrorMessage)))
        {
            bitTimePicker.InvalidErrorMessage = InvalidErrorMessage;
        }

        if (Label.HasValue() && bitTimePicker.HasNotBeenSet(nameof(Label)))
        {
            bitTimePicker.Label = Label;
        }

        if (LabelTemplate is not null && bitTimePicker.HasNotBeenSet(nameof(LabelTemplate)))
        {
            bitTimePicker.LabelTemplate = LabelTemplate;
        }

        if (MaxTime.HasValue && bitTimePicker.HasNotBeenSet(nameof(MaxTime)))
        {
            bitTimePicker.MaxTime = MaxTime.Value;
        }

        if (MinTime.HasValue && bitTimePicker.HasNotBeenSet(nameof(MinTime)))
        {
            bitTimePicker.MinTime = MinTime.Value;
        }

        if (MinuteInputAriaLabel.HasValue() && bitTimePicker.HasNotBeenSet(nameof(MinuteInputAriaLabel)))
        {
            bitTimePicker.MinuteInputAriaLabel = MinuteInputAriaLabel!;
        }

        if (MinuteStep.HasValue && bitTimePicker.HasNotBeenSet(nameof(MinuteStep)))
        {
            bitTimePicker.MinuteStep = MinuteStep.Value;
        }

        if (NowButtonText.HasValue() && bitTimePicker.HasNotBeenSet(nameof(NowButtonText)))
        {
            bitTimePicker.NowButtonText = NowButtonText!;
        }

        if (OutOfRangeErrorMessage.HasValue() && bitTimePicker.HasNotBeenSet(nameof(OutOfRangeErrorMessage)))
        {
            bitTimePicker.OutOfRangeErrorMessage = OutOfRangeErrorMessage;
        }

        if (Placeholder.HasValue() && bitTimePicker.HasNotBeenSet(nameof(Placeholder)))
        {
            bitTimePicker.Placeholder = Placeholder;
        }

        if (Responsive.HasValue && bitTimePicker.HasNotBeenSet(nameof(Responsive)))
        {
            bitTimePicker.Responsive = Responsive.Value;
        }

        if (SecondInputAriaLabel.HasValue() && bitTimePicker.HasNotBeenSet(nameof(SecondInputAriaLabel)))
        {
            bitTimePicker.SecondInputAriaLabel = SecondInputAriaLabel!;
        }

        if (SecondStep.HasValue && bitTimePicker.HasNotBeenSet(nameof(SecondStep)))
        {
            bitTimePicker.SecondStep = SecondStep.Value;
        }

        if (ShowClearButton.HasValue && bitTimePicker.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitTimePicker.ShowClearButton = ShowClearButton.Value;
        }

        if (ShowCloseButton.HasValue && bitTimePicker.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitTimePicker.ShowCloseButton = ShowCloseButton.Value;
        }

        if (ShowInputClearButton.HasValue && bitTimePicker.HasNotBeenSet(nameof(ShowInputClearButton)))
        {
            bitTimePicker.ShowInputClearButton = ShowInputClearButton.Value;
        }

        if (ShowNowButton.HasValue && bitTimePicker.HasNotBeenSet(nameof(ShowNowButton)))
        {
            bitTimePicker.ShowNowButton = ShowNowButton.Value;
        }

        if (ShowSeconds.HasValue && bitTimePicker.HasNotBeenSet(nameof(ShowSeconds)))
        {
            bitTimePicker.ShowSeconds = ShowSeconds.Value;
        }

        if (Size.HasValue && bitTimePicker.HasNotBeenSet(nameof(Size)))
        {
            bitTimePicker.Size = Size.Value;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (Standalone.HasValue && bitTimePicker.HasNotBeenSet(nameof(Standalone)))
        {
            bitTimePicker.Standalone = Standalone.Value;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (StartingValue.HasValue && bitTimePicker.HasNotBeenSet(nameof(StartingValue)))
        {
            bitTimePicker.StartingValue = StartingValue.Value;
        }

        if (Styles is not null && bitTimePicker.HasNotBeenSet(nameof(Styles)))
        {
            bitTimePicker.Styles = Styles;

            bitTimePicker.StyleBuilder.Reset();
        }

        if (TimeFormat.HasValue && bitTimePicker.HasNotBeenSet(nameof(TimeFormat)))
        {
            bitTimePicker.TimeFormat = TimeFormat.Value;
        }

        if (Underlined.HasValue && bitTimePicker.HasNotBeenSet(nameof(Underlined)))
        {
            bitTimePicker.Underlined = Underlined.Value;

            bitTimePicker.ClassBuilder.Reset();
        }

        if (ValueFormat.HasValue() && bitTimePicker.HasNotBeenSet(nameof(ValueFormat)))
        {
            bitTimePicker.ValueFormat = ValueFormat;
        }

        if (recomputeCulture)
        {
            bitTimePicker.OnSetCulture();

            bitTimePicker.ClassBuilder.Reset();
        }
    }
}
