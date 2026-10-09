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
    public BitPlacement? IconPlacement { get; set; }

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
    public BitPlacement? LoadingLabelPlacement { get; set; }

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

        if (AllowDisabledFocus.HasValue)
        {
            bitButton.TakeFromCascade(nameof(AllowDisabledFocus), AllowDisabledFocus.Value, static b => b.AllowDisabledFocus, static (b, v) => b.AllowDisabledFocus = v);
        }

        if (AriaDescription.HasValue())
        {
            bitButton.TakeFromCascade(nameof(AriaDescription), AriaDescription, static b => b.AriaDescription, static (b, v) => b.AriaDescription = v);
        }

        if (AriaHidden.HasValue)
        {
            bitButton.TakeFromCascade(nameof(AriaHidden), AriaHidden.Value, static b => b.AriaHidden, static (b, v) => b.AriaHidden = v);
        }

        if (AutoFocus.HasValue)
        {
            bitButton.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static b => b.AutoFocus, static (b, v) => b.AutoFocus = v);
        }

        if (AutoLoading.HasValue)
        {
            bitButton.TakeFromCascade(nameof(AutoLoading), AutoLoading.Value, static b => b.AutoLoading, static (b, v) => b.AutoLoading = v);
        }

        if (ButtonType.HasValue)
        {
            bitButton.TakeFromCascade(nameof(ButtonType), ButtonType.Value, static b => b.ButtonType, static (b, v) => b.ButtonType = v);
        }

        if (Classes is not null)
        {
            bitButton.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Color.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Color), Color.Value, static b => b.Color, static (b, v) => b.Color = v);
        }

        if (Download.HasValue())
        {
            bitButton.TakeFromCascade(nameof(Download), Download, static b => b.Download, static (b, v) => b.Download = v);
        }

        if (Draggable.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Draggable), Draggable.Value, static b => b.Draggable, static (b, v) => b.Draggable = v);
        }

        if (FixedColor.HasValue)
        {
            bitButton.TakeFromCascade(nameof(FixedColor), FixedColor.Value, static b => b.FixedColor, static (b, v) => b.FixedColor = v);
        }

        if (Float.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Float), Float.Value, static b => b.Float, static (b, v) => b.Float = v);
        }

        if (FloatAbsolute.HasValue)
        {
            bitButton.TakeFromCascade(nameof(FloatAbsolute), FloatAbsolute.Value, static b => b.FloatAbsolute, static (b, v) => b.FloatAbsolute = v);
        }

        if (FloatOffset.HasValue())
        {
            bitButton.TakeFromCascade(nameof(FloatOffset), FloatOffset, static b => b.FloatOffset, static (b, v) => b.FloatOffset = v);
        }

        if (FloatPosition.HasValue)
        {
            bitButton.TakeFromCascade(nameof(FloatPosition), FloatPosition.Value, static b => b.FloatPosition, static (b, v) => b.FloatPosition = v);
        }

        if (FormId.HasValue())
        {
            bitButton.TakeFromCascade(nameof(FormId), FormId, static b => b.FormId, static (b, v) => b.FormId = v);
        }

        if (FullWidth.HasValue)
        {
            bitButton.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static b => b.FullWidth, static (b, v) => b.FullWidth = v);
        }

        if (Href.HasValue())
        {
            bitButton.TakeFromCascade(nameof(Href), Href, static b => b.Href, static (b, v) => b.Href = v);
        }

        // Icon, IconName and IconUrl are one setting - which icon is shown - and the first of them set wins, so
        // a component that picked its icon through any of them keeps it.
        var ownIcon = bitButton.HasSetAnyOf(nameof(Icon), nameof(IconName), nameof(IconUrl));

        if (Icon is not null)
        {
            bitButton.TakeFromCascade(nameof(Icon), Icon, static b => b.Icon, static (b, v) => b.Icon = v, outranked: ownIcon);
        }

        if (IconName.HasValue())
        {
            bitButton.TakeFromCascade(nameof(IconName), IconName, static b => b.IconName, static (b, v) => b.IconName = v, outranked: ownIcon);
        }

        if (IconOnly.HasValue)
        {
            bitButton.TakeFromCascade(nameof(IconOnly), IconOnly.Value, static b => b.IconOnly, static (b, v) => b.IconOnly = v);
        }

        if (IconPlacement.HasValue)
        {
            bitButton.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static b => b.IconPlacement, static (b, v) => b.IconPlacement = v);
        }

        if (IconUrl.HasValue())
        {
            bitButton.TakeFromCascade(nameof(IconUrl), IconUrl, static b => b.IconUrl, static (b, v) => b.IconUrl = v, outranked: ownIcon);
        }

        if (IsLoading.HasValue)
        {
            bitButton.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static b => b.IsLoading, static (b, v) => b.IsLoading = v);
        }

        if (LoadingDelay.HasValue)
        {
            bitButton.TakeFromCascade(nameof(LoadingDelay), LoadingDelay.Value, static b => b.LoadingDelay, static (b, v) => b.LoadingDelay = v);
        }

        if (LoadingLabel.HasValue())
        {
            bitButton.TakeFromCascade(nameof(LoadingLabel), LoadingLabel, static b => b.LoadingLabel, static (b, v) => b.LoadingLabel = v);
        }

        if (LoadingLabelPlacement.HasValue)
        {
            bitButton.TakeFromCascade(nameof(LoadingLabelPlacement), LoadingLabelPlacement.Value, static b => b.LoadingLabelPlacement, static (b, v) => b.LoadingLabelPlacement = v);
        }

        if (NoWrap.HasValue)
        {
            bitButton.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static b => b.NoWrap, static (b, v) => b.NoWrap = v);
        }

        if (Reclickable.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Reclickable), Reclickable.Value, static b => b.Reclickable, static (b, v) => b.Reclickable = v);
        }

        if (Rel.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Rel), Rel.Value, static b => b.Rel, static (b, v) => b.Rel = v);
        }

        if (Rounded.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Rounded), Rounded.Value, static b => b.Rounded, static (b, v) => b.Rounded = v);
        }

        if (SecondaryText.HasValue())
        {
            bitButton.TakeFromCascade(nameof(SecondaryText), SecondaryText, static b => b.SecondaryText, static (b, v) => b.SecondaryText = v);
        }

        if (Size.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Size), Size.Value, static b => b.Size, static (b, v) => b.Size = v);
        }

        if (StopPropagation.HasValue)
        {
            bitButton.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static b => b.StopPropagation, static (b, v) => b.StopPropagation = v);
        }

        if (Styles is not null)
        {
            bitButton.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }

        if (Target.HasValue())
        {
            bitButton.TakeFromCascade(nameof(Target), Target, static b => b.Target, static (b, v) => b.Target = v);
        }

        if (Title.HasValue())
        {
            bitButton.TakeFromCascade(nameof(Title), Title, static b => b.Title, static (b, v) => b.Title = v);
        }

        if (Variant.HasValue)
        {
            bitButton.TakeFromCascade(nameof(Variant), Variant.Value, static b => b.Variant, static (b, v) => b.Variant = v);
        }
    }
}
