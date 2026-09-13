namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Toggle;

public partial class BitToggleDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaControls",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element the toggle controls, rendered as aria-controls on the switch.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the toggle for the benefit of screen readers, rendered as a visually hidden element that the switch points to via aria-describedby.",
        },
        new()
        {
            Name = "AriaDescribedby",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of an existing element that describes the toggle, added to the aria-describedby of the switch alongside its state text and its AriaDescription rather than replacing them.",
        },
        new()
        {
            Name = "AriaLabelledby",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of an existing element that labels the toggle, rendered as aria-labelledby on the switch. Takes precedence over the label of the toggle and over AriaLabel.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the toggle automatically receives focus when the page renders (rendered as the autofocus attribute).",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the toggle busy on its own for as long as the awaited OnClick, OnChanging and OnChange callbacks behind a change are still running, without a loading flag having to be tracked outside the component.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitToggleClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the toggle.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the toggle, applied to the track of the checked state.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A visible explanation of what the toggle switches, rendered on a line of its own under it and announced after the name of the switch through aria-describedby.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom description of the toggle, replacing Description with arbitrary markup.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the toggle in full width of its container while putting space between the label and the knob.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the label and the knob in a single line together.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label of the toggle.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "The position of the label in regards to the knob of the toggle. Takes precedence over Inline and Reversed when set.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label of the toggle.",
        },
        new()
        {
            Name = "Loading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a spinner in place of the knob's icon and suspends the toggle until the pending work behind the change is done.",
        },
        new()
        {
            Name = "OffContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content rendered inside the track of the toggle while it is OFF, next to the knob.",
        },
        new()
        {
            Name = "OffIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon rendered inside the knob while the toggle is OFF, using custom CSS classes for external icon libraries. Takes precedence over OffIconName when both are set.",
        },
        new()
        {
            Name = "OffIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in icon rendered inside the knob while the toggle is OFF.",
        },
        new()
        {
            Name = "OffText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text to display when toggle is OFF.",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the toggle loses focus.",
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitToggleChangeArgs>",
            DefaultValue = "",
            Description = "Callback invoked before the state of the toggle changes, letting the change be cancelled by setting Cancel on the provided arguments.",
            LinkType = LinkType.Link,
            Href = "#change-args",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the toggle is clicked, invoked before the state changes.",
        },
        new()
        {
            Name = "OnContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content rendered inside the track of the toggle while it is ON, next to the knob.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the toggle receives focus.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the toggle.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves out of the toggle.",
        },
        new()
        {
            Name = "OnIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon rendered inside the knob while the toggle is ON, using custom CSS classes for external icon libraries. Takes precedence over OnIconName when both are set.",
        },
        new()
        {
            Name = "OnIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the built-in icon rendered inside the knob while the toggle is ON.",
        },
        new()
        {
            Name = "OnText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text to display when toggle is ON.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the positions of the label and input of the toggle.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "switch",
            Description = "Denotes role of the toggle, default is switch.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the toggle.",
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
            Type = "BitToggleClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the toggle.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default text used when the On or Off texts are null.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The native tooltip of the knob of the toggle, shown on hover.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference of the switch button of the BitToggle - the element that holds the tab stop, not the hidden checkbox that carries the value into a form.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "(bool preventScroll = false) => ValueTask",
            Description = "Gives focus to the switch button of the BitToggle. Pass true to keep the browser from scrolling the toggle into view along the way.",
        },
        new()
        {
            Name = "ToggleAsync",
            Type = "() => Task",
            Description = "Flips the state of the BitToggle from code, as a click would: OnChanging still gets to cancel the change and OnChange still reports it. Unlike a click it is not blocked by the read-only or loading states, which only close the toggle to the user; a disabled toggle changes through neither.",
        },
        new()
        {
            Name = "ToggleAsync",
            Type = "(bool value) => Task",
            Description = "Moves the BitToggle to a specific state from code, doing nothing when it is already in it. Otherwise identical to the parameterless overload.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitToggleClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitToggle."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the BitToggle."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description of the BitToggle."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitToggle."
                },
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the BitToggle."
                },
                new()
                {
                    Name = "Checked",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the checked state of the BitToggle."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content rendered inside the track of the BitToggle."
                },
                new()
                {
                    Name = "OnContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the ON side of the content rendered inside the track of the BitToggle."
                },
                new()
                {
                    Name = "OffContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the OFF side of the content rendered inside the track of the BitToggle."
                },
                new()
                {
                    Name = "Thumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the thumb of the BitToggle."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon rendered inside the thumb of the BitToggle."
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the loading spinner of the BitToggle."
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text of the BitToggle."
                }
            ]
        },
        new()
        {
            Id = "change-args",
            Title = "BitToggleChangeArgs",
            Parameters =
            [
                new()
                {
                    Name = "Value",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "The state the toggle is about to move to (read-only)."
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the change and keep the current state of the toggle."
                }
            ]
        }
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
                    Description="Primary general color.",
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
            Description = "The position of the label in regards to the knob of the toggle.",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the toggle.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the toggle.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the toggle.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows on the start of the toggle.",
                    Value="3",
                }
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
                    Name= "Small",
                    Description="The small size toggle.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size toggle.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size toggle.",
                    Value="2",
                }
            ]
        }
    ];



    private bool isSaving;
    private bool savedValue;
    private bool autoLoadingValue;

    private bool readOnlyValue;

    private bool advancedSettingsVisible;

    private bool oneWayValue;
    private bool twoWayValue;

    private string methodChangeLog = string.Empty;
    private BitToggle methodToggleRef = default!;

    private string eventsLog = string.Empty;
    private string focusLog = string.Empty;
    private int cancelledCounter;
    private int containerClickCounter;
    private bool allowChange;

    private BitToggle toggleRef = default!;

    private string successMessage = string.Empty;
    private BitToggleValidationModel validationModel = new();



    private async Task HandleSaveToggle(bool value)
    {
        isSaving = true;

        await Task.Delay(1000);

        savedValue = value;
        isSaving = false;
    }

    private async Task SaveTheToggle(bool value)
    {
        // AutoLoading keeps the toggle busy for exactly as long as this callback takes
        await Task.Delay(1000);
    }

    private void LogMethodChange(bool value)
    {
        methodChangeLog = $"OnChange fired with {value}";
    }

    private void LogOnClick()
    {
        eventsLog = "OnClick";
    }

    private void LogOnChanging(BitToggleChangeArgs args)
    {
        eventsLog += $" → OnChanging (to {args.Value})";
    }

    private void LogOnChange(bool value)
    {
        eventsLog += $" → OnChange ({value})";
    }

    private void LogOnFocus()
    {
        focusLog = "OnFocus";
    }

    private void LogOnFocusIn()
    {
        focusLog += " → OnFocusIn";
    }

    private void LogOnFocusOut()
    {
        focusLog = "OnFocusOut";
    }

    private void LogOnBlur()
    {
        focusLog += " → OnBlur";
    }

    private void HandleOnChanging(BitToggleChangeArgs args)
    {
        if (allowChange) return;

        args.Cancel = true;
        cancelledCounter++;
    }

    private async Task FocusTheToggle()
    {
        await toggleRef.FocusAsync();
    }

    private async Task HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        successMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }
}
