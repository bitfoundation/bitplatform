namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupItemDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";

    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";

    private string? successMessage;
    private ChoiceGroupValidationModel validationModel = new();


    private readonly List<BitChoiceGroupItem<string>> basicItems =
    [
        new() { Text = "Item A", Value = "A" },
    new() { Text = "Item B", Value = "B" },
    new() { Text = "Item C", Value = "C" },
    new() { Text = "Item D", Value = "D" }
    ];

    private readonly List<BitChoiceGroupItem<string>> disabledItems =
    [
        new() { Text = "Item A", Value = "A" },
    new() { Text = "Item B", Value = "B" },
    new() { Text = "Item C", Value = "C", IsEnabled = false },
    new() { Text = "Item D", Value = "D" }
    ];

    private readonly List<BitChoiceGroupItem<string>> imageItems =
    [
        new()
        {
            Text = "Bar",
            Value = "Bar",
            ImageAlt = "alt for Bar image",
            ImageSize = new BitImageSize(32, 32),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Text = "Pie",
            Value = "Pie",
            ImageAlt = "alt for Pie image",
            ImageSize = new BitImageSize(32, 32),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<BitChoiceGroupItem<string>> inlineImageItems =
    [
        new()
        {
            Text = "Bar",
            Value = "Bar",
            ImageAlt = "alt for Bar image",
            ImageSize = new BitImageSize(20, 20),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Text = "Pie",
            Value = "Pie",
            ImageAlt = "alt for Pie image",
            ImageSize = new BitImageSize(20, 20),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<BitChoiceGroupItem<string>> iconItems =
    [
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    ];

    private readonly List<BitChoiceGroupItem<string>> itemStyleClassItems =
    [
        new() { Text = "Item A", Value = "A", Class = "custom-item" },
        new() { Text = "Item B", Value = "B", Style = "padding: 8px; border-radius: 20px; border: 1px solid gray;" },
        new() { Text = "Item C", Value = "C", Class = "custom-item" },
        new() { Text = "Item D", Value = "D", Class = "custom-item" }
    ];

    private readonly List<BitChoiceGroupItem<string>> itemTemplateItems =
    [
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar }
    ];

    private readonly List<BitChoiceGroupItem<string>> rtlItems =
    [
        new() { Text = "بخش آ", Value = "A" },
        new() { Text = "بخش ب", Value = "B" },
        new() { Text = "بخش پ", Value = "C" },
        new() { Text = "بخش ت", Value = "D" }
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
<BitChoiceGroup Label=""Basic Items""
                Items=""basicItems""
                DefaultValue=""basicItems[1].Value"" />

<BitChoiceGroup Label=""NoCircle""
                NoCircle
                Items=""basicItems""
                DefaultValue=""basicItems[1].Value"" />";
    private readonly string example1CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];";

    private readonly string example2RazorCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                Items=""basicItems""
                DefaultValue=""@(""A"")"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Items""
                Items=""disabledItems""
                DefaultValue=""@(""A"")"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];

private readonly List<BitChoiceGroupItem<string>> disabledItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"", IsEnabled = false },
    new() { Text = ""Item D"", Value = ""D"" }
];";

    private readonly string example3RazorCode = @"
<BitChoiceGroup Label=""Image Items"" Items=""imageItems"" DefaultValue=""@(""Bar"")"" />
<BitChoiceGroup Label=""Icon Items"" Items=""iconItems"" DefaultValue=""@(""Day"")"" />

<BitChoiceGroup Label=""Image Items"" Items=""inlineImageItems"" DefaultValue=""@(""Bar"")"" Inline />
<BitChoiceGroup Label=""Icon Items"" Items=""iconItems"" DefaultValue=""@(""Day"")"" Inline />";
    private readonly string example3CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> imageItems =
[
    new()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new BitImageSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new BitImageSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
];

private readonly List<BitChoiceGroupItem<string>> inlineImageItems =
[
    new()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new BitImageSize(20, 20),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new BitImageSize(20, 20),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
];

private readonly List<BitChoiceGroupItem<string>> iconItems =
[
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
];";

    private readonly string example4RazorCode = @"
<BitChoiceGroup Label=""Basic"" Items=""basicItems"" DefaultValue=""@(""A"")"" Horizontal />
<BitChoiceGroup Label=""Disabled"" Items=""basicItems"" IsEnabled=""false"" DefaultValue=""@(""A"")"" Horizontal />
<BitChoiceGroup Label=""Image"" Items=""imageItems"" DefaultValue=""@(""Bar"")"" Horizontal />
<BitChoiceGroup Label=""Icon"" Items=""iconItems"" DefaultValue=""@(""Day"")"" Horizontal />";
    private readonly string example4CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];

private readonly List<BitChoiceGroupItem<string>> imageItems =
[
    new()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new BitImageSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new BitImageSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
];

private readonly List<BitChoiceGroupItem<string>> iconItems =
[
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
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
                Items=""basicItems""
                DefaultValue=""basicItems[1].Value""
                Style=""margin-inline: 16px; text-shadow: red 0 0 8px;"" />

<BitChoiceGroup Label=""Classed ChoiceGroup""
                Items=""basicItems""
                DefaultValue=""basicItems[1].Value""
                Class=""custom-class"" />


<BitChoiceGroup Items=""itemStyleClassItems"" DefaultValue=""itemStyleClassItems[1].Value"" />


<BitChoiceGroup Label=""Styles""
                Items=""basicItems""
                DefaultValue=""basicItems[1].Value""
                Styles=""@(new() { Root = ""margin-inline: 16px; --item-background: #d3d3d347; --item-border: 1px solid gray;"",
                                  ItemLabel = ""width: 100%; cursor: pointer;"",
                                  ItemChecked = ""--item-background: #87cefa24; --item-border: 1px solid dodgerblue;"",
                                  ItemContainer = ""padding: 8px; border-radius: 2px; background-color: var(--item-background); border: var(--item-border);"" })"" />

<BitChoiceGroup Label=""Classes""
                Items=""basicItems""
                DefaultValue=""basicItems[1].Value""
                Classes=""@(new() { Root = ""custom-root"",
                                   ItemText = ""custom-text"",
                                   ItemChecked = ""custom-checked"",
                                   ItemLabelWrapper = ""custom-label-wrapper"" })"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];

private readonly List<BitChoiceGroupItem<string>> itemStyleClassItems =
[
    new() { Text = ""Item A"", Value = ""A"", Class = ""custom-item"" },
    new() { Text = ""Item B"", Value = ""B"", Style = ""padding: 8px; border-radius: 20px; border: 1px solid gray;"" },
    new() { Text = ""Item C"", Value = ""C"", Class = ""custom-item"" },
    new() { Text = ""Item D"", Value = ""D"", Class = ""custom-item"" }
];";

    private readonly string example6RazorCode = @"
<style>
    .custom-label {
        color: red;
        font-size: 18px;
        font-weight: bold;
    }
</style>

<BitChoiceGroup Items=""basicItems"" DefaultValue=""@(""A"")"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>";
    private readonly string example6CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];";

    private readonly string example7RazorCode = @"
<BitChoiceGroup Label=""One-way"" Items=""basicItems"" Value=""@oneWayValue"" />
<BitTextField @bind-Value=""oneWayValue"" />

<BitChoiceGroup Label=""Two-way"" Items=""basicItems"" @bind-Value=""twoWayValue"" />
<BitTextField @bind-Value=""twoWayValue"" />";
    private readonly string example7CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";

private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
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

<BitChoiceGroup Label=""ItemPrefixTemplate"" Items=""basicItems"" DefaultValue=""@string.Empty"">
    <ItemPrefixTemplate Context=""item"">
        @(item.Index + 1).&nbsp;
    </ItemPrefixTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemLabelTemplate"" Items=""itemTemplateItems"" @bind-Value=""itemLabelTemplateValue"">
    <ItemLabelTemplate Context=""item"">
        <div class=""custom-container @(itemLabelTemplateValue == item.Value ? ""selected"" : string.Empty)"">
            <BitIcon IconName=""@item.IconName"" />
            <span>@item.Text</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>


<BitChoiceGroup Label=""ItemTemplate"" Items=""itemTemplateItems"" @bind-Value=""itemTemplateValue"">
    <ItemTemplate Context=""item"">
        <div class=""custom-container @(itemTemplateValue == item.Value ? ""selected"" : string.Empty)"">
            <div class=""custom-circle""></div>
            <span>@item.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Item's Template"" Items=""itemTemplateItems2"" @bind-Value=""itemTemplateValue2"" />";
    private readonly string example8CsharpCode = @"
private string itemTemplateValue = ""Day"";
private string itemTemplateValue2 = ""Day"";
private string itemLabelTemplateValue = ""Day"";

private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];

private readonly List<BitChoiceGroupItem<string>> itemTemplateItems =
[
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
];

private List<BitChoiceGroupItem<string>> itemTemplateItems2 = default!;
protected override void OnInitialized()
{
    itemTemplateItems2 = new()
    {
        new()
        {
            Text = ""Day"",
            Value = ""Day"",
            Template = (item => @<div class=""custom-container @(itemTemplateValue2 == item.Value ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:red"">@item.Text</span>
                                 </div>)
        },
        new()
        {
            Text = ""Week"",
            Value = ""Week"",
            Template = (item => @<div class=""custom-container @(itemTemplateValue2 == item.Value ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:green"">@item.Text</span>
                                 </div>)
        },
        new()
        {
            Text = ""Month"",
            Value = ""Month"",
            Template = (item => @<div class=""custom-container @(itemTemplateValue2 == item.Value ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:blue"">@item.Text</span>
                                 </div>)
        }
    };
}";

    private readonly string example9RazorCode = @"
<BitChoiceGroup Label=""ساده"" Items=""rtlItems"" DefaultValue=""@(""A"")"" Dir=""BitDir.Rtl"" />
<BitChoiceGroup Label=""غیرفعال"" Items=""rtlItems"" IsEnabled=""false"" DefaultValue=""@(""A"")"" Dir=""BitDir.Rtl"" />";
    private readonly string example9CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> rtlItems = new()
{
    new() { Text = ""بخش آ"", Value = ""A"" },
    new() { Text = ""بخش ب"", Value = ""B"" },
    new() { Text = ""بخش پ"", Value = ""C"" },
    new() { Text = ""بخش ت"", Value = ""D"" }
};";

    private readonly string example10RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    
    <BitChoiceGroup Items=""basicItems"" @bind-Value=""validationModel.Value"" />
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

private readonly List<BitChoiceGroupItem<string>> basicItems =
[
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
];";
}
