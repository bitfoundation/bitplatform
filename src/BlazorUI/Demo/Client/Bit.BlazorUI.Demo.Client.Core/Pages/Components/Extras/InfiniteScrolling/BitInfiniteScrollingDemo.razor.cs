namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.InfiniteScrolling;

public partial class BitInfiniteScrollingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "ChildContent",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render each item.",
         },
         new()
         {
            Name = "Classes",
            Type = "BitInfiniteScrollingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
         },
         new()
         {
            Name = "EmptyMessage",
            Type = "string",
            DefaultValue = "There is no item",
            Description = "The message to render when there is no item available and no EmptyTemplate is provided.",
         },
         new()
         {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render when there is no item available.",
         },
         new()
         {
            Name = "EndMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to render after the last page, when there is no more item to fetch. Nothing is rendered while both this parameter and the EndTemplate are empty.",
         },
         new()
         {
            Name = "EndTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render after the last page, when there is no more item to fetch.",
         },
         new()
         {
            Name = "ErrorMessage",
            Type = "string",
            DefaultValue = "Failed to load the items.",
            Description = "The message to render when the items provider throws and no ErrorTemplate is provided.",
         },
         new()
         {
            Name = "ErrorTemplate",
            Type = "RenderFragment<Exception>?",
            DefaultValue = "null",
            Description = "The custom template to render when the items provider throws, receiving the thrown exception as its context. It replaces the default error message and its retry button.",
         },
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
            Description = "Alias for ChildContent.",
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
            Name = "LoadingMessage",
            Type = "string",
            DefaultValue = "Loading...",
            Description = "The message to render while loading the new items and no LoadingTemplate is provided.",
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
            Name = "LoadMoreTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template of the button that loads the next page in the manual mode and retries a failed load.",
         },
         new()
         {
            Name = "LoadMoreText",
            Type = "string",
            DefaultValue = "Load more",
            Description = "The text of the button that loads the next page in the manual mode.",
         },
         new()
         {
            Name = "Manual",
            Type = "bool",
            DefaultValue = "false",
            Description = "Replaces the automatic loading with an explicit button, so each page is fetched only when the user asks for it.",
         },
         new()
         {
            Name = "MaxItems",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of items to load. The component stops fetching new pages as soon as the number of the loaded items reaches this value, and the last page it requests is narrowed down to what is still missing, so the list never grows beyond the cap.",
         },
         new()
         {
            Name = "OnEnd",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is invoked when the last page is loaded and there is no more item to fetch.",
         },
         new()
         {
            Name = "OnError",
            Type = "EventCallback<Exception>",
            DefaultValue = "",
            Description = "The callback that is invoked when the items provider throws, receiving the thrown exception.",
         },
         new()
         {
            Name = "OnItemsLoaded",
            Type = "EventCallback<IReadOnlyList<TItem>>",
            DefaultValue = "",
            Description = "The callback that is invoked after each successful load, receiving the newly loaded items of that page.",
         },
         new()
         {
            Name = "PageSize",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of the items to request in each page, which is sent to the items provider as the Count of its request. A provider returning fewer items than this value is considered the last page.",
         },
         new()
         {
            Name = "Preload",
            Type = "bool",
            DefaultValue = "false",
            Description = "Pre-loads the data at the initialization of the component. Useful in prerendering mode.",
         },
         new()
         {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prepends each loaded page before the already rendered items and moves the sentinel element to the top of the list, so scrolling up loads the older items of a chat or a log while the scroll position stays put. The list starts out scrolled to its newest items, and its root element becomes a flex column in this mode.",
         },
         new()
         {
            Name = "RootMargin",
            Type = "string?",
            DefaultValue = "null",
            Description = "The rootMargin parameter of the IntersectionObserver, which grows (or shrinks) the area around the scroll viewport that the last element is checked against.",
         },
         new()
         {
            Name = "RetryText",
            Type = "string",
            DefaultValue = "Retry",
            Description = "The text of the button that retries the failed load.",
         },
         new()
         {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the scroll container, by default the root element of the component is selected for this purpose. The window, document, body and html values all select the viewport of the page itself.",
         },
         new()
         {
            Name = "Styles",
            Type = "BitInfiniteScrollingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
         },
         new()
         {
            Name = "Threshold",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The threshold parameter for the IntersectionObserver that specifies a ratio of intersection area to total bounding box area of the last element. It defaults to 0, which fires as soon as a single pixel of the last element shows up.",
         },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitInfiniteScrollingClassStyles",
            Description = "Custom CSS classes/styles for different parts of the BitInfiniteScrolling.",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitInfiniteScrolling." },
                new() { Name = "LastElement", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the sentinel (last) element of the BitInfiniteScrolling that triggers the loading." },
                new() { Name = "Loading", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the loading container of the BitInfiniteScrolling." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the empty container of the BitInfiniteScrolling." },
                new() { Name = "End", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the end container of the BitInfiniteScrolling that renders after the last page." },
                new() { Name = "Error", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error container of the BitInfiniteScrolling." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the button of the BitInfiniteScrolling that loads the next page in manual mode and retries a failed load." },
            ]
        },
        new()
        {
            Id = "items-provider-request",
            Title = "BitInfiniteScrollingItemsProviderRequest",
            Description = "A request to a BitInfiniteScrollingItemsProvider for the next page of items.",
            Parameters =
            [
                new() { Name = "Skip", Type = "int", DefaultValue = "", Description = "The number of items already loaded, which is the index of the first item requested." },
                new() { Name = "Count", Type = "int", DefaultValue = "", Description = "The maximum number of items requested, which is the PageSize parameter of the component. It is 0 when no page size is configured." },
                new() { Name = "CancellationToken", Type = "CancellationToken", DefaultValue = "", Description = "A token that is cancelled when this request is no longer needed, for example when the data gets refreshed or the component gets disposed while the request is still in flight." },
            ]
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
         new()
         {
            Name = "Error",
            Type = "Exception?",
            DefaultValue = "null",
            Description = "The exception of the last failed load, or null while the last load succeeded.",
         },
         new()
         {
            Name = "HasMore",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines whether another page can still be fetched from the items provider.",
         },
         new()
         {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether a page is currently being fetched from the items provider.",
         },
         new()
         {
            Name = "Items",
            Type = "IReadOnlyList<TItem>",
            DefaultValue = "",
            Description = "The items loaded so far, in the order they are rendered.",
         },
         new()
         {
            Name = "AppendItemsAsync",
            Type = "Func<IEnumerable<TItem>, Task>",
            DefaultValue = "",
            Description = "Appends the provided items to the end of the already loaded items, without calling the items provider.",
         },
         new()
         {
            Name = "LoadMoreAsync",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Loads the next page of the items, the same way reaching the end of the list does. It is the way to load the next page in the manual mode, and to retry a failed load.",
         },
         new()
         {
            Name = "PrependItemsAsync",
            Type = "Func<IEnumerable<TItem>, Task>",
            DefaultValue = "",
            Description = "Prepends the provided items to the beginning of the already loaded items, without calling the items provider. The scroll position is kept stable.",
         },
         new()
         {
            Name = "RefreshDataAsync",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Refreshes the items and re-renders them from scratch.",
         },
         new()
         {
            Name = "ScrollToBottomAsync",
            Type = "Func<bool, Task>",
            DefaultValue = "",
            Description = "Scrolls the scroll container to its bottom. Useful in the reversed (chat) mode.",
         },
         new()
         {
            Name = "ScrollToTopAsync",
            Type = "Func<bool, Task>",
            DefaultValue = "",
            Description = "Scrolls the scroll container to its top.",
         },
    ];



    private const int TotalItems = 40;
    private const int TotalMessages = 40;

    private int loadedPages;
    private int loadedItems;
    private bool reachedEnd;
    private string? lastError;
    private int appendedItems;
    private bool faultyLoadFailed;
    private BitInfiniteScrolling<int>? membersRef;


    private async ValueTask<IEnumerable<int>> LoadBasicItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);
        return Enumerable.Range(request.Skip, 20);
    }

    private async ValueTask<IEnumerable<int>> LoadEmptyItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(2000);
        return [];
    }

    private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);
        var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
        return Enumerable.Range(request.Skip, count);
    }

    private async ValueTask<IEnumerable<int>> LoadFaultyItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);

        if (request.Skip == request.Count && faultyLoadFailed is false)
        {
            faultyLoadFailed = true;
            throw new InvalidOperationException("The demo provider fails once on the second page.");
        }

        var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
        return Enumerable.Range(request.Skip, count);
    }

    private async ValueTask<IEnumerable<string>> LoadOlderMessages(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);

        var remaining = TotalMessages - request.Skip;
        if (remaining <= 0) return [];

        var count = Math.Min(remaining, request.Count);
        var start = remaining - count;

        return Enumerable.Range(start, count).Select(i => $"Message {i + 1}");
    }

    private async ValueTask<IEnumerable<int>> LoadScrollerItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        if (request.Skip >= 200) return [];
        await Task.Delay(1000);
        return Enumerable.Range(request.Skip, 50);
    }

    private async ValueTask<IEnumerable<int>> LoadRtlItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);
        var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
        return Enumerable.Range(request.Skip, count);
    }


    private void HandleOnItemsLoaded(IReadOnlyList<int> items)
    {
        loadedPages++;
        loadedItems += items.Count;
    }

    private void HandleOnEnd() => reachedEnd = true;

    private void HandleOnError(Exception exception) => lastError = exception.Message;


    // The status line below the list lives in this page, so it needs a render of its own to catch up with
    // what the component has just loaded.
    private void HandleMembersLoaded(IReadOnlyList<int> items) => StateHasChanged();

    private async Task RefreshMembers()
    {
        if (membersRef is null) return;
        await membersRef.RefreshDataAsync();
    }

    private async Task AppendMemberItem()
    {
        if (membersRef is null) return;
        await membersRef.AppendItemsAsync([1000 + appendedItems++]);
    }

    private async Task ScrollMembersToTop()
    {
        if (membersRef is null) return;
        await membersRef.ScrollToTopAsync(true);
    }

    private async Task ScrollMembersToBottom()
    {
        if (membersRef is null) return;
        await membersRef.ScrollToBottomAsync(true);
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
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 20);
}";

    private readonly string example2RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }

    .item {
        padding: 0.5rem;
    }

    .loading, .empty {
        width: 100%;
        padding: 1rem;
        display: flex;
        align-items: center;
        justify-content: center;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadBasicItems"" Class=""basic"">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <LoadingTemplate>
        <div class=""loading"">
            <BitEllipsisLoading />
        </div>
    </LoadingTemplate>
</BitInfiniteScrolling>

<BitInfiniteScrolling ItemsProvider=""LoadEmptyItems"" Class=""basic"">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <EmptyTemplate>
        <div class=""empty""><b>--- No item ---</b></div>
    </EmptyTemplate>
</BitInfiniteScrolling>";
    private readonly string example2CsharpCode = @"
private async ValueTask<IEnumerable<int>> LoadBasicItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 20);
}

private async ValueTask<IEnumerable<int>> LoadEmptyItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(2000);
    return [];
}";

    private readonly string example3RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      PageSize=""15""
                      Class=""basic""
                      Context=""item""
                      EndMessage=""You have reached the end of the list."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example3CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example4RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      Manual
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      LoadMoreText=""Load more items""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example4CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example5RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadFaultyItems""
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      RetryText=""Try again""
                      ErrorMessage=""Something went wrong while loading the items.""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example5CsharpCode = @"
private const int TotalItems = 40;

private bool faultyLoadFailed;

private async ValueTask<IEnumerable<int>> LoadFaultyItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);

    if (request.Skip == request.Count && faultyLoadFailed is false)
    {
        faultyLoadFailed = true;
        throw new InvalidOperationException(""The demo provider fails once on the second page."");
    }

    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example6RazorCode = @"
<style>
    .chat {
        gap: 0.5rem;
        padding: 0.5rem;
        max-height: 300px;
    }

    .message {
        padding: 0.5rem;
        border-radius: 0.5rem;
        background-color: #80808040;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadOlderMessages""
                      Reversed
                      Preload
                      PageSize=""10""
                      Class=""chat""
                      Context=""message""
                      LoadingMessage=""Loading older messages...""
                      EndMessage=""This is the beginning of the conversation."">
    <div class=""message"">@message</div>
</BitInfiniteScrolling>";
    private readonly string example6CsharpCode = @"
private const int TotalMessages = 40;

private async ValueTask<IEnumerable<string>> LoadOlderMessages(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);

    var remaining = TotalMessages - request.Skip;
    if (remaining <= 0) return [];

    var count = Math.Min(remaining, request.Count);
    var start = remaining - count;

    return Enumerable.Range(start, count).Select(i => $""Message {i + 1}"");
}";

    private readonly string example7RazorCode = @"
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

<BitInfiniteScrolling ItemsProvider=""LoadScrollerItems""
                      Preload
                      Class=""advanced""
                      ScrollerSelector=""window""
                      RootMargin=""200px""
                      LastElementHeight=""96px""
                      EndMessage=""No more items to load."">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <LoadingTemplate>
        <div class=""loading"">
            <BitEllipsisLoading />
        </div>
    </LoadingTemplate>
</BitInfiniteScrolling>";
    private readonly string example7CsharpCode = @"
private async ValueTask<IEnumerable<int>> LoadScrollerItems(BitInfiniteScrollingItemsProviderRequest request)
{
    if (request.Skip >= 200) return [];
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 50);
}";

    private readonly string example8RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling TItem=""int""
                      ItemsProvider=""LoadPagedItems""
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      OnEnd=""HandleOnEnd""
                      OnError=""HandleOnError""
                      OnItemsLoaded=""HandleOnItemsLoaded"">
    <div>Item @item</div>
</BitInfiniteScrolling>

<div>Loaded pages: @loadedPages, loaded items: @loadedItems @(reachedEnd ? ""(end reached)"" : "" "") @lastError</div>";
    private readonly string example8CsharpCode = @"
private int loadedPages;
private int loadedItems;
private bool reachedEnd;
private string? lastError;

private void HandleOnItemsLoaded(IReadOnlyList<int> items)
{
    loadedPages++;
    loadedItems += items.Count;
}

private void HandleOnEnd() => reachedEnd = true;

private void HandleOnError(Exception exception) => lastError = exception.Message;";

    private readonly string example9RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling @ref=""membersRef""
                      TItem=""int""
                      ItemsProvider=""LoadPagedItems""
                      PageSize=""10""
                      MaxItems=""20""
                      Class=""basic""
                      Context=""item""
                      OnItemsLoaded=""HandleMembersLoaded""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton OnClick=""RefreshMembers"">RefreshDataAsync</BitButton>
    <BitButton OnClick=""AppendMemberItem"">AppendItemsAsync</BitButton>
    <BitButton OnClick=""ScrollMembersToTop"">ScrollToTopAsync</BitButton>
    <BitButton OnClick=""ScrollMembersToBottom"">ScrollToBottomAsync</BitButton>
</BitStack>

<div>Items: @(membersRef?.Items.Count ?? 0) &nbsp; HasMore: @(membersRef?.HasMore) &nbsp; IsLoading: @(membersRef?.IsLoading)</div>";
    private readonly string example9CsharpCode = @"
private int appendedItems;
private BitInfiniteScrolling<int>? membersRef;

private void HandleMembersLoaded(IReadOnlyList<int> items) => StateHasChanged();

private async Task RefreshMembers()
{
    if (membersRef is null) return;
    await membersRef.RefreshDataAsync();
}

private async Task AppendMemberItem()
{
    if (membersRef is null) return;
    await membersRef.AppendItemsAsync([1000 + appendedItems++]);
}

private async Task ScrollMembersToTop()
{
    if (membersRef is null) return;
    await membersRef.ScrollToTopAsync(true);
}

private async Task ScrollMembersToBottom()
{
    if (membersRef is null) return;
    await membersRef.ScrollToBottomAsync(true);
}";

    private readonly string example10RazorCode = @"
<style>
    .custom-loading {
        font-style: italic;
        color: blueviolet;
    }

    .custom-end {
        padding: 0.5rem;
        text-align: center;
        border-top: 1px solid gray;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      PageSize=""15""
                      Context=""item""
                      Style=""max-height:300px;border:1px solid gray""
                      Classes=""@(new() { Loading = ""custom-loading"", End = ""custom-end"" })""
                      Styles=""@(new() { Empty = ""text-align:center"" })""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example10CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example11RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling Dir=""BitDir.Rtl""
                      ItemsProvider=""LoadRtlItems""
                      PageSize=""15""
                      Class=""basic""
                      Context=""item""
                      LoadingMessage=""در حال دریافت...""
                      EndMessage=""به انتهای لیست رسیدید."">
    <div>آیتم @item</div>
</BitInfiniteScrolling>";
    private readonly string example11CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadRtlItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";
}
