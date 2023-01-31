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
    private string oneWayBindOtpInput;
    private string twoWayBindOtpInput;
    private string onChangeBindOtpInput;

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
            Name = "Length",
            Type = "int",
            Description = "Length of input in Otp.",
        },
        new ComponentParameter()
        {
            Name = "OnInput",
            Type = "EventCallback<ChangeEventArgs>",
            Description = "Callback for when OtpInput value changed.",
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
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            Description = "Callback for when the OtpInput value change.",
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

    #region Sample Code 1

    private static readonly string example1HtmlCode = @"
<div>
    <BitLabel>OtpInput</BitLabel>
    <BitOtpInput Length=""4"" @bind-Value=""basicOtpInput"" />
    <div>Output: [@basicOtpInput]</div>
</div>
<div>
    <BitLabel>Disabled OtpInput</BitLabel>
    <BitOtpInput Length=""6"" IsEnabled=""false"" />
</div>
";

    #endregion

    #region Sample Code 2

    private static readonly string example2HtmlCode = @"
<div>
    <BitOtpInput Length=""4"" AutoFocus=""true"" @bind-Value=""autoFocusOtpInput"" />
    <div>Output: [@autoFocusOtpInput]</div>
</div>
";

    private static readonly string example2CSharpCode = @"
private string autoFocusOtpInput;
";

    #endregion

    #region Sample Code 3

    private static readonly string example3HtmlCode = @"
<div>
    <BitLabel>Text</BitLabel>
    <BitOtpInput Length=""4"" InputType=""BitOtpInputType.Text"" @bind-Value=""textOtpInput"" />
    <div>Output: [@textOtpInput]</div>
</div>
<div>
    <BitLabel>Number</BitLabel>
    <BitOtpInput Length=""4"" InputType=""BitOtpInputType.Number"" @bind-Value=""numberOtpInput"" />
    <div>Output: [@numberOtpInput]</div>
</div>
<div>
    <BitLabel>Password</BitLabel>
    <BitOtpInput Length=""4"" InputType=""BitOtpInputType.Password"" @bind-Value=""passwordOtpInput"" />
    <div>Output: [@passwordOtpInput]</div>
</div>
";

    private static readonly string example3CSharpCode = @"
private string textOtpInput;
private string numberOtpInput;
private string passwordOtpInput;
";

    #endregion

    #region Sample Code 4

    private static readonly string example4HtmlCode = @"
<div>
    <BitLabel>Left to right</BitLabel>
    <BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.LeftToRight"" @bind-Value=""leftToRightOtpInput"" />
    <div>Output: [@leftToRightOtpInput]</div>
</div>
<div>
    <BitLabel>Right to left</BitLabel>
    <BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.RightToLeft"" @bind-Value=""rightToLeftOtpInput"" />
    <div>Output: [@rightToLeftOtpInput]</div>
</div>

<div>
    <BitLabel>Top to bottom</BitLabel>
    <BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.TopToBottom"" @bind-Value=""topToBottomOtpInput"" />
    <div>Output: [@topToBottomOtpInput]</div>
</div>
<div>
    <BitLabel>Bottom to top</BitLabel>
    <BitOtpInput Length=""4"" Direction=""BitOtpInputDirection.BottomToTop"" @bind-Value=""BottomToTopOtpInput"" />
    <div>Output: [@BottomToTopOtpInput]</div>
</div>
";

    private static readonly string example4CSharpCode = @"
private string leftToRightOtpInput;
private string rightToLeftOtpInput;
private string topToBottomOtpInput;
private string BottomToTopOtpInput;
";

    #endregion

    #region Sample Code 5

    private static readonly string example5HtmlCode = @"
<div>
    <BitLabel>One-way</BitLabel>
    <BitOtpInput Length=""4"" Value=""@oneWayBindOtpInput"" />
    <BitTextField Style=""margin-top: 5px;"" @bind-Value=""oneWayBindOtpInput"" />
</div>
<div>
    <BitLabel>Two-way</BitLabel>
    <BitOtpInput Length=""4"" @bind-Value=""twoWayBindOtpInput"" />
    <BitTextField Style=""margin-top: 5px;"" @bind-Value=""twoWayBindOtpInput"" />
</div>
<div>
    <BitLabel>OnChange</BitLabel>
    <BitOtpInput Length=""4"" OnChange=""(value) => onChangeBindOtpInput = value"" />
    <div>Output: [@onChangeBindOtpInput]</div>
</div>
";

    private static readonly string example5CSharpCode = @"
private string oneWayBindOtpInput;
private string twoWayBindOtpInput;
private string onChangeBindOtpInput;
";

    #endregion

    #region Sample Code 6

    private static readonly string example6HtmlCode = @"
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
}
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

    #endregion
}
