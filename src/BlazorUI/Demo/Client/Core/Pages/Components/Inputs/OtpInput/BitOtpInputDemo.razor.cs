namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.OtpInput;

public partial class BitOtpInputDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the first input is focused.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#otpinput-class-styles",
            Description = "Custom CSS classes for different parts of the BitOtpInput.",
        },
        new()
        {
            Name = "Direction",
            Type = "BitOtpInputDirection",
            LinkType = LinkType.Link,
            Href = "#direction-enum",
            DefaultValue = "BitOtpInputDirection.LeftToRight",
            Description = "The OtpInput direction in four available directions.",
        },
        new()
        {
            Name = "InputType",
            Type = "BitOtpInputType",
            LinkType = LinkType.Link,
            Href = "#inputType-enum",
            DefaultValue = "BitOtpInputType.Text",
            Description = "Type of input shown as text, number, and password.",
        },
        new()
        {
            Name = "Length",
            Type = "int",
            DefaultValue = "0",
            Description = "Length of input in Otp.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<ChangeEventArgs>",
            Description = "Callback for when OtpInput value changed.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a keyboard key is pressed.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when OtpInput is focused in.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when OtpInput is focused out.",
        },
        new()
        {
            Name = "OnPaste",
            Type = "EventCallback<ClipboardEventArgs>",
            Description = "Callback for when in the OtpInput paste a content.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            Description = "Callback for when the OtpInput value change.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#otpinput-class-styles",
            Description = "Custom CSS styles for different parts of the BitOtpInput.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "otpinput-class-styles",
            Title = "BitOtpInputClassStyles",
            Description = "",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the otp input.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each input in otp input.",
                }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "direction-enum",
            Name = "BitOtpInputDirection",
            Items = new()
            {
                new()
                {
                    Name = "Text",
                    Description = "The OtpInput characters are shown as text.",
                    Value = "0"
                },
                new()
                {
                    Name = "Password",
                    Description = "The OtpInput characters are masked.",
                    Value = "1"
                },
                new()
                {
                    Name = "Number",
                    Description = "The OtpInput characters are number.",
                    Value = "2"
                }
            }
        },
        new()
        {
            Id = "inputType-enum",
            Name = "BitOtpInputType",
            Items = new()
            {
                new()
                {
                    Name = "LeftToRight",
                    Description = "The OtpInput showed in the left to right direction.",
                    Value = "0"
                },
                new()
                {
                    Name = "RightToLeft",
                    Description = "The OtpInput showed in the right to left direction.",
                    Value = "1"
                },
                new()
                {
                    Name = "TopToBottom",
                    Description = "The OtpInput showed in the top to bottom direction.",
                    Value = "2"
                },
                new()
                {
                    Name = "BottomToTop",
                    Description = "The OtpInput showed in the bottom to top direction.",
                    Value = "3"
                }
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitLabel>OtpInput</BitLabel>
<BitOtpInput Length=""4"" @bind-Value=""basicOtpInput"" />
<BitLabel>Output: [@basicOtpInput]</BitLabel>

<BitLabel>Disabled OtpInput</BitLabel>
<BitOtpInput Length=""4"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitOtpInput Length=""4"" AutoFocus=""true"" @bind-Value=""autoFocusOtpInput"" />
<BitLabel>Output: [@autoFocusOtpInput]</BitLabel>";
    private readonly string example2CsharpCode = @"
private string autoFocusOtpInput;";

    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        max-width: max-content;
        background-color: lightskyblue;
    }

    .custom-input {
        border-radius: 50%;
        border: 1px solid red;
        box-shadow: tomato 0 0 1rem;
    }
</style>

<BitOtpInput Length=""4"" Style=""box-shadow: aqua 0 0 0.5rem; max-width: max-content;"" />
<BitOtpInput Length=""4"" Class=""custom-class"" />

<BitOtpInput Length=""4"" Styles=""@(new() { Input = ""padding: 0.5rem; background-color: goldenrod""})"" />
<BitOtpInput Length=""4"" Classes=""@(new() { Input = ""custom-input""})"" />";

    private readonly string example4RazorCode = @"
Visible: [ <BitOtpInput Length=""4"" Visibility=""BitVisibility.Visible"" /> ]
Hidden: [ <BitOtpInput Length=""4"" Visibility=""BitVisibility.Hidden"" /> ]
Collapsed: [ <BitOtpInput Length=""4"" Visibility=""BitVisibility.Collapsed"" /> ]";

    private readonly string example5RazorCode = @"
<BitLabel>Text</BitLabel>
<BitOtpInput Length=""4"" InputType=""BitOtpInputType.Text"" @bind-Value=""textOtpInput"" />
<BitLabel>Output: [@textOtpInput]</BitLabel>
    
<BitLabel>Number</BitLabel>
<BitOtpInput Length=""4"" InputType=""BitOtpInputType.Number"" @bind-Value=""numberOtpInput"" />
<BitLabel>Output: [@numberOtpInput]</BitLabel>

<BitLabel>Password</BitLabel>
<BitOtpInput Length=""4"" InputType=""BitOtpInputType.Password"" @bind-Value=""passwordOtpInput"" />
<BitLabel>Output: [@passwordOtpInput]</BitLabel>";
    private readonly string example5CsharpCode = @"
private string textOtpInput;
private string numberOtpInput;
private string passwordOtpInput;";

    private readonly string example6RazorCode = @"
<BitLabel>Left to right</BitLabel>
<BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.LeftToRight"" @bind-Value=""leftToRightOtpInput"" />
<BitLabel>Output: [@leftToRightOtpInput]</BitLabel>
    
<BitLabel>Right to left</BitLabel>
<BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.RightToLeft"" @bind-Value=""rightToLeftOtpInput"" />
<BitLabel>Output: [@rightToLeftOtpInput]</BitLabel>


<BitLabel>Top to bottom</BitLabel>
<BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.TopToBottom"" @bind-Value=""topToBottomOtpInput"" />
<BitLabel>Output: [@topToBottomOtpInput]</BitLabel>

<BitLabel>Bottom to top</BitLabel>
<BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.BottomToTop"" @bind-Value=""BottomToTopOtpInput"" />
<BitLabel>Output: [@BottomToTopOtpInput]</BitLabel>";
    private readonly string example6CsharpCode = @"
private string leftToRightOtpInput;
private string rightToLeftOtpInput;
private string topToBottomOtpInput;
private string BottomToTopOtpInput;";

    private readonly string example7RazorCode = @"
<BitLabel>One-way</BitLabel>
<BitOtpInput Length=""4"" Value=""@oneWayBindOtpInput"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""oneWayBindOtpInput"" />

<BitLabel>Two-way</BitLabel>
<BitOtpInput Length=""4"" @bind-Value=""twoWayBindOtpInput"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""twoWayBindOtpInput"" />

<BitLabel>OnChange</BitLabel>
<BitOtpInput Length=""4"" OnChange=""(value) => onChangeBindOtpInput = value"" />
<BitLabel>Output: [@onChangeBindOtpInput]</BitLabel>";
    private readonly string example7CsharpCode = @"
private string oneWayBindOtpInput;
private string twoWayBindOtpInput;
private string onChangeBindOtpInput;";

    private readonly string example8RazorCode = @"
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

@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationOtpInputModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitOtpInput Length=""6"" @bind-Value=""validationOtpInputModel.OtpValue"" />
            <ValidationMessage For=""() => validationOtpInputModel.OtpValue"" />
        </div>

        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form is valid to submit successfully.
    </BitMessageBar>
}";
    private readonly string example8CsharpCode = @"
public class ValidationOtpInputModel
{
    [Required(ErrorMessage = ""Is required."")]
    [MinLength(6, ErrorMessage = ""Minimum length is 6."")]
    public string OtpValue { get; set; }
}

private ValidationOtpInputModel validationOtpInputModel = new();
public bool formIsValidSubmit;

private async Task HandleValidSubmit()
{
    formIsValidSubmit = true;

    await Task.Delay(3000);

    formIsValidSubmit = false;

    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}";
}
