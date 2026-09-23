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
    public BitIconLocation? IconLocation { get; set; }

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

        // Some of the parameters below carry a [CallOnSet] hook on the component, which has already run for
        // whatever the markup set before anything cascaded here reached it. Assigning through the cascade
        // bypasses the setter, so the hooks are re-run at the end - but only when the assignment actually
        // changed something. This method runs on every parameters-set, and re-running them unconditionally
        // would drag the dial back to its starting view on every re-render of the page around it.
        // The three flags are kept apart rather than rolled into one because the hooks behind them are not
        // the same: a change of the seconds only moves the dial off a ring the picker has stopped carrying,
        // where a change of the edit mode or the start view moves it back to where a picker begins.
        var cultureChanged = false;
        var viewChanged = false;
        var secondsChanged = false;

        if (AllowTextInput.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(AllowTextInput)))
        {
            bitCircularTimePicker.AllowTextInput = AllowTextInput.Value;
        }

        if (AllowedHours is not null && bitCircularTimePicker.HasNotBeenSet(nameof(AllowedHours)))
        {
            bitCircularTimePicker.AllowedHours = AllowedHours;
        }

        if (AllowedMinutes is not null && bitCircularTimePicker.HasNotBeenSet(nameof(AllowedMinutes)))
        {
            bitCircularTimePicker.AllowedMinutes = AllowedMinutes;
        }

        if (AllowedSeconds is not null && bitCircularTimePicker.HasNotBeenSet(nameof(AllowedSeconds)))
        {
            bitCircularTimePicker.AllowedSeconds = AllowedSeconds;
        }

        if (AmPmInClock.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(AmPmInClock)))
        {
            bitCircularTimePicker.AmPmInClock = AmPmInClock.Value;
        }

        if (AutoClose.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(AutoClose)))
        {
            bitCircularTimePicker.AutoClose = AutoClose.Value;
        }

        if (AutoCloseDelay.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(AutoCloseDelay)))
        {
            bitCircularTimePicker.AutoCloseDelay = AutoCloseDelay.Value;
        }

        if (AutoFocus.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitCircularTimePicker.AutoFocus = AutoFocus.Value;
        }

        if (CalloutAriaLabel.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(CalloutAriaLabel)))
        {
            bitCircularTimePicker.CalloutAriaLabel = CalloutAriaLabel!;
        }

        if (CalloutFooterTemplate is not null && bitCircularTimePicker.HasNotBeenSet(nameof(CalloutFooterTemplate)))
        {
            bitCircularTimePicker.CalloutFooterTemplate = CalloutFooterTemplate;
        }

        if (CalloutHeaderTemplate is not null && bitCircularTimePicker.HasNotBeenSet(nameof(CalloutHeaderTemplate)))
        {
            bitCircularTimePicker.CalloutHeaderTemplate = CalloutHeaderTemplate;
        }

        if (CalloutHtmlAttributes is not null)
        {
            foreach (var attr in CalloutHtmlAttributes)
            {
                if (bitCircularTimePicker.CalloutHtmlAttributes.ContainsKey(attr.Key)) continue;

                bitCircularTimePicker.CalloutHtmlAttributes[attr.Key] = attr.Value;
            }
        }

        if (Classes is not null && bitCircularTimePicker.HasNotBeenSet(nameof(Classes)))
        {
            bitCircularTimePicker.Classes = Classes;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (ClearButtonText.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(ClearButtonText)))
        {
            bitCircularTimePicker.ClearButtonText = ClearButtonText!;
        }

        if (CloseButtonIcon is not null && bitCircularTimePicker.HasNotBeenSet(nameof(CloseButtonIcon)))
        {
            bitCircularTimePicker.CloseButtonIcon = CloseButtonIcon;
        }

        if (CloseButtonIconName.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(CloseButtonIconName)))
        {
            bitCircularTimePicker.CloseButtonIconName = CloseButtonIconName;
        }

        if (CloseButtonTitle.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitCircularTimePicker.CloseButtonTitle = CloseButtonTitle!;
        }

        if (Color.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(Color)))
        {
            bitCircularTimePicker.Color = Color.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (Culture is not null && bitCircularTimePicker.HasNotBeenSet(nameof(Culture)))
        {
            cultureChanged = ReferenceEquals(bitCircularTimePicker.Culture, Culture) is false;

            bitCircularTimePicker.Culture = Culture;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (DisallowedTimeErrorMessage.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(DisallowedTimeErrorMessage)))
        {
            bitCircularTimePicker.DisallowedTimeErrorMessage = DisallowedTimeErrorMessage;
        }

        if (DisableFuture.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(DisableFuture)))
        {
            bitCircularTimePicker.DisableFuture = DisableFuture.Value;
        }

        if (DisablePast.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(DisablePast)))
        {
            bitCircularTimePicker.DisablePast = DisablePast.Value;
        }

        if (DropDirection.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(DropDirection)))
        {
            bitCircularTimePicker.DropDirection = DropDirection.Value;
        }

        if (EditMode.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(EditMode)))
        {
            viewChanged = viewChanged || bitCircularTimePicker.EditMode != EditMode.Value;

            bitCircularTimePicker.EditMode = EditMode.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (HasBorder.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(HasBorder)))
        {
            bitCircularTimePicker.HasBorder = HasBorder.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (HourButtonTitle.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(HourButtonTitle)))
        {
            bitCircularTimePicker.HourButtonTitle = HourButtonTitle!;
        }

        if (HourStep.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(HourStep)))
        {
            bitCircularTimePicker.HourStep = HourStep.Value;
        }

        if (Icon is not null && bitCircularTimePicker.HasNotBeenSet(nameof(Icon)))
        {
            bitCircularTimePicker.Icon = Icon;
        }

        if (IconLocation.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(IconLocation)))
        {
            bitCircularTimePicker.IconLocation = IconLocation.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (IconName.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(IconName)))
        {
            bitCircularTimePicker.IconName = IconName;
        }

        if (IconTemplate is not null && bitCircularTimePicker.HasNotBeenSet(nameof(IconTemplate)))
        {
            bitCircularTimePicker.IconTemplate = IconTemplate;
        }

        if (InvalidErrorMessage.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(InvalidErrorMessage)))
        {
            bitCircularTimePicker.InvalidErrorMessage = InvalidErrorMessage;
        }

        if (InvertMouseWheel.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(InvertMouseWheel)))
        {
            bitCircularTimePicker.InvertMouseWheel = InvertMouseWheel.Value;
        }

        if (Landscape.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(Landscape)))
        {
            bitCircularTimePicker.Landscape = Landscape.Value;
        }

        if (MaxTime.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(MaxTime)))
        {
            bitCircularTimePicker.MaxTime = MaxTime.Value;
        }

        if (MinTime.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(MinTime)))
        {
            bitCircularTimePicker.MinTime = MinTime.Value;
        }

        if (MinuteButtonTitle.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(MinuteButtonTitle)))
        {
            bitCircularTimePicker.MinuteButtonTitle = MinuteButtonTitle!;
        }

        if (MinuteStep.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(MinuteStep)))
        {
            bitCircularTimePicker.MinuteStep = MinuteStep.Value;
        }

        if (NoMouseWheel.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(NoMouseWheel)))
        {
            bitCircularTimePicker.NoMouseWheel = NoMouseWheel.Value;
        }

        if (NowButtonText.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(NowButtonText)))
        {
            bitCircularTimePicker.NowButtonText = NowButtonText!;
        }

        if (OutOfRangeErrorMessage.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(OutOfRangeErrorMessage)))
        {
            bitCircularTimePicker.OutOfRangeErrorMessage = OutOfRangeErrorMessage;
        }

        if (Placeholder.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(Placeholder)))
        {
            bitCircularTimePicker.Placeholder = Placeholder;
        }

        if (Responsive.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(Responsive)))
        {
            bitCircularTimePicker.Responsive = Responsive.Value;
        }

        if (SecondButtonTitle.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(SecondButtonTitle)))
        {
            bitCircularTimePicker.SecondButtonTitle = SecondButtonTitle!;
        }

        if (SecondStep.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(SecondStep)))
        {
            bitCircularTimePicker.SecondStep = SecondStep.Value;
        }

        if (ShowClearButton.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitCircularTimePicker.ShowClearButton = ShowClearButton.Value;
        }

        if (ShowCloseButton.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitCircularTimePicker.ShowCloseButton = ShowCloseButton.Value;
        }

        if (ShowNowButton.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(ShowNowButton)))
        {
            bitCircularTimePicker.ShowNowButton = ShowNowButton.Value;
        }

        if (ShowSeconds.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(ShowSeconds)))
        {
            secondsChanged = secondsChanged || bitCircularTimePicker.ShowSeconds != ShowSeconds.Value;

            bitCircularTimePicker.ShowSeconds = ShowSeconds.Value;
        }

        if (Size.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(Size)))
        {
            bitCircularTimePicker.Size = Size.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (Standalone.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(Standalone)))
        {
            bitCircularTimePicker.Standalone = Standalone.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (StartingValue.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(StartingValue)))
        {
            bitCircularTimePicker.StartingValue = StartingValue.Value;
        }

        if (StartView.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(StartView)))
        {
            viewChanged = viewChanged || bitCircularTimePicker.StartView != StartView.Value;

            bitCircularTimePicker.StartView = StartView.Value;
        }

        if (Styles is not null && bitCircularTimePicker.HasNotBeenSet(nameof(Styles)))
        {
            bitCircularTimePicker.Styles = Styles;

            bitCircularTimePicker.StyleBuilder.Reset();
        }

        if (TimeFormat.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(TimeFormat)))
        {
            bitCircularTimePicker.TimeFormat = TimeFormat.Value;
        }

        if (Underlined.HasValue && bitCircularTimePicker.HasNotBeenSet(nameof(Underlined)))
        {
            bitCircularTimePicker.Underlined = Underlined.Value;

            bitCircularTimePicker.ClassBuilder.Reset();
        }

        if (ValueFormat.HasValue() && bitCircularTimePicker.HasNotBeenSet(nameof(ValueFormat)))
        {
            bitCircularTimePicker.ValueFormat = ValueFormat;
        }

        bitCircularTimePicker.ApplyCascadedParameters(cultureChanged, viewChanged, secondsChanged);
    }
}
