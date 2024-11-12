namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumberField;

public partial class BitNumberFieldDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Description = "Custom CSS classes for different parts of the BitNumberField.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom icon name for the decrement button.",
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
            Description = "Icon name for an icon to display alongside the number field's label.",
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
            Name = "InlineLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "The position of the label in regards to the number field.",
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
            Name = "Min",
            Type = "string?",
            DefaultValue = "null",
            Description = "Min value of the number field.",
        },
        new()
        {
            Name = "Max",
            Type = "string?",
            DefaultValue = "null",
            Description = "Max value of the number field.",
        },
        new()
        {
            Name = "NumberFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the number in the number field.",
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
            Name = "OnIncrement",
            Type = "EventCallback<TValue>",
            Description = "Callback for when the increment button or up arrow key is pressed.",
        },
        new()
        {
            Name = "ParsingErrorMessage",
            Type = "string",
            DefaultValue="The {0} field is not valid.",
            Description = "The message format used for invalid values entered in the input.",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "Difference between two adjacent values of the number field.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitNumberFieldClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitNumberField.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
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
        }
    ];
    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
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
    ];
    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitNumberField.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitNumberField.",
        }
    ];


    private int minValue;
    private int maxValue;
    private int minMaxValue;

    private double oneWayValue;
    private double twoWayValue;

    private int onIncrementCounter;
    private int onDecrementCounter;
    private int onChangeCounter;

    private int? classesValue;

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
<BitNumberField Label=""Basic"" TValue=""int?"" />

<BitNumberField Label=""Disabled & DefaultValue"" DefaultValue=""1363"" IsEnabled=""false"" />

<BitNumberField Label=""Placeholder"" TValue=""int?"" Placeholder=""Enter a number..."" />

<BitNumberField Label=""Step(5) & Buttons"" TValue=""int"" Step=""5"" ShowButtons />

<BitNumberField Label=""Required"" TValue=""int?"" Required />";

    private readonly string example2RazorCode = @"
<BitNumberField Label=""Label Top"" TValue=""int"" />

<BitNumberField Label=""Inline Label"" TValue=""int"" InlineLabel />

<BitNumberField TValue=""int"">
    <LabelTemplate>
        <div style=""display:flex;align-items:center;gap:10px"">
            <BitLabel Style=""color:green;"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" Style=""font-size:18px;"" />
        </div>
    </LabelTemplate>
</BitNumberField>";

    private readonly string example3RazorCode = @"
<BitNumberField Label=""Label & Icon"" TValue=""int"" 
                IconName=""@BitIconName.Lightbulb"" />

<BitNumberField Label=""Increment & Decrement Icon"" TValue=""int""
                ShowButtons=""true""
                IncrementIconName=""@BitIconName.LikeSolid""
                DecrementIconName=""@BitIconName.DislikeSolid"" />";

    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid brown;
    }

    .custom-class *, .custom-class *::after {
        border: none;
    }


    .custom-root {
        height: 3rem;
        display: flex;
        align-items: end;
        position: relative;
        margin-inline: 1rem;
    }

    .custom-label {
        top: 0;
        left: 0;
        z-index: 1;
        padding: 0;
        font-size: 1rem;
        color: darkgray;
        position: absolute;
        transform-origin: top left;
        transform: translate(0, 22px) scale(1);
        transition: color 200ms cubic-bezier(0, 0, 0.2, 1) 0ms, transform 200ms cubic-bezier(0, 0, 0.2, 1) 0ms;
    }

    .custom-label-top {
        transform: translate(0, 1.5px) scale(0.75);
    }

    .custom-input {
        padding: 0;
        font-size: 1rem;
        font-weight: 900;
    }

    .custom-input-wrapper {
        border-radius: 0;
        position: relative;
        border-width: 0 0 1px 0;
    }

    .custom-input-wrapper::after {
        content: '';
        width: 0;
        height: 2px;
        border: none;
        position: absolute;
        inset: 100% 0 0 50%;
        background-color: blueviolet;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-focus .custom-input-wrapper::after {
        left: 0;
        width: 100%;
    }

    .custom-focus .custom-label {
        color: blueviolet;
        transform: translate(0, 1.5px) scale(0.75);
    }
</style>


<BitNumberField DefaultValue=""10"" Style=""box-shadow: aqua 0 0 1rem; margin-inline: 1rem;"" />

<BitNumberField DefaultValue=""20"" Class=""custom-class"" />


<BitNumberField DefaultValue=""1""
                Label=""Styles""
                Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                  Focused = ""--focused-background: #b2b2b25a;"",
                                  InputWrapper = ""background: var(--focused-background);"",
                                  Label = ""text-shadow: aqua 0 0 1rem; font-weight: 900; font-size: 1.25rem;"",
                                  Input = ""padding: 0.5rem;"" })"" />

<BitNumberField TValue=""int?""
                Label=""Classes""
                @bind-Value=""classesValue""
                Classes=""@(new() { Root = ""custom-root"",
                                 InputWrapper = ""custom-input-wrapper"",
                                 Focused = ""custom-focus"",
                                 Input = ""custom-input"",
                                 Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"" })"" />";

    private readonly string example5RazorCode = @"
<BitNumberField Label=""N0"" DefaultValue=""1234567890d"" NumberFormat=""N0"" />

<BitNumberField Label=""C0"" DefaultValue=""150"" NumberFormat=""C0"" />

<BitNumberField Label=""000000"" DefaultValue=""1363"" NumberFormat=""000000"" />";

    private readonly string example6RazorCode = @"
<BitNumberField TValue=""int"" Label=""Prefix"" Prefix=""Distance:"" />

<BitNumberField TValue=""int"" Label=""Suffix"" Suffix=""km"" />

<BitNumberField TValue=""int"" Label=""Prefix & Suffix"" Prefix=""Distance:"" Suffix=""km"" />

<BitNumberField TValue=""int"" Label=""With buttons"" Prefix=""Distance:"" Suffix=""km"" ShowButtons=""true"" />

<BitNumberField TValue=""int"" Label=""Disabled"" Prefix=""Distance:"" Suffix=""km"" IsEnabled=""false"" />";

    private readonly string example7RazorCode = @"
<BitNumberField Label=""One-way"" Value=""oneWayValue"" />
<BitRating @bind-Value=""oneWayValue"" />

<BitNumberField Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitRating @bind-Value=""twoWayValue"" />";
    private readonly string example7CsharpCode = @"
private double oneWayValue;
private double twoWayValue;
";

    private readonly string example8RazorCode = @"
<BitNumberField Label=""Min = 0"" Min=""0"" @bind-Value=""minValue"" />
<div>value: [@minValue]</div>

<BitNumberField Label=""Max = 100"" Max=""100"" @bind-Value=""maxValue"" />
<div>value: [@maxValue]</div>

<BitNumberField Label=""Min & Max (-10, 10)"" Min=""-10"" Max=""10"" @bind-Value=""minMaxValue"" />
<div>value: [@minMaxValue]</div>";
    private readonly string example8CsharpCode = @"
private int minValue;
private int maxValue;
private int minMaxValue;";

    private readonly string example9RazorCode = @"
<BitNumberField Label=""OnIncrement & OnDecrement"" ShowButtons=""true""
                OnIncrement=""(double v) => onIncrementCounter++""
                OnDecrement=""(double v) => onDecrementCounter++"" />
<div>OnIncrement Counter: @onIncrementCounter</div>
<div>OnDecrement Counter: @onDecrementCounter</div>

<BitNumberField Label=""OnChange"" OnChange=""(double v) => onChangeCounter++"" />
<div>OnChange Counter: @onChangeCounter</div>";
    private readonly string example9CsharpCode = @"
private int onIncrementCounter;
private int onDecrementCounter;
private int onChangeCounter;";

    private readonly string example10RazorCode = @"
<EditForm Model=""@validationModel"">
    <DataAnnotationsValidator />

    <BitNumberField Label=""@($""Age: [{validationModel.Age}]"")"" @bind-Value=""validationModel.Age"" />
    <ValidationMessage For=""@(() => validationModel.Age)"" />
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example10CsharpCode = @"
public class BitNumberFieldValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 150, ErrorMessage = ""Nobody is that old"")]
    public int? Age { get; set; }
}

private BitNumberFieldValidationModel validationModel = new();";

    private readonly string example11RazorCode = @"
<CascadingValue Value=""BitDir.Rtl"">

    <BitNumberField Label=""برچسب در بالا"" TValue=""int"" ShowButtons />

    <BitNumberField Label=""برچسب درخط"" TValue=""int"" InlineLabel />

    <BitNumberField TValue=""int"" Required />

    <BitNumberField Label=""الزامی"" TValue=""int"" Required />

</CascadingValue>";
}
