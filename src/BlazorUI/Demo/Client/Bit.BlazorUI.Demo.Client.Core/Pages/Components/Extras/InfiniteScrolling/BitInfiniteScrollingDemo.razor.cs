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
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render while loading the new items.",
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


    private async ValueTask<IEnumerable<int>> LoadItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);
        return Enumerable.Range(request.Skip, 20);
    }


    private readonly string example1RazorCode = @"
<BitInfiniteScrolling ItemsProvider=""GetItems"" Class=""container"">
    <ItemTemplate Context=""item"">
        <div>Item @item</div>
    </ItemTemplate>
    <LoadingTemplate>
        <div>Loading...</div>
    </LoadingTemplate>
</BitInfiniteScrolling>";
    private readonly string example1CsharpCode = @"
private async Task<IEnumerable<int>> GetItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 20);
}";

    private readonly string example2RazorCode = @"
<BitInfiniteScrolling ItemsProvider=""LoadItems"" Class=""advanced"">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <LoadingTemplate>
        <div class=""loading"">Loading...</div>
    </LoadingTemplate>
</BitInfiniteScrolling>";
    private readonly string example2CsharpCode = @"
private async Task<IEnumerable<int>> GetItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 20);
}";
}
