namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitLayout"/> component.
/// </summary>
public class BitLayoutParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitLayout"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitLayout value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitLayout)}";



    public string Name => ParamName;



    /// <summary>
    /// The accessible label of the aside section.
    /// </summary>
    public string? AsideAriaLabel { get; set; }

    /// <summary>
    /// The width of the aside section in pixels.
    /// </summary>
    public int? AsideWidth { get; set; }

    /// <summary>
    /// Draws divider lines between the sections of the layout.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the layout.
    /// </summary>
    public BitLayoutClassStyles? Classes { get; set; }

    /// <summary>
    /// The height of the footer section in pixels.
    /// </summary>
    public int? FooterHeight { get; set; }

    /// <summary>
    /// Makes the layout fill at least the height of the viewport.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the nav panel and the aside span the whole height of the layout, with the header and the footer between them.
    /// </summary>
    public bool? FullHeightPanels { get; set; }

    /// <summary>
    /// The space between the nav panel, the main content and the aside.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// The height of the header section in pixels, and the offset a sticky nav panel or aside pins itself at.
    /// </summary>
    public int? HeaderHeight { get; set; }

    /// <summary>
    /// Hides the aside section.
    /// </summary>
    public bool? HideAside { get; set; }

    /// <summary>
    /// Hides the footer section.
    /// </summary>
    public bool? HideFooter { get; set; }

    /// <summary>
    /// Hides the header section.
    /// </summary>
    public bool? HideHeader { get; set; }

    /// <summary>
    /// Hides the nav panel section.
    /// </summary>
    public bool? HideNavPanel { get; set; }

    /// <summary>
    /// The accessible label of the nav panel section.
    /// </summary>
    public string? NavPanelAriaLabel { get; set; }

    /// <summary>
    /// The width of the nav panel section in pixels.
    /// </summary>
    public int? NavPanelWidth { get; set; }

    /// <summary>
    /// Renders the main section as a plain element instead of the main landmark, for a layout nested inside another one.
    /// </summary>
    public bool? Nested { get; set; }

    /// <summary>
    /// The padding around the content of the main section.
    /// </summary>
    public string? Padding { get; set; }

    /// <summary>
    /// Reverses the position of the nav panel and the aside inside the middle row.
    /// </summary>
    public bool? ReverseNavPanel { get; set; }

    /// <summary>
    /// Keeps the header and the footer in place and lets only the sections of the middle row scroll.
    /// </summary>
    public bool? ScrollableMain { get; set; }

    /// <summary>
    /// Renders a skip link as the first focusable element of the layout, which jumps to the main section.
    /// </summary>
    public bool? SkipLink { get; set; }

    /// <summary>
    /// The text of the skip link.
    /// </summary>
    public string? SkipLinkText { get; set; }

    /// <summary>
    /// Enables sticky positioning of the aside.
    /// </summary>
    public bool? StickyAside { get; set; }

    /// <summary>
    /// Enables sticky positioning of the footer at the bottom of the viewport.
    /// </summary>
    public bool? StickyFooter { get; set; }

    /// <summary>
    /// Enables sticky positioning of the header at the top of the viewport.
    /// </summary>
    public bool? StickyHeader { get; set; }

    /// <summary>
    /// Enables sticky positioning of the nav panel.
    /// </summary>
    public bool? StickyNavPanel { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the layout.
    /// </summary>
    public BitLayoutClassStyles? Styles { get; set; }

    /// <summary>
    /// The stacking order of the pinned sections of the layout.
    /// </summary>
    public int? ZIndex { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitLayout"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitLayout"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitLayout"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitLayout"/>.
    /// </remarks>
    /// <param name="bitLayout">
    /// The <see cref="BitLayout"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitLayout bitLayout)
    {
        if (bitLayout is null) return;

        UpdateBaseParameters(bitLayout);

        if (AsideAriaLabel.HasValue())
        {
            bitLayout.TakeFromCascade(nameof(AsideAriaLabel), AsideAriaLabel, static l => l.AsideAriaLabel, static (l, v) => l.AsideAriaLabel = v);
        }

        if (AsideWidth.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(AsideWidth), AsideWidth.Value, static l => l.AsideWidth, static (l, v) => l.AsideWidth = v);
        }

        if (Bordered.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(Bordered), Bordered.Value, static l => l.Bordered, static (l, v) => l.Bordered = v);
        }

        if (Classes is not null)
        {
            bitLayout.TakeFromCascade(nameof(Classes), Classes, static l => l.Classes, static (l, v) => l.Classes = v);
        }

        if (FooterHeight.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(FooterHeight), FooterHeight.Value, static l => l.FooterHeight, static (l, v) => l.FooterHeight = v);
        }

        if (FullHeight.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(FullHeight), FullHeight.Value, static l => l.FullHeight, static (l, v) => l.FullHeight = v);
        }

        if (FullHeightPanels.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(FullHeightPanels), FullHeightPanels.Value, static l => l.FullHeightPanels, static (l, v) => l.FullHeightPanels = v);
        }

        if (Gap.HasValue())
        {
            bitLayout.TakeFromCascade(nameof(Gap), Gap, static l => l.Gap, static (l, v) => l.Gap = v);
        }

        if (HeaderHeight.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(HeaderHeight), HeaderHeight.Value, static l => l.HeaderHeight, static (l, v) => l.HeaderHeight = v);
        }

        if (HideAside.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(HideAside), HideAside.Value, static l => l.HideAside, static (l, v) => l.HideAside = v);
        }

        if (HideFooter.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(HideFooter), HideFooter.Value, static l => l.HideFooter, static (l, v) => l.HideFooter = v);
        }

        if (HideHeader.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(HideHeader), HideHeader.Value, static l => l.HideHeader, static (l, v) => l.HideHeader = v);
        }

        if (HideNavPanel.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(HideNavPanel), HideNavPanel.Value, static l => l.HideNavPanel, static (l, v) => l.HideNavPanel = v);
        }

        if (NavPanelAriaLabel.HasValue())
        {
            bitLayout.TakeFromCascade(nameof(NavPanelAriaLabel), NavPanelAriaLabel, static l => l.NavPanelAriaLabel, static (l, v) => l.NavPanelAriaLabel = v);
        }

        if (NavPanelWidth.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(NavPanelWidth), NavPanelWidth.Value, static l => l.NavPanelWidth, static (l, v) => l.NavPanelWidth = v);
        }

        if (Nested.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(Nested), Nested.Value, static l => l.Nested, static (l, v) => l.Nested = v);
        }

        if (Padding.HasValue())
        {
            bitLayout.TakeFromCascade(nameof(Padding), Padding, static l => l.Padding, static (l, v) => l.Padding = v);
        }

        if (ReverseNavPanel.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(ReverseNavPanel), ReverseNavPanel.Value, static l => l.ReverseNavPanel, static (l, v) => l.ReverseNavPanel = v);
        }

        if (ScrollableMain.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(ScrollableMain), ScrollableMain.Value, static l => l.ScrollableMain, static (l, v) => l.ScrollableMain = v);
        }

        if (SkipLink.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(SkipLink), SkipLink.Value, static l => l.SkipLink, static (l, v) => l.SkipLink = v);
        }

        if (SkipLinkText.HasValue())
        {
            bitLayout.TakeFromCascade(nameof(SkipLinkText), SkipLinkText, static l => l.SkipLinkText, static (l, v) => l.SkipLinkText = v);
        }

        if (StickyAside.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(StickyAside), StickyAside.Value, static l => l.StickyAside, static (l, v) => l.StickyAside = v);
        }

        if (StickyFooter.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(StickyFooter), StickyFooter.Value, static l => l.StickyFooter, static (l, v) => l.StickyFooter = v);
        }

        if (StickyHeader.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(StickyHeader), StickyHeader.Value, static l => l.StickyHeader, static (l, v) => l.StickyHeader = v);
        }

        if (StickyNavPanel.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(StickyNavPanel), StickyNavPanel.Value, static l => l.StickyNavPanel, static (l, v) => l.StickyNavPanel = v);
        }

        if (Styles is not null)
        {
            bitLayout.TakeFromCascade(nameof(Styles), Styles, static l => l.Styles, static (l, v) => l.Styles = v);
        }

        if (ZIndex.HasValue)
        {
            bitLayout.TakeFromCascade(nameof(ZIndex), ZIndex.Value, static l => l.ZIndex, static (l, v) => l.ZIndex = v);
        }
    }
}
