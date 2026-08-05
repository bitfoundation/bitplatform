namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumberField;

public partial class BitNumberFieldDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the number field, used for its focus indicator and for the icon, prefix and suffix while the field is focused (Primary by default).",
            LinkType = LinkType.Link,
            Href = "#bit-color",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the input for the benefit of screen readers. It is rendered into a visually hidden element that the input references through its aria-describedby attribute.",
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
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the number field background (Primary by default).",
            LinkType = LinkType.Link,
            Href = "#bit-color-kind",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the number field border (Primary by default).",
            LinkType = LinkType.Link,
            Href = "#bit-color-kind",
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
            Name = "ContinuousSpinDelay",
            Type = "int",
            DefaultValue = "400",
            Description = "The delay in milliseconds before the value starts changing continuously while an increment/decrement button is held down.",
        },
        new()
        {
            Name = "ContinuousSpinInterval",
            Type = "int",
            DefaultValue = "75",
            Description = "The interval in milliseconds between two consecutive value changes while an increment/decrement button is held down.",
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
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A hint rendered under the field, describing what is expected of it (e.g. the accepted range or the unit). Unlike AriaDescription it is visible, and the input references it through its aria-describedby attribute so it is announced along with the field.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom template rendered in place of the Description, referenced by the input through its aria-describedby attribute just the same.",
        },
        new()
        {
            Name = "DigitsNormalizer",
            Type = "Func<string?, string?>?",
            DefaultValue = "null",
            Description = "A custom function to normalize the raw input string before it gets parsed into the value. When provided, it takes precedence over NormalizeDigits and lets the developer plug in their own culture-specific or domain-specific transformation.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the number field to the full width of its container. By default the field only takes the width it needs, which keeps a stepper from spanning a whole form row.",
        },
        new()
        {
            Name = "HideInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the text input element while keeping the increment/decrement buttons functional, turning the component into a stepper-only control.",
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
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Overrides the virtual keyboard the browser shows for the input. By default it is Numeric for the integral types and Decimal for the fractional ones (float, double and decimal). Since neither of those keypads offers a minus sign on every platform, a field that has to accept negative values on touch devices is better served by Text, which brings up the full keyboard.",
            LinkType = LinkType.Link,
            Href = "#inputMode-enum",
        },
        new()
        {
            Name = "InvertMouseWheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the direction of the value change when the user spins the value using the mouse wheel (the wheel only changes the value while the Shift key is held down, to keep normal page scrolling intact).",
        },
        new()
        {
            Name = "IsInputReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes only the text input part read-only, preventing typing, while the value can still be changed using the increment/decrement buttons, the arrow keys and the mouse wheel (unlike ReadOnly, which blocks all of them).",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "The position of the label in regards to the field (Top by default).",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Descriptive label for the number field, rendered next to it (per LabelPosition) and read by screen readers.",
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
            Description = "The minimum value of the number field. Values below it get clamped to it, both when typed and when spinning. It is a string to support any numeric type of the field; an unparsable value falls back to the type's MinValue.",
        },
        new()
        {
            Name = "Max",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum value of the number field. Values above it get clamped to it, both when typed and when spinning. It is a string to support any numeric type of the field; an unparsable value falls back to the type's MaxValue.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitSpinButtonMode?",
            DefaultValue = "null",
            Description = "Determines how the increment/decrement buttons render: Compact (stacked at the end of the input), Inline (side by side at the end) or Spread (one on each side). When null (default), no buttons render, while the value can still be changed using the arrow keys and the mouse wheel.",
            LinkType = LinkType.Link,
            Href = "#spinMode-enum",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border of the number field, which is what you want when it sits inside a surface that already provides one (a toolbar, a table cell or a card).",
        },
        new()
        {
            Name = "NoClamp",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps values typed outside of the Min/Max range intact instead of clamping them to the nearest bound, so that a form validation (e.g. a [Range] data annotation) can report the out-of-range value to the user instead of it being silently corrected. Stepping with the increment/decrement buttons, the arrow keys or the mouse wheel still stays inside the range, and the Home/End keys still jump to the bounds.",
        },
        new()
        {
            Name = "NoMouseWheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables changing the value using the mouse wheel entirely (by default the value changes when the wheel is scrolled over the focused field while the Shift key is held down).",
        },
        new()
        {
            Name = "NoSelectOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the automatic select-all of the input's text when the field receives focus.",
        },
        new()
        {
            Name = "NormalizeDigits",
            Type = "bool",
            DefaultValue = "false",
            Description = "Normalizes non-Latin (e.g. Persian \"۱۲۳\" or Arabic \"١٢٣\") decimal digits to their Latin (0-9) equivalents before parsing. This is culture-agnostic and works for any Unicode decimal digit system.",
        },
        new()
        {
            Name = "NumberFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the number in the number field, using the standard or custom .NET numeric format strings (e.g. \"N0\", \"C0\" or \"000000\"). The formatting is applied whenever the value is committed, while the bound value stays a plain number. Value-scaling formats (like the percent \"P\" format) are not suitable, since the scaled display cannot be parsed back into the same value.",
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
            Description = "Callback executed when the user clears the number field by clicking the clear button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the input is clicked.",
        },
        new()
        {
            Name = "OnDecrement",
            Type = "EventCallback<TValue>",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
        },
        new()
        {
            Name = "OnEnter",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when the Enter key is pressed on the input. It is invoked after the typed text has been committed, so the bound value it observes is already the one the user just entered.",
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
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a key is pressed down on the input. It is invoked for every key, including the ones the field handles itself (the arrow keys, PageUp/PageDown, Home/End and Escape).",
        },
        new()
        {
            Name = "OnKeyUp",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a key is released on the input.",
        },
        new()
        {
            Name = "OnMaxReached",
            Type = "EventCallback<TValue>",
            Description = "Callback for when a step lands the value on (or beyond) the explicit Max, letting the consumer react to the ceiling being hit. It only fires for an explicit Max, and only on the step that reaches it.",
        },
        new()
        {
            Name = "OnMinReached",
            Type = "EventCallback<TValue>",
            Description = "Callback for when a step lands the value on (or beyond) the explicit Min. It only fires for an explicit Min, and only on the step that reaches it.",
        },
        new()
        {
            Name = "PageStep",
            Type = "string?",
            DefaultValue = "null",
            Description = "The amount by which the value changes when the user presses the PageUp/PageDown keys, providing a larger jump than the regular Step. It is a string to support any numeric type of the field; when not provided (or unparsable), PageUp/PageDown change the value by 10 times the Step.",
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
            Description = "How many decimal places the value should be rounded to. When not provided, the precision is derived from the fractional digits of the Step parameter (if any); otherwise no rounding is applied. A negative value rounds to a power of ten (e.g. -2 rounds to the nearest hundred).",
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
            Name = "ClearButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label text for the clear button (for screen reader users), useful for localization.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the clear button whenever the field is showing something, resetting the value to null with a single click (most useful with nullable value types). \"Showing something\" covers a string the user typed that failed to parse as well as a real value, so the button is also there to wipe an entry that has to be corrected. It is not rendered while the field is read-only or empty. It stays out of the tab order (like the increment/decrement buttons), the Escape key being the keyboard equivalent of clicking it.",
        },
        new()
        {
            Name = "SnapToStep",
            Type = "bool",
            DefaultValue = "false",
            Description = "Snaps the committed value to the nearest multiple of the Step (anchored at the Min when one is provided), so typed values align to the same grid that the increment/decrement stepping produces. Without it, typed values are kept as-is (aside from min/max clamping and precision rounding).",
        },
        new()
        {
            Name = "Step",
            Type = "string?",
            DefaultValue = "null",
            Description = "The difference between two adjacent values of the number field, applied when spinning the value using the increment/decrement buttons, the Up/Down arrow keys or the mouse wheel. A fractional step (e.g. \"0.01\") also implies the rounding precision of the field, unless an explicit Precision is provided. It is a string to support any numeric type of the field; an unparsable value falls back to 1.",
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
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the number field with a single bottom rule instead of a full border, the classic \"underlined\" input variant.",
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
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's description."
                },
                new()
                {
                    Name = "DescriptionContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's description container."
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
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's clear button."
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's clear button icon."
                },
                new()
                {
                    Name = "PrefixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's prefix container."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's prefix."
                },
                new()
                {
                    Name = "SuffixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's suffix container."
                },
                new()
                {
                    Name = "Suffix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the number field's suffix."
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
            Id = "inputMode-enum",
            Name = "BitInputMode",
            Description = "This allows a browser to display an appropriate virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="No virtual keyboard. For when the page implements its own keyboard input control.",
                    Value="0",
                },
                new()
                {
                    Name= "Text",
                    Description="Standard input keyboard for the user's current locale.",
                    Value="1",
                },
                new()
                {
                    Name= "Decimal",
                    Description="Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.",
                    Value="2",
                },
                new()
                {
                    Name= "Numeric",
                    Description="Numeric input keyboard, but only requires the digits 0–9.",
                    Value="3",
                },
                new()
                {
                    Name= "Tel",
                    Description="A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key.",
                    Value="4",
                },
                new()
                {
                    Name= "Search",
                    Description="A virtual keyboard optimized for search input.",
                    Value="5",
                },
                new()
                {
                    Name= "Email",
                    Description="A virtual keyboard optimized for entering email addresses.",
                    Value="6",
                },
                new()
                {
                    Name= "Url",
                    Description="A keypad optimized for entering URLs.",
                    Value="7",
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
        new()
        {
            Id = "bit-color",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name= "Primary", Description="Primary general color.", Value="0" },
                new() { Name= "Secondary", Description="Secondary general color.", Value="1" },
                new() { Name= "Tertiary", Description="Tertiary general color.", Value="2" },
                new() { Name= "Info", Description="Info general color.", Value="3" },
                new() { Name= "Success", Description="Success general color.", Value="4" },
                new() { Name= "Warning", Description="Warning general color.", Value="5" },
                new() { Name= "SevereWarning", Description="SevereWarning general color.", Value="6" },
                new() { Name= "Error", Description="Error general color.", Value="7" },
                new() { Name= "PrimaryBackground", Description="Primary background color.", Value="8" },
                new() { Name= "SecondaryBackground", Description="Secondary background color.", Value="9" },
                new() { Name= "TertiaryBackground", Description="Tertiary background color.", Value="10" },
                new() { Name= "PrimaryForeground", Description="Primary foreground color.", Value="11" },
                new() { Name= "SecondaryForeground", Description="Secondary foreground color.", Value="12" },
                new() { Name= "TertiaryForeground", Description="Tertiary foreground color.", Value="13" },
                new() { Name= "PrimaryBorder", Description="Primary border color.", Value="14" },
                new() { Name= "SecondaryBorder", Description="Secondary border color.", Value="15" },
                new() { Name= "TertiaryBorder", Description="Tertiary border color.", Value="16" }
            ]
        },
        new()
        {
            Id = "bit-color-kind",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new() { Name= "Primary", Description="Primary color kind.", Value="0" },
                new() { Name= "Secondary", Description="Secondary color kind.", Value="1" },
                new() { Name= "Tertiary", Description="Tertiary color kind.", Value="2" },
                new() { Name= "Transparent", Description="Transparent color kind.", Value="3" }
            ]
        },
    ];
    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ClearAsync",
            Type = "Task",
            Description = "Clears whatever the field is showing - a value or a string that failed to parse - and raises OnClear, exactly as the clear button and the Escape key do (without requiring ShowClearButton, since there is no button involved). It does nothing while the field is disabled or read-only.",
        },
        new()
        {
            Name = "DecrementAsync",
            Type = "Task",
            Description = "Decrements the value by the Step, the mirror image of IncrementAsync.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitNumberField.",
        },
        new()
        {
            Name = "IncrementAsync",
            Type = "Task",
            Description = "Increments the value by the Step, exactly as the increment button does - bounds, snapping, rounding and the OnIncrement/OnMaxReached callbacks all included. It does nothing while the field is disabled or read-only, or when the value already sits at the Max.",
        },
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitNumberField.",
        }
    ];


    private int minValue;
    private int maxValue;
    private int minMaxValue;

    private int stepValue;
    private double fractionalStepValue;
    private int stepMinMaxValue;
    private int fastSpinValue;
    private int pageStepValue;

    private int snapValue;
    private int snapAnchoredValue = 2;
    private double snapFractionValue;

    private double oneWayValue;
    private double twoWayValue;
    private int? uncontrolledValue;

    private int? immediateValue;
    private double? immediateDecimalValue;
    private int? debounceValue;
    private int? throttleValue;

    private int clampValue;
    private int noClampValue;

    private int readOnlyValue = 10;
    private int inputReadOnlyValue = 10;

    private int onIncrementCounter;
    private int onDecrementCounter;
    private int onChangeCounter;
    private int onClearCounter;
    private int clearedCounter;
    private int onKeyUpCounter;
    private int onClickCounter;
    private string? lastKey;
    private string? boundMessage;
    private string? enterMessage;
    private int? enterValue;

    private int? apiValue = 4;
    private BitNumberField<int?>? apiNumberField;

    private int? classesValue;

    private int? normalizeOffValue;
    private int? normalizeOnValue;
    private double? normalizeDecimalValue;
    private int? customNormalizerValue;

    private int hideInputValue;

    private bool invertMouseWheel;

    private double precisionInputValue = 3.1415;
    private double negativePrecisionInputValue;

    private byte byteValue = 5;
    private long longValue = 1_000_000_000_000;
    private double doubleValue = 1.5;
    private decimal decimalValue = 0.05m;
    private int signedValue = -5;

    private string SuccessMessage = string.Empty;
    private BitNumberFieldValidationModel validationModel = new();
    private double? parsingErrorValue;

    public class RangeModel
    {
        [Range(0, 100, ErrorMessage = "The percentage must be between 0 and 100")]
        public int Percentage { get; set; }
    }

    private RangeModel rangeModel = new();

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

    private void HandleMinReached(int value)
    {
        boundMessage = $"Reached the minimum ({value}).";
    }

    private void HandleMaxReached(int value)
    {
        boundMessage = $"Reached the maximum ({value}).";
    }

    private void HandleEnter()
    {
        enterMessage = $"Submitted: {enterValue}.";
    }

    // Custom normalizer: maps any Unicode decimal digit to its Latin equivalent
    // and strips spaces and thousand separators (Latin ',' and Persian '٬').
    private string? CustomDigitsNormalizer(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is ' ' or ',' or '٬') continue;

            var digit = System.Globalization.CharUnicodeInfo.GetDecimalDigitValue(c);
            sb.Append(digit >= 0 ? (char)('0' + digit) : c);
        }

        return sb.ToString();
    }
}
