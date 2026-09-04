namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupCustomDemo
{
    private readonly string example1RazorCode = @"
<BitChoiceGroup Label=""Basic Customs""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""NoCircle""
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
    public BitImageSize? ImageSize { get; set; }
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
    public BitImageSize? ImageSize { get; set; }
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
<BitChoiceGroup Label=""End (default)""
                LabelPosition=""BitSide.End"" Horizontal
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Start""
                LabelPosition=""BitSide.Start"" Horizontal
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Top""
                LabelPosition=""BitSide.Top"" Horizontal
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Bottom""
                LabelPosition=""BitSide.Bottom"" Horizontal
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />";
    private readonly string example5CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public BitImageSize? ImageSize { get; set; }
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

<BitChoiceGroup Label=""ItemPrefixTemplate & ItemSuffixTemplate"" Items=""basicCustoms"" DefaultValue=""@string.Empty""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name},
                                         Value = { Selector = i => i.ItemValue },
                                         Index = nameof(Order.Idx) })"">
    <ItemPrefixTemplate Context=""item"">
        @(item.Idx + 1).&nbsp;
    </ItemPrefixTemplate>
    <ItemSuffixTemplate Context=""item"">
        &nbsp;<b>(@item.ItemValue)</b>
    </ItemSuffixTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemLabelTemplate"" @bind-Value=""itemLabelTemplateValue""
                Items=""itemLabelTemplateCustoms""
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
    private readonly string example7CsharpCode = @"
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

private readonly List<Order> itemLabelTemplateCustoms =
    [
        new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
        new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
        new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
    ];

private readonly List<Order> itemTemplateCustoms =
[
    new() { Name = ""Day"", ItemValue = ""Day"" },
    new() { Name = ""Week"", ItemValue = ""Week"" },
    new() { Name = ""Month"", ItemValue = ""Month"" }
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

    private readonly string example8RazorCode = @"
<BitChoiceGroup Label=""Uncontrolled (DefaultValue)"" DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                OnChange=""(string? value) => uncontrolledValue = value""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
<div>Selected: <b>@uncontrolledValue</b></div>

<BitChoiceGroup Label=""One-way"" Value=""@oneWayValue""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
<BitTextField @bind-Value=""oneWayValue"" />

<BitChoiceGroup Label=""Two-way"" @bind-Value=""twoWayValue""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />
<BitTextField @bind-Value=""twoWayValue"" />";
    private readonly string example8CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";
private string? uncontrolledValue = ""A"";

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

    private readonly string example9RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"" novalidate>
    <DataAnnotationsValidator />
    
    <BitChoiceGroup Label=""Pick one"" Required @bind-Value=""validationModel.Value""
                    Items=""basicCustoms""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
    <ValidationMessage For=""@(() => validationModel.Value)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example9CsharpCode = @"
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

    private readonly string example10RazorCode = @"
<BitChoiceGroup Label=""Backup frequency""
                Items=""descriptionCustoms""
                DefaultValue=""@(""Daily"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Description = { Name = nameof(Order.Summary) } })"" />";
    private readonly string example10CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string Summary { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> descriptionCustoms =
[
    new() { Name = ""Daily"", ItemValue = ""Daily"", Summary = ""Backs up every night at 2 AM."" },
    new() { Name = ""Weekly"", ItemValue = ""Weekly"", Summary = ""Backs up every Sunday at 2 AM."" },
    new() { Name = ""Monthly"", ItemValue = ""Monthly"", Summary = ""Backs up on the first day of each month."" }
];";

    private readonly string example11RazorCode = @"
<BitChoiceGroup Label=""ReadOnly""
                ReadOnly
                Items=""basicCustoms""
                @bind-Value=""readOnlyValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />";
    private readonly string example11CsharpCode = @"
private string readOnlyValue = ""A"";

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

    private readonly string example12RazorCode = @"
<BitChoiceGroup Label=""1rem gap""
                Gap=""1rem""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""3rem gap (Horizontal)""
                Gap=""3rem""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />";
    private readonly string example12CsharpCode = @"
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

    private readonly string example13RazorCode = @"
<BitChoiceGroup Label=""Shipping method (Prefix)""
                Items=""prefixCustoms""
                DefaultValue=""@(""Standard"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Prefix = { Name = nameof(Order.Prefix) } })"" />

<BitChoiceGroup Label=""Shipping method (Suffix)""
                Items=""suffixCustoms""
                DefaultValue=""@(""Standard"")""
                FullWidth
                Styles=""@(new() { ItemSuffix = ""margin-inline-start: auto;"" })""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Suffix = { Name = nameof(Order.Fee) } })"" />";
    private readonly string example13CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string Fee { get; set; }
    public string Prefix { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> prefixCustoms =
[
    new() { Name = ""Standard"", ItemValue = ""Standard"", Prefix = ""$0 - "" },
    new() { Name = ""Express"", ItemValue = ""Express"", Prefix = ""$10 - "" },
    new() { Name = ""Overnight"", ItemValue = ""Overnight"", Prefix = ""$25 - "" }
];

private readonly List<Order> suffixCustoms =
[
    new() { Name = ""Standard"", ItemValue = ""Standard"", Fee = ""Free"" },
    new() { Name = ""Express"", ItemValue = ""Express"", Fee = ""$10"" },
    new() { Name = ""Overnight"", ItemValue = ""Overnight"", Fee = ""$25"" }
];";

    private readonly string example14RazorCode = @"
<BitChoiceGroup Label=""Events"" Items=""basicCustoms"" DefaultValue=""@(""A"")""
                OnChange=""(string? value) => changedValue = value""
                OnClick=""(Order custom) => clickedCustom = custom.Name""
                OnFocus=""(Order custom) => focusedCustom = custom.Name""
                OnBlur=""(Order custom) => blurredCustom = custom.Name""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
<div>Changed value: <b>@changedValue</b></div>
<div>Clicked custom: <b>@clickedCustom</b></div>
<div>Focused custom: <b>@focusedCustom</b></div>
<div>Blurred custom: <b>@blurredCustom</b></div>";
    private readonly string example14CsharpCode = @"
private string? changedValue;
private string? clickedCustom;
private string? focusedCustom;
private string? blurredCustom;

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

    private readonly string example15RazorCode = @"
<BitButton OnClick=""AddDynamicCustom"">Add item</BitButton>
<BitButton OnClick=""RemoveDynamicCustom"">Remove item</BitButton>
<BitButton OnClick=""ReverseDynamicCustoms"">Reverse items</BitButton>

<BitChoiceGroup Label=""Dynamic customs"" Items=""dynamicCustoms"" @bind-Value=""dynamicValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Index = nameof(Order.Idx) })"">
    <ItemPrefixTemplate Context=""custom"">
        @(custom.Idx + 1).&nbsp;
    </ItemPrefixTemplate>
</BitChoiceGroup>";
    private readonly string example15CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public int Idx { get; set; }
}

private int dynamicCounter = 3;
private string? dynamicValue = ""1"";
private List<Order> dynamicCustoms =
[
    new() { Name = ""Custom 1"", ItemValue = ""1"" },
    new() { Name = ""Custom 2"", ItemValue = ""2"" },
    new() { Name = ""Custom 3"", ItemValue = ""3"" }
];

private void AddDynamicCustom()
{
    dynamicCounter++;
    dynamicCustoms = [.. dynamicCustoms, new Order { Name = $""Custom {dynamicCounter}"", ItemValue = $""{dynamicCounter}"" }];
}

private void RemoveDynamicCustom()
{
    if (dynamicCustoms.Count <= 1) return;

    dynamicCustoms = [.. dynamicCustoms.Take(dynamicCustoms.Count - 1)];
}

private void ReverseDynamicCustoms()
{
    dynamicCustoms = [.. Enumerable.Reverse(dynamicCustoms)];
}";

    private readonly string example16RazorCode = @"
<style>
    .custom-description {
        gap: 0.25rem;
        display: flex;
        align-items: center;
    }
</style>

<BitChoiceGroup Label=""Deployment target""
                Description=""Only the selected environment receives the new build.""
                Items=""deploymentCustoms""
                DefaultValue=""@(""Staging"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""Deployment target""
                Items=""deploymentCustoms""
                DefaultValue=""@(""Staging"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) } })"">
    <DescriptionTemplate>
        <div class=""custom-description"">
            <BitIcon IconName=""@BitIconName.Info"" />
            <span>Only the selected environment receives the new build.</span>
        </div>
    </DescriptionTemplate>
</BitChoiceGroup>";
    private readonly string example16CsharpCode = @"
public class Order
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
}

private readonly List<Order> deploymentCustoms =
[
    new() { Name = ""Development"", ItemValue = ""Development"" },
    new() { Name = ""Staging"", ItemValue = ""Staging"" },
    new() { Name = ""Production"", ItemValue = ""Production"" }
];";

    private readonly string example17RazorCode = @"
<BitChoiceGroup Label=""Default (hugs the widest item)""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                Horizontal
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""FullWidth (horizontal, equal columns)""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                Horizontal FullWidth
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""FullWidth + LabelPosition.Start (items at the far edge)""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                LabelPosition=""BitSide.Start"" FullWidth
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""FullWidth + LabelPosition.Start + stretched item label (settings list)""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                LabelPosition=""BitSide.Start"" FullWidth
                Styles=""@(new() { ItemLabel = ""width: 100%; justify-content: space-between;"" })""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) } })"" />";
    private readonly string example17CsharpCode = @"
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

    private readonly string example18RazorCode = @"
<BitChoiceGroup Label=""Delivery window (hover an item)""
                Items=""titleCustoms""
                DefaultValue=""@(""24h"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                         Value = { Name = nameof(Order.ItemValue) },
                                         Title = { Name = nameof(Order.Tooltip) } })"" />";
    private readonly string example18CsharpCode = @"
public class Order
{
    public string? Name { get; set; }
    public string? Tooltip { get; set; }
    public string? ItemValue { get; set; }
}

private readonly List<Order> titleCustoms =
[
    new() { Name = ""1 h"", ItemValue = ""1h"", Tooltip = ""Delivered within one hour of dispatch"" },
    new() { Name = ""24 h"", ItemValue = ""24h"", Tooltip = ""Delivered within one business day"" },
    new() { Name = ""72 h"", ItemValue = ""72h"", Tooltip = ""Delivered within three business days"" }
];";

    private readonly string example19RazorCode = @"
<BitButton OnClick=""() => showAutoFocus = !showAutoFocus"">@(showAutoFocus ? ""Unmount"" : ""Mount"") the auto focused ChoiceGroup</BitButton>

@if (showAutoFocus)
{
    <BitChoiceGroup AutoFocus
                    Label=""Auto focused""
                    Items=""basicCustoms""
                    DefaultValue=""@(""B"")""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) },
                                             Value = { Name = nameof(Order.ItemValue) } })"" />
}";
    private readonly string example19CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private bool showAutoFocus;

private readonly List<Order> basicCustoms =
[
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
];";

    private readonly string example20RazorCode = @"
<BitChoiceGroup Color=""BitColor.Primary""
                Label=""Primary"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.Secondary"" 
                Label=""Secondary"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.Tertiary"" 
                Label=""Tertiary"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.Info"" 
                Label=""Info"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.Success"" 
                Label=""Success"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.Warning"" 
                Label=""Warning"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.SevereWarning"" 
                Label=""SevereWarning"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.Error"" 
                Label=""Error"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<div style=""background:var(--bit-clr-fg-sec);color:var(--bit-clr-bg-sec);padding:1rem"">
    <BitChoiceGroup Color=""BitColor.PrimaryBackground""
                    Label=""PrimaryBackground""
                    Horizontal
                    Items=""basicCustoms""
                    DefaultValue=""basicCustoms[1].ItemValue""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

    <BitChoiceGroup Color=""BitColor.SecondaryBackground""
                    Label=""SecondaryBackground""
                    Horizontal
                    Items=""basicCustoms""
                    DefaultValue=""basicCustoms[1].ItemValue""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

    <BitChoiceGroup Color=""BitColor.TertiaryBackground""
                    Label=""TertiaryBackground""
                    Horizontal
                    Items=""basicCustoms""
                    DefaultValue=""basicCustoms[1].ItemValue""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
</div>

<BitChoiceGroup Color=""BitColor.PrimaryForeground"" 
                Label=""PrimaryForeground"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.SecondaryForeground"" 
                Label=""SecondaryForeground"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.TertiaryForeground"" 
                Label=""TertiaryForeground"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.PrimaryBorder"" 
                Label=""PrimaryBorder"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.SecondaryBorder"" 
                Label=""SecondaryBorder"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Color=""BitColor.TertiaryBorder"" 
                Label=""TertiaryBorder"" 
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Primary""
                Label=""Primary""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Secondary""
                Label=""Secondary""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Tertiary""
                Label=""Tertiary""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Info""
                Label=""Info""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Success""
                Label=""Success""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Warning""
                Label=""Warning""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.SevereWarning""
                Label=""SevereWarning""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.Error""
                Label=""Error""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<div style=""background:var(--bit-clr-fg-sec);color:var(--bit-clr-bg-sec);padding:1rem"">
    <BitChoiceGroup IsEnabled=""false""
                    Color=""BitColor.PrimaryBackground""
                    Label=""PrimaryBackground""
                    Horizontal
                    Items=""basicCustoms""
                    DefaultValue=""basicCustoms[0].ItemValue""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

    <BitChoiceGroup IsEnabled=""false""
                    Color=""BitColor.SecondaryBackground""
                    Label=""SecondaryBackground""
                    Horizontal
                    Items=""basicCustoms""
                    DefaultValue=""basicCustoms[0].ItemValue""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

    <BitChoiceGroup IsEnabled=""false""
                    Color=""BitColor.TertiaryBackground""
                    Label=""TertiaryBackground""
                    Horizontal
                    Items=""basicCustoms""
                    DefaultValue=""basicCustoms[0].ItemValue""
                    NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />
</div>

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.PrimaryForeground""
                Label=""PrimaryForeground""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.SecondaryForeground""
                Label=""SecondaryForeground""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.TertiaryForeground""
                Label=""TertiaryForeground""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.PrimaryBorder""
                Label=""PrimaryBorder""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.SecondaryBorder""
                Label=""SecondaryBorder""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup IsEnabled=""false""
                Color=""BitColor.TertiaryBorder""
                Label=""TertiaryBorder""
                Horizontal
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[0].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />";
    private readonly string example20CsharpCode = @"
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

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitChoiceGroup Label=""External Icons"" 
                Items=""externalIconCustoms"" 
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, 
                                         Value = { Name = nameof(Order.ItemValue) }, 
                                         Icon = { Name = nameof(Order.Icon) } })"" />

<BitChoiceGroup Label=""External Icons (Inline)"" 
                Items=""externalIconCustoms"" 
                DefaultValue=""@(""Day"")"" Inline
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, 
                                         Value = { Name = nameof(Order.ItemValue) }, 
                                         Icon = { Name = nameof(Order.Icon) } })"" />

<BitChoiceGroup Label=""External Icons (Horizontal)"" 
                Items=""externalIconCustoms"" 
                DefaultValue=""@(""Day"")"" Horizontal
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, 
                                         Value = { Name = nameof(Order.ItemValue) }, 
                                         Icon = { Name = nameof(Order.Icon) } })"" />";
    private readonly string example21CsharpCode = @"
public class Order
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
    public BitIconInfo? Icon { get; set; }
}

private readonly List<Order> externalIconCustoms =
[
    new() { Name = ""Day"", ItemValue = ""Day"", Icon = BitIconInfo.Fa(""solid sun"") },
    new() { Name = ""Week"", ItemValue = ""Week"", Icon = BitIconInfo.Css(""fa-solid fa-calendar-week"") },
    new() { Name = ""Month"", ItemValue = ""Month"", Icon = BitIconInfo.Bi(""calendar-month"") }
];";

    private readonly string example22RazorCode = @"
<BitChoiceGroup Size=""BitSize.Small""
                Label=""Small""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" Horizontal />

<BitChoiceGroup Size=""BitSize.Medium""
                Label=""Medium""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" Horizontal />

<BitChoiceGroup Size=""BitSize.Large""
                Label=""Large""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" Horizontal />

<BitChoiceGroup Size=""BitSize.Small""
                Label=""Small""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                          Value = { Selector = i => i.ItemValue },
                                          IconName = { Selector = i => i.IconName },
                                          IsEnabled = { Selector = i => i.IsDisabled is false } })"" Horizontal Inline />

<BitChoiceGroup Size=""BitSize.Medium""
                Label=""Medium""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                          Value = { Selector = i => i.ItemValue },
                                          IconName = { Selector = i => i.IconName },
                                          IsEnabled = { Selector = i => i.IsDisabled is false } })"" Horizontal Inline />

<BitChoiceGroup Size=""BitSize.Large""
                Label=""Large""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                          Value = { Selector = i => i.ItemValue },
                                          IconName = { Selector = i => i.IconName },
                                          IsEnabled = { Selector = i => i.IsDisabled is false } })"" Horizontal Inline />

<BitChoiceGroup Size=""BitSize.Small""
                Label=""Small""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                          Value = { Selector = i => i.ItemValue },
                                          IconName = { Selector = i => i.IconName },
                                          IsEnabled = { Selector = i => i.IsDisabled is false } })"" Horizontal />

<BitChoiceGroup Size=""BitSize.Medium""
                Label=""Medium""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                          Value = { Selector = i => i.ItemValue },
                                          IconName = { Selector = i => i.IconName },
                                          IsEnabled = { Selector = i => i.IsDisabled is false } })"" Horizontal />

<BitChoiceGroup Size=""BitSize.Large""
                Label=""Large""
                Items=""iconCustoms""
                DefaultValue=""@(""Day"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                          Value = { Selector = i => i.ItemValue },
                                          IconName = { Selector = i => i.IconName },
                                          IsEnabled = { Selector = i => i.IsDisabled is false } })"" Horizontal />";
    private readonly string example22CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
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

private readonly List<Order> iconCustoms =
[
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsDisabled = true }
];";

    private readonly string example23RazorCode = @"
<style>
    .custom-class {
        color: dodgerblue;
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

    .custom-radio-btn::after {
        width: 8px;
        height: 8px;
        background-color: whitesmoke;
    }

    .custom-checked .custom-radio-btn::after {
        background-color: whitesmoke;
    }

    .custom-radio-btn::before {
        background-color: whitesmoke;
    }

    .custom-checked .custom-radio-btn::before {
        background-color: dodgerblue;
    }
</style>


<BitChoiceGroup Label=""Styled ChoiceGroup""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Style=""margin-inline: 16px; color:lightseagreen; text-shadow: lightseagreen 0 0 8px;""
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
                                   ItemRadioButton = ""custom-radio-btn"" })""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })""/>";
    private readonly string example23CsharpCode = @"
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

    private readonly string example24RazorCode = @"
<BitChoiceGroup Label=""Ø³Ø§Ø¯Ù‡""
                Dir=""BitDir.Rtl""
                DefaultValue=""@(""A"")""
                Items=""rtlCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(Order.Name) }, Value = { Name = nameof(Order.ItemValue) } })"" />

<BitChoiceGroup Label=""ØºÛŒØ±ÙØ¹Ø§Ù„""
                Dir=""BitDir.Rtl""
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                Items=""rtlCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />";
    private readonly string example24CsharpCode = @"
public class Order
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<Order> rtlCustoms =
[
    new() { Name = ""ÙˆÛŒÚ˜Ù‡ Ø¢"", ItemValue = ""A"" },
    new() { Name = ""ÙˆÛŒÚ˜Ù‡ Ø¨"", ItemValue = ""B"" },
    new() { Name = ""ÙˆÛŒÚ˜Ù‡ Ù¾"", ItemValue = ""C"" },
    new() { Name = ""ÙˆÛŒÚ˜Ù‡ Øª"", ItemValue = ""D"" }
];";
}
