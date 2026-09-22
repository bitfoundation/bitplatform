namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

public partial class BitCheckboxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the disabled checkbox in the tab order and announced as disabled through aria-disabled instead of the native disabled attribute, with its toggling suppressed either way.",
        },
        new()
        {
            Name = "AriaControls",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element the checkbox controls - the list a select-all checkbox governs - rendered as aria-controls on the checkbox input.",
        },
        new()
        {
            Name = "AriaDescribedby",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ids of the elements that describe the checkbox, rendered into aria-describedby beside the ids Description and AriaDescription contribute.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the checkbox for the benefit of screen readers, rendered as a visually hidden element that the checkbox input points to via aria-describedby.",
        },
        new()
        {
            Name = "AriaLabelledby",
            Type = "string?",
            DefaultValue = "null",
            Description = "ID for element that contains label information for the checkbox.",
        },
        new()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            DefaultValue = "null",
            Description = "The position in the parent set (if in a set) for aria-posinset.",
        },
        new()
        {
            Name = "AriaSetSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The total size of the parent set (if in a set) for aria-setsize.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the checkbox input automatically receives focus when the page renders.",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the checkbox busy by itself for as long as the awaited callbacks behind a change are still running. A Loading set from the outside still applies on top of it.",
        },
        new()
        {
            Name = "CheckIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The check icon using custom CSS classes for external icon libraries. Takes precedence over CheckIconName when both are set. Use BitIconInfo.Bi(), BitIconInfo.Fa(), or BitIconInfo.Css() for Bootstrap Icons, FontAwesome, or custom CSS.",
        },
        new()
        {
            Name = "CheckIconName",
            Type = "string?",
            DefaultValue = "Accept",
            Description = "The name of the built-in icon to render as the check mark inside the checkbox.",
        },
        new()
        {
            Name = "CheckIconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Exposes the glyph inside the box as an image with this name. It is hidden from assistive technologies by default, since the state it stands for is already announced by the input itself.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the content of checkbox(Label and Box).",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCheckboxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitCheckbox.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the checkbox.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultIndeterminate",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default indeterminate visual state for checkbox.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The default value of the checkbox to be used in uncontrolled mode (i.e. when the Value is not bound).",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A visible explanation of what checking the box means, rendered on a line of its own under it and announced after the name of the checkbox through aria-describedby.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom description of the checkbox, replacing Description with arbitrary markup.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the checkbox across the full available width, pushing the box and the label to opposite edges. Applies to the single-line label placements only.",
        },
        new()
        {
            Name = "Indeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "An indeterminate visual state for checkbox. The indeterminate state takes visual precedence over the checked state but does not affect the Value.",
        },
        new()
        {
            Name = "IndeterminateIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to render in the indeterminate state using custom CSS classes for external icon libraries, replacing the default filled square. Takes precedence over IndeterminateIconName when both are set.",
        },
        new()
        {
            Name = "IndeterminateIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in icon to render in the indeterminate state, replacing the default filled square.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Descriptive label for the checkbox.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "The position of the label in regards to the checkbox box. Takes precedence over Reversed when both are set.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the checkbox.",
        },
        new()
        {
            Name = "Loading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the checkbox busy: the glyph becomes a spinner, clicks are turned away and the checkbox is announced as busy, while it keeps the state it is in and stays focusable.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the label of the checkbox on a single line and ends it with an ellipsis where it does not fit. Pair it with a Title so the part that was cut off is still reachable.",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the checkbox loses focus.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback for when the checkbox value changes, once the new state is committed.",
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitCheckboxChangeArgs>",
            Description = "Callback invoked before the state of the checkbox changes, letting the change be cancelled by setting Cancel on its arguments.",
            LinkType = LinkType.Link,
            Href = "#change-args",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the checkbox clicked.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the checkbox receives focus.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the focus moves into the checkbox.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the focus moves out of the checkbox.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the checkbox read-only: it stays focusable and gets announced by screen readers, but user interaction no longer changes its state.",
        },
        new()
        {
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the checkbox required, rendering the native required attribute on its input and an asterisk next to its label.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the label and checkbox location."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the checkbox.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, stops the click event from bubbling up to the parent elements.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCheckboxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitCheckbox.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ThreeState",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables cycling through the unchecked, checked and indeterminate states on each click, instead of the indeterminate state being reachable only programmatically.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title text applied to the label container of the checkbox.",
        },
        new()
        {
            Name = "UncheckedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to render in the unchecked state using custom CSS classes for external icon libraries. Takes precedence over UncheckedIconName when both are set.",
        },
        new()
        {
            Name = "UncheckedIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in icon to render in the unchecked state. By default the unchecked box is empty and previews the check icon on hover.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCheckboxClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCheckBox.",
                },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitCheckbox."
               },
               new()
               {
                   Name = "Checked",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the checked state of the BitCheckbox."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitCheckbox."
               },
               new()
               {
                   Name = "Indeterminate",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the indeterminate state of the BitCheckbox."
               },
               new()
               {
                   Name = "Box",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the box element of the BitCheckbox."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitCheckbox."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitCheckbox."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner rendered inside the box while the BitCheckbox is busy."
               }
            ]
        },
        new()
        {
            Id = "change-args",
            Title = "BitCheckboxChangeArgs",
            Description = "The arguments of the OnChanging callback of the BitCheckbox.",
            Parameters =
            [
                new()
                {
                    Name = "Value",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "The checked state the checkbox is about to move to.",
                },
                new()
                {
                    Name = "Indeterminate",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "The indeterminate state the checkbox is about to move to.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the change and keep the current state of the checkbox.",
                }
            ]
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Checkbox-color",
            DefaultValue = "Inherited from the page",
            Description = "Color of the label text. Left unset the label takes the color of whatever it sits in, which is what keeps a checkbox legible on any surface.",
        },
        new()
        {
            Name = "--bit-Checkbox-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the Description line under the label.",
        },
        new()
        {
            Name = "--bit-Checkbox-disabled-color",
            DefaultValue = "The Color role's disabled color",
            Description = "Stroke and fill of the box when IsEnabled is false; also the focus ring color of a disabled checkbox kept focusable with AllowDisabledFocus.",
        },
        new()
        {
            Name = "--bit-Checkbox-disabled-text-color",
            DefaultValue = "The Color role's disabled text color",
            Description = "Color of the label, the description and the glyph when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-Checkbox-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the keyboard focus ring, drawn around the box - or around the whole face of a checkbox given a ChildContent.",
        },
        new()
        {
            Name = "--bit-Checkbox-required-color",
            DefaultValue = "--bit-clr-req",
            Description = "Color of the asterisk that marks a required checkbox.",
        },
        new()
        {
            Name = "--bit-Checkbox-background",
            DefaultValue = "transparent",
            Description = "Fill of the box while unchecked, which the indeterminate state keeps.",
        },
        new()
        {
            Name = "--bit-Checkbox-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Stroke of the box while unchecked.",
        },
        new()
        {
            Name = "--bit-Checkbox-hover-background",
            DefaultValue = "--bit-Checkbox-background",
            Description = "Fill of the box while unchecked and hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Checkbox-hover-border-color",
            DefaultValue = "--bit-Checkbox-border-color",
            Description = "Stroke of the box while unchecked and hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Checkbox-checked-background",
            DefaultValue = "The Color role's main color",
            Description = "Fill of the box while checked.",
        },
        new()
        {
            Name = "--bit-Checkbox-checked-hover-background",
            DefaultValue = "The Color role's hover color",
            Description = "Fill of the box while checked and hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Checkbox-checked-border-color",
            DefaultValue = "--bit-Checkbox-checked-background",
            Description = "Stroke of the box while checked. Set it apart from the fill for the Material-style outlined box.",
        },
        new()
        {
            Name = "--bit-Checkbox-check-color",
            DefaultValue = "The Color role's on-color",
            Description = "Color of the glyph drawn on the checked box.",
        },
        new()
        {
            Name = "--bit-Checkbox-indeterminate-color",
            DefaultValue = "The Color role's main color",
            Description = "Color of the mark of the mixed state - the filled square or a custom IndeterminateIcon - and of the box stroke around it.",
        },
        new()
        {
            Name = "--bit-Checkbox-indeterminate-size",
            DefaultValue = "Half the box",
            Description = "Side of the filled square the mixed state draws in the middle of the box. It follows the box size on its own, so set it only to change that proportion. It has no effect where an IndeterminateIcon replaces the square.",
        },
        new()
        {
            Name = "--bit-Checkbox-box-size",
            DefaultValue = "Per Size: --bit-siz-sel-sm / -md / -lg",
            Description = "Side of the box, and the one number the rest of the face follows: the glyph, the filled square of the mixed state and the indent that keeps the description lined up with the label all scale with it.",
        },
        new()
        {
            Name = "--bit-Checkbox-icon-size",
            DefaultValue = "9/16 of the box",
            Description = "Font size of the glyph inside the box. It follows the box size on its own, so set it only for a glyph that needs more or less room than a check mark - a wide external icon, say.",
        },
        new()
        {
            Name = "--bit-Checkbox-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Thickness of the box stroke.",
        },
        new()
        {
            Name = "--bit-Checkbox-radius",
            DefaultValue = "--bit-shp-radius-selection",
            Description = "Corner radius of the box, which the filled square of the mixed state follows. Set it to 50% for a round box.",
        },
        new()
        {
            Name = "--bit-Checkbox-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the box and the label.",
        },
        new()
        {
            Name = "--bit-Checkbox-font-size",
            DefaultValue = "Per Size: --bit-tpg-fs-xs / -sm / -md",
            Description = "Font size of the label.",
        },
        new()
        {
            Name = "--bit-Checkbox-font-weight",
            DefaultValue = "--bit-tpg-fw-regular",
            Description = "Font weight of the label.",
        },
        new()
        {
            Name = "--bit-Checkbox-description-font-size",
            DefaultValue = "Per Size: --bit-tpg-fs-2xs / -xs / -sm",
            Description = "Font size of the description, one step below the label on the type ramp.",
        },
        new()
        {
            Name = "--bit-Checkbox-description-gap",
            DefaultValue = "spacing(0.25)",
            Description = "Room between the label and the description under it.",
        },
        new()
        {
            Name = "--bit-Checkbox-min-height",
            DefaultValue = "The box size, never below spacing(3)",
            Description = "Smallest height and width of the click target, which is the label rather than the box. It is what keeps every size above the 24px minimum pointer target of WCAG 2.2 (SC 2.5.8), and it is a floor rather than a size: a wrapped label still grows the row. Set it to 0 for a checkbox that has to sit on the line of the running text around it.",
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
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "The position of the label in regards to the checkbox box.",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the checkbox.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the checkbox.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the checkbox.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows on the start of the checkbox.",
                    Value="3",
                }
            ]
        },
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
                    Description="The small size checkbox.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size checkbox.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size checkbox.",
                    Value="2",
                }
            ]
        }
    ];



    private bool basicIndeterminate = true;

    private bool apple;
    private bool banana;
    private bool orange;
    private bool selectAll;
    private bool selectAllIndeterminate;

    private bool threeStateValue;
    private bool threeStateIndeterminate;

    private bool? subscribed = false;

    private bool oneWayValue;
    private bool twoWayValue;
    private bool oneWayIndeterminate = true;
    private bool twoWayIndeterminate = true;

    private string eventsLog = string.Empty;
    private bool eventsCycleEnded;
    private int cancelledCounter;
    private int containerClickCounter;
    private bool allowChange;

    private bool readOnlyValue;

    private bool customCheckboxValue;

    private bool customContentValue;
    private bool customContentIndeterminate = true;

    private BitCheckbox checkboxRef = default!;

    private string SuccessMessage = string.Empty;
    private BitCheckboxValidationModel validationModel = new();

    private int savedCount;



    private void HandleSelectAllChange(bool value)
    {
        selectAll = value;
        selectAllIndeterminate = false;
        apple = banana = orange = value;
    }

    // A click reports the mixed state before the value, so the mixed one has the last word: the value that
    // follows a "no answer" is not an answer either, which is what keeps the null from being overwritten
    // with the false underneath it.
    private void HandleSubscribedIndeterminateChanged(bool indeterminate) => subscribed = indeterminate ? null : subscribed is true;

    private void HandleSubscribedValueChanged(bool value) => subscribed = subscribed is null ? null : value;

    private void RefreshSelectAll()
    {
        var checkedCount = (apple ? 1 : 0) + (banana ? 1 : 0) + (orange ? 1 : 0);

        selectAll = checkedCount == 3;
        selectAllIndeterminate = checkedCount is > 0 and < 3;
    }

    private void LogOnClick() => AppendEventLog("OnClick");

    private void LogOnChanging(BitCheckboxChangeArgs args) => AppendEventLog($"OnChanging({args.Value})");

    private void LogOnChange(bool value)
    {
        AppendEventLog($"OnChange({value})");
        eventsCycleEnded = true;
    }

    private void LogOnFocus() => AppendEventLog("OnFocus");

    private void LogOnFocusIn() => AppendEventLog("OnFocusIn");

    private void LogOnFocusOut() => AppendEventLog("OnFocusOut");

    private void LogOnBlur() => AppendEventLog("OnBlur");

    // Every callback appends, so the log is the order they actually fired in - the focus arriving is still
    // on the line when the click that follows it is written. Only the first one after a completed click
    // starts the line over, which is what keeps a second click from being read as part of the first.
    private void AppendEventLog(string name)
    {
        if (eventsCycleEnded)
        {
            eventsLog = string.Empty;
            eventsCycleEnded = false;
        }

        eventsLog += eventsLog.Length == 0 ? name : $" → {name}";
    }

    private void HandleOnChanging(BitCheckboxChangeArgs args)
    {
        if (allowChange) return;

        args.Cancel = true;
        cancelledCounter++;
    }

    private async Task FocusTheCheckbox() => await checkboxRef.FocusAsync();

    private async Task HandleSlowChanging(BitCheckboxChangeArgs args)
    {
        await Task.Delay(2000);
        savedCount++;
    }

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
