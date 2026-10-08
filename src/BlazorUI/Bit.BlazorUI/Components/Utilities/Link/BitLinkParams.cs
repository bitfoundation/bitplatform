namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitLink"/> component.
/// </summary>
/// <remarks>
/// Every parameter of the link that is a value rather than content or a callback is here, and each is a default
/// rather than an override: a link that sets a parameter for itself keeps its own value, and only what it left
/// unset is filled in from the cascade. What a subtree of links usually shares is how they look and how they
/// behave - a color, an underline, a size, a <see cref="Target"/>, the <see cref="Match"/> every link of a
/// navigation bar follows the URL with - while an <see cref="Href"/> or a <see cref="Title"/> shared by every
/// link would be one link written many times over. The one worth having above all the others is <see cref="NewTabHint"/>: the sentence a new-tab link is announced with is English
/// until an app says otherwise, and an app says it once here rather than at every link it writes.
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
    /// Reports the link as the current item of the set it belongs to, through the <c>aria-current</c> attribute.
    /// <br />
    /// <see cref="BitLink.AriaCurrent"/>.
    /// </summary>
    public BitNavAriaCurrent? AriaCurrent { get; set; }

    /// <summary>
    /// A longer description of the link for the benefit of screen readers, rendered into <c>aria-describedby</c>.
    /// <br />
    /// <see cref="BitLink.AriaDescription"/>.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// Gives the link the focus as soon as it is rendered, through the <c>autofocus</c> attribute.
    /// <br />
    /// <see cref="BitLink.AutoFocus"/>.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// The general color of the link.
    /// <br />
    /// <see cref="BitLink.Color"/>.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The value of the download attribute of the link, which makes the browser save the linked resource.
    /// <br />
    /// <see cref="BitLink.Download"/>.
    /// </summary>
    public string? Download { get; set; }

    /// <summary>
    /// The URL the link points to.
    /// <br />
    /// <see cref="BitLink.Href"/>.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// The icon rendered beside the link content, using custom CSS classes for external icon libraries.
    /// <br />
    /// <see cref="BitLink.Icon"/>.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon rendered beside the link content, from the built-in Fluent UI icons.
    /// <br />
    /// <see cref="BitLink.IconName"/>.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The position of the icon relative to the link content.
    /// <br />
    /// <see cref="BitLink.IconPlacement"/>.
    /// </summary>
    /// <remarks>
    /// Only <see cref="BitPlacement.Start"/> and <see cref="BitPlacement.End"/> mean anything here, and they
    /// follow the reading direction: Start puts the icon before the content in an LTR component and after it
    /// in an RTL one. Every other placement leaves the icon where Start would put it.
    /// </remarks>
    public BitPlacement? IconPlacement { get; set; }

    /// <summary>
    /// Follows the URL the app sits on and reports the link as the current one while its href matches it.
    /// <br />
    /// <see cref="BitLink.Match"/>.
    /// </summary>
    public BitNavMatch? Match { get; set; }

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
    /// Suppresses the navigation a click on the link would otherwise perform.
    /// <br />
    /// <see cref="BitLink.PreventDefault"/>.
    /// </summary>
    public bool? PreventDefault { get; set; }

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
    /// The tooltip to show when the mouse is placed on the link.
    /// <br />
    /// <see cref="BitLink.Title"/>.
    /// </summary>
    public string? Title { get; set; }

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

        if (AllowDisabledFocus.HasValue)
        {
            bitLink.TakeFromCascade(nameof(AllowDisabledFocus), AllowDisabledFocus.Value, static l => l.AllowDisabledFocus, static (l, v) => l.AllowDisabledFocus = v);
        }

        if (AriaCurrent.HasValue)
        {
            bitLink.TakeFromCascade(nameof(AriaCurrent), AriaCurrent.Value, static l => l.AriaCurrent, static (l, v) => l.AriaCurrent = v);
        }

        if (AriaDescription.HasValue())
        {
            bitLink.TakeFromCascade(nameof(AriaDescription), AriaDescription, static l => l.AriaDescription, static (l, v) => l.AriaDescription = v);
        }

        if (AutoFocus.HasValue)
        {
            bitLink.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static l => l.AutoFocus, static (l, v) => l.AutoFocus = v);
        }

        if (Color.HasValue)
        {
            bitLink.TakeFromCascade(nameof(Color), Color.Value, static l => l.Color, static (l, v) => l.Color = v);
        }

        // An empty download is meaningful - it keeps the file name the server gives - so only a null one is unset.
        if (Download is not null)
        {
            bitLink.TakeFromCascade(nameof(Download), Download, static l => l.Download, static (l, v) => l.Download = v);
        }

        if (Href.HasValue())
        {
            bitLink.TakeFromCascade(nameof(Href), Href, static l => l.Href, static (l, v) => l.Href = v);
        }

        if (Icon is not null)
        {
            bitLink.TakeFromCascade(nameof(Icon), Icon, static l => l.Icon, static (l, v) => l.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitLink.TakeFromCascade(nameof(IconName), IconName, static l => l.IconName, static (l, v) => l.IconName = v);
        }

        if (IconPlacement.HasValue)
        {
            bitLink.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static l => l.IconPlacement, static (l, v) => l.IconPlacement = v);
        }

        if (Match.HasValue)
        {
            bitLink.TakeFromCascade(nameof(Match), Match.Value, static l => l.Match, static (l, v) => l.Match = v);
        }

        if (NewTabHint is not null)
        {
            bitLink.TakeFromCascade(nameof(NewTabHint), NewTabHint, static l => l.NewTabHint, static (l, v) => l.NewTabHint = v);
        }

        if (NoColor.HasValue)
        {
            bitLink.TakeFromCascade(nameof(NoColor), NoColor.Value, static l => l.NoColor, static (l, v) => l.NoColor = v);
        }

        if (NoNewTabHint.HasValue)
        {
            bitLink.TakeFromCascade(nameof(NoNewTabHint), NoNewTabHint.Value, static l => l.NoNewTabHint, static (l, v) => l.NoNewTabHint = v);
        }

        if (NoUnderline.HasValue)
        {
            bitLink.TakeFromCascade(nameof(NoUnderline), NoUnderline.Value, static l => l.NoUnderline, static (l, v) => l.NoUnderline = v);
        }

        if (PreventDefault.HasValue)
        {
            bitLink.TakeFromCascade(nameof(PreventDefault), PreventDefault.Value, static l => l.PreventDefault, static (l, v) => l.PreventDefault = v);
        }

        if (Rel.HasValue)
        {
            bitLink.TakeFromCascade(nameof(Rel), Rel.Value, static l => l.Rel, static (l, v) => l.Rel = v);
        }

        if (Size.HasValue)
        {
            bitLink.TakeFromCascade(nameof(Size), Size.Value, static l => l.Size, static (l, v) => l.Size = v);
        }

        if (StopPropagation.HasValue)
        {
            bitLink.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static l => l.StopPropagation, static (l, v) => l.StopPropagation = v);
        }

        if (Target.HasValue())
        {
            bitLink.TakeFromCascade(nameof(Target), Target, static l => l.Target, static (l, v) => l.Target = v);
        }

        if (Title.HasValue())
        {
            bitLink.TakeFromCascade(nameof(Title), Title, static l => l.Title, static (l, v) => l.Title = v);
        }

        if (Underlined.HasValue)
        {
            bitLink.TakeFromCascade(nameof(Underlined), Underlined.Value, static l => l.Underlined, static (l, v) => l.Underlined = v);
        }
    }
}
