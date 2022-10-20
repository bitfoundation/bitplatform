using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.RadioButtonGroup;

public partial class BitRadioButtonGroupDemo
{
    private string MyValue = "B";
    private string CustomOptionValue = "Day";
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
            Name = "ChildContent",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of action button, It can be Any custom tag or a text.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "string",
            DefaultValue = "",
            Description = "Default value for RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, an option must be selected in the RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Descriptive label for the RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "LabelFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Used to customize the label for the RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "",
            Description = "Name of RadioButtonGroup, this name is used to group each RadioButtonOption into the same logical RadioButtonGroup.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the action button clicked.",
        },
        new ComponentParameter()
        {
            Name = "OnValueChange",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Callback that is called when the value parameter is changed.",
        },
        new ComponentParameter()
        {
            Name = "Value",
            Type = "string",
            DefaultValue = "",
            Description = "Value of RadioButtonGroup, the value of selected RadioButtonOption set on it.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Callback for when the selected value changed.",
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    #region Example Code 1

    private readonly string example1HTMLCode = @"
<BitLabel>Selected Key is : @MyValue</BitLabel>
<BitTextField @bind-Value=""MyValue"" Placeholder=""Select one of A, B or C""></BitTextField>
<BitRadioButtonGroup Name=""Group1"" Label=""Pick one"" IsRequired=""true"" @bind-Value=""MyValue"">
    <BitRadioButtonOption Text=""Option A"" Value=""A""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Option B"" Value=""B""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Disabled option C"" Value=""C"" IsEnabled=""false""></BitRadioButtonOption>
</BitRadioButtonGroup>";

    private readonly string example1CSharpCode = @"
private string MyValue = ""B"";";

    #endregion

    #region Example Code 2

    private readonly string example2HTMLCode = @"
<BitRadioButtonGroup Name=""Group2"" IsEnabled=""false"" Label=""Pick one"" DefaultValue=""C"">
    <BitRadioButtonOption Text=""Option A"" Value=""A""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Option2 B"" Value=""B""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Disabled option C"" Value=""C"" IsEnabled=""false""></BitRadioButtonOption>
</BitRadioButtonGroup>";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""Pick one image"" DefaultValue=""pie"">
    <BitRadioButtonOption Text=""Clustered bar chart"" Value=""bar"" ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"" ImageAlt=""alt for image Option 1"" SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" ImageSize=""new System.Drawing.Size( width: 32, height: 32)""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Pie chart"" Value=""pie"" ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"" ImageAlt=""alt for image Option 2"" SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" ImageSize=""new System.Drawing.Size( width: 32, height: 32)""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Disabeled"" IsEnabled=""false"" Value=""disabeled_option"" ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"" ImageAlt=""alt for image Option 2"" SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" ImageSize=""new System.Drawing.Size( width: 32, height: 32)""></BitRadioButtonOption>
</BitRadioButtonGroup>";

    #endregion

    #region Example Code 4

    private readonly string example4HTMLCode = @"
<BitRadioButtonGroup Name=""Group1"" Label=""Pick one icon"">
    <BitRadioButtonOption Text=""Day"" Value=""day"" IconName=""BitIconName.CalendarDay""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Week"" Value=""week"" IconName=""BitIconName.CalendarWeek""></BitRadioButtonOption>
    <BitRadioButtonOption Text=""Month"" Value=""month"" IconName=""BitIconName.Calendar"" IsEnabled=""false""></BitRadioButtonOption>
</BitRadioButtonGroup>";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"
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

<BitRadioButtonGroup Name=""Group1"" Label=""Pick one option"" @bind-Value=""CustomOptionValue"">
    <BitRadioButtonOption Value=""Day"">
        <div class=""custom-option @(CustomOptionValue is ""Day"" ? ""selected-option"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""BitIconName.CalendarDay"" />
            <span>Day</span>
        </div>
    </BitRadioButtonOption>
    <BitRadioButtonOption Value=""Week"">
        <div class=""custom-option @(CustomOptionValue is ""Week"" ? ""selected-option"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""BitIconName.CalendarWeek"" />
            <span>Week</span>
        </div>
    </BitRadioButtonOption>
    <BitRadioButtonOption Value=""Month"">
        <div class=""custom-option @(CustomOptionValue is ""Month"" ? ""selected-option"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""BitIconName.Calendar"" />
            <span>Month</span>
        </div>
    </BitRadioButtonOption>
</BitRadioButtonGroup>";

    private readonly string example5CSharpCode = @"
private string CustomOptionValue = ""Day"";";

    #endregion

    #region Example Code 6

    private readonly string example6HTMLCode = @"
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

        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";

    private readonly string example6CSharpCode = @"
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
}";

    #endregion
}
