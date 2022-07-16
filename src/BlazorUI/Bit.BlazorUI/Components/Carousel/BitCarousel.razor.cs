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

    [Parameter] public bool IsSlideShow { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

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

    private void SelectItemByIndex(int index)
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
