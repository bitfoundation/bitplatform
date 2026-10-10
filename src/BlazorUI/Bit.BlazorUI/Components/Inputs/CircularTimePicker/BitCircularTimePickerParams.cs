using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCircularTimePicker"/> component.
/// </summary>
public class BitCircularTimePickerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCircularTimePicker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCircularTimePicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCircularTimePicker)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether the TimePicker allows input a time string directly or not.
    /// </summary>
    public bool? AllowTextInput { get; set; }

    /// <summary>
    /// The hours that can be selected, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// <see cref="HourStep"/> already allow.
    /// </summary>
    public Func<int, bool>? AllowedHours { get; set; }

    /// <summary>
    /// The minutes that can be selected, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// <see cref="MinuteStep"/> already allow.
    /// </summary>
    public Func<int, bool>? AllowedMinutes { get; set; }

    /// <summary>
    /// The seconds that can be selected, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// <see cref="SecondStep"/> already allow.
    /// </summary>
    public Func<int, bool>? AllowedSeconds { get; set; }

    /// <summary>
    /// Renders the AM/PM pair under the clock instead of beside the time in the toolbar.
    /// </summary>
    public bool? AmPmInClock { get; set; }

    /// <summary>
    /// Closes the callout as soon as the selection is complete, without waiting for the close button or a
    /// click outside of it.
    /// </summary>
    public bool? AutoClose { get; set; }

    /// <summary>
    /// How long, in milliseconds, an <see cref="AutoClose"/> picker waits before it closes.
    /// </summary>
    public int? AutoCloseDelay { get; set; }

    /// <summary>
    /// If true, the input of the TimePicker automatically receives focus when the page renders.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Aria label for time picker popup for screen reader users.
    /// </summary>
    public string? CalloutAriaLabel { get; set; }

    /// <summary>
    /// Custom template to render at the bottom of the TimePicker's callout, below everything it holds.
    /// </summary>
    public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render at the top of the TimePicker's callout, above everything it holds.
    /// </summary>
    public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the main callout's parameters.
    /// </summary>
    public Dictionary<string, object>? CalloutHtmlAttributes { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the TimePicker component.
    /// </summary>
    public BitCircularTimePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The text of the button that clears the value of the TimePicker.
    /// </summary>
    public string? ClearButtonText { get; set; }

    /// <summary>
    /// The icon to display on the close button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseButtonIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CloseButtonIcon { get; set; }

    /// <summary>
    /// The name of the icon to display on the close button from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseButtonIconName { get; set; }

    /// <summary>
    /// The title of the close button (tooltip).
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The general color of the TimePicker, applied to the toolbar, the dial pointer and the selected numbers.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// CultureInfo for the TimePicker.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The custom validation error message for a time entered as text that
    /// <see cref="AllowedHours"/>, <see cref="AllowedMinutes"/> or <see cref="AllowedSeconds"/> rejects.
    /// </summary>
    public string? DisallowedTimeErrorMessage { get; set; }

    /// <summary>
    /// Disables every time of day after the current time, exactly as a <see cref="MaxTime"/> of now would.
    /// </summary>
    public bool? DisableFuture { get; set; }

    /// <summary>
    /// Disables every time of day before the current time, exactly as a <see cref="MinTime"/> of now would.
    /// </summary>
    public bool? DisablePast { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// Choose the edition mode. By default, you can edit every part the picker shows.
    /// </summary>
    public BitCircularTimePickerEditMode? EditMode { get; set; }

    /// <summary>
    /// Determines if the TimePicker has a border.
    /// </summary>
    public bool? HasBorder { get; set; }

    /// <summary>
    /// The title (and accessible name) of the button that switches the dial to the hours.
    /// </summary>
    public string? HourButtonTitle { get; set; }

    /// <summary>
    /// The step, in hours, the dial and the keyboard move the hour by.
    /// </summary>
    public int? HourStep { get; set; }

    /// <summary>
    /// The icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// TimePicker icon location.
    /// </summary>
    public BitPlacement? IconPlacement { get; set; }

    /// <summary>
    /// The name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Custom TimePicker icon template.
    /// </summary>
    public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Reverses the direction the mouse wheel moves the dial in.
    /// </summary>
    public bool? InvertMouseWheel { get; set; }

    /// <summary>
    /// Lays the clock out beside its toolbar instead of under it, for a picker that has more width than
    /// height to work with.
    /// </summary>
    public bool? Landscape { get; set; }

    /// <summary>
    /// The latest time that can be selected.
    /// </summary>
    public TimeSpan? MaxTime { get; set; }

    /// <summary>
    /// The earliest time that can be selected.
    /// </summary>
    public TimeSpan? MinTime { get; set; }

    /// <summary>
    /// The title (and accessible name) of the button that switches the dial to the minutes.
    /// </summary>
    public string? MinuteButtonTitle { get; set; }

    /// <summary>
    /// The step, in minutes, the dial and the keyboard move the minute by.
    /// </summary>
    public int? MinuteStep { get; set; }

    /// <summary>
    /// Disables moving the dial with the mouse wheel entirely.
    /// </summary>
    public bool? NoMouseWheel { get; set; }

    /// <summary>
    /// The text of the button that sets the TimePicker to the current time.
    /// </summary>
    public string? NowButtonText { get; set; }

    /// <summary>
    /// The custom validation error message for a time entered as text that falls outside of
    /// <see cref="MinTime"/> and <see cref="MaxTime"/>.
    /// </summary>
    public string? OutOfRangeErrorMessage { get; set; }

    /// <summary>
    /// Placeholder text for the TimePicker.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The title (and accessible name) of the button that switches the dial to the seconds.
    /// </summary>
    public string? SecondButtonTitle { get; set; }

    /// <summary>
    /// The step, in seconds, the dial and the keyboard move the second by.
    /// </summary>
    public int? SecondStep { get; set; }

    /// <summary>
    /// Renders a button that clears the value of the TimePicker under the clock.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Whether the TimePicker's close button should be shown or not.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Renders a button that sets the TimePicker to the current time under the clock.
    /// </summary>
    public bool? ShowNowButton { get; set; }

    /// <summary>
    /// Adds the seconds to the picker: a third ring the dial moves on to after the minute, a third part in the
    /// toolbar, and the seconds of the value kept instead of zeroed.
    /// </summary>
    public bool? ShowSeconds { get; set; }

    /// <summary>
    /// The size of the TimePicker.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Whether the TimePicker is rendered standalone or with the input component and callout.
    /// </summary>
    public bool? Standalone { get; set; }

    /// <summary>
    /// The time an empty TimePicker starts from, instead of midnight.
    /// </summary>
    public TimeSpan? StartingValue { get; set; }

    /// <summary>
    /// The part of the time the clock starts on when the picker opens.
    /// </summary>
    public BitCircularTimePickerView? StartView { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the TimePicker component.
    /// </summary>
    public BitCircularTimePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    public BitTimeFormat? TimeFormat { get; set; }

    /// <summary>
    /// Whether or not the Text field of the TimePicker is underlined.
    /// </summary>
    public bool? Underlined { get; set; }

    /// <summary>
    /// The format of the time in the TimePicker.
    /// </summary>
    public string? ValueFormat { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCircularTimePicker"/> instance with any values that have
    /// been set on this object, if those properties have not already been set on the <see cref="BitCircularTimePicker"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCircularTimePicker"/>
    /// will be updated. This method does not overwrite existing values on <paramref name="bitCircularTimePicker"/>.
    /// </remarks>
    /// <param name="bitCircularTimePicker">
    /// The <see cref="BitCircularTimePicker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCircularTimePicker bitCircularTimePicker)
    {
        if (bitCircularTimePicker is null) return;

        UpdateBaseParameters(bitCircularTimePicker);

        // Whether any of the parameters the dial starts out on is filled in, which the first pass reads.
        var viewSourceChanged = false;

        if (AllowTextInput.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AllowTextInput), AllowTextInput.Value, static c => c.AllowTextInput, static (c, v) => c.AllowTextInput = v);
        }

        if (AllowedHours is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AllowedHours), AllowedHours, static c => c.AllowedHours, static (c, v) => c.AllowedHours = v);
        }

        if (AllowedMinutes is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AllowedMinutes), AllowedMinutes, static c => c.AllowedMinutes, static (c, v) => c.AllowedMinutes = v);
        }

        if (AllowedSeconds is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AllowedSeconds), AllowedSeconds, static c => c.AllowedSeconds, static (c, v) => c.AllowedSeconds = v);
        }

        if (AmPmInClock.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AmPmInClock), AmPmInClock.Value, static c => c.AmPmInClock, static (c, v) => c.AmPmInClock = v);
        }

        if (AutoClose.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AutoClose), AutoClose.Value, static c => c.AutoClose, static (c, v) => c.AutoClose = v);
        }

        if (AutoCloseDelay.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AutoCloseDelay), AutoCloseDelay.Value, static c => c.AutoCloseDelay, static (c, v) => c.AutoCloseDelay = v);
        }

        if (AutoFocus.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static c => c.AutoFocus, static (c, v) => c.AutoFocus = v);
        }

        if (CalloutAriaLabel.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(CalloutAriaLabel), CalloutAriaLabel!, static c => c.CalloutAriaLabel, static (c, v) => c.CalloutAriaLabel = v);
        }

        if (CalloutFooterTemplate is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(CalloutFooterTemplate), CalloutFooterTemplate, static c => c.CalloutFooterTemplate, static (c, v) => c.CalloutFooterTemplate = v);
        }

        if (CalloutHeaderTemplate is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(CalloutHeaderTemplate), CalloutHeaderTemplate, static c => c.CalloutHeaderTemplate, static (c, v) => c.CalloutHeaderTemplate = v);
        }

        bitCircularTimePicker.CascadedCalloutHtmlAttributes = CalloutHtmlAttributes;

        if (Classes is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (ClearButtonText.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(ClearButtonText), ClearButtonText!, static c => c.ClearButtonText, static (c, v) => c.ClearButtonText = v);
        }


        if (CloseButtonIcon is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(CloseButtonIcon), CloseButtonIcon, static c => c.CloseButtonIcon, static (c, v) => c.CloseButtonIcon = v);
        }

        if (CloseButtonIconName.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(CloseButtonIconName), CloseButtonIconName, static c => c.CloseButtonIconName, static (c, v) => c.CloseButtonIconName = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle!, static c => c.CloseButtonTitle, static (c, v) => c.CloseButtonTitle = v);
        }

        if (Color.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Color), Color.Value, static c => c.Color, static (c, v) => c.Color = v);
        }

        if (Culture is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Culture), Culture, static c => c.Culture, static (c, v) => c.Culture = v);
        }

        if (DisallowedTimeErrorMessage.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(DisallowedTimeErrorMessage), DisallowedTimeErrorMessage, static c => c.DisallowedTimeErrorMessage, static (c, v) => c.DisallowedTimeErrorMessage = v);
        }

        if (DisableFuture.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(DisableFuture), DisableFuture.Value, static c => c.DisableFuture, static (c, v) => c.DisableFuture = v);
        }

        if (DisablePast.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(DisablePast), DisablePast.Value, static c => c.DisablePast, static (c, v) => c.DisablePast = v);
        }

        if (DropDirection.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static c => c.DropDirection, static (c, v) => c.DropDirection = v);
        }

        if (EditMode.HasValue && bitCircularTimePicker.TakeFromCascade(nameof(EditMode), EditMode.Value, static c => c.EditMode, static (c, v) => c.EditMode = v))
        {
            viewSourceChanged = true;
        }

        if (HasBorder.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(HasBorder), HasBorder.Value, static c => c.HasBorder, static (c, v) => c.HasBorder = v);
        }

        if (HourButtonTitle.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(HourButtonTitle), HourButtonTitle!, static c => c.HourButtonTitle, static (c, v) => c.HourButtonTitle = v);
        }

        if (HourStep.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(HourStep), HourStep.Value, static c => c.HourStep, static (c, v) => c.HourStep = v);
        }


        if (Icon is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Icon), Icon, static c => c.Icon, static (c, v) => c.Icon = v);
        }

        if (IconPlacement.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static c => c.IconPlacement, static (c, v) => c.IconPlacement = v);
        }

        if (IconName.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(IconName), IconName, static c => c.IconName, static (c, v) => c.IconName = v);
        }

        if (IconTemplate is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(IconTemplate), IconTemplate, static c => c.IconTemplate, static (c, v) => c.IconTemplate = v);
        }

        if (InvalidErrorMessage.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(InvalidErrorMessage), InvalidErrorMessage, static c => c.InvalidErrorMessage, static (c, v) => c.InvalidErrorMessage = v);
        }

        if (InvertMouseWheel.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(InvertMouseWheel), InvertMouseWheel.Value, static c => c.InvertMouseWheel, static (c, v) => c.InvertMouseWheel = v);
        }

        if (Landscape.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Landscape), Landscape.Value, static c => c.Landscape, static (c, v) => c.Landscape = v);
        }

        if (MaxTime.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(MaxTime), MaxTime.Value, static c => c.MaxTime, static (c, v) => c.MaxTime = v);
        }

        if (MinTime.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(MinTime), MinTime.Value, static c => c.MinTime, static (c, v) => c.MinTime = v);
        }

        if (MinuteButtonTitle.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(MinuteButtonTitle), MinuteButtonTitle!, static c => c.MinuteButtonTitle, static (c, v) => c.MinuteButtonTitle = v);
        }

        if (MinuteStep.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(MinuteStep), MinuteStep.Value, static c => c.MinuteStep, static (c, v) => c.MinuteStep = v);
        }

        if (NoMouseWheel.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(NoMouseWheel), NoMouseWheel.Value, static c => c.NoMouseWheel, static (c, v) => c.NoMouseWheel = v);
        }

        if (NowButtonText.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(NowButtonText), NowButtonText!, static c => c.NowButtonText, static (c, v) => c.NowButtonText = v);
        }

        if (OutOfRangeErrorMessage.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(OutOfRangeErrorMessage), OutOfRangeErrorMessage, static c => c.OutOfRangeErrorMessage, static (c, v) => c.OutOfRangeErrorMessage = v);
        }

        if (Placeholder.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Placeholder), Placeholder, static c => c.Placeholder, static (c, v) => c.Placeholder = v);
        }

        if (Responsive.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Responsive), Responsive.Value, static c => c.Responsive, static (c, v) => c.Responsive = v);
        }

        if (SecondButtonTitle.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(SecondButtonTitle), SecondButtonTitle!, static c => c.SecondButtonTitle, static (c, v) => c.SecondButtonTitle = v);
        }

        if (SecondStep.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(SecondStep), SecondStep.Value, static c => c.SecondStep, static (c, v) => c.SecondStep = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static c => c.ShowClearButton, static (c, v) => c.ShowClearButton = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static c => c.ShowCloseButton, static (c, v) => c.ShowCloseButton = v);
        }

        if (ShowNowButton.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(ShowNowButton), ShowNowButton.Value, static c => c.ShowNowButton, static (c, v) => c.ShowNowButton = v);
        }

        if (ShowSeconds.HasValue && bitCircularTimePicker.TakeFromCascade(nameof(ShowSeconds), ShowSeconds.Value, static c => c.ShowSeconds, static (c, v) => c.ShowSeconds = v))
        {
            viewSourceChanged = true;
        }

        if (Size.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (Standalone.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Standalone), Standalone.Value, static c => c.Standalone, static (c, v) => c.Standalone = v);
        }

        if (StartingValue.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(StartingValue), StartingValue.Value, static c => c.StartingValue, static (c, v) => c.StartingValue = v);
        }

        if (StartView.HasValue && bitCircularTimePicker.TakeFromCascade(nameof(StartView), StartView.Value, static c => c.StartView, static (c, v) => c.StartView = v))
        {
            viewSourceChanged = true;
        }

        if (Styles is not null)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (TimeFormat.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(TimeFormat), TimeFormat.Value, static c => c.TimeFormat, static (c, v) => c.TimeFormat = v);
        }

        if (Underlined.HasValue)
        {
            bitCircularTimePicker.TakeFromCascade(nameof(Underlined), Underlined.Value, static c => c.Underlined, static (c, v) => c.Underlined = v);
        }

        if (ValueFormat.HasValue())
        {
            bitCircularTimePicker.TakeFromCascade(nameof(ValueFormat), ValueFormat, static c => c.ValueFormat, static (c, v) => c.ValueFormat = v);
        }

        bitCircularTimePicker.ApplyCascadedView(viewSourceChanged);
    }
}
