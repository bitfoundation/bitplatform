namespace Bit.BlazorUI;

public partial class BitSwiperItem : BitComponentBase
{
    private string internalStyle = string.Empty;
    private string internalTransformStyle = string.Empty;
    private string internalTransitionStyle = string.Empty;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [CascadingParameter] protected BitSwiper? Swiper { get; set; }

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
            if (internalTransformStyle == value) return;

            internalTransformStyle = value;
            StyleBuilder.Reset();
        }
    }

    internal string InternalTransitionStyle
    {
        get => internalTransitionStyle;
        set
        {
            if (internalTransitionStyle == value) return;

            internalTransitionStyle = value;
            StyleBuilder.Reset();
        }
    }


    protected override string RootElementClass => "bit-swpi";

    protected override void RegisterCssClasses()
    {
        StyleBuilder.Register(() => internalStyle);
        StyleBuilder.Register(() => internalTransformStyle);
        StyleBuilder.Register(() => internalTransitionStyle);
    }

    protected override Task OnInitializedAsync()
    {
        Swiper?.RegisterItem(this);

        return base.OnInitializedAsync();
    }
}
