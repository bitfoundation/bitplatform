namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.InfiniteScrolling;

public partial class BitInfiniteScrollingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "ItemsProvider",
            Type = "BitInfiniteScrollingItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The item provider function that will be called when scrolling ends.",
         },
         new()
         {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render each item.",
         },
         new()
         {
            Name = "LastElementClass",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS class of the last element that triggers the loading.",
         },
         new()
         {
            Name = "LastElementHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the last element that triggers the loading.",
         },
         new()
         {
            Name = "LastElementStyle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS style of the last element that triggers the loading.",
         },
         new()
         {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render while loading the new items.",
         },
         new()
         {
            Name = "Preload",
            Type = "bool",
            DefaultValue = "null",
            Description = "Pre-loads the data at the initialization of the component. Useful in prerendering mode.",
         },
         new()
         {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the scroll container, by default the root element of the component is selected for this purpose.",
         },
         new()
         {
            Name = "Threshold",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The threshold parameter for the IntersectionObserver that specifies a ratio of intersection area to total bounding box area of the last element.",
         },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
         new()
         {
            Name = "RefreshDataAsync",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Refreshes the items and re-renders them from scratch.",
         },
    ];


    private async ValueTask<IEnumerable<int>> LoadBasicItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);
        return Enumerable.Range(request.Skip, 20);
    }

    private async ValueTask<IEnumerable<int>> LoadAdvancedItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        if (request.Skip > 200) return [];
        await Task.Delay(1000);
        return Enumerable.Range(request.Skip, 50);
    }


    private readonly string example1RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadBasicItems"" Class=""basic"" Context=""item"">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example1CsharpCode = @"
private async ValueTask<IEnumerable<int>> LoadBasicItems(BitInfiniteScrollingItemsProviderRequest request)
{
    if (request.Skip > 200) return [];
    await Task.Delay(1000);1
    return Enumerable.Range(request.Skip, 20);
}";

    private readonly string example2RazorCode = @"
<style>
    .advanced {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
        position: relative;
    }

    .item {
        padding: 1rem;
        border: 1px solid gray;
    }

    .loading {
        width: 100%;
        padding: 1rem;
        display: flex;
        align-items: center;
        justify-content: center;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadAdvancedItems"" Class=""advanced"" LastElementHeight=""96px"" ScrollerSelector=""body"" Preload>
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <LoadingTemplate>
        <div class=""loading"">
            <BitEllipsisLoading />
        </div>
    </LoadingTemplate>
</BitInfiniteScrolling>";
    private readonly string example2CsharpCode = @"
private async ValueTask<IEnumerable<int>> LoadAdvancedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 50);
}";
}
