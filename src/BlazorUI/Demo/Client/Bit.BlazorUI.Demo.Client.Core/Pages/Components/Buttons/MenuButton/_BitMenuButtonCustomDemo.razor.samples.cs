namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Split"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Split />";
    private readonly string example2CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" />

<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" IsEnabled=""false"" />

<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Split />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Split />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Split />

<BitMenuButton Text=""Fill"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Outline"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Text"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" IsEnabled=""false"" Split />";
    private readonly string example3CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" />

<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" Split />


<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" />

<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" Split />


<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" />

<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Split />


<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" />

<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" Split />


<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" />

<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" Split />


<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" />

<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" Split />


<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" />

<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" Split />


<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" />

<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" Split />";
    private readonly string example4CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""Small"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Size=""BitSize.Small"" />

<BitMenuButton Text=""Medium"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Size=""BitSize.Medium"" />

<BitMenuButton Text=""Large"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Size=""BitSize.Large"" />";
    private readonly string example5CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example6RazorCode = @"
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Sticky />
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" Split Sticky />

<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Sticky />
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" Split Sticky />

<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Sticky />
<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" Split Sticky />";
    private readonly string example6CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example7RazorCode = @"
<BitMenuButton Text=""IconName"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" IconName=""@BitIconName.Edit"" />
<BitMenuButton Text=""ChevronDownIcon"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" Split />";
    private readonly string example7CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        overflow: hidden;
        border-radius: 1rem;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }

    .custom-icon {
        color: red;
    }

    .custom-text {
        color: aqua;
    }
</style>


<BitMenuButton Text=""Styled Button"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Style=""width: 200px; height: 40px;"" />
<BitMenuButton Text=""Classed Button"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" Class=""custom-class"" />

<BitMenuButton Text=""Item Styled & Classed Button"" Items=""itemStyleClassCustoms"" NameSelectors=""nameSelectors"" />

<BitMenuButton Text=""Styles"" Items=""basicCustoms"" IconName=""@BitIconName.ExpandMenu"" NameSelectors=""nameSelectors""
               Styles=""@(new() { Icon = ""color: red;"",
                                 Text = ""color: aqua;"",
                                 ItemText = ""color: dodgerblue; font-size: 11px;"",
                                 Overlay = ""background-color: var(--bit-clr-bg-overlay);"" })"" />

<BitMenuButton Text=""Classes"" Items=""basicCustoms"" IconName=""@BitIconName.ExpandMenu"" NameSelectors=""nameSelectors""
               Classes=""@(new() { Icon = ""custom-icon"", Text = ""custom-text"" })"" />";
    private readonly string example8CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example9RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton Items=""basicCustoms"" NameSelectors=""nameSelectors"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Text=""Customs"" Items=""itemTemplateCustoms"" NameSelectors=""nameSelectors"" Split>
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color:brown"">@item.Name (@item.Id)</span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Customs"" Items=""itemTemplateCustoms2"" NameSelectors=""nameSelectors"" />";
    private readonly string example9CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public RenderFragment<MenuActionItem>? Fragment { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private List<MenuActionItem> itemTemplateCustoms =
[
    new() { Name = ""Add"", Id = ""add-key"", Icon = BitIconName.Add },
    new() { Name = ""Edit"", Id = ""edit-key"", Icon = BitIconName.Edit },
    new() { Name = ""Delete"", Id = ""delete-key"", Icon = BitIconName.Delete }
];

private List<MenuActionItem> itemTemplateCustoms2 = 
[
    new()
    {
        Name = ""Add"", Id = ""add-key"", Icon = BitIconName.Add,
        Fragment = (item => @<div class=""item-template-box"" style=""color:green"">@item.Name (@item.Id)</div>)
    },
    new()
    {
        Name = ""Edit"", Id = ""edit-key"", Icon = BitIconName.Edit,
        Fragment = (item => @<div class=""item-template-box"" style=""color:yellow"">@item.Name (@item.Id)</div>)
    },
    new()
    {
        Name = ""Delete"", Id = ""delete-key"", Icon = BitIconName.Delete,
        Fragment = (item => @<div class=""item-template-box"" style=""color:red"">@item.Name (@item.Id)</div>)
    }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false },
    Template = { Name = nameof(MenuActionItem.Fragment) }
};";

    private readonly string example10RazorCode = @"
<BitMenuButton Text=""Customs"" Items=""basicCustoms"" NameSelectors=""nameSelectors""
               OnChange=""(MenuActionItem item) => eventsChangedCustom = item?.Id""
               OnClick=""(MenuActionItem item) => eventsClickedCustom = item?.Id"" />

<BitMenuButton Split Text=""Customs"" Items=""basicCustomsOnClick"" NameSelectors=""nameSelectors""
               OnChange=""(MenuActionItem item) => eventsChangedCustom = item?.Id""
               OnClick=""@((MenuActionItem item) => eventsClickedCustom = ""Main button clicked"")"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors""
               OnChange=""(MenuActionItem item) => eventsChangedCustom = item?.Id""
               OnClick=""(MenuActionItem item) => eventsClickedCustom = item?.Id"" />

<BitMenuButton Sticky Split Items=""basicCustomsOnClick"" NameSelectors=""nameSelectors""
               OnChange=""(MenuActionItem item) => eventsChangedCustom = item?.Id""
               OnClick=""(MenuActionItem item) => eventsClickedCustom = item?.Id"" />

<div>Changed custom item: @eventsChangedCustom</div>
<div>Clicked custom item: @eventsClickedCustom</div>";
    private readonly string example10CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private List<MenuActionItem> basicCustomsOnClick =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2 }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example11RazorCode = @"
<BitMenuButton Split Sticky Items=""basicCustoms"" DefaultSelectedItem=""basicCustoms[1]"" NameSelectors=""nameSelectors"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors"" @bind-SelectedItem=""twoWaySelectedCustom"" />
<BitChoiceGroup Horizontal Items=""@choiceGroupCustoms"" @bind-Value=""@twoWaySelectedCustom"" />

<BitMenuButton Sticky Items=""isSelectedCustoms"" NameSelectors=""nameSelectors"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors"" IsOpen=""oneWayIsOpen"" />
<BitCheckbox Label=""One-way IsOpen"" @bind-Value=""oneWayIsOpen"" OnChange=""async _ => { await Task.Delay(2000); oneWayIsOpen = false; }"" />

<BitMenuButton Sticky Items=""basicCustoms"" NameSelectors=""nameSelectors"" @bind-IsOpen=""twoWayIsOpen"" />
<BitCheckbox Label=""Two-way IsOpen"" @bind-Value=""twoWayIsOpen"" />";
    private readonly string example11CsharpCode = @"
private MenuActionItem twoWaySelectedCustom = default!;
private bool oneWayIsOpen;
private bool twoWayIsOpen;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private static List<MenuActionItem> isSelectedCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2, IsSelected = true }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";

    private readonly string example12RazorCode = @"
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" Items=""rtlCustoms"" IconName=""@BitIconName.Edit"" NameSelectors=""nameSelectors"" />
<BitMenuButton Text=""گرینه ها"" Dir=""BitDir.Rtl"" Items=""rtlCustoms"" ChevronDownIcon=""@BitIconName.DoubleChevronDown"" NameSelectors=""nameSelectors"" Split />";
    private readonly string example12CsharpCode = @"
public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<MenuActionItem> basicCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"" },
    new() { Name = ""Custom B"", Id = ""B"", Disabled = true },
    new() { Name = ""Custom C"", Id = ""C"" }
];

private static List<MenuActionItem> itemStyleClassCustoms =
[
    new() { Name = ""Custom A"", Id = ""A"", Icon = BitIconName.Emoji, Style = ""color:red"" },
    new() { Name = ""Custom B"", Id = ""B"", Icon = BitIconName.Emoji, Class = ""custom-item"" },
    new() { Name = ""Custom C"", Id = ""C"", Icon = BitIconName.Emoji2, Style = ""background:blue"" }
];

private BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
{
    Text = { Name = nameof(MenuActionItem.Name) },
    Key = { Name = nameof(MenuActionItem.Id) },
    IconName = { Name = nameof(MenuActionItem.Icon) },
    IsEnabled = { Selector = m => m.Disabled is false }
};";
}
