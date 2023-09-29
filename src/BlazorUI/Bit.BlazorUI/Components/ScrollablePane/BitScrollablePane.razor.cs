namespace Bit.BlazorUI;

public partial class BitScrollablePane
{
    private int? _tabIndex;
    private string? _ariaLabel;
    private BitScrollbarVisibility _scrollbarVisibility = BitScrollbarVisibility.Auto;

    /// <summary>
    /// The content of the ScrollablePane, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Callback for when the ScrollablePane scrolled.
    /// </summary>
    [Parameter] public EventCallback OnScroll { get; set; }

    /// <summary>
    /// Controls the visibility of scrollbars in the ScrollablePane.
    /// </summary>
    [Parameter] public BitScrollbarVisibility ScrollbarVisibility
    {
        get => _scrollbarVisibility;
        set
        {
            _scrollbarVisibility = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// If ScrollContainerFocus is set to true, use this to give the container an accessible name.
    /// </summary>
    [Parameter] public string? ScrollContainerAriaLabel { get; set; }

    /// <summary>
    /// Makes the scrollable container focusable, to aid with keyboard-only scrolling Should only be set to true if the scrollable region will not contain any other focusable items.
    /// </summary>
    [Parameter] public bool ScrollContainerFocus { get; set; }



    protected override string RootElementClass => "bit-scp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ScrollbarVisibility switch
        {
            BitScrollbarVisibility.Hidden => $"{RootElementClass}-hdn",
            BitScrollbarVisibility.Scroll => $"{RootElementClass}-scr",
            _ => $"{RootElementClass}-aut"
        });
    }

    protected override void OnParametersSet()
    {
        if (ScrollContainerFocus)
        {
            _tabIndex = 0;
            _ariaLabel = ScrollContainerAriaLabel;
        }

        base.OnParametersSet();
    }
}
