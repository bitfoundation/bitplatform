using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.RadioButtonGroup;

public partial class BitRadioButtonGroupDemo
{
    private string OptionTemplateValue;
    private string OptionLabelTemplateValue;
    private string ChoiceGroupOneWayValue;
    private string ChoiceGroupTwoWayValue;
    private string SuccessMessage = string.Empty;
    private FormValidationModel FormValidationModel = new();

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

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLabelledBy",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "ID of an element to use as the aria label for this RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of RadioButtonGroup, common values are RadioButtonGroup component.",
            LinkType = LinkType.Link,
            Href = "#radio-button-option"
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "string?",
            Description = "Default value for RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "IsRequired",
            Type = "bool",
            Description = "If true, an option must be selected in the RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string?",
            Description = "Descriptive label for the RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the label for the RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "a Guid",
            Description = "Name of RadioButtonGroup, this name is used to group each RadioButtonGroup into the same logical RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "OnValueChange",
            Type = "EventCallback<string>",
            Description = "Callback that is called when the value parameter is changed.",
        }
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "radio-button-option",
            Title = "BitRadioButtonOption",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "ChildContent",
                   Type = "RenderFragment?",
                   Description = "Used to customize the label for the RadioButtonOption.",
               },
               new ComponentParameter()
               {
                   Name = "IsChecked",
                   Type = "bool",
                   Description = "Whether or not the option is checked.",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "Icon to display with this option.",
               },
               new ComponentParameter()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   Description = "Image src to display with this option.",
               },
               new ComponentParameter()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   Description = "Alt text if the option is an image. default is an empty string.",
               },
               new ComponentParameter()
               {
                   Name = "ImageSize",
                   Type = "Size?",
                   Description = "The width and height of the image in px for choice field.",
               },
               new ComponentParameter()
               {
                   Name = "LabelTemplate",
                   Type = "RenderFragment?",
                   Description = "Used to customize the label for the RadioButtonGroupOption.",
               },
               new ComponentParameter()
               {
                   Name = "Name",
                   Type = "string?",
                   Description = "This value is used to group each RadioButtonGroupOption into the same logical RadioButtonGroup.",
               },
               new ComponentParameter()
               {
                   Name = "OnClick",
                   Type = "EventCallback<MouseEventArgs>",
                   Description = "Callback for when the RadioButtonOption clicked.",
               },
               new ComponentParameter()
               {
                   Name = "OnChange",
                   Type = "EventCallback<bool>",
                   Description = "Callback for when the option has been changed.",
               },
               new ComponentParameter()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   Description = "The src of image for choice field which is selected.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "RadioButtonOption content, It can be a text.",
               },
               new ComponentParameter()
               {
                   Name = "Value",
                   Type = "string?",
                   Description = "Value of selected RadioButtonOption.",
               }
            }
        }
    };

    #region Example Code 1

    private readonly string example1HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""Pick one"">
    <BitRadioButtonOption Text=""Option A"" Value=""A"" />
    <BitRadioButtonOption Text=""Option B"" Value=""B"" />
    <BitRadioButtonOption Text=""Option C"" Value=""C"" />
</BitRadioButtonGroup>
";

    #endregion

    #region Example Code 2

    private readonly string example2HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""Disabled RadioButtonGroup"" IsEnabled=""false"">
    <BitRadioButtonOption Text=""Option A"" Value=""A"" />
    <BitRadioButtonOption Text=""Option B"" Value=""B"" />
    <BitRadioButtonOption Text=""Option C"" Value=""C"" />
</BitRadioButtonGroup>

<BitRadioButtonGroup Name=""Group1"" Label=""Disabled RadioButtonOption"">
    <BitRadioButtonOption Text=""Option A"" Value=""A"" />
    <BitRadioButtonOption Text=""Option B"" Value=""B"" IsEnabled=""false"" />
    <BitRadioButtonOption Text=""Option C"" Value=""C"" />
</BitRadioButtonGroup>
";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""Pick one image"" DefaultValue=""bar"">
    <BitRadioButtonOption Text=""Clustered bar chart""
                            Value=""bar""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            ImageAlt=""alt for image Option 1""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageSize=""new System.Drawing.Size( width: 32, height: 32)"" />
    <BitRadioButtonOption Text=""Pie chart""
                            Value=""pie""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            ImageAlt=""alt for image Option 2""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageSize=""new System.Drawing.Size( width: 32, height: 32)"" />
</BitRadioButtonGroup>
";

    #endregion

    #region Example Code 4

    private readonly string example4HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""Pick one icon"">
    <BitRadioButtonOption Text=""Day"" Value=""day"" IconName=""BitIconName.CalendarDay"" />
    <BitRadioButtonOption Text=""Week"" Value=""week"" IconName=""BitIconName.CalendarWeek"" />
    <BitRadioButtonOption Text=""Month"" Value=""month"" IconName=""BitIconName.Calendar"" IsEnabled=""false"" />
</BitRadioButtonGroup>
";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitRadioButtonGroup Name=""Group1"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitRadioButtonOption Text=""Option A"" Value=""A"" />
        <BitRadioButtonOption Text=""Option B"" Value=""B"" />
        <BitRadioButtonOption Text=""Option C"" Value=""C"" />
    </ChildContent>
</BitRadioButtonGroup>
";

    #endregion

    #region Example Code 6

    private readonly string example6HTMLCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .radio-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .radio-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .radio-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitRadioButtonGroup Name=""Group1"" Label=""Option Template"" @bind-Value=""OptionTemplateValue"" DefaultValue=""Day"">
    <BitRadioButtonOption Value=""Day"">
        <div class=""custom-option @(OptionTemplateValue is ""Day"" ? ""selected-option"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""BitIconName.CalendarDay"" />
            <span>Day</span>
        </div>
    </BitRadioButtonOption>
    <BitRadioButtonOption Value=""Week"">
        <div class=""custom-option @(OptionTemplateValue is ""Week"" ? ""selected-option"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""BitIconName.CalendarWeek"" />
            <span>Week</span>
        </div>
    </BitRadioButtonOption>
    <BitRadioButtonOption Value=""Month"">
        <div class=""custom-option @(OptionTemplateValue is ""Month"" ? ""selected-option"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""BitIconName.Calendar"" />
            <span>Month</span>
        </div>
    </BitRadioButtonOption>
</BitRadioButtonGroup>

<BitRadioButtonGroup Name=""Group1"" Label=""Option Label Template"" @bind-Value=""OptionLabelTemplateValue"" DefaultValue=""Day"">
    <BitRadioButtonOption Value=""Day"">
        <LabelTemplate>
            <div style=""margin-left: 27px;"" class=""custom-option @(OptionLabelTemplateValue == ""Day"" ? ""selected-option"" : """")"">
                <BitIcon IconName=""BitIconName.CalendarDay"" />
                <span>Day</span>
            </div>
        </LabelTemplate>
    </BitRadioButtonOption>
    <BitRadioButtonOption Value=""Week"">
        <LabelTemplate>
            <div style=""margin-left: 27px;"" class=""custom-option @(OptionLabelTemplateValue is ""Week"" ? ""selected-option"" : """")"">
                <BitIcon IconName=""BitIconName.CalendarWeek"" />
                <span>Week</span>
            </div>
        </LabelTemplate>
    </BitRadioButtonOption>
    <BitRadioButtonOption Value=""Month"">
        <LabelTemplate>
            <div style=""margin-left: 27px;"" class=""custom-option @(OptionLabelTemplateValue is ""Month"" ? ""selected-option"" : """")"">
                <BitIcon IconName=""BitIconName.Calendar"" />
                <span>Month</span>
            </div>
        </LabelTemplate>
    </BitRadioButtonOption>
</BitRadioButtonGroup>
";

    private readonly string example6CSharpCode = @"
private string OptionTemplateValue;
private string OptionLabelTemplateValue;
";

    #endregion

    #region Example Code 7

    private readonly string example7HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""One-way"" Value=""@ChoiceGroupOneWayValue"">
    <BitRadioButtonOption Text=""Option A"" Value=""A"" />
    <BitRadioButtonOption Text=""Option B"" Value=""B"" />
    <BitRadioButtonOption Text=""Option C"" Value=""C"" />
</BitRadioButtonGroup>
<BitTextField Placeholder=""A..."" @bind-Value=""ChoiceGroupOneWayValue"" />

<BitRadioButtonGroup Name=""Group1"" Label=""Two-way"" @bind-Value=""ChoiceGroupTwoWayValue"">
    <BitRadioButtonOption Text=""Option A"" Value=""A"" />
    <BitRadioButtonOption Text=""Option B"" Value=""B"" />
    <BitRadioButtonOption Text=""Option C"" Value=""C"" />
</BitRadioButtonGroup>
<BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />
";

    private readonly string example7CSharpCode = @"
private string ChoiceGroupOneWayValue;
private string ChoiceGroupTwoWayValue;
";

    #endregion

    #region Example Code 8

    private readonly string example8HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""FormValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitRadioButtonGroup Name=""Group1"" Label=""Pick one"" @bind-Value=""@FormValidationModel.Option"">
                <BitRadioButtonOption Text=""Option A"" Value=""A""></BitRadioButtonOption>
                <BitRadioButtonOption Text=""Option B"" Value=""B""></BitRadioButtonOption>
                <BitRadioButtonOption Text=""Disabled option C"" Value=""C"" IsEnabled=""false""></BitRadioButtonOption>
            </BitRadioButtonGroup>
            <ValidationMessage For=""@(() => FormValidationModel.Option)"" />
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

    private readonly string example8CSharpCode = @"
public class FormValidationModel
{
    [Required]
    public string Option { get; set; }
}

private string SuccessMessage = string.Empty;
private FormValidationModel FormValidationModel = new();

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

    #endregion
}
