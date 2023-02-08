namespace Bit.BlazorUI;

public partial class BitCarouselItem
{
    protected override bool UseVisual => false;

    internal int Index;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [CascadingParameter] protected BitCarousel? Carousel { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (Carousel is not null)
        {
            Carousel.RegisterItem(this);
        }

        return base.OnInitializedAsync();
    }

    protected override string RootElementClass => "bit-crsi";

    protected override void RegisterComponentClasses()
    {
        StyleBuilder.Register(() => internalStyle);
        StyleBuilder.Register(() => internalTransformStyle);
        StyleBuilder.Register(() => internalTransitionStyle);
    }

    private string internalStyle = string.Empty;
    internal string InternalStyle
    {
        get => internalStyle;
        set
        {
            internalStyle = value;
            StyleBuilder.Reset();
        }
    }

    private string internalTransformStyle = string.Empty;
    internal string InternalTransformStyle
    {
        get => internalTransformStyle;
        set
        {
            internalTransformStyle = value;
            StyleBuilder.Reset();
        }
    }

    private string internalTransitionStyle = string.Empty;
    internal string InternalTransitionStyle
    {
        get => internalTransitionStyle;
        set
        {
            internalTransitionStyle = value;
            StyleBuilder.Reset();
        }
    }
}
