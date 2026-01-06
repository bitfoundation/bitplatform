namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitActionButton"/> component.
/// </summary>
public class BitActionButtonParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitActionButton"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitActionButton value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitActionButton)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps the disabled action button focusable by not forcing a negative tabindex when <see cref="BitComponentBase.IsEnabled"/> is false.
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
    /// The type of the button element; defaults to <c>submit</c> inside an <see cref="Microsoft.AspNetCore.Components.Forms.EditForm"/> otherwise <c>button</c>.
    /// </summary>
    public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// Custom CSS classes for the root, icon, and content sections of the action button.
    /// </summary>
    public BitActionButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button that applies to the icon and text of the action button.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component should expand to occupy the full available width.
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
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.AddFriend</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery: 
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// The value is case-sensitive and must match a valid icon identifier. 
    /// If not set or set to <c>null</c>, no icon will be rendered.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the icon is displayed, without accompanying text.
    /// </summary>
    /// <remarks>
    /// Set this property to <see langword="true"/> to render the component with only its icon visible. 
    /// When <see langword="false"/>, both icon and text are shown if available.
    /// </remarks>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the component's content.
    /// </summary>
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Determines whether the action button is in loading mode or not.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// Gets or sets the relationship type between the current element and the linked resource, as defined by the link's rel attribute.
    /// </summary>
    /// <remarks>
    /// Sets the <c>rel</c> attribute for link-rendered buttons when <see cref="Href"/> is a non-anchor URL; ignored for empty or hash-only hrefs.
    /// The <c>rel</c> attribute specifies the relationship between the current document and the linked document.
    /// <br />
    /// Set this property to specify how the linked resource is related to the current context.
    /// Common values include "stylesheet", "noopener", or "nofollow". The value determines how browsers and search
    /// engines interpret the link.
    /// </remarks>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Sets the preset size (Small, Medium, Large) for typography and padding of the action button.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS inline styles to apply to the action button component.
    /// </summary>
    /// <remarks>
    /// Use this property to override the default styles of the action button.
    /// If not set, the component uses its built-in styling. 
    /// This property is typically used to provide additional visual customization.
    /// </remarks>
    public BitActionButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the name of the target frame or window for the navigation action when the action button renders as an anchor (by providing the Href parameter).
    /// </summary>
    /// <remarks>
    /// Specify a value to control where the linked content will be displayed. Common values include
    /// "_blank" to open in a new window or tab, "_self" for the same frame, "_parent" for the parent frame, and "_top"
    /// for the full body of the window. If not set, the default browser behavior is used.
    /// </remarks>
    public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Adds an underline to the action button text, useful for link-style buttons.
    /// </summary>
    public bool? Underlined { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitActionButton"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitActionButton"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitActionButton"/> will be updated. 
    /// This method does not overwrite existing values on <paramref name="bitActionButton"/>.
    /// </remarks>
    /// <param name="bitActionButton">
    /// The <see cref="BitActionButton"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitActionButton bitActionButton)
    {
        if (bitActionButton is null) return;

        UpdateBaseParameters(bitActionButton);

        if (AllowDisabledFocus.HasValue && bitActionButton.HasNotBeenSet(nameof(AllowDisabledFocus)))
        {
            bitActionButton.AllowDisabledFocus = AllowDisabledFocus.Value;
        }

        if (AriaDescription.HasValue() && bitActionButton.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitActionButton.AriaDescription = AriaDescription;
        }

        if (AriaHidden.HasValue && bitActionButton.HasNotBeenSet(nameof(AriaHidden)))
        {
            bitActionButton.AriaHidden = AriaHidden.Value;
        }

        if (ButtonType.HasValue && bitActionButton.HasNotBeenSet(nameof(ButtonType)))
        {
            bitActionButton.ButtonType = ButtonType.Value;
        }

        if (Classes is not null && bitActionButton.HasNotBeenSet(nameof(Classes)))
        {
            bitActionButton.Classes = Classes;

            bitActionButton.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitActionButton.HasNotBeenSet(nameof(Color)))
        {
            bitActionButton.Color = Color.Value;

            bitActionButton.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitActionButton.HasNotBeenSet(nameof(FullWidth)))
        {
            bitActionButton.FullWidth = FullWidth.Value;

            bitActionButton.ClassBuilder.Reset();
        }

        bool hrefWasSet = false;
        bool relWasSet = false;

        if (Href.HasValue() && bitActionButton.HasNotBeenSet(nameof(Href)))
        {
            bitActionButton.Href = Href;
            
            hrefWasSet = true;
        }

        if (Icon is not null && bitActionButton.HasNotBeenSet(nameof(Icon)))
        {
            bitActionButton.Icon = Icon;
        }

        if (IconName.HasValue() && bitActionButton.HasNotBeenSet(nameof(IconName)))
        {
            bitActionButton.IconName = IconName;
        }

        if (IconOnly.HasValue && bitActionButton.HasNotBeenSet(nameof(IconOnly)))
        {
            bitActionButton.IconOnly = IconOnly.Value;
        }

        if (IconPosition.HasValue && bitActionButton.HasNotBeenSet(nameof(IconPosition)))
        {
            bitActionButton.IconPosition = IconPosition.Value;

            bitActionButton.ClassBuilder.Reset();
        }

        if (IsLoading.HasValue && bitActionButton.HasNotBeenSet(nameof(IsLoading)))
        {
            bitActionButton.IsLoading = IsLoading.Value;

            bitActionButton.ClassBuilder.Reset();
        }

        if (Rel.HasValue && bitActionButton.HasNotBeenSet(nameof(Rel)))
        {
            bitActionButton.Rel = Rel.Value;

            relWasSet = true;
        }

        // Call OnSetHrefAndRel if either Href or Rel was set, to update the _rel field
        if (hrefWasSet || relWasSet)
        {
            bitActionButton.OnSetHrefAndRel();
        }

        if (Size.HasValue && bitActionButton.HasNotBeenSet(nameof(Size)))
        {
            bitActionButton.Size = Size.Value;

            bitActionButton.ClassBuilder.Reset();
        }

        if (Styles is not null && bitActionButton.HasNotBeenSet(nameof(Styles)))
        {
            bitActionButton.Styles = Styles;
        }

        if (Target.HasValue() && bitActionButton.HasNotBeenSet(nameof(Target)))
        {
            bitActionButton.Target = Target;
        }

        if (Title.HasValue() && bitActionButton.HasNotBeenSet(nameof(Title)))
        {
            bitActionButton.Title = Title;
        }

        if (Underlined.HasValue && bitActionButton.HasNotBeenSet(nameof(Underlined)))
        {
            bitActionButton.Underlined = Underlined.Value;

            bitActionButton.ClassBuilder.Reset();
        }
    }
}

