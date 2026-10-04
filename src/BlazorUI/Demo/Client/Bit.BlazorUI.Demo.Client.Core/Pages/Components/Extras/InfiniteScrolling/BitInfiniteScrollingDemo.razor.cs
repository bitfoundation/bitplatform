namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.InfiniteScrolling;

public partial class BitInfiniteScrollingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "AutoLoadLimit",
            Type = "int?",
            DefaultValue = "null",
            Description = "The number of the pages the list loads on its own, as its end comes into view, before it switches to the Load more button of the manual mode. The count starts over with every refresh; null keeps the loading automatic for good.",
         },
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
            Description = "The message to render when the items provider throws and no ErrorTemplate is provided. With an ErrorTemplate it is what screen readers are told instead.",
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
            Name = "Feed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the list as a WAI-ARIA feed: every item is wrapped in a focusable article carrying aria-posinset / aria-setsize, Page Down / Page Up move between the articles and Ctrl+End / Ctrl+Home leave the feed. A page loaded from the built-in button moves the focus to its first article.",
         },
         new()
         {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the list out along the horizontal axis, so the pages are fetched while scrolling sideways and every scroll operation of the component works on the horizontal axis of its scroll container. The root element becomes a flex row in this mode and the sentinel element is given a width instead of a height.",
         },
         new()
         {
            Name = "ItemAriaLabel",
            Type = "Func<TItem, string?>?",
            DefaultValue = "null",
            Description = "The function that returns the accessible name of the article of each item in the Feed mode, which a screen reader announces as the focus lands on it. Without one an article is announced by its whole content.",
         },
         new()
         {
            Name = "ItemKey",
            Type = "Func<TItem, object>?",
            DefaultValue = "null",
            Description = "The function that returns a stable key for each item, which is rendered as the @key of that item. A keyed item is matched by its key instead of by its position, so a page prepended above the rendered items inserts new nodes rather than rewriting the content of every node below it.",
         },
         new()
         {
            Name = "ItemsProvider",
            Type = "BitInfiniteScrollingItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The item provider function that will be called when scrolling ends. It receives a BitInfiniteScrollingItemsProviderRequest and returns the items of that page, optionally as a BitInfiniteScrollingItemsProviderResult that also states where the data ends.",
            LinkType = LinkType.Link,
            Href = "#items-provider-request",
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
            Name = "LastElementWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the last element that triggers the loading, which is the size along the scroll axis in the horizontal mode.",
         },
         new()
         {
            Name = "LoadedMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message the live region announces to screen readers after each loaded page, formatted with the number of the items of that page ({0}) and of all the loaded items ({1}).",
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
            Description = "The text of the button that loads the next page in the manual mode. The button keeps its place and the focus (aria-disabled) while the page it asked for is loading.",
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
            Name = "ResetKey",
            Type = "object?",
            DefaultValue = "null",
            Description = "An arbitrary value that resets the component whenever it changes: the loaded items are thrown away and the first page is fetched again from the items provider. It is what tells the component that a provider written as a lambda over a changed filter now answers differently.",
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-InfiniteScrolling-status-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the loading, empty and end blocks." },
        new() { Name = "--bit-InfiniteScrolling-status-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Font size of every status block and of the button." },
        new() { Name = "--bit-InfiniteScrolling-status-padding", DefaultValue = "spacing(1)", Description = "Padding of every status block." },
        new() { Name = "--bit-InfiniteScrolling-status-gap", DefaultValue = "spacing(1)", Description = "Room between the loading spinner and its text." },
        new() { Name = "--bit-InfiniteScrolling-status-text-align", DefaultValue = "center", Description = "Alignment of every status block." },
        new() { Name = "--bit-InfiniteScrolling-error-color", DefaultValue = "var(--bit-clr-err)", Description = "Text color of the default error block." },
        new() { Name = "--bit-InfiniteScrolling-spinner-size", DefaultValue = "var(--bit-siz-icon-md)", Description = "Diameter of the default loading spinner." },
        new() { Name = "--bit-InfiniteScrolling-spinner-color", DefaultValue = "var(--bit-clr-pri)", Description = "The moving arc of the spinner." },
        new() { Name = "--bit-InfiniteScrolling-spinner-track-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "The ring the arc of the spinner travels on." },
        new() { Name = "--bit-InfiniteScrolling-button-color", DefaultValue = "var(--bit-clr-pri)", Description = "Label of the Load more / Retry button." },
        new() { Name = "--bit-InfiniteScrolling-button-hover-color", DefaultValue = "var(--bit-clr-pri-hover)", Description = "Label of the button under the pointer." },
        new() { Name = "--bit-InfiniteScrolling-button-background", DefaultValue = "transparent", Description = "Background of the button." },
        new() { Name = "--bit-InfiniteScrolling-button-hover-background", DefaultValue = "var(--bit-InfiniteScrolling-button-background)", Description = "Background of the button under the pointer." },
        new() { Name = "--bit-InfiniteScrolling-button-radius", DefaultValue = "var(--bit-shp-radius-button)", Description = "Corner radius of the button and its focus ring." },
        new() { Name = "--bit-InfiniteScrolling-button-padding", DefaultValue = "var(--bit-siz-ctrl-pad-y-sm) var(--bit-siz-ctrl-pad-x-sm)", Description = "Padding of the button." },
        new() { Name = "--bit-InfiniteScrolling-item-focus-color", DefaultValue = "var(--bit-clr-pri-focus)", Description = "Focus indicator of an article in the Feed mode." },
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
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the element that wraps each item: the article of the Feed mode, or the (display:contents) element that carries the key of a keyed item." },
                new() { Name = "LastElement", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the sentinel (last) element of the BitInfiniteScrolling that triggers the loading." },
                new() { Name = "Loading", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the loading container of the BitInfiniteScrolling." },
                new() { Name = "Spinner", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the spinner of the default loading container of the BitInfiniteScrolling." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the empty container of the BitInfiniteScrolling." },
                new() { Name = "End", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the end container of the BitInfiniteScrolling that renders after the last page." },
                new() { Name = "Error", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error container of the BitInfiniteScrolling." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the button of the BitInfiniteScrolling that loads the next page in manual mode and retries a failed load." },
            ]
        },
        new()
        {
            Id = "items-provider-result",
            Title = "BitInfiniteScrollingItemsProviderResult<T>",
            Description = "The optional result of a BitInfiniteScrollingItemsProvider, which lets a provider state explicitly whether another page still exists and how many items the data source holds in total. It is itself an IEnumerable of the items of the page, so a provider can return one wherever a plain sequence is expected.",
            Parameters =
            [
                new() { Name = "Items", Type = "IEnumerable<T>", DefaultValue = "", Description = "The items of the requested page." },
                new() { Name = "HasMore", Type = "bool?", DefaultValue = "null", Description = "Whether another page can still be fetched, or null to let the component infer it from the size of the page: a page shorter than the requested count is the last one." },
                new() { Name = "TotalCount", Type = "int?", DefaultValue = "null", Description = "The total number of the items of the data source, when the provider knows it. The component exposes the last reported value through its TotalCount member." },
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
            Name = "TotalCount",
            Type = "int?",
            DefaultValue = "null",
            Description = "The total number of the items of the data source, as it was last reported by a BitInfiniteScrollingItemsProviderResult returned from the items provider.",
            LinkType = LinkType.Link,
            Href = "#items-provider-result",
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
            Name = "RemoveItemAsync",
            Type = "Func<TItem, Task<bool>>",
            DefaultValue = "",
            Description = "Removes the first occurrence of the provided item from the loaded items, and reports whether it was found.",
         },
         new()
         {
            Name = "SetItemsAsync",
            Type = "Func<IEnumerable<TItem>, Task>",
            DefaultValue = "",
            Description = "Replaces every loaded item with the provided ones, without calling the items provider. It is the way to filter, sort, deduplicate or patch the loaded items in place.",
         },
         new()
         {
            Name = "GetScrollOffsetAsync",
            Type = "Func<Task<double>>",
            DefaultValue = "",
            Description = "Returns the current offset of the scroll container, in pixels, along the scroll axis of the component.",
         },
         new()
         {
            Name = "ScrollToOffsetAsync",
            Type = "Func<double, bool, Task>",
            DefaultValue = "",
            Description = "Scrolls the scroll container to the provided offset, in pixels, along the scroll axis of the component.",
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



    public record ChatMessage(int Id, string Text);


    private const int TotalItems = 40;
    private const int TotalMessages = 40;
    private const int TotalCatalogItems = 35;

    private int loadedPages;
    private bool reachedEnd;
    private string? lastError;
    private int appendedItems;
    private double membersOffset;
    private bool faultyLoadFailed;
    private bool faultyTemplateLoadFailed;
    private string filter = "even";
    private BitInfiniteScrolling<int>? errorTemplateRef;
    private BitInfiniteScrolling<int>? progressRef;
    private BitInfiniteScrolling<int>? membersRef;

    private bool IsEvenFilter => filter == "even";

    private readonly BitInfiniteScrollingParams[] infiniteScrollingParams =
    [
        new()
        {
            Manual = true,
            PageSize = 5,
            LoadMoreText = "Load the next 5",
            EndMessage = "That is all of them.",
        }
    ];

    private const string cssVariablesStyle = "--bit-InfiniteScrolling-status-color:var(--bit-clr-pri);" +
                                             "--bit-InfiniteScrolling-spinner-color:var(--bit-clr-sec);" +
                                             "--bit-InfiniteScrolling-button-color:var(--bit-clr-pri-text);" +
                                             "--bit-InfiniteScrolling-button-hover-color:var(--bit-clr-pri-text);" +
                                             "--bit-InfiniteScrolling-button-background:var(--bit-clr-pri);" +
                                             "--bit-InfiniteScrolling-button-hover-background:var(--bit-clr-pri-hover);" +
                                             "--bit-InfiniteScrolling-button-radius:999px;" +
                                             "--bit-InfiniteScrolling-button-padding:0.5rem 1.5rem";


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

    private async ValueTask<IEnumerable<int>> LoadFaultyTemplateItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);

        if (request.Skip == request.Count && faultyTemplateLoadFailed is false)
        {
            faultyTemplateLoadFailed = true;
            throw new InvalidOperationException("The demo provider fails once on the second page.");
        }

        var count = Math.Clamp(TotalItems - request.Skip, 0, request.Count);
        return Enumerable.Range(request.Skip, count);
    }

    private async ValueTask<IEnumerable<ChatMessage>> LoadKeyedMessages(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);

        var remaining = TotalMessages - request.Skip;
        if (remaining <= 0) return [];

        var count = Math.Min(remaining, request.Count);
        var start = remaining - count;

        return Enumerable.Range(start, count).Select(i => new ChatMessage(i, $"Message {i + 1}"));
    }

    private async ValueTask<IEnumerable<int>> LoadScrollerItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        if (request.Skip >= 200) return [];
        await Task.Delay(1000);
        return Enumerable.Range(request.Skip, 50);
    }

    private async ValueTask<IEnumerable<int>> LoadCatalogItems(BitInfiniteScrollingItemsProviderRequest request)
    {
        await Task.Delay(1000);

        var count = Math.Clamp(TotalCatalogItems - request.Skip, 0, request.Count);

        return new BitInfiniteScrollingItemsProviderResult<int>(Enumerable.Range(request.Skip, count),
                                                               hasMore: request.Skip + count < TotalCatalogItems,
                                                               totalCount: TotalCatalogItems);
    }

    private async ValueTask<IEnumerable<int>> LoadFilteredItems(BitInfiniteScrollingItemsProviderRequest request, string filter)
    {
        await Task.Delay(1000);

        var source = Enumerable.Range(0, TotalItems).Where(i => filter == "even" ? i % 2 == 0 : i % 2 == 1);

        return source.Skip(request.Skip).Take(request.Count);
    }


    private void HandleOnError(Exception exception) => lastError = exception.Message;

    // The ErrorTemplate replaces the built-in retry button, so it offers its own.
    private async Task RetryErrorTemplate()
    {
        if (errorTemplateRef is null) return;
        await errorTemplateRef.LoadMoreAsync();
    }


    private void HandleOnItemsLoaded(IReadOnlyList<int> items) => loadedPages++;

    private void HandleOnEnd() => reachedEnd = true;


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
    }


    private void SelectEvenFilter() => filter = "even";

    private void SelectOddFilter() => filter = "odd";
}
