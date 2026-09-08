namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListCustomDemo
{
    private const string sectionCsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool? Locked { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public string? Glyph { get; set; }
    public BitIconInfo? CustomGlyph { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public RenderFragment<Section>? Extra { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ReadOnly = { Selector = i => i.Locked },
    ExpanderIconName = { Selector = i => i.Image },
    IconName = { Selector = i => i.Glyph },
    Icon = { Selector = i => i.CustomGlyph },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Actions = { Selector = i => i.Extra },
    Body = { Selector = i => i.Content },
};

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private const string basicItemsCsharpCode = @"
private readonly List<Section> basicItems =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings of the application"", Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", Content = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Id = ""advanced"", Name = ""Advanced settings"", Info = ""Filtering has been entirely disabled"", Content = BodyFor(""In the beginning, there is silence, ..."") },
];
" + sectionCsharpCode;

    private const string keyedItemsCsharpCode = @"
private readonly List<Section> keyedItems =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings of the application"", Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", Content = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Id = ""advanced"", Name = ""Advanced settings"", Info = ""Filtering has been entirely disabled"", Content = BodyFor(""In the beginning, there is silence, ..."") },
];
" + sectionCsharpCode;


    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = basicItemsCsharpCode;

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Multiple MaxExpanded=""2"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example2CsharpCode = basicItemsCsharpCode;

    private readonly string example3RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" DefaultExpandedKey=""users"" />

<BitAccordionList Multiple Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" DefaultExpandedKeys=""@([""general"", ""advanced""])"" />";
    private readonly string example3CsharpCode = keyedItemsCsharpCode;

    private readonly string example4RazorCode = @"
<BitAccordionList Items=""keyedItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Collapsible=""false""
                  DefaultExpandedKey=""general"" />";
    private readonly string example4CsharpCode = keyedItemsCsharpCode;

    private readonly string example5RazorCode = @"
<BitAccordionList Items=""iconItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  ExpanderIconName=""@BitIconName.Add""
                  ExpandedExpanderIconName=""@BitIconName.Remove"" />

<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" ExpanderIconPosition=""BitIconPosition.Start"" />

<BitAccordionList HideExpanderIcon Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example5CsharpCode = @"
private readonly List<Section> iconItems =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings of the application"", Glyph = BitIconName.Settings, Image = BitIconName.ChevronDownSmall, Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", Glyph = BitIconName.Contact, Image = BitIconName.ChevronDownSmall, Content = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Id = ""advanced"", Name = ""Advanced settings"", Info = ""Filtering has been entirely disabled"", Glyph = BitIconName.Ringer, Content = BodyFor(""In the beginning, there is silence, ..."") },
];
" + basicItemsCsharpCode;

    private readonly string example6RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"">
    <ActionsTemplate Context=""item"">
        <BitButton IconOnly
                   Variant=""BitVariant.Text""
                   IconName=""@BitIconName.MoreVertical""
                   Title=""@($""More about {item.Name}"")""
                   OnClick=""() => actionedTitle = item.Name"" />
    </ActionsTemplate>
</BitAccordionList>

<div>Last action: <b>@actionedTitle</b></div>";
    private readonly string example6CsharpCode = @"
private string? actionedTitle;

// An item can also carry its own actions through the member mapped to Actions (Section.Extra here),
// which take precedence over the ActionsTemplate.
" + keyedItemsCsharpCode;

    private readonly string example7RazorCode = @"
<BitAccordionList Multiple
                  Items=""stateItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  DefaultExpandedKeys=""@([""locked""])""
                  OnItemClick=""(Section item) => { if (item.Locked is true) readOnlyClickCount++; }"" />

<div>Clicks on the read-only header: <b>@readOnlyClickCount</b></div>";
    private readonly string example7CsharpCode = @"
private int readOnlyClickCount;

private readonly List<Section> stateItems =
[
    new() { Id = ""normal"", Name = ""General settings"", Info = ""A live item"", Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""disabled"", Name = ""Users"", Info = ""Turned off altogether"", IsEnabled = false, Content = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Id = ""locked"", Name = ""Advanced settings"", Info = ""Open on purpose and staying that way"", Locked = true, Content = BodyFor(""In the beginning, there is silence, ..."") },
];
" + sectionCsharpCode;

    private readonly string example8RazorCode = @"
<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  OnExpand=""(Section item) => expandedTitle = item.Name""
                  OnCollapse=""(Section item) => collapsedTitle = item.Name""
                  OnToggle=""(Section item) => toggledTitle = item.Name"" />

<div>Last expanded: <b>@expandedTitle</b></div>
<div>Last collapsed: <b>@collapsedTitle</b></div>
<div>Last toggled: <b>@toggledTitle</b></div>

<BitAccordionList Items=""eventsItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />
<div>Item click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;

private readonly List<Section> eventsItems =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings of the application"", Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", Content = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Id = ""advanced"", Name = ""Advanced settings"", Info = ""Filtering has been entirely disabled"", Content = BodyFor(""In the beginning, there is silence, ..."") },
];

protected override void OnInitialized()
{
    foreach (var item in eventsItems)
    {
        item.Clicked = _ => { clickCounter++; StateHasChanged(); };
    }
}
" + sectionCsharpCode;

    private readonly string example9RazorCode = @"
<BitCheckbox @bind-Value=""lockToggling"" Label=""Refuse every toggle"" />
<BitCheckbox @bind-Value=""slowToggling"" Label=""Take a second to decide"" />

<BitAccordionList Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" OnToggling=""HandleOnToggling"" />

<div>Last request: <b>@togglingReport</b></div>";
    private readonly string example9CsharpCode = @"
private bool lockToggling;
private bool slowToggling;
private string? togglingReport;

private async Task HandleOnToggling(BitAccordionListToggleArgs<Section> args)
{
    togglingReport = $""{args.Item.Name} is {(args.IsExpanding ? ""expanding"" : ""collapsing"")} ({args.Reason})"";

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

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<div>Bound expanded keys: <b>@string.Join("", "", boundExpandedKeys)</b></div>

<BitAccordionList Multiple @bind-ExpandedKeys=""boundExpandedKeys"" Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
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

<BitAccordionList @ref=""accordionListRef"" Multiple @bind-ExpandedKeys=""programmaticKeys"" Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<div>Expanded keys: <b>@string.Join("", "", programmaticKeys)</b></div>";
    private readonly string example11CsharpCode = @"
private IEnumerable<string> programmaticKeys = [];
private BitAccordionList<Section>? accordionListRef;

// The same state can also be read back without a binding:
// accordionListRef.IsExpanded(""users""); accordionListRef.GetExpandedKeys();
" + keyedItemsCsharpCode;

    private readonly string example12RazorCode = @"
<BitAccordionList Multiple LazyContent Items=""lazyItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Multiple UnmountOnCollapse Items=""unmountItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Items=""longItems"" TItem=""Section"" NameSelectors=""nameSelectors"" MaxHeight=""100px"" DefaultExpandedKey=""long-1"" />";
    private readonly string example12CsharpCode = @"
private readonly List<Section> lazyItems =
[
    new() { Id = ""lazy-1"", Name = ""Lazy panel"", Info = ""Rendered the first time it is opened, and kept afterwards"", Content = TimestampBody() },
];

private readonly List<Section> unmountItems =
[
    new() { Id = ""unmount-1"", Name = ""Unmounted panel"", Info = ""Rendered again on every open"", Content = TimestampBody() },
];

private readonly List<Section> longItems =
[
    new() { Id = ""long-1"", Name = ""A long panel"", Info = ""Scrolls inside the item"", Content = BodyFor(""a very long text ..."") },
    new() { Id = ""long-2"", Name = ""Another long panel"", Info = ""Scrolls inside the item"", Content = BodyFor(""a very long text ..."") },
];

private static RenderFragment<Section> TimestampBody() => section => builder =>
{
    builder.AddContent(0, $""This panel was rendered at {DateTime.Now:HH:mm:ss.fff}"");
};
" + sectionCsharpCode;

    private readonly string example13RazorCode = @"
<BitAccordionList TransitionDuration=""0"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList TransitionDuration=""1500"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example13CsharpCode = basicItemsCsharpCode;

    private readonly string example14RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"">
    <HeaderTemplate Context=""item"">
        <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
        <b>@item.Name</b>
    </HeaderTemplate>
    <BodyTemplate Context=""item"">
        <BitText Typography=""BitTypography.Caption1"">@item.Info</BitText>
    </BodyTemplate>
</BitAccordionList>

<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"">
    <TitleTemplate Context=""item"">
        <BitTag Text=""@item.Name"" Color=""BitColor.SecondaryBackground"" />
    </TitleTemplate>
    <ExpanderTemplate Context=""item"">
        <BitIcon IconName=""@BitIconName.ChevronDownSmall"" />
    </ExpanderTemplate>
</BitAccordionList>";
    private readonly string example14CsharpCode = basicItemsCsharpCode;

    private readonly string example15RazorCode = @"
<BitAccordionList Multiple
                  Items=""keyedItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  HeadingLevel=""2""
                  NoContentRegion
                  NoNavigationLoop
                  AriaLabel=""Application settings"" />";
    private readonly string example15CsharpCode = keyedItemsCsharpCode;

    private readonly string example16RazorCode = @"
<BitAccordionList ExpandOnPrint Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example16CsharpCode = basicItemsCsharpCode;

    private readonly string example17RazorCode = @"
<BitAccordionList Gap=""0"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList NoBorder Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example17CsharpCode = basicItemsCsharpCode;

    private readonly string example18RazorCode = @"
<BitCheckbox @bind-Value=""showEmptyItems"" Label=""Show the items"" />

<BitAccordionList Items=""@(showEmptyItems ? basicItems : noItems)"" TItem=""Section"" NameSelectors=""nameSelectors"">
    <EmptyContent>
        <BitText Typography=""BitTypography.Body2"">There is nothing to show here yet.</BitText>
    </EmptyContent>
</BitAccordionList>";
    private readonly string example18CsharpCode = @"
private bool showEmptyItems;

private readonly List<Section> noItems = [];
" + basicItemsCsharpCode;

    private readonly string example19RazorCode = @"
<div class=""scroll-box"">
    <BitAccordionList ScrollIntoViewOnExpand Items=""scrollItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />
</div>";
    private readonly string example19CsharpCode = @"
private readonly List<Section> scrollItems =
[
    new() { Id = ""scroll-1"", Name = ""First section"", Info = ""Opens without moving anything"", Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""scroll-2"", Name = ""Second section"", Info = ""Sits just below the fold"", Content = BodyFor(""Every story starts with a blank canvas, ..."") },
    new() { Id = ""scroll-3"", Name = ""Third section"", Info = ""Is scrolled to when it opens"", Content = BodyFor(""In the beginning, there is silence, ..."") },
    new() { Id = ""scroll-4"", Name = ""Fourth section"", Info = ""Is scrolled to when it opens"", Content = BodyFor(""Once upon a time, ..."") },
];
" + sectionCsharpCode;

    private readonly string example20RazorCode = @"
<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Background=""BitColorKind.Secondary""
                  Border=""BitColorKind.Tertiary"" />

<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Background=""BitColorKind.Tertiary""
                  Border=""BitColorKind.Transparent"" />";
    private readonly string example20CsharpCode = basicItemsCsharpCode;

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitAccordionList Items=""faItems"" TItem=""Section"" NameSelectors=""nameSelectors"" ExpanderIcon=""@BitIconInfo.Fa(""solid angle-down"")"" />

<BitAccordionList Items=""biItems"" TItem=""Section"" NameSelectors=""nameSelectors"" ExpanderIcon=""@BitIconInfo.Bi(""chevron-down"")"" />";
    private readonly string example21CsharpCode = @"
private readonly List<Section> faItems =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings of the application"", CustomGlyph = BitIconInfo.Fa(""solid gear""), Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", CustomGlyph = BitIconInfo.Fa(""solid user""), Content = BodyFor(""Every story starts with a blank canvas, ..."") },
];

private readonly List<Section> biItems =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings of the application"", CustomGlyph = BitIconInfo.Bi(""gear""), Content = BodyFor(""Once upon a time, ..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", CustomGlyph = BitIconInfo.Bi(""person""), Content = BodyFor(""Every story starts with a blank canvas, ..."") },
];
" + sectionCsharpCode;

    private readonly string example22RazorCode = @"
<BitAccordionList Size=""BitSize.Small"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Size=""BitSize.Medium"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Size=""BitSize.Large"" Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
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

<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" />
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" Gap=""8"" Class=""custom-item"" />

<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" />
<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Classes=""@(new() { ItemTitle = ""custom-title"", ItemExpanded = ""custom-expanded"" })"" />";
    private readonly string example23CsharpCode = basicItemsCsharpCode;

    private readonly string example24RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" Items=""rtlItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example24CsharpCode = @"
private readonly List<Section> rtlItems =
[
    new() { Id = ""general"", Name = ""تنظیمات عمومی"", Info = ""تنظیمات کلی برنامه"", Content = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."") },
    new() { Id = ""users"", Name = ""کاربران"", Info = ""شما در حال حاضر مالک نیستید"", Content = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."") },
];
" + sectionCsharpCode;
}
