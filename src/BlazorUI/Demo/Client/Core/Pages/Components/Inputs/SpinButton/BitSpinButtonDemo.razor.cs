namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SpinButton;

public partial class BitSpinButtonDemo
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
            Type = "double?",
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
            Name = "Classes",
            Type = "BitSpinButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitSpinButton.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ChangeHandler",
            Type = "EventCallback<BitSpinButtonAction>",
            Description = "?",
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
            Name = "DecrementTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse is placed on the decrement button.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "Initial value of the spin button.",
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
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Icon name for an icon to display alongside the spin button's label.",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom icon name for the increment button.",
        },
        new()
        {
            Name = "IncrementTitle",
            Type = "string",
            DefaultValue = "ChevronUpSmall",
            Description = "The title to show when the mouse is placed on the increment button.",
        },
        new()
        {
            Name = "IsInputReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the input is readonly.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Descriptive label for the spin button, Label displayed above the spin button and read by screen readers.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitSpinButtonLabelPosition",
            DefaultValue = "BitSpinButtonLabelPosition.Top",
            Description = "The position of the label in regards to the spin button.",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom Label content for spin button.",
        },
        new()
        {
            Name = "Max",
            Type = "double?",
            DefaultValue = "null",
            Description = "Max value of the spin button. If not provided, the spin button has max value of double type.",
        },
        new()
        {
            Name = "Min",
            Type = "double?",
            DefaultValue = "null",
            Description = "Min value of the spin button. If not provided, the spin button has minimum value of double type.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitSpinButtonMode",
            DefaultValue = "BitSpinButtonMode.Compact",
            Description = "Determines how the spinning buttons should be rendered.",
            LinkType = LinkType.Link,
            Href = "#spinMode-enum",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the control loses focus.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<double>",
            Description = "Callback for when the spin button value change.",
        },
        new()
        {
            Name = "OnDecrement",
            Type = "EventCallback<BitSpinButtonChangeEventArgs>",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitSpinButtonChangeEventArgs>",
            Description = "Callback for when the increment button or up arrow key is pressed.",
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
            Name = "ShowInput",
            Type = "bool",
            DefaultValue = "true",
            Description = "If false, the input is hidden.",
        },
        new()
        {
            Name = "Step",
            Type = "double",
            DefaultValue = "1",
            Description = "Difference between two adjacent values of the spin button.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSpinButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitSpinButton.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "A text is shown after the spin button value.",
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
            DefaultValue = "\"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.\"",
            Description = "The message format used for invalid values entered in the input.",
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitSpinButtonClassStyles",
            Description = "",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the spin button.",
                },
                new()
                {
                    Name = "LabelContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's label container.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's icon.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's label.",
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's container.",
                },
                new()
                {
                    Name = "DecrementButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's decrement button container.",
                },
                new()
                {
                    Name = "DecrementIconWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's decrement icon wrapper.",
                },
                new()
                {
                    Name = "DecrementIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's decrement icon.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's input.",
                },
                new()
                {
                    Name = "CompactButtonsWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's buttons wrapper in Compact mode.",
                },
                new()
                {
                    Name = "IncrementButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's increment icon.",
                },
                new()
                {
                    Name = "IncrementIconWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's increment icon.",
                },
                new()
                {
                    Name = "IncrementIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spin button's increment icon.",
                }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "labelPosition-enum",
            Name = "BitSpinButtonLabelPosition",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spin button.",
                    Value="0",
                },
                new()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spin button.",
                    Value="1",
                },
                new()
                {
                    Name= "Right",
                    Description="The label shows on the right side of the spin button.",
                    Value="2",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the spin button.",
                    Value="3",
                }
            }
        },
        new()
        {
            Id = "spinMode-enum",
            Name = "BitSpinButtonMode",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Compact",
                    Description="Spinning buttons render as a compact stack at the end of the input.",
                    Value="0",
                },
                new()
                {
                    Name= "Inline",
                    Description="Spinning buttons render inlined at the end of the input.",
                    Value="0",
                },
                new()
                {
                    Name= "Spread",
                    Description="Spinning buttons render at the start and end of the input.",
                    Value="1",
                }
            }
        },
    };



    private readonly string example1RazorCode = @"
<BitSpinButton Label=""Basic"" />

<BitSpinButton Label=""Disabled"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitSpinButton Label=""Compact"" Mode=""BitSpinButtonMode.Compact"" />

<BitSpinButton Label=""Inline"" Mode=""BitSpinButtonMode.Inline"" />

<BitSpinButton Label=""Spread"" Mode=""BitSpinButtonMode.Spread"" />";

    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        background: darkblue;
        border-radius: 0.5rem;
    }

    .custom-input {
        color: darkred;
        font-size: 18px;
    }

    .custom-wrapper {
        background-color: darkcyan;
    }
</style>

<BitSpinButton Label=""Styled"" Style=""background:darkred;padding:1rem"" />

<BitSpinButton Label=""Classed"" Class=""custom-class"" />



<BitSpinButton Label=""Styles"" Styles=""@(new() { LabelContainer = ""background:darkred;padding:1.5rem"",
                                                Label = ""font-size:22px;color:chartreuse"",
                                                DecrementButton = ""background:blue"",
                                                IncrementButton = ""background:green"",})"" />

<BitSpinButton Label=""Classes"" Classes=""@(new() { Input = ""custom-input"",
                                                  CompactButtonsWrapper = ""custom-wrapper"" })"" />";

    private readonly string example4RazorCode = @"
<BitSpinButton Label=""Top Label & Icon"" IconName=""@BitIconName.Lightbulb"" />
<BitSpinButton Label=""Left Label"" LabelPosition=""BitSpinButtonLabelPosition.Left"" />
<BitSpinButton Label=""Right Label"" LabelPosition=""BitSpinButtonLabelPosition.Right"" />
<BitSpinButton Label=""Bottom Label"" LabelPosition=""BitSpinButtonLabelPosition.Bottom"" />";

    private readonly string example5RazorCode = @"
<BitSpinButton>
    <LabelTemplate>
        <div style=""display:flex; align-items: center; gap: 10px"">
            <BitLabel Style=""color: green;"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitSpinButton>";

    private readonly string example6RazorCode = @"
<BitSpinButton Label=""Like and Dislike""
               IncrementIconName=""@BitIconName.LikeSolid""
               DecrementIconName=""@BitIconName.DislikeSolid"" />

<BitSpinButton Label=""Back & Forward""
               Mode=""BitSpinButtonMode.Inline""
               DecrementIconName=""@BitIconName.Back""
               IncrementIconName=""@BitIconName.Forward"" />

<BitSpinButton Label=""Plus and Minus""
               Mode=""BitSpinButtonMode.Spread""
               IncrementIconName=""@BitIconName.CalculatorAddition""
               DecrementIconName=""@BitIconName.CalculatorSubtract"" />";

    private readonly string example7RazorCode = @"
<BitSpinButton Label=""Min: -10, Max: 10""
               Min=""-10""
               Max=""10"" />

<BitSpinButton Label=""Min: -20, Max: 20, Step: 2""
               Min=""-20""
               Max=""20""
               Step=""2"" />

<BitSpinButton Label=""Min: -1, Max: 1, Step: 0.1""
               Min=""-1""
               Max=""1""
               Step=""0.1"" />";

    private readonly string example8RazorCode = @"
<BitSpinButton Label=""Height""
               IconName=""@BitIconName.AutoHeight""
               DefaultValue=""150""
               Suffix="" cm"" />

<BitSpinButton Label=""Weight""
               IconName=""@BitIconName.Weights""
               DefaultValue=""50""
               Step=""0.5""
               Suffix="" kg"" />";

    private readonly string example9RazorCode = @"
<BitSpinButton Label=""One-way"" Value=""OneWayValue"" />
<BitRating @bind-Value=""OneWayValue"" />

<BitSpinButton Label=""Two-way"" Step=""0.5"" @bind-Value=""TwoWayValue"" />
<BitRating @bind-Value=""TwoWayValue"" />";
    private readonly string example9CsharpCode = @"
private double OneWayValue = 3;
private double TwoWayValue = 5;";

    private readonly string example10RazorCode = @"
<BitSpinButton @bind-Value=""IncrementEventValue""
               Label=""OnIncrement / OnDecrement""
               Step=""0.1""
               OnIncrement=""() => OnIncrementCounter++""
               OnDecrement=""() => OnDecrementCounter++"" />
<BitLabel>OnIncrement Counter: @OnIncrementCounter</BitLabel>
<BitLabel>OnDecrement Counter: @OnDecrementCounter</BitLabel>

<BitSpinButton @bind-Value=""OnChangeEventBindedValue""
               Label=""OnChange""
               Step=""0.1"" 
               OnChange=""HandleOnChangeEvent""/>
<BitLabel>OnChange Clicked Counter: @OnChangeClickedCounter</BitLabel>
<BitLabel>OnChange Returned Value: @OnChangeEventReturnedValue</BitLabel>";
    private readonly string example10CsharpCode = @"
private double IncrementEventValue;
private int OnIncrementCounter;
private int OnDecrementCounter;

private double OnChangeEventBindedValue;
private double OnChangeEventReturnedValue;
private int OnChangeClickedCounter;
private void HandleOnChangeEvent(double value)
{
    OnChangeEventReturnedValue = value;

    OnChangeClickedCounter++;
}";

    private readonly string example11RazorCode = @"
<BitSpinButton ShowInput=""false""
               IncrementTitle=""Add""
               DecrementTitle=""Remove""
               @bind-Value=""showInputValue""
               Mode=""BitSpinButtonMode.Inline""
               Label=""@showInputValue.ToString()""
               IncrementIconName=""@BitIconName.Add""
               DecrementIconName=""@BitIconName.Remove""
               LabelPosition=""@BitSpinButtonLabelPosition.Right"" />";
    private readonly string example11CsharpCode = @"
private double showInputValue;";

    private readonly string example12RazorCode = @"
<BitSpinButton IsInputReadOnly=""true"" />";

    private readonly string example13RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">

    <DataAnnotationsValidator />

    <BitSpinButton Label=""Age"" @bind-Value=""@ValidationModel.AgeInYears""></BitSpinButton>
    <ValidationMessage For=""@(() => ValidationModel.AgeInYears)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example13CsharpCode = @"
public class BitSpinButtonValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 200, ErrorMessage = ""Nobody is that old"")]
    public double AgeInYears { get; set; }
}

private BitSpinButtonValidationModel ValidationModel = new();

private async Task HandleValidSubmit() { }

private void HandleInvalidSubmit() { }";
}
