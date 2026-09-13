namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

public partial class BitCheckboxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Description = "The aria label of the icon for the benefit of screen readers.",
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
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
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



    private bool apple;
    private bool banana;
    private bool orange;
    private bool selectAll;
    private bool selectAllIndeterminate;

    private bool threeStateValue;
    private bool threeStateIndeterminate;

    private bool oneWayValue;
    private bool twoWayValue;
    private bool oneWayIndeterminate = true;
    private bool twoWayIndeterminate = true;

    private string eventsLog = string.Empty;
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



    private void HandleSelectAllChange(bool value)
    {
        selectAll = value;
        selectAllIndeterminate = false;
        apple = banana = orange = value;
    }

    private void RefreshSelectAll()
    {
        var checkedCount = (apple ? 1 : 0) + (banana ? 1 : 0) + (orange ? 1 : 0);

        selectAll = checkedCount == 3;
        selectAllIndeterminate = checkedCount is > 0 and < 3;
    }

    private void LogOnClick() => eventsLog = "OnClick";

    private void LogOnChanging(BitCheckboxChangeArgs args) => eventsLog += $" → OnChanging({args.Value})";

    private void LogOnChange(bool value) => eventsLog += $" → OnChange({value})";

    private void HandleOnChanging(BitCheckboxChangeArgs args)
    {
        if (allowChange) return;

        args.Cancel = true;
        cancelledCounter++;
    }

    private async Task FocusTheCheckbox() => await checkboxRef.FocusAsync();

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
