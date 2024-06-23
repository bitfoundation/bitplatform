namespace Bit.BlazorUI;

public partial class BitCarouselItem
{
    private string internalStyle = string.Empty;

    private string internalTransformStyle = string.Empty;

    private string internalTransitionStyle = string.Empty;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [CascadingParameter] protected BitCarousel? Carousel { get; set; }


    internal int Index;

    internal string InternalStyle
    {
        get => internalStyle;
        set
        {
            if (internalStyle == value) return;

            internalStyle = value;
            StyleBuilder.Reset();
        }
    }

    internal string InternalTransformStyle
    {
        get => internalTransformStyle;
        set
        {
            if (internalStyle == value) return;

            internalTransformStyle = value;
            StyleBuilder.Reset();
        }
    }

    internal string InternalTransitionStyle
    {
        get => internalTransitionStyle;
        set
        {
            if (internalStyle == value) return;

            internalTransitionStyle = value;
            StyleBuilder.Reset();
        }
    }


    protected override Task OnInitializedAsync()
    {
        if (Carousel is not null)
        {
            Carousel.RegisterItem(this);
        }

        return base.OnInitializedAsync();
    }

    protected override string RootElementClass => "bit-crsi";

    protected override void RegisterCssClasses()
    {
        StyleBuilder.Register(() => internalStyle);
        StyleBuilder.Register(() => internalTransformStyle);
        StyleBuilder.Register(() => internalTransitionStyle);
    }
}
