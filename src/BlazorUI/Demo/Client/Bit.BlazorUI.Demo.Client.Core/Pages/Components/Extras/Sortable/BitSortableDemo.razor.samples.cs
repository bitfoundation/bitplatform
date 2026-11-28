namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Sortable;

public partial class BitSortableDemo
{
    private readonly string example1RazorCode = @"
<div class=""panel"">
    <BitSortable @bind-Items=""basicItems"">
        <ItemTemplate Context=""item"">
            <div class=""item"">@item</div>
        </ItemTemplate>
    </BitSortable>
</div>
<div class=""order"">Order: @string.Join("", "", basicItems)</div>";
    private readonly string example1CsharpCode = @"
private IList<string> basicItems = [""Item 1"", ""Item 2"", ""Item 3"", ""Item 4"", ""Item 5""];";

    private readonly string example2RazorCode = @"
<div class=""controls"">
    <BitSlider Label=""Animation (ms)"" Min=""0"" Max=""1000"" Step=""50"" @bind-Value=""animation"" />
</div>
<div class=""panel"">
    <BitSortable @bind-Items=""animationItems"" Animation=""(int)animation"">
        <ItemTemplate Context=""item"">
            <div class=""item"">@item</div>
        </ItemTemplate>
    </BitSortable>
</div>";
    private readonly string example2CsharpCode = @"
private double animation = 150;

private IList<string> animationItems = [""Apples"", ""Bread"", ""Cheese"", ""Dates"", ""Eggs""];";

    private readonly string example3RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">List A</div>
        <BitSortable @bind-Items=""groupA"" Group=""shared"" ItemClass=""@(item => item.StartsWith('B') ? ""tinted"" : null)"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">List B</div>
        <BitSortable @bind-Items=""groupB"" Group=""shared"" ItemClass=""@(item => item.StartsWith('B') ? ""tinted"" : null)"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>
<div class=""order"">A: @string.Join("", "", groupA)</div>
<div class=""order"">B: @string.Join("", "", groupB)</div>";
    private readonly string example3CsharpCode = @"
private IList<string> groupA = [""A1"", ""A2"", ""A3"", ""A4""];
private IList<string> groupB = [""B1"", ""B2"", ""B3"", ""B4""];";

    private readonly string example4RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Inbox</div>
        <BitSortable @bind-Items=""inbox"" Group=""mail"" EmptyText=""Inbox zero - nothing left to sort."">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Archive</div>
        <BitSortable @bind-Items=""mailArchive"" Group=""mail"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
            <EmptyTemplate>
                <div class=""empty"">
                    <BitIcon IconName=""@BitIconName.Archive"" />
                    Nothing archived yet - drop a message here.
                </div>
            </EmptyTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Spam</div>
        <BitSortable @bind-Items=""spam"" Group=""mail"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example4CsharpCode = @"
private IList<string> inbox = [""Invoice"", ""Newsletter"", ""Meeting notes""];
private IList<string> mailArchive = [];
private IList<string> spam = [];";

    private readonly string example5RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Palette</div>
        <BitSortable Items=""palette"" Group=""form"" Pull=""BitSortablePull.Clone"" Clone=""@(field => new FormField(field.Name))"">
            <ItemTemplate Context=""field"">
                <div class=""item"">@field.Name</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Form</div>
        <BitSortable @bind-Items=""form"" Group=""form"" EmptyText=""Drag fields from the palette."">
            <ItemTemplate Context=""field"">
                <div class=""item"">@field.Name</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>
<div class=""order"">Form: @string.Join("", "", form.Select(f => f.Name))</div>";
    private readonly string example5CsharpCode = @"
// A read-only list: it is shown and cloned from, but never sorted, pulled from or dropped into.
private readonly FormField[] palette = [new(""Text box""), new(""Checkbox""), new(""Drop-down""), new(""Date"")];

private IList<FormField> form = [];

// A class, not a record: two copies of the same field must not be equal, since a list holds an item once.
public sealed class FormField(string name)
{
    public string Name { get; } = name;
}";

    private readonly string example6RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Pick from (no Sort, no Put)</div>
        <BitSortable @bind-Items=""pickFrom"" Group=""rules"" Sort=""false"" Put=""false"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Pinned (Pull None)</div>
        <BitSortable @bind-Items=""pinned"" Group=""rules"" Pull=""BitSortablePull.None"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Target</div>
        <BitSortable @bind-Items=""target"" Group=""rules"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example6CsharpCode = @"
private IList<string> pickFrom = [""Pick 1"", ""Pick 2"", ""Pick 3""];
private IList<string> pinned = [""Rent"", ""Groceries""];
private IList<string> target = [""Gym"", ""Books""];";

    private readonly string example7RazorCode = @"
<div class=""controls"">
    <BitToggle Label=""Disabled"" @bind-Value=""archiveDisabled"" />
</div>
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Archive</div>
        <BitSortable @bind-Items=""archive"" Group=""desk"" Disabled=""archiveDisabled"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Desk</div>
        <BitSortable @bind-Items=""desk"" Group=""desk"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example7CsharpCode = @"
private bool archiveDisabled = true;

private IList<string> archive = [""2024 report"", ""2025 report""];
private IList<string> desk = [""Draft"", ""Notes""];";

    private readonly string example8RazorCode = @"
<div class=""panel"">
    <BitSortable @bind-Items=""steps"" Handle="".handle"">
        <ItemTemplate Context=""step"">
            <div class=""item row"">
                <BitIcon IconName=""@BitIconName.GripperDotsVertical"" Class=""handle"" />
                <BitCheckbox Label=""@step.Text"" @bind-Value=""step.Done"" />
            </div>
        </ItemTemplate>
    </BitSortable>
</div>
<div class=""order"">Steps: @string.Join("", "", steps.Select(s => s.Done ? $""{s.Text} (done)"" : s.Text))</div>";
    private readonly string example8CsharpCode = @"
private IList<ChecklistStep> steps =
[
    new() { Text = ""Pack the tent"" },
    new() { Text = ""Check the weather"", Done = true },
    new() { Text = ""Fill the water bottles"" },
    new() { Text = ""Charge the torch"" },
];

public sealed class ChecklistStep
{
    public required string Text { get; set; }
    public bool Done { get; set; }
}";

    private readonly string example9RazorCode = @"
<div class=""panel"">
    <BitSortable @bind-Items=""tasks"" Filter=""@(task => task.Locked)"">
        <ItemTemplate Context=""task"">
            <div class=""item row"">
                <span class=""grow"">@task.Title</span>
                <BitButton IconName=""@(task.Locked ? BitIconName.Lock : BitIconName.Unlock)""
                           Variant=""BitVariant.Text""
                           Size=""BitSize.Small""
                           AriaLabel=""@($""Lock {task.Title}"")""
                           OnClick=""@(() => task.Locked = !task.Locked)"" />
            </div>
        </ItemTemplate>
    </BitSortable>
</div>";
    private readonly string example9CsharpCode = @"
private IList<LockableTask> tasks =
[
    new() { Title = ""Write the tests"" },
    new() { Title = ""Release notes"", Locked = true },
    new() { Title = ""Deploy"" },
    new() { Title = ""Announce"" },
];

public sealed class LockableTask
{
    public required string Title { get; init; }
    public bool Locked { get; set; }
}";

    private readonly string example10RazorCode = @"
<div class=""controls"">
    <BitSlider Label=""SwapThreshold"" Min=""0.1"" Max=""1"" Step=""0.05"" @bind-Value=""swapThreshold"" />
    <BitToggle Label=""InvertSwap"" @bind-Value=""invertSwap"" />
    <BitSlider Label=""InvertedSwapThreshold"" Min=""0.1"" Max=""1"" Step=""0.05"" @bind-Value=""invertedSwapThreshold"" />
</div>
<div class=""panel zones @(invertSwap ? ""inverted"" : null)"" style=""@ZoneStyle"">
    <BitSortable @bind-Items=""thresholdItems""
                 Direction=""BitSortableDirection.Vertical""
                 SwapThreshold=""swapThreshold""
                 InvertSwap=""invertSwap""
                 InvertedSwapThreshold=""invertedSwapThreshold"">
        <ItemTemplate Context=""item"">
            <div class=""item"">@item</div>
        </ItemTemplate>
    </BitSortable>
</div>";
    private readonly string example10CsharpCode = @"
private double swapThreshold = 0.65;
private bool invertSwap;
private double invertedSwapThreshold = 0.65;

private IList<string> thresholdItems = [""Item 1"", ""Item 2"", ""Item 3"", ""Item 4"", ""Item 5""];

// The zones are drawn by the page's stylesheet, from these two custom properties.
private string ZoneStyle => string.Create(CultureInfo.InvariantCulture,
    $""--zone:{swapThreshold:0.00};--inverted-zone:{invertedSwapThreshold:0.00}"");";

    private readonly string example11RazorCode = @"
<div class=""panel grid"">
    <BitSortable @bind-Items=""numbers"">
        <ItemTemplate Context=""number"">
            <div class=""tile"">@number</div>
        </ItemTemplate>
    </BitSortable>
</div>
<br />
<div class=""panel strip"">
    <BitSortable @bind-Items=""tags"" Direction=""BitSortableDirection.Horizontal"">
        <ItemTemplate Context=""tag"">
            <div class=""chip"">@tag</div>
        </ItemTemplate>
    </BitSortable>
</div>";
    private readonly string example11CsharpCode = @"
private IList<int> numbers = Enumerable.Range(1, 12).ToList();

private IList<string> tags = [""blazor"", ""dotnet"", ""drag-and-drop"", ""sortable"", ""css"", ""a11y""];";
    private const string example11ScssCode = @"
// The lists are laid out from a plain element of the page, which carries its scope attribute.
.grid ::deep .bit-srt {
    gap: 0.5rem;
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(3.5rem, 1fr));
}

.strip ::deep .bit-srt {
    gap: 0.5rem;
    display: flex;
    flex-wrap: wrap;
}

.tile {
    display: grid;
    aspect-ratio: 1;
    place-items: center;
    border-radius: 0.25rem;
    background-color: var(--bit-clr-bg-sec);
    border: 1px solid var(--bit-clr-brd-pri);
}

.chip {
    padding: 0.25rem 0.75rem;
    border-radius: 1rem;
    border: 1px solid var(--bit-clr-brd-pri);
}";
    private readonly DemoCodeFile[] example11CodeFiles =
    [
        new("BitSortableDemo.razor.scss", example11ScssCode),
    ];

    private readonly string example12RazorCode = @"
<div class=""panel tree"">
    @NestedList(tree)
</div>
<div class=""order"">Tree: @Describe(tree)</div>";
    private readonly string example12CsharpCode = @"
// A list inside every item: each level shares the group, so an item moves from branch to branch with its children.
private RenderFragment NestedList(IList<TreeNode> nodes) =>
    @<BitSortable TItem=""TreeNode"" Items=""nodes"" ItemsChanged=""StateHasChanged"" Group=""tree"" SwapThreshold=""0.65"" EmptyText="""">
        <ItemTemplate Context=""node"">
            <div class=""item"">@node.Name</div>
            <div class=""children"">
                @NestedList(node.Children)
            </div>
        </ItemTemplate>
    </BitSortable>;

private IList<TreeNode> tree =
[
    new(""Item 1.1"", [new(""Item 2.1""), new(""Item 2.2"", [new(""Item 3.1""), new(""Item 3.2"")]), new(""Item 2.3"")]),
    new(""Item 1.2""),
    new(""Item 1.3"", [new(""Item 2.4""), new(""Item 2.5"")]),
    new(""Item 1.4""),
];

// ""Item 1.1 [Item 2.1, Item 2.2 [Item 3.1, Item 3.2]], Item 1.2"": every item, its children in brackets.
private static string Describe(IList<TreeNode> nodes)
    => string.Join("", "", nodes.Select(n => n.Children.Count == 0 ? n.Name : $""{n.Name} [{Describe(n.Children)}]""));

public sealed class TreeNode(string name, List<TreeNode>? children = null)
{
    public string Name { get; } = name;
    public IList<TreeNode> Children { get; } = children ?? [];
}";

    private readonly string example13RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Tasks</div>
        <BitSortable @bind-Items=""todo"" Group=""tasks"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Bugs</div>
        <BitSortable @bind-Items=""bugs"" Group=""bugs"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Trash</div>
        <BitSortable @bind-Items=""trash"" Group=""trash"" PutGroups=""@([""tasks"", ""bugs""])"" EmptyText=""Drop a task or a bug here."">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example13CsharpCode = @"
private IList<string> todo = [""Write the docs"", ""Plan the release"", ""Tidy the backlog""];
private IList<string> bugs = [""Crash on save"", ""Typo in the footer""];
private IList<string> trash = [];";

    private readonly string example14RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Pipeline (at least 2)</div>
        <BitSortable @bind-Items=""pipeline"" Group=""limits"" MinItems=""2"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Shelf (at most 3)</div>
        <BitSortable @bind-Items=""shelf"" Group=""limits"" MaxItems=""3"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example14CsharpCode = @"
private IList<string> pipeline = [""Build"", ""Test"", ""Deploy""];
private IList<string> shelf = [""Lint"", ""Benchmark""];";

    private readonly string example15RazorCode = @"
<div class=""controls"">
    <BitToggle Label=""The server approves drops into Done"" @bind-Value=""approve"" />
</div>
<div class=""panels"" style=""@boardOutlines"">
    <div class=""panel"">
        <div class=""title"">To do</div>
        <BitSortable @bind-Items=""boardTodo"" Group=""board"" Id=""board-todo"" RevertOnSpill>
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">In progress</div>
        <BitSortable @bind-Items=""boardDoing"" Group=""board"" Id=""board-doing"" RevertOnSpill>
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Done (from In progress)</div>
        <BitSortable @bind-Items=""boardDone""
                     Group=""board""
                     Id=""board-done""
                     RevertOnSpill
                     CanDrop=""CanDropInDone""
                     ConfirmDrop=""ConfirmDoneAsync"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>
<div class=""order"">Server: @serverStatus</div>";
    private readonly string example15CsharpCode = @"
private bool approve = true;
private string serverStatus = ""idle"";

private IList<string> boardTodo = [""Write the spec"", ""Draw the icons"", ""Plan the launch""];
private IList<string> boardDoing = [""Build the API"", ""Review the PR""];
private IList<string> boardDone = [""Set up CI""];

// The script marks the lists that take a drag, the one under the pointer and one that refused the position; the
// public variables, set on an ancestor of the board, draw them.
private const string boardOutlines = ""--bit-Sortable-accepts-outline:2px dashed var(--bit-clr-pri);"" +
                                     ""--bit-Sortable-over-outline:2px solid var(--bit-clr-pri);"" +
                                     ""--bit-Sortable-refused-outline:2px solid var(--bit-clr-err);"";

// Done takes the cards of In progress, and its own.
private bool CanDropInDone(BitSortableEventArgs<string> e) => e.FromId is ""board-doing"" or ""board-done"";

// Stands in for a call to a server that may refuse the move.
private async Task<bool> ConfirmDoneAsync(BitSortableEventArgs<string> e)
{
    serverStatus = $""asking about {e.Item}..."";
    StateHasChanged();
    await Task.Delay(500);
    serverStatus = approve ? $""saved {e.Item}"" : $""refused {e.Item}"";
    StateHasChanged();
    return approve;
}";

    private readonly string example16RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Backlog</div>
        <BitSortable @ref=""backlogList"" @bind-Items=""backlog"" Group=""sprint"" Id=""move-backlog"">
            <ItemTemplate Context=""item"">
                <div class=""item row"">
                    <span class=""grow"">@item</span>
                    <BitButton IconName=""@BitIconName.Up""
                               Variant=""BitVariant.Text""
                               Size=""BitSize.Small""
                               AriaLabel=""@($""Move {item} up"")""
                               OnClick=""@(() => backlogList.MoveAsync(item, backlog.IndexOf(item) - 1))"" />
                    <BitButton IconName=""@BitIconName.Down""
                               Variant=""BitVariant.Text""
                               Size=""BitSize.Small""
                               AriaLabel=""@($""Move {item} down"")""
                               OnClick=""@(() => backlogList.MoveAsync(item, backlog.IndexOf(item) + 1))"" />
                    <BitButton IconName=""@BitIconName.Forward""
                               Variant=""BitVariant.Text""
                               Size=""BitSize.Small""
                               AriaLabel=""@($""Move {item} to the sprint"")""
                               OnClick=""@(() => backlogList.MoveToAsync(item, ""move-sprint"", sprint.Count))"" />
                </div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Sprint</div>
        <BitSortable @ref=""sprintList"" @bind-Items=""sprint"" Group=""sprint"" Id=""move-sprint"">
            <ItemTemplate Context=""item"">
                <div class=""item row"">
                    <span class=""grow"">@item</span>
                    <BitButton IconName=""@BitIconName.Back""
                               Variant=""BitVariant.Text""
                               Size=""BitSize.Small""
                               AriaLabel=""@($""Move {item} back to the backlog"")""
                               OnClick=""@(() => sprintList.MoveToAsync(item, ""move-backlog"", backlog.Count))"" />
                </div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example16CsharpCode = @"
private BitSortable<string> backlogList = default!;
private BitSortable<string> sprintList = default!;

private IList<string> backlog = [""Sign-up page"", ""Password reset"", ""Audit log"", ""Dark mode""];
private IList<string> sprint = [""Search"", ""Export to CSV""];";

    private readonly string example17RazorCode = @"
<div class=""panel"">
    <BitSortable @bind-Items=""swapItems"" TItem=""string"" Swap OnUpdate=""HandleSwap"">
        <ItemTemplate Context=""item"">
            <div class=""item"">@item</div>
        </ItemTemplate>
    </BitSortable>
</div>
<div class=""order"">Order: @string.Join("", "", swapItems)</div>
<div class=""order"">Last swap: @lastSwap</div>
<br />
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Left</div>
        <BitSortable @bind-Items=""swapLeft"" Group=""swap"" Swap>
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Right</div>
        <BitSortable @bind-Items=""swapRight"" Group=""swap"" Swap>
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example17CsharpCode = @"
private string lastSwap = ""nothing yet"";

private IList<string> swapItems = [""Item 1"", ""Item 2"", ""Item 3"", ""Item 4"", ""Item 5""];

private IList<string> swapLeft = [""Item 1"", ""Item 2"", ""Item 3""];
private IList<string> swapRight = [""Item A"", ""Item B"", ""Item C""];

// SwappedWith names the item the dragged one traded places with: both moves, for a handler that saves them.
private void HandleSwap(BitSortableEventArgs<string> e)
{
    lastSwap = e.IsSwap ? $""{e.Item} and {e.SwappedWith}"" : $""{e.Item} moved"";
}";

    private readonly string example18RazorCode = @"
<div class=""controls"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => selectedItems = multiItems.ToList())"">Select all</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => selectedItems = [])"">Clear the selection</BitButton>
</div>
<div class=""panel"">
    <BitSortable @bind-Items=""multiItems"" @bind-SelectedItems=""selectedItems"" MultiDrag>
        <ItemTemplate Context=""item"">
            <div class=""item"">@item</div>
        </ItemTemplate>
    </BitSortable>
</div>
<div class=""order"">Selected: @(selectedItems.Count == 0 ? ""nothing"" : string.Join("", "", selectedItems))</div>
<br />
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Left</div>
        <BitSortable @bind-Items=""multiLeft"" Group=""multi"" MultiDrag>
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Right</div>
        <BitSortable @bind-Items=""multiRight"" Group=""multi"" MultiDrag>
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example18CsharpCode = @"
private IList<string> multiItems = [""Item 1"", ""Item 2"", ""Item 3"", ""Item 4"", ""Item 5"", ""Item 6""];
private IReadOnlyCollection<string> selectedItems = [];

private IList<string> multiLeft = [""Item 1"", ""Item 2"", ""Item 3"", ""Item 4""];
private IList<string> multiRight = [""Item A"", ""Item B""];";

    private readonly string example19RazorCode = @"
<div class=""controls"">
    <BitToggle Label=""Log OnChange too"" @bind-Value=""logChange"" />
    <BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => eventLog.Clear())"">Clear the log</BitButton>
</div>
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">events-left</div>
        <BitSortable @bind-Items=""eventsLeft""
                     TItem=""string""
                     Group=""events""
                     Id=""events-left""
                     OnStart=""@(e => Log(""OnStart"", ""events-left"", e))""
                     OnChange=""@(e => Log(""OnChange"", ""events-left"", e))""
                     OnUpdate=""@(e => Log(""OnUpdate"", ""events-left"", e))""
                     OnRemove=""@(e => Log(""OnRemove"", ""events-left"", e))""
                     OnAdd=""@(e => Log(""OnAdd"", ""events-left"", e))""
                     OnSort=""@(e => Log(""OnSort"", ""events-left"", e))""
                     OnEnd=""@(e => Log(""OnEnd"", ""events-left"", e))"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">events-right</div>
        <BitSortable @bind-Items=""eventsRight""
                     TItem=""string""
                     Group=""events""
                     Id=""events-right""
                     OnStart=""@(e => Log(""OnStart"", ""events-right"", e))""
                     OnChange=""@(e => Log(""OnChange"", ""events-right"", e))""
                     OnUpdate=""@(e => Log(""OnUpdate"", ""events-right"", e))""
                     OnRemove=""@(e => Log(""OnRemove"", ""events-right"", e))""
                     OnAdd=""@(e => Log(""OnAdd"", ""events-right"", e))""
                     OnSort=""@(e => Log(""OnSort"", ""events-right"", e))""
                     OnEnd=""@(e => Log(""OnEnd"", ""events-right"", e))"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>
<div class=""log"">
    @if (eventLog.Count == 0)
    {
        <div>No events yet.</div>
    }
    @foreach (var line in eventLog)
    {
        <div>@line</div>
    }
</div>";
    private readonly string example19CsharpCode = @"
private bool logChange;
private readonly List<string> eventLog = [];

private IList<string> eventsLeft = [""Item 1"", ""Item 2"", ""Item 3"", ""Item 4""];
private IList<string> eventsRight = [""Item A"", ""Item B""];

// ""[OnAdd @events-right] Item 4: events-left#3 -> events-right#0 (mouse)"": the event, the list that raised it, the
// item, where it came from and where it went, how it was dragged and the flags that are set.
private void Log(string name, string list, BitSortableEventArgs<string> e)
{
    if (name == ""OnChange"" && logChange is false) return;

    var flags = string.Join("", "", new[] { e.Input.ToString().ToLowerInvariant(), e.IsClone ? ""clone"" : null, e.Canceled ? ""canceled"" : null }.OfType<string>());
    eventLog.Insert(0, $""[{name} @{list}] {e.Item}: {e.FromId}#{e.OldIndex} -> {e.ToId}#{e.NewIndex} ({flags})"");
    if (eventLog.Count > 30) eventLog.RemoveAt(eventLog.Count - 1);
}";

    private readonly string example20RazorCode = @"
<div class=""controls"">
    <BitToggle Label=""Keyboard"" @bind-Value=""keyboard"" />
    <BitToggle Label=""Deutsch"" @bind-Value=""german"" />
</div>
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Backlog</div>
        <BitSortable @bind-Items=""ticketBacklog""
                     Group=""tickets""
                     AriaLabel=""Backlog""
                     ItemLabel=""@(ticket => ticket.Title)""
                     Keyboard=""keyboard""
                     Strings=""@(german ? germanStrings : null)"">
            <ItemTemplate Context=""ticket"">
                <div class=""item"">#@ticket.Id @ticket.Title</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Sprint</div>
        <BitSortable @bind-Items=""ticketSprint""
                     Group=""tickets""
                     AriaLabel=""Sprint""
                     ItemLabel=""@(ticket => ticket.Title)""
                     Keyboard=""keyboard""
                     Strings=""@(german ? germanStrings : null)"">
            <ItemTemplate Context=""ticket"">
                <div class=""item"">#@ticket.Id @ticket.Title</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example20CsharpCode = @"
private bool keyboard = true;
private bool german;

private IList<Ticket> ticketBacklog = [new(1, ""Sign-up page""), new(2, ""Password reset""), new(3, ""Audit log"")];
private IList<Ticket> ticketSprint = [new(4, ""Search""), new(5, ""Export to CSV"")];

private sealed record Ticket(int Id, string Title);

private static string In(string? list) => list is null ? """" : $"" in {list}"";

private readonly BitSortableStrings germanStrings = new()
{
    Instructions = ""Leertaste oder Eingabetaste nimmt das Element auf. Danach verschieben es die Pfeiltasten, Pos1 und Ende an den "" +
                   ""Anfang oder das Ende der Liste, Leertaste oder Eingabetaste legt es ab, und Escape bringt es zurück."",
    SelectionInstructions = ""Umschalt und Leertaste wählen das Element aus oder ab; ein ausgewähltes Element nimmt die ganze Auswahl mit."",
    RoleDescription = ""sortierbares Element"",
    PickedUp = a => $""{a.Item} aufgenommen. Position {a.Position} von {a.Count}{In(a.List)}."",
    Moved = a => $""{a.Item} auf Position {a.Position} von {a.Count}{In(a.List)} verschoben."",
    SwapTarget = a => a.Target is null ? $""{a.Item} bleibt, wo es ist."" : $""{a.Item} tauscht den Platz mit {a.Target}."",
    Dropped = a => a.Returned
        ? $""{a.Item} abgelegt. Wieder auf Position {a.Position} von {a.Count}{In(a.List)}.""
        : $""{a.Item} auf Position {a.Position} von {a.Count}{In(a.List)} abgelegt."",
    Canceled = a => $""Abgebrochen. {a.Item} ist wieder auf Position {a.Position} von {a.Count}{In(a.List)}."",
    Refused = a => $""{a.Item} kann nicht dorthin verschoben werden."",
    Locked = a => $""{a.Item} kann nicht verschoben werden."",
    Selected = a => $""{a.Item} ausgewählt. {a.Selected} ausgewählt."",
    Deselected = a => $""{a.Item} abgewählt. {a.Selected} ausgewählt."",
};";

    private readonly string example21RazorCode = @"
<div class=""controls"">
    <BitSlider Label=""TouchDelay (ms)"" Min=""0"" Max=""1000"" Step=""50"" @bind-Value=""touchDelay"" />
    <BitSlider Label=""Delay (ms)"" Min=""0"" Max=""1000"" Step=""50"" @bind-Value=""delay"" />
    <BitSlider Label=""DelayTolerance (px)"" Min=""0"" Max=""40"" @bind-Value=""delayTolerance"" />
    <BitToggle Label=""ForceFallback"" @bind-Value=""forceFallback"" />
</div>
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Long press</div>
        <BitSortable @bind-Items=""groceries""
                     TouchDelay=""(int)touchDelay""
                     Delay=""(int)delay""
                     DelayTolerance=""(int)delayTolerance""
                     ForceFallback=""forceFallback"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Handle, at once</div>
        <BitSortable @bind-Items=""chores"" Handle="".handle"" TouchDelay=""0"">
            <ItemTemplate Context=""item"">
                <div class=""item row"">
                    <BitIcon IconName=""@BitIconName.GripperDotsVertical"" Class=""handle"" />
                    <span>@item</span>
                </div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example21CsharpCode = @"
private double touchDelay = 200;
private double delay;
private double delayTolerance = 5;
private bool forceFallback;

private IList<string> groceries = [""Kale"", ""Lemons"", ""Milk"", ""Nuts"", ""Oats""];

private IList<string> chores = [""Dishes"", ""Laundry"", ""Plants"", ""Recycling""];";
    private const string example21ScssCode = @"
// The handle is rendered by the list's item template: reached through ::deep. The browser never scrolls from it, so
// with TouchDelay=""0"" a finger on it drags at once.
::deep .handle {
    cursor: grab;
    touch-action: none;
}";
    private readonly DemoCodeFile[] example21CodeFiles =
    [
        new("BitSortableDemo.razor.scss", example21ScssCode),
    ];

    private readonly string example22RazorCode = @"
<div class=""controls"">
    <BitToggle Label=""IsolateItems"" @bind-Value=""isolateItems"" />
    <BitToggle Label=""ForceFallback"" @bind-Value=""largeForceFallback"" />
    <BitToggle Label=""Scroll"" @bind-Value=""scroll"" />
    <BitSlider Label=""ScrollSensitivity (px)"" Min=""10"" Max=""120"" Step=""5"" @bind-Value=""scrollSensitivity"" />
    <BitSlider Label=""ScrollSpeed (px)"" Min=""2"" Max=""60"" Step=""2"" @bind-Value=""scrollSpeed"" />
</div>
<div class=""panel scroller"">
    <BitSortable @bind-Items=""rows""
                 IsolateItems=""isolateItems""
                 ForceFallback=""largeForceFallback""
                 Scroll=""scroll""
                 ScrollSensitivity=""(int)scrollSensitivity""
                 ScrollSpeed=""(int)scrollSpeed"">
        <ItemTemplate Context=""row"">
            <div class=""item row"">
                <span class=""grow"">@row.Name</span>
                <span class=""note"">@row.Note</span>
            </div>
        </ItemTemplate>
    </BitSortable>
</div>";
    private readonly string example22CsharpCode = @"
private bool isolateItems;
private bool largeForceFallback;
private bool scroll = true;
private double scrollSensitivity = 40;
private double scrollSpeed = 16;

private IList<LargeRow> rows = Enumerable.Range(1, 300)
    .Select(i => new LargeRow($""Row {i}"", i % 3 == 0 ? ""blocked"" : i % 2 == 0 ? ""in review"" : ""open""))
    .ToList();

private sealed record LargeRow(string Name, string Note);";

    private readonly string example23RazorCode = @"
<div class=""controls"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""ReloadRows"">Load the rows again</BitButton>
    <BitToggle Label=""ItemKey"" @bind-Value=""keyById"" />
</div>
<div class=""panel"">
    <BitSortable @bind-Items=""taskRows"" ItemKey=""@(keyById ? byId : null)"" MultiDrag>
        <ItemTemplate Context=""row"">
            <div class=""item"">@row.Title</div>
        </ItemTemplate>
    </BitSortable>
</div>
<div class=""order"">Loaded @loads time(s)</div>";
    private readonly string example23CsharpCode = @"
private static readonly Func<TaskRow, object> byId = row => row.Id;

private bool keyById = true;
private int loads = 1;

private IList<TaskRow> taskRows = [new(1, ""Write the spec""), new(2, ""Review the design""), new(3, ""Build the API""), new(4, ""Ship it"")];

// What a refresh from the server does: the same rows, in the order last saved, as new objects.
private void ReloadRows()
{
    taskRows = taskRows.Select(row => new TaskRow(row.Id, row.Title)).ToList();
    loads++;
}

// A row as a server sends it: a class, so two loads of the same row are two different objects.
public sealed class TaskRow(int id, string title)
{
    public int Id { get; } = id;
    public string Title { get; } = title;
}";

    private readonly string example24RazorCode = @"
<BitParams Parameters=""sortableParams"">
    <div class=""panels"">
        <div class=""panel"">
            <BitSortable @bind-Items=""paramsFirst"" Group=""params"">
                <ItemTemplate Context=""item"">
                    <div class=""item"">@item</div>
                </ItemTemplate>
            </BitSortable>
        </div>
        <div class=""panel"">
            <BitSortable @bind-Items=""paramsSecond"" Group=""params"" EmptyText=""My own empty text."">
                <ItemTemplate Context=""item"">
                    <div class=""item"">@item</div>
                </ItemTemplate>
            </BitSortable>
        </div>
    </div>
</BitParams>";
    private readonly string example24CsharpCode = @"
private readonly BitSortableParams[] sortableParams =
[
    new() { Animation = 600, EmptyText = ""Nothing here - drop an item."" }
];

private IList<string> paramsFirst = [""One"", ""Two"", ""Three""];
private IList<string> paramsSecond = [];";

    private readonly string example25RazorCode = @"
<div class=""panels"">
    <div class=""panel"">
        <div class=""title"">Style & Class</div>
        <BitSortable @bind-Items=""styledItems""
                     Style=""border:1px dashed var(--bit-clr-brd-pri);border-radius:0.25rem;padding:0 0.25rem""
                     Class=""custom-class"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">Styles & Classes</div>
        <BitSortable @bind-Items=""classedItems""
                     Styles=""@(new() { Chosen = ""background-color:var(--bit-clr-sec-light)"" })""
                     Classes=""@(new() { Item = ""custom-item"", Ghost = ""custom-ghost"" })"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <div class=""title"">ItemStyle</div>
        <BitSortable @bind-Items=""itemStyled"" ItemStyle=""@(item => item == ""Important"" ? ""font-weight:bold"" : null)"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>
<br />
<div class=""panels"" style=""@cssVariablesStyle"">
    <div class=""panel"">
        <BitSortable @bind-Items=""fruits"" Group=""variables"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
    <div class=""panel"">
        <BitSortable @bind-Items=""basket"" Group=""variables"">
            <ItemTemplate Context=""item"">
                <div class=""item"">@item</div>
            </ItemTemplate>
        </BitSortable>
    </div>
</div>";
    private readonly string example25CsharpCode = @"
private const string cssVariablesStyle = ""--bit-Sortable-ghost-opacity:0.15;"" +
                                         ""--bit-Sortable-chosen-background:var(--bit-clr-bg-sec);"" +
                                         ""--bit-Sortable-accepts-outline:2px dashed var(--bit-clr-pri);"" +
                                         ""--bit-Sortable-over-outline:2px solid var(--bit-clr-pri);"";

private IList<string> styledItems = [""Styled 1"", ""Styled 2"", ""Styled 3""];
private IList<string> classedItems = [""Classy 1"", ""Classy 2"", ""Classy 3""];
private IList<string> itemStyled = [""Ordinary"", ""Important"", ""Routine""];
private IList<string> fruits = [""Apple"", ""Banana"", ""Cherry""];
private IList<string> basket = [""Fig"", ""Grape""];";

    private readonly string example26RazorCode = @"
<div dir=""rtl"" class=""panel strip"">
    <BitSortable @bind-Items=""rtlSteps"" Dir=""BitDir.Rtl"">
        <ItemTemplate Context=""step"">
            <div class=""chip"">@step</div>
        </ItemTemplate>
    </BitSortable>
</div>
<div dir=""rtl"" class=""order"">ترتیب: @string.Join(""، "", rtlSteps)</div>";
    private readonly string example26CsharpCode = @"
private IList<string> rtlSteps = [""گام ۱"", ""گام ۲"", ""گام ۳"", ""گام ۴"", ""گام ۵""];";
}
