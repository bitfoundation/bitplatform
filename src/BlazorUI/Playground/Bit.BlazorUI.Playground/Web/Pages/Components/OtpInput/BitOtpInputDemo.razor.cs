using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.OtpInput;

public partial class BitOtpInputDemo
{
    private string basicOtpInput;
    private string autoFocusOtpInput;
    private string textOtpInput;
    private string numberOtpInput;
    private string passwordOtpInput;
    private string leftToRightOtpInput;
    private string rightToLeftOtpInput;
    private string topToBottomOtpInput;
    private string BottomToTopOtpInput;
    private string twoWayBindOtpInput;
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
<div class=""otp-input-box"">
    <BitLabel>OtpInput</BitLabel>
    <BitOtpInput InputCount=""4"" @bind-Value=""basicOtpInput"" />
    <div>Output: [@basicOtpInput]</div>
</div>
<div class=""otp-input-box"">
    <BitLabel>Disabled OtpInput</BitLabel>
    <BitOtpInput InputCount=""6"" IsEnabled=""false"" />
</div>
";
    private static readonly string example2HtmlCode = @"
<BitOtpInput InputCount=""4"" AutoFocus=""true"" @bind-Value=""autoFocusOtpInput"" />
<div>Output: [@autoFocusOtpInput]</div>
";
    private static readonly string example3HtmlCode = @"
<div class=""otp-input-box"">
    <BitLabel>Text</BitLabel>
    <BitOtpInput InputCount=""4"" InputType=""BitOtpInputType.Text"" @bind-Value=""textOtpInput"" />
    <div>Output: [@textOtpInput]</div>
</div>
<div class=""otp-input-box"">
    <BitLabel>Number</BitLabel>
    <BitOtpInput InputCount=""4"" InputType=""BitOtpInputType.Number"" @bind-Value=""numberOtpInput"" />
    <div>Output: [@numberOtpInput]</div>
</div>
<div class=""otp-input-box"">
    <BitLabel>Password</BitLabel>
    <BitOtpInput InputCount=""4"" InputType=""BitOtpInputType.Password"" @bind-Value=""passwordOtpInput"" />
    <div>Output: [@passwordOtpInput]</div>
</div>
";
    private static readonly string example4HtmlCode = @"
<BitLabel>Left to right</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.LeftToRight"" @bind-Value=""leftToRightOtpInput"" />

<BitLabel>Right to left</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.RightToLeft"" @bind-Value=""rightToLeftOtpInput"" />

<BitLabel>Top to bottom</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.TopToBottom"" @bind-Value=""topToBottomOtpInput"" />

<BitLabel>Bottom to top</BitLabel>
<BitOtpInput InputCount=""4"" Direction=""BitOtpInputDirection.BottomToTop"" @bind-Value=""BottomToTopOtpInput"" />
";
    private static readonly string example5HtmlCode = @"
<div class=""otp-input-box"">
    <BitOtpInput InputCount=""4"" @bind-Value=""twoWayBindOtpInput"" />
</div>
<div>
    <BitLabel>Output:</BitLabel>
    <BitTextField @bind-Value=""twoWayBindOtpInput"" />
</div>
";
    private static readonly string example6HtmlCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationOtpInputModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""otp-input-box"">
            <BitOtpInput InputCount=""6"" @bind-Value=""validationOtpInputModel.OtpValue"" />
            <ValidationMessage For=""() => validationOtpInputModel.OtpValue"" />
            <div>Output: [@validationOtpInputModel.OtpValue]</div>
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
    private static readonly string example2CSharpCode = @"
private string autoFocusOtpInput;
";
    private static readonly string example3CSharpCode = @"
private string textOtpInput;
private string numberOtpInput;
private string passwordOtpInput;
";
    private static readonly string example4CSharpCode = @"
private string leftToRightOtpInput;
private string rightToLeftOtpInput;
private string topToBottomOtpInput;
private string BottomToTopOtpInput;
";
    private static readonly string example5CSharpCode = @"
private string twoWayBindOtpInput;
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
