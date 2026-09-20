namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitLink"/> component.
/// </summary>
/// <remarks>
/// What a subtree of links can share is how they look and what they are allowed to do, never where they go:
/// an <c>Href</c>, an icon or a title shared by every link of a page would be one link written many times
/// over, which is never what was meant. The one worth having above all the others is
/// <see cref="NewTabHint"/> - the sentence a new-tab link is announced with is English until an app says
/// otherwise, and an app says it once here rather than at every link it writes.
/// </remarks>
public class BitLinkParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitLink cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitLink value in parameterized APIs or
    /// configuration settings. Using this constant helps ensure consistency and reduces the risk of typographical
    /// errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitLink)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps the disabled link focusable and discoverable by assistive technologies.
    /// <br />
    /// <see cref="BitLink.AllowDisabledFocus"/>.
    /// </summary>
    public bool? AllowDisabledFocus { get; set; }

    /// <summary>
    /// The general color of the link.
    /// <br />
    /// <see cref="BitLink.Color"/>.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The position of the icon relative to the link content.
    /// <br />
    /// <see cref="BitLink.IconPosition"/>.
    /// </summary>
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// Replaces the text a new-tab link is announced with, for translating it or for saying it another way.
    /// <br />
    /// <see cref="BitLink.NewTabHint"/>.
    /// </summary>
    public string? NewTabHint { get; set; }

    /// <summary>
    /// Removes applying any foreground color to the link content, letting it keep its own color.
    /// <br />
    /// <see cref="BitLink.NoColor"/>.
    /// </summary>
    public bool? NoColor { get; set; }

    /// <summary>
    /// Stops a new-tab link from announcing that it opens in a new tab.
    /// <br />
    /// <see cref="BitLink.NoNewTabHint"/>.
    /// </summary>
    public bool? NoNewTabHint { get; set; }

    /// <summary>
    /// Styles the link to have no underline at any state.
    /// <br />
    /// <see cref="BitLink.NoUnderline"/>.
    /// </summary>
    public bool? NoUnderline { get; set; }

    /// <summary>
    /// The relationship between the current document and the linked document.
    /// <br />
    /// <see cref="BitLink.Rel"/>.
    /// </summary>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// The preset size of the link text.
    /// <br />
    /// <see cref="BitLink.Size"/>.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stops the propagation of the click event to the parent elements.
    /// <br />
    /// <see cref="BitLink.StopPropagation"/>.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// How to open the link, for example <c>_blank</c> to open it in a new tab.
    /// <br />
    /// <see cref="BitLink.Target"/>.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Styles the link with a fixed underline at all states.
    /// <br />
    /// <see cref="BitLink.Underlined"/>.
    /// </summary>
    public bool? Underlined { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitLink"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitLink"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitLink"/> will be
    /// updated. This method does not overwrite existing values on <paramref name="bitLink"/>.
    /// </remarks>
    /// <param name="bitLink">
    /// The <see cref="BitLink"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitLink bitLink)
    {
        if (bitLink is null) return;

        UpdateBaseParameters(bitLink);

        if (AllowDisabledFocus.HasValue && bitLink.HasNotBeenSet(nameof(AllowDisabledFocus)))
        {
            bitLink.AllowDisabledFocus = AllowDisabledFocus.Value;
        }

        if (Color.HasValue && bitLink.HasNotBeenSet(nameof(Color)))
        {
            bitLink.Color = Color.Value;

            bitLink.ClassBuilder.Reset();
        }

        if (IconPosition.HasValue && bitLink.HasNotBeenSet(nameof(IconPosition)))
        {
            bitLink.IconPosition = IconPosition.Value;

            bitLink.ClassBuilder.Reset();
        }

        if (NewTabHint is not null && bitLink.HasNotBeenSet(nameof(NewTabHint)))
        {
            bitLink.NewTabHint = NewTabHint;
        }

        if (NoColor.HasValue && bitLink.HasNotBeenSet(nameof(NoColor)))
        {
            bitLink.NoColor = NoColor.Value;

            bitLink.ClassBuilder.Reset();
        }

        if (NoNewTabHint.HasValue && bitLink.HasNotBeenSet(nameof(NoNewTabHint)))
        {
            bitLink.NoNewTabHint = NoNewTabHint.Value;
        }

        if (NoUnderline.HasValue && bitLink.HasNotBeenSet(nameof(NoUnderline)))
        {
            bitLink.NoUnderline = NoUnderline.Value;

            bitLink.ClassBuilder.Reset();
        }

        if (Rel.HasValue && bitLink.HasNotBeenSet(nameof(Rel)))
        {
            bitLink.Rel = Rel.Value;
        }

        if (Size.HasValue && bitLink.HasNotBeenSet(nameof(Size)))
        {
            bitLink.Size = Size.Value;

            bitLink.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitLink.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitLink.StopPropagation = StopPropagation.Value;
        }

        if (Target.HasValue() && bitLink.HasNotBeenSet(nameof(Target)))
        {
            bitLink.Target = Target;
        }

        if (Underlined.HasValue && bitLink.HasNotBeenSet(nameof(Underlined)))
        {
            bitLink.Underlined = Underlined.Value;

            bitLink.ClassBuilder.Reset();
        }
    }
}
