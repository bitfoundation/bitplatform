namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumericTextField;

public partial class BitNumericTextFieldDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the input for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            DefaultValue = "null",
            Description = "The position in the parent set (if in a set).",
        },
        new()
        {
            Name = "AriaSetSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The total size of the parent set (if in a set).",
        },
        new()
        {
            Name = "AriaValueNow",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
        },
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            DefaultValue = "null",
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
            Name = "Classes",
            Type = "BitNumericTextFieldClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#numerictextfield-class-styles",
            Description = "Custom CSS classes for different parts of the BitNumericTextField.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Initial value of the numeric text field.",
        },
        new()
        {
            Name = "DecrementAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label text for the decrement button (for screen reader users).",
        },
        new()
        {
            Name = "DecrementIconName",
            Type = "string",
            DefaultValue = "ChevronDownSmall",
            Description = "Custom icon name for the decrement button.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
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
            DefaultValue = "null",
            Description = "Accessible label text for the increment button (for screen reader users).",
        },
        new()
        {
            Name = "IncrementIconName",
            Type = "string",
            DefaultValue = "ChevronUpSmall",
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
            DefaultValue = "null",
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
            DefaultValue = "null",
            Description = "Min value of the numeric text field. If not provided, the numeric text field has minimum value.",
        },
        new()
        {
            Name = "Max",
            Type = "TValue?",
            DefaultValue = "null",
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
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves out of the input.",
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
            Href = "#numerictextfield-change-value"
        },
        new()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitNumericTextFieldChangeValue<TValue>>",
            Description = "Callback for when the increment button or up arrow key is pressed.",
            LinkType = LinkType.Link,
            Href = "#numerictextfield-change-value"
        },
        new()
        {
            Name = "Precision",
            Type = "int?",
            DefaultValue = "null",
            Description = "How many decimal places the value should be rounded to.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Input placeholder text.",
        },
        new()
        {
            Name = "Step",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Difference between two adjacent values of the numeric text field.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitNumericTextFieldClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#numerictextfield-class-styles",
            Description = "Custom CSS styles for different parts of the BitNumericTextField.",
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
            DefaultValue = "false",
            Description = "Whether to show the up/down spinner arrows (buttons).",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "A more descriptive title for the control, visible on its tooltip.",
        },
        new()
        {
            Name = "ValidationMessage",
            Type = "string",
            DefaultValue="The {0} field is not valid.",
            Description = "The message format used for invalid values entered in the input.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "numerictextfield-change-value",
            Title = "BitNumericTextFieldChangeValue",
            Parameters = new()
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
               }
            }
        },
        new()
        {
            Id = "numerictextfield-class-styles",
            Title = "BitNumericTextFieldClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "ArrowContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow container."
                },
                new()
                {
                    Name = "ArrowContainerFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow container focus state."
                },
                new()
                {
                    Name = "ArrowDownButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow down button."
                },
                new()
                {
                    Name = "ArrowDownButtonFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow down button focus state."
                },
                new()
                {
                    Name = "ArrowDownIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow down icon."
                },
                new()
                {
                    Name = "ArrowDownIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow down icon container."
                },
                new()
                {
                    Name = "ArrowDownIconFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow down icon focus state."
                },
                new()
                {
                    Name = "ArrowUpButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow up button."
                },
                new()
                {
                    Name = "ArrowUpButtonFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow up button focus state."
                },
                new()
                {
                    Name = "ArrowUpIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow up icon."
                },
                new()
                {
                    Name = "ArrowUpIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow up icon container."
                },
                new()
                {
                    Name = "ArrowUpIconFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's arrow up icon focus state."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's label."
                },
                new()
                {
                    Name = "LabelContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's label container."
                },
                new()
                {
                    Name = "LabelFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's label focus state."
                },
                new()
                {
                    Name = "Focus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's focus state."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's icon."
                },
                new()
                {
                    Name = "IconFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's icon focus state."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's input."
                },
                new()
                {
                    Name = "InputFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the numeric text field's input focus state."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of label and input in the numeric text field."
                },
                new()
                {
                    Name = "InputWrapperFocus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of label and input in the numeric text field focus state."
                }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "labelPosition-enum",
            Name = "BitNumericTextFieldLabelPosition",
            Description = "",
            Items = new()
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



    private readonly string example1HtmlCode = @"
<BitNumericTextField @bind-Value=""BasicValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Basic"" />

<BitNumericTextField @bind-Value=""DisabledValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Disabled""
                     IsEnabled=""false"" />";
    private readonly string example1CsharpCode = @"
private int BasicValue;
private int DisabledValue;";

    private readonly string example2HtmlCode = @"
<BitNumericTextField @bind-Value=""LabelTopValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Label Top""
                     LabelPosition=""BitNumericTextFieldLabelPosition.Top""/>

<BitNumericTextField @bind-Value=""LabelLeftValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Label Left""
                     LabelPosition=""BitNumericTextFieldLabelPosition.Left"" />";
    private readonly string example2CsharpCode = @"
private int LabelTopValue;
private int LabelLeftValue;";

    private readonly string example3HtmlCode = @"
<BitNumericTextField @bind-Value=""LabelTemplateValue"" Placeholder=""Enter a number..."" Step=""@(1)"">
    <LabelTemplate>
        <BitLabel Style=""color: green;"">This is custom Label</BitLabel>
        <BitIcon IconName=""@BitIconName.Filter"" />
    </LabelTemplate>
</BitNumericTextField>";
    private readonly string example3CsharpCode = @"
private int LabelTemplateValue;";

    private readonly string example4HtmlCode = @"
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
                     IncrementIconName=""@BitIconName.LikeSolid""
                     DecrementIconName=""@BitIconName.DislikeSolid"" />

<BitNumericTextField @bind-Value=""LabelAndIconValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Label & Icon""
                     IconName=""@BitIconName.Lightbulb"" />";
    private readonly string example4CsharpCode = @"
private int SpinArrowValue;
private int LabelAndIconValue;
private int SpinArrowWithIconValue;";

    private readonly string example5HtmlCode = @"
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
                     Max=""1"" />";
    private readonly string example5CsharpCode = @"
private int MinMaxValue1;
private int MinMaxValue2;
private decimal MinMaxValue3;";

    private readonly string example6HtmlCode = @"
<BitNumericTextField @bind-Value=""SuffixValue1""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""Height""
                     IconName=""@BitIconName.AutoHeight""
                     DefaultValue=""150""
                     Suffix="" cm"" />

<BitNumericTextField @bind-Value=""SuffixValue2""
                     Placeholder=""Enter a number...""
                     Step=""@(0.5M)""
                     Label=""Weight""
                     IconName=""@BitIconName.Weights""
                     DefaultValue=""50""
                     Suffix="" kg"" />";
    private readonly string example6CsharpCode = @"
private int SuffixValue1;
private decimal SuffixValue2;";

    private readonly string example7HtmlCode = @"
<BitNumericTextField Value=""OneWayValue""
                     Placeholder=""Enter a number...""
                     Step=""@(1)""
                     Label=""One-way"" />
<BitRating @bind-Value=""OneWayValue"" />
    
<BitNumericTextField @bind-Value=""TwoWayValue""
                     Placeholder=""Enter a number...""
                     Step=""@(0.5)""
                     Label=""Two-way"" />
<BitRating @bind-Value=""TwoWayValue"" />";
    private readonly string example7CsharpCode = @"
private double OneWayValue;
private double TwoWayValue;
";

    private readonly string example8HtmlCode = @"
<BitNumericTextField @bind-Value=""ArrowsEventBindedValue""
                     Placeholder=""Enter a number...""
                     Step=""@(0.1)""
                     Label=""OnIncrement / OnDecrement""
                     ShowArrows=""true""
                     OnIncrement=""(BitNumericTextFieldChangeValue<double> v) => HandleOnIncrementEvent(v)""
                     OnDecrement=""(BitNumericTextFieldChangeValue<double> v) => HandleOnDecrementEvent(v)"" />
<BitLabel>OnIncrement Counter: @OnIncrementCounter</BitLabel>
<BitLabel>OnDecrement Counter: @OnDecrementCounter</BitLabel>
<BitLabel>Returned Value: @ArrowsEventReturnedValue</BitLabel>

<BitNumericTextField @bind-Value=""OnChangeEventBindedValue""
                     Placeholder=""Enter a number...""
                     Step=""@(0.1)""
                     Label=""OnChange""
                     OnChange=""(double v) => HandleOnChangeEvent(v)"" />
<BitLabel>OnChange Counter: @OnChangeCounter</BitLabel>
<BitLabel>Returned Value: @OnChangeEventReturnedValue</BitLabel>";
    private readonly string example8CsharpCode = @"
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
}";

    private readonly string example9HtmlCode = @"
<style>
    .custom-class {
        margin-left: 0.5rem;
        border: 1px solid red;
        box-shadow: aqua 0 0 1rem;
    }

    .custom-input {
        color: darkgreen;
        font-weight: 900;
        font-size: 1rem;
        padding: 1rem;
        height: 3rem;
    }

    .custom-input-wrapper {
        height: auto;
        border-radius: 1rem;
        background-color: tomato;
    }

    .input-wrapper-focus::after {
        border-radius: 1rem;
    }

    .custom-label-focus {
        color: chartreuse;
    }
</style>

<BitNumericTextField @bind-Value=""@StyleValue""
                     Placeholder=""Enter a text...""
                     Style=""background-color: lightskyblue; border-radius: 1rem; padding: 0.5rem"" />
<BitNumericTextField @bind-Value=""@ClassValue""
                     Placeholder=""Enter a text...""
                     Class=""custom-class"" />

<BitNumericTextField @bind-Value=""@StylesValue""
                     Placeholder=""Enter a text...""
                     Label=""Custom label style""
                     IconName=""@BitIconName.Microphone""
                     Styles=""@(new() { Icon = ""color: darkorange;"",
                                       Label = ""color: blue; font-weight: 900; font-size: 1.25rem;"",
                                       Input = ""padding: 0.5rem; background-color: goldenrod""} )"" />
<BitNumericTextField @bind-Value=""@ClassesValue""
                     Placeholder=""Enter a text...""
                     Label=""Custom label class""
                     Classes=""@(new() { InputWrapper = ""custom-input-wrapper"",
                                        InputWrapperFocus = ""input-wrapper-focus"",
                                        Input = ""custom-input"",
                                        LabelFocus = ""custom-label-focus"" } )"" />";
    private readonly string example9CsharpCode = @"
private int StyleValue;
private int ClassValue;
private int StylesValue;
private int ClassesValue;";

    private readonly string example10HtmlCode = @"
Visible: [ <BitNumericTextField @bind-Value=""@VisibilityValue"" Visibility=""BitVisibility.Visible"" Placeholder=""Visible NumericTextField"" /> ]
Hidden: [ <BitNumericTextField @bind-Value=""@VisibilityValue"" Visibility=""BitVisibility.Hidden"" Placeholder=""Hidden NumericTextField"" />  ]
Collapsed: [ <BitNumericTextField @bind-Value=""@VisibilityValue"" Visibility=""BitVisibility.Collapsed"" Placeholder=""Collapsed NumericTextField"" />  ]";
    private readonly string example10CsharpCode = @"
private int VisibilityValue;";

    private readonly string example11HtmlCode = @"
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
}";
    private readonly string example11CsharpCode = @"
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
}";
}
