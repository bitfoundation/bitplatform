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
    /// native <c>disabled</c> attribute when <see cref="BitComponentBase.Disabled"/> is true, preserving a consistent tab order.
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
    public BitPlacement? IconPlacement { get; set; }

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
    public BitPlacement? LoadingLabelPlacement { get; set; }

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

        if (AllowDisabledFocus.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(AllowDisabledFocus), AllowDisabledFocus.Value, static t => t.AllowDisabledFocus, static (t, v) => t.AllowDisabledFocus = v);
        }

        if (AriaControls.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(AriaControls), AriaControls, static t => t.AriaControls, static (t, v) => t.AriaControls = v);
        }

        if (AriaDescription.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(AriaDescription), AriaDescription, static t => t.AriaDescription, static (t, v) => t.AriaDescription = v);
        }

        if (AriaHidden.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(AriaHidden), AriaHidden.Value, static t => t.AriaHidden, static (t, v) => t.AriaHidden = v);
        }

        if (AriaLabelledBy.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(AriaLabelledBy), AriaLabelledBy, static t => t.AriaLabelledBy, static (t, v) => t.AriaLabelledBy = v);
        }

        if (AriaMode.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(AriaMode), AriaMode.Value, static t => t.AriaMode, static (t, v) => t.AriaMode = v);
        }

        if (AutoFocus.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static t => t.AutoFocus, static (t, v) => t.AutoFocus = v);
        }

        if (AutoLoading.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(AutoLoading), AutoLoading.Value, static t => t.AutoLoading, static (t, v) => t.AutoLoading = v);
        }

        var ownCheckMarkIcon = bitToggleButton.HasSetAnyOf(nameof(CheckMarkIcon), nameof(CheckMarkIconName));

        if (CheckMarkIcon is not null)
        {
            bitToggleButton.TakeFromCascade(nameof(CheckMarkIcon), CheckMarkIcon, static t => t.CheckMarkIcon, static (t, v) => t.CheckMarkIcon = v, outranked: ownCheckMarkIcon);
        }

        if (CheckMarkIconName.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(CheckMarkIconName), CheckMarkIconName, static t => t.CheckMarkIconName, static (t, v) => t.CheckMarkIconName = v, outranked: ownCheckMarkIcon);
        }

        if (Classes is not null)
        {
            bitToggleButton.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Color.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (DefaultIsChecked.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(DefaultIsChecked), DefaultIsChecked.Value, static t => t.DefaultIsChecked, static (t, v) => t.DefaultIsChecked = v);
        }

        if (FixedCheckMark.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(FixedCheckMark), FixedCheckMark.Value, static t => t.FixedCheckMark, static (t, v) => t.FixedCheckMark = v);
        }

        if (FixedColor.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(FixedColor), FixedColor.Value, static t => t.FixedColor, static (t, v) => t.FixedColor = v);
        }

        if (FullWidth.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static t => t.FullWidth, static (t, v) => t.FullWidth = v);
        }

        var ownIcon = bitToggleButton.HasSetAnyOf(nameof(Icon), nameof(IconName));

        if (Icon is not null)
        {
            bitToggleButton.TakeFromCascade(nameof(Icon), Icon, static t => t.Icon, static (t, v) => t.Icon = v, outranked: ownIcon);
        }

        if (IconName.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(IconName), IconName, static t => t.IconName, static (t, v) => t.IconName = v, outranked: ownIcon);
        }

        if (IconOnly.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(IconOnly), IconOnly.Value, static t => t.IconOnly, static (t, v) => t.IconOnly = v);
        }

        if (IconPlacement.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static t => t.IconPlacement, static (t, v) => t.IconPlacement = v);
        }

        if (IsLoading.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static t => t.IsLoading, static (t, v) => t.IsLoading = v);
        }

        if (LoadingDelay.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(LoadingDelay), LoadingDelay.Value, static t => t.LoadingDelay, static (t, v) => t.LoadingDelay = v);
        }

        if (LoadingLabel.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(LoadingLabel), LoadingLabel, static t => t.LoadingLabel, static (t, v) => t.LoadingLabel = v);
        }

        if (LoadingLabelPlacement.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(LoadingLabelPlacement), LoadingLabelPlacement.Value, static t => t.LoadingLabelPlacement, static (t, v) => t.LoadingLabelPlacement = v);
        }

        if (NoWrap.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static t => t.NoWrap, static (t, v) => t.NoWrap = v);
        }

        if (OffAriaLabel.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OffAriaLabel), OffAriaLabel, static t => t.OffAriaLabel, static (t, v) => t.OffAriaLabel = v);
        }

        if (OffColor.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(OffColor), OffColor.Value, static t => t.OffColor, static (t, v) => t.OffColor = v);
        }

        var ownOffIcon = bitToggleButton.HasSetAnyOf(nameof(OffIcon), nameof(OffIconName));

        if (OffIcon is not null)
        {
            bitToggleButton.TakeFromCascade(nameof(OffIcon), OffIcon, static t => t.OffIcon, static (t, v) => t.OffIcon = v, outranked: ownOffIcon);
        }

        if (OffIconName.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OffIconName), OffIconName, static t => t.OffIconName, static (t, v) => t.OffIconName = v, outranked: ownOffIcon);
        }

        if (OffText.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OffText), OffText, static t => t.OffText, static (t, v) => t.OffText = v);
        }

        if (OffTitle.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OffTitle), OffTitle, static t => t.OffTitle, static (t, v) => t.OffTitle = v);
        }

        if (OffVariant.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(OffVariant), OffVariant.Value, static t => t.OffVariant, static (t, v) => t.OffVariant = v);
        }

        if (OnAriaLabel.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OnAriaLabel), OnAriaLabel, static t => t.OnAriaLabel, static (t, v) => t.OnAriaLabel = v);
        }

        if (OnColor.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(OnColor), OnColor.Value, static t => t.OnColor, static (t, v) => t.OnColor = v);
        }

        var ownOnIcon = bitToggleButton.HasSetAnyOf(nameof(OnIcon), nameof(OnIconName));

        if (OnIcon is not null)
        {
            bitToggleButton.TakeFromCascade(nameof(OnIcon), OnIcon, static t => t.OnIcon, static (t, v) => t.OnIcon = v, outranked: ownOnIcon);
        }

        if (OnIconName.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OnIconName), OnIconName, static t => t.OnIconName, static (t, v) => t.OnIconName = v, outranked: ownOnIcon);
        }

        if (OnText.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OnText), OnText, static t => t.OnText, static (t, v) => t.OnText = v);
        }

        if (OnTitle.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(OnTitle), OnTitle, static t => t.OnTitle, static (t, v) => t.OnTitle = v);
        }

        if (OnVariant.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(OnVariant), OnVariant.Value, static t => t.OnVariant, static (t, v) => t.OnVariant = v);
        }

        if (Reclickable.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(Reclickable), Reclickable.Value, static t => t.Reclickable, static (t, v) => t.Reclickable = v);
        }

        if (ShowCheckMark.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(ShowCheckMark), ShowCheckMark.Value, static t => t.ShowCheckMark, static (t, v) => t.ShowCheckMark = v);
        }

        if (Size.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (StopPropagation.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static t => t.StopPropagation, static (t, v) => t.StopPropagation = v);
        }

        if (Styles is not null)
        {
            bitToggleButton.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Text.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(Text), Text, static t => t.Text, static (t, v) => t.Text = v);
        }

        if (Title.HasValue())
        {
            bitToggleButton.TakeFromCascade(nameof(Title), Title, static t => t.Title, static (t, v) => t.Title = v);
        }

        if (Variant.HasValue)
        {
            bitToggleButton.TakeFromCascade(nameof(Variant), Variant.Value, static t => t.Variant, static (t, v) => t.Variant = v);
        }
    }
}
