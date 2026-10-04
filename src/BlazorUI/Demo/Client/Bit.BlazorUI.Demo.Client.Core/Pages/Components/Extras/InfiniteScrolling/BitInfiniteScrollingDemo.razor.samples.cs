namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.InfiniteScrolling;

public partial class BitInfiniteScrollingDemo
{
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
</style>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      PageSize=""15""
                      Class=""basic""
                      Context=""item""
                      EndMessage=""You have reached the end of the list."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example2CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example3RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadEmptyItems""
                      Class=""basic""
                      Context=""item""
                      EmptyMessage=""Nothing to show here."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example3CsharpCode = @"
private async ValueTask<IEnumerable<int>> LoadEmptyItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(2000);
    return [];
}";

    private readonly string example4RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }

    .item {
        padding: 1rem;
        border: 1px solid gray;
    }

    .loading, .empty, .end {
        width: 100%;
        padding: 1rem;
        display: flex;
        align-items: center;
        justify-content: center;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems"" PageSize=""15"" Class=""basic"">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <LoadingTemplate>
        <div class=""loading"">
            <BitEllipsisLoading />
        </div>
    </LoadingTemplate>
    <EndTemplate>
        <div class=""end""><b>--- The end ---</b></div>
    </EndTemplate>
</BitInfiniteScrolling>

<BitInfiniteScrolling ItemsProvider=""LoadEmptyItems"" Class=""basic"">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
    <EmptyTemplate>
        <div class=""empty""><b>--- No item ---</b></div>
    </EmptyTemplate>
</BitInfiniteScrolling>";
    private readonly string example4CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}

private async ValueTask<IEnumerable<int>> LoadEmptyItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(2000);
    return [];
}";

    private readonly string example5RazorCode = @"
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
</BitInfiniteScrolling>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      Manual
                      PageSize=""10""
                      Class=""basic""
                      EndMessage=""No more items to load."">
    <ItemTemplate Context=""item"">
        <div>Item @item</div>
    </ItemTemplate>
    <LoadMoreTemplate>
        <b>+ Show 10 more</b>
    </LoadMoreTemplate>
</BitInfiniteScrolling>";
    private readonly string example5CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example6RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }

    .error {
        gap: 0.5rem;
        width: 100%;
        padding: 1rem;
        display: flex;
        align-items: center;
        justify-content: center;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadFaultyItems""
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      RetryText=""Try again""
                      ErrorMessage=""Something went wrong while loading the items.""
                      EndMessage=""No more items to load.""
                      OnError=""HandleOnError"">
    <div>Item @item</div>
</BitInfiniteScrolling>
<div>Last error: @(lastError ?? ""-"")</div>

<BitInfiniteScrolling @ref=""errorTemplateRef""
                      TItem=""int""
                      ItemsProvider=""LoadFaultyTemplateItems""
                      PageSize=""10""
                      Class=""basic""
                      EndMessage=""No more items to load."">
    <ItemTemplate Context=""item"">
        <div>Item @item</div>
    </ItemTemplate>
    <ErrorTemplate Context=""error"">
        <div class=""error"">
            <span>@error.Message</span>
            <BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"" OnClick=""RetryErrorTemplate"">Retry</BitButton>
        </div>
    </ErrorTemplate>
</BitInfiniteScrolling>";
    private readonly string example6CsharpCode = @"
private const int TotalItems = 40;

private string? lastError;
private bool faultyLoadFailed;
private bool faultyTemplateLoadFailed;
private BitInfiniteScrolling<int>? errorTemplateRef;

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
}

private async ValueTask<IEnumerable<int>> LoadFaultyTemplateItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);

    if (request.Skip == request.Count && faultyTemplateLoadFailed is false)
    {
        faultyTemplateLoadFailed = true;
        throw new InvalidOperationException(""The demo provider fails once on the second page."");
    }

    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}

private void HandleOnError(Exception exception) => lastError = exception.Message;

// The ErrorTemplate replaces the built-in retry button, so it offers its own.
private async Task RetryErrorTemplate()
{
    if (errorTemplateRef is null) return;
    await errorTemplateRef.LoadMoreAsync();
}";

    private readonly string example7RazorCode = @"
<style>
    .chat {
        gap: 0.5rem;
        padding: 0.5rem;
        max-height: 300px;
    }

    .message {
        gap: 0.5rem;
        display: flex;
        padding: 0.5rem;
        align-items: center;
        border-radius: 0.5rem;
        justify-content: space-between;
        background-color: #80808040;
    }

    .note {
        width: 6rem;
    }
</style>

<BitInfiniteScrolling TItem=""ChatMessage""
                      ItemsProvider=""LoadKeyedMessages""
                      Reversed
                      Preload
                      PageSize=""10""
                      Class=""chat""
                      Context=""message""
                      ItemKey=""@(m => m.Id)""
                      LoadingMessage=""Loading older messages...""
                      EndMessage=""This is the beginning of the conversation."">
    <div class=""message"">
        <span>@message.Text</span>
        <input class=""note"" placeholder=""note"" aria-label=""Note for @message.Text"" />
    </div>
</BitInfiniteScrolling>";
    private readonly string example7CsharpCode = @"
public record ChatMessage(int Id, string Text);

private const int TotalMessages = 40;

private async ValueTask<IEnumerable<ChatMessage>> LoadKeyedMessages(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);

    var remaining = TotalMessages - request.Skip;
    if (remaining <= 0) return [];

    var count = Math.Min(remaining, request.Count);
    var start = remaining - count;

    return Enumerable.Range(start, count).Select(i => new ChatMessage(i, $""Message {i + 1}""));
}";

    private readonly string example8RazorCode = @"
<style>
    .h-list {
        gap: 0.5rem;
        padding: 0.5rem;
    }

    .h-item {
        flex: 0 0 auto;
        padding: 1rem;
        white-space: nowrap;
        border: 1px solid gray;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      Horizontal
                      PageSize=""10""
                      Class=""h-list""
                      Context=""item""
                      EndMessage=""The end."">
    <div class=""h-item"">Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example8CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example9RazorCode = @"
<style>
    .grid {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
    }

    .item {
        padding: 1rem;
        border: 1px solid gray;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadScrollerItems""
                      Preload
                      Class=""grid""
                      ScrollerSelector=""window""
                      RootMargin=""200px""
                      LastElementHeight=""96px""
                      EndMessage=""No more items to load."">
    <ItemTemplate Context=""item"">
        <div class=""item"">Item @item</div>
    </ItemTemplate>
</BitInfiniteScrolling>";
    private readonly string example9CsharpCode = @"
private async ValueTask<IEnumerable<int>> LoadScrollerItems(BitInfiniteScrollingItemsProviderRequest request)
{
    if (request.Skip >= 200) return [];
    await Task.Delay(1000);
    return Enumerable.Range(request.Skip, 50);
}";

    private readonly string example10RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling @ref=""progressRef""
                      TItem=""int""
                      ItemsProvider=""LoadCatalogItems""
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      OnEnd=""HandleOnEnd""
                      OnItemsLoaded=""HandleOnItemsLoaded""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>

<div>
    Loaded @(progressRef?.Items.Count ?? 0) of @(progressRef?.TotalCount?.ToString() ?? ""?"") items
    in @loadedPages pages @(reachedEnd ? ""(end reached)"" : """")
</div>";
    private readonly string example10CsharpCode = @"
private const int TotalCatalogItems = 35;

private int loadedPages;
private bool reachedEnd;
private BitInfiniteScrolling<int>? progressRef;

private async ValueTask<IEnumerable<int>> LoadCatalogItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);

    var count = Math.Clamp(TotalCatalogItems - request.Skip, 0, request.Count);

    return new BitInfiniteScrollingItemsProviderResult<int>(Enumerable.Range(request.Skip, count),
                                                           hasMore: request.Skip + count < TotalCatalogItems,
                                                           totalCount: TotalCatalogItems);
}

private void HandleOnItemsLoaded(IReadOnlyList<int> items) => loadedPages++;

private void HandleOnEnd() => reachedEnd = true;";

    private readonly string example11RazorCode = @"
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
    <BitButton OnClick=""RemoveMemberItem"">RemoveItemAsync</BitButton>
    <BitButton OnClick=""SetMemberItems"">SetItemsAsync</BitButton>
    <BitButton OnClick=""ScrollMembersToTop"">ScrollToTopAsync</BitButton>
    <BitButton OnClick=""ScrollMembersToBottom"">ScrollToBottomAsync</BitButton>
    <BitButton OnClick=""ScrollMembersToOffset"">ScrollToOffsetAsync</BitButton>
    <BitButton OnClick=""ReadMembersOffset"">GetScrollOffsetAsync</BitButton>
</BitStack>

<div>
    Items: @(membersRef?.Items.Count ?? 0) &nbsp; HasMore: @(membersRef?.HasMore) &nbsp;
    IsLoading: @(membersRef?.IsLoading) &nbsp; Offset: @membersOffset
</div>";
    private readonly string example11CsharpCode = @"
private const int TotalItems = 40;

private int appendedItems;
private double membersOffset;
private BitInfiniteScrolling<int>? membersRef;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}

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

private async Task RemoveMemberItem()
{
    if (membersRef is null || membersRef.Items.Count == 0) return;
    await membersRef.RemoveItemAsync(membersRef.Items[0]);
}

private async Task SetMemberItems()
{
    if (membersRef is null) return;
    await membersRef.SetItemsAsync([101, 102, 103]);
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

private async Task ScrollMembersToOffset()
{
    if (membersRef is null) return;
    await membersRef.ScrollToOffsetAsync(150, true);
}

private async Task ReadMembersOffset()
{
    if (membersRef is null) return;
    membersOffset = await membersRef.GetScrollOffsetAsync();
}";

    private readonly string example12RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton Variant=""@(IsEvenFilter ? BitVariant.Fill : BitVariant.Outline)"" OnClick=""SelectEvenFilter"">Even</BitButton>
    <BitButton Variant=""@(IsEvenFilter ? BitVariant.Outline : BitVariant.Fill)"" OnClick=""SelectOddFilter"">Odd</BitButton>
</BitStack>

<BitInfiniteScrolling TItem=""int""
                      ItemsProvider=""@(request => LoadFilteredItems(request, filter))""
                      ResetKey=""filter""
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example12CsharpCode = @"
private const int TotalItems = 40;

private string filter = ""even"";

private bool IsEvenFilter => filter == ""even"";

private void SelectEvenFilter() => filter = ""even"";

private void SelectOddFilter() => filter = ""odd"";

private async ValueTask<IEnumerable<int>> LoadFilteredItems(BitInfiniteScrollingItemsProviderRequest request, string filter)
{
    await Task.Delay(1000);

    var source = Enumerable.Range(0, TotalItems).Where(i => filter == ""even"" ? i % 2 == 0 : i % 2 == 1);

    return source.Skip(request.Skip).Take(request.Count);
}";

    private readonly string example13RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }

    .product {
        display: flex;
        padding: 0.75rem;
        flex-direction: column;
        border-bottom: 1px solid gray;
    }
</style>

<BitInfiniteScrolling ItemsProvider=""LoadCatalogItems""
                      Feed
                      Manual
                      PageSize=""5""
                      Class=""basic""
                      Context=""item""
                      AriaLabel=""Products""
                      LoadMoreText=""Load more products""
                      LoadedMessage=""{0} more products loaded, {1} in total.""
                      EndMessage=""All products are loaded."">
    <div class=""product"">
        <b>Product @item</b>
        <span>A short description of product @item.</span>
    </div>
</BitInfiniteScrolling>";
    private readonly string example13CsharpCode = @"
private const int TotalCatalogItems = 35;

private async ValueTask<IEnumerable<int>> LoadCatalogItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);

    var count = Math.Clamp(TotalCatalogItems - request.Skip, 0, request.Count);

    return new BitInfiniteScrollingItemsProviderResult<int>(Enumerable.Range(request.Skip, count),
                                                           hasMore: request.Skip + count < TotalCatalogItems,
                                                           totalCount: TotalCatalogItems);
}";

    private readonly string example14RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitParams Parameters=""infiniteScrollingParams"">
    <BitInfiniteScrolling ItemsProvider=""LoadPagedItems"" Class=""basic"" Context=""item"">
        <div>Item @item</div>
    </BitInfiniteScrolling>

    <BitInfiniteScrolling ItemsProvider=""LoadPagedItems"" LoadMoreText=""Show me more"" Class=""basic"" Context=""item"">
        <div>Item @item</div>
    </BitInfiniteScrolling>
</BitParams>";
    private readonly string example14CsharpCode = @"
private const int TotalItems = 40;

private readonly BitInfiniteScrollingParams[] infiniteScrollingParams =
[
    new()
    {
        Manual = true,
        PageSize = 5,
        LoadMoreText = ""Load the next 5"",
        EndMessage = ""That is all of them."",
    }
];

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example15RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }

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
                      Styles=""@(new() { Spinner = ""display:none"" })""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>

<BitInfiniteScrolling ItemsProvider=""LoadPagedItems""
                      Manual
                      PageSize=""10""
                      Class=""basic""
                      Context=""item""
                      Style=""@cssVariablesStyle""
                      EndMessage=""No more items to load."">
    <div>Item @item</div>
</BitInfiniteScrolling>";
    private readonly string example15CsharpCode = @"
private const int TotalItems = 40;

private const string cssVariablesStyle = ""--bit-InfiniteScrolling-status-color:var(--bit-clr-pri);"" +
                                         ""--bit-InfiniteScrolling-spinner-color:var(--bit-clr-sec);"" +
                                         ""--bit-InfiniteScrolling-button-color:var(--bit-clr-pri-text);"" +
                                         ""--bit-InfiniteScrolling-button-hover-color:var(--bit-clr-pri-text);"" +
                                         ""--bit-InfiniteScrolling-button-background:var(--bit-clr-pri);"" +
                                         ""--bit-InfiniteScrolling-button-radius:999px;"" +
                                         ""--bit-InfiniteScrolling-button-padding:0.5rem 1.5rem"";

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";

    private readonly string example16RazorCode = @"
<style>
    .basic {
        max-height: 300px;
    }
</style>

<BitInfiniteScrolling Dir=""BitDir.Rtl""
                      ItemsProvider=""LoadPagedItems""
                      PageSize=""15""
                      Class=""basic""
                      Context=""item""
                      LoadingMessage=""در حال دریافت...""
                      EndMessage=""به انتهای لیست رسیدید."">
    <div>آیتم @item</div>
</BitInfiniteScrolling>";
    private readonly string example16CsharpCode = @"
private const int TotalItems = 40;

private async ValueTask<IEnumerable<int>> LoadPagedItems(BitInfiniteScrollingItemsProviderRequest request)
{
    await Task.Delay(1000);
    var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
    return Enumerable.Range(request.Skip, count);
}";
}
