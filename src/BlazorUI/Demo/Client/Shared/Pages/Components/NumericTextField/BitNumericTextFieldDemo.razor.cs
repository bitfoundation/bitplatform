﻿using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.NumericTextField;

public partial class BitNumericTextFieldDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            Description = "Detailed description of the input for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            Description = "The position in the parent set (if in a set).",
        },
        new()
        {
            Name = "AriaSetSize",
            Type = "int?",
            Description = "The total size of the parent set (if in a set).",
        },
        new()
        {
            Name = "AriaValueNow",
            Type = "TValue?",
            Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
        },
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            Description = "Sets the control's aria-valuetext.",
        },
        new()
        {
            Name = "ChangeHandler",
            Type = "EventCallback<BitNumericTextFieldAction>",
            Description = "",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "TValue?",
            Description = "Initial value of the numeric text field.",
        },
        new()
        {
            Name = "DecrementAriaLabel",
            Type = "string?",
            Description = "Accessible label text for the decrement button (for screen reader users).",
        },
        new()
        {
            Name = "DecrementIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronDownSmall",
            Description = "Custom icon name for the decrement button.",
        },
        new()
        {
            Name = "IconName",
            Type = "BitIconName?",
            Description = "Icon name for an icon to display alongside the numeric text field's label.",
        },
        new()
        {
            Name = "IconAriaLabel",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new()
        {
            Name = "IncrementAriaLabel",
            Type = "string?",
            Description = "Accessible label text for the increment button (for screen reader users).",
        },
        new()
        {
            Name = "IncrementIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronUpSmall",
            Description = "Custom icon name for the increment button.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Descriptive label for the numeric text field, Label displayed above the numeric text field and read by screen readers.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom Label for numeric text field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitNumericTextFieldLabelPosition",
            DefaultValue = "BitNumericTextFieldLabelPosition.Top",
            Description = "The position of the label in regards to the numeric textfield.",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum"
        },
        new()
        {
            Name = "Min",
            Type = "TValue?",
            Description = "Min value of the numeric text field. If not provided, the numeric text field has minimum value.",
        },
        new()
        {
            Name = "Max",
            Type = "TValue?",
            Description = "Max value of the numeric text field. If not provided, the numeric text field has max value.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<TValue>",
            Description = "Callback for when the numeric text field value change.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the control loses focus.",
        },
        new()
        {
            Name = "OnDecrement",
            Type = "EventCallback<BitNumericTextFieldChangeValue<TValue>>",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
            LinkType = LinkType.Link,
            Href = "#Bit-NumericTextField-ChangeValue"
        },
        new()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitNumericTextFieldChangeValue<TValue>>",
            Description = "Callback for when the increment button or up arrow key is pressed.",
            LinkType = LinkType.Link,
            Href = "#Bit-NumericTextField-ChangeValue"
        },
        new()
        {
            Name = "Precision",
            Type = "int?",
            Description = "How many decimal places the value should be rounded to.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            Description = "Input placeholder text.",
        },
        new()
        {
            Name = "Step",
            Type = "TValue?",
            Description = "Difference between two adjacent values of the numeric text field.",
        },
        new()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "A text is shown after the numeric text field value.",
        },
        new()
        {
            Name = "ShowArrows",
            Type = "bool",
            Description = "Whether to show the up/down spinner arrows (buttons).",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            Description = "A more descriptive title for the control, visible on its tooltip.",
        },
        new()
        {
            Name = "ValidationMessage",
            Type = "string",
            DefaultValue="The {0} field is not valid.",
            Description = "The message format used for invalid values entered in the input.",
        },
    };

    private readonly List<ComponentSubClass> componentSubParameters = new()
    {
        new()
        {
            Id = "Bit-NumericTextField-ChangeValue",
            Title = "BitNumericTextFieldChangeValue",
            Parameters = new List<ComponentParameter>()
            {
               new()
               {
                   Name = "Value",
                   Type = "T?",
               },
               new()
               {
                   Name = "MouseEventArgs",
                   Type = "MouseEventArgs?",
               },
               new()
               {
                   Name = "KeyboardEventArgs",
                   Type = "KeyboardEventArgs?",
               },
            }
        }
    };

    private readonly List<ComponentSubEnum> enumParameters = new()
    {
        new()
        {
            Id = "labelPosition-enum",
            Name = "BitNumericTextFieldLabelPosition",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the numeric textfield.",
                    Value="0",
                },
                new()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the numeric textfield.",
                    Value="1",
                }
            }
        }
    };



    private int BasicValue;
    private int DisabledValue;
    private int LabelAndIconValue;
    private int LabelLeftValue;

    private int LabelTemplateValue;

    private int SpinArrowValue;
    private int SpinArrowWithIconValue;

    private int MinMaxValue1;
    private int MinMaxValue2;
    private decimal MinMaxValue3;

    private int SuffixValue1;
    private decimal SuffixValue2;

    private double OneWayValue;
    private double TwoWayValue;

    private double ArrowsEventBindedValue;
    private double ArrowsEventReturnedValue;
    private int OnIncrementCounter;
    private int OnDecrementCounter;

    private double OnChangeEventBindedValue;
    private double OnChangeEventReturnedValue;
    private int OnChangeCounter;

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


    private readonly string example1HTMLCode = @"
<BitNumericTextField @bind-Value=""BasicValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Basic"" />

<BitNumericTextField @bind-Value=""DisabledValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Disabled""
                     IsEnabled=""false"" />

<BitNumericTextField @bind-Value=""LabelAndIconValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Label & Icon""
                     IconName=""BitIconName.Lightbulb"" />

<BitNumericTextField @bind-Value=""LabelLeftValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Label Left""
                     LabelPosition=""BitNumericTextFieldLabelPosition.Left"" />
";
    private readonly string example1CSharpCode = @"
private int BasicValue;
private int DisabledValue;
private int LabelAndIconValue;
private int LabelLeftValue;
";

    private readonly string example2HTMLCode = @"
<BitNumericTextField @bind-Value=""LabelTemplateValue"" Placeholder=""Enter a number..."" Step=""@(1)"">
    <LabelTemplate>
        <label style=""color: green;"">This is custom Label</label>
        <BitIcon IconName=""BitIconName.Filter"" />
    </LabelTemplate>
</BitNumericTextField>
";
    private readonly string example2CSharpCode = @"
private int LabelTemplateValue;
";

    private readonly string example3HTMLCode = @"
<BitNumericTextField @bind-Value=""SpinArrowValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Increment & Decrement""
                     ShowArrows=""true"" />

<BitNumericTextField @bind-Value=""SpinArrowWithIconValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Increment & Decrement Icon""
                     ShowArrows=""true""
                     IncrementIconName=""BitIconName.LikeSolid""
                     DecrementIconName=""BitIconName.DislikeSolid"" />
";
    private readonly string example3CSharpCode = @"
private int SpinArrowValue;
private int SpinArrowWithIconValue;
";

    private readonly string example4HTMLCode = @"
<BitNumericTextField @bind-Value=""MinMaxValue1""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Min: -10, Max: 10, Step: 1""
                     Min=""-10""
                     Max=""10"" />

<BitNumericTextField @bind-Value=""MinMaxValue2""
                     Placeholder=""Enter a number...""
                     Step=""@(2)""
                     Label=""Min: -20, Max: 20, Step: 2""
                     Min=""-20""
                     Max=""20"" />

<BitNumericTextField @bind-Value=""MinMaxValue3""
                     Placeholder=""Enter a number...""
                     Step=""@(0.1M)""
                     Label=""Min: -1, Max: 1, Step: 0.1""
                     Min=""-1""
                     Max=""1"" />
";
    private readonly string example4CSharpCode = @"
private int MinMaxValue1;
private int MinMaxValue2;
private decimal MinMaxValue3;
";

    private readonly string example5HTMLCode = @"
<BitNumericTextField @bind-Value=""SuffixValue1""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Height""
                     IconName=""BitIconName.AutoHeight""
                     DefaultValue=""150""
                     Suffix="" cm"" />

<BitNumericTextField @bind-Value=""SuffixValue2""
                     Placeholder=""Enter a number...""
                     Step=""@(0.5M)""
                     Label=""Weight""
                     IconName=""BitIconName.Weights""
                     DefaultValue=""50""
                     Suffix="" kg"" />
";
    private readonly string example5CSharpCode = @"
private int SuffixValue1;
private decimal SuffixValue2;
";

    private readonly string example6HTMLCode = @"
<div>
    <BitNumericTextField Value=""OneWayValue""
                         Placeholder=""Enter a number...""
                         Step=""@(1)""
                         Label=""One-way"" />
    <BitRating @bind-Value=""OneWayValue"" />
</div>
<div>
    <BitNumericTextField @bind-Value=""TwoWayValue""
                         Placeholder=""Enter a number...""
                         Step=""@(0.5)""
                         Label=""Two-way"" />
    <BitRating @bind-Value=""TwoWayValue"" />
</div>
";
    private readonly string example6CSharpCode = @"
private double OneWayValue;
private double TwoWayValue;
";

    private readonly string example7HTMLCode = @"
<div class=""column"">
    <BitNumericTextField @bind-Value=""ArrowsEventBindedValue""
                         Placeholder=""Enter a number...""
                         Step=""@(0.1)""
                         Label=""OnIncrement / OnDecrement""
                         ShowArrows=""true""
                         OnIncrement=""(BitNumericTextFieldChangeValue<double> v) => HandleOnIncrementEvent(v)""
                         OnDecrement=""(BitNumericTextFieldChangeValue<double> v) => HandleOnDecrementEvent(v)"" />
    <span>OnIncrement Counter: @OnIncrementCounter</span>
    <span>OnDecrement Counter: @OnDecrementCounter</span>
    <span>Returned Value: @ArrowsEventReturnedValue</span>
</div>

<div class=""column"">
    <BitNumericTextField @bind-Value=""OnChangeEventBindedValue""
                         Placeholder=""Enter a number...""
                         Step=""@(0.1)""
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
                             Step=""@(1)""
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
