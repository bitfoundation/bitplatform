namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListCustomDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example2CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example3RazorCode = @"
<BitAccordionList Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" DefaultExpandedKey=""users"" />

<BitAccordionList Multiple Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" DefaultExpandedKeys=""@([""general"", ""advanced""])"" />";
    private readonly string example3CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> keyedItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example4RazorCode = @"
<BitAccordionList Items=""eventsItems""
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
    private readonly string example4CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;

private List<Section> eventsItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

protected override void OnInitialized()
{
    foreach (var item in eventsItems)
    {
        item.Clicked = _ => { clickCounter++; StateHasChanged(); };
    }
}

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => accordionListRef.ExpandAll()"">Expand all</BitButton>
<BitButton OnClick=""() => accordionListRef.CollapseAll()"">Collapse all</BitButton>

<BitAccordionList @ref=""accordionListRef"" Multiple Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example5CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private BitAccordionList<Section> accordionListRef = default!;

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Items=""bindingButtons"" TItem=""BitButtonGroupItem"" @bind-ToggleKey=""boundExpandedKey"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""keyedItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example6CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private string? boundExpandedKey = ""users"";

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Key = ""general"", Text = ""General"" },
    new() { Key = ""users"", Text = ""Users"" },
    new() { Key = ""advanced"", Text = ""Advanced"" },
];

private readonly List<Section> keyedItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example7RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" ExpanderIconName=""@BitIconName.Add"" />

<BitAccordionList Items=""iconItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example7CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private readonly List<Section> iconItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Image = BitIconName.Settings,
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Image = BitIconName.Contact,
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Image = BitIconName.Ringer,
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example8RazorCode = @"
<BitAccordionList NoBorder Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />

<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Background=""BitColorKind.Secondary""
                  Border=""BitColorKind.Tertiary"" />";
    private readonly string example8CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example9RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"">
    <HeaderTemplate Context=""item"">
        <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
        <b>@item.Name</b>
    </HeaderTemplate>
    <BodyTemplate Context=""item"">
        <BitText Typography=""BitTypography.Caption1"">@item.Info</BitText>
    </BodyTemplate>
</BitAccordionList>";
    private readonly string example9CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example10RazorCode = @"
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" />
<BitAccordionList Items=""basicItems"" TItem=""Section"" NameSelectors=""nameSelectors"" Gap=""8"" Class=""custom-item"" />

<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" />

<BitAccordionList Items=""basicItems""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  Classes=""@(new() { ItemTitle = ""custom-title"" })"" />";
    private readonly string example10CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicItems =
[
    new()
    {
        Id = ""general"",
        Name = ""General settings"",
        Info = ""The general settings of the application"",
        Content = BodyFor(""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams."")
    },
    new()
    {
        Id = ""users"",
        Name = ""Users"",
        Info = ""You are currently not an owner"",
        Content = BodyFor(""Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams."")
    },
    new()
    {
        Id = ""advanced"",
        Name = ""Advanced settings"",
        Info = ""Filtering has been entirely disabled"",
        Content = BodyFor(""In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example11RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" Items=""rtlItems"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example11CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsEnabled = { Selector = i => i.IsEnabled },
    ExpanderIconName = { Selector = i => i.Image },
    Style = { Selector = i => i.Style },
    Class = { Selector = i => i.Class },
    OnClick = { Selector = i => i.Clicked },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> rtlItems =
[
    new()
    {
        Id = ""general"",
        Name = ""تنظیمات عمومی"",
        Info = ""تنظیمات کلی برنامه"",
        Content = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."")
    },
    new()
    {
        Id = ""users"",
        Name = ""کاربران"",
        Info = ""شما در حال حاضر مالک نیستید"",
        Content = BodyFor(""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است."")
    },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";
}
