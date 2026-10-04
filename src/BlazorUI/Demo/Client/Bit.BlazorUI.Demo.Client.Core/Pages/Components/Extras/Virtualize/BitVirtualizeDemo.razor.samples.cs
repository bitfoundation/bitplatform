namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Virtualize;

public partial class BitVirtualizeDemo
{
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

<BitNumberField @bind-Value=""gridLanes"" Min=""1"" Max=""8"" Mode=""BitSpinButtonMode.Inline""
                Label=""Lanes"" LabelPosition=""BitLabelPosition.Start""
                IsEnabled=""@(gridResponsive is false)"" Style=""max-width:12rem"" />
<BitToggle @bind-Value=""gridResponsive"" Label=""MinLaneSize = 200"" Inline />

<BitVirtualize Items=""gridItems"" ItemSize=""120"" Lanes=""gridLanes"" MinLaneSize=""@(gridResponsive ? 200 : null)""
               TItem=""int"" Context=""item""
               Class=""list"">
    <div class=""tile"">
        <b>@item.ToString(""N0"")</b>
    </div>
</BitVirtualize>";
    private readonly string example5CsharpCode = @"
private readonly int[] gridItems = Enumerable.Range(0, 100_000).ToArray();
private int gridLanes = 4;
private bool gridResponsive;";

    private readonly string example6RazorCode = @"
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
    <BitTag Text=""@($""[{visibleRange.Start:N0}, {visibleRange.End:N0}) visible"")"" Color=""BitColor.SecondaryBackground"" />
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
    private readonly string example6CsharpCode = @"
private BitVirtualize<int> scrollRef = default!;
private readonly int[] scrollItems = Enumerable.Range(0, 100_000).ToArray();
private int scrollTargetIndex = 5_000;
private bool scrollSmooth = true;
private (int Start, int End) visibleRange;

private async Task ScrollToTarget(BitVirtualizeScrollAlignment alignment)
{
    await scrollRef.ScrollToIndexAsync(scrollTargetIndex, alignment, scrollSmooth);
}";

    private readonly string example7RazorCode = @"
<style>
    .page-scroller {
        height: 25rem;
        overflow: auto;
        border: 1px solid gray;
    }

    .page-content {
        padding: 2rem 1rem;
        background-color: #8881;
    }

    .basic-item {
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
        border-bottom: 1px solid lightgray;
    }
</style>

<div class=""page-scroller"">
    <div class=""page-content"">Content above the list scrolls away with it.</div>
    <BitVirtualize Items=""pageItems"" ItemSize=""48"" ScrollerSelector="".page-scroller""
                   TItem=""int"" Context=""item"">
        <div class=""basic-item"">Item @item</div>
    </BitVirtualize>
    <div class=""page-content"">Content below the list.</div>
</div>";
    private readonly string example7CsharpCode = @"
private readonly int[] pageItems = Enumerable.Range(0, 10_000).ToArray();";

    private readonly string example8RazorCode = @"
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
</style>

<BitVirtualize Items=""articles"" ItemSize=""72""
               TItem=""Article"" Context=""article""
               Role=""feed"" ItemRole=""article"" AriaLabel=""News feed""
               Class=""list"">
    <div class=""news-item"">
        <b>@article.Title</b>
        <span>@article.Summary</span>
    </div>
</BitVirtualize>";
    private readonly string example8CsharpCode = @"
private readonly Article[] articles = Enumerable.Range(1, 10_000)
                                                .Select(i => new Article($""Headline number {i:N0}"", $""A short summary of the story number {i:N0}.""))
                                                .ToArray();

public record Article(string Title, string Summary);";

    private readonly string example9RazorCode = @"
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

    .basic-item.odd {
        background-color: #8881;
    }

    .list-header,
    .list-footer {
        padding: 0.75rem 1rem;
        text-align: center;
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
               TItem=""int""
               Class=""list"">
    <HeaderTemplate>
        <div class=""list-header"">Header · @templateItems.Count.ToString(""N0"") items</div>
    </HeaderTemplate>
    <IndexedItemTemplate Context=""context"">
        <div class=""basic-item @(context.Index % 2 == 1 ? ""odd"" : null)"">
            <b>@(context.Index + 1).</b> Item @context.Item
        </div>
    </IndexedItemTemplate>
    <FooterTemplate>
        <div class=""list-footer"">Footer · end of the list</div>
    </FooterTemplate>
    <EmptyTemplate>
        No items to show
    </EmptyTemplate>
</BitVirtualize>";
    private readonly string example9CsharpCode = @"
private List<int> templateItems = [.. Enumerable.Range(0, 1_000)];";

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

    .list-footer {
        padding: 0.75rem 1rem;
        text-align: center;
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
    private readonly string example10CsharpCode = @"
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

    private readonly string example11RazorCode = @"
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
    private readonly string example11CsharpCode = @"
private List<Contact> contacts = [];

protected override void OnInitialized()
{
    var random = new Random(11);
    var firstNames = ""Alice Bruno Chloe Daniel Emma Felix Grace Hugo Isla Jack Kira Liam Maya Noah Olivia Paul Quinn Ruby Sam Tara Umar Vera Will Xena Yusuf Zoe"".Split(' ');
    var lastNames = ""Adams Baker Clark Davis Evans Foster Green Hall Irwin Jones King Lewis Moore Nash Owen Price Reed Scott Turner"".Split(' ');

    contacts = [];
    for (var c = 'A'; c <= 'Z'; c++)
    {
        contacts.Add(new Contact(true, c.ToString(), string.Empty));
        var count = random.Next(5, lastNames.Length + 1);
        for (var i = 0; i < count; i++)
        {
            var name = $""{firstNames[c - 'A']} {lastNames[i]}"";
            contacts.Add(new Contact(false, name, $""{name.Replace(' ', '.').ToLower()}@example.com""));
        }
    }
}

public record Contact(bool IsHeader, string Name, string Email);";

    private readonly string example12RazorCode = @"
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
    private readonly string example12CsharpCode = @"
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

    private readonly string example13RazorCode = @"
<style>
    .list {
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
               AriaLabel=""Messages""
               Class=""list"">
    <HeaderTemplate>
        <div class=""list-header"">@(loadingChatHistory ? ""Loading older messages..."" : chatHistoryRemaining > 0 ? ""Scroll up for older messages"" : ""This is the beginning of the conversation"")</div>
    </HeaderTemplate>
    <ItemTemplate Context=""message"">
        <div class=""message @(message.Mine ? ""mine"" : null)"">
            <div class=""bubble"">@message.Text</div>
        </div>
    </ItemTemplate>
</BitVirtualize>
<div class=""composer"">
    <BitTextField @bind-Value=""draftMessage"" Immediate Placeholder=""Write a message..."" AriaLabel=""Message"" Style=""flex-grow:1"" />
    <BitButton OnClick=""SendChatMessage"" IsEnabled=""@(string.IsNullOrWhiteSpace(draftMessage) is false)"">Send</BitButton>
</div>
<div class=""composer"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => chatRef.ScrollToEndAsync(smooth: true)"">Jump to latest</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""NewConversation"">New conversation</BitButton>
</div>";
    private readonly string example13CsharpCode = @"
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

    private readonly string example14RazorCode = @"
<style>
    .cascaded-list {
        height: 15rem;
        border: 1px solid gray;
    }

    .cascaded-item {
        border-bottom: 1px solid lightgray;
    }

    .basic-item {
        display: flex;
        height: 100%;
        padding: 0 1rem;
        align-items: center;
        box-sizing: border-box;
    }
</style>

<BitParams Parameters=""virtualizeParams"">
    <BitVirtualize Items=""styleItems"" TItem=""int"" Context=""item"">
        <div class=""basic-item"">Item @item</div>
    </BitVirtualize>

    <BitVirtualize Items=""styleItems"" ItemSize=""64"" TItem=""int"" Context=""item"">
        <div class=""basic-item"">Item @item</div>
    </BitVirtualize>
</BitParams>";
    private readonly string example14CsharpCode = @"
private readonly int[] styleItems = Enumerable.Range(0, 1_000).ToArray();

private readonly BitVirtualizeParams[] virtualizeParams =
[
    new()
    {
        ItemSize = 40,
        OverscanCount = 5,
        Classes = new() { Root = ""cascaded-list"", Item = ""cascaded-item"" },
    }
];";

    private readonly string example15RazorCode = @"
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

    .custom-item {
        border-inline-start: 4px solid seagreen;
    }

    .short-list {
        height: 15rem;
        border: 1px solid gray;
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
</BitVirtualize>

<BitVirtualize Items=""styleItems"" ItemSize=""48""
               TItem=""int""
               IsStickyItem=""i => i % 10 == 0""
               Class=""short-list""
               Style=""@cssVariablesStyle"">
    <ItemTemplate Context=""item"">
        <div class=""basic-item"">@(item % 10 == 0 ? $""Group {item / 10}"" : $""Item {item}"")</div>
    </ItemTemplate>
</BitVirtualize>";
    private readonly string example15CsharpCode = @"
private readonly int[] styleItems = Enumerable.Range(0, 1_000).ToArray();

private const string cssVariablesStyle = ""--bit-Virtualize-focus-color:tomato;"" +
                                         ""--bit-Virtualize-sticky-background:var(--bit-clr-bg-sec);"" +
                                         ""--bit-Virtualize-sticky-shadow:0 2px 6px rgba(0, 0, 0, 0.2)"";";

    private readonly string example16RazorCode = @"
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
    private readonly string example16CsharpCode = @"
private readonly int[] horizontalItems = Enumerable.Range(0, 100_000).ToArray();";
}
