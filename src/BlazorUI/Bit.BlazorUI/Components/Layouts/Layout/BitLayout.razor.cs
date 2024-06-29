namespace Bit.BlazorUI;

public partial class BitLayout : BitComponentBase
{
    /// <summary>
    /// Custom CSS classes for different parts of the BitLayout.
    /// </summary>
    [Parameter] public BitLayoutClassStyles? Classes { get; set; }

    /// <summary>
    /// Enables fixed positioning of the header at the top of the viewport.
    /// </summary>
    [Parameter] public bool FixedHeader { get; set; }

    /// <summary>
    /// Enables fixed positioning of the footer at the bottom of the viewport.
    /// </summary>
    [Parameter] public bool FixedFooter { get; set; }

    /// <summary>
    /// The content of the footer section.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The height of the footer in px to calculate heights and paddings.
    /// </summary>
    [Parameter] public int FooterHeight { get; set; }

    /// <summary>
    /// The content of the header section.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The height of the header in px to calculate heights and paddings.
    /// </summary>
    [Parameter] public int HeaderHeight { get; set; }

    /// <summary>
    /// Hides NavMenu content when true.
    /// </summary>
    [Parameter] public bool HideNavMenu { get; set; }

    /// <summary>
    /// The content of the main section.
    /// </summary>
    [Parameter] public RenderFragment? Main { get; set; }

    /// <summary>
    /// The content of the nav-menu section.
    /// </summary>
    [Parameter] public RenderFragment? NavMenu { get; set; }

    /// <summary>
    /// The height of the status bar on mobile devices to calculate heights and paddings.
    /// </summary>
    [Parameter] public int StatusBarHeight { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitLayout.
    /// </summary>
    [Parameter] public BitLayoutClassStyles? Styles { get; set; }


    protected override string RootElementClass => "bit-lyt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }
}
