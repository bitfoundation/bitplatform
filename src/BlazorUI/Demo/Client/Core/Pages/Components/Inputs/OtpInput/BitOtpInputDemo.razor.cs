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
            Description = "If true, the first input is auto focused.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitOtpInput.",
            LinkType = LinkType.Link,
            Href = "#otpinput-class-styles",
        },
        new()
        {
            Name = "Direction",
            Type = "BitOtpInputDirection",
            DefaultValue = "BitOtpInputDirection.LeftToRight",
            Description = "The render direction of the inputs.",
            LinkType = LinkType.Link,
            Href = "#direction-enum",
        },
        new()
        {
            Name = "InputType",
            Type = "BitOtpInputType",
            DefaultValue = "BitOtpInputType.Text",
            Description = "Type of the inputs.",
            LinkType = LinkType.Link,
            Href = "#inputType-enum",
        },
        new()
        {
            Name = "Length",
            Type = "int",
            DefaultValue = "5",
            Description = "Length of the OTP or number of the inputs.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            Description = "Callback for when the OtpInput value change.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "onfocusin event callback for each input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "onfocusout event callback for each input.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<ChangeEventArgs>",
            Description = "oninput event callback for each input.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "onkeydown event callback for each input.",
        },
        new()
        {
            Name = "OnPaste",
            Type = "EventCallback<ClipboardEventArgs>",
            Description = "onpaste event callback for each input.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitOtpInput.",
            LinkType = LinkType.Link,
            Href = "#otpinput-class-styles",
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
        },
        new()
        {
            Id = "inputType-enum",
            Name = "BitOtpInputType",
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
        }
    };



    private string? oneWayValue;
    private string? twoWayValue;
    private string? onChangeValue;

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
    }



    private readonly string example1RazorCode = @"
<BitOtpInput />
<BitOtpInput Length=""4"" />
<BitOtpInput IsEnabled=""false"" />
<BitOtpInput AutoFocus=""true"" />";

    private readonly string example2RazorCode = @"
<BitOtpInput InputType=""BitOtpInputType.Text"" />
<BitOtpInput InputType=""BitOtpInputType.Number"" />
<BitOtpInput InputType=""BitOtpInputType.Password"" />";

    private readonly string example3RazorCode = @"
<BitOtpInput Direction=""BitOtpInputDirection.LeftToRight"" />
<BitOtpInput Direction=""BitOtpInputDirection.RightToLeft"" />
<BitOtpInput Direction=""BitOtpInputDirection.TopToBottom"" />
<BitOtpInput Direction=""BitOtpInputDirection.BottomToTop"" />";

    private readonly string example4RazorCode = @"
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

<BitOtpInput Style=""box-shadow:aqua 0 0 0.5rem;max-width:max-content;"" />
<BitOtpInput Class=""custom-class"" />

<BitOtpInput Styles=""@(new() { Input = ""padding:0.5rem;background-color:goldenrod""})"" />
<BitOtpInput Classes=""@(new() { Input = ""custom-input""})"" />";

    private readonly string example5RazorCode = @"
<BitOtpInput Value=""@oneWayValue"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""oneWayValue"" />

<BitOtpInput @bind-Value=""twoWayValue"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""twoWayValue"" />

<BitOtpInput OnChange=""v => onChangeValue = v"" />
<div>OnChange value: @onChangeValue</div>";
    private readonly string example5CsharpCode = @"
private string? oneWayValue;
private string? twoWayValue;
private string? onChangeValue;";

    private readonly string example6RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationOtpInputModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitOtpInput Length=""6"" @bind-Value=""validationOtpInputModel.OtpValue"" />
    <ValidationMessage For=""() => validationOtpInputModel.OtpValue"" />

    <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example6CsharpCode = @"
public class ValidationOtpInputModel
{
    [Required(ErrorMessage = ""The OTP value is required."")]
    [MinLength(6, ErrorMessage = ""Minimum length is 6."")]
    public string OtpValue { get; set; }
}

private ValidationOtpInputModel validationOtpInputModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";
}
