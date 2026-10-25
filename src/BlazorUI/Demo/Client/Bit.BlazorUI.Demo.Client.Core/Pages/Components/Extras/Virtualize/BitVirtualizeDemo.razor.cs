namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Virtualize;

public partial class BitVirtualizeDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "AlignToEnd",
            Type = "bool",
            DefaultValue = "false",
            Description = "Pushes the items to the end (bottom, or the right in horizontal mode) of the viewport while they are too few to fill it, the way a chat conversation starts at the bottom. Pairs naturally with Reversed.",
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
            Type = "BitVirtualizeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitVirtualize.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
         },
         new()
         {
            Name = "Dynamic",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables dynamic item sizing in which each rendered item gets measured in the browser and its real size gets cached, using the EstimatedItemSize for the items that have not been measured yet.",
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
            Name = "EstimatedItemSize",
            Type = "float",
            DefaultValue = "50",
            Description = "The assumed size in pixels of the items that have not been measured yet in dynamic mode.",
         },
         new()
         {
            Name = "Gap",
            Type = "float",
            DefaultValue = "0",
            Description = "The space in pixels between consecutive items along the scroll axis, and between the lanes of a grid.",
         },
         new()
         {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render after the last item, inside the scroll container (for example, a loading indicator at the end of an infinite list).",
         },
         new()
         {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render before the first item, inside the scroll container.",
         },
         new()
         {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the items horizontally so the viewport scrolls along the x-axis.",
         },
         new()
         {
            Name = "IndexedItemTemplate",
            Type = "RenderFragment<BitVirtualizeItemContext<TItem>>?",
            DefaultValue = "null",
            Description = "The custom template to render each item, which also receives the index of the item in the whole list (for example, for numbering or striping the rows). Takes precedence over ItemTemplate and ChildContent.",
            LinkType = LinkType.Link,
            Href = "#item-context",
         },
         new()
         {
            Name = "InitialIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The index of the item to scroll to on the first render. Ignored when Reversed is set.",
         },
         new()
         {
            Name = "IsStickyItem",
            Type = "Func<TItem, bool>?",
            DefaultValue = "null",
            Description = "A predicate that marks certain items (for example, group headers) as sticky. The active sticky item gets pinned to the leading edge of the viewport while its group scrolls. Fully supported with in-memory Items; in provider mode it is applied on a best-effort basis to the currently loaded window. A change in the state the predicate reads, rather than in the predicate itself, gets applied by RefreshDataAsync.",
         },
         new()
         {
            Name = "Items",
            Type = "ICollection<TItem>?",
            DefaultValue = "null",
            Description = "The in-memory collection of items to virtualize. Mutually exclusive with ItemsProvider.",
         },
         new()
         {
            Name = "ItemAttributes",
            Type = "Func<TItem, IReadOnlyDictionary<string, object>?>?",
            DefaultValue = "null",
            Description = "A function that returns extra HTML attributes for the element of an item, which is the one that takes the keyboard focus: an aria-selected, aria-labelledby or aria-describedby for assistive technologies, a class or a style for the whole slot of the item. A role or an aria attribute it returns overrides the default one, while a class or a style is appended to the ones of the component.",
         },
         new()
         {
            Name = "ItemKey",
            Type = "Func<TItem, object>?",
            DefaultValue = "null",
            Description = "A function that returns a stable and unique identity key for an item. When provided, rendered rows are keyed by identity (instead of by index) so per-item DOM/component state survives insertions, removals and reordering, dynamic measurements follow their item across those mutations, and the item in view stays in place when items get inserted or removed before it.",
         },
         new()
         {
            Name = "ItemRole",
            Type = "string?",
            DefaultValue = "listitem",
            Description = "The ARIA role of each item element.",
         },
         new()
         {
            Name = "ItemSize",
            Type = "float",
            DefaultValue = "50",
            Description = "The size in pixels of each item (each row of items with Lanes) along the scroll axis when the Dynamic mode is off.",
         },
         new()
         {
            Name = "ItemsProvider",
            Type = "BitVirtualizeItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The item provider function that lazily supplies windows of items on demand. Mutually exclusive with Items.",
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
            Name = "Lanes",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of items laid side by side across the scroll axis, which turns the list into a virtualized grid: the items fill each row (each column in horizontal mode) from the start, every one as wide as an equal share of the row, and the arrow keys move across the lanes as well as along them. In dynamic mode a row is as tall as its tallest item.",
         },
         new()
         {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render until the component has performed its first load.",
         },
         new()
         {
            Name = "MinLaneSize",
            Type = "float?",
            DefaultValue = "null",
            Description = "The smallest size in pixels of a lane across the scroll axis, which makes the grid responsive: the list has as many lanes as fit in its width (its height in horizontal mode), at least one. Takes precedence over Lanes once the list has been measured.",
         },
         new()
         {
            Name = "OnAtEndChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback to be called when the viewport arrives at the end of the list (true) or leaves it (false), for example to show a \"Jump to latest\" button only while the newest items are out of view. Also called with the initial state once the list has taken its initial position. The end is the one ScrollToEndAsync goes to, the FooterTemplate included; an emptied list is at both edges.",
         },
         new()
         {
            Name = "OnAtStartChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback to be called when the viewport arrives at the start of the list (true) or leaves it (false), for example to show a \"Back to top\" button only once the user has scrolled away. Also called with the initial state once the list has taken its initial position. The start is the one ScrollToStartAsync goes to, the HeaderTemplate included; an emptied list is at both edges.",
         },
         new()
         {
            Name = "OnEndReached",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback to be called when the last item comes within ReachedThreshold items of the visible window, useful for appending more data in infinite scrolling scenarios. Fires once per item-count value.",
         },
         new()
         {
            Name = "OnStartReached",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback to be called when the first item comes within ReachedThreshold items of the visible window, useful for prepending older data (for example, loading chat history when scrolling up). Fires again when items get prepended while the start is still within reach.",
         },
         new()
         {
            Name = "OnVisibleRangeChanged",
            Type = "EventCallback<(int Start, int End)>",
            DefaultValue = "",
            Description = "The callback to be called whenever the visible index range changes.",
         },
         new()
         {
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "The number of extra items to render on each side of the visible window for smoother scrolling.",
         },
         new()
         {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment<BitVirtualizePlaceholderContext>?",
            DefaultValue = "null",
            Description = "The custom template to render an item whose data has not been loaded yet in provider mode, the first window too: before the provider tells the count of the items (and so in a prerendered or statically rendered page) the window it is first asked for is rendered as placeholders, unless a LoadingTemplate is provided.",
            LinkType = LinkType.Link,
            Href = "#placeholder-context",
         },
         new()
         {
            Name = "ReachedThreshold",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of items away from an edge the visible window must be before OnEndReached/OnStartReached fire.",
         },
         new()
         {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the bottom-anchored mode in which the list starts scrolled to the end and automatically keeps the newest items in view when data gets appended while the user is at the bottom. Ideal for chat and log views.",
         },
         new()
         {
            Name = "Role",
            Type = "string?",
            DefaultValue = "list",
            Description = "The ARIA role of the root element, which is a group instead while the loading or the empty content takes the place of the items.",
         },
         new()
         {
            Name = "ScrollerSelector",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of an ancestor that scrolls the list instead of the list itself, which then grows to the full size of its items and only virtualizes them; the \"window\", \"document\", \"body\" and \"html\" values select the page itself. The list then has no height of its own to give.",
         },
         new()
         {
            Name = "StickyTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render the pinned sticky item. Falls back to the item template when not provided.",
         },
         new()
         {
            Name = "Styles",
            Type = "BitVirtualizeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitVirtualize.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
         },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-Virtualize-focus-color", DefaultValue = "var(--bit-clr-pri-focus)", Description = "Focus indicator of the list and of its items." },
        new() { Name = "--bit-Virtualize-sticky-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Background of the pinned sticky item, which hides the items scrolling under it." },
        new() { Name = "--bit-Virtualize-sticky-shadow", DefaultValue = "none", Description = "Shadow under the pinned sticky item." },
        new() { Name = "--bit-Virtualize-state-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the loading and empty content." },
        new() { Name = "--bit-Virtualize-state-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Font size of the loading and empty content." },
        new() { Name = "--bit-Virtualize-state-padding", DefaultValue = "spacing(2)", Description = "Padding of the loading and empty content." },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
         new()
         {
            Name = "RefreshDataAsync",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Re-requests the data from the ItemsProvider (or re-reads the Items) and refreshes the view.",
         },
         new()
         {
            Name = "ScrollToIndexAsync",
            Type = "Func<int, BitScrollAlignment, bool, Task>",
            DefaultValue = "",
            Description = "Scrolls the viewport so that the item at the provided index becomes visible. A call made before the component is ready (for example, before its data arrives) gets applied once it is.",
            LinkType = LinkType.Link,
            Href = "#scroll-alignment-enum",
         },
         new()
         {
            Name = "ScrollToOffsetAsync",
            Type = "Func<double, bool, Task>",
            DefaultValue = "",
            Description = "Scrolls to an absolute pixel offset along the scroll axis, measured from the start of the first item.",
         },
         new()
         {
            Name = "ScrollByAsync",
            Type = "Func<double, bool, Task>",
            DefaultValue = "",
            Description = "Scrolls the viewport by the provided number of pixels along the scroll axis (negative values scroll back).",
         },
         new()
         {
            Name = "ScrollToStartAsync",
            Type = "Func<bool, Task>",
            DefaultValue = "",
            Description = "Scrolls to the very start (top/left) of the list, including the HeaderTemplate.",
         },
         new()
         {
            Name = "ScrollToEndAsync",
            Type = "Func<bool, Task>",
            DefaultValue = "",
            Description = "Scrolls to the very end (bottom/right) of the list, including the FooterTemplate. Useful for chat and log views.",
         },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "items-provider-request",
            Title = "BitVirtualizeItemsProviderRequest",
            Description = "The request passed to the ItemsProvider function for a window of items.",
            Parameters =
            [
                new()
                {
                    Name = "StartIndex",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The zero-based index of the first item requested.",
                },
                new()
                {
                    Name = "Count",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The maximum number of items requested.",
                },
                new()
                {
                    Name = "CancellationToken",
                    Type = "CancellationToken",
                    DefaultValue = "",
                    Description = "A token that is cancelled when this request is no longer needed.",
                },
            ]
        },
        new()
        {
            Id = "items-provider-result",
            Title = "BitVirtualizeItemsProviderResult<TItem>",
            Description = "The result returned from the ItemsProvider function.",
            Parameters =
            [
                new()
                {
                    Name = "Items",
                    Type = "IReadOnlyList<TItem>",
                    DefaultValue = "",
                    Description = "The items that were loaded for the requested window.",
                },
                new()
                {
                    Name = "TotalItemCount",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The total number of items in the underlying data source.",
                },
            ]
        },
        new()
        {
            Id = "item-context",
            Title = "BitVirtualizeItemContext<TItem>",
            Description = "The context passed to the IndexedItemTemplate: an item along with its position in the whole list.",
            Parameters =
            [
                new()
                {
                    Name = "Item",
                    Type = "TItem",
                    DefaultValue = "",
                    Description = "The item to render.",
                },
                new()
                {
                    Name = "Index",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The zero-based index of the item in the whole list (not in the rendered window).",
                },
            ]
        },
        new()
        {
            Id = "placeholder-context",
            Title = "BitVirtualizePlaceholderContext",
            Description = "The context passed to the PlaceholderTemplate while real items are being loaded.",
            Parameters =
            [
                new()
                {
                    Name = "Index",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The zero-based index of the item this placeholder represents.",
                },
                new()
                {
                    Name = "Size",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The estimated size (px) reserved for the placeholder along the scroll axis.",
                },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitVirtualizeClassStyles",
            Description = "Custom CSS classes/styles for the different parts of the BitVirtualize.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root (scroll container) element of the BitVirtualize.",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header container of the BitVirtualize.",
                },
                new()
                {
                    Name = "Item",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper element of each rendered item (and placeholder) of the BitVirtualize.",
                },
                new()
                {
                    Name = "Sticky",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the pinned sticky item container of the BitVirtualize.",
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer container of the BitVirtualize.",
                },
                new()
                {
                    Name = "Loading",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the loading container of the BitVirtualize.",
                },
                new()
                {
                    Name = "Empty",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the empty container of the BitVirtualize.",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "scroll-alignment-enum",
            Name = "BitScrollAlignment",
            Description = "Determines where a target item is positioned within the viewport when scrolling to it.",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                    Description = "The element is brought to the start of the pane: its top edge to the top of the pane, and its leading edge to the leading edge of the pane."
                },
                new()
                {
                    Name = "Center",
                    Value = "1",
                    Description = "The element is centered in the pane along both axes."
                },
                new()
                {
                    Name = "End",
                    Value = "2",
                    Description = "The element is brought to the end of the pane: its bottom edge to the bottom of the pane, and its trailing edge to the trailing edge of the pane."
                },
                new()
                {
                    Name = "Nearest",
                    Value = "3",
                    Description = "The pane moves as little as it can: an element that is already fully in view is not moved to at all, and one that is not is brought to whichever edge it is nearest."
                }
            ]
        },
    ];



    private readonly int[] basicItems = Enumerable.Range(0, 1_000_000).ToArray();


    private const int TotalProducts = 100_000;
    private async ValueTask<BitVirtualizeItemsProviderResult<Product>> LoadProducts(BitVirtualizeItemsProviderRequest request)
    {
        await Task.Delay(500, request.CancellationToken); // simulate a network fetch

        var items = Enumerable.Range(request.StartIndex, Math.Min(request.Count, TotalProducts - request.StartIndex))
                              .Select(i => new Product(i, $"Product {i:N0}", DateTimeOffset.Now.ToString("HH:mm:ss")))
                              .ToList();

        return new(items, TotalProducts);
    }


    private List<Post> posts = [];
    private void InitPosts()
    {
        var random = new Random(42);
        var words = "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua".Split(' ');

        posts = Enumerable.Range(0, 10_000).Select(i =>
        {
            var body = string.Join(' ', Enumerable.Range(0, random.Next(5, 60)).Select(_ => words[random.Next(words.Length)]));
            return new Post($"Author {i % 20}", $"{random.Next(1, 59)}m ago", body);
        }).ToList();
    }


    private readonly int[] horizontalItems = Enumerable.Range(0, 100_000).ToArray();


    private readonly int[] gridItems = Enumerable.Range(0, 100_000).ToArray();
    private int gridLanes = 4;
    private bool gridResponsive;


    private BitVirtualize<int> scrollRef = default!;
    private readonly int[] scrollItems = Enumerable.Range(0, 100_000).ToArray();
    private int scrollTargetIndex = 5_000;
    private bool scrollSmooth = true;
    private (int Start, int End) visibleRange;
    private bool scrollAtStart;
    private bool scrollAtEnd;

    private async Task ScrollToTarget(BitScrollAlignment alignment)
    {
        await scrollRef.ScrollToIndexAsync(scrollTargetIndex, alignment, scrollSmooth);
    }


    private readonly int[] pageItems = Enumerable.Range(0, 10_000).ToArray();


    private readonly Article[] articles = Enumerable.Range(1, 10_000)
                                                    .Select(i => new Article(i, $"Headline number {i:N0}", $"A short summary of the story number {i:N0}."))
                                                    .ToArray();

    private static Dictionary<string, object> ArticleAttributes(Article article) => new()
    {
        ["aria-labelledby"] = $"article-title-{article.Id}",
        ["aria-describedby"] = $"article-summary-{article.Id}",
    };


    private List<int> templateItems = [.. Enumerable.Range(0, 1_000)];


    private BitVirtualize<string> feedRef = default!;
    private readonly List<string> feedItems = [.. Enumerable.Range(0, 25).Select(i => $"Feed item {i:N0}")];
    private bool feedLoading;

    private async Task LoadMoreFeedItems()
    {
        if (feedLoading) return;
        feedLoading = true;
        StateHasChanged();

        await Task.Delay(700); // simulate a network fetch

        feedItems.AddRange(Enumerable.Range(feedItems.Count, 25).Select(i => $"Feed item {i:N0}"));
        feedLoading = false;

        await feedRef.RefreshDataAsync();
    }


    private List<Contact> contacts = [];
    private void InitContacts()
    {
        var random = new Random(11);
        var firstNames = "Alice Bruno Chloe Daniel Emma Felix Grace Hugo Isla Jack Kira Liam Maya Noah Olivia Paul Quinn Ruby Sam Tara Umar Vera Will Xena Yusuf Zoe".Split(' ');
        var lastNames = "Adams Baker Clark Davis Evans Foster Green Hall Irwin Jones King Lewis Moore Nash Owen Price Reed Scott Turner".Split(' ');

        contacts = [];
        for (var c = 'A'; c <= 'Z'; c++)
        {
            contacts.Add(new Contact(true, c.ToString(), string.Empty));
            var count = random.Next(5, lastNames.Length + 1);
            for (var i = 0; i < count; i++)
            {
                var name = $"{firstNames[c - 'A']} {lastNames[i]}";
                contacts.Add(new Contact(false, name, $"{name.Replace(' ', '.').ToLower()}@example.com"));
            }
        }
    }


    private List<TaskItem> tasks = [.. Enumerable.Range(1, 1_000).Select(i => new TaskItem(i, $"Task {i}"))];
    private int nextTaskId = 1_001;

    private void AddTasks()
    {
        tasks = [.. Enumerable.Range(0, 5).Select(_ => new TaskItem(nextTaskId, $"New task {nextTaskId++}")), .. tasks];
    }

    private void RemoveTasks()
    {
        tasks = [.. tasks.Skip(5)];
    }


    private BitVirtualize<Message> chatRef = default!;
    private List<Message> messages = [];
    private string? draftMessage;
    private bool loadingChatHistory;
    private bool chatAtEnd = true;
    private int chatHistoryRemaining = 381; // count of the older messages (0..380) not loaded yet

    private void InitMessages()
    {
        // The newest messages (381..400); scrolling up loads the older ones down to 0.
        messages = Enumerable.Range(381, 20).Select(i => new Message(i, i % 3 == 0, $"Message number {i}")).ToList();
    }

    private async Task SendChatMessage()
    {
        if (string.IsNullOrWhiteSpace(draftMessage)) return;

        var id = messages.Count == 0 ? 1000 : Math.Max(1000, messages.Max(m => m.Id) + 1);
        messages.Add(new Message(id, true, draftMessage.Trim()));
        draftMessage = string.Empty;

        await chatRef.RefreshDataAsync(); // Reversed mode keeps the list pinned to the bottom
    }

    private async Task LoadChatHistory()
    {
        if (loadingChatHistory || chatHistoryRemaining <= 0) return;
        loadingChatHistory = true;

        await Task.Delay(600); // simulate fetching older messages

        var batch = Math.Min(15, chatHistoryRemaining);
        messages.InsertRange(0, Enumerable.Range(chatHistoryRemaining - batch, batch).Select(i => new Message(i, i % 3 == 0, $"Message number {i}")));
        chatHistoryRemaining -= batch;
        loadingChatHistory = false;

        await chatRef.RefreshDataAsync(); // the message in view stays where it was
    }

    private void NewConversation()
    {
        chatHistoryRemaining = 0;
        messages = [new Message(1000, false, "Hi! How can I help you?")];
    }


    private readonly BitVirtualizeParams[] virtualizeParams =
    [
        new()
        {
            ItemSize = 40,
            OverscanCount = 5,
            Classes = new() { Root = "cascaded-list", Item = "cascaded-item" },
        }
    ];


    private readonly int[] styleItems = Enumerable.Range(0, 1_000).ToArray();

    private const string cssVariablesStyle = "--bit-Virtualize-focus-color:crimson;" +
                                             "--bit-Virtualize-sticky-background:var(--bit-clr-bg-sec);" +
                                             "--bit-Virtualize-sticky-shadow:0 2px 6px rgba(0, 0, 0, 0.2)";


    protected override void OnInitialized()
    {
        InitPosts();
        InitContacts();
        InitMessages();
    }


    public record Product(int Id, string Name, string LoadedAt);
    public record Post(string Author, string Time, string Body);
    public record Article(int Id, string Title, string Summary);
    public record Contact(bool IsHeader, string Name, string Email);
    public record TaskItem(int Id, string Title);
    public record Message(int Id, bool Mine, string Text);
}
