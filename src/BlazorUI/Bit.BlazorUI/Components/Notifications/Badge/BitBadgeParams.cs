namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitBadge"/> component.
/// </summary>
public class BitBadgeParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitBadge"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitBadge value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitBadge)}";



    public string Name => ParamName;



    /// <summary>
    /// Draws a ring around the badge in the color of the page behind it, so it stays legible over a busy child.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the badge.
    /// </summary>
    public BitBadgeClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the badge.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Content you want inside the badge. A number is capped by <see cref="Max"/> and hidden by <see cref="ShowZero"/> when it is zero.
    /// </summary>
    public object? Content { get; set; }

    /// <summary>
    /// The custom template to render inside the badge, in place of <see cref="Content"/>.
    /// </summary>
    public RenderFragment? ContentTemplate { get; set; }

    /// <summary>
    /// The text alternative of the badge for assistive technologies, for example "5 unread messages".
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reduces the size of the badge and hides any of its content.
    /// </summary>
    public bool? Dot { get; set; }

    /// <summary>
    /// Removes the badge from the DOM while its child content keeps rendering.
    /// </summary>
    public bool? Hidden { get; set; }

    /// <summary>
    /// The URL the badge navigates to, which also turns the badge into a link.
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
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Lays the badge out next to its child content in the normal flow of the page instead of over it.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// Announces the badge to assistive technologies whenever its content changes, through a polite live region.
    /// </summary>
    public bool? Live { get; set; }

    /// <summary>
    /// Max value to display when content is a number; a content above it renders as the max followed by a plus sign.
    /// </summary>
    public int? Max { get; set; }

    /// <summary>
    /// Moves the badge along the horizontal axis by the given CSS length, on top of its <see cref="Position"/>.
    /// </summary>
    public string? OffsetX { get; set; }

    /// <summary>
    /// Moves the badge along the vertical axis by the given CSS length, on top of its <see cref="Position"/>.
    /// </summary>
    public string? OffsetY { get; set; }

    /// <summary>
    /// Pulls the badge in towards the child content, for a child with a rounded outline such as an avatar.
    /// </summary>
    public bool? Overlap { get; set; }

    /// <summary>
    /// The position of the badge.
    /// </summary>
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Renders an expanding ring around the badge to report that something is in progress.
    /// </summary>
    public bool? Pulse { get; set; }

    /// <summary>
    /// The relationship between the current document and the one the <see cref="Href"/> of the badge leads to.
    /// </summary>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Reverses the direction flow of the content of the badge, which puts the icon after the content.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The corner shape of the badge.
    /// </summary>
    public BitBadgeShape? Shape { get; set; }

    /// <summary>
    /// Renders the badge when its content is the number zero.
    /// </summary>
    public bool? ShowZero { get; set; }

    /// <summary>
    /// The size of the badge.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the badge.
    /// </summary>
    public BitBadgeClassStyles? Styles { get; set; }

    /// <summary>
    /// The browsing context the <see cref="Href"/> of the badge is opened in, for example <c>_blank</c>.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the badge.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the badge.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitBadge"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitBadge"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitBadge"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitBadge"/>.
    /// </remarks>
    /// <param name="bitBadge">
    /// The <see cref="BitBadge"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitBadge bitBadge)
    {
        if (bitBadge is null) return;

        UpdateBaseParameters(bitBadge);

        if (Bordered.HasValue && bitBadge.HasNotBeenSet(nameof(Bordered)))
        {
            bitBadge.Bordered = Bordered.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Classes is not null && bitBadge.HasNotBeenSet(nameof(Classes)))
        {
            bitBadge.Classes = Classes;

            bitBadge.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitBadge.HasNotBeenSet(nameof(Color)))
        {
            bitBadge.Color = Color.Value;

            bitBadge.ClassBuilder.Reset();
        }

        bool contentOrMaxWasSet = false;

        if (Content is not null && bitBadge.HasNotBeenSet(nameof(Content)))
        {
            bitBadge.Content = Content;

            contentOrMaxWasSet = true;
        }

        if (ContentTemplate is not null && bitBadge.HasNotBeenSet(nameof(ContentTemplate)))
        {
            bitBadge.ContentTemplate = ContentTemplate;
        }

        if (Description.HasValue() && bitBadge.HasNotBeenSet(nameof(Description)))
        {
            bitBadge.Description = Description;
        }

        if (Dot.HasValue && bitBadge.HasNotBeenSet(nameof(Dot)))
        {
            bitBadge.Dot = Dot.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Hidden.HasValue && bitBadge.HasNotBeenSet(nameof(Hidden)))
        {
            bitBadge.Hidden = Hidden.Value;
        }

        bool linkWasSet = false;

        if (Href.HasValue() && bitBadge.HasNotBeenSet(nameof(Href)))
        {
            bitBadge.Href = Href;

            linkWasSet = true;
        }

        if (Icon is not null && bitBadge.HasNotBeenSet(nameof(Icon)))
        {
            bitBadge.Icon = Icon;
        }

        if (IconName.HasValue() && bitBadge.HasNotBeenSet(nameof(IconName)))
        {
            bitBadge.IconName = IconName;
        }

        if (Inline.HasValue && bitBadge.HasNotBeenSet(nameof(Inline)))
        {
            bitBadge.Inline = Inline.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Live.HasValue && bitBadge.HasNotBeenSet(nameof(Live)))
        {
            bitBadge.Live = Live.Value;
        }

        if (Max.HasValue && bitBadge.HasNotBeenSet(nameof(Max)))
        {
            bitBadge.Max = Max.Value;

            contentOrMaxWasSet = true;
        }

        // Content and Max decide together what the badge prints, so the pair is read again once either of them
        // has been filled in from the cascade rather than through its own setter.
        if (contentOrMaxWasSet)
        {
            bitBadge.OnSetContentAndMax();
        }

        if (OffsetX.HasValue() && bitBadge.HasNotBeenSet(nameof(OffsetX)))
        {
            bitBadge.OffsetX = OffsetX;

            bitBadge.StyleBuilder.Reset();
        }

        if (OffsetY.HasValue() && bitBadge.HasNotBeenSet(nameof(OffsetY)))
        {
            bitBadge.OffsetY = OffsetY;

            bitBadge.StyleBuilder.Reset();
        }

        if (Overlap.HasValue && bitBadge.HasNotBeenSet(nameof(Overlap)))
        {
            bitBadge.Overlap = Overlap.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Position.HasValue && bitBadge.HasNotBeenSet(nameof(Position)))
        {
            bitBadge.Position = Position.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Pulse.HasValue && bitBadge.HasNotBeenSet(nameof(Pulse)))
        {
            bitBadge.Pulse = Pulse.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Rel.HasValue && bitBadge.HasNotBeenSet(nameof(Rel)))
        {
            bitBadge.Rel = Rel.Value;

            linkWasSet = true;
        }

        if (Reversed.HasValue && bitBadge.HasNotBeenSet(nameof(Reversed)))
        {
            bitBadge.Reversed = Reversed.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Shape.HasValue && bitBadge.HasNotBeenSet(nameof(Shape)))
        {
            bitBadge.Shape = Shape.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (ShowZero.HasValue && bitBadge.HasNotBeenSet(nameof(ShowZero)))
        {
            bitBadge.ShowZero = ShowZero.Value;
        }

        if (Size.HasValue && bitBadge.HasNotBeenSet(nameof(Size)))
        {
            bitBadge.Size = Size.Value;

            bitBadge.ClassBuilder.Reset();
        }

        if (Styles is not null && bitBadge.HasNotBeenSet(nameof(Styles)))
        {
            bitBadge.Styles = Styles;

            bitBadge.StyleBuilder.Reset();
        }

        if (Target.HasValue() && bitBadge.HasNotBeenSet(nameof(Target)))
        {
            bitBadge.Target = Target;

            linkWasSet = true;
        }

        // The rel attribute is derived from Href, Rel and Target together, so it is worked out again once any of
        // them has been filled in from the cascade.
        if (linkWasSet)
        {
            bitBadge.OnSetHrefAndRel();
        }

        if (Title.HasValue() && bitBadge.HasNotBeenSet(nameof(Title)))
        {
            bitBadge.Title = Title;
        }

        if (Variant.HasValue && bitBadge.HasNotBeenSet(nameof(Variant)))
        {
            bitBadge.Variant = Variant.Value;

            bitBadge.ClassBuilder.Reset();
        }
    }
}
