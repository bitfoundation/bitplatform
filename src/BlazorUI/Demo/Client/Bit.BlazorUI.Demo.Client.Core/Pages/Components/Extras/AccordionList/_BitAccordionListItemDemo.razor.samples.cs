namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListItemDemo
{
    private const string basicItemsCsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"", Body = BodyFor(""In the beginning, there is silence, ..."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private const string keyedItemsCsharpCode = @"
private readonly List<BitAccordionListItem> keyedItems =
[
    new() { Key = ""general"", Title = ""General settings"", Description = ""The general settings of the application"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Key = ""users"", Title = ""Users"", Description = ""You are currently not an owner"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Key = ""advanced"", Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"", Body = BodyFor(""In the beginning, there is silence, ..."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";


    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example1CsharpCode = basicItemsCsharpCode;

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Multiple MaxExpanded=""2"" Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example2CsharpCode = basicItemsCsharpCode;

    private readonly string example3RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""BitAccordionListItem"" DefaultExpandedKey=""users"" />

<BitAccordionList Multiple Items=""keyedItems"" TItem=""BitAccordionListItem"" DefaultExpandedKeys=""@([""general"", ""advanced""])"" />";
    private readonly string example3CsharpCode = keyedItemsCsharpCode;

    private readonly string example4RazorCode = @"
<BitAccordionList Items=""keyedItems""
                  TItem=""BitAccordionListItem""
                  Collapsible=""false""
                  DefaultExpandedKey=""general"" />";
    private readonly string example4CsharpCode = keyedItemsCsharpCode;

    private readonly string example5RazorCode = @"
<BitAccordionList Items=""iconItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  ExpanderIconName=""@BitIconName.Add""
                  ExpandedExpanderIconName=""@BitIconName.Remove"" />

<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" ExpanderIconPosition=""BitIconPosition.Start"" />

<BitAccordionList HideExpanderIcon Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitAccordionListItem> iconItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        IconName = BitIconName.Settings,
        ExpanderIconName = BitIconName.ChevronDownSmall,
        Body = BodyFor(""Once upon a time, ..."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        IconName = BitIconName.Contact,
        ExpanderIconName = BitIconName.ChevronDownSmall,
        Body = BodyFor(""Every story starts with a blank canvas, ..."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        IconName = BitIconName.Ringer,
        Body = BodyFor(""In the beginning, there is silence, ..."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example6RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""BitAccordionListItem"">
    <ActionsTemplate Context=""item"">
        <BitButton IconOnly
                   Variant=""BitVariant.Text""
                   IconName=""@BitIconName.MoreVertical""
                   Title=""@($""More about {item.Title}"")""
                   OnClick=""() => actionedTitle = item.Title"" />
    </ActionsTemplate>
</BitAccordionList>

<div>Last action: <b>@actionedTitle</b></div>";
    private readonly string example6CsharpCode = @"
private string? actionedTitle;

// An item can also carry its own actions, which take precedence over the ActionsTemplate:
// new BitAccordionListItem { Title = ""Users"", Actions = item => @<BitIcon IconName=""@BitIconName.Lock"" /> }
" + keyedItemsCsharpCode;

    private readonly string example7RazorCode = @"
<BitAccordionList Multiple
                  Items=""stateItems""
                  TItem=""BitAccordionListItem""
                  DefaultExpandedKeys=""@([""locked""])""
                  OnItemClick=""(BitAccordionListItem item) => { if (item.ReadOnly is true) readOnlyClickCount++; }"" />

<div>Clicks on the read-only header: <b>@readOnlyClickCount</b></div>";
    private readonly string example7CsharpCode = @"
private int readOnlyClickCount;

private readonly List<BitAccordionListItem> stateItems =
[
    new() { Key = ""normal"", Title = ""General settings"", Description = ""A live item"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Key = ""disabled"", Title = ""Users"", Description = ""Turned off altogether"", IsEnabled = false, Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Key = ""locked"", Title = ""Advanced settings"", Description = ""Open on purpose and staying that way"", ReadOnly = true, Body = BodyFor(""In the beginning, there is silence, ..."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example8RazorCode = @"
<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  OnExpand=""(BitAccordionListItem item) => expandedTitle = item.Title""
                  OnCollapse=""(BitAccordionListItem item) => collapsedTitle = item.Title""
                  OnToggle=""(BitAccordionListItem item) => toggledTitle = item.Title"" />

<div>Last expanded: <b>@expandedTitle</b></div>
<div>Last collapsed: <b>@collapsedTitle</b></div>
<div>Last toggled: <b>@toggledTitle</b></div>

<BitAccordionList Items=""eventsItems"" TItem=""BitAccordionListItem"" />
<div>Item click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;

private readonly List<BitAccordionListItem> eventsItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"", Body = BodyFor(""In the beginning, there is silence, ..."") },
];

protected override void OnInitialized()
{
    foreach (var item in eventsItems)
    {
        item.OnClick = _ => { clickCounter++; StateHasChanged(); };
    }
}

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example9RazorCode = @"
<BitCheckbox @bind-Value=""lockToggling"" Label=""Refuse every toggle"" />
<BitCheckbox @bind-Value=""slowToggling"" Label=""Take a second to decide"" />

<BitAccordionList Items=""keyedItems"" TItem=""BitAccordionListItem"" OnToggling=""HandleOnToggling"" />

<div>Last request: <b>@togglingReport</b></div>";
    private readonly string example9CsharpCode = @"
private bool lockToggling;
private bool slowToggling;
private string? togglingReport;

private async Task HandleOnToggling(BitAccordionListToggleArgs<BitAccordionListItem> args)
{
    togglingReport = $""{args.Item.Title} is {(args.IsExpanding ? ""expanding"" : ""collapsing"")} ({args.Reason})"";

    // The header of this item reports itself as aria-busy for as long as the callback is awaited.
    if (slowToggling)
    {
        await Task.Delay(1000);
    }

    args.Cancel = lockToggling;
}
" + keyedItemsCsharpCode;

    private readonly string example10RazorCode = @"
<BitButtonGroup Toggle Items=""bindingButtons"" TItem=""BitButtonGroupItem"" @bind-ToggleKey=""boundExpandedKey"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />

<div>Bound expanded keys: <b>@string.Join("", "", boundExpandedKeys)</b></div>

<BitAccordionList Multiple @bind-ExpandedKeys=""boundExpandedKeys"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example10CsharpCode = @"
private string? boundExpandedKey = ""users"";
private IEnumerable<string> boundExpandedKeys = [""general""];

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Key = ""general"", Text = ""General"" },
    new() { Key = ""users"", Text = ""Users"" },
    new() { Key = ""advanced"", Text = ""Advanced"" },
];
" + keyedItemsCsharpCode;

    private readonly string example11RazorCode = @"
<BitButton OnClick=""@(() => accordionListRef!.ExpandAll())"">Expand all</BitButton>
<BitButton OnClick=""@(() => accordionListRef!.CollapseAll())"">Collapse all</BitButton>
<BitButton OnClick=""@(() => accordionListRef!.Toggle(""users""))"">Toggle Users</BitButton>
<BitButton OnClick=""@(() => accordionListRef!.FocusItem(""advanced""))"">Focus Advanced</BitButton>

<BitAccordionList @ref=""accordionListRef"" Multiple @bind-ExpandedKeys=""programmaticKeys"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />

<div>Expanded keys: <b>@string.Join("", "", programmaticKeys)</b></div>";
    private readonly string example11CsharpCode = @"
private IEnumerable<string> programmaticKeys = [];
private BitAccordionList<BitAccordionListItem>? accordionListRef;

// The same state can also be read back without a binding:
// accordionListRef.IsExpanded(""users""); accordionListRef.GetExpandedKeys();
" + keyedItemsCsharpCode;

    private readonly string example12RazorCode = @"
<BitAccordionList Multiple LazyContent Items=""lazyItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Multiple UnmountOnCollapse Items=""unmountItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Items=""longItems"" TItem=""BitAccordionListItem"" MaxHeight=""100px"" DefaultExpandedKey=""long-1"" />";
    private readonly string example12CsharpCode = @"
private readonly List<BitAccordionListItem> lazyItems =
[
    new() { Key = ""lazy-1"", Title = ""Lazy panel"", Description = ""Rendered the first time it is opened, and kept afterwards"", Body = TimestampBody() },
];

private readonly List<BitAccordionListItem> unmountItems =
[
    new() { Key = ""unmount-1"", Title = ""Unmounted panel"", Description = ""Rendered again on every open"", Body = TimestampBody() },
];

private readonly List<BitAccordionListItem> longItems =
[
    new() { Key = ""long-1"", Title = ""A long panel"", Description = ""Scrolls inside the item"", Body = BodyFor(""a very long text ..."") },
    new() { Key = ""long-2"", Title = ""Another long panel"", Description = ""Scrolls inside the item"", Body = BodyFor(""a very long text ..."") },
];

private static RenderFragment<BitAccordionListItem> TimestampBody() => item => builder =>
{
    builder.AddContent(0, $""This panel was rendered at {DateTime.Now:HH:mm:ss.fff}"");
};

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example13RazorCode = @"
<BitAccordionList TransitionDuration=""0"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList TransitionDuration=""1500"" Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example13CsharpCode = basicItemsCsharpCode;

    private readonly string example14RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"">
    <HeaderTemplate Context=""item"">
        <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
        <b>@item.Title</b>
    </HeaderTemplate>
    <BodyTemplate Context=""item"">
        <BitText Typography=""BitTypography.Caption1"">@item.Description</BitText>
    </BodyTemplate>
</BitAccordionList>

<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"">
    <TitleTemplate Context=""item"">
        <BitTag Text=""@item.Title"" Color=""BitColor.SecondaryBackground"" />
    </TitleTemplate>
    <ExpanderTemplate Context=""item"">
        <BitIcon IconName=""@BitIconName.ChevronDownSmall"" />
    </ExpanderTemplate>
</BitAccordionList>";
    private readonly string example14CsharpCode = basicItemsCsharpCode;

    private readonly string example15RazorCode = @"
<BitAccordionList Multiple
                  Items=""keyedItems""
                  TItem=""BitAccordionListItem""
                  HeadingLevel=""2""
                  NoContentRegion
                  NoNavigationLoop
                  AriaLabel=""Application settings"" />";
    private readonly string example15CsharpCode = keyedItemsCsharpCode;

    private readonly string example16RazorCode = @"
<BitAccordionList ExpandOnPrint Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example16CsharpCode = basicItemsCsharpCode;

    private readonly string example17RazorCode = @"
<BitAccordionList Gap=""0"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList NoBorder Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example17CsharpCode = basicItemsCsharpCode;

    private readonly string example18RazorCode = @"
<BitCheckbox @bind-Value=""showEmptyItems"" Label=""Show the items"" />

<BitAccordionList Items=""@(showEmptyItems ? basicItems : noItems)"" TItem=""BitAccordionListItem"">
    <EmptyContent>
        <BitText Typography=""BitTypography.Body2"">There is nothing to show here yet.</BitText>
    </EmptyContent>
</BitAccordionList>";
    private readonly string example18CsharpCode = @"
private bool showEmptyItems;

private readonly List<BitAccordionListItem> noItems = [];
" + basicItemsCsharpCode;

    private readonly string example19RazorCode = @"
<div class=""scroll-box"">
    <BitAccordionList ScrollIntoViewOnExpand Items=""scrollItems"" TItem=""BitAccordionListItem"" />
</div>";
    private readonly string example19CsharpCode = @"
private readonly List<BitAccordionListItem> scrollItems =
[
    new() { Key = ""scroll-1"", Title = ""First section"", Description = ""Opens without moving anything"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Key = ""scroll-2"", Title = ""Second section"", Description = ""Sits just below the fold"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Key = ""scroll-3"", Title = ""Third section"", Description = ""Is scrolled to when it opens"", Body = BodyFor(""In the beginning, there is silence, ..."") },
    new() { Key = ""scroll-4"", Title = ""Fourth section"", Description = ""Is scrolled to when it opens"", Body = BodyFor(""Once upon a time, ..."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example20RazorCode = @"
<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Background=""BitColorKind.Secondary""
                  Border=""BitColorKind.Tertiary"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Background=""BitColorKind.Tertiary""
                  Border=""BitColorKind.Transparent"" />";
    private readonly string example20CsharpCode = basicItemsCsharpCode;

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitAccordionList Items=""faItems"" TItem=""BitAccordionListItem"" ExpanderIcon=""@BitIconInfo.Fa(""solid angle-down"")"" />

<BitAccordionList Items=""biItems"" TItem=""BitAccordionListItem"" ExpanderIcon=""@BitIconInfo.Bi(""chevron-down"")"" />";
    private readonly string example21CsharpCode = @"
private readonly List<BitAccordionListItem> faItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Icon = BitIconInfo.Fa(""solid gear""), Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Icon = BitIconInfo.Fa(""solid user""), Body = BodyFor(""Every story starts with a blank canvas, ..."") },
];

private readonly List<BitAccordionListItem> biItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Icon = BitIconInfo.Bi(""gear""), Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Icon = BitIconInfo.Bi(""person""), Body = BodyFor(""Every story starts with a blank canvas, ..."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example22RazorCode = @"
<BitAccordionList Size=""BitSize.Small"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Size=""BitSize.Medium"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Size=""BitSize.Large"" Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example22CsharpCode = basicItemsCsharpCode;

    private readonly string example23RazorCode = @"
<style>
    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }

    .custom-title {
        color: tomato;
        font-style: italic;
    }

    .custom-expanded {
        border-color: tomato;
    }
</style>

<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" />
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" Gap=""8"" Class=""custom-item"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" />
<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Classes=""@(new() { ItemTitle = ""custom-title"", ItemExpanded = ""custom-expanded"" })"" />";
    private readonly string example23CsharpCode = basicItemsCsharpCode;

    private readonly string example24RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" Items=""rtlItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example24CsharpCode = @"
private readonly List<BitAccordionListItem> rtlItems =
[
    new() { Title = ""تنظیمات عمومی"", Description = ""تنظیمات کلی برنامه"", Body = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."") },
    new() { Title = ""کاربران"", Description = ""شما در حال حاضر مالک نیستید"", Body = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";
}
