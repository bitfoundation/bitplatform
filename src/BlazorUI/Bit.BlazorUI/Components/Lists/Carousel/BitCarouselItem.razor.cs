namespace Bit.BlazorUI;

public partial class BitCarouselItem : BitComponentBase
{
    [CascadingParameter] protected BitCarousel? Carousel { get; set; }



    [Parameter] public RenderFragment? ChildContent { get; set; }



    internal int Index;



    private string internalStyle = string.Empty;
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

    private string internalTransformStyle = string.Empty;
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

    private string internalTransitionStyle = string.Empty;
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



    protected override string RootElementClass => "bit-crsi";

    protected override void RegisterCssClasses()
    {
        StyleBuilder.Register(() => InternalStyle);
        StyleBuilder.Register(() => InternalTransformStyle);
        StyleBuilder.Register(() => InternalTransitionStyle);
    }

    protected override Task OnInitializedAsync()
    {
        Carousel?.RegisterItem(this);

        return base.OnInitializedAsync();
    }
}
