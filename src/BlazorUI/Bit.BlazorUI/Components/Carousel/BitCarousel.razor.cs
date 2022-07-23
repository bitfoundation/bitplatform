using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitCarousel
{
    private BitCarouselItem? SelectedItem;
    private int CurrentIndex;
    private List<BitCarouselItem> AllCarouselItems = new();
    private string? selectedKey;
    private bool SelectedKeyHasBeenSet;

    /// <summary>
    /// If enabled the carousel items will navigate in a loop (first item comes after last item and last item comes before first item).
    /// </summary>
    [Parameter] public bool IsSlideShow { get; set; }

    /// <summary>
    /// Items of the carousel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The Key of the current(selected) item of the carousel.
    /// </summary>
    [Parameter]
    public string? SelectedKey
    {
        get => selectedKey;
        set
        {
            if (value == selectedKey) return;

            SelectItemByKey(value);
        }
    }

    [Parameter] public EventCallback<string?> SelectedKeyChanged { get; set; }

    /// <summary>
    /// Shows the Dots indicator at the bottom of the BitCarousel.
    /// </summary>
    [Parameter] public bool ShowDots { get; set; } = true;

    public void GoNext()
    {
        SelectItem(CurrentIndex + 1);
    }

    public void GoPrev()
    {
        SelectItem(CurrentIndex - 1);
    }

    public void GoTo(int index)
    {
        SelectItem(index);
    }

    protected override string RootElementClass => "bit-crsl";

    internal void RegisterItem(BitCarouselItem carouselItem)
    {
        if (IsEnabled is false)
        {
            carouselItem.IsEnabled = false;
        }

        carouselItem.Index = AllCarouselItems.Count;

        AllCarouselItems.Add(carouselItem);

        if (AllCarouselItems.Count == 1 || SelectedKey.HasValue() && SelectedKey == carouselItem.Key)
        {
            carouselItem.SetState(true);
            SelectedItem = carouselItem;
        }
        StateHasChanged();
    }

    internal void UnregisterItem(BitCarouselItem carouselItem)
    {
        AllCarouselItems.Remove(carouselItem);
    }

    internal async Task SelectItem(BitCarouselItem item)
    {
        if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

        SelectedItem?.SetState(false);
        item.SetState(true);

        SelectedItem = item;
        selectedKey = item.Key;
        CurrentIndex = item.Index;

        await SelectedKeyChanged.InvokeAsync(selectedKey);
    }

    private void SelectItemByKey(string? key)
    {
        if (key.HasNoValue()) return;

        var newItem = AllCarouselItems.FirstOrDefault(i => i.Key == key);

        if (newItem == null || newItem == SelectedItem || newItem.IsEnabled is false)
        {
            _ = SelectedKeyChanged.InvokeAsync(selectedKey);
            return;
        }

        _ = SelectItem(newItem);
    }

    private void SelectItem(int index)
    {
        if (index < 0)
        {
            index = IsSlideShow ? AllCarouselItems.Count - 1 : 0;
        }
        else if (index >= AllCarouselItems.Count)
        {
            index = IsSlideShow ? 0 : AllCarouselItems.Count - 1;
        }

        var newItem = AllCarouselItems.ElementAt(index);

        if (newItem == null || newItem == SelectedItem || newItem.IsEnabled is false)
        {
            return;
        }

        _ = SelectItem(newItem);
    }
}
