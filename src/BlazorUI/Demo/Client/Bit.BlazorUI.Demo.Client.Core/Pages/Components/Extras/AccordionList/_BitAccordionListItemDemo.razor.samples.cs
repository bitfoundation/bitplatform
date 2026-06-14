namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListItemDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example1CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example3RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""BitAccordionListItem"" DefaultExpandedKey=""users"" />

<BitAccordionList Multiple Items=""keyedItems"" TItem=""BitAccordionListItem"" DefaultExpandedKeys=""@([""general"", ""advanced""])"" />";
    private readonly string example3CsharpCode = @"
private readonly List<BitAccordionListItem> keyedItems =
[
    new()
    {
        Key = ""general"",
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Key = ""users"",
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Key = ""advanced"",
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

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

private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private List<BitAccordionListItem> eventsItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

protected override void OnInitialized()
{
    foreach (var item in eventsItems)
    {
        item.OnClick = _ => { clickCounter++; StateHasChanged(); };
    }
}

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => accordionListRef.ExpandAll()"">Expand all</BitButton>
<BitButton OnClick=""() => accordionListRef.CollapseAll()"">Collapse all</BitButton>

<BitAccordionList @ref=""accordionListRef"" Multiple Items=""basicItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example5CsharpCode = @"
private BitAccordionList<BitAccordionListItem> accordionListRef = default!;

private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Items=""bindingButtons"" TItem=""BitButtonGroupItem"" @bind-ToggleKey=""boundExpandedKey"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""keyedItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example6CsharpCode = @"
private string? boundExpandedKey = ""users"";

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Key = ""general"", Text = ""General"" },
    new() { Key = ""users"", Text = ""Users"" },
    new() { Key = ""advanced"", Text = ""Advanced"" },
];

private readonly List<BitAccordionListItem> keyedItems =
[
    new()
    {
        Key = ""general"",
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Key = ""users"",
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Key = ""advanced"",
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example7RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" ExpanderIconName=""@BitIconName.Add"" />

<BitAccordionList Items=""iconItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example7CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private readonly List<BitAccordionListItem> iconItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        ExpanderIconName = BitIconName.Settings,
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        ExpanderIconName = BitIconName.Contact,
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        ExpanderIconName = BitIconName.Ringer,
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example8RazorCode = @"
<BitAccordionList NoBorder Items=""basicItems"" TItem=""BitAccordionListItem"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Background=""BitColorKind.Secondary""
                  Border=""BitColorKind.Tertiary"" />";
    private readonly string example8CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

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
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example10RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" />
<BitAccordionList Items=""basicItems"" TItem=""BitAccordionListItem"" Gap=""8"" Class=""custom-item"" />

<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" />
<BitAccordionList Items=""basicItems""
                  TItem=""BitAccordionListItem""
                  Classes=""@(new() { ItemTitle = ""custom-title"" })"" />";
    private readonly string example10CsharpCode = @"
private readonly List<BitAccordionListItem> basicItems =
[
    new()
    {
        Title = ""General settings"",
        Description = ""The general settings of the application"",
        Body = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Title = ""Users"",
        Description = ""You are currently not an owner"",
        Body = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Title = ""Advanced settings"",
        Description = ""Filtering has been entirely disabled"",
        Body = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";

    private readonly string example11RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" Items=""rtlItems"" TItem=""BitAccordionListItem"" />";
    private readonly string example11CsharpCode = @"
private readonly List<BitAccordionListItem> rtlItems =
[
    new()
    {
        Title = ""تنظیمات عمومی"",
        Description = ""تنظیمات کلی برنامه"",
        Body = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."")
    },
    new()
    {
        Title = ""کاربران"",
        Description = ""شما در حال حاضر مالک نیستید"",
        Body = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."")
    },
];

private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder => builder.AddContent(0, text);";
}
