using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NumericTextField;

public partial class BitNumericTextFieldDemo
{
    private int BasicNumericTextFieldValue = 5;
    private double BasicNumericTextFieldDisableValue = 20;
    private double NumericTextFieldWithLabelAboveValue = 7;
    private double BitNumericTextFieldBindValue = 8;
    private decimal BitNumericTextFieldValueChanged = 16;

    private string SuccessMessage = string.Empty;
    private BitNumericTextFieldValidationModel ValidationModel = new();

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

    private void HandleControlledNumericTextFieldValueChange(decimal value)
    {
        BitNumericTextFieldValueChanged = value;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the input for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "AriaPositionInSet",
            Type = "int",
            DefaultValue = "0",
            Description = "The position in the parent set (if in a set).",
        },
        new ComponentParameter()
        {
            Name = "AriaSetSize",
            Type = "int",
            DefaultValue = "0",
            Description = "The total size of the parent set (if in a set).",
        },
        new ComponentParameter()
        {
            Name = "AriaValueNow",
            Type = "double",
            DefaultValue = "0",
            Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
        },
        new ComponentParameter()
        {
            Name = "AriaValueText",
            Type = "string",
            DefaultValue = "",
            Description = "Sets the control's aria-valuetext.",
        },
        new ComponentParameter()
        {
            Name = "Arrows",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enable/Disable arrow keys",
        },
        new ComponentParameter()
        {
            Name = "DecrementButtonAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Accessible label text for the decrement button (for screen reader users).",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "double",
            DefaultValue = "0",
            Description = "Initial value of the numeric textfield.",
        },
        new ComponentParameter()
        {
            Name = "DecrementButtonIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronDownSmall",
            Description = "Custom icon name for the decrement button.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIconName",
            DefaultValue = "",
            Description = "Icon name for an icon to display alongside the numeric textfield's label.",
        },
        new ComponentParameter()
        {
            Name = "IconAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "IncrementButtonAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Accessible label text for the increment button (for screen reader users).",
        },
        new ComponentParameter()
        {
            Name = "IncrementButtonIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronUpSmall",
            Description = "Custom icon name for the increment button.",
        },
        new ComponentParameter()
        {
            Name = "InputHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "",
            Description = "Additional props for the input field.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Descriptive label for the numeric textfield, Label displayed above the numeric textfield and read by screen readers.",
        },
        new ComponentParameter()
        {
            Name = "LabelFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Shows the custom Label for numeric textfield. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id..",
        },
        new ComponentParameter()
        {
            Name = "LabelPosition",
            Type = "BitNumericTextFieldLabelPosition",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum",
            DefaultValue = "BitNumericTextFieldLabelPosition.Left",
            Description = "The position of the label in regards to the numeric textfield.",
        },
        new ComponentParameter()
        {
            Name = "Max",
            Type = "double",
            DefaultValue = "0",
            Description = "Max value of the numeric textfield. If not provided, the numeric textfield has max value of double type.",
        },
        new ComponentParameter()
        {
            Name = "Min",
            Type = "double",
            DefaultValue = "0",
            Description = "Min value of the numeric textfield. If not provided, the numeric textfield has minimum value of double type.",
        },
        new ComponentParameter()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the control loses focus.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "Callback for when the numeric textfield value change.",
        },
        new ComponentParameter()
        {
            Name = "OnDecrement",
            Type = "EventCallback<BitNumericTextFieldChangeEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitNumericTextFieldChangeEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the increment button or up arrow key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "Precision",
            Type = "int",
            DefaultValue = "",
            Description = "How many decimal places the value should be rounded to.",
        },
        new ComponentParameter()
        {
            Name = "Step",
            Type = "double",
            DefaultValue = "1",
            Description = "Difference between two adjacent values of the numeric textfield.",
        },
        new ComponentParameter()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "",
            Description = "A text is shown after the numeric textfield value.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "ChevronUpSmall",
            Description = "A more descriptive title for the control, visible on its tooltip.",
        },
        new ComponentParameter()
        {
            Name = "Value",
            Type = "double",
            DefaultValue = "0",
            Description = "Current value of the numeric textfield.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "Callback for when the numeric textfield value change.",
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
            Id = "labelPosition-enum",
            Title = "BitNumericTextFieldLabelPosition Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Top",
                    Description="The label shows on the top of the numeric textfield.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the numeric textfield.",
                    Value="1",
                }
            }
        },
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

    private readonly string example1HTMLCode = @"<BitNumericTextField @bind-Value=""BasicNumericTextFieldValue""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Basic NumericTextField with integer value"">
</BitNumericTextField>
<BitNumericTextField Min=""-10m""
               Max=""10m""
               Step=""0.1m""
               Label=""Basic NumericTextField with decimal value"">
</BitNumericTextField>
<BitNumericTextField @bind-Value=""BasicNumericTextFieldDisableValue""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Disabled NumericTextField""
               IsEnabled=""false"">
</BitNumericTextField>";

    private readonly string example1CSharpCode = @"
private int BasicNumericTextFieldValue = 5;
private int BasicNumericTextFieldDisableValue = 20;";

    private readonly string example2HTMLCode = @"<BitNumericTextField
               Min=""0""
               Max=""100""
               Step=""1""
               Arrows=""true""
               Label=""Basic NumericTextField with arrow keys"">
</BitNumericTextField>";

    private readonly string example3HTMLCode = @"<BitNumericTextField IconName=""BitIconName.IncreaseIndentText""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""With Icon"">
</BitNumericTextField>
<BitNumericTextField IconName=""BitIconName.IncreaseIndentText""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Disabled With Icon""
               IsEnabled=""false"">
</BitNumericTextField>";

    private readonly string example4HTMLCode = @"<BitNumericTextField Suffix=""Inch""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""With suffix"">
</BitNumericTextField>";

    private readonly string example5HTMLCode = @"<BitNumericTextField @bind-Value=""NumericTextFieldWithLabelAboveValue""
               Suffix=""cm""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""With Labal Above""
               LabelPosition=""@BitNumericTextFieldLabelPosition.Top"">
</BitNumericTextField>";

    private readonly string example5CSharpCode = @"
private double NumericTextFieldWithLabelAboveValue = 7;";

    private readonly string example6HTMLCode = @"<BitNumericTextField Class=""custom-spb""
               Min=""0""
               Max=""100""
               Step=""1""
               Label=""Custom Styled"">
</BitNumericTextField>

<style>
    .custom-spb {
            .bit-spb-wrapper input {
                    background-color: #D7D7D7;
            }
    }
</style>";

    private readonly string example7CSharpCode = @"
private double BitNumericTextFieldBindValue = 8;
private decimal BitNumericTextFieldValueChanged = 16;        
private void HandleControlledNumericTextFieldValueChange(decimal value)
{
    BitNumericTextFieldValueChanged = value;
}";

    private readonly string example7HTMLCode = @"<BitNumericTextField Label=""Controlled NumericTextField with bind-value""
               @bind-Value=""BitNumericTextFieldBindValue""
               Min=""0d""
               Max=""100d""
               Step=""1d"">
</BitNumericTextField>
<BitNumericTextField Label=""Controlled NumericTextField with Value""
               Value=""BitNumericTextFieldValueChanged""
               ValueChanged=""(decimal value) => HandleControlledNumericTextFieldValueChange(value)""
               Min=""0m""
               Max=""100m""
               Step=""1m"">
</BitNumericTextField>";

    private readonly string example8CSharpCode = @"private string SuccessMessage = string.Empty;
private BitNumericTextFieldValidationModel ValidationModel = new();

public class BitNumericTextFieldValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 200, ErrorMessage = ""Nobody is that old"")]
    public double AgeInYears { get; set; }
}

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

    private readonly string example8HTMLCode = @"@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitNumericTextField Label=""Age"" @bind-Value=""@ValidationModel.AgeInYears""></BitNumericTextField>

            <ValidationMessage For=""@(() => ValidationModel.AgeInYears)"" />
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


    #region Example Code 9

    private readonly string example9HTMLCode = @"
<BitTextField Label=""With Placeholder"" Placeholder=""Please enter a number""></BitTextField>";

    #endregion
}
