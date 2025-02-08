namespace Bit.BlazorUI;

/// <summary>
/// Layout can be used to create a base UI structure for an application.
/// </summary>
public partial class BitLayout : BitComponentBase
{
    /// <summary>
    /// Custom CSS classes for different parts of the BitLayout.
    /// </summary>
    [Parameter] public BitLayoutClassStyles? Classes { get; set; }

    /// <summary>
    /// The content of the footer section.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The content of the header section.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// Hides NavPanel content.
    /// </summary>
    [Parameter] public bool HideNavPanel { get; set; }

    /// <summary>
    /// The content of the main section.
    /// </summary>
    [Parameter] public RenderFragment? Main { get; set; }

    /// <summary>
    /// The content of the nav panel section.
    /// </summary>
    [Parameter] public RenderFragment? NavPanel { get; set; }

    /// <summary>
    /// The width of the nav panel section in px.
    /// </summary>
    [Parameter] public int NavPanelWidth { get; set; }

    /// <summary>
    /// Reverses the position of the nav panel inside the main container.
    /// </summary>
    [Parameter] public bool ReverseNavPanel { get; set; }

    /// <summary>
    /// Enables sticky positioning of the footer at the bottom of the viewport.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool StickyFooter { get; set; }

    /// <summary>
    /// Enables sticky positioning of the header at the top of the viewport.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool StickyHeader { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitLayout.
    /// </summary>
    [Parameter] public BitLayoutClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-lyt";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => StickyHeader ? "bit-lyt-shd" : string.Empty);
        ClassBuilder.Register(() => StickyFooter ? "bit-lyt-sft" : string.Empty);
    }
}
