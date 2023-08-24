namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupItemDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string itemTemplateValue = "Day";
    private string itemLabelTemplateValue = "Day";
    public ChoiceGroupValidationModel validationModel = new();
    public string? successMessage;


    private readonly List<BitChoiceGroupItem<string>> basicItems = new()
    {
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
        new() { Text = "Item C", Value = "C" },
        new() { Text = "Item D", Value = "D" }
    };
    private readonly List<BitChoiceGroupItem<string>> disabledItems = new()
    {
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
        new() { Text = "Item C", Value = "C", IsEnabled = false },
        new() { Text = "Item D", Value = "D" }
    };
    private readonly List<BitChoiceGroupItem<string>> imageItems = new()
    {
        new()
        {
            Text = "Bar",
            Value = "Bar",
            ImageAlt = "alt for Bar image",
            ImageSize = new BitSize(32, 32),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Text = "Pie",
            Value = "Pie",
            ImageAlt = "alt for Pie image",
            ImageSize = new BitSize(32, 32),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    };
    private readonly List<BitChoiceGroupItem<string>> iconItems = new()
    {
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    };
    private readonly List<BitChoiceGroupItem<string>> itemTemplateItems = new()
    {
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar }
    };
    private readonly List<BitChoiceGroupItem<string>> rtlItems = new()
    {
        new() { Text = "بخش آ", Value = "A" },
        new() { Text = "بخش ب", Value = "B" },
        new() { Text = "بخش پ", Value = "C" },
        new() { Text = "بخش ت", Value = "D" }
    };


    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }


    private readonly string example1HtmlCode = @"
<BitChoiceGroup Label=""Basic Items"" Items=""basicItems"" DefaultValue=""basicItems[1].Value"" />";
    private readonly string example1CsharpCode = @"
private readonly List<BitChoiceGroupItem> basicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                Items=""basicItems""
                DefaultValue=""@(""A"")"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Items""
                Items=""disabledItems""
                DefaultValue=""@(""A"")"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitChoiceGroupItem> basicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> disabledItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"", IsEnabled = false },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Image Items"" Items=""imageItems"" DefaultValue=""@(""Bar"")"" />

<BitChoiceGroup Label=""Icon Items"" Items=""iconItems"" DefaultValue=""@(""Day"")"" />";
    private readonly string example3CsharpCode = @"
private readonly List<BitChoiceGroupItem> imageItems<string> = new()
{
    new BitChoiceGroupItem()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new BitSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new BitChoiceGroupItem()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new BitSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
};

private readonly List<BitChoiceGroupItem> iconItems<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};";

    private readonly string example4HtmlCode = @"
<style>
    .custom-label {
        color: #A4262C;
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
    private readonly string example4CsharpCode = @"
private readonly List<BitChoiceGroupItem> basicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example5HtmlCode = @"
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

<BitChoiceGroup Label=""ItemLabelTemplate"" Items=""itemTemplateItems"" @bind-Value=""itemLabelTemplateValue"">
    <ItemLabelTemplate Context=""item"">
        <div style=""margin-left:30px;height:20px"" class=""custom-container @(itemLabelTemplateValue == item.Value ? ""selected"" : string.Empty)"">
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
</BitChoiceGroup>";
    private readonly string example5CsharpCode = @"
private string itemLabelTemplateValue = ""Day"";
private string itemTemplateValue = ""Day"";

private readonly List<BitChoiceGroupItem> itemTemplateItems<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};";

    private readonly string example6HtmlCode = @"
<BitChoiceGroup Label=""One-way"" Items=""basicItems"" Value=""@oneWayValue"" />
<BitTextField @bind-Value=""oneWayValue"" />

<BitChoiceGroup Label=""Two-way"" Items=""basicItems"" @bind-Value=""twoWayValue"" />
<BitTextField @bind-Value=""twoWayValue"" />";
    private readonly string example6CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";

private readonly List<BitChoiceGroupItem> basicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""Basic"" Items=""basicItems"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Disabled"" Items=""basicItems"" IsEnabled=""false"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Image"" Items=""imageItems"" DefaultValue=""@(""Bar"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Icon"" Items=""iconItems"" DefaultValue=""@(""Day"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />";
    private readonly string example7CsharpCode = @"
private readonly List<BitChoiceGroupItem> basicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> imageItems<string> = new()
{
    new BitChoiceGroupItem()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new BitSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new BitChoiceGroupItem()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new BitSize(32, 32),
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
};

private readonly List<BitChoiceGroupItem> iconItems<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};";

    private readonly string example8HtmlCode = @"
<BitChoiceGroup Label=""Basic"" Items=""rtlItems"" DefaultValue=""@(""A"")"" IsRtl=""true"" />

<BitChoiceGroup Label=""Disabled"" Items=""rtlItems"" IsEnabled=""false"" DefaultValue=""@(""A"")"" IsRtl=""true"" />";
    private readonly string example8CsharpCode = @"
private readonly List<BitChoiceGroupItem<string>> rtlItems = new()
{
    new() { Text = ""بخش آ"", Value = ""A"" },
    new() { Text = ""بخش ب"", Value = ""B"" },
    new() { Text = ""بخش پ"", Value = ""C"" },
    new() { Text = ""بخش ت"", Value = ""D"" }
};";

    private readonly string example9HtmlCode = @"
<style>
    .validation-message {
        color: #A4262C;
        font-size: rem2(12px);
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    <div>
        <BitChoiceGroup Items=""basicItems"" @bind-Value=""validationModel.Value"" />
        <ValidationMessage For=""@(() => validationModel.Value)"" />
    </div>
    <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example9CSharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel validationModel = new();

private readonly List<BitChoiceGroupItem> basicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private void HandleValidSubmit() { }

private void HandleInvalidSubmit() { }";
}
