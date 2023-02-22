using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup;

public partial class BitChoiceGroupDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            Description = "ID of an element to use as the aria label for this ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "DefaultValue",
            Type = "string?",
            Description = "Default selected Value for ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, an option must be selected in the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "IsRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Change direction to RTL."
        },
        new ComponentParameter
        {
            Name = "Label",
            Type = "string?",
            Description = "Descriptive label for the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the label for the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "LayoutFlow",
            Type = "BitLayoutFlow?",
            Description = "You can define the ChoiceGroup in Horizontal or Vertical mode."
        },
        new ComponentParameter
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "a Guid",
            Description = "Name of ChoiceGroup, this name is used to group each option into the same logical ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "Options",
            Type = "List<BitChoiceGroupOption>",
            DefaultValue = "new List<BitChoiceGroupOption>()",
            Description = "List of options, each of which is a selection in the ChoiceGroup.",
            LinkType = LinkType.Link,
            Href = "#choice-group-option"
        },
        new ComponentParameter
        {
            Name = "OptionTemplate",
            Type = "RenderFragment<BitChoiceGroupOption>?",
            Description = "Used to customize the Option for the ChoiceGroup."
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<BitChoiceGroupOption>",
            Description = "Callback that is called when the ChoiceGroup value has changed.",
        }
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "choice-group-option",
            Title = "BitChoiceGroupOption",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "AriaLabel attribute for the GroupOption Option input.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the GroupOption Option is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "The icon to show as Option content.",
               },
               new ComponentParameter()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   Description = "The image address to show as Option content.",
               },
               new ComponentParameter()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   Description = "Provides alternative information for the Option image.",
               },
               new ComponentParameter()
               {
                   Name = "ImageSize",
                   Type = "Size?",
                   Description = "Provides Height and Width for the Option image.",
               },
               new ComponentParameter()
               {
                   Name = "Id",
                   Type = "string?",
                   Description = "Set attribute of Id for the GroupOption Option input.",
               },
               new ComponentParameter()
               {
                   Name = "LabelId",
                   Type = "string?",
                   Description = "Set attribute of Id for the GroupOption Option label.",
               },
               new ComponentParameter()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   Description = "Provides a new image for the selected Option in the Image-GroupOption.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to show as content of GroupOption Option.",
               },
               new ComponentParameter()
               {
                   Name = "Value",
                   Type = "string?",
                   Description = "This value is returned when GroupOption Option is Clicked.",
               }
            }
        }
    };

    private string ChoiceGroupOneWayValue = "A";
    private string ChoiceGroupTwoWayValue = "A";
    private string ChoiceGroupWithOptionTemplateValue = "Day";
    private string ChoiceGroupWithOptionLabelTemplateValue = "Day";
    private string ChoiceGroupLayoutFlowWithOptionTemplateValue = "Day";
    private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = "Day";
    public ChoiceGroupValidationModel ValidationModel = new();
    public string SuccessMessage;

    private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
    {
        new() { Text = "Option A", Value = "A" },
        new() { Text = "Option B", Value = "B" },
        new() { Text = "Option C", Value = "C" },
        new() { Text = "Option D", Value = "D" }
    };
    private readonly List<BitChoiceGroupItem> ChoiceGroupDisabledItems = new()
    {
        new() { Text = "Option A", Value = "A" },
        new() { Text = "Option B", Value = "B" },
        new() { Text = "Option C", Value = "C", IsEnabled = false },
        new() { Text = "Option D", Value = "D" }
    };
    private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems = new()
    {
        new BitChoiceGroupItem()
        {
            Text = "Bar",
            Value = "Bar",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
            ImageAlt = "alt for Bar image",
            ImageSize = new Size(32, 32)
        },
        new BitChoiceGroupItem()
        {
            Text = "Pie",
            Value = "Pie",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
            ImageAlt = "alt for Pie image",
            ImageSize = new Size(32, 32)
        }
    };
    private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems = new()
    {
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    };
    private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate = new()
    {
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar }
    };

    private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
    {
        new() { Name = "Option A", ItemValue = "A" },
        new() { Name = "Option B", ItemValue = "B" },
        new() { Name = "Option C", ItemValue = "C" },
        new() { Name = "Option D", ItemValue = "D" }
    };
    private readonly List<ChoiceModel> CustomChoiceGroupDisabledItems = new()
    {
        new() { Name = "Option A", ItemValue = "A" },
        new() { Name = "Option B", ItemValue = "B" },
        new() { Name = "Option C", ItemValue = "C", IsEnabled = false },
        new() { Name = "Option D", ItemValue = "D" }
    };
    private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
    {
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
            ImageDescription = "alt for Bar image",
            ImageSize = new Size(32, 32)
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
            ImageDescription = "alt for Pie image",
            ImageSize = new Size(32, 32)
        }
    };
    private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
    {
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    };
    private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
    {
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar }
    };

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }

    private readonly string example1BitChoiceGroupItemHtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Items=""ChoiceGroupBasicItems"" DefaultValue=""A"" />
";
    private readonly string example1BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example2BitChoiceGroupItemHtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                Items=""ChoiceGroupBasicItems""
                IsEnabled=""false""
                DefaultValue=""A"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                Items=""ChoiceGroupDisabledItems""
                DefaultValue=""A"" />
";
    private readonly string example2BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
private readonly List<BitChoiceGroupItem> ChoiceGroupDisabledItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"", IsEnabled = false },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example3BitChoiceGroupItemHtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Items=""ChoiceGroupImageItems"" DefaultValue=""Bar"" />
";
    private readonly string example3BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems = new()
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
";

    private readonly string example4BitChoiceGroupItemHtmlCode = @"
<BitChoiceGroup Label=""Pick one icon"" Items=""ChoiceGroupIconItems"" DefaultValue=""Day"" />
";
    private readonly string example4BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};
";

    private readonly string example5BitChoiceGroupItemHtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitChoiceGroup Items=""ChoiceGroupBasicItems"" DefaultValue=""A"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
";
    private readonly string example5BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example6BitChoiceGroupItemHtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .option-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .option-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .option-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
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
</BitChoiceGroup>
";
    private readonly string example6BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupWithOptionTemplateValue = ""Day"";
private string ChoiceGroupWithOptionLabelTemplateValue = ""Day"";
";

    private readonly string example7BitChoiceGroupItemHtmlCode = @"
<div>
    <BitChoiceGroup Label=""One-way""
                    Items=""ChoiceGroupBasicItems""
                    Value=""@ChoiceGroupOneWayValue"" />
    <div class=""chg-output"">
        <BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />
    </div>
</div>
<div>
    <BitChoiceGroup Label=""Two-way""
                    Items=""ChoiceGroupBasicItems""
                    @bind-Value=""ChoiceGroupTwoWayValue"" />
    <div class=""chg-output"">
        <BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />
    </div>
</div>
";
    private readonly string example7BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};

private string ChoiceGroupOneWayValue = ""A"";
private string ChoiceGroupTwoWayValue = ""A"";
";

    private readonly string example8BitChoiceGroupItemHtmlCode = @"
<BitChoiceGroup Label=""Basic"" Items=""ChoiceGroupBasicItems"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Disabled"" Items=""ChoiceGroupBasicItems"" IsEnabled=""false"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Image"" Items=""ChoiceGroupImageItems"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Icon"" Items=""ChoiceGroupIconItems"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Items=""ChoiceGroupBasicItems"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
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
</BitChoiceGroup>
";
    private readonly string example8BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems = new()
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

private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupLayoutFlowWithOptionTemplateValue = ""Day"";
";

    private readonly string example9BitChoiceGroupItemHtmlCode = @"
<BitChoiceGroup Label=""Basic"" Items=""ChoiceGroupBasicItems"" DefaultValue=""A"" IsRtl=""true"" />
<BitChoiceGroup Label=""Disabled"" Items=""ChoiceGroupBasicItems"" IsEnabled=""false"" DefaultValue=""A"" IsRtl=""true"" />
<BitChoiceGroup Label=""Image"" Items=""ChoiceGroupImageItems"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />
<BitChoiceGroup Label=""Icon"" Items=""ChoiceGroupIconItems"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />
<BitChoiceGroup Items=""ChoiceGroupBasicItems"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
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
</BitChoiceGroup>
";
    private readonly string example9BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupImageItems = new()
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

private readonly List<BitChoiceGroupItem> ChoiceGroupIconItems = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<BitChoiceGroupItem> ChoiceGroupItemsTemplate = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = ""Day"";
";

    private readonly string example10BitChoiceGroupItemHtmlCode = @"
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
<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => { ValidationModel = new(); SuccessMessage=string.Empty; }"">Reset</BitButton>
";
    private readonly string example10BitChoiceGroupItemCSharpCode = @"
private readonly List<BitChoiceGroupItem> ChoiceGroupBasicItems = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel ValidationModel = new();
public string SuccessMessage;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}
";


    private readonly string example1CustomItemHtmlCode = @"
<BitChoiceGroup Label=""Pick one""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A"" />
";
    private readonly string example1CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};
";

    private readonly string example2CustomItemHtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IsEnabled=""false""
                DefaultValue=""A"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                Items=""CustomChoiceGroupDisabledItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A"" />
";
    private readonly string example2CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};
private readonly List<ChoiceModel> CustomChoiceGroupDisabledItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"", IsEnabled = false },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};
";

    private readonly string example3CustomItemHtmlCode = @"
<BitChoiceGroup Label=""Pick one image""
                Items=""CustomChoiceGroupImageItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                ImageSrcField=""@nameof(ChoiceModel.ImageAddress)""
                ImageAltField=""@nameof(ChoiceModel.ImageDescription)""
                ImageSizeField=""@nameof(ChoiceModel.ImageSize)""
                SelectedImageSrcField=""@nameof(ChoiceModel.SelectedImageAddress)""
                DefaultValue=""Bar"" />
";
    private readonly string example3CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageDescription = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageDescription = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};
";

    private readonly string example4CustomItemHtmlCode = @"
<BitChoiceGroup Label=""Pick one icon""
                Items=""CustomChoiceGroupIconItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                DefaultValue=""Day"" />
";
    private readonly string example4CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};
";

    private readonly string example5CustomItemHtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
";
    private readonly string example5CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};
";

    private readonly string example6CustomItemHtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .option-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .option-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .option-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitChoiceGroup Label=""Option Template""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                @bind-Value=""ChoiceGroupWithOptionTemplateValue"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.ItemValue ? ""selected-option"" : string.Empty)"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Label Template""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"">
    <ItemLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.ItemValue ? ""selected-option"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>
";
    private readonly string example6CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupWithOptionTemplateValue = ""Day"";
private string ChoiceGroupWithOptionLabelTemplateValue = ""Day"";
";

    private readonly string example7CustomItemHtmlCode = @"
<div>
    <BitChoiceGroup Label=""One-way""
                    Items=""CustomChoiceGroupBasicItems""
                    TextField=""@nameof(ChoiceModel.Name)""
                    ValueField=""@nameof(ChoiceModel.ItemValue)""
                    Value=""@ChoiceGroupOneWayValue"" />
    <div class=""chg-output"">
        <BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />
    </div>
</div>
<div>
    <BitChoiceGroup Label=""Two-way""
                    Items=""CustomChoiceGroupBasicItems""
                    TextField=""@nameof(ChoiceModel.Name)""
                    ValueField=""@nameof(ChoiceModel.ItemValue)""
                    @bind-Value=""ChoiceGroupTwoWayValue"" />
    <div class=""chg-output"">
        <BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />
    </div>
</div>
";
    private readonly string example7CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};

private string ChoiceGroupOneWayValue = ""A"";
private string ChoiceGroupTwoWayValue = ""A"";
";

    private readonly string example8CustomItemHtmlCode = @"
<BitChoiceGroup Label=""Basic""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Disabled""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IsEnabled=""false"" DefaultValue=""A""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Image""
                Items=""CustomChoiceGroupImageItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                ImageSrcField=""@nameof(ChoiceModel.ImageAddress)""
                ImageAltField=""@nameof(ChoiceModel.ImageDescription)""
                ImageSizeField=""@nameof(ChoiceModel.ImageSize)""
                SelectedImageSrcField=""@nameof(ChoiceModel.SelectedImageAddress)""
                DefaultValue=""Bar""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Icon""
                Items=""CustomChoiceGroupIconItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                DefaultValue=""Day""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template""
                @bind-Value=""@ChoiceGroupLayoutFlowWithOptionTemplateValue""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.ItemValue ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>
";
    private readonly string example8CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageDescription = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageDescription = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupLayoutFlowWithOptionTemplateValue = ""Day"";
";

    private readonly string example9CustomItemHtmlCode = @"
<BitChoiceGroup Label=""Basic""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A""
                IsRtl=""true"" />

<BitChoiceGroup Label=""Disabled""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IsEnabled=""false""
                DefaultValue=""A""
                IsRtl=""true"" />

<BitChoiceGroup Label=""Image""
                Items=""CustomChoiceGroupImageItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                ImageSrcField=""@nameof(ChoiceModel.ImageAddress)""
                ImageAltField=""@nameof(ChoiceModel.ImageDescription)""
                ImageSizeField=""@nameof(ChoiceModel.ImageSize)""
                SelectedImageSrcField=""@nameof(ChoiceModel.SelectedImageAddress)""
                DefaultValue=""Bar""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"" />

<BitChoiceGroup Label=""Icon""
                Items=""CustomChoiceGroupIconItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                DefaultValue=""Day""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"" />

<BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""A""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupRtlLayoutFlowWithOptionTemplateValue""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupRtlLayoutFlowWithOptionTemplateValue == option.ItemValue ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>
";
    private readonly string example9CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageDescription = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageDescription = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = ""Day"";
";

    private readonly string example10CustomItemHtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                        TextField=""@nameof(ChoiceModel.Name)""
                        ValueField=""@nameof(ChoiceModel.ItemValue)""
                        @bind-Value=""ValidationModel.Value"" />
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
<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => { ValidationModel = new(); SuccessMessage=string.Empty; }"">Reset</BitButton>
";
    private readonly string example10CustomItemCSharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Option A"", ItemValue = ""A"" },
    new() { Name = ""Option B"", ItemValue = ""B"" },
    new() { Name = ""Option C"", ItemValue = ""C"" },
    new() { Name = ""Option D"", ItemValue = ""D"" }
};

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel ValidationModel = new();
public string SuccessMessage;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}
";


    private readonly string example1BitChoiceGroupOptionHtmlCode = @"
<BitChoiceGroup TItem=""BitChoiceGroupOption"" Label=""Pick one"" DefaultValue=""A"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>
";

    private readonly string example2BitChoiceGroupOptionHtmlCode = @"
<BitChoiceGroup TItem=""BitChoiceGroupOption""
                Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                DefaultValue=""A"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                TItem=""BitChoiceGroupOption""
                DefaultValue=""A"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" IsEnabled=""false"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>
";

    private readonly string example3BitChoiceGroupOptionHtmlCode = @"
<BitChoiceGroup Label=""Pick one image""
                TItem=""BitChoiceGroupOption""
                DefaultValue=""Bar"">
    <BitChoiceGroupOption Text=""Bar""
                            Value=""Bar""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageAlt=""Alt for Bar image""
                            ImageSize=""@(new Size(32, 32))"" />
    <BitChoiceGroupOption Text=""Pie""
                            Value=""Pie""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png""
                            ImageAlt=""Alt for Pie image""
                            ImageSize=""@(new Size(32, 32))"" />
</BitChoiceGroup>
";

    private readonly string example4BitChoiceGroupOptionHtmlCode = @"
<BitChoiceGroup Label=""Pick one icon""
                TItem=""BitChoiceGroupOption""
                DefaultValue=""Day"">
    <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>
";

    private readonly string example5BitChoiceGroupOptionHtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitChoiceGroup TItem=""BitChoiceGroupOption"" DefaultValue=""A"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
    </ChildContent>
</BitChoiceGroup>
";

    private readonly string example6BitChoiceGroupOptionHtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .option-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .option-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .option-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitChoiceGroup Label=""Option Template""
                TItem=""BitChoiceGroupOption""
                @bind-Value=""ChoiceGroupWithOptionTemplateValue"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" />
    </ChildContent>
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Label Template""
                TItem=""BitChoiceGroupOption""
                @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" />
    </ChildContent>
    <ItemLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>
";
    private readonly string example6BitChoiceGroupOptionCSharpCode = @"
private string ChoiceGroupWithOptionTemplateValue = ""Day"";
private string ChoiceGroupWithOptionLabelTemplateValue = ""Day"";
";

    private readonly string example7BitChoiceGroupOptionHtmlCode = @"
<div>
    <BitChoiceGroup TItem=""BitChoiceGroupOption""
                    Label=""One-way""
                    Value=""@ChoiceGroupOneWayValue"">
        <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
    </BitChoiceGroup>
    <div class=""chg-output"">
        <BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />
    </div>
</div>
<div>
    <BitChoiceGroup TItem=""BitChoiceGroupOption""
                    Label=""Two-way""
                    @bind-Value=""ChoiceGroupTwoWayValue"">
        <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
    </BitChoiceGroup>

    <div class=""chg-output"">
        <BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />
    </div>
</div>
";
    private readonly string example7BitChoiceGroupOptionCSharpCode = @"
private string ChoiceGroupOneWayValue = ""A"";
private string ChoiceGroupTwoWayValue = ""A"";
";

    private readonly string example8BitChoiceGroupOptionHtmlCode = @"
<BitChoiceGroup Label=""Basic"" TItem=""BitChoiceGroupOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Disabled"" TItem=""BitChoiceGroupOption"" IsEnabled=""false"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Image"" TItem=""BitChoiceGroupOption"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Bar""
                            Value=""Bar""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageAlt=""Alt for Bar image""
                            ImageSize=""@(new Size(32, 32))"" />
    <BitChoiceGroupOption Text=""Pie""
                            Value=""Pie""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png""
                            ImageAlt=""Alt for Pie image""
                            ImageSize=""@(new Size(32, 32))"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon"" TItem=""BitChoiceGroupOption"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup TItem=""BitChoiceGroupOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
    </ChildContent>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupLayoutFlowWithOptionTemplateValue""
                TItem=""BitChoiceGroupOption""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" />
    </ChildContent>
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>
";
    private readonly string example8BitChoiceGroupOptionCSharpCode = @"
private string ChoiceGroupLayoutFlowWithOptionTemplateValue = ""Day"";
";

    private readonly string example9BitChoiceGroupOptionHtmlCode = @"
<BitChoiceGroup Label=""Basic"" TItem=""BitChoiceGroupOption"" DefaultValue=""A"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Disabled"" TItem=""BitChoiceGroupOption"" IsEnabled=""false"" DefaultValue=""A"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Image"" TItem=""BitChoiceGroupOption"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Bar""
                            Value=""Bar""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageAlt=""Alt for Bar image""
                            ImageSize=""@(new Size(32, 32))"" />
    <BitChoiceGroupOption Text=""Pie""
                            Value=""Pie""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png""
                            ImageAlt=""Alt for Pie image""
                            ImageSize=""@(new Size(32, 32))"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon"" TItem=""BitChoiceGroupOption"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup TItem=""BitChoiceGroupOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
    </ChildContent>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupRtlLayoutFlowWithOptionTemplateValue""
                TItem=""BitChoiceGroupOption""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""Day"" IconName=""BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""Week"" IconName=""BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""Month"" IconName=""BitIconName.Calendar"" />
    </ChildContent>
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupRtlLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>
";
    private readonly string example9BitChoiceGroupOptionCSharpCode = @"
private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = ""Day"";
";

    private readonly string example10BitChoiceGroupOptionHtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup TItem=""BitChoiceGroupOption"" @bind-Value=""ValidationModel.Value"">
                <BitChoiceGroupOption Text=""Option A"" Value=""A"" />
                <BitChoiceGroupOption Text=""Option B"" Value=""B"" />
                <BitChoiceGroupOption Text=""Option C"" Value=""C"" />
                <BitChoiceGroupOption Text=""Option D"" Value=""D"" />
            </BitChoiceGroup>
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
<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => { ValidationModel = new(); SuccessMessage=string.Empty; }"">Reset</BitButton>
";
    private readonly string example10BitChoiceGroupOptionCSharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel ValidationModel = new();
public string SuccessMessage;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}
";
}
