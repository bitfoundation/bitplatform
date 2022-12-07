using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NumericTextField;

public partial class BitNumericTextFieldDemo
{
    private int BasicStepValue = 1;

    #region Example 1

    private int BasicValue;
    private int DisabledValue;
    private int LabelAndIconValue;
    private int LabelLeftValue;

    #endregion

    #region Example 2

    private int LabelTemplateValue;

    #endregion

    #region Example 3

    private int SpinArrowValue;
    private int SpinArrowWithIconValue;

    #endregion

    #region Example 4

    private int MinMaxValue1;
    private int MinMaxValue2;
    private decimal MinMaxValue3;
    private int MinMaxStepValue2 = 2;
    private decimal MinMaxStepValue3 = 0.1M;

    #endregion

    #region Example 5

    private int SuffixValue1;
    private decimal SuffixValue2;
    private decimal SuffixStepValue2 = 0.5M;

    #endregion

    #region Example 6

    private double OneWayValue;
    private double TwoWayValue;
    private double TwoWayStepValue = 0.5;

    #endregion

    #region Example 7

    private double ArrowsEventBindedValue;
    private double ArrowsEventReturnedValue;
    private int OnIncrementCounter;
    private int OnDecrementCounter;

    private double OnChangeEventBindedValue;
    private double OnChangeEventReturnedValue;
    private int OnChangeCounter;

    private double EventsStepValue = 0.1;

    private void HandleOnIncrementEvent(BitNumericTextFieldChangeValue<double> onChangeValue)
    {
        ArrowsEventReturnedValue = onChangeValue.Value;

        OnIncrementCounter++;
    }

    private void HandleOnDecrementEvent(BitNumericTextFieldChangeValue<double> onChangeValue)
    {
        ArrowsEventReturnedValue = onChangeValue.Value;

        OnDecrementCounter++;
    }

    private void HandleOnChangeEvent(double value)
    {
        OnChangeEventReturnedValue = value;

        OnChangeCounter++;
    }

    #endregion

    #region Example 8

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

    #endregion

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string?",
            Description = "Detailed description of the input for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            Description = "The position in the parent set (if in a set).",
        },
        new ComponentParameter()
        {
            Name = "AriaSetSize",
            Type = "int?",
            Description = "The total size of the parent set (if in a set).",
        },
        new ComponentParameter()
        {
            Name = "AriaValueNow",
            Type = "TValue?",
            Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
        },
        new ComponentParameter()
        {
            Name = "AriaValueText",
            Type = "string?",
            Description = "Sets the control's aria-valuetext.",
        },
        new ComponentParameter()
        {
            Name = "Arrows",
            Type = "bool",
            Description = "Whether to show the up/down spinner arrows (buttons).",
        },
        new ComponentParameter()
        {
            Name = "ChangeHandler",
            Type = "EventCallback<BitNumericTextFieldAction>",
            Description = "",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "TValue?",
            Description = "Initial value of the numeric text field.",
        },
        new ComponentParameter()
        {
            Name = "DecrementButtonAriaLabel",
            Type = "string?",
            Description = "Accessible label text for the decrement button (for screen reader users).",
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
            Type = "BitIconName?",
            Description = "Icon name for an icon to display alongside the numeric text field's label.",
        },
        new ComponentParameter()
        {
            Name = "IconAriaLabel",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "IncrementButtonAriaLabel",
            Type = "string?",
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
            Name = "Label",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Descriptive label for the numeric text field, Label displayed above the numeric text field and read by screen readers.",
        },
        new ComponentParameter()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom Label for numeric text field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.",
        },
        new ComponentParameter()
        {
            Name = "LabelPosition",
            Type = "BitNumericTextFieldLabelPosition",
            DefaultValue = "BitNumericTextFieldLabelPosition.Top",
            Description = "The position of the label in regards to the numeric textfield.",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum"
        },
        new ComponentParameter()
        {
            Name = "Min",
            Type = "TValue?",
            Description = "Min value of the numeric text field. If not provided, the numeric text field has minimum value.",
        },
        new ComponentParameter()
        {
            Name = "Max",
            Type = "TValue?",
            Description = "Max value of the numeric text field. If not provided, the numeric text field has max value.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<TValue>",
            Description = "Callback for when the numeric text field value change.",
        },
        new ComponentParameter()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the control loses focus.",
        },
        new ComponentParameter()
        {
            Name = "OnDecrement",
            Type = "EventCallback<BitNumericTextFieldChangeValue<TValue>>",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
            LinkType = LinkType.Link,
            Href = "#Bit-NumericTextField-ChangeValue"
        },
        new ComponentParameter()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitNumericTextFieldChangeValue<TValue>>",
            Description = "Callback for when the increment button or up arrow key is pressed.",
            LinkType = LinkType.Link,
            Href = "#Bit-NumericTextField-ChangeValue"
        },
        new ComponentParameter()
        {
            Name = "Precision",
            Type = "int?",
            Description = "How many decimal places the value should be rounded to.",
        },
        new ComponentParameter()
        {
            Name = "Placeholder",
            Type = "string?",
            Description = "Input placeholder text.",
        },
        new ComponentParameter()
        {
            Name = "Step",
            Type = "TValue?",
            Description = "Difference between two adjacent values of the numeric text field.",
        },
        new ComponentParameter()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "A text is shown after the numeric text field value.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string?",
            Description = "A more descriptive title for the control, visible on its tooltip.",
        },
    };

    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "Bit-NumericTextField-ChangeValue",
            Title = "BitNumericTextFieldChangeValue",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "Value",
                   Type = "T?",
               },
               new ComponentParameter()
               {
                   Name = "MouseEventArgs",
                   Type = "MouseEventArgs?",
               },
               new ComponentParameter()
               {
                   Name = "KeyboardEventArgs",
                   Type = "KeyboardEventArgs?",
               },
            }
        }
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
        }
    };

    #region Example Code 1

    private readonly string example1HTMLCode = @"
<BitNumericTextField @bind-Value=""BasicValue""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Basic"" />

<BitNumericTextField @bind-Value=""DisabledValue""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Disabled""
                     IsEnabled=""false"" />

<BitNumericTextField @bind-Value=""LabelAndIconValue""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Label & Icon""
                     IconName=""BitIconName.Lightbulb"" />

<BitNumericTextField @bind-Value=""LabelLeftValue""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Label Left""
                     LabelPosition=""BitNumericTextFieldLabelPosition.Left"" />
";

    private readonly string example1CSharpCode = @"
private int BasicValue;
private int DisabledValue;
private int LabelAndIconValue;
private int LabelLeftValue;
private int BasicStepValue = 1;
";

    #endregion

    #region Example Code 2

    private readonly string example2HTMLCode = @"
<BitNumericTextField @bind-Value=""LabelTemplateValue"" Placeholder=""Enter a number..."" Step=""@BasicStepValue"">
    <LabelTemplate>
        <BitIcon IconName=""BitIconName.ChevronUpSmall"" Style=""color:green;"" />
        <BitIcon IconName=""BitIconName.ChevronDownSmall"" Style=""color:red;"" />
    </LabelTemplate>
</BitNumericTextField>
";

    private readonly string example2CSharpCode = @"
private int LabelTemplateValue;
private int BasicStepValue = 1;
";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"
<BitNumericTextField @bind-Value=""SpinArrowValue""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Increment & Decrement""
                     Arrows=""true"" />

<BitNumericTextField @bind-Value=""SpinArrowWithIconValue""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Increment & Decrement Icon""
                     Arrows=""true""
                     IncrementButtonIconName=""BitIconName.LikeSolid""
                     DecrementButtonIconName=""BitIconName.DislikeSolid"" />
";

    private readonly string example3CSharpCode = @"
private int SpinArrowValue;
private int SpinArrowWithIconValue;
private int BasicStepValue = 1;
";

    #endregion

    #region Example Code 4

    private readonly string example4HTMLCode = @"
<BitNumericTextField @bind-Value=""MinMaxValue1""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Min: -10, Max: 10, Step: 1""
                     Min=""-10""
                     Max=""10"" />

<BitNumericTextField @bind-Value=""MinMaxValue2""
                     Placeholder=""Enter a number...""
                     Step=""@MinMaxStepValue2""
                     Label=""Min: -20, Max: 20, Step: 2""
                     Min=""-20""
                     Max=""20"" />

<BitNumericTextField @bind-Value=""MinMaxValue3""
                     Placeholder=""Enter a number...""
                     Step=""@MinMaxStepValue3""
                     Label=""Min: -1, Max: 1, Step: 0.1""
                     Min=""-1""
                     Max=""1"" />
";

    private readonly string example4CSharpCode = @"
private int MinMaxValue1;
private int MinMaxValue2;
private decimal MinMaxValue3;
private int BasicStepValue = 1;
private int MinMaxStepValue2 = 2;
private decimal MinMaxStepValue3 = 0.1M;
";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"
<BitNumericTextField @bind-Value=""SuffixValue1""
                     Placeholder=""Enter a number...""
                     Step=""@BasicStepValue""
                     Label=""Height""
                     IconName=""BitIconName.AutoHeight""
                     DefaultValue=""150""
                     Suffix="" cm"" />

<BitNumericTextField @bind-Value=""SuffixValue2""
                     Placeholder=""Enter a number...""
                     Step=""@SuffixStepValue2""
                     Label=""Weight""
                     IconName=""BitIconName.Weights""
                     DefaultValue=""50""
                     Suffix="" kg"" />
";

    private readonly string example5CSharpCode = @"
private int SuffixValue1;
private decimal SuffixValue2;
private int BasicStepValue = 1;
private decimal SuffixStepValue2 = 0.5M;
";

    #endregion

    #region Example Code 6

    private readonly string example6HTMLCode = @"
<div>
    <BitNumericTextField Value=""OneWayValue""
                         Placeholder=""Enter a number...""
                         Step=""@BasicStepValue""
                         Label=""One-way"" />
    <BitRating @bind-Value=""OneWayValue"" />
</div>
<div>
    <BitNumericTextField @bind-Value=""TwoWayValue""
                         Placeholder=""Enter a number...""
                         Step=""TwoWayStepValue""
                         Label=""Two-way"" />
    <BitRating @bind-Value=""TwoWayValue"" />
</div>
";

    private readonly string example6CSharpCode = @"
private double OneWayValue;
private double TwoWayValue;
private int BasicStepValue = 1;
private double TwoWayStepValue = 0.5;
";

    #endregion

    #region Example Code 7

    private readonly string example7HTMLCode = @"
<div class=""column"">
    <BitNumericTextField @bind-Value=""ArrowsEventBindedValue""
                         Placeholder=""Enter a number...""
                         Step=""@EventsStepValue""
                         Label=""OnIncrement / OnDecrement""
                         OnIncrement=""(BitNumericTextFieldChangeValue<double> v) => HandleOnIncrementEvent(v)""
                         OnDecrement=""(BitNumericTextFieldChangeValue<double> v) => HandleOnDecrementEvent(v)"" />
    <span>OnIncrement Counter: @OnIncrementCounter</span>
    <span>OnDecrement Counter: @OnDecrementCounter</span>
    <span>Returned Value: @ArrowsEventReturnedValue</span>
</div>

<div class=""column"">
    <BitNumericTextField @bind-Value=""OnChangeEventBindedValue""
                         Placeholder=""Enter a number...""
                         Step=""@EventsStepValue""
                         Label=""OnChange""
                         OnChange=""(double v) => HandleOnChangeEvent(v)"" />
    <span>OnChange Counter: @OnChangeCounter</span>
    <span>Returned Value: @OnChangeEventReturnedValue</span>
</div>
";

    private readonly string example7CSharpCode = @"
private double ArrowsEventBindedValue;
private double ArrowsEventReturnedValue;
private int OnIncrementCounter;
private int OnDecrementCounter;

private double OnChangeEventBindedValue;
private double OnChangeEventReturnedValue;
private int OnChangeCounter;

private double EventsStepValue = 0.1;

private void HandleOnIncrementEvent(BitNumericTextFieldChangeValue<double> onChangeValue)
{
    ArrowsEventReturnedValue = onChangeValue.Value;

    OnIncrementCounter++;
}

private void HandleOnDecrementEvent(BitNumericTextFieldChangeValue<double> onChangeValue)
{
    ArrowsEventReturnedValue = onChangeValue.Value;

    OnDecrementCounter++;
}

private void HandleOnChangeEvent(double value)
{
    OnChangeEventReturnedValue = value;

    OnChangeCounter++;
}
";

    #endregion

    #region Example Code 8

    private readonly string example8HTMLCode = @"
<style>
    .validation-summary {
        border-left: rem(5px) solid $Red10;
        background-color: $ErrorBlockRed;
        overflow: hidden;
        margin-bottom: rem(10px);
    }

    .validation-message {
        color: $Red20;
        font-size: rem(12px);
    }

    .validation-errors {
        margin: rem(5px);
    }
</style>

@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <BitNumericTextField @bind-Value=""@ValidationModel.AgeInYears""
                             Placeholder=""Enter a number...""
                             Step=""@BasicStepValue""
                             Label=""Age"" />
        <ValidationMessage For=""@(() => ValidationModel.AgeInYears)"" />

        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
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
public class BitNumericTextFieldValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 150, ErrorMessage = ""Nobody is that old"")]
    public double AgeInYears { get; set; }
}

private BitNumericTextFieldValidationModel ValidationModel = new();
private string SuccessMessage = string.Empty;
private int BasicStepValue = 1;

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
