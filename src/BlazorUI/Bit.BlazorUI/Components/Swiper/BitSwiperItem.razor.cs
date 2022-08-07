using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitSwiperItem
{
    internal int Index;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [CascadingParameter] protected BitSwiper? Swiper { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (Swiper is not null)
        {
            Swiper.RegisterItem(this);
        }

        return base.OnInitializedAsync();
    }

    protected override string RootElementClass => "bit-sls-item";

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
