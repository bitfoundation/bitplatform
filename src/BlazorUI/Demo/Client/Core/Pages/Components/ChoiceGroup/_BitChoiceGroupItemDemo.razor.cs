namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ChoiceGroup;

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
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
            ImageAlt = "alt for Bar image",
            ImageSize = new BitSize(32, 32)
        },
        new()
        {
            Text = "Pie",
            Value = "Pie",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
            ImageAlt = "alt for Pie image",
            ImageSize = new BitSize(32, 32)
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
<BitChoiceGroup Label=""Pick one"" Items=""ChoiceGroupBasicItems"" DefaultValue=""@(""A"")"" />";
    private readonly string example1CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                Items=""ChoiceGroupBasicItems""
                IsEnabled=""false""
                DefaultValue=""@(""A"")"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                Items=""ChoiceGroupDisabledItems""
                DefaultValue=""@(""A"")"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupDisabledItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"", IsEnabled = false },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Items=""ChoiceGroupImageItems"" DefaultValue=""@(""Bar"")"" />
<BitChoiceGroup Label=""Pick one icon"" Items=""ChoiceGroupIconItems"" DefaultValue=""@(""Day"")"" />";
    private readonly string example3CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems<string> = new()
{
    new BitChoiceGroupItem()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupItem()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};";

    private readonly string example4HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitChoiceGroup Items=""ChoiceGroupBasicItems"" DefaultValue=""@(""A"")"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>";
    private readonly string example4CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};";

    private readonly string example5HtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;
    }

    .custom-option .option-pointer {
        width: rem(20px);
        height: rem(20px);
        border: 1px solid;
        border-radius: rem(10px);
    }

    .custom-option:hover .option-pointer {
        border-top: rem(5px) solid #C66;
        border-bottom: rem(5px) solid #6C6;
        border-left: rem(5px) solid #66C;
        border-right: rem(5px) solid #CC6;
    }

    .custom-option.selected-option {
        color: #C66;
    }

    .custom-option.selected-option .option-pointer {
        border-top: rem(10px) solid #C66;
        border-bottom: rem(10px) solid #6C6;
        border-left: rem(10px) solid #66C;
        border-right: rem(10px) solid #CC6;
    }
</style>

<BitChoiceGroup Label=""Option Template""
                Items=""ChoiceGroupItemsTemplate""
                @bind-Value=""ChoiceGroupWithOptionTemplateValue"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Label Template""
                Items=""ChoiceGroupItemsTemplate""
                @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"">
    <ItemLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>";
    private readonly string example5CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupWithOptionTemplateValue = ""Day"";
private string ChoiceGroupWithOptionLabelTemplateValue = ""Day"";";

    private readonly string example6HtmlCode = @"
<BitChoiceGroup Label=""One-way""
                Items=""ChoiceGroupBasicItems""
                Value=""@ChoiceGroupOneWayValue"" />
<BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />

<BitChoiceGroup Label=""Two-way""
                Items=""ChoiceGroupBasicItems""
                @bind-Value=""ChoiceGroupTwoWayValue"" />
<BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />";
    private readonly string example6CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private string ChoiceGroupOneWayValue = ""A"";
private string ChoiceGroupTwoWayValue = ""A"";";

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""Basic"" Items=""ChoiceGroupBasicItems"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Disabled"" Items=""ChoiceGroupBasicItems"" IsEnabled=""false"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Image"" Items=""ChoiceGroupImageItems"" DefaultValue=""@(""Bar"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Icon"" Items=""ChoiceGroupIconItems"" DefaultValue=""@(""Day"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Items=""ChoiceGroupBasicItems"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupLayoutFlowWithOptionTemplateValue""
                Items=""ChoiceGroupItemsTemplate""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>";
    private readonly string example7CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems<string> = new()
{
    new BitChoiceGroupItem()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupItem()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupLayoutFlowWithOptionTemplateValue = ""Day"";";

    private readonly string example8HtmlCode = @"
<BitChoiceGroup Label=""Basic"" Items=""ChoiceGroupBasicItems"" DefaultValue=""@(""A"")"" IsRtl=""true"" />
<BitChoiceGroup Label=""Disabled"" Items=""ChoiceGroupBasicItems"" IsEnabled=""false"" DefaultValue=""@(""A"")"" IsRtl=""true"" />
<BitChoiceGroup Label=""Image"" Items=""ChoiceGroupImageItems"" DefaultValue=""@(""Bar"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />
<BitChoiceGroup Label=""Icon"" Items=""ChoiceGroupIconItems"" DefaultValue=""@(""Day"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />
<BitChoiceGroup Items=""ChoiceGroupBasicItems"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupRtlLayoutFlowWithOptionTemplateValue""
                Items=""ChoiceGroupItemsTemplate""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupRtlLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>";
    private readonly string example8CsharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems<string> = new()
{
    new BitChoiceGroupItem()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupItem()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate<string> = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = ""Day"";";

    private readonly string example9HtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Items=""ChoiceGroupBasicItems"" @bind-Value=""ValidationModel.Value"" />
            <ValidationMessage For=""@(() => ValidationModel.Value)"" />
        </div>
        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}
<br />
<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => { ValidationModel = new(); SuccessMessage=string.Empty; }"">Reset</BitButton>";
    private readonly string example9CSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems<string> = new()
{
    new() { Text = ""Item A"", Value = ""A"" },
    new() { Text = ""Item B"", Value = ""B"" },
    new() { Text = ""Item C"", Value = ""C"" },
    new() { Text = ""Item D"", Value = ""D"" }
};

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel ValidationModel = new();
public string SuccessMessage;

private void HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";
}
