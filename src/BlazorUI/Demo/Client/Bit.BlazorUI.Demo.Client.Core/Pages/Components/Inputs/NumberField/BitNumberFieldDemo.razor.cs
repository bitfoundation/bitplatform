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
            Name = "DecrementIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display on the decrement button using custom CSS classes for external icon libraries. Takes precedence over DecrementIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DecrementIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon for the decrement button from the built-in Fluent UI icons. For external icon libraries, use DecrementIcon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
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
            Type = "TValue?",
            DefaultValue = "null",
            Description = "Initial value of the number field.",
        },
        new()
        {
            Name = "HideInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the input is hidden.",
        },
        new()
        {
            Name = "IconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display alongside the number field using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display alongside the number field from the built-in Fluent UI icons. For external icon libraries, use Icon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
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
            Name = "IncrementIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display on the increment button using custom CSS classes for external icon libraries. Takes precedence over IncrementIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IncrementIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon for the increment button from the built-in Fluent UI icons. For external icon libraries, use IncrementIcon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IncrementTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse is placed on the increment button.",
        },
        new()
        {
            Name = "InvertMouseWheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the mouse wheel direction.",
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
            Name = "LabelPosition",
            Type = "BitLabelPosition",
            DefaultValue = "BitLabelPosition.Top",
            Description = "The position of the label in regards to the spin button.",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum",
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
            Name = "Mode",
            Type = "BitSpinButtonMode?",
            DefaultValue = "null",
            Description = "Determines how the spinning buttons should be rendered.",
            LinkType = LinkType.Link,
            Href = "#spinMode-enum",
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
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback executed when the user clears the number field by either clicking 'X' or hitting escape.",
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
            Name = "Precision",
            Type = "int?",
            DefaultValue = "null",
            Description = "How many decimal places the value should be rounded to.",
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
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display on the clear button using custom CSS classes for external icon libraries. Takes precedence over ClearButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon for the clear button from the built-in Fluent UI icons. For external icon libraries, use ClearButtonIcon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to shows the clear button when the BitNumberField has value.",
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
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Description = "Represents icon information for rendering icons in Bit BlazorUI components. Supports both built-in Fluent UI icons and custom/external icon libraries (e.g. FontAwesome, Bootstrap Icons). Use BitIconInfo.Css(string), BitIconInfo.Fa(string), or BitIconInfo.Bi(string) for external icons.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon. For external icons, this can be the full CSS class name if BaseClass and Prefix are empty."
                },
                new()
                {
                    Name = "BaseClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
                },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitNumberFieldClassStyles",
            Parameters =
            [
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
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of label and input in the number field."
                },
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's root element."
                }
            ]
        }
    ];
    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "labelPosition-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spin button.",
                    Value="0",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows on the start of the spin button.",
                    Value="1",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the spin button.",
                    Value="2",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the spin button.",
                    Value="3",
                }
            ]
        },
        new()
        {
            Id = "spinMode-enum",
            Name = "BitSpinButtonMode",
            Description = "",
            Items =
            [
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
                    Value="1",
                },
                new()
                {
                    Name= "Spread",
                    Description="Spinning buttons render at the start and end of the input.",
                    Value="2",
                }
            ]
        },
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

    private int hideInputValue;

    private bool invertMouseWheel;

    private double precisionInputValue = 3.1415;

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
}
