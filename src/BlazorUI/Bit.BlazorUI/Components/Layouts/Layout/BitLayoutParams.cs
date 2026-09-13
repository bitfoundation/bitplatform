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

        if (AsideAriaLabel.HasValue() && bitLayout.HasNotBeenSet(nameof(AsideAriaLabel)))
        {
            bitLayout.AsideAriaLabel = AsideAriaLabel;
        }

        if (AsideWidth.HasValue && bitLayout.HasNotBeenSet(nameof(AsideWidth)))
        {
            bitLayout.AsideWidth = AsideWidth.Value;
        }

        if (Bordered.HasValue && bitLayout.HasNotBeenSet(nameof(Bordered)))
        {
            bitLayout.Bordered = Bordered.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (Classes is not null && bitLayout.HasNotBeenSet(nameof(Classes)))
        {
            bitLayout.Classes = Classes;

            bitLayout.ClassBuilder.Reset();
        }

        if (FooterHeight.HasValue && bitLayout.HasNotBeenSet(nameof(FooterHeight)))
        {
            bitLayout.FooterHeight = FooterHeight.Value;

            bitLayout.StyleBuilder.Reset();
        }

        if (FullHeight.HasValue && bitLayout.HasNotBeenSet(nameof(FullHeight)))
        {
            bitLayout.FullHeight = FullHeight.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (FullHeightPanels.HasValue && bitLayout.HasNotBeenSet(nameof(FullHeightPanels)))
        {
            bitLayout.FullHeightPanels = FullHeightPanels.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (Gap.HasValue() && bitLayout.HasNotBeenSet(nameof(Gap)))
        {
            bitLayout.Gap = Gap;

            bitLayout.StyleBuilder.Reset();
        }

        if (HeaderHeight.HasValue && bitLayout.HasNotBeenSet(nameof(HeaderHeight)))
        {
            bitLayout.HeaderHeight = HeaderHeight.Value;

            bitLayout.StyleBuilder.Reset();
        }

        if (HideAside.HasValue && bitLayout.HasNotBeenSet(nameof(HideAside)))
        {
            bitLayout.HideAside = HideAside.Value;
        }

        if (HideFooter.HasValue && bitLayout.HasNotBeenSet(nameof(HideFooter)))
        {
            bitLayout.HideFooter = HideFooter.Value;
        }

        if (HideHeader.HasValue && bitLayout.HasNotBeenSet(nameof(HideHeader)))
        {
            bitLayout.HideHeader = HideHeader.Value;
        }

        if (HideNavPanel.HasValue && bitLayout.HasNotBeenSet(nameof(HideNavPanel)))
        {
            bitLayout.HideNavPanel = HideNavPanel.Value;
        }

        if (NavPanelAriaLabel.HasValue() && bitLayout.HasNotBeenSet(nameof(NavPanelAriaLabel)))
        {
            bitLayout.NavPanelAriaLabel = NavPanelAriaLabel;
        }

        if (NavPanelWidth.HasValue && bitLayout.HasNotBeenSet(nameof(NavPanelWidth)))
        {
            bitLayout.NavPanelWidth = NavPanelWidth.Value;
        }

        if (Nested.HasValue && bitLayout.HasNotBeenSet(nameof(Nested)))
        {
            bitLayout.Nested = Nested.Value;
        }

        if (Padding.HasValue() && bitLayout.HasNotBeenSet(nameof(Padding)))
        {
            bitLayout.Padding = Padding;

            bitLayout.StyleBuilder.Reset();
        }

        if (ReverseNavPanel.HasValue && bitLayout.HasNotBeenSet(nameof(ReverseNavPanel)))
        {
            bitLayout.ReverseNavPanel = ReverseNavPanel.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (ScrollableMain.HasValue && bitLayout.HasNotBeenSet(nameof(ScrollableMain)))
        {
            bitLayout.ScrollableMain = ScrollableMain.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (SkipLink.HasValue && bitLayout.HasNotBeenSet(nameof(SkipLink)))
        {
            bitLayout.SkipLink = SkipLink.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (SkipLinkText.HasValue() && bitLayout.HasNotBeenSet(nameof(SkipLinkText)))
        {
            bitLayout.SkipLinkText = SkipLinkText;
        }

        if (StickyAside.HasValue && bitLayout.HasNotBeenSet(nameof(StickyAside)))
        {
            bitLayout.StickyAside = StickyAside.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (StickyFooter.HasValue && bitLayout.HasNotBeenSet(nameof(StickyFooter)))
        {
            bitLayout.StickyFooter = StickyFooter.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (StickyHeader.HasValue && bitLayout.HasNotBeenSet(nameof(StickyHeader)))
        {
            bitLayout.StickyHeader = StickyHeader.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (StickyNavPanel.HasValue && bitLayout.HasNotBeenSet(nameof(StickyNavPanel)))
        {
            bitLayout.StickyNavPanel = StickyNavPanel.Value;

            bitLayout.ClassBuilder.Reset();
        }

        if (Styles is not null && bitLayout.HasNotBeenSet(nameof(Styles)))
        {
            bitLayout.Styles = Styles;

            bitLayout.StyleBuilder.Reset();
        }

        if (ZIndex.HasValue && bitLayout.HasNotBeenSet(nameof(ZIndex)))
        {
            bitLayout.ZIndex = ZIndex.Value;

            bitLayout.StyleBuilder.Reset();
        }
    }
}
