namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumberField;

public partial class BitNumberFieldDemo
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
            Name = "Classes",
            Type = "BitNumberFieldClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#numberfield-class-styles",
            Description = "Custom CSS classes for different parts of the BitNumberField.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Initial value of the number field.",
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
            Description = "Icon name for an icon to display alongside the number field's label.",
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
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the associated input is required or not, add an asterisk \"*\" to its label.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Descriptive label for the number field, Label displayed above the number field and read by screen readers.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom Label for number field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.",
        },
        new()
        {
            Name = "LeftLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "The position of the label in regards to the number field.",
        },
        new()
        {
            Name = "Min",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Min value of the number field. If not provided, the number field has minimum value.",
        },
        new()
        {
            Name = "Max",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Max value of the number field. If not provided, the number field has max value.",
        },
        new()
        {
            Name = "NumberFormat",
            Type = "string",
            DefaultValue = "{0}",
            Description = "The format of the number in the number field.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<TValue>",
            Description = "Callback for when the number field value change.",
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
            Type = "EventCallback<TValue>",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
        },
        new()
        {
            Name = "OnIncrement",
            Type = "EventCallback<TValue>",
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
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Input placeholder text.",
        },
        new()
        {
            Name = "Prefix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Prefix displayed before the numeric field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom prefix for numeric field.",
        },
        new()
        {
            Name = "ShowButtons",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the increment and decrement buttons.",
        },
        new()
        {
            Name = "Step",
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Difference between two adjacent values of the number field.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitNumberFieldClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#numberfield-class-styles",
            Description = "Custom CSS styles for different parts of the BitNumberField.",
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix displayed after the numeric field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom suffix for numeric field.",
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
            Id = "numberfield-class-styles",
            Title = "BitNumberFieldClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "ButtonsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's buttons (increment and decrement) container."
                },
                new()
                {
                    Name = "DecrementButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's decrement button."
                },
                new()
                {
                    Name = "DecrementIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's decrement icon."
                },
                new()
                {
                    Name = "DecrementIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's decrement icon container."
                },
                new()
                {
                    Name = "IncrementButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's increment button."
                },
                new()
                {
                    Name = "IncrementIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's increment icon."
                },
                new()
                {
                    Name = "IncrementIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's increment icon container."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's label."
                },
                new()
                {
                    Name = "LabelContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's label container."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's focus state."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's icon."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's input."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of label and input in the number field."
                },
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's root element."
                }
            }
        }
    };


    private double oneWayValue;
    private double twoWayValue;

    private int onIncrementCounter;
    private int onDecrementCounter;
    private int onChangeCounter;

    private string SuccessMessage = string.Empty;
    private BitNumberFieldValidationModel validationModel = new();

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



    private readonly string example1RazorCode = @"
<BitNumberField Label=""Basic"" TValue=""int"" />
<BitNumberField Label=""Disabled"" Step=""1"" IsEnabled=""false"" />
<BitNumberField Label=""Placeholder"" DefaultValue=""1"" Placeholder=""Enter a number..."" />
<BitNumberField Label=""Step & Buttons"" Step=""1"" ShowButtons=""true"" />
<BitNumberField Label=""Min & Max"" Step=""1"" Min=""-10"" Max=""10"" />
<BitNumberField Label=""Required"" TValue=""int"" Required />";

    private readonly string example2RazorCode = @"
<BitNumberField Label=""Label Top"" Step=""1"" />
<BitNumberField Label=""Label Left"" Step=""1"" LeftLabel />

<BitNumberField TValue=""int"">
    <LabelTemplate>
        <div style=""display:flex;align-items:center;gap:10px"">
            <BitLabel Style=""color:green"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" Style=""font-size:18px;""/>
        </div>
    </LabelTemplate>
</BitNumberField>";

    private readonly string example3RazorCode = @"
<BitNumberField Label=""Label & Icon""  Step=""1"" IconName=""@BitIconName.Lightbulb"" />

<BitNumberField Label=""Increment & Decrement Icon""  Step=""1""
                     ShowButtons=""true""
                     IncrementIconName=""@BitIconName.LikeSolid""
                     DecrementIconName=""@BitIconName.DislikeSolid"" />";

    private readonly string example4RazorCode = @"
<BitNumberField Label=""Height"" DefaultValue=""150"" NumberFormat=""{0} cm"" />
<BitNumberField Label=""Weight"" DefaultValue=""2500"" NumberFormat=""{0,0:N0} kg"" />";

    private readonly string example5RazorCode = @"
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

    .custom-focus .custom-label {
        color: chartreuse;
    }

    .custom-input-wrapper {
        height: auto;
        border-radius: 1rem;
        background-color: tomato;
    }

    .custom-focus .custom-input-wrapper::after {
        border-radius: 1rem;
        border-width: 0.25rem;
        border-color: rebeccapurple;
    }
</style>

<BitNumberField Label=""Styled"" DefaultValue=""10"" Style=""background:lightskyblue;border-radius:1rem;padding:0.5rem"" />

<BitNumberField Label=""Classed"" DefaultValue=""20"" Class=""custom-class"" />



<BitNumberField Label=""Styles"" DefaultValue=""1"" IconName=""@BitIconName.Microphone""
                     Styles=""@(new() { Root = ""background-color: pink;"",
                                       Icon = ""color: red;"",
                                       Label = ""color: blue; font-weight: 900; font-size: 1.25rem;"",
                                       Input = ""padding: 0.5rem; background-color: goldenrod"" })"" />

<BitNumberField Label=""Classes"" DefaultValue=""2""
                     Classes=""@(new() { Input = ""custom-input"",
                                        Focused = ""custom-focus"",
                                        Label = ""custom-label"",
                                        InputWrapper = ""custom-input-wrapper"" })"" />";

    private readonly string example6RazorCode = @"
<BitNumberField Label=""One-way"" Value=""oneWayValue"" />
<BitRating @bind-Value=""oneWayValue"" />

<BitNumberField Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitRating @bind-Value=""twoWayValue"" />";
    private readonly string example6CsharpCode = @"
private double oneWayValue;
private double twoWayValue;
";

    private readonly string example7RazorCode = @"
<BitNumberField Label=""OnIncrement & OnDecrement"" ShowButtons=""true""
                     OnIncrement=""(double v) => onIncrementCounter++""
                     OnDecrement=""(double v) => onDecrementCounter++"" />
<BitLabel>OnIncrement Counter: @onIncrementCounter</BitLabel>
<BitLabel>OnDecrement Counter: @onDecrementCounter</BitLabel>

<BitNumberField Label=""OnChange"" OnChange=""(double v) => onChangeCounter++"" />
<BitLabel>OnChange Counter: @onChangeCounter</BitLabel>";
    private readonly string example7CsharpCode = @"
private int onIncrementCounter;
private int onDecrementCounter;
private int onChangeCounter;";

    private readonly string example8RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitNumberField Label=""Age"" @bind-Value=""@validationModel.AgeInYears"" />
    <ValidationMessage For=""@(() => validationModel.AgeInYears)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example8CsharpCode = @"
public class BitNumberFieldValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 150, ErrorMessage = ""Nobody is that old"")]
    public double AgeInYears { get; set; }
}

private BitNumberFieldValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example9RazorCode = @"
<BitNumberField TValue=""int"" Label=""Prefix"" Prefix=""Distance:"" />
<BitNumberField TValue=""int"" Label=""Suffix"" Suffix=""km"" />
<BitNumberField TValue=""int"" Label=""Prefix and Suffix"" Prefix=""Distance:"" Suffix=""km"" />
<BitNumberField TValue=""int"" Label=""Step & Buttons"" Prefix=""Distance:"" Suffix=""km"" Step=""1"" ShowButtons=""true"" />
<BitNumberField TValue=""int"" Label=""Disabled"" Prefix=""Distance:"" Suffix=""km"" IsEnabled=""false"" />";
}
