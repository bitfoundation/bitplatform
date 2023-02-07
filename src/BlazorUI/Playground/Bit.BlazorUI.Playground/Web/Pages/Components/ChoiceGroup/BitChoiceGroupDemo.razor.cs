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

    private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
    {
        new() { Text = "Option A", Value = "A" },
        new() { Text = "Option B", Value = "B" },
        new() { Text = "Option C", Value = "C" },
        new() { Text = "Option D", Value = "D" }
    };
    private readonly List<BitChoiceGroupOption> ChoiceGroupWithDisabledOption = new()
    {
        new() { Text = "Option A", Value = "A" },
        new() { Text = "Option B", Value = "B" },
        new() { Text = "Option C", Value = "C", IsEnabled = false },
        new() { Text = "Option D", Value = "D" }
    };
    private readonly List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Bar",
            Value = "Bar",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
            ImageAlt = "alt for Bar image",
            ImageSize = new Size(32, 32)
        },
        new BitChoiceGroupOption()
        {
            Text = "Pie",
            Value = "Pie",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
            ImageAlt = "alt for Pie image",
            ImageSize = new Size(32, 32)
        }
    };
    private readonly List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
    {
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    };
    private readonly List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
    {
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar }
    };


    private readonly string example1HtmlCode = @"
 <BitChoiceGroup Label=""Pick one"" Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" />
";
    private readonly string example1CSharpCode = @"
private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup"" Options=""ChoiceGroupBasicOption"" IsEnabled=""false"" DefaultValue=""A"" />
<BitChoiceGroup Label=""ChoiceGroup with Disabled Option"" Options=""ChoiceGroupWithDisabledOption"" DefaultValue=""A"" />
";
    private readonly string example2CSharpCode = @"
private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};

private readonly List<BitChoiceGroupOption> ChoiceGroupWithDisabledOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"", IsEnabled = false },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Options=""ChoiceGroupWithImage"" DefaultValue=""Bar"" />
";
    private readonly string example3CSharpCode = @"
private List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupOption()
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

    private readonly string example4HtmlCode = @"
<BitChoiceGroup Label=""Pick one icon"" Options=""ChoiceGroupWithIcon"" DefaultValue=""Day"" />
";
    private readonly string example4CSharpCode = @"
private readonly List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};
";

    private readonly string example5HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: red;
    }
</style>

<BitChoiceGroup Options=""ChoiceGroupBasicOption"" DefaultValue=""A"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
";
    private readonly string example5CSharpCode = @"
private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example6HtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: 10px;
        cursor: pointer;
    }
    .custom-option .option-pointer {
        width: 20px;
        height: 20px;
        border: 1px solid;
        border-radius: 10px;
    }

    .custom-option:hover .option-pointer {
        border-top: 5px solid #C66;
        border-bottom: 5px solid #6C6;
        border-left: 5px solid #66C;
        border-right: 5px solid #CC6;
    }

    .custom-option.selected-option {
        color: #C66;
    }

    .custom-option.selected-option .option-pointer {
        border-top: 10px solid #C66;
        border-bottom: 10px solid #6C6;
        border-left: 10px solid #66C;
        border-right: 10px solid #CC6;
    }
</style>

<BitChoiceGroup Label=""Option Template"" Options=""ChoiceGroupWithOptionTemplate"" @bind-Value=""ChoiceGroupWithOptionTemplateValue"">
    <OptionTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Label Template"" Options=""ChoiceGroupWithOptionTemplate"" @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"">
    <OptionLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionLabelTemplate>
</BitChoiceGroup>
";
    private readonly string example6CSharpCode = @"
private string ChoiceGroupWithOptionTemplateValue;
private string ChoiceGroupWithOptionLabelTemplateValue;
private readonly List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};
";

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""One-way"" Options=""ChoiceGroupBasicOption"" Value=""@ChoiceGroupOneWayValue"" />
<BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />

<BitChoiceGroup Label=""Two-way"" Options=""ChoiceGroupBasicOption"" @bind-Value=""ChoiceGroupTwoWayValue"" />
<BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />
";
    private readonly string example7CSharpCode = @"
private string ChoiceGroupOneWayValue;
private string ChoiceGroupTwoWayValue;
private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
";

    private readonly string example8HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: red;
    }

    .custom-option {
        display: flex;
        align-items: center;
        gap: 10px;
        cursor: pointer;
    }

    .custom-option .option-pointer {
        width: 20px;
        height: 20px;
        border: 1px solid;
        border-radius: 10px;
    }

    .custom-option:hover .option-pointer {
        border-top: 5px solid #C66;
        border-bottom: 5px solid #6C6;
        border-left: 5px solid #66C;
        border-right: 5px solid #CC6;
    }

    .custom-option.selected-option {
        color: #C66;
    }

    .custom-option.selected-option .option-pointer {
        border-top: 10px solid #C66;
        border-bottom: 10px solid #6C6;
        border-left: 10px solid #66C;
        border-right: 10px solid #CC6;
    }
</style>

<BitChoiceGroup Label=""Basic"" Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Disabled"" Options=""ChoiceGroupBasicOption"" IsEnabled=""false"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Image"" Options=""ChoiceGroupWithImage"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Label=""Icon"" Options=""ChoiceGroupWithIcon"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" />
<BitChoiceGroup Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupLayoutFlowWithOptionTemplateValue""
                Options=""ChoiceGroupWithOptionTemplate""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <OptionTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionTemplate>
</BitChoiceGroup>
";
    private readonly string example8CSharpCode = @"
private string ChoiceGroupLayoutFlowWithOptionTemplateValue;
private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
private readonly List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupOption()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};
private readonly List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};
private readonly List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};
";

    private readonly string example9HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: red;
    }

    .custom-option {
        display: flex;
        align-items: center;
        gap: 10px;
        cursor: pointer;
    }

    .custom-option .option-pointer {
        width: 20px;
        height: 20px;
        border: 1px solid;
        border-radius: 10px;
    }

    .custom-option:hover .option-pointer {
        border-top: 5px solid #C66;
        border-bottom: 5px solid #6C6;
        border-left: 5px solid #66C;
        border-right: 5px solid #CC6;
    }

    .custom-option.selected-option {
        color: #C66;
    }

    .custom-option.selected-option .option-pointer {
        border-top: 10px solid #C66;
        border-bottom: 10px solid #6C6;
        border-left: 10px solid #66C;
        border-right: 10px solid #CC6;
    }
</style>

<BitChoiceGroup Label=""Basic"" Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" IsRtl=""true"" />
<BitChoiceGroup Label=""Disabled"" Options=""ChoiceGroupBasicOption"" IsEnabled=""false"" DefaultValue=""A"" IsRtl=""true"" />
<BitChoiceGroup Label=""Image"" Options=""ChoiceGroupWithImage"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />
<BitChoiceGroup Label=""Icon"" Options=""ChoiceGroupWithIcon"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />
<BitChoiceGroup Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupRtlLayoutFlowWithOptionTemplateValue""
                Options=""ChoiceGroupWithOptionTemplate""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <OptionTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupRtlLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionTemplate>
</BitChoiceGroup>
";
    private readonly string example9CSharpCode = @"
private string ChoiceGroupRtlWithOptionTemplateValue;
private readonly List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
{
    new() { Text = ""Option A"", Value = ""A"" },
    new() { Text = ""Option B"", Value = ""B"" },
    new() { Text = ""Option C"", Value = ""C"" },
    new() { Text = ""Option D"", Value = ""D"" }
};
private readonly List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupOption()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};
private readonly List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};
private readonly List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
{
    new() { Text = ""Day"", Value = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Text = ""Week"", Value = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Text = ""Month"", Value = ""Month"", IconName = BitIconName.Calendar }
};
";

    private readonly string example10HtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Options=""ChoiceGroupBasicOption"" @bind-Value=""ValidationModel.Value"" />
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
";
    private readonly string example10CSharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public string SuccessMessage;
public ChoiceGroupValidationModel ValidationModel = new();
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
}
