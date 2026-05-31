namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListItemDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example1CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new() { Title = ""Accordion 1"", Description = ""The first item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 2"", Description = ""The second item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 3"", Description = ""The third item"", Body = BodyFor(""..."") },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new() { Title = ""Accordion 1"", Description = ""The first item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 2"", Description = ""The second item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 3"", Description = ""The third item"", Body = BodyFor(""..."") },
];";

    private readonly string example3RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""BitAccordionListItem"" DefaultExpandedKey=""users"" />

<BitAccordionList Multiple Items=""keyedItems"" TItem=""BitAccordionListItem"" DefaultExpandedKeys=""@([""general"", ""advanced""])"" />";
    private readonly string example3CsharpCode = @"
private readonly List<BitAccordionListItem> keyedItems =
[
    new() { Key = ""general"", Title = ""General settings"", Body = BodyFor(""..."") },
    new() { Key = ""users"", Title = ""Users"", Body = BodyFor(""..."") },
    new() { Key = ""advanced"", Title = ""Advanced settings"", Body = BodyFor(""..."") },
];";

    private readonly string example4RazorCode = @"
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
    private readonly string example4CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;

private List<BitAccordionListItem> eventsItems =
[
    new() { Title = ""Accordion 1"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 2"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 3"", Body = BodyFor(""..."") },
];

protected override void OnInitialized()
{
    foreach (var item in eventsItems)
    {
        item.OnClick = _ => { clickCounter++; StateHasChanged(); };
    }
}";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => accordionListRef.ExpandAll()"">Expand all</BitButton>
<BitButton OnClick=""() => accordionListRef.CollapseAll()"">Collapse all</BitButton>

<BitAccordionList @ref=""accordionListRef"" Multiple Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example5CsharpCode = @"
private BitAccordionList<BitAccordionListItem> accordionListRef = default!;";

    private readonly string example6RazorCode = @"
<BitButtonGroup Items=""bindingButtons"" TItem=""BitButtonGroupItem"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example6CsharpCode = @"
private string? boundExpandedKey = ""users"";

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Text = ""General"", OnClick = _ => boundExpandedKey = ""general"" },
    new() { Text = ""Users"", OnClick = _ => boundExpandedKey = ""users"" },
    new() { Text = ""Advanced"", OnClick = _ => boundExpandedKey = ""advanced"" },
    new() { Text = ""None"", OnClick = _ => boundExpandedKey = null },
];";

    private readonly string example7RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" ExpanderIconName=""@BitIconName.ChevronDownMed"" />

<BitAccordionList Items=""iconItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example7CsharpCode = @"
private readonly List<BitAccordionListItem> iconItems =
[
    new() { Title = ""Profile"", ExpanderIconName = BitIconName.Contact, Body = BodyFor(""..."") },
    new() { Title = ""Settings"", ExpanderIconName = BitIconName.Settings, Body = BodyFor(""..."") },
    new() { Title = ""Notifications"", ExpanderIconName = BitIconName.Ringer, Body = BodyFor(""..."") },
];";

    private readonly string example8RazorCode = @"
<BitAccordionList NoBorder Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Background=""BitColorKind.Secondary""
                  Border=""BitColorKind.Tertiary"" />";
    private readonly string example8CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new() { Title = ""Accordion 1"", Description = ""The first item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 2"", Description = ""The second item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 3"", Description = ""The third item"", Body = BodyFor(""..."") },
];";

    private readonly string example9RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"">
    <HeaderTemplate Context=""item"">
        <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
        <b>@item.Title</b>
    </HeaderTemplate>
    <BodyTemplate Context=""item"">
        <BitText Typography=""BitTypography.Caption1"">@item.Description</BitText>
    </BodyTemplate>
</BitAccordionList>";
    private readonly string example9CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new() { Title = ""Accordion 1"", Description = ""The first item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 2"", Description = ""The second item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 3"", Description = ""The third item"", Body = BodyFor(""..."") },
];";

    private readonly string example10RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" />";
    private readonly string example10CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new() { Title = ""Accordion 1"", Description = ""The first item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 2"", Description = ""The second item"", Body = BodyFor(""..."") },
    new() { Title = ""Accordion 3"", Description = ""The third item"", Body = BodyFor(""..."") },
];";

    private readonly string example11RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" Items=""rtlItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example11CsharpCode = @"
private readonly List<BitAccordionListItem> rtlItems =
[
    new() { Title = ""تنظیمات عمومی"", Description = ""تنظیمات کلی برنامه"", Body = BodyFor(""..."") },
    new() { Title = ""کاربران"", Description = ""شما در حال حاضر مالک نیستید"", Body = BodyFor(""..."") },
];";
}
