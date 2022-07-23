using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitCarouselItem
{
    private bool IsCurrentHasBeenSet;

    private bool isCurrent;
    internal int Index;

    [Parameter]
    public bool IsCurrent
    {
        get => isCurrent;
        set
        {
            if (value == isCurrent) return;
            isCurrent = value;
            ClassBuilder.Reset();
            _ = IsCurrentChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsCurrentChanged { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string Key { get; set; } = string.Empty;

    [CascadingParameter] protected BitCarousel? Carousel { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (Carousel is not null)
        {
            Carousel.RegisterItem(this);
        }

        return base.OnInitializedAsync();
    }

    protected override string RootElementClass => "bit-crsl-item";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsCurrent
                                   ? $"{RootElementClass}-current" : string.Empty);
    }

    internal void SetState(bool status)
    {
        IsCurrent = status;
        StateHasChanged();
    }
}
