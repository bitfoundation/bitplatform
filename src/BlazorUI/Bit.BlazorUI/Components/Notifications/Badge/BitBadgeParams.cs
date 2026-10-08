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
    /// Hides the badge from assistive technologies, for a badge whose child content already says what it shows.
    /// </summary>
    public bool? Decorative { get; set; }

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
    public BitShape? Shape { get; set; }

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

        if (Bordered.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Bordered), Bordered.Value, static b => b.Bordered, static (b, v) => b.Bordered = v);
        }

        if (Classes is not null)
        {
            bitBadge.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Color.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Color), Color.Value, static b => b.Color, static (b, v) => b.Color = v);
        }

        if (Content is not null)
        {
            bitBadge.TakeFromCascade(nameof(Content), Content, static b => b.Content, static (b, v) => b.Content = v);
        }

        if (ContentTemplate is not null)
        {
            bitBadge.TakeFromCascade(nameof(ContentTemplate), ContentTemplate, static b => b.ContentTemplate, static (b, v) => b.ContentTemplate = v);
        }

        if (Decorative.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Decorative), Decorative.Value, static b => b.Decorative, static (b, v) => b.Decorative = v);
        }

        if (Description.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(Description), Description, static b => b.Description, static (b, v) => b.Description = v);
        }

        if (Dot.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Dot), Dot.Value, static b => b.Dot, static (b, v) => b.Dot = v);
        }

        if (Hidden.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Hidden), Hidden.Value, static b => b.Hidden, static (b, v) => b.Hidden = v);
        }

        if (Href.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(Href), Href, static b => b.Href, static (b, v) => b.Href = v);
        }

        if (Icon is not null)
        {
            bitBadge.TakeFromCascade(nameof(Icon), Icon, static b => b.Icon, static (b, v) => b.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(IconName), IconName, static b => b.IconName, static (b, v) => b.IconName = v);
        }

        if (Inline.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Inline), Inline.Value, static b => b.Inline, static (b, v) => b.Inline = v);
        }

        if (Live.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Live), Live.Value, static b => b.Live, static (b, v) => b.Live = v);
        }

        if (Max.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Max), Max.Value, static b => b.Max, static (b, v) => b.Max = v);
        }

        if (OffsetX.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(OffsetX), OffsetX, static b => b.OffsetX, static (b, v) => b.OffsetX = v);
        }

        if (OffsetY.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(OffsetY), OffsetY, static b => b.OffsetY, static (b, v) => b.OffsetY = v);
        }

        if (Overlap.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Overlap), Overlap.Value, static b => b.Overlap, static (b, v) => b.Overlap = v);
        }

        if (Position.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Position), Position.Value, static b => b.Position, static (b, v) => b.Position = v);
        }

        if (Pulse.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Pulse), Pulse.Value, static b => b.Pulse, static (b, v) => b.Pulse = v);
        }

        if (Rel.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Rel), Rel.Value, static b => b.Rel, static (b, v) => b.Rel = v);
        }

        if (Reversed.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Reversed), Reversed.Value, static b => b.Reversed, static (b, v) => b.Reversed = v);
        }

        if (Shape.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Shape), Shape.Value, static b => b.Shape, static (b, v) => b.Shape = v);
        }

        if (ShowZero.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(ShowZero), ShowZero.Value, static b => b.ShowZero, static (b, v) => b.ShowZero = v);
        }

        if (Size.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Size), Size.Value, static b => b.Size, static (b, v) => b.Size = v);
        }

        if (Styles is not null)
        {
            bitBadge.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }

        if (Target.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(Target), Target, static b => b.Target, static (b, v) => b.Target = v);
        }

        if (Title.HasValue())
        {
            bitBadge.TakeFromCascade(nameof(Title), Title, static b => b.Title, static (b, v) => b.Title = v);
        }

        if (Variant.HasValue)
        {
            bitBadge.TakeFromCascade(nameof(Variant), Variant.Value, static b => b.Variant, static (b, v) => b.Variant = v);
        }
    }
}
