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

    /// <inheritdoc cref="BitTimePicker.AutoAdvance"/>
    public bool? AutoAdvance { get; set; }

    /// <inheritdoc cref="BitTimePicker.AutoClose"/>
    public bool? AutoClose { get; set; }

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

    /// <inheritdoc cref="BitTimePicker.IconPlacement"/>
    public BitPlacement? IconPlacement { get; set; }

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

    /// <inheritdoc cref="BitTimePicker.InvertMouseWheel"/>
    public bool? InvertMouseWheel { get; set; }

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

    /// <inheritdoc cref="BitTimePicker.NoMouseWheel"/>
    public bool? NoMouseWheel { get; set; }

    /// <inheritdoc cref="BitTimePicker.Now"/>
    public TimeSpan? Now { get; set; }

    /// <inheritdoc cref="BitTimePicker.NowButtonText"/>
    public string? NowButtonText { get; set; }

    /// <inheritdoc cref="BitTimePicker.OutOfRangeErrorMessage"/>
    public string? OutOfRangeErrorMessage { get; set; }

    /// <inheritdoc cref="BitTimePicker.Placeholder"/>
    public string? Placeholder { get; set; }

    /// <inheritdoc cref="BitTimePicker.Prefix"/>
    public string? Prefix { get; set; }

    /// <inheritdoc cref="BitTimePicker.PrefixTemplate"/>
    public RenderFragment? PrefixTemplate { get; set; }

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

    /// <inheritdoc cref="BitTimePicker.Suffix"/>
    public string? Suffix { get; set; }

    /// <inheritdoc cref="BitTimePicker.SuffixTemplate"/>
    public RenderFragment? SuffixTemplate { get; set; }

    /// <inheritdoc cref="BitTimePicker.TimeFormat"/>
    public BitTimeFormat? TimeFormat { get; set; }

    /// <inheritdoc cref="BitTimePicker.TimeZone"/>
    public TimeZoneInfo? TimeZone { get; set; }

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

        if (AllowTextInput.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(AllowTextInput), AllowTextInput.Value, static t => t.AllowTextInput, static (t, v) => t.AllowTextInput = v);
        }

        if (AllowedHours is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(AllowedHours), AllowedHours, static t => t.AllowedHours, static (t, v) => t.AllowedHours = v);
        }

        if (AllowedMinutes is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(AllowedMinutes), AllowedMinutes, static t => t.AllowedMinutes, static (t, v) => t.AllowedMinutes = v);
        }

        if (AllowedSeconds is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(AllowedSeconds), AllowedSeconds, static t => t.AllowedSeconds, static (t, v) => t.AllowedSeconds = v);
        }

        if (AriaDescription.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(AriaDescription), AriaDescription, static t => t.AriaDescription, static (t, v) => t.AriaDescription = v);
        }

        if (AutoAdvance.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(AutoAdvance), AutoAdvance.Value, static t => t.AutoAdvance, static (t, v) => t.AutoAdvance = v);
        }

        if (AutoClose.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(AutoClose), AutoClose.Value, static t => t.AutoClose, static (t, v) => t.AutoClose = v);
        }

        if (AutoFocus.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static t => t.AutoFocus, static (t, v) => t.AutoFocus = v);
        }

        if (CalloutAriaLabel.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(CalloutAriaLabel), CalloutAriaLabel!, static t => t.CalloutAriaLabel, static (t, v) => t.CalloutAriaLabel = v);
        }

        if (CalloutFooterTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(CalloutFooterTemplate), CalloutFooterTemplate, static t => t.CalloutFooterTemplate, static (t, v) => t.CalloutFooterTemplate = v);
        }

        if (CalloutHeaderTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(CalloutHeaderTemplate), CalloutHeaderTemplate, static t => t.CalloutHeaderTemplate, static (t, v) => t.CalloutHeaderTemplate = v);
        }

        if (CalloutHtmlAttributes is not null)
        {
            foreach (var attr in CalloutHtmlAttributes)
            {
                if (bitTimePicker.CalloutHtmlAttributes.ContainsKey(attr.Key)) continue;

                bitTimePicker.CalloutHtmlAttributes[attr.Key] = attr.Value;
            }
        }

        if (Classes is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static t => t.ClearButtonIcon, static (t, v) => t.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static t => t.ClearButtonIconName, static (t, v) => t.ClearButtonIconName = v);
        }

        if (ClearButtonText.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(ClearButtonText), ClearButtonText!, static t => t.ClearButtonText, static (t, v) => t.ClearButtonText = v);
        }

        if (ClearButtonTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(ClearButtonTitle), ClearButtonTitle!, static t => t.ClearButtonTitle, static (t, v) => t.ClearButtonTitle = v);
        }

        if (CloseButtonIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(CloseButtonIcon), CloseButtonIcon, static t => t.CloseButtonIcon, static (t, v) => t.CloseButtonIcon = v);
        }

        if (CloseButtonIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(CloseButtonIconName), CloseButtonIconName, static t => t.CloseButtonIconName, static (t, v) => t.CloseButtonIconName = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle!, static t => t.CloseButtonTitle, static (t, v) => t.CloseButtonTitle = v);
        }

        if (Color.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (ContinuousSpinDelay.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ContinuousSpinDelay), ContinuousSpinDelay.Value, static t => t.ContinuousSpinDelay, static (t, v) => t.ContinuousSpinDelay = v);
        }

        if (ContinuousSpinInterval.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ContinuousSpinInterval), ContinuousSpinInterval.Value, static t => t.ContinuousSpinInterval, static (t, v) => t.ContinuousSpinInterval = v);
        }

        // The culture decides how the value is written, how a typed one is read, and which way the picker
        // lays itself out, and the component reads all three off it in a hook that has already run by the
        // time this cascade reaches it - so the hook is run once more below.
        var recomputeCulture = false;

        if (Culture is not null && bitTimePicker.TakeFromCascade(nameof(Culture), Culture, static t => t.Culture, static (t, v) => t.Culture = v))
        {
            recomputeCulture = true;
        }

        if (DecreaseHourIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseHourIcon), DecreaseHourIcon, static t => t.DecreaseHourIcon, static (t, v) => t.DecreaseHourIcon = v);
        }

        if (DecreaseHourIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseHourIconName), DecreaseHourIconName, static t => t.DecreaseHourIconName, static (t, v) => t.DecreaseHourIconName = v);
        }

        if (DecreaseHourTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseHourTitle), DecreaseHourTitle!, static t => t.DecreaseHourTitle, static (t, v) => t.DecreaseHourTitle = v);
        }

        if (DecreaseMinuteIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseMinuteIcon), DecreaseMinuteIcon, static t => t.DecreaseMinuteIcon, static (t, v) => t.DecreaseMinuteIcon = v);
        }

        if (DecreaseMinuteIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseMinuteIconName), DecreaseMinuteIconName, static t => t.DecreaseMinuteIconName, static (t, v) => t.DecreaseMinuteIconName = v);
        }

        if (DecreaseMinuteTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseMinuteTitle), DecreaseMinuteTitle!, static t => t.DecreaseMinuteTitle, static (t, v) => t.DecreaseMinuteTitle = v);
        }

        if (DecreaseSecondIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseSecondIcon), DecreaseSecondIcon, static t => t.DecreaseSecondIcon, static (t, v) => t.DecreaseSecondIcon = v);
        }

        if (DecreaseSecondIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseSecondIconName), DecreaseSecondIconName, static t => t.DecreaseSecondIconName, static (t, v) => t.DecreaseSecondIconName = v);
        }

        if (DecreaseSecondTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DecreaseSecondTitle), DecreaseSecondTitle!, static t => t.DecreaseSecondTitle, static (t, v) => t.DecreaseSecondTitle = v);
        }

        if (Description.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(Description), Description, static t => t.Description, static (t, v) => t.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static t => t.DescriptionTemplate, static (t, v) => t.DescriptionTemplate = v);
        }

        if (DisallowedTimeErrorMessage.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(DisallowedTimeErrorMessage), DisallowedTimeErrorMessage, static t => t.DisallowedTimeErrorMessage, static (t, v) => t.DisallowedTimeErrorMessage = v);
        }

        if (DisableFuture.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(DisableFuture), DisableFuture.Value, static t => t.DisableFuture, static (t, v) => t.DisableFuture = v);
        }

        if (DisablePast.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(DisablePast), DisablePast.Value, static t => t.DisablePast, static (t, v) => t.DisablePast = v);
        }

        if (DropDirection.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static t => t.DropDirection, static (t, v) => t.DropDirection = v);
        }

        if (HasBorder.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(HasBorder), HasBorder.Value, static t => t.HasBorder, static (t, v) => t.HasBorder = v);
        }

        if (HourInputAriaLabel.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(HourInputAriaLabel), HourInputAriaLabel!, static t => t.HourInputAriaLabel, static (t, v) => t.HourInputAriaLabel = v);
        }

        if (HourStep.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(HourStep), HourStep.Value, static t => t.HourStep, static (t, v) => t.HourStep = v);
        }

        if (Icon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(Icon), Icon, static t => t.Icon, static (t, v) => t.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IconName), IconName, static t => t.IconName, static (t, v) => t.IconName = v);
        }

        if (IconPlacement.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static t => t.IconPlacement, static (t, v) => t.IconPlacement = v);
        }

        if (IconTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(IconTemplate), IconTemplate, static t => t.IconTemplate, static (t, v) => t.IconTemplate = v);
        }

        if (IncreaseHourIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseHourIcon), IncreaseHourIcon, static t => t.IncreaseHourIcon, static (t, v) => t.IncreaseHourIcon = v);
        }

        if (IncreaseHourIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseHourIconName), IncreaseHourIconName, static t => t.IncreaseHourIconName, static (t, v) => t.IncreaseHourIconName = v);
        }

        if (IncreaseHourTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseHourTitle), IncreaseHourTitle!, static t => t.IncreaseHourTitle, static (t, v) => t.IncreaseHourTitle = v);
        }

        if (IncreaseMinuteIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseMinuteIcon), IncreaseMinuteIcon, static t => t.IncreaseMinuteIcon, static (t, v) => t.IncreaseMinuteIcon = v);
        }

        if (IncreaseMinuteIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseMinuteIconName), IncreaseMinuteIconName, static t => t.IncreaseMinuteIconName, static (t, v) => t.IncreaseMinuteIconName = v);
        }

        if (IncreaseMinuteTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseMinuteTitle), IncreaseMinuteTitle!, static t => t.IncreaseMinuteTitle, static (t, v) => t.IncreaseMinuteTitle = v);
        }

        if (IncreaseSecondIcon is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseSecondIcon), IncreaseSecondIcon, static t => t.IncreaseSecondIcon, static (t, v) => t.IncreaseSecondIcon = v);
        }

        if (IncreaseSecondIconName.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseSecondIconName), IncreaseSecondIconName, static t => t.IncreaseSecondIconName, static (t, v) => t.IncreaseSecondIconName = v);
        }

        if (IncreaseSecondTitle.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(IncreaseSecondTitle), IncreaseSecondTitle!, static t => t.IncreaseSecondTitle, static (t, v) => t.IncreaseSecondTitle = v);
        }

        if (InvalidErrorMessage.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(InvalidErrorMessage), InvalidErrorMessage, static t => t.InvalidErrorMessage, static (t, v) => t.InvalidErrorMessage = v);
        }

        if (InvertMouseWheel.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(InvertMouseWheel), InvertMouseWheel.Value, static t => t.InvertMouseWheel, static (t, v) => t.InvertMouseWheel = v);
        }

        if (Label.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(Label), Label, static t => t.Label, static (t, v) => t.Label = v);
        }

        if (LabelTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static t => t.LabelTemplate, static (t, v) => t.LabelTemplate = v);
        }

        if (MaxTime.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(MaxTime), MaxTime.Value, static t => t.MaxTime, static (t, v) => t.MaxTime = v);
        }

        if (MinTime.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(MinTime), MinTime.Value, static t => t.MinTime, static (t, v) => t.MinTime = v);
        }

        if (MinuteInputAriaLabel.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(MinuteInputAriaLabel), MinuteInputAriaLabel!, static t => t.MinuteInputAriaLabel, static (t, v) => t.MinuteInputAriaLabel = v);
        }

        if (MinuteStep.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(MinuteStep), MinuteStep.Value, static t => t.MinuteStep, static (t, v) => t.MinuteStep = v);
        }

        if (NoMouseWheel.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(NoMouseWheel), NoMouseWheel.Value, static t => t.NoMouseWheel, static (t, v) => t.NoMouseWheel = v);
        }

        if (Now.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(Now), Now.Value, static t => t.Now, static (t, v) => t.Now = v);
        }

        if (NowButtonText.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(NowButtonText), NowButtonText!, static t => t.NowButtonText, static (t, v) => t.NowButtonText = v);
        }

        if (OutOfRangeErrorMessage.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(OutOfRangeErrorMessage), OutOfRangeErrorMessage, static t => t.OutOfRangeErrorMessage, static (t, v) => t.OutOfRangeErrorMessage = v);
        }

        if (Placeholder.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(Placeholder), Placeholder, static t => t.Placeholder, static (t, v) => t.Placeholder = v);
        }

        if (Prefix.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(Prefix), Prefix, static t => t.Prefix, static (t, v) => t.Prefix = v);
        }

        if (PrefixTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(PrefixTemplate), PrefixTemplate, static t => t.PrefixTemplate, static (t, v) => t.PrefixTemplate = v);
        }

        if (Responsive.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(Responsive), Responsive.Value, static t => t.Responsive, static (t, v) => t.Responsive = v);
        }

        if (SecondInputAriaLabel.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(SecondInputAriaLabel), SecondInputAriaLabel!, static t => t.SecondInputAriaLabel, static (t, v) => t.SecondInputAriaLabel = v);
        }

        if (SecondStep.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(SecondStep), SecondStep.Value, static t => t.SecondStep, static (t, v) => t.SecondStep = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static t => t.ShowClearButton, static (t, v) => t.ShowClearButton = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static t => t.ShowCloseButton, static (t, v) => t.ShowCloseButton = v);
        }

        if (ShowInputClearButton.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ShowInputClearButton), ShowInputClearButton.Value, static t => t.ShowInputClearButton, static (t, v) => t.ShowInputClearButton = v);
        }

        if (ShowNowButton.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ShowNowButton), ShowNowButton.Value, static t => t.ShowNowButton, static (t, v) => t.ShowNowButton = v);
        }

        if (ShowSeconds.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(ShowSeconds), ShowSeconds.Value, static t => t.ShowSeconds, static (t, v) => t.ShowSeconds = v);
        }

        if (Size.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (Standalone.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(Standalone), Standalone.Value, static t => t.Standalone, static (t, v) => t.Standalone = v);
        }

        if (StartingValue.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(StartingValue), StartingValue.Value, static t => t.StartingValue, static (t, v) => t.StartingValue = v);
        }

        if (Styles is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Suffix.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(Suffix), Suffix, static t => t.Suffix, static (t, v) => t.Suffix = v);
        }

        if (SuffixTemplate is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(SuffixTemplate), SuffixTemplate, static t => t.SuffixTemplate, static (t, v) => t.SuffixTemplate = v);
        }

        if (TimeFormat.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(TimeFormat), TimeFormat.Value, static t => t.TimeFormat, static (t, v) => t.TimeFormat = v);
        }

        if (TimeZone is not null)
        {
            bitTimePicker.TakeFromCascade(nameof(TimeZone), TimeZone, static t => t.TimeZone, static (t, v) => t.TimeZone = v);
        }

        if (Underlined.HasValue)
        {
            bitTimePicker.TakeFromCascade(nameof(Underlined), Underlined.Value, static t => t.Underlined, static (t, v) => t.Underlined = v);
        }

        if (ValueFormat.HasValue())
        {
            bitTimePicker.TakeFromCascade(nameof(ValueFormat), ValueFormat, static t => t.ValueFormat, static (t, v) => t.ValueFormat = v);
        }

        if (recomputeCulture)
        {
            bitTimePicker.OnSetCulture();

            bitTimePicker.ClassBuilder.Reset();
        }
    }
}
