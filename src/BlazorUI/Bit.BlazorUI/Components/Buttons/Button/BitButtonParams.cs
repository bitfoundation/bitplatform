namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitButton"/> component.
/// </summary>
public class BitButtonParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitButton"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitButton value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitButton)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps the disabled button focusable and discoverable by screen readers, rendering <c>aria-disabled</c> instead of
    /// the native <c>disabled</c> attribute, so the button remains in the tab order while its action is suppressed.
    /// </summary>
    public bool? AllowDisabledFocus { get; set; }

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an <c>aria-hidden</c> attribute instructing screen readers to ignore the button.
    /// </summary>
    public bool? AriaHidden { get; set; }

    /// <summary>
    /// If true, the button automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// If true, enters the loading state automatically while awaiting the OnClick event and prevents subsequent clicks by default.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// The type of the button element; defaults to <c>submit</c> inside an <see cref="Microsoft.AspNetCore.Components.Forms.EditForm"/> otherwise <c>button</c>.
    /// </summary>
    public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the button.
    /// </summary>
    public BitButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The value of the download attribute of the link rendered by the button when the Href parameter is provided.
    /// Instructs the browser to download the linked resource instead of navigating to it.
    /// </summary>
    public string? Download { get; set; }

    /// <summary>
    /// Makes the Float/FloatAbsolute button draggable on the page, by pointer or with the arrow keys; ignored when neither is set.
    /// </summary>
    public bool? Draggable { get; set; }

    /// <summary>
    /// Preserves the foreground color of the button through hover and focus.
    /// </summary>
    public bool? FixedColor { get; set; }

    /// <summary>
    /// Enables floating behavior for the button, allowing it to be positioned relative to the viewport.
    /// </summary>
    public bool? Float { get; set; }

    /// <summary>
    /// Enables floating behavior for the button, allowing it to be positioned relative to its container.
    /// </summary>
    public bool? FloatAbsolute { get; set; }

    /// <summary>
    /// Specifies the offset of the floating button.
    /// </summary>
    public string? FloatOffset { get; set; }

    /// <summary>
    /// Specifies the position of the floating button.
    /// </summary>
    public BitPosition? FloatPosition { get; set; }

    /// <summary>
    /// The id of the form element that the button is associated with (rendered as the <c>form</c> attribute).
    /// Allows a submit/reset button to be placed outside of its form element.
    /// </summary>
    public string? FormId { get; set; }

    /// <summary>
    /// Expand the button width to 100% of the available width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The value of the href attribute of the link rendered by the button.
    /// If provided, the component will be rendered as an anchor tag instead of button.
    /// </summary>
    public string? Href { get; set; }

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
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Emoji</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered; the text is kept as screen-reader-only content so the
    /// button keeps its accessible name.
    /// </summary>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the component's content.
    /// </summary>
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// The url of the custom icon to render inside the button.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Determines whether the button is in loading mode or not.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the spinner appears after the button enters the loading state.
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
    /// Keeps each line of the button's text on a single line and ends it with an ellipsis where it does not fit.
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// Enables re-clicking while the button is in the loading state.
    /// </summary>
    public bool? Reclickable { get; set; }

    /// <summary>
    /// Sets the <c>rel</c> attribute for link-rendered buttons when the Href parameter is a non-anchor URL; ignored for
    /// empty or hash-only hrefs. The <c>rel</c> attribute specifies the relationship between the current document and
    /// the linked document.
    /// </summary>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Renders the button with fully rounded (pill shaped) corners, and an icon-only one as a circle.
    /// </summary>
    public bool? Rounded { get; set; }

    /// <summary>
    /// The text of the secondary section of the button.
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// Sets the preset size (Small, Medium, Large) for typography and padding of the button.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom inline styles for different parts of the button.
    /// </summary>
    public BitButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Specifies target attribute of the link when the button renders as an anchor (by providing the Href parameter).
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the button.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitButton"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitButton"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitButton"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitButton"/>.
    /// </remarks>
    /// <param name="bitButton">
    /// The <see cref="BitButton"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitButton bitButton)
    {
        if (bitButton is null) return;

        UpdateBaseParameters(bitButton);

        if (AllowDisabledFocus.HasValue && bitButton.HasNotBeenSet(nameof(AllowDisabledFocus)))
        {
            bitButton.AllowDisabledFocus = AllowDisabledFocus.Value;
        }

        if (AriaDescription.HasValue() && bitButton.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitButton.AriaDescription = AriaDescription;
        }

        if (AriaHidden.HasValue && bitButton.HasNotBeenSet(nameof(AriaHidden)))
        {
            bitButton.AriaHidden = AriaHidden.Value;
        }

        if (AutoFocus.HasValue && bitButton.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitButton.AutoFocus = AutoFocus.Value;
        }

        if (AutoLoading.HasValue && bitButton.HasNotBeenSet(nameof(AutoLoading)))
        {
            bitButton.AutoLoading = AutoLoading.Value;
        }

        if (ButtonType.HasValue && bitButton.HasNotBeenSet(nameof(ButtonType)))
        {
            bitButton.ButtonType = ButtonType.Value;
        }

        if (Classes is not null && bitButton.HasNotBeenSet(nameof(Classes)))
        {
            bitButton.Classes = Classes;

            bitButton.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitButton.HasNotBeenSet(nameof(Color)))
        {
            bitButton.Color = Color.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (Download.HasValue() && bitButton.HasNotBeenSet(nameof(Download)))
        {
            bitButton.Download = Download;
        }

        if (Draggable.HasValue && bitButton.HasNotBeenSet(nameof(Draggable)))
        {
            bitButton.Draggable = Draggable.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (FixedColor.HasValue && bitButton.HasNotBeenSet(nameof(FixedColor)))
        {
            bitButton.FixedColor = FixedColor.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (Float.HasValue && bitButton.HasNotBeenSet(nameof(Float)))
        {
            bitButton.Float = Float.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (FloatAbsolute.HasValue && bitButton.HasNotBeenSet(nameof(FloatAbsolute)))
        {
            bitButton.FloatAbsolute = FloatAbsolute.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (FloatOffset.HasValue() && bitButton.HasNotBeenSet(nameof(FloatOffset)))
        {
            bitButton.FloatOffset = FloatOffset;

            bitButton.StyleBuilder.Reset();
        }

        if (FloatPosition.HasValue && bitButton.HasNotBeenSet(nameof(FloatPosition)))
        {
            bitButton.FloatPosition = FloatPosition.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (FormId.HasValue() && bitButton.HasNotBeenSet(nameof(FormId)))
        {
            bitButton.FormId = FormId;
        }

        if (FullWidth.HasValue && bitButton.HasNotBeenSet(nameof(FullWidth)))
        {
            bitButton.FullWidth = FullWidth.Value;

            bitButton.ClassBuilder.Reset();
        }

        bool hrefWasSet = false;
        bool relWasSet = false;
        bool targetWasSet = false;

        if (Href.HasValue() && bitButton.HasNotBeenSet(nameof(Href)))
        {
            bitButton.Href = Href;

            hrefWasSet = true;
        }

        if (Icon is not null && bitButton.HasNotBeenSet(nameof(Icon)))
        {
            bitButton.Icon = Icon;
        }

        if (IconName.HasValue() && bitButton.HasNotBeenSet(nameof(IconName)))
        {
            bitButton.IconName = IconName;
        }

        if (IconOnly.HasValue && bitButton.HasNotBeenSet(nameof(IconOnly)))
        {
            bitButton.IconOnly = IconOnly.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (IconPosition.HasValue && bitButton.HasNotBeenSet(nameof(IconPosition)))
        {
            bitButton.IconPosition = IconPosition.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (IconUrl.HasValue() && bitButton.HasNotBeenSet(nameof(IconUrl)))
        {
            bitButton.IconUrl = IconUrl;
        }

        if (IsLoading.HasValue && bitButton.HasNotBeenSet(nameof(IsLoading)))
        {
            bitButton.IsLoading = IsLoading.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (LoadingDelay.HasValue && bitButton.HasNotBeenSet(nameof(LoadingDelay)))
        {
            bitButton.LoadingDelay = LoadingDelay.Value;
        }

        if (LoadingLabel.HasValue() && bitButton.HasNotBeenSet(nameof(LoadingLabel)))
        {
            bitButton.LoadingLabel = LoadingLabel;
        }

        if (LoadingLabelPosition.HasValue && bitButton.HasNotBeenSet(nameof(LoadingLabelPosition)))
        {
            bitButton.LoadingLabelPosition = LoadingLabelPosition.Value;
        }

        if (NoWrap.HasValue && bitButton.HasNotBeenSet(nameof(NoWrap)))
        {
            bitButton.NoWrap = NoWrap.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (Reclickable.HasValue && bitButton.HasNotBeenSet(nameof(Reclickable)))
        {
            bitButton.Reclickable = Reclickable.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (Rel.HasValue && bitButton.HasNotBeenSet(nameof(Rel)))
        {
            bitButton.Rel = Rel.Value;

            relWasSet = true;
        }

        if (Rounded.HasValue && bitButton.HasNotBeenSet(nameof(Rounded)))
        {
            bitButton.Rounded = Rounded.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (SecondaryText.HasValue() && bitButton.HasNotBeenSet(nameof(SecondaryText)))
        {
            bitButton.SecondaryText = SecondaryText;

            bitButton.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitButton.HasNotBeenSet(nameof(Size)))
        {
            bitButton.Size = Size.Value;

            bitButton.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitButton.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitButton.StopPropagation = StopPropagation.Value;
        }

        if (Styles is not null && bitButton.HasNotBeenSet(nameof(Styles)))
        {
            bitButton.Styles = Styles;

            bitButton.StyleBuilder.Reset();
        }

        if (Target.HasValue() && bitButton.HasNotBeenSet(nameof(Target)))
        {
            bitButton.Target = Target;

            targetWasSet = true;
        }

        // Call OnSetHrefRelAndTarget if any of Href, Rel or Target was set, to update the rel attribute value
        if (hrefWasSet || relWasSet || targetWasSet)
        {
            bitButton.OnSetHrefRelAndTarget();
        }

        if (Title.HasValue() && bitButton.HasNotBeenSet(nameof(Title)))
        {
            bitButton.Title = Title;
        }

        if (Variant.HasValue && bitButton.HasNotBeenSet(nameof(Variant)))
        {
            bitButton.Variant = Variant.Value;

            bitButton.ClassBuilder.Reset();
        }
    }
}
