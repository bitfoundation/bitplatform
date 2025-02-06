namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupCustomDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";
    public ChoiceGroupValidationModel validationModel = new();
    public string? successMessage;


    private readonly List<Order> basicCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B" },
        new() { Name = "Custom C", ItemValue = "C" },
        new() { Name = "Custom D", ItemValue = "D" }
    ];

    private readonly List<Order> disabledCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B" },
        new() { Name = "Custom C", ItemValue = "C", IsDisabled = true },
        new() { Name = "Custom D", ItemValue = "D" }
    ];

    private readonly List<Order> imageCustoms =
    [
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageSize = new BitImageSize(32, 32),
            ImageDescription = "alt for Bar image",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageSize = new BitImageSize(32, 32),
            ImageDescription = "alt for Pie image",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<Order> inlineImageCustoms =
    [
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageSize = new BitImageSize(20, 20),
            ImageDescription = "alt for Bar image",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageSize = new BitImageSize(20, 20),
            ImageDescription = "alt for Pie image",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<Order> iconCustoms =
    [
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar, IsDisabled = true }
    ];

    private readonly List<Order> itemStyleClassCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A", Class = "custom-item" },
        new() { Name = "Custom B", ItemValue = "B", Style = "padding: 8px; border-radius: 20px; border: 1px solid gray;" },
        new() { Name = "Custom C", ItemValue = "C", Class = "custom-item" },
        new() { Name = "Custom D", ItemValue = "D", Class = "custom-item" }
    ];

    private readonly List<Order> itemTemplateCustoms =
    [
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar }
    ];

    private readonly List<Order> rtlCustoms =
    [
        new() { Name = "ویژه آ", ItemValue = "A" },
        new() { Name = "ویژه ب", ItemValue = "B" },
        new() { Name = "ویژه پ", ItemValue = "C" },
        new() { Name = "ویژه ت", ItemValue = "D" }
    ];


    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }


    private readonly string example1RazorCode = @"
<BitChoiceGroup Label=""Basic Customs""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Basic Customs""
                NoCircle
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />";
    private readonly string example1CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];";

    private readonly string example2RazorCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Custom""
                Items=""disabledCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, 
                                         Value = { Selector = i => i.ItemValue },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />";
    private readonly string example2CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public bool IsDisabled { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];

private readonly List<Order> disabledCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"", IsDisabled = true },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];";

    private readonly string example3RazorCode = @"
<BitChoiceGroup Label=""Image Customs""
                Items=""imageCustoms""
                DefaultValue=""@(""Bar"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         ImageSrc = { Name = nameof(Order.ImageAddress) },
                                         ImageAlt = { Name = nameof(Order.ImageDescription) },
                                         ImageSize = { Name = nameof(Order.ImageSize) },
                                         SelectedImageSrc = { Name = nameof(Order.SelectedImageAddress) }})"" />

<BitChoiceGroup Label=""Icon Customs""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Value = { Selector = i => i.ItemValue },
                                         IconName = { Selector = i => i.IconName },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />


<BitChoiceGroup Label=""Image Customs""
                Inline
                Items=""inlineImageCustoms""
                DefaultValue=""@(""Bar"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         ImageSrc = { Name = nameof(Order.ImageAddress) },
                                         ImageAlt = { Name = nameof(Order.ImageDescription) },
                                         ImageSize = { Name = nameof(Order.ImageSize) },
                                         SelectedImageSrc = { Name = nameof(Order.SelectedImageAddress) }})"" />

<BitChoiceGroup Label=""Icon Customs""
                Inline
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Value = { Selector = i => i.ItemValue },
                                         IconName = { Selector = i => i.IconName },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />";
    private readonly string example3CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsDisabled { get; set; }
}

private readonly List<Order> imageCustoms =
[
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageSize = new BitImageSize(32, 32),
        ImageDescription = ""alt for Bar image"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageSize = new BitImageSize(32, 32),
        ImageDescription = ""alt for Pie image"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
];

private readonly List<Order> inlineImageCustoms =
[
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageSize = new BitImageSize(20, 20),
        ImageDescription = ""alt for Bar image"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageSize = new BitImageSize(20, 20),
        ImageDescription = ""alt for Pie image"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
];

private readonly List<Order> iconCustoms =
[
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsDisabled = true }
];";

    private readonly string example4RazorCode = @"
<BitChoiceGroup Label=""Basic""
                Horizontal
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Disabled""
                Horizontal
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />

<BitChoiceGroup Label=""Image""
                Horizontal
                DefaultValue=""@(""Bar"")""
                Items=""imageCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         ImageSrc = { Name = nameof(Order.ImageAddress) },
                                         ImageAlt = { Name = nameof(Order.ImageDescription) },
                                         ImageSize = { Name = nameof(Order.ImageSize) },
                                         SelectedImageSrc = { Name = nameof(Order.SelectedImageAddress) }})"" />

<BitChoiceGroup Label=""Icon""
                Horizontal
                DefaultValue=""@(""Day"")""
                Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Value = { Selector = i => i.ItemValue },
                                         IconName = { Selector = i => i.IconName },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />";
    private readonly string example4CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsDisabled { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];

private readonly List<Order> imageCustoms =
[
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageSize = new BitImageSize(32, 32),
        ImageDescription = ""alt for Bar image"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageSize = new BitImageSize(32, 32),
        ImageDescription = ""alt for Pie image"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
];

private readonly List<Order> iconCustoms =
[
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsDisabled = true }
];";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        margin-inline: 16px;
        text-shadow: dodgerblue 0 0 8px;
    }


    .custom-item {
        padding: 8px;
        border-radius: 20px;
        border: 1px solid gray;
    }


    .custom-root {
        margin-inline: 16px;
    }

    .custom-text {
        font-weight: bold;
    }

    .custom-label-wrapper::after {
        width: 8px;
        height: 8px;
        border: none;
        inset-inline-start: 6px;
        background-color: whitesmoke;
    }

    .custom-checked .custom-label-wrapper::after {
        background-color: whitesmoke;
    }

    .custom-label-wrapper::before {
        background-color: whitesmoke;
    }

    .custom-checked .custom-label-wrapper::before {
        background-color: dodgerblue;
    }
</style>


<BitChoiceGroup Label=""Styled ChoiceGroup""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Style=""margin-inline: 16px; text-shadow: red 0 0 8px;""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Classed ChoiceGroup""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Class=""custom-class""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />


<BitChoiceGroup Items=""itemStyleClassCustoms""
                DefaultValue=""itemStyleClassCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, 
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Class = { Name = nameof(Order.Class) },
                                         Style = { Name = nameof(Order.Style) } })"" />


<BitChoiceGroup Label=""Styles""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Styles=""@(new() { Root = ""margin-inline: 16px; --item-background: #d3d3d347; --item-border: 1px solid gray;"",
                                  ItemLabel = ""width: 100%; cursor: pointer;"",
                                  ItemChecked = ""--item-background: #87cefa24; --item-border: 1px solid dodgerblue;"",
                                  ItemContainer = ""padding: 8px; border-radius: 2px; background-color: var(--item-background); border: var(--item-border);"" })""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Classes""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Classes=""@(new() { Root = ""custom-root"",
                                   ItemText = ""custom-text"",
                                   ItemChecked = ""custom-checked"",
                                   ItemLabelWrapper = ""custom-label-wrapper"" })""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })""/>";
    private readonly string example5CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];

private readonly List<Order> itemStyleClassCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"", Class = ""custom-item"" },
    new() { Name = ""Custom B"", ItemValue = ""B"", Style = ""padding: 8px; border-radius: 20px; border: 1px solid gray;"" },
    new() { Name = ""Custom C"", ItemValue = ""C"", Class = ""custom-item"" },
    new() { Name = ""Custom D"", ItemValue = ""D"", Class = ""custom-item"" }
];";

    private readonly string example6RazorCode = @"
<style>
    .custom-label {
        color: red;
        font-size: 18px;
        font-weight: bold;
    }
</style>

<BitChoiceGroup Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>";
    private readonly string example6CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];";

    private readonly string example7RazorCode = @"
<BitChoiceGroup Label=""One-way"" Value=""@oneWayValue""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
<BitTextField @bind-Value=""oneWayValue"" />

<BitChoiceGroup Label=""Two-way"" @bind-Value=""twoWayValue""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />
<BitTextField @bind-Value=""twoWayValue"" />";
    private readonly string example7CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";

public class Order
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];";

    private readonly string example8RazorCode = @"
<style>
    .custom-container {
        display: flex;
        align-items: center;
        gap: 10px;
        cursor: pointer;
    }

    .custom-circle {
        width: 20px;
        height: 20px;
        border: 1px solid;
        border-radius: 10px;
    }

    .custom-container:hover .custom-circle {
        border-top: 5px solid #C66;
        border-bottom: 5px solid #6C6;
        border-left: 5px solid #66C;
        border-right: 5px solid #CC6;
    }

    .custom-container.selected {
        color: #C66;
    }

    .custom-container.selected .custom-circle {
        border-top: 10px solid #C66;
        border-bottom: 10px solid #6C6;
        border-left: 10px solid #66C;
        border-right: 10px solid #CC6;
    }
</style>

<BitChoiceGroup Label=""ItemPrefixTemplate"" Items=""basicCustoms"" DefaultValue=""@string.Empty""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name},
                                         Value = { Selector = i => i.ItemValue },
                                         Index = nameof(Order.Idx) })"">
    <ItemPrefixTemplate Context=""item"">
        @(item.Idx + 1).&nbsp;
    </ItemPrefixTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemLabelTemplate"" @bind-Value=""itemLabelTemplateValue""
                Items=""itemTemplateCustoms""
                NameSelectors=""@(new() { Value = { Selector = i => i.ItemValue } })"">
    <ItemLabelTemplate Context=""custom"">
        <div class=""custom-container @(itemLabelTemplateValue == custom.ItemValue ? ""selected"" : string.Empty)"">
            <BitIcon IconName=""@custom.IconName"" />
            <span>@custom.Name</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemTemplate"" @bind-Value=""itemTemplateValue""
                Items=""itemTemplateCustoms""
                NameSelectors=""@(new() { Value = { Name = nameof(Order.ItemValue) } })"">
    <ItemTemplate Context=""custom"">
        <div class=""custom-container @(itemTemplateValue == custom.ItemValue ? ""selected"" : string.Empty)"">
            <div class=""custom-circle""></div>
            <span>@custom.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>


<BitChoiceGroup Label=""Item's Template"" Items=""itemTemplateCustoms2"" @bind-Value=""itemTemplateValue2""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, 
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Template = { Name = nameof(Order.Fragment) } })"" />";
    private readonly string example8CsharpCode = @"
private string itemLabelTemplateValue = ""Day"";
private string itemTemplateValue = ""Day"";
private string itemTemplateValue2 = ""Day"";

public class Order
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
    public string? IconName { get; set; }
    public RenderFragment<Order>? Fragment { get; set; }
    public int Idx { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];

private readonly List<Order> itemTemplateCustoms =
[
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
];

private List<Order> itemTemplateCustoms2 = default!;    
protected override void OnInitialized()
{
    itemTemplateCustoms2 =
    [
        new()
        {
            Name = ""Day"",
            ItemValue = ""Day"",
            Fragment = (item => @<div class=""custom-container @(itemTemplateValue2 == item.ItemValue ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:red"">@item.Name</span>
                                 </div>)
        },
        new()
        {
            Name = ""Week"",
            ItemValue = ""Week"",
            Fragment = (item => @<div class=""custom-container @(itemTemplateValue2 == item.ItemValue ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:green"">@item.Name</span>
                                 </div>)
        },
        new()
        {
            Name = ""Month"",
            ItemValue = ""Month"",
            Fragment = (item => @<div class=""custom-container @(itemTemplateValue2 == item.ItemValue ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:blue"">@item.Name</span>
                                 </div>)
        }
    ];
}";

    private readonly string example9RazorCode = @"
<BitChoiceGroup Label=""ساده""
                Dir=""BitDir.Rtl""
                DefaultValue=""@(""A"")""
                Items=""rtlCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""غیرفعال""
                Dir=""BitDir.Rtl""
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                Items=""rtlCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />";
    private readonly string example9CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> rtlCustoms =
[
    new() { Name = ""ویژه آ"", ItemValue = ""A"" },
    new() { Name = ""ویژه ب"", ItemValue = ""B"" },
    new() { Name = ""ویژه پ"", ItemValue = ""C"" },
    new() { Name = ""ویژه ت"", ItemValue = ""D"" }
];";

    private readonly string example10RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    
    <BitChoiceGroup @bind-Value=""validationModel.Value""
                    Items=""basicCustoms""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
    <ValidationMessage For=""@(() => validationModel.Value)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example10CsharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }

public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];";
}
