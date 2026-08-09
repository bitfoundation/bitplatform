using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TextField;

public partial class BitTextFieldDemo : IDisposable
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the text field used when focused.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the input for the benefit of screen readers. It is rendered into a visually hidden element that the input references through its aria-describedby attribute, which is what lets a field carry an instruction that would be too long to show next to it.",
        },
        new()
        {
            Name = "AutoCapitalize",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the autocapitalize html attribute of the input element, which tells the on-screen keyboard of a mobile device whether and how the typed text should be capitalized automatically. Accepted values are \"off\", \"none\", \"on\", \"sentences\", \"words\" and \"characters\".",
        },
        new()
        {
            Name = "AutoCorrect",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Sets the autocorrect html attribute of the input element, which turns the automatic correction of the typed text on or off. Useful to turn off for identifiers, codes and other non-prose values.",
        },
        new()
        {
            Name = "AutoHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Automatically adjust the height of the input in Multiline mode.",
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the text field background.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the text field border.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "CanRevealPassword",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the reveal password button for input type 'password'.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitTextFieldClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTextField.",
            LinkType = LinkType.Link,
            Href = "#textfield-class-styles",
        },
        new()
        {
            Name = "ClearButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the clear button, which is what a screen reader announces for it since the button only holds an icon. Defaults to \"Clear text\".",
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the clear button. Takes precedence over ClearButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "Gets or sets the name of the icon to display on the clear button from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "ClearButtonTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the clear button, which replaces its icon.",
        },
        new()
        {
            Name = "CountStrategy",
            Type = "Func<string?, int>?",
            DefaultValue = "null",
            Description = "Decides how the characters of the value are counted for the counter rendered by ShowCount. Leaving it unset counts the value the way the browser counts it against MaxLength - in UTF-16 code units - which makes an emoji count as two and a flag as four. A strategy of its own counts them the way the rest of the app does instead, for instance v => new StringInfo(v ?? string.Empty).LengthInTextElements to count what a reader actually sees. It only changes the number that is shown: the html maxlength attribute keeps holding the keyboard back at its own count.",
        },
        new()
        {
            Name = "CountTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "The custom content of the character counter, which receives the current number of characters and replaces the default \"count/maxLength\" text.",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "Description displayed below the text field to provide additional details about what text to enter.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom description for text field.",
        },
        new()
        {
            Name = "EnterKeyHint",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the enterkeyhint html attribute of the input element, which decides the label of the return key of an on-screen keyboard. Accepted values are \"enter\", \"done\", \"go\", \"next\", \"previous\", \"search\" and \"send\".",
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message shown under the field when the value was rejected. Setting it marks the field invalid on its own - the same look and the same aria-invalid attribute Invalid gives it - and the message is referenced by the input through its aria-describedby attribute and announced by the live region of the field.",
        },
        new()
        {
            Name = "ErrorMessageTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the error message, which replaces the plain ErrorMessage text and marks the field invalid in the same way. Only the plain text is announced by the live region, since a template is free to render anything at all.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Forces the text field fill 100% of its container width.",
        },
        new()
        {
            Name = "GhostText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ghost/suggestion text displayed inline after the current cursor position. Update this value from outside (e.g. from an AI or autocomplete suggestion) to show a faded inline suggestion. The user can accept it by pressing Tab or Enter, or clicking/touching the ghost text.",
        },
        new()
        {
            Name = "HidePasswordIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the reveal password button when password is shown using custom CSS classes for external icon libraries.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "HidePasswordIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the reveal password button when password is shown from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the icon shown at the trailing end of the text field. The icon is decorative and hidden from assistive technologies by default; setting this turns it into an image with a name, which is what an icon carrying a meaning of its own needs.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the icon shown in the far right end of the text field from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the input element.",
            LinkType = LinkType.Link,
            Href = "#input-mode",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the value of the field as invalid, which gives a value rejected by something other than the cascading EditContext - a server, a rule of the app, a validator of its own - the same look and the same aria-invalid attribute that a failing data annotation gives it. A field failing its own validation stays invalid regardless of this parameter.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label displayed above the text field and read by screen readers.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "Where the label sits relative to the input. Leaving it unset keeps the layout each variant comes with: above the input in the default one, and next to it in the Underlined one.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom label for text field.",
        },
        new()
        {
            Name = "Loading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a busy indicator inside the field, which is what tells the user that something is running against what was typed - a suggestion being fetched, a value being checked against a server. The field stays editable while it is on, so the typing is never interrupted by it.",
        },
        new()
        {
            Name = "LoadingAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "What a screen reader announces while Loading is on, in place of the default \"Loading\". It is announced whichever indicator is drawn, so a LoadingTemplate drawing a bare spinner of its own still tells an assistive technology that something is running.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the busy indicator, which replaces the default spinner.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Specifies the maximum number of characters allowed in the input. A negative value (the default) removes the limit and renders no maxlength attribute.",
        },
        new()
        {
            Name = "MaxRows",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of rows the input grows to in the Multiline mode while AutoHeight is enabled. Beyond that height the input keeps its size and scrolls its content instead of pushing the rest of the page down. A null value (the default) lets the input grow indefinitely.",
        },
        new()
        {
            Name = "MinLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Specifies the minimum number of characters the input accepts, which is what the browser validates the value against before the form is submitted. A negative value (the default) removes the constraint and renders no minlength attribute.",
        },
        new()
        {
            Name = "Multiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is a Multiline text field.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border of the text input.",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the input loses focus. Unlike OnFocusOut it does not bubble, so it is the one to use when only the input itself losing focus is of interest.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback executed when the user clears the text field by clicking the clear button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the input clicked.",
        },
        new()
        {
            Name = "OnEnter",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when the Enter key is pressed while input has focus.",
        },
        new()
        {
            Name = "OnEscape",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when the Escape key is pressed while input has focus.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the input receives focus. Unlike OnFocusIn it does not bubble, so it is the one to use when only the input itself receiving focus is of interest.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input or any of its descendants, since unlike OnFocus it bubbles.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves out of the input or any of its descendants, since unlike OnBlur it bubbles.",
        },
        new()
        {
            Name = "OnGhostTextAccepted",
            Type = "EventCallback<string?>",
            Description = "Callback invoked when the ghost text is accepted via Tab or Enter key, or click/touch. The accepted ghost text string is passed as the argument.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<ChangeEventArgs>",
            Description = "Callback for every input event of the input element, which is what lets a field watch the text as it is typed without having to turn Immediate on and commit the value along with it. It is raised before the value is committed and is not held back by DebounceTime or ThrottleTime, and it is not raised for the half-composed text of an input method editor.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a keyboard key is pressed.",
        },
        new()
        {
            Name = "OnKeyUp",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for When a keyboard key is released.",
        },
        new()
        {
            Name = "Pattern",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the pattern html attribute of the input element, which is the regular expression the value is checked against by the browser before the form is submitted.",
        },
        new()
        {
            Name = "PermanentGhost",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables permanent ghost mode that forces the scrollbar-gutter to always be present, preventing layout shift of the ghost text rendering.",
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
            Description = "Prefix displayed before the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom prefix for text field.",
        },
        new()
        {
            Name = "PreventEnter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the enter to add new line character into the input in the Multiline mode.",
        },
        new()
        {
            Name = "Resizable",
            Type = "bool",
            DefaultValue = "false",
            Description = "For multiline text fields, whether or not the field is resizable.",
        },
        new()
        {
            Name = "RevealPasswordAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Aria label for the reveal password button. It stays the same in both states on purpose: the pressed state of the button is what tells a screen reader whether the password is currently revealed. Defaults to \"Reveal password\".",
        },
        new()
        {
            Name = "RevealPasswordIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the reveal password button when password is hidden using custom CSS classes for external icon libraries.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "RevealPasswordIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the reveal password button when password is hidden from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "RevealPasswordTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "The custom content of the reveal password button, which receives whether the password is currently revealed and replaces the default icon.",
        },
        new()
        {
            Name = "Rows",
            Type = "int?",
            DefaultValue = "null",
            Description = "For multiline text, Number of rows.",
        },
        new()
        {
            Name = "SelectOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Selects the whole value when the input receives focus, so that the next keystroke replaces it. It is what a field holding a value the user is expected to overwrite rather than edit - a search term, a quantity, a generated code - usually wants.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the clear button while the input holds any text. The button follows what the input reports rather than the bound value, so it is there from the first keystroke even on a field that only commits its value when it loses focus.",
        },
        new()
        {
            Name = "ShowCount",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the number of characters that were typed under the text field, followed by the MaxLength when one is set. A count above the limit - only reachable from the code, since the maxlength attribute holds the keyboard back - is colored like a rejected value.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the text field.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "SpellCheck",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Sets the spellcheck html attribute of the input element, which turns the spell checking of the browser on or off for this input.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTextFieldClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#textfield-class-styles",
            Description = "Custom CSS styles for different parts of the BitTextField.",
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix displayed after the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom suffix for text field.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "A more descriptive title of the text field, shown by the browser as its tooltip.",
        },
        new()
        {
            Name = "Trim",
            Type = "bool",
            DefaultValue = "false",
            Description = "Specifies whether to remove any leading or trailing whitespace from the value. The trimming happens when the input reports a change, which is what lets a space still be typed in the middle of a word while Immediate is enabled.",
        },
        new()
        {
            Name = "Type",
            Type = "BitInputType?",
            DefaultValue = "null",
            Description = "Input type.",
            LinkType = LinkType.Link,
            Href = "#input-type-enum"
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is underlined.",
        },
        new()
        {
            Name = "Wrap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the wrap html attribute of the textarea rendered in the Multiline mode, which decides how the text is wrapped and whether the breaks the wrapping adds travel with the value when a form is submitted. Accepted values are \"soft\" (what a browser does on its own), \"hard\" and \"off\", the last of which turns the wrapping off entirely and scrolls long lines sideways instead.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "textfield-class-styles",
            Title = "BitTextFieldClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's root element."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles of the root element in focus state."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of label and input in the BitTextField."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's label."
                },
                new()
                {
                    Name = "FieldGroup",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's field group."
                },
                new()
                {
                    Name = "PrefixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's prefix container."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's prefix."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's input."
                },
                new()
                {
                    Name = "Loading",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's busy indicator container."
                },
                new()
                {
                    Name = "RevealPassword",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's reveal password."
                },
                new()
                {
                    Name = "RevealPasswordIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's reveal password icon container."
                },
                new()
                {
                    Name = "RevealPasswordIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's reveal password icon."
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's clear button."
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's clear button icon."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's icon."
                },
                new()
                {
                    Name = "SuffixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's suffix container."
                },
                new()
                {
                    Name = "Suffix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's suffix."
                },
                new()
                {
                    Name = "ErrorMessageContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's error message container."
                },
                new()
                {
                    Name = "ErrorMessage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's error message."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's footer, which holds the description and the character counter."
                },
                new()
                {
                    Name = "DescriptionContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's description container."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's description."
                },
                new()
                {
                    Name = "Count",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's character counter."
                },
                new()
                {
                    Name = "GhostTextWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's ghost text wrapper element."
                },
                new()
                {
                    Name = "GhostTextOverlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's ghost text overlay container."
                }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
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
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [

        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "The primary color kind.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "The secondary color kind.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "The tertiary color kind.",
                    Value = "2",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "The transparent color kind.",
                    Value = "3",
                },
            ]
        },
        new()
        {
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "Defines the positions a label can take relative to the control it belongs to.",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Description = "The label sits above the input.",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Description = "The label sits after the input, on the same line.",
                    Value = "1",
                },
                new()
                {
                    Name = "Bottom",
                    Description = "The label sits under the input.",
                    Value = "2",
                },
                new()
                {
                    Name = "Start",
                    Description = "The label sits before the input, on the same line.",
                    Value = "3",
                },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "The small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "The medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "input-type-enum",
            Name = "BitInputType",
            Description = "",
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
                },
                new()
                {
                    Name= "Search",
                    Description="The input expects a search term, which is what lets a browser offer the previous searches of the same field and show its own clear affordance.",
                    Value="6",
                }
            ]
        },
        new()
        {
            Id = "input-mode",
            Name = "BitInputMode",
            Description = "This allows a browser to display an appropriate virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="The input expects text characters.",
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
                    Description="A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key",
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
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitTextField.",
        },
        new()
        {
            Name = "ClearAsync",
            Type = "Task",
            Description = "Empties the text field and raises OnClear, exactly as the clear button does (without requiring ShowClearButton, since there is no button involved). It does nothing while the field is disabled or read-only.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitTextField. The overload taking a preventScroll flag focuses it without scrolling the document to bring it into view.",
        },
        new()
        {
            Name = "SelectAsync",
            Type = "ValueTask",
            Description = "Selects the whole value of the input, the programmatic counterpart of SelectOnFocus.",
        },
        new()
        {
            Name = "SelectRangeAsync",
            Type = "ValueTask",
            Description = "Selects the text between the two given positions, or moves the caret when they are equal. A null start counts from the beginning of the value and a null end runs to its very end, and both are clamped to the length of the value.",
        },
        new()
        {
            Name = "ToggleRevealPassword",
            Type = "void",
            Description = "Toggles the revealed state of the value while the type of the input is password and CanRevealPassword is enabled.",
        }
    ];



    private string? oneWayValue;
    private string? twoWayValue;
    private string? onChangeValue;
    private string? immediateValue;
    private string? debounceValue;
    private string? throttleValue;
    private string? defaultValueChanged;

    private string? countValue;

    private static int CountTextElements(string? value) => new StringInfo(value ?? string.Empty).LengthInTextElements;

    private string? trimmedValue;
    private string? notTrimmedValue;

    private string? classesValue;

    private int eventCount;
    private int clearCount;
    private string? eventLog;
    private string? lastKey;
    private string? lastKeyUp;
    private string? focusLog;
    private string? clearLog;

    private void HandleOnEnter() => eventLog = $"OnEnter ({++eventCount})";

    private void HandleOnEscape() => eventLog = $"OnEscape ({++eventCount})";

    private void HandleOnKeyDown(KeyboardEventArgs e) => lastKey = e.Key;

    private void HandleOnKeyUp(KeyboardEventArgs e) => lastKeyUp = e.Key;

    private void HandleOnClick() => eventLog = $"OnClick ({++eventCount})";

    private void HandleOnFocus() => focusLog = "focused";

    private void HandleOnBlur() => focusLog = "blurred";

    private void HandleOnFocusIn() => focusLog = "focused in";

    private void HandleOnFocusOut() => focusLog = "focused out";

    private void HandleOnClear() => clearLog = $"cleared ({++clearCount})";

    private string? onInputText;
    private string? onInputCommittedValue;

    private void HandleOnInput(ChangeEventArgs e) => onInputText = e.Value?.ToString();


    private BitTextField? passwordRef;

    private BitTextField? clearRef;
    private string? clearApiValue = "Clear me from the button below";

    private BitTextField? selectionRef;


    private string? userName;
    private bool userNameLoading;
    private string? userNameStatus;
    private CancellationTokenSource? _userNameCts;

    private async Task CheckUserNameAsync(string? value)
    {
        CancelAndDispose(ref _userNameCts);

        if (string.IsNullOrWhiteSpace(value))
        {
            userNameLoading = false;
            userNameStatus = null;
            return;
        }

        var cts = new CancellationTokenSource();
        _userNameCts = cts;

        userNameLoading = true;
        userNameStatus = null;

        try
        {
            await Task.Delay(1500, cts.Token);

            userNameStatus = value.Length < 5
                ? $"'{value}' is already taken."
                : $"'{value}' is available.";
        }
        catch (OperationCanceledException)
        {
            // A newer keystroke started another check and canceled this one.
            return;
        }
        finally
        {
            if (cts.IsCancellationRequested is false)
            {
                userNameLoading = false;
            }
        }
    }

    private string? invalidValue = "no-space-here";

    private string? errorMessageValue = "not-an-email";

    private string? EmailError => errorMessageValue.HasValue() && errorMessageValue!.Contains('@') is false
                                    ? $"'{errorMessageValue}' is not an email address."
                                    : null;

    private ValidationTextFieldModel validationTextFieldModel = new();
    public bool formIsValidSubmit;

    private async Task HandleValidSubmit()
    {
        formIsValidSubmit = true;

        await Task.Delay(2000);

        validationTextFieldModel = new();

        formIsValidSubmit = false;

        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        formIsValidSubmit = false;
    }



    private string? ghostBasicTextValue;
    private string? ghostBasicSuggestion;

    private string? ghostBasicMultilineValue;
    private string? ghostBasicMultilineSuggestion;

    private string? ghostTextValue;
    private string? ghostSuggestion;

    private string? ghostMultilineValue;
    private string? ghostMultilineSuggestion;

    private CancellationTokenSource? _ghostSuggestionCts;
    private CancellationTokenSource? _ghostMultilineSuggestionCts;

    private static readonly string[] _suggestions =
    [
        "application form",
        "banana smoothie",
        "car repair manual",
        "dog training guide"
    ];

    private async Task SetGhostSuggestionAsync(string? value, bool isMultiline)
    {
        var cts = new CancellationTokenSource();

        if (isMultiline)
        {
            CancelAndDispose(ref _ghostMultilineSuggestionCts);
            _ghostMultilineSuggestionCts = cts;
            ghostMultilineSuggestion = null;
        }
        else
        {
            CancelAndDispose(ref _ghostSuggestionCts);
            _ghostSuggestionCts = cts;
            ghostSuggestion = null;
        }

        try
        {
            var suggestion = await GetGhostSuggestionAsync(value, cts.Token);

            if (cts.IsCancellationRequested) return;

            if (isMultiline)
            {
                ghostMultilineSuggestion = suggestion;
            }
            else
            {
                ghostSuggestion = suggestion;
            }
        }
        catch (OperationCanceledException)
        {
            // A newer request started and canceled this one.
        }
    }

    private void ClearGhostSuggestion(bool isMultiline)
    {
        if (isMultiline)
        {
            CancelAndDispose(ref _ghostMultilineSuggestionCts);
            ghostMultilineSuggestion = null;
        }
        else
        {
            CancelAndDispose(ref _ghostSuggestionCts);
            ghostSuggestion = null;
        }
    }

    private static void CancelAndDispose(ref CancellationTokenSource? cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private static string? GetGhostSuggestion(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        if (char.IsWhiteSpace(value[^1])) return null;

        var lastWord = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        if (string.IsNullOrEmpty(lastWord)) return null;

        var match = _suggestions.FirstOrDefault(s => s.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase));
        return match?[lastWord.Length..];
    }

    private static async Task<string?> GetGhostSuggestionAsync(string? value, CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken);

        return GetGhostSuggestion(value);
    }

    public void Dispose()
    {
        CancelAndDispose(ref _ghostSuggestionCts);
        CancelAndDispose(ref _ghostMultilineSuggestionCts);
        CancelAndDispose(ref _userNameCts);
    }



    private readonly string example1RazorCode = @"
<BitTextField Label=""Basic"" />
<BitTextField Label=""Placeholder"" Placeholder=""Enter a text..."" />
<BitTextField Label=""Disabled"" IsEnabled=""false"" />
<BitTextField Label=""ReadOnly"" ReadOnly DefaultValue=""This is ReadOnly"" />
<BitTextField Label=""Description"" Description=""This is Description"" />
<BitTextField Label=""Required"" Required />
<BitTextField Label=""MaxLength: 5"" MaxLength=""5"" />
<BitTextField Label=""MinLength: 3"" MinLength=""3"" Description=""The browser rejects a shorter value on submit."" />
<BitTextField Label=""Auto focused"" AutoFocus />
<BitTextField Label=""FullWidth"" FullWidth Placeholder=""Fills the width of its container..."" />";

    private readonly string example2RazorCode = @"
<BitTextField Label=""Basic"" Underlined />
<BitTextField Label=""Placeholder"" Underlined Placeholder=""Enter a text..."" />
<BitTextField Label=""Disabled"" Underlined IsEnabled=""false"" />
<BitTextField Label=""Required"" Underlined Required />";

    private readonly string example3RazorCode = @"
<BitTextField Label=""Basic"" Placeholder=""Enter a text..."" NoBorder />
<BitTextField Label=""Disabled"" Placeholder=""Enter a text..."" NoBorder IsEnabled=""false"" />
<BitTextField Label=""Required"" Placeholder=""Enter a text..."" NoBorder Required />";

    private readonly string example4RazorCode = @"
<BitTextField Label=""Multiline"" Multiline />
<BitTextField Label=""Resizable"" Multiline Resizable />
<BitTextField Label=""Rows = 10"" Multiline Rows=""10"" />
<BitTextField Label=""AutoHeight"" Multiline AutoHeight />

<BitTextField Label=""AutoHeight with Rows = 4 (never shrinks below it)"" Multiline AutoHeight Rows=""4""
              Placeholder=""Empty the field: it stays four rows tall..."" />

<BitTextField Label=""AutoHeight with MaxRows = 5"" Multiline AutoHeight MaxRows=""5""
              Placeholder=""Type more than 5 lines to see it stop growing..."" />

<BitTextField Label=""PreventEnter (use Shift+Enter for new-line)"" Multiline AutoHeight PreventEnter />

<BitTextField Label=""Wrap = off (long lines scroll sideways)"" Multiline Rows=""3"" Wrap=""off""
              DefaultValue=""A single very long line that is never wrapped, so the field scrolls sideways instead of breaking it apart."" />";

    private readonly string example5RazorCode = @"
<BitTextField Label=""Text"" Type=""BitInputType.Text"" />
<BitTextField Label=""Number"" Type=""BitInputType.Number"" />
<BitTextField Label=""Email"" Type=""BitInputType.Email"" />
<BitTextField Label=""Tel"" Type=""BitInputType.Tel"" />
<BitTextField Label=""Url"" Type=""BitInputType.Url"" />
<BitTextField Label=""Search"" Type=""BitInputType.Search"" />

<BitTextField Label=""InputMode = Numeric"" InputMode=""BitInputMode.Numeric""
              Description=""A text input that opens the numeric keypad on a phone."" />

<BitTextField Label=""EnterKeyHint = search"" EnterKeyHint=""search""
              Description=""Relabels the return key of the on-screen keyboard."" />

<BitTextField Label=""Pattern"" Pattern=""[A-Za-z]{3}""
              Description=""The browser rejects anything but 3 letters on submit."" />

<BitTextField Label=""No spellcheck, no autocorrect, no autocapitalize""
              SpellCheck=""false""
              AutoCorrect=""false""
              AutoCapitalize=""none""
              Description=""What a coupon code or a username field wants."" />";

    private readonly string example6RazorCode = @"
<BitTextField Label=""Email"" IconName=""@BitIconName.EditMail"" />
<BitTextField Label=""Calendar"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example7RazorCode = @"
<BitTextField Label=""Prefix"" Prefix=""https://"" />
<BitTextField Label=""Suffix"" Suffix="".com"" />
<BitTextField Label=""Prefix and Suffix"" Prefix=""https://"" Suffix="".com"" />
<BitTextField Label=""Disabled"" Prefix=""https://"" Suffix="".com"" IsEnabled=""false"" />";

    private readonly string example8RazorCode = @"
<BitTextField>
    <LabelTemplate>
        <BitLabel Style=""color:coral"">Custom Label</BitLabel>
    </LabelTemplate>
</BitTextField>

<BitTextField Required>
    <LabelTemplate>
        <BitLabel Style=""color:coral"">Custom Label of a required field</BitLabel>
    </LabelTemplate>
</BitTextField>

<BitTextField Label=""Custom Description"">
    <DescriptionTemplate>
        <BitLabel Style=""color:coral"">Description</BitLabel>
    </DescriptionTemplate>
</BitTextField>

<BitTextField Label=""Custom Prefix"">
    <PrefixTemplate>
        <BitLabel Style=""color:coral;margin:0 5px"">Prefix</BitLabel>
    </PrefixTemplate>
</BitTextField>

<BitTextField Label=""Custom Suffix"">
    <SuffixTemplate>
        <BitLabel Style=""color:coral;margin:0 5px"">Suffix</BitLabel>
    </SuffixTemplate>
</BitTextField>

<BitTextField Label=""Custom clear button"" ShowClearButton DefaultValue=""Clear me"">
    <ClearButtonTemplate>
        <span style=""color:coral"">✕</span>
    </ClearButtonTemplate>
</BitTextField>

<BitTextField Label=""Custom reveal password button"" Type=""BitInputType.Password"" CanRevealPassword DefaultValue=""p@ssw0rd"">
    <RevealPasswordTemplate Context=""isRevealed"">
        <span style=""color:coral;font-size:0.75rem"">@(isRevealed ? ""hide"" : ""show"")</span>
    </RevealPasswordTemplate>
</BitTextField>

<BitTextField Label=""Custom counter"" ShowCount MaxLength=""20"" Immediate>
    <CountTemplate Context=""count"">
        <span style=""color:coral"">@count of 20 characters</span>
    </CountTemplate>
</BitTextField>

<BitTextField Label=""Custom error message"" DefaultValue=""admin"">
    <ErrorMessageTemplate>
        <span>✖ This name is reserved. <a href=""#example20"">See the Validation section</a>.</span>
    </ErrorMessageTemplate>
</BitTextField>";

    private readonly string example9RazorCode = @"
<BitTextField Label=""Password"" Type=""BitInputType.Password"" />

<BitTextField Label=""Reveal Password"" Type=""BitInputType.Password"" CanRevealPassword />

<BitTextField Label=""Offered to the password manager""
              Type=""BitInputType.Password""
              CanRevealPassword
              AutoComplete=""current-password""
              Description=""AutoComplete tells the browser which credential belongs here."" />

<BitTextField Label=""Custom icons and aria-label""
              Type=""BitInputType.Password""
              CanRevealPassword
              RevealPasswordAriaLabel=""Show the password""
              RevealPasswordIconName=""@BitIconName.RedEye""
              HidePasswordIconName=""@BitIconName.Hide"" />

<BitTextField @ref=""passwordRef""
              Label=""Toggled from the outside""
              Type=""BitInputType.Password""
              CanRevealPassword
              DefaultValue=""p@ssw0rd"" />
<BitButton OnClick=""() => passwordRef?.ToggleRevealPassword()"">ToggleRevealPassword</BitButton>";
    private readonly string example9CsharpCode = @"
private BitTextField? passwordRef;";

    private readonly string example10RazorCode = @"
<BitTextField Label=""Email"" DefaultValue=""example@email.com"" ShowClearButton />

<BitTextField Label=""Custom icon and aria-label""
              ShowClearButton
              DefaultValue=""Clear me""
              ClearButtonAriaLabel=""Empty this field""
              ClearButtonIconName=""@BitIconName.ChromeClose"" />

<BitTextField Label=""Search (the native clear affordance is hidden)""
              Type=""BitInputType.Search""
              ShowClearButton
              DefaultValue=""bit BlazorUI"" />

<BitTextField Label=""Multiline"" Multiline Rows=""3"" ShowClearButton
              DefaultValue=""A multiline value that can be cleared."" />

<BitTextField Label=""ReadOnly (the button is disabled)"" ReadOnly ShowClearButton
              DefaultValue=""Read only value"" />

<BitTextField @ref=""clearRef"" Label=""Cleared from the outside"" @bind-Value=""clearApiValue"" />
<BitButton OnClick=""() => clearRef?.ClearAsync()"">ClearAsync</BitButton>
<div>Value: [@clearApiValue]</div>";
    private readonly string example10CsharpCode = @"
private BitTextField? clearRef;
private string? clearApiValue = ""Clear me from the button below"";";

    private readonly string example11RazorCode = @"
<BitTextField Label=""With a limit"" ShowCount MaxLength=""30"" Placeholder=""Up to 30 characters..."" />

<BitTextField Label=""Without a limit"" ShowCount Placeholder=""Just counts what you type..."" />

<BitTextField Label=""With a description"" ShowCount MaxLength=""30""
              Description=""Both the description and the counter share the footer."" />

<BitTextField Label=""Over the limit (assigned from the code)""
              ShowCount
              MaxLength=""10""
              DefaultValue=""A value longer than the limit of the field"" />

<BitTextField Label=""Multiline"" Multiline Rows=""4"" ShowCount MaxLength=""140"" @bind-Value=""countValue"" />
<div>Value: [@countValue]</div>

<BitTextField Label=""Counted in UTF-16 code units (the default)""
              ShowCount
              Immediate
              MaxLength=""20""
              DefaultValue=""👍🏽 hi"" />

<BitTextField Label=""Counted in text elements (CountStrategy)""
              ShowCount
              Immediate
              MaxLength=""20""
              DefaultValue=""👍🏽 hi""
              CountStrategy=""CountTextElements""
              Description=""The same value, counted the way it reads."" />";
    private readonly string example11CsharpCode = @"
private string? countValue;

private static int CountTextElements(string? value) => new StringInfo(value ?? string.Empty).LengthInTextElements;";

    private readonly string example12RazorCode = @"
<BitTextField Label=""One-way"" Value=""@oneWayValue"" />
<div>Value: [@oneWayValue]</div>
<BitOtpInput Length=""5"" Style=""margin-top: 5px;"" @bind-Value=""oneWayValue"" />

<BitTextField Label=""Two-way"" @bind-Value=""twoWayValue"" />
<div>Value: [@twoWayValue]</div>
<BitOtpInput Length=""5"" Style=""margin-top: 5px;"" @bind-Value=""twoWayValue"" />

<BitTextField Label=""OnChange"" OnChange=""(v) => onChangeValue = v"" />
<BitLabel>Value: [@onChangeValue]</BitLabel>

<BitTextField Label=""DefaultValue (uncontrolled)"" DefaultValue=""Initial value""
              OnChange=""(v) => defaultValueChanged = v"" />
<div>Value: [@defaultValueChanged]</div>

<BitTextField Label=""Immediate"" @bind-Value=""@immediateValue"" Immediate />
<div>Value: [@immediateValue]</div>

<BitTextField Label=""Debounce"" @bind-Value=""@debounceValue"" Immediate DebounceTime=""300"" />
<div>Value: [@debounceValue]</div>

<BitTextField Label=""Throttle"" @bind-Value=""@throttleValue"" Immediate ThrottleTime=""300"" />
<div>Value: [@throttleValue]</div>";
    private readonly string example12CsharpCode = @"
private string? oneWayValue;
private string? twoWayValue;
private string? onChangeValue;
private string? defaultValueChanged;
private string? immediateValue;
private string? debounceValue;
private string? throttleValue;";

    private readonly string example13RazorCode = @"
<BitTextField @bind-Value=""ghostBasicTextValue""
              Immediate
              Label=""Basic Single-line""
              GhostText=""@ghostBasicSuggestion""
              Placeholder=""Type 'app', 'ban', 'car', or 'dog'...""
              OnGhostTextAccepted=""(_ => ghostBasicSuggestion = null)""
              OnChange=""(v => ghostBasicSuggestion = GetGhostSuggestion(v))"" />
<div>Value: [@ghostBasicTextValue]</div>

<BitTextField @bind-Value=""ghostBasicMultilineValue""
              Multiline
              Resizable
              Rows=""5""
              Immediate
              PermanentGhost
              Label=""Basic Multiline""
              GhostText=""@ghostBasicMultilineSuggestion""
              Placeholder=""Type 'app', 'ban', 'car', or 'dog'...""
              OnGhostTextAccepted=""(_ => ghostBasicMultilineSuggestion = null)""
              OnChange=""(v => ghostBasicMultilineSuggestion = GetGhostSuggestion(v))"" />
<div>Value: [@ghostBasicMultilineValue]</div>


<BitTextField @bind-Value=""ghostTextValue""
              Immediate
              Label=""Single-line""
              GhostText=""@ghostSuggestion""
              Placeholder=""Type 'app', 'ban', 'car', or 'dog'...""
              OnGhostTextAccepted=""(_ => ClearGhostSuggestion(isMultiline: false))""
              OnChange=""(v => SetGhostSuggestionAsync(v, isMultiline: false))"" />
<div>Value: [@ghostTextValue]</div>

<BitTextField @bind-Value=""ghostMultilineValue""
              Multiline
              Resizable
              Rows=""5""
              Immediate
              PermanentGhost
              Label=""Multiline""
              GhostText=""@ghostMultilineSuggestion""
              Placeholder=""Type 'app', 'ban', 'car', or 'dog'...""
              OnGhostTextAccepted=""(_ => ClearGhostSuggestion(isMultiline: true))""
              OnChange=""(v => SetGhostSuggestionAsync(v, isMultiline: true))"" />
<div>Value: [@ghostMultilineValue]</div>";
    private readonly string example13CsharpCode = @"
private string? ghostBasicTextValue;
private string? ghostBasicSuggestion;

private string? ghostBasicMultilineValue;
private string? ghostBasicMultilineSuggestion;

private string? ghostTextValue;
private string? ghostSuggestion;

private string? ghostMultilineValue;
private string? ghostMultilineSuggestion;

private CancellationTokenSource? _ghostSuggestionCts;
private CancellationTokenSource? _ghostMultilineSuggestionCts;

private static readonly string[] _suggestions =
[
    ""application form"",
    ""banana smoothie"",
    ""car repair manual"",
    ""dog training guide""
];

private static string? GetGhostSuggestion(string? value)
{
    if (string.IsNullOrEmpty(value)) return null;

    if (char.IsWhiteSpace(value[^1])) return null;

    var lastWord = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

    if (string.IsNullOrEmpty(lastWord)) return null;

    var match = _suggestions.FirstOrDefault(s => s.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase));
    return match?[lastWord.Length..];
}

private async Task SetGhostSuggestionAsync(string? value, bool isMultiline)
{
    var cts = new CancellationTokenSource();

    if (isMultiline)
    {
        CancelAndDispose(ref _ghostMultilineSuggestionCts);
        _ghostMultilineSuggestionCts = cts;
        ghostMultilineSuggestion = null;
    }
    else
    {
        CancelAndDispose(ref _ghostSuggestionCts);
        _ghostSuggestionCts = cts;
        ghostSuggestion = null;
    }

    try
    {
        var suggestion = await GetGhostSuggestionAsync(value, cts.Token);

        if (cts.IsCancellationRequested) return;

        if (isMultiline)
        {
            ghostMultilineSuggestion = suggestion;
        }
        else
        {
            ghostSuggestion = suggestion;
        }
    }
    catch (OperationCanceledException)
    {
    }
}

private void ClearGhostSuggestion(bool isMultiline)
{
    if (isMultiline)
    {
        CancelAndDispose(ref _ghostMultilineSuggestionCts);
        ghostMultilineSuggestion = null;
    }
    else
    {
        CancelAndDispose(ref _ghostSuggestionCts);
        ghostSuggestion = null;
    }
}

private static void CancelAndDispose(ref CancellationTokenSource? cts)
{
    cts?.Cancel();
    cts?.Dispose();
    cts = null;
}

private static async Task<string?> GetGhostSuggestionAsync(string? value, CancellationToken cancellationToken)
{
    await Task.Delay(300, cancellationToken);

    return GetGhostSuggestion(value);
}";

    private readonly string example14RazorCode = @"
<BitTextField Label=""Trimmed"" Trim @bind-Value=""trimmedValue"" />
<pre>[@trimmedValue]</pre>

<BitTextField Label=""Not Trimmed"" @bind-Value=""notTrimmedValue"" />
<pre>[@notTrimmedValue]</pre>";
    private readonly string example14CsharpCode = @"
private string? trimmedValue;
private string? notTrimmedValue;";

    private readonly string example15RazorCode = @"
<BitTextField Label=""Type here and press Enter or Escape""
              OnEnter=""HandleOnEnter""
              OnEscape=""HandleOnEscape""
              OnKeyDown=""HandleOnKeyDown""
              OnKeyUp=""HandleOnKeyUp""
              OnClick=""HandleOnClick""
              OnFocus=""HandleOnFocus""
              OnBlur=""HandleOnBlur""
              OnFocusIn=""HandleOnFocusIn""
              OnFocusOut=""HandleOnFocusOut"" />

<div>Last event: [@eventLog]</div>
<div>Last key down: [@lastKey]</div>
<div>Last key up: [@lastKeyUp]</div>
<div>Focus: [@focusLog]</div>

<BitTextField Label=""OnInput (the value is still committed on blur)""
              OnInput=""HandleOnInput""
              @bind-Value=""onInputCommittedValue"" />
<div>Typed so far: [@onInputText]</div>
<div>Committed value: [@onInputCommittedValue]</div>

<BitTextField Label=""Clearable"" ShowClearButton DefaultValue=""Clear me"" OnClear=""HandleOnClear"" />
<div>OnClear: [@clearLog]</div>";
    private readonly string example15CsharpCode = @"
private int eventCount;
private int clearCount;
private string? eventLog;
private string? lastKey;
private string? lastKeyUp;
private string? focusLog;
private string? clearLog;

private void HandleOnEnter() => eventLog = $""OnEnter ({++eventCount})"";

private void HandleOnEscape() => eventLog = $""OnEscape ({++eventCount})"";

private void HandleOnKeyDown(KeyboardEventArgs e) => lastKey = e.Key;

private void HandleOnKeyUp(KeyboardEventArgs e) => lastKeyUp = e.Key;

private void HandleOnClick() => eventLog = $""OnClick ({++eventCount})"";

private void HandleOnFocus() => focusLog = ""focused"";

private void HandleOnBlur() => focusLog = ""blurred"";

private void HandleOnFocusIn() => focusLog = ""focused in"";

private void HandleOnFocusOut() => focusLog = ""focused out"";

private void HandleOnClear() => clearLog = $""cleared ({++clearCount})"";

private string? onInputText;
private string? onInputCommittedValue;

private void HandleOnInput(ChangeEventArgs e) => onInputText = e.Value?.ToString();";

    private readonly string example16RazorCode = @"
<BitTextField Label=""SelectOnFocus"" SelectOnFocus DefaultValue=""Focus me and start typing"" />

<BitTextField Label=""ReadOnly + SelectOnFocus"" SelectOnFocus ReadOnly DefaultValue=""AB12-CD34-EF56"" />

<BitTextField @ref=""selectionRef"" Label=""Driven from code"" DefaultValue=""bit BlazorUI components"" />

<BitStack>
    <BitButton OnClick=""async () => { if (selectionRef is not null) await selectionRef.SelectAsync(); }"">SelectAsync</BitButton>
    <BitButton OnClick=""async () => { if (selectionRef is not null) await selectionRef.SelectRangeAsync(4, 12); }"">SelectRangeAsync(4, 12)</BitButton>
    <BitButton OnClick=""async () => { if (selectionRef is not null) await selectionRef.SelectRangeAsync(0, 0); }"">Caret to the start</BitButton>
</BitStack>";
    private readonly string example16CsharpCode = @"
private BitTextField? selectionRef;";

    private readonly string example17RazorCode = @"
<BitTextField Label=""Top"" LabelPosition=""BitLabelPosition.Top"" Placeholder=""Enter a text..."" />
<BitTextField Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" Placeholder=""Enter a text..."" />
<BitTextField Label=""Start"" LabelPosition=""BitLabelPosition.Start"" Placeholder=""Enter a text..."" />
<BitTextField Label=""End"" LabelPosition=""BitLabelPosition.End"" Placeholder=""Enter a text..."" />

<BitTextField Label=""Start + Required + a counter""
              Required
              ShowCount
              MaxLength=""20""
              LabelPosition=""BitLabelPosition.Start""
              Description=""The footer keeps its own line under the whole row."" />";

    private readonly string example18RazorCode = @"
<BitTextField Label=""Loading"" Loading DefaultValue=""Checking..."" />

<BitTextField Label=""Loading with a clear button and an icon""
              Loading
              ShowClearButton
              DefaultValue=""bit""
              IconName=""@BitIconName.Search"" />

<BitTextField Label=""Custom indicator"" Loading LoadingAriaLabel=""Checking the availability"">
    <LoadingTemplate>
        <BitEllipsisLoading CustomSize=""24"" CustomColor=""currentColor"" />
    </LoadingTemplate>
</BitTextField>

<BitTextField Label=""Availability of a user name""
              Immediate
              DebounceTime=""400""
              Loading=""@userNameLoading""
              Placeholder=""Type a user name...""
              Description=""@userNameStatus""
              @bind-Value=""userName""
              OnChange=""CheckUserNameAsync"" />";
    private readonly string example18CsharpCode = @"
private string? userName;
private bool userNameLoading;
private string? userNameStatus;
private CancellationTokenSource? _userNameCts;

private async Task CheckUserNameAsync(string? value)
{
    CancelAndDispose(ref _userNameCts);

    if (string.IsNullOrWhiteSpace(value))
    {
        userNameLoading = false;
        userNameStatus = null;
        return;
    }

    var cts = new CancellationTokenSource();
    _userNameCts = cts;

    userNameLoading = true;
    userNameStatus = null;

    try
    {
        await Task.Delay(1500, cts.Token);

        userNameStatus = value.Length < 5
            ? $""'{value}' is already taken.""
            : $""'{value}' is available."";
    }
    catch (OperationCanceledException)
    {
        return;
    }
    finally
    {
        if (cts.IsCancellationRequested is false)
        {
            userNameLoading = false;
        }
    }
}

private static void CancelAndDispose(ref CancellationTokenSource? cts)
{
    cts?.Cancel();
    cts?.Dispose();
    cts = null;
}";

    private readonly string example19RazorCode = @"
<BitTextField Label=""With a hidden description""
              AriaDescription=""Use the number printed on the back of your card, without the spaces between the groups.""
              Description=""Card number""
              Placeholder=""1234 5678 9012 3456"" />

<BitTextField Label=""With a tooltip"" Title=""The name your colleagues see next to your messages."" />

<BitTextField Label=""With a meaningful icon""
              IconName=""@BitIconName.Lock""
              IconAriaLabel=""This value is encrypted""
              DefaultValue=""•••• •••• •••• 3456"" />

<BitTextField AriaLabel=""Search the documentation""
              Type=""BitInputType.Search""
              Placeholder=""Search...""
              IconName=""@BitIconName.Search"" />

<BitTextField Label=""Card number""
              AriaLabel=""Card number, sixteen digits, no spaces""
              Placeholder=""1234 5678 9012 3456"" />

<BitTextField Label=""With a description of its own""
              Description=""Card number""
              InputHtmlAttributes=""@(new() { { ""aria-describedby"", ""card-number-hint"" } })"" />
<div id=""card-number-hint"">The field also points at this element, which is not a part of it.</div>";

    private readonly string example20RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>


<BitTextField Label=""Invalid"" Invalid DefaultValue=""Not accepted by the server"" />

<BitTextField Label=""Invalid with a description""
              Invalid=""@(invalidValue.HasValue() && invalidValue!.Contains(' ') is false)""
              Description=""Anything without a space in it is rejected.""
              @bind-Value=""invalidValue""
              Immediate />

<BitTextField Label=""Invalid + Underlined"" Invalid Underlined DefaultValue=""Not accepted either"" />

<BitTextField Label=""ErrorMessage"" ErrorMessage=""That user name is already taken."" DefaultValue=""admin"" />

<BitTextField Label=""ErrorMessage, a description and a counter""
              ShowCount
              MaxLength=""20""
              Description=""Letters and digits only.""
              ErrorMessage=""A user name cannot contain a space.""
              DefaultValue=""john doe"" />

<BitTextField Label=""Checked as you type""
              Immediate
              MaxLength=""20""
              @bind-Value=""errorMessageValue""
              Placeholder=""Type an email address...""
              ErrorMessage=""@EmailError""
              Description=""The message shows up as soon as the value stops looking like an email address."" />


<EditForm Model=""validationTextFieldModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
    <DataAnnotationsValidator />

    <BitTextField Label=""Required"" Required @bind-Value=""validationTextFieldModel.Text"" />
    <ValidationMessage For=""() => validationTextFieldModel.Text"" />

    <BitTextField Label=""Numeric"" @bind-Value=""validationTextFieldModel.NumericText"" />
    <ValidationMessage For=""() => validationTextFieldModel.NumericText"" />

    <BitTextField Label=""Only chars"" @bind-Value=""validationTextFieldModel.CharacterText"" />
    <ValidationMessage For=""() => validationTextFieldModel.CharacterText"" />

    <BitTextField Label=""Email"" @bind-Value=""validationTextFieldModel.EmailText"" />
    <ValidationMessage For=""() => validationTextFieldModel.EmailText"" />

    <BitTextField Label=""3 < Length < 5"" @bind-Value=""validationTextFieldModel.RangeText"" />
    <ValidationMessage For=""() => validationTextFieldModel.RangeText"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example20CsharpCode = @"
public class ValidationTextFieldModel
{
    [Required(ErrorMessage = ""This field is required."")]
    public string Text { get; set; }

    [RegularExpression(""0*[1-9][0-9]*"", ErrorMessage = ""Only numeric values are allowed."")]
    public string NumericText { get; set; }

    [RegularExpression(""^[a-zA-Z0-9.]*$"", ErrorMessage = ""Only letters(a-z), numbers(0-9), and period(.) are allowed."")]
    public string CharacterText { get; set; }

    [EmailAddress(ErrorMessage = ""Invalid e-mail address."")]
    public string EmailText { get; set; }

    [StringLength(5, MinimumLength = 3, ErrorMessage = ""The text length must be between 3 and 5 chars."")]
    public string RangeText { get; set; }
}

private string? invalidValue = ""no-space-here"";

private string? errorMessageValue = ""not-an-email"";

private string? EmailError => errorMessageValue.HasValue() && errorMessageValue!.Contains('@') is false
                                ? $""'{errorMessageValue}' is not an email address.""
                                : null;

private ValidationTextFieldModel validationTextFieldModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example21RazorCode = @"
<BitTextField Label=""Primary"" Background=""BitColorKind.Primary"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Secondary"" Background=""BitColorKind.Secondary"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Tertiary"" Background=""BitColorKind.Tertiary"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Transparent"" Background=""BitColorKind.Transparent"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example22RazorCode = @"
<BitTextField Label=""Primary"" Border=""BitColorKind.Primary"" />
<BitTextField Label=""Secondary"" Border=""BitColorKind.Secondary"" />
<BitTextField Label=""Tertiary"" Border=""BitColorKind.Tertiary"" />
<BitTextField Label=""Transparent"" Border=""BitColorKind.Transparent"" />";

    private readonly string example23RazorCode = @"
<BitTextField Label=""Primary"" Accent=""BitColor.Primary"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Secondary"" Accent=""BitColor.Secondary"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Tertiary"" Accent=""BitColor.Tertiary"" IconName=""@BitIconName.Calendar"" />

<BitTextField Label=""Info"" Accent=""BitColor.Info"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Success"" Accent=""BitColor.Success"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Warning"" Accent=""BitColor.Warning"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""SevereWarning"" Accent=""BitColor.SevereWarning"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Error"" Accent=""BitColor.Error"" IconName=""@BitIconName.Calendar"" />

<BitTextField Label=""PrimaryBackground"" Accent=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""SecondaryBackground"" Accent=""BitColor.SecondaryBackground"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""TertiaryBackground"" Accent=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Calendar"" />

<BitTextField Label=""PrimaryForeground"" Accent=""BitColor.PrimaryForeground"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""SecondaryForeground"" Accent=""BitColor.SecondaryForeground"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""TertiaryForeground"" Accent=""BitColor.TertiaryForeground"" IconName=""@BitIconName.Calendar"" />

<BitTextField Label=""PrimaryBorder"" Accent=""BitColor.PrimaryBorder"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""SecondaryBorder"" Accent=""BitColor.SecondaryBorder"" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""TertiaryBorder"" Accent=""BitColor.TertiaryBorder"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example24RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTextField Label=""House"" Icon=""@(""fa-solid fa-house"")"" />

<BitTextField Label=""Heart"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" />

<BitTextField Label=""GitHub"" Icon=""@BitIconInfo.Fa(""brands github"")"" />

<BitTextField Label=""Rocket"" Icon=""@BitIconInfo.Fa(""solid rocket"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTextField Label=""House"" Icon=""@(""bi bi-house-fill"")"" />

<BitTextField Label=""Heart"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" />

<BitTextField Label=""GitHub"" Icon=""@BitIconInfo.Bi(""github"")"" />

<BitTextField Label=""Gear"" Icon=""@BitIconInfo.Bi(""gear-fill"")"" />";

    private readonly string example25RazorCode = @"
<BitTextField Label=""Small"" Size=""BitSize.Small"" Placeholder=""Enter a text..."" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Medium"" Size=""BitSize.Medium"" Placeholder=""Enter a text..."" IconName=""@BitIconName.Calendar"" />
<BitTextField Label=""Large"" Size=""BitSize.Large"" Placeholder=""Enter a text..."" IconName=""@BitIconName.Calendar"" />

<BitTextField Label=""Small multiline"" Size=""BitSize.Small"" Multiline Rows=""3"" ShowClearButton
              DefaultValue=""A small multiline field."" />

<BitTextField Label=""Large with a clear button"" Size=""BitSize.Large"" ShowClearButton
              DefaultValue=""A large field."" />";

    private readonly string example26RazorCode = @"
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

    .custom-class::after {
        content: '';
        width: 0;
        left: 50%;
        bottom: 0;
        height: 2px;
        position: absolute;
        background-color: red;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-class:focus::after {
        left: 0;
        width: 100%;
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

    .custom-field {
        border-radius: 0;
        position: relative;
        border-width: 0 0 1px 0;
    }

    .custom-field::after {
        content: '';
        width: 0;
        height: 2px;
        border: none;
        position: absolute;
        inset: 100% 0 0 50%;
        background-color: blueviolet;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-focus .custom-field::after {
        left: 0;
        width: 100%;
    }

    .custom-focus .custom-label {
        color: blueviolet;
        transform: translate(0, 1.5px) scale(0.75);
    }
</style>


<BitTextField Style=""box-shadow: aqua 0 0 1rem; margin-inline: 1rem;"" />

<BitTextField Class=""custom-class"" />


<BitTextField Label=""Styles""
              Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                Focused = ""--focused-background: #b2b2b25a;"",
                                FieldGroup = ""background: var(--focused-background);"",
                                Label = ""text-shadow: aqua 0 0 1rem; font-weight: 900; font-size: 1.25rem;"",
                                Input = ""padding: 0.5rem;"" })"" />

<BitTextField @bind-Value=""classesValue""
              Label=""Classes""
              Classes=""@(new() { Root = ""custom-root"",
                                 FieldGroup = ""custom-field"",
                                 Focused = ""custom-focus"",
                                 Input = ""custom-input"",
                                 Label = $""custom-label{(string.IsNullOrEmpty(classesValue) ? string.Empty : "" custom-label-top"")}"" })"" />";
    private readonly string example26CsharpCode = @"
private string? classesValue;";

    private readonly string example27RazorCode = @"
<BitTextField Dir=""BitDir.Rtl""
              Placeholder=""پست الکترونیکی""
              IconName=""@BitIconName.EditMail"" />

<BitTextField Underlined
              Label=""تقویم""
              Dir=""BitDir.Rtl""
              IconName=""@BitIconName.Calendar"" />

<BitTextField Required
              ShowCount
              MaxLength=""20""
              ShowClearButton
              Dir=""BitDir.Rtl""
              Label=""نام و نام خانوادگی""
              DefaultValue=""بیت"" />";
}
