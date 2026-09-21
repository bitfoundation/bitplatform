namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitToggleButton"/> component.
/// </summary>
public class BitToggleButtonParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitToggleButton"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitToggleButton value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitToggleButton)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps the disabled toggle button focusable and discoverable by screen readers, rendering <c>aria-disabled</c> instead of the
    /// native <c>disabled</c> attribute when <see cref="BitComponentBase.IsEnabled"/> is false, preserving a consistent tab order.
    /// Set it to false to render the native <c>disabled</c> attribute and remove the toggle button from the tab order.
    /// </summary>
    public bool? AllowDisabledFocus { get; set; }

    /// <summary>
    /// The id of the element that the toggle button controls (rendered into <c>aria-controls</c>).
    /// </summary>
    public string? AriaControls { get; set; }

    /// <summary>
    /// Detailed description of the toggle button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an <c>aria-hidden</c> attribute instructing screen readers to ignore the toggle button.
    /// </summary>
    public bool? AriaHidden { get; set; }

    /// <summary>
    /// The id of the element that labels the toggle button (rendered into <c>aria-labelledby</c>).
    /// </summary>
    public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Determines which ARIA state attribute the toggle button exposes to assistive technologies.
    /// </summary>
    public BitToggleButtonAriaMode? AriaMode { get; set; }

    /// <summary>
    /// If true, the toggle button automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// If true, enters the loading state automatically for as long as the OnClick, OnChanging and OnChange callbacks
    /// take, preventing subsequent clicks by default.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// Gets or sets the check mark icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckMarkIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CheckMarkIcon { get; set; }

    /// <summary>
    /// The name of the check mark icon that renders in the checked state when <see cref="ShowCheckMark"/> is enabled.
    /// </summary>
    public string? CheckMarkIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the toggle button.
    /// </summary>
    public BitToggleButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the toggle button.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the IsChecked parameter.
    /// </summary>
    /// <remarks>
    /// The checked state itself is deliberately not cascaded: it is the one parameter a toggle button assigns to
    /// itself on every click, so a cascaded value would be written back over the click on the next render. This one
    /// is only read while the toggle button initializes, which is what makes it the safe way to start a whole group
    /// of them in the checked state.
    /// </remarks>
    public bool? DefaultIsChecked { get; set; }

    /// <summary>
    /// Keeps the space of the check mark reserved in the unchecked state so the content does not shift while toggling.
    /// </summary>
    public bool? FixedCheckMark { get; set; }

    /// <summary>
    /// Preserves the foreground color of the toggle button through hover and press.
    /// </summary>
    public bool? FixedColor { get; set; }

    /// <summary>
    /// Expands the toggle button width to 100% of the available width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon name that renders inside the toggle button.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Microphone</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered and changes the styles accordingly.
    /// </summary>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the content of the toggle button.
    /// </summary>
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Determines whether the toggle button is in the loading state, which covers its content
    /// with a spinner and prevents subsequent clicks unless <see cref="Reclickable"/> is enabled.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the spinner appears after the toggle button enters the loading state.
    /// </summary>
    public int? LoadingDelay { get; set; }

    /// <summary>
    /// The loading label text to show next to the spinner icon.
    /// </summary>
    public string? LoadingLabel { get; set; }

    /// <summary>
    /// The position of the loading label in regards to the spinner icon.
    /// </summary>
    public BitLabelPosition? LoadingLabelPosition { get; set; }

    /// <summary>
    /// Keeps the text of the toggle button on a single line and ends it with an ellipsis where it does not fit.
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// The aria-label of the toggle button when it is not checked.
    /// </summary>
    public string? OffAriaLabel { get; set; }

    /// <summary>
    /// The color of the toggle button when it is not checked.
    /// </summary>
    public BitColor? OffColor { get; set; }

    /// <summary>
    /// Gets or sets the icon to display when the toggle button is not checked using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OffIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? OffIcon { get; set; }

    /// <summary>
    /// The icon of the toggle button when it is not checked.
    /// </summary>
    public string? OffIconName { get; set; }

    /// <summary>
    /// The text of the toggle button when it is not checked.
    /// </summary>
    public string? OffText { get; set; }

    /// <summary>
    /// The title of the toggle button when it is not checked.
    /// </summary>
    public string? OffTitle { get; set; }

    /// <summary>
    /// The visual variant of the toggle button when it is not checked.
    /// </summary>
    public BitVariant? OffVariant { get; set; }

    /// <summary>
    /// The aria-label of the toggle button when it is checked.
    /// </summary>
    public string? OnAriaLabel { get; set; }

    /// <summary>
    /// The color of the toggle button when it is checked.
    /// </summary>
    public BitColor? OnColor { get; set; }

    /// <summary>
    /// Gets or sets the icon to display when the toggle button is checked using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OnIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? OnIcon { get; set; }

    /// <summary>
    /// The icon of the toggle button when it is checked.
    /// </summary>
    public string? OnIconName { get; set; }

    /// <summary>
    /// The text of the toggle button when it is checked.
    /// </summary>
    public string? OnText { get; set; }

    /// <summary>
    /// The title of the toggle button when it is checked.
    /// </summary>
    public string? OnTitle { get; set; }

    /// <summary>
    /// The visual variant of the toggle button when it is checked.
    /// </summary>
    public BitVariant? OnVariant { get; set; }

    /// <summary>
    /// Enables re-clicking while the toggle button is in the loading state.
    /// </summary>
    public bool? Reclickable { get; set; }

    /// <summary>
    /// Renders a check mark in the checked state so the state is not conveyed by color alone.
    /// </summary>
    public bool? ShowCheckMark { get; set; }

    /// <summary>
    /// The size of the toggle button.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the toggle button.
    /// </summary>
    public BitToggleButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the toggle button.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the toggle button.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the toggle button.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitToggleButton"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitToggleButton"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitToggleButton"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitToggleButton"/>.
    /// </remarks>
    /// <param name="bitToggleButton">
    /// The <see cref="BitToggleButton"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitToggleButton bitToggleButton)
    {
        if (bitToggleButton is null) return;

        UpdateBaseParameters(bitToggleButton);

        if (AllowDisabledFocus.HasValue && bitToggleButton.HasNotBeenSet(nameof(AllowDisabledFocus)))
        {
            bitToggleButton.AllowDisabledFocus = AllowDisabledFocus.Value;
        }

        if (AriaControls.HasValue() && bitToggleButton.HasNotBeenSet(nameof(AriaControls)))
        {
            bitToggleButton.AriaControls = AriaControls;
        }

        if (AriaDescription.HasValue() && bitToggleButton.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitToggleButton.AriaDescription = AriaDescription;
        }

        if (AriaHidden.HasValue && bitToggleButton.HasNotBeenSet(nameof(AriaHidden)))
        {
            bitToggleButton.AriaHidden = AriaHidden.Value;
        }

        if (AriaLabelledBy.HasValue() && bitToggleButton.HasNotBeenSet(nameof(AriaLabelledBy)))
        {
            bitToggleButton.AriaLabelledBy = AriaLabelledBy;
        }

        if (AriaMode.HasValue && bitToggleButton.HasNotBeenSet(nameof(AriaMode)))
        {
            bitToggleButton.AriaMode = AriaMode.Value;
        }

        if (AutoFocus.HasValue && bitToggleButton.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitToggleButton.AutoFocus = AutoFocus.Value;
        }

        if (AutoLoading.HasValue && bitToggleButton.HasNotBeenSet(nameof(AutoLoading)))
        {
            bitToggleButton.AutoLoading = AutoLoading.Value;
        }

        if (CheckMarkIcon is not null && bitToggleButton.HasNotBeenSet(nameof(CheckMarkIcon)))
        {
            bitToggleButton.CheckMarkIcon = CheckMarkIcon;
        }

        if (CheckMarkIconName.HasValue() && bitToggleButton.HasNotBeenSet(nameof(CheckMarkIconName)))
        {
            bitToggleButton.CheckMarkIconName = CheckMarkIconName;
        }

        if (Classes is not null && bitToggleButton.HasNotBeenSet(nameof(Classes)))
        {
            bitToggleButton.Classes = Classes;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitToggleButton.HasNotBeenSet(nameof(Color)))
        {
            bitToggleButton.Color = Color.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (DefaultIsChecked.HasValue && bitToggleButton.HasNotBeenSet(nameof(DefaultIsChecked)))
        {
            bitToggleButton.DefaultIsChecked = DefaultIsChecked.Value;
        }

        if (FixedCheckMark.HasValue && bitToggleButton.HasNotBeenSet(nameof(FixedCheckMark)))
        {
            bitToggleButton.FixedCheckMark = FixedCheckMark.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (FixedColor.HasValue && bitToggleButton.HasNotBeenSet(nameof(FixedColor)))
        {
            bitToggleButton.FixedColor = FixedColor.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitToggleButton.HasNotBeenSet(nameof(FullWidth)))
        {
            bitToggleButton.FullWidth = FullWidth.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (Icon is not null && bitToggleButton.HasNotBeenSet(nameof(Icon)))
        {
            bitToggleButton.Icon = Icon;
        }

        if (IconName.HasValue() && bitToggleButton.HasNotBeenSet(nameof(IconName)))
        {
            bitToggleButton.IconName = IconName;
        }

        if (IconOnly.HasValue && bitToggleButton.HasNotBeenSet(nameof(IconOnly)))
        {
            bitToggleButton.IconOnly = IconOnly.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (IconPosition.HasValue && bitToggleButton.HasNotBeenSet(nameof(IconPosition)))
        {
            bitToggleButton.IconPosition = IconPosition.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (IsLoading.HasValue && bitToggleButton.HasNotBeenSet(nameof(IsLoading)))
        {
            bitToggleButton.IsLoading = IsLoading.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (LoadingDelay.HasValue && bitToggleButton.HasNotBeenSet(nameof(LoadingDelay)))
        {
            bitToggleButton.LoadingDelay = LoadingDelay.Value;
        }

        if (LoadingLabel.HasValue() && bitToggleButton.HasNotBeenSet(nameof(LoadingLabel)))
        {
            bitToggleButton.LoadingLabel = LoadingLabel;
        }

        if (LoadingLabelPosition.HasValue && bitToggleButton.HasNotBeenSet(nameof(LoadingLabelPosition)))
        {
            bitToggleButton.LoadingLabelPosition = LoadingLabelPosition.Value;
        }

        if (NoWrap.HasValue && bitToggleButton.HasNotBeenSet(nameof(NoWrap)))
        {
            bitToggleButton.NoWrap = NoWrap.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (OffAriaLabel.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OffAriaLabel)))
        {
            bitToggleButton.OffAriaLabel = OffAriaLabel;
        }

        if (OffColor.HasValue && bitToggleButton.HasNotBeenSet(nameof(OffColor)))
        {
            bitToggleButton.OffColor = OffColor.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (OffIcon is not null && bitToggleButton.HasNotBeenSet(nameof(OffIcon)))
        {
            bitToggleButton.OffIcon = OffIcon;
        }

        if (OffIconName.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OffIconName)))
        {
            bitToggleButton.OffIconName = OffIconName;
        }

        if (OffText.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OffText)))
        {
            bitToggleButton.OffText = OffText;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (OffTitle.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OffTitle)))
        {
            bitToggleButton.OffTitle = OffTitle;
        }

        if (OffVariant.HasValue && bitToggleButton.HasNotBeenSet(nameof(OffVariant)))
        {
            bitToggleButton.OffVariant = OffVariant.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (OnAriaLabel.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OnAriaLabel)))
        {
            bitToggleButton.OnAriaLabel = OnAriaLabel;
        }

        if (OnColor.HasValue && bitToggleButton.HasNotBeenSet(nameof(OnColor)))
        {
            bitToggleButton.OnColor = OnColor.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (OnIcon is not null && bitToggleButton.HasNotBeenSet(nameof(OnIcon)))
        {
            bitToggleButton.OnIcon = OnIcon;
        }

        if (OnIconName.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OnIconName)))
        {
            bitToggleButton.OnIconName = OnIconName;
        }

        if (OnText.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OnText)))
        {
            bitToggleButton.OnText = OnText;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (OnTitle.HasValue() && bitToggleButton.HasNotBeenSet(nameof(OnTitle)))
        {
            bitToggleButton.OnTitle = OnTitle;
        }

        if (OnVariant.HasValue && bitToggleButton.HasNotBeenSet(nameof(OnVariant)))
        {
            bitToggleButton.OnVariant = OnVariant.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (Reclickable.HasValue && bitToggleButton.HasNotBeenSet(nameof(Reclickable)))
        {
            bitToggleButton.Reclickable = Reclickable.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (ShowCheckMark.HasValue && bitToggleButton.HasNotBeenSet(nameof(ShowCheckMark)))
        {
            bitToggleButton.ShowCheckMark = ShowCheckMark.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitToggleButton.HasNotBeenSet(nameof(Size)))
        {
            bitToggleButton.Size = Size.Value;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitToggleButton.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitToggleButton.StopPropagation = StopPropagation.Value;
        }

        if (Styles is not null && bitToggleButton.HasNotBeenSet(nameof(Styles)))
        {
            bitToggleButton.Styles = Styles;

            bitToggleButton.StyleBuilder.Reset();
        }

        if (Text.HasValue() && bitToggleButton.HasNotBeenSet(nameof(Text)))
        {
            bitToggleButton.Text = Text;

            bitToggleButton.ClassBuilder.Reset();
        }

        if (Title.HasValue() && bitToggleButton.HasNotBeenSet(nameof(Title)))
        {
            bitToggleButton.Title = Title;
        }

        if (Variant.HasValue && bitToggleButton.HasNotBeenSet(nameof(Variant)))
        {
            bitToggleButton.Variant = Variant.Value;

            bitToggleButton.ClassBuilder.Reset();
        }
    }
}
