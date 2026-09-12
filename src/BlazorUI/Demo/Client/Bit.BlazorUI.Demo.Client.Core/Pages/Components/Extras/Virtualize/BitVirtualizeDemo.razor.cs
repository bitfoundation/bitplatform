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
            Description = "A predicate that marks certain items (for example, group headers) as sticky. The active sticky item gets pinned to the leading edge of the viewport while its group scrolls. Fully supported with in-memory Items; in provider mode it is applied on a best-effort basis to the currently loaded window.",
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
            Description = "The size in pixels of each item along the scroll axis when the Dynamic mode is off.",
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
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render until the component has performed its first load.",
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
            Description = "The callback to be called when the first item comes within ReachedThreshold items of the visible window, useful for prepending older data (for example, loading chat history when scrolling up). Fires once per item-count value.",
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
            Description = "The custom template to render an item whose data has not been loaded yet in provider mode.",
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
            Description = "The ARIA role of the root element.",
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
            Type = "Func<int, BitVirtualizeScrollAlignment, bool, Task>",
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
            Name = "BitVirtualizeScrollAlignment",
            Description = "Determines where a target item is positioned within the viewport when scrolling to it.",
            Items =
            [
                new()
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "Scroll the minimum amount required to bring the item fully into view.",
                },
                new()
                {
                    Name = "Start",
                    Value = "1",
                    Description = "Align the item to the start (top/left) of the viewport.",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                    Description = "Center the item within the viewport.",
                },
                new()
                {
                    Name = "End",
                    Value = "3",
                    Description = "Align the item to the end (bottom/right) of the viewport.",
                },
            ]
        },
    ];



    private readonly int[] basicItems = Enumerable.Range(0, 1_000_000).ToArray();


    private const int TotalProducts = 100_000;
    private async ValueTask<BitVirtualizeItemsProviderResult<Product>> LoadProducts(BitVirtualizeItemsProviderRequest request)
    {
        await Task.Delay(500, request.CancellationToken); // simulate a network fetch

        var items = Enumerable.Range(request.StartIndex, Math.Min(request.Count, TotalProducts - request.StartIndex))
                              .Select(i => new Product(i, $"Product {i:N0}", DateTime.Now.ToString("HH:mm:ss")))
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


    private BitVirtualize<int> scrollRef = default!;
    private readonly int[] scrollItems = Enumerable.Range(0, 100_000).ToArray();
    private int scrollTargetIndex = 5_000;
    private bool scrollSmooth = true;
    private (int Start, int End) visibleRange;

    private async Task ScrollToTarget(BitVirtualizeScrollAlignment alignment)
    {
        await scrollRef.ScrollToIndexAsync(scrollTargetIndex, alignment, scrollSmooth);
    }


    private readonly Article[] articles = Enumerable.Range(1, 10_000)
                                                    .Select(i => new Article($"Headline number {i:N0}", $"A short summary of the story number {i:N0}."))
                                                    .ToArray();


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
        contacts = [];
        for (var c = 'A'; c <= 'Z'; c++)
        {
            contacts.Add(new Contact(true, c.ToString(), string.Empty));
            for (var i = 0; i < random.Next(5, 20); i++)
            {
                var name = $"{c}ontact {i + 1}";
                contacts.Add(new Contact(false, name, $"{name.Replace(" ", ".").ToLower()}@example.com"));
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


    private readonly int[] styleItems = Enumerable.Range(0, 1_000).ToArray();


    protected override void OnInitialized()
    {
        InitPosts();
        InitContacts();
        InitMessages();
    }


    public record Product(int Id, string Name, string LoadedAt);
    public record Post(string Author, string Time, string Body);
    public record Article(string Title, string Summary);
    public record Contact(bool IsHeader, string Name, string Email);
    public record TaskItem(int Id, string Title);
    public record Message(int Id, bool Mine, string Text);



    private readonly string example1RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .basic-item {
        gap: 0.5rem;
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }
</style>

<BitVirtualize Items=""basicItems"" ItemSize=""56""
               TItem=""int"" Context=""item""
               Class=""list"">
    <div class=""basic-item"">
        <b>#@item.ToString(""N0"")</b>
        <span>Item number @item of one million</span>
    </div>
</BitVirtualize>";
    private readonly string example1CsharpCode = @"
private readonly int[] basicItems = Enumerable.Range(0, 1_000_000).ToArray();";

    private readonly string example2RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .provider-item {
        display: flex;
        height: 100%;
        padding: 0 1rem;
        flex-direction: column;
        justify-content: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }
</style>

<BitVirtualize TItem=""Product"" ItemsProvider=""LoadProducts"" ItemSize=""60"" Class=""list"">
    <ItemTemplate Context=""product"">
        <div class=""provider-item"">
            <b>@product.Name</b>
            <span>record #@product.Id.ToString(""N0"") · loaded at @product.LoadedAt</span>
        </div>
    </ItemTemplate>
    <PlaceholderTemplate Context=""context"">
        <div class=""provider-item"">
            <BitShimmer Height=""@($""{context.Size / 2}px"")"" Width=""@($""{100 - (context.Index % 3) * 15}%"")"" />
        </div>
    </PlaceholderTemplate>
    <LoadingTemplate>
        <BitSpinnerLoading />
    </LoadingTemplate>
</BitVirtualize>";
    private readonly string example2CsharpCode = @"
private const int TotalProducts = 100_000;

private async ValueTask<BitVirtualizeItemsProviderResult<Product>> LoadProducts(BitVirtualizeItemsProviderRequest request)
{
    await Task.Delay(500, request.CancellationToken); // simulate a network fetch

    var items = Enumerable.Range(request.StartIndex, Math.Min(request.Count, TotalProducts - request.StartIndex))
                          .Select(i => new Product(i, $""Product {i:N0}"", DateTime.Now.ToString(""HH:mm:ss"")))
                          .ToList();

    return new(items, TotalProducts);
}

public record Product(int Id, string Name, string LoadedAt);";

    private readonly string example3RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .post {
        padding: 1rem;
        border-bottom: 1px solid lightgray;
    }

    .post-header {
        color: gray;
        font-size: 0.75rem;
        margin-bottom: 0.25rem;
    }
</style>

<BitVirtualize Items=""posts"" Dynamic EstimatedItemSize=""96""
               TItem=""Post"" Context=""post""
               Class=""list"">
    <div class=""post"">
        <div class=""post-header"">@post.Author · @post.Time</div>
        <div>@post.Body</div>
    </div>
</BitVirtualize>";
    private readonly string example3CsharpCode = @"
private List<Post> posts = [];

protected override void OnInitialized()
{
    var random = new Random(42);
    var words = ""lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua"".Split(' ');

    posts = Enumerable.Range(0, 10_000).Select(i =>
    {
        var body = string.Join(' ', Enumerable.Range(0, random.Next(5, 60)).Select(_ => words[random.Next(words.Length)]));
        return new Post($""Author {i % 20}"", $""{random.Next(1, 59)}m ago"", body);
    }).ToList();
}

public record Post(string Author, string Time, string Body);";

    private readonly string example4RazorCode = @"
<style>
    .horizontal-list {
        height: 9rem;
        border: 1px solid gray;
    }

    .tile {
        display: flex;
        margin: 0.5rem;
        border-radius: 0.5rem;
        align-items: center;
        justify-content: center;
        box-sizing: border-box;
        height: calc(100% - 1rem);
        border: 1px solid lightgray;
    }
</style>

<BitVirtualize Items=""horizontalItems"" ItemSize=""120"" Horizontal
               TItem=""int"" Context=""item""
               Class=""horizontal-list"">
    <div class=""tile"">
        <b>@item.ToString(""N0"")</b>
    </div>
</BitVirtualize>";
    private readonly string example4CsharpCode = @"
private readonly int[] horizontalItems = Enumerable.Range(0, 100_000).ToArray();";

    private readonly string example5RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .basic-item {
        gap: 0.5rem;
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }

    .basic-item.target {
        color: white;
        background-color: dodgerblue;
    }

    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }
</style>

<div class=""toolbar"">
    <BitNumberField @bind-Value=""scrollTargetIndex"" Min=""0"" Max=""99999"" Style=""max-width:9rem"" />
    <BitButton OnClick=""() => ScrollToTarget(BitVirtualizeScrollAlignment.Start)"">Start</BitButton>
    <BitButton OnClick=""() => ScrollToTarget(BitVirtualizeScrollAlignment.Center)"">Center</BitButton>
    <BitButton OnClick=""() => ScrollToTarget(BitVirtualizeScrollAlignment.End)"">End</BitButton>
    <BitButton OnClick=""() => ScrollToTarget(BitVirtualizeScrollAlignment.Auto)"">Auto</BitButton>
    <BitToggle @bind-Value=""scrollSmooth"" Label=""Smooth"" Inline />
</div>

<div class=""toolbar"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => scrollRef.ScrollToStartAsync(scrollSmooth)"">To start</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => scrollRef.ScrollByAsync(-500, scrollSmooth)"">-500px</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => scrollRef.ScrollByAsync(500, scrollSmooth)"">+500px</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => scrollRef.ScrollToEndAsync(scrollSmooth)"">To end</BitButton>
    <BitTag Text=""@($""[{visibleRange.Start:N0}, {visibleRange.End:N0}) visible"")"" />
</div>

<BitVirtualize @ref=""scrollRef"" Items=""scrollItems"" ItemSize=""48""
               TItem=""int"" Context=""item""
               InitialIndex=""500""
               OnVisibleRangeChanged=""range => visibleRange = range""
               Class=""list"">
    <div class=""basic-item @(item == scrollTargetIndex ? ""target"" : null)"">
        <b>#@item.ToString(""N0"")</b>
    </div>
</BitVirtualize>";
    private readonly string example5CsharpCode = @"
private BitVirtualize<int> scrollRef = default!;
private readonly int[] scrollItems = Enumerable.Range(0, 100_000).ToArray();
private int scrollTargetIndex = 5_000;
private bool scrollSmooth = true;
private (int Start, int End) visibleRange;

private async Task ScrollToTarget(BitVirtualizeScrollAlignment alignment)
{
    await scrollRef.ScrollToIndexAsync(scrollTargetIndex, alignment, scrollSmooth);
}";

    private readonly string example6RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .news-item {
        display: flex;
        height: 100%;
        padding: 0 1rem;
        flex-direction: column;
        justify-content: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }

    .news-list .bit-vir-itm:focus-visible {
        outline-offset: -2px;
        outline: 2px solid dodgerblue;
    }
</style>

<BitVirtualize Items=""articles"" ItemSize=""72""
               TItem=""Article"" Context=""article""
               Role=""feed"" ItemRole=""article"" AriaLabel=""News feed""
               Class=""list news-list"">
    <div class=""news-item"">
        <b>@article.Title</b>
        <span>@article.Summary</span>
    </div>
</BitVirtualize>";
    private readonly string example6CsharpCode = @"
private readonly Article[] articles = Enumerable.Range(1, 10_000)
                                                .Select(i => new Article($""Headline number {i:N0}"", $""A short summary of the story number {i:N0}.""))
                                                .ToArray();

public record Article(string Title, string Summary);";

    private readonly string example7RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .basic-item {
        gap: 0.5rem;
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }

    .list-header,
    .list-footer {
        padding: 0.75rem 1rem;
        background-color: #f4f4f4;
    }

    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }
</style>

<div class=""toolbar"">
    <BitButton OnClick=""() => templateItems = []"">Empty the list</BitButton>
    <BitButton OnClick=""() => templateItems = [.. Enumerable.Range(0, 1_000)]"">Refill the list</BitButton>
</div>

<BitVirtualize Items=""templateItems"" ItemSize=""48""
               TItem=""int"" Context=""item""
               Class=""list"">
    <HeaderTemplate>
        <div class=""list-header"">Header · @templateItems.Count.ToString(""N0"") items</div>
    </HeaderTemplate>
    <ItemTemplate>
        <div class=""basic-item"">Item @item</div>
    </ItemTemplate>
    <FooterTemplate>
        <div class=""list-footer"">Footer · end of the list</div>
    </FooterTemplate>
    <EmptyTemplate>
        <b>No items to show</b>
    </EmptyTemplate>
</BitVirtualize>";
    private readonly string example7CsharpCode = @"
private List<int> templateItems = [.. Enumerable.Range(0, 1_000)];";

    private readonly string example8RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .basic-item {
        gap: 0.5rem;
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }

    .list-footer {
        padding: 0.75rem 1rem;
        background-color: #f4f4f4;
    }
</style>

<BitVirtualize @ref=""feedRef"" Items=""feedItems"" ItemSize=""56""
               TItem=""string""
               OnEndReached=""LoadMoreFeedItems"" ReachedThreshold=""6""
               Class=""list"">
    <ItemTemplate Context=""item"">
        <div class=""basic-item"">@item</div>
    </ItemTemplate>
    <FooterTemplate>
        <div class=""list-footer"">@(feedLoading ? ""Loading more..."" : $""{feedItems.Count:N0} items loaded"")</div>
    </FooterTemplate>
</BitVirtualize>";
    private readonly string example8CsharpCode = @"
private BitVirtualize<string> feedRef = default!;
private readonly List<string> feedItems = [.. Enumerable.Range(0, 25).Select(i => $""Feed item {i:N0}"")];
private bool feedLoading;

private async Task LoadMoreFeedItems()
{
    if (feedLoading) return;
    feedLoading = true;
    StateHasChanged();

    await Task.Delay(700); // simulate a network fetch

    feedItems.AddRange(Enumerable.Range(feedItems.Count, 25).Select(i => $""Feed item {i:N0}""));
    feedLoading = false;

    await feedRef.RefreshDataAsync();
}";

    private readonly string example9RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .group-header {
        display: flex;
        height: 100%;
        font-weight: bold;
        padding: 0.5rem 1rem;
        align-items: center;
        box-sizing: border-box;
        background-color: #f4f4f4;
    }

    .group-header.pinned {
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
    }

    .contact {
        display: flex;
        height: 100%;
        padding: 0.5rem 1rem;
        flex-direction: column;
        justify-content: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }
</style>

<BitVirtualize Items=""contacts"" Dynamic EstimatedItemSize=""56""
               TItem=""Contact""
               IsStickyItem=""c => c.IsHeader""
               Class=""list"">
    <ItemTemplate Context=""contact"">
        @if (contact.IsHeader)
        {
            <div class=""group-header"">@contact.Name</div>
        }
        else
        {
            <div class=""contact"">
                <b>@contact.Name</b>
                <span>@contact.Email</span>
            </div>
        }
    </ItemTemplate>
    <StickyTemplate Context=""contact"">
        <div class=""group-header pinned"">@contact.Name</div>
    </StickyTemplate>
</BitVirtualize>";
    private readonly string example9CsharpCode = @"
private List<Contact> contacts = [];

protected override void OnInitialized()
{
    var random = new Random(11);
    contacts = [];
    for (var c = 'A'; c <= 'Z'; c++)
    {
        contacts.Add(new Contact(true, c.ToString(), string.Empty));
        for (var i = 0; i < random.Next(5, 20); i++)
        {
            var name = $""{c}ontact {i + 1}"";
            contacts.Add(new Contact(false, name, $""{name.Replace("" "", ""."").ToLower()}@example.com""));
        }
    }
}

public record Contact(bool IsHeader, string Name, string Email);";

    private readonly string example10RazorCode = @"
<style>
    .list {
        height: 25rem;
        border: 1px solid gray;
    }

    .basic-item {
        gap: 0.5rem;
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }

    .toolbar {
        gap: 0.5rem;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
    }
</style>

<div class=""toolbar"">
    <BitButton OnClick=""AddTasks"">Add 5 at the top</BitButton>
    <BitButton OnClick=""RemoveTasks"" IsEnabled=""@(tasks.Count > 0)"">Remove the first 5</BitButton>
</div>

<BitVirtualize Items=""tasks"" ItemSize=""48"" ItemKey=""t => t.Id""
               TItem=""TaskItem"" Context=""task""
               Class=""list"">
    <div class=""basic-item"">
        <BitCheckbox Label=""@task.Title"" />
    </div>
</BitVirtualize>";
    private readonly string example10CsharpCode = @"
private List<TaskItem> tasks = [.. Enumerable.Range(1, 1_000).Select(i => new TaskItem(i, $""Task {i}""))];
private int nextTaskId = 1_001;

private void AddTasks()
{
    tasks = [.. Enumerable.Range(0, 5).Select(_ => new TaskItem(nextTaskId, $""New task {nextTaskId++}"")), .. tasks];
}

private void RemoveTasks()
{
    tasks = [.. tasks.Skip(5)];
}

public record TaskItem(int Id, string Title);";

    private readonly string example11RazorCode = @"
<style>
    .chat-list {
        height: 25rem;
        border: 1px solid gray;
    }

    .list-header {
        padding: 0.75rem 1rem;
        text-align: center;
        background-color: #f4f4f4;
    }

    .message {
        display: flex;
        padding: 0.25rem 1rem;
        box-sizing: border-box;
    }

    .message.mine {
        justify-content: flex-end;
    }

    .bubble {
        max-width: 70%;
        padding: 0.5rem 1rem;
        border-radius: 1rem;
        background-color: #f4f4f4;
    }

    .message.mine .bubble {
        color: white;
        background-color: dodgerblue;
    }

    .composer {
        gap: 0.5rem;
        display: flex;
        margin-top: 0.5rem;
    }
</style>

<BitVirtualize @ref=""chatRef"" Items=""messages"" Dynamic EstimatedItemSize=""48""
               TItem=""Message""
               Reversed AlignToEnd ItemKey=""m => m.Id""
               OnStartReached=""LoadChatHistory"" ReachedThreshold=""3""
               Class=""chat-list"">
    <HeaderTemplate>
        <div class=""list-header"">@(loadingChatHistory ? ""Loading older messages..."" : chatHistoryRemaining > 0 ? """" : ""This is the beginning of the conversation"")</div>
    </HeaderTemplate>
    <ItemTemplate Context=""message"">
        <div class=""message @(message.Mine ? ""mine"" : null)"">
            <div class=""bubble"">@message.Text</div>
        </div>
    </ItemTemplate>
</BitVirtualize>
<div class=""composer"">
    <BitTextField @bind-Value=""draftMessage"" Immediate Placeholder=""Write a message..."" Style=""flex-grow:1"" />
    <BitButton OnClick=""SendChatMessage"" IsEnabled=""@(string.IsNullOrWhiteSpace(draftMessage) is false)"">Send</BitButton>
</div>
<div class=""composer"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => chatRef.ScrollToEndAsync(smooth: true)"">Jump to latest</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""NewConversation"">New conversation</BitButton>
</div>";
    private readonly string example11CsharpCode = @"
private BitVirtualize<Message> chatRef = default!;
private List<Message> messages = [];
private string? draftMessage;
private bool loadingChatHistory;
private int chatHistoryRemaining = 381; // count of the older messages (0..380) not loaded yet

protected override void OnInitialized()
{
    // The newest messages (381..400); scrolling up loads the older ones down to 0.
    messages = Enumerable.Range(381, 20).Select(i => new Message(i, i % 3 == 0, $""Message number {i}"")).ToList();
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
    messages.InsertRange(0, Enumerable.Range(chatHistoryRemaining - batch, batch).Select(i => new Message(i, i % 3 == 0, $""Message number {i}"")));
    chatHistoryRemaining -= batch;
    loadingChatHistory = false;

    await chatRef.RefreshDataAsync(); // the message in view stays where it was
}

private void NewConversation()
{
    chatHistoryRemaining = 0;
    messages = [new Message(1000, false, ""Hi! How can I help you?"")];
}

public record Message(int Id, bool Mine, string Text);";

    private readonly string example12RazorCode = @"
<style>
    .basic-item {
        gap: 0.5rem;
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }

    .custom-class {
        box-shadow: dodgerblue 0 0 1rem;
    }

    .custom-root {
        height: 15rem;
        border: 1px solid seagreen;
    }

    .custom-item:nth-child(odd) {
        background-color: #2e8b5722;
    }

    .custom-footer {
        color: white;
        padding: 0.5rem 1rem;
        background-color: seagreen;
    }
</style>

<BitVirtualize Items=""styleItems"" ItemSize=""48""
               TItem=""int"" Context=""item""
               Style=""height: 15rem; border: 2px solid dodgerblue; border-radius: 0.5rem;""
               Class=""custom-class"">
    <div class=""basic-item"">Item @item</div>
</BitVirtualize>

<BitVirtualize Items=""styleItems"" ItemSize=""48""
               TItem=""int""
               IsStickyItem=""i => i % 10 == 0""
               Styles=""@(new() { Root = ""height: 15rem; border: 1px solid tomato;"",
                                 Header = ""padding: 0.5rem 1rem; color: white; background: tomato;"",
                                 Sticky = ""color: white; background: darkorange;"",
                                 Item = ""padding-inline-start: 1rem;"" })"">
    <HeaderTemplate>Header</HeaderTemplate>
    <ItemTemplate Context=""item"">
        <div class=""basic-item"">@(item % 10 == 0 ? $""Group {item / 10}"" : $""Item {item}"")</div>
    </ItemTemplate>
</BitVirtualize>

<BitVirtualize Items=""styleItems"" ItemSize=""48""
               TItem=""int"" Context=""item""
               Classes=""@(new() { Root = ""custom-root"", Item = ""custom-item"", Footer = ""custom-footer"" })"">
    <ItemTemplate>
        <div class=""basic-item"">Item @item</div>
    </ItemTemplate>
    <FooterTemplate>Footer</FooterTemplate>
</BitVirtualize>";
    private readonly string example12CsharpCode = @"
private readonly int[] styleItems = Enumerable.Range(0, 1_000).ToArray();";

    private readonly string example13RazorCode = @"
<style>
    .horizontal-list {
        height: 9rem;
        border: 1px solid gray;
    }

    .tile {
        display: flex;
        margin: 0.5rem;
        border-radius: 0.5rem;
        align-items: center;
        justify-content: center;
        box-sizing: border-box;
        height: calc(100% - 1rem);
        border: 1px solid lightgray;
    }
</style>

<BitVirtualize Items=""horizontalItems"" ItemSize=""120"" Horizontal Dir=""BitDir.Rtl""
               TItem=""int"" Context=""item""
               Class=""horizontal-list"">
    <div class=""tile"">
        <b>مورد @item.ToString(""N0"")</b>
    </div>
</BitVirtualize>";
    private readonly string example13CsharpCode = @"
private readonly int[] horizontalItems = Enumerable.Range(0, 100_000).ToArray();";
}
