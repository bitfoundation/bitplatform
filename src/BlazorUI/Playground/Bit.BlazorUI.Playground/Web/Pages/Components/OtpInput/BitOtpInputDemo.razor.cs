using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.OtpInput;

public partial class BitOtpInputDemo
{
    private string otpInput;
    private ValidationOtpInputModel validationOtpInputModel = new();
    public bool formIsValidSubmit;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the first input is focused.",
        },
        new ComponentParameter()
        {
            Name = "Direction",
            Type = "BitOtpInputDirection",
            LinkType = LinkType.Link,
            Href = "#direction-enum",
            DefaultValue = "BitOtpInputDirection.LeftToRight",
            Description = "The OtpInput direction in four available directions.",
        },
        new ComponentParameter()
        {
            Name = "InputType",
            Type = "BitOtpInputType",
            LinkType = LinkType.Link,
            Href = "#inputType-enum",
            DefaultValue = "BitOtpInputType.Text",
            Description = "Type of input shown as text, number, and password.",
        },
        new ComponentParameter()
        {
            Name = "InputCount",
            Type = "int",
            Description = "Count of input in Otp.",
        },
        new ComponentParameter()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a keyboard key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when OtpInput is focused in.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when OtpInput is focused out.",
        },
        new ComponentParameter()
        {
            Name = "OnPaste",
            Type = "EventCallback<ClipboardEventArgs>",
            Description = "Callback for when in the OtpInput paste a content.",
        },
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "direction-enum",
            Title = "BitOtpInputDirection Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem
                {
                    Name = "Text",
                    Description = "The OtpInput characters are shown as text.",
                    Value = "0"
                },
                new EnumItem
                {
                    Name = "Password",
                    Description = "The OtpInput characters are masked.",
                    Value = "1"
                },
                new EnumItem
                {
                    Name = "Number",
                    Description = "The OtpInput characters are number.",
                    Value = "2"
                }
            }
        },
        new EnumParameter()
        {
            Id = "inputType-enum",
            Title = "BitOtpInputType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem
                {
                    Name = "LeftToRight",
                    Description = "The OtpInput showed in the left to right direction.",
                    Value = "0"
                },
                new EnumItem
                {
                    Name = "RightToLeft",
                    Description = "The OtpInput showed in the right to left direction.",
                    Value = "1"
                },
                new EnumItem
                {
                    Name = "TopToBottom",
                    Description = "The OtpInput showed in the top to bottom direction.",
                    Value = "2"
                },
                new EnumItem
                {
                    Name = "BottomToTop",
                    Description = "The OtpInput showed in the bottom to top direction.",
                    Value = "3"
                }
            }
        }
    };

    private static readonly string example1HtmlCode = @"
<BitLabel>OtpInput</BitLabel>
<BitOtpInput InputCount=""4"" />

<BitLabel>Disabled OtpInput</BitLabel>
<BitOtpInput InputCount=""6"" IsEnabled=""false"" />
";
    private static readonly string example2HtmlCode = @"
<BitOtpInput InputCount=""4"" AutoFocus=""true"" />
";
    private static readonly string example3HtmlCode = @"
<BitLabel>Text</BitLabel>
<BitOtpInput InputCount=""4"" InputType=""BitOtpInputType.Text"" />

<BitLabel>Number</BitLabel>
<BitOtpInput InputCount=""4"" InputType=""BitOtpInputType.Number"" />

<BitLabel>Password</BitLabel>
<BitOtpInput InputCount=""4"" InputType=""BitOtpInputType.Password"" />
";
    private static readonly string example4HtmlCode = @"
<BitLabel>Left to right</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.LeftToRight"" />

<BitLabel>Right to left</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.RightToLeft"" />

<BitLabel>Top to bottom</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.TopToBottom"" />

<BitLabel>Bottom to top</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.BottomToTop"" />
";
    private static readonly string example5HtmlCode = @"
<BitOtpInput InputCount=""4"" @bind-Value=""otpInput"" />

<div>Output: @otpInput</div>
";
    private static readonly string example6HtmlCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationOtpInputModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""otp-input-box"">
            <BitOtpInput InputCount=""6"" @bind-Value=""validationOtpInputModel.OtpValue"" />
            <ValidationMessage For=""() => validationOtpInputModel.OtpValue"" />
        </div>
        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form is valid to submit successfully.
    </BitMessageBar>
}
";
    private static readonly string example5CSharpCode = @"
private string otpInput;
";
    private static readonly string example6CSharpCode = @"
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
}
";

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
}
