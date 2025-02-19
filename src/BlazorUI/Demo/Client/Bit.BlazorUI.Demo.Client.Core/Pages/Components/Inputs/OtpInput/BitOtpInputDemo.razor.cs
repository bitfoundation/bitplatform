using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.OtpInput;

public partial class BitOtpInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the first input is auto focused.",
        },
        new()
        {
            Name = "AutoShift",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto shifting the indexes while clearing the inputs using Delete or Backspace.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitOtpInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label displayed above the inputs.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the label displayed above the inputs.",
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
            Name = "OnFill",
            Type = "EventCallback<string?>",
            Description = "Callback for when all of the inputs are filled.",
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
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render inputs in the opposite direction.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the inputs.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitOtpInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Type",
            Type = "BitInputType?",
            DefaultValue = "null",
            Description = "Type of the inputs.",
            LinkType = LinkType.Link,
            Href = "#input-type-enum",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render inputs vertically.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitOtpInputClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the otp input.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the otp input.",
                },
                new()
                {
                    Name = "InputsWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper element of the inputs.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each input in otp input.",
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focused input in otp input.",
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [

        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "input-type-enum",
            Name = "BitInputType",
            Items =
            [
                new()
                {
                    Name= "Text",
                    Description="The input expects text characters.",
                    Value="0",
                },
                new()
                {
                    Name= "Password",
                    Description="The input expects password characters.",
                    Value="1",
                },
                new()
                {
                    Name= "Number",
                    Description="The input expects number characters.",
                    Value="2",
                },
                new()
                {
                    Name= "Email",
                    Description="The input expects email characters.",
                    Value="3",
                },
                new()
                {
                    Name= "Tel",
                    Description="The input expects tel characters.",
                    Value="4",
                },
                new()
                {
                    Name= "Url",
                    Description="The input expects url characters.",
                    Value="5",
                }
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElements",
            Type = "ElementReference[]",
            Description = "The ElementReferences to the input elements of the BitOtpInput.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to a specific input element of the BitOtpInput.",
        }
    ];



    private string? oneWayValue;
    private string? twoWayValue;
    private string? immediateValue;
    private string? debounceValue;
    private string? throttleValue;

    private string? onChangeValue;
    private string? onFillValue;
    private (FocusEventArgs Event, int Index)? onFocusInArgs;
    private (FocusEventArgs Event, int Index)? onFocusOutArgs;
    private (ChangeEventArgs Event, int Index)? onInputArgs;
    private (KeyboardEventArgs Event, int Index)? onKeyDownArgs;
    private (ClipboardEventArgs Event, int Index)? onPasteArgs;

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
}
