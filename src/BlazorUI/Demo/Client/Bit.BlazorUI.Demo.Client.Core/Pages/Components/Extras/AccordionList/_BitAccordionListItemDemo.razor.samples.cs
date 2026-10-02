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
";

    private const string bodyForCsharpCode = @"
private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);
";

    private const string keyedItemsCsharpCode = @"
private readonly List<BitAccordionListItem> keyedItems =
[
    new() { Key = ""general"", Title = ""General settings"", Description = ""The general settings of the application"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Key = ""users"", Title = ""Users"", Description = ""You are currently not an owner"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Key = ""advanced"", Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"", Body = BodyFor(""In the beginning, there is silence, ..."") },
];
";

    private const string iconItemsCsharpCode = @"
private readonly List<BitAccordionListItem> iconItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", IconName = BitIconName.Settings, ExpanderIconName = BitIconName.ChevronDownSmall, Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", IconName = BitIconName.Contact, ExpanderIconName = BitIconName.ChevronDownSmall, Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"", IconName = BitIconName.Ringer, Body = BodyFor(""In the beginning, there is silence, ..."") },
];
";

    private const string templateItemsCsharpCode = @"
private readonly List<BitAccordionListItem> templateItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"" },
    new() { Title = ""Users"", Description = ""You are currently not an owner"" },
    new() { Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"" },
];
";

    private const string stateItemsCsharpCode = @"
private readonly List<BitAccordionListItem> stateItems =
[
    new() { Key = ""normal"", Title = ""General settings"", Description = ""A live item"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Key = ""disabled"", Title = ""Users"", Description = ""Turned off altogether"", IsEnabled = false, Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Key = ""locked"", Title = ""Advanced settings"", Description = ""Open on purpose and staying that way"", ReadOnly = true, Body = BodyFor(""In the beginning, there is silence, ..."") },
];
";

    private const string eventsItemsCsharpCode = @"
private readonly List<BitAccordionListItem> eventsItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Title = ""Advanced settings"", Description = ""Filtering has been entirely disabled"", Body = BodyFor(""In the beginning, there is silence, ..."") },
];
";

    private const string lazyItemsCsharpCode = @"
private readonly List<BitAccordionListItem> lazyItems =
[
    new() { Title = ""Lazy panel"", Description = ""Rendered on its first open, and kept afterwards"", Body = TimestampBody() },
];
";

    private const string unmountItemsCsharpCode = @"
private readonly List<BitAccordionListItem> unmountItems =
[
    new() { Title = ""Unmounted panel"", Description = ""Rendered again on every open"", Body = TimestampBody() },
];
";

    private const string timestampBodyCsharpCode = @"
private static RenderFragment<BitAccordionListItem> TimestampBody() => item => builder =>
{
    builder.AddContent(0, $""This panel was rendered at {DateTime.Now:HH:mm:ss.fff}"");
};
";

    private const string longItemsCsharpCode = @"
private readonly List<BitAccordionListItem> longItems =
[
    new() { Key = ""long-1"", Title = ""A long panel"", Description = ""Scrolls inside the item"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Key = ""long-2"", Title = ""Another long panel"", Description = ""Scrolls inside the item"", Body = BodyFor(""In the beginning, there is silence, ..."") },
];
";

    private const string faqItemsCsharpCode = @"
private readonly List<BitAccordionListItem> faqItems =
[
    new() { Title = ""How do I reset my password?"", Description = ""Account"", Body = BodyFor(""Open Settings, choose Security and pick Reset password; the link we email you expires after one hour."") },
    new() { Title = ""Can I get a refund?"", Description = ""Billing"", Body = BodyFor(""Refunds are issued within 14 days, to the payment method the order was paid with."") },
    new() { Title = ""Where is my invoice?"", Description = ""Billing"", Body = BodyFor(""Every invoice is listed under Billing, ready to download as a PDF."") },
];
";

    private const string scrollItemsCsharpCode = @"
private readonly List<BitAccordionListItem> scrollItems =
[
    new() { Title = ""First section"", Description = ""Opens without moving anything"", Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Second section"", Description = ""Sits just below the fold"", Body = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Title = ""Third section"", Description = ""Is scrolled to when it opens"", Body = BodyFor(""In the beginning, there is silence, ..."") },
    new() { Title = ""Fourth section"", Description = ""Is scrolled to when it opens"", Body = BodyFor(""Once upon a time, ..."") },
];
";

    private const string faItemsCsharpCode = @"
private readonly List<BitAccordionListItem> faItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Icon = BitIconInfo.Fa(""solid gear""), Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Icon = BitIconInfo.Fa(""solid user""), Body = BodyFor(""Every story starts with a blank canvas, ..."") },
];
";

    private const string biItemsCsharpCode = @"
private readonly List<BitAccordionListItem> biItems =
[
    new() { Title = ""General settings"", Description = ""The general settings of the application"", Icon = BitIconInfo.Bi(""gear""), Body = BodyFor(""Once upon a time, ..."") },
    new() { Title = ""Users"", Description = ""You are currently not an owner"", Icon = BitIconInfo.Bi(""person""), Body = BodyFor(""Every story starts with a blank canvas, ..."") },
];
";

    private const string rtlItemsCsharpCode = @"
private readonly List<BitAccordionListItem> rtlItems =
[
    new() { Title = ""تنظیمات عمومی"", Description = ""تنظیمات کلی برنامه"", Body = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."") },
    new() { Title = ""کاربران"", Description = ""شما در حال حاضر مالک نیستید"", Body = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."") },
];
";

    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example1CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Multiple MaxExpanded=""2"" Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example2CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example3RazorCode = @"
<BitAccordionList DefaultExpandedKey=""users"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Multiple
                  DefaultExpandedKeys=""@([""general"", ""advanced""])""
                  Items=""keyedItems""
                  TItem=""BitAccordionListItem"" />

<BitAccordionList Collapsible=""false"" DefaultExpandedKey=""general"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example3CsharpCode = keyedItemsCsharpCode + bodyForCsharpCode;

    private readonly string example4RazorCode = @"
<BitAccordionList Items=""iconItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList ExpanderIconName=""@BitIconName.Add""
                  ExpandedExpanderIconName=""@BitIconName.Remove""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />

<BitAccordionList ExpanderIconPosition=""BitIconPosition.Start"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList HideExpanderIcon Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example4CsharpCode = iconItemsCsharpCode + basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example5RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"">
    <ActionsTemplate Context=""item"">
        <BitButton IconOnly
                   Variant=""BitVariant.Text""
                   IconName=""@BitIconName.MoreVertical""
                   Title=""@($""More about {item.Title}"")""
                   OnClick=""() => actionedTitle = item.Title"" />
    </ActionsTemplate>
</BitAccordionList>

<div>Last action: <b>@actionedTitle</b></div>";
    private readonly string example5CsharpCode = @"
private string? actionedTitle;
" + basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example6RazorCode = @"
<BitAccordionList Items=""templateItems"" TItem=""BitAccordionListItem"">
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
    private readonly string example6CsharpCode = templateItemsCsharpCode + basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example7RazorCode = @"
<BitAccordionList Multiple
                  DefaultExpandedKeys=""@([""locked""])""
                  OnItemClick=""(BitAccordionListItem item) => { if (item.ReadOnly is true) readOnlyClickCount++; }""
                  Items=""stateItems""
                  TItem=""BitAccordionListItem"" />

<div>Clicks on the read-only header: <b>@readOnlyClickCount</b></div>";
    private readonly string example7CsharpCode = @"
private int readOnlyClickCount;
" + stateItemsCsharpCode + bodyForCsharpCode;

    private readonly string example8RazorCode = @"
<BitAccordionList OnExpand=""(BitAccordionListItem item) => expandedTitle = item.Title""
                  OnCollapse=""(BitAccordionListItem item) => collapsedTitle = item.Title""
                  OnToggle=""(BitAccordionListItem item) => toggledTitle = item.Title""
                  Items=""eventsItems""
                  TItem=""BitAccordionListItem"" />

<div>Last expanded: <b>@expandedTitle</b></div>
<div>Last collapsed: <b>@collapsedTitle</b></div>
<div>Last toggled: <b>@toggledTitle</b></div>
<div>Header clicks: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;

protected override void OnInitialized()
{
    foreach (var item in eventsItems)
    {
        item.OnClick = _ => { clickCounter++; StateHasChanged(); };
    }
}
" + eventsItemsCsharpCode + bodyForCsharpCode;

    private readonly string example9RazorCode = @"
<BitCheckbox @bind-Value=""lockToggling"" Label=""Refuse every toggle"" />
<BitCheckbox @bind-Value=""slowToggling"" Label=""Take a second to decide"" />

<BitAccordionList OnToggling=""HandleOnToggling"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<div>Last request: <b>@togglingReport</b></div>";
    private readonly string example9CsharpCode = @"
private bool lockToggling;
private bool slowToggling;
private string? togglingReport;

private async Task HandleOnToggling(BitAccordionListToggleArgs<BitAccordionListItem> args)
{
    togglingReport = $""{args.Item.Title} is {(args.IsExpanding ? ""expanding"" : ""collapsing"")} ({args.Reason})"";

    // The header reports itself as aria-busy for as long as the callback is awaited.
    if (slowToggling)
    {
        await Task.Delay(1000);
    }

    args.Cancel = lockToggling;
}
" + basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example10RazorCode = @"
<BitButtonGroup Toggle Items=""bindingButtons"" TItem=""BitButtonGroupItem"" @bind-ToggleKey=""boundExpandedKey"" />

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />

<BitStack Horizontal Wrap>
    <BitButton OnClick=""@(() => accordionListRef!.ExpandAll())"">Expand all</BitButton>
    <BitButton OnClick=""@(() => accordionListRef!.CollapseAll())"">Collapse all</BitButton>
    <BitButton OnClick=""@(() => accordionListRef!.Toggle(""users""))"">Toggle Users</BitButton>
    <BitButton OnClick=""@(() => accordionListRef!.FocusItem(""advanced""))"">Focus Advanced</BitButton>
</BitStack>

<BitAccordionList @ref=""accordionListRef""
                  Multiple
                  @bind-ExpandedKeys=""programmaticKeys""
                  Items=""keyedItems""
                  TItem=""BitAccordionListItem"" />

<div>Expanded keys: <b>@string.Join("", "", programmaticKeys)</b></div>";
    private readonly string example10CsharpCode = @"
private string? boundExpandedKey = ""users"";
private IEnumerable<string> programmaticKeys = [];
private BitAccordionList<BitAccordionListItem>? accordionListRef;

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Key = ""general"", Text = ""General"" },
    new() { Key = ""users"", Text = ""Users"" },
    new() { Key = ""advanced"", Text = ""Advanced"" },
];
" + keyedItemsCsharpCode + bodyForCsharpCode;

    private readonly string example11RazorCode = @"
<BitAccordionList Multiple LazyContent Items=""lazyItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Multiple UnmountOnCollapse Items=""unmountItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example11CsharpCode = lazyItemsCsharpCode + unmountItemsCsharpCode + timestampBodyCsharpCode;

    private readonly string example12RazorCode = @"
<BitAccordionList MaxHeight=""100px"" DefaultExpandedKey=""long-1"" Items=""longItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList TransitionDuration=""0"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList TransitionDuration=""1500"" Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example12CsharpCode = longItemsCsharpCode + basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example13RazorCode = @"
<div>Search the page (Ctrl+F) for <b>refund</b> or <b>invoice</b>, or open the print preview (Ctrl+P).</div>

<BitAccordionList HiddenUntilFound ExpandOnPrint Items=""faqItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example13CsharpCode = faqItemsCsharpCode + bodyForCsharpCode;

    private readonly string example14RazorCode = @"
<div>Open the last panels: the box follows them.</div>

<div class=""scroll-box"">
    <BitAccordionList ScrollIntoViewOnExpand Items=""scrollItems"" TItem=""BitAccordionListItem"" />
</div>";
    private readonly string example14CsharpCode = scrollItemsCsharpCode + bodyForCsharpCode;

    private readonly string example15RazorCode = @"
<BitAccordionList Joined Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Joined NoBorder Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Gap=""16"" NoBorder Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example15CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example16RazorCode = @"
<BitCheckbox @bind-Value=""showEmptyItems"" Label=""Show the items"" />

<BitAccordionList Items=""@(showEmptyItems ? basicItems : noItems)"" TItem=""BitAccordionListItem"">
    <EmptyContent>
        <BitText Typography=""BitTypography.Body2"">There is nothing to show here yet.</BitText>
    </EmptyContent>
</BitAccordionList>";
    private readonly string example16CsharpCode = @"
private bool showEmptyItems;
" + basicItemsCsharpCode + @"
private readonly List<BitAccordionListItem> noItems = [];
" + bodyForCsharpCode;

    private readonly string example17RazorCode = @"
<BitAccordionList Multiple
                  HeadingLevel=""2""
                  NoContentRegion
                  NoNavigationLoop
                  AriaLabel=""Application settings""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />";
    private readonly string example17CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example18RazorCode = @"
<BitParams Parameters=""@accordionListParams"">
    <BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" />
    
    <BitAccordionList Joined=""false"" Items=""basicItems"" TItem=""BitAccordionListItem"" />
</BitParams>";
    private readonly string example18CsharpCode = @"
private readonly BitAccordionListParams[] accordionListParams =
[
    new()
    {
        Joined = true,
        Multiple = true,
        ExpanderIconName = BitIconName.Add,
        ExpandedExpanderIconName = BitIconName.Remove,
    }
];
" + basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example19RazorCode = @"
<BitAccordionList Background=""BitColorKind.Secondary""
                  Border=""BitColorKind.Tertiary""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />

<BitAccordionList Background=""BitColorKind.Tertiary""
                  Border=""BitColorKind.Transparent""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />";
    private readonly string example19CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example20RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitAccordionList ExpanderIcon=""@BitIconInfo.Fa(""solid angle-down"")"" Items=""faItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList ExpanderIcon=""@BitIconInfo.Bi(""chevron-down"")"" Items=""biItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example20CsharpCode = faItemsCsharpCode + biItemsCsharpCode + bodyForCsharpCode;

    private readonly string example21RazorCode = @"
<BitAccordionList Size=""BitSize.Small"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Size=""BitSize.Medium"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Size=""BitSize.Large"" Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example21CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example22RazorCode = @"
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

<BitAccordionList Gap=""8""
                  Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />

<BitAccordionList Gap=""8"" Class=""custom-item"" Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />

<BitAccordionList Classes=""@(new() { ItemTitle = ""custom-title"", ItemExpanded = ""custom-expanded"" })""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />

<BitAccordionList Style=""--bit-AccordionList-gap: 2px; --bit-Accordion-radius: 0; --bit-Accordion-title-color: var(--bit-clr-pri); --bit-Accordion-header-expanded-background: var(--bit-clr-bg-sec);""
                  Items=""basicItems""
                  TItem=""BitAccordionListItem"" />";
    private readonly string example22CsharpCode = basicItemsCsharpCode + bodyForCsharpCode;

    private readonly string example23RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" Items=""rtlItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example23CsharpCode = rtlItemsCsharpCode + bodyForCsharpCode;
}
