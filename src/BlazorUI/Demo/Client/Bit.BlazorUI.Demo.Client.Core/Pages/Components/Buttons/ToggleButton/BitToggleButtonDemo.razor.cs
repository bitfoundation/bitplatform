namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ToggleButton;

public partial class BitToggleButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Keeps the disabled toggle button focusable and discoverable by screen readers, rendering aria-disabled instead of the native disabled attribute. Set it to false to render the native disabled attribute and remove the toggle button from the tab order.",
        },
        new()
        {
            Name = "AriaControls",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that the toggle button controls (rendered into aria-controls). Where the controlled element is a part of the page the checked state reveals, pair it with the Expanded AriaMode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the toggle button for the benefit of screen readers (rendered into aria-describedby).",
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, adds an aria-hidden attribute instructing screen readers to ignore the toggle button.",
        },
        new()
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that labels the toggle button (rendered into aria-labelledby).",
        },
        new()
        {
            Name = "AriaMode",
            Type = "BitToggleButtonAriaMode?",
            DefaultValue = "null",
            Description = "Determines which ARIA state attribute the toggle button exposes to assistive technologies. The default Auto mode drops aria-pressed when the accessible name of the toggle button changes between the two states.",
            LinkType = LinkType.Link,
            Href = "#aria-mode-enum",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the toggle button automatically receives focus when the page renders.",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, enters the loading state automatically while awaiting the click and change events, preventing subsequent clicks by default.",
        },
        new()
        {
            Name = "CheckMarkIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The check mark icon to display using custom CSS classes for external icon libraries. Takes precedence over CheckMarkIconName when both are set.",
        },
        new()
        {
            Name = "CheckMarkIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the check mark icon that renders in the checked state when ShowCheckMark is enabled.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the toggle button.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitToggleButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the toggle button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the toggle button.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultIsChecked",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value of the IsChecked parameter.",
        },
        new()
        {
            Name = "FixedCheckMark",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the space of the check mark reserved in the unchecked state so the content does not shift while toggling.",
        },
        new()
        {
            Name = "FixedColor",
            Type = "bool",
            DefaultValue = "false",
            Description = "Preserves the foreground color of the toggle button through hover and focus.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the toggle button width to 100% of the available width.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name from built-in Fluent UI icons that renders inside the toggle button.",
        },
        new()
        {
            Name = "IconOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines that only the icon should be rendered and changes the styles accordingly.",
        },
        new()
        {
            Name = "IconPosition",
            Type = "BitIconPosition?",
            DefaultValue = "null",
            Description = "The position of the icon relative to the content of the toggle button.",
            LinkType = LinkType.Link,
            Href = "#icon-position-enum",
        },
        new()
        {
            Name = "IsChecked",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the toggle button is in the checked state.",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the toggle button is in the loading state, which covers its content with a spinner and prevents subsequent clicks unless Reclickable is enabled. The content stays in place behind the spinner, so the accessible name does not disappear while the toggle button is busy.",
        },
        new()
        {
            Name = "LoadingDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the spinner appears after the toggle button enters the loading state, which keeps a fast toggle from flashing one. The click guard of the loading state applies immediately regardless of the delay.",
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The loading label text to show next to the spinner icon. It is also announced by a live region beside the toggle button when the loading state begins.",
        },
        new()
        {
            Name = "LoadingLabelPosition",
            Type = "BitLabelPosition",
            DefaultValue = "BitLabelPosition.End",
            Description = "The position of the loading label in regards to the spinner icon.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template used to replace the default content of the toggle button in the loading state.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the text of the toggle button on a single line and ends it with an ellipsis where it does not fit.",
        },
        new()
        {
            Name = "OffAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the toggle button when it is not checked.",
        },
        new()
        {
            Name = "OffColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the toggle button when it is not checked. Falls back to the Color parameter when not provided.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "OffIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display when the toggle button is not checked, using custom CSS classes for external icon libraries. Takes precedence over OffIconName.",
        },
        new()
        {
            Name = "OffIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon from built-in Fluent UI icons when the toggle button is not checked.",
        },
        new()
        {
            Name = "OffTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the toggle button when it is not checked. A template that differs from the one of the other state usually changes the accessible name with it, which the automatic AriaMode cannot detect the way it detects a changing text; set AriaLabel or AriaMode to settle it.",
        },
        new()
        {
            Name = "OffText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the toggle button when it is not checked.",
        },
        new()
        {
            Name = "OffTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the toggle button when it is not checked. With no other wording to name the toggle button, the tooltip becomes its accessible name, so a title that differs per state suppresses aria-pressed just as a per-state text does.",
        },
        new()
        {
            Name = "OffVariant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the toggle button when it is not checked. Falls back to the Variant parameter when not provided.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "OnAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the toggle button when it is checked.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback for when the IsChecked value has changed.",
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitToggleButtonChangeArgs>",
            Description = "Callback invoked before the checked state changes, letting the change be cancelled by setting Cancel on its arguments.",
            LinkType = LinkType.Link,
            Href = "#change-args",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the toggle button is clicked.",
        },
        new()
        {
            Name = "OnColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the toggle button when it is checked. Falls back to the Color parameter when not provided.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "OnIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display when the toggle button is checked, using custom CSS classes for external icon libraries. Takes precedence over OnIconName.",
        },
        new()
        {
            Name = "OnIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon from built-in Fluent UI icons when the toggle button is checked.",
        },
        new()
        {
            Name = "OnTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the toggle button when it is checked. A template that differs from the one of the other state usually changes the accessible name with it, which the automatic AriaMode cannot detect the way it detects a changing text; set AriaLabel or AriaMode to settle it.",
        },
        new()
        {
            Name = "OnText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the toggle button when it is checked.",
        },
        new()
        {
            Name = "OnTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the toggle button when it is checked. With no other wording to name the toggle button, the tooltip becomes its accessible name, so a title that differs per state suppresses aria-pressed just as a per-state text does.",
        },
        new()
        {
            Name = "OnVariant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the toggle button when it is checked. Falls back to the Variant parameter when not provided.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Reclickable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables re-clicking while the toggle button is in the loading state. A loading toggle button otherwise stops responding to the pointer altogether, keeping neither the hover shade nor the pointer cursor of a control that takes clicks.",
        },
        new()
        {
            Name = "ShowCheckMark",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a check mark in the checked state so the state is not conveyed by color alone, which is also what keeps the state readable in Windows High Contrast. It is part of the default body, so a toggle button given a template renders none.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the toggle button.",
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
            Type = "BitToggleButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the toggle button.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the toggle button.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse is placed on the toggle button.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the toggle button.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the root element of the toggle button.",
        },
        new()
        {
            Name = "ToggleAsync",
            Type = "Task",
            Description = "Toggles the checked state of the toggle button, going through the same cancellation and change notification path a click does.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-ToggleButton-color",
            DefaultValue = "Per Variant: the role's on color (Fill), its main color (Outline, Text)",
            Description = "Foreground of the unchecked toggle button at rest. The checked state paints its own, so setting this one alone never changes how the state reads.",
        },
        new()
        {
            Name = "--bit-ToggleButton-background",
            DefaultValue = "Per Variant: the role's main color (Fill), transparent (Outline, Text)",
            Description = "Background of the unchecked toggle button at rest, and the fallback of its resting border color.",
        },
        new()
        {
            Name = "--bit-ToggleButton-border-color",
            DefaultValue = "--bit-ToggleButton-background, then per Variant",
            Description = "Border color of the unchecked toggle button at rest. The Outline variant is the one that draws it in the role color while the background stays transparent.",
        },
        new()
        {
            Name = "--bit-ToggleButton-hover-color",
            DefaultValue = "Per Variant: the resting foreground (Fill), the role's on color (Outline, Text)",
            Description = "Foreground while hovered, on pointer devices only.",
        },
        new()
        {
            Name = "--bit-ToggleButton-hover-background",
            DefaultValue = "The Color role's hover color",
            Description = "Background while hovered, and the fallback of the hovered border color.",
        },
        new()
        {
            Name = "--bit-ToggleButton-hover-border-color",
            DefaultValue = "--bit-ToggleButton-hover-background",
            Description = "Border color while hovered. Set it on its own to keep an outline steady under a changing background.",
        },
        new()
        {
            Name = "--bit-ToggleButton-active-color",
            DefaultValue = "--bit-ToggleButton-hover-color",
            Description = "Foreground while pressed.",
        },
        new()
        {
            Name = "--bit-ToggleButton-active-background",
            DefaultValue = "The Color role's active color",
            Description = "Background while pressed, and the fallback of the pressed border color.",
        },
        new()
        {
            Name = "--bit-ToggleButton-active-border-color",
            DefaultValue = "--bit-ToggleButton-active-background",
            Description = "Border color while pressed.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-color",
            DefaultValue = "The Color role's on color",
            Description = "Foreground while checked, in every variant.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-background",
            DefaultValue = "The Color role's dark color",
            Description = "Background while checked, and the fallback of the checked border color. It is what separates the two states visually, so it stays worth keeping distinct from the resting background.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-border-color",
            DefaultValue = "--bit-ToggleButton-checked-background",
            Description = "Border color while checked.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-hover-background",
            DefaultValue = "The Color role's dark hover color",
            Description = "Background while checked and hovered.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-hover-border-color",
            DefaultValue = "--bit-ToggleButton-checked-hover-background",
            Description = "Border color while checked and hovered.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-active-background",
            DefaultValue = "The Color role's dark active color",
            Description = "Background while checked and pressed.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-active-border-color",
            DefaultValue = "--bit-ToggleButton-checked-active-background",
            Description = "Border color while checked and pressed.",
        },
        new()
        {
            Name = "--bit-ToggleButton-disabled-color",
            DefaultValue = "The Color role's disabled text color",
            Description = "Foreground when IsEnabled is false; also the focus ring color of a disabled toggle button kept focusable with AllowDisabledFocus.",
        },
        new()
        {
            Name = "--bit-ToggleButton-disabled-background",
            DefaultValue = "Per Variant: the role's disabled color (Fill), transparent (Outline, Text)",
            Description = "Background when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-ToggleButton-disabled-border-color",
            DefaultValue = "--bit-ToggleButton-disabled-background, then per Variant",
            Description = "Border color when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-ToggleButton-checked-disabled-opacity",
            DefaultValue = "--bit-opa-dis",
            Description = "Dimming of a disabled toggle button that is checked. It keeps the checked colors rather than taking the disabled ones, since a setting greyed out at \"on\" and one greyed out at \"off\" are different facts; set it to 1 to hand the whole appearance back to the disabled variables above.",
        },
        new()
        {
            Name = "--bit-ToggleButton-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the keyboard focus ring.",
        },
        new()
        {
            Name = "--bit-ToggleButton-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of the box, which the background and the focus ring follow. Set it to 999px for the pill-shaped toggle buttons of a filter row.",
        },
        new()
        {
            Name = "--bit-ToggleButton-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Thickness of the border. The Fill and Text variants draw theirs in the background color, so thickening it only shows on Outline unless a border color is set with it.",
        },
        new()
        {
            Name = "--bit-ToggleButton-min-width",
            DefaultValue = "--bit-siz-ctrl-min-width",
            Description = "Smallest width of the box. An icon-only toggle button falls back to its minimum height rather than to the control minimum, which is what keeps it square.",
        },
        new()
        {
            Name = "--bit-ToggleButton-min-height",
            DefaultValue = "Per Size: --bit-siz-ctrl-sm / -md / -lg",
            Description = "Smallest height of the box, which is what lines a toggle button up with the other controls of its size and keeps the smallest one above the 24px minimum pointer target of WCAG 2.2. It is a floor, not a height: a wrapped label still grows the box. It is also the minimum width of an icon-only toggle button.",
        },
        new()
        {
            Name = "--bit-ToggleButton-padding",
            DefaultValue = "Per Size: the control's y and x padding, square when icon-only",
            Description = "Padding of the box.",
        },
        new()
        {
            Name = "--bit-ToggleButton-gap",
            DefaultValue = "spacing(0.5), 0 when there is no text",
            Description = "Room between the check mark, the icon and the text.",
        },
        new()
        {
            Name = "--bit-ToggleButton-font-size",
            DefaultValue = "Per Size: --bit-tpg-fs-xs / -sm / -md",
            Description = "Font size of the text.",
        },
        new()
        {
            Name = "--bit-ToggleButton-font-weight",
            DefaultValue = "--bit-tpg-font-weight",
            Description = "Font weight of the text. Raising it on the checked state alone is not possible from here, since one box carries both states; use Styles.Checked for that.",
        },
        new()
        {
            Name = "--bit-ToggleButton-icon-size",
            DefaultValue = "The text size, or per Size the glyph size when icon-only",
            Description = "Size of the icon and the check mark. With a label beside it the glyph rides the text by default, which is what keeps the two lined up at any font scale.",
        },
        new()
        {
            Name = "--bit-ToggleButton-spinner-size",
            DefaultValue = "Per Size: spacing(2) / spacing(2.35) / spacing(2.75)",
            Description = "Diameter of the spinner shown in the loading state.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitToggleButtonClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitToggleButton.",
               },
               new()
               {
                   Name = "CheckMark",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the check mark element of the BitToggleButton.",
               },
               new()
               {
                   Name = "Checked",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the checked state of the BitToggleButton.",
               },
               new()
               {
                   Name = "HiddenContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the hidden content of the BitToggleButton in the loading state.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon element of the BitToggleButton.",
               },
               new()
               {
                   Name = "LoadingContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading container of the BitToggleButton.",
               },
               new()
               {
                   Name = "LoadingLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading label of the BitToggleButton.",
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading spinner of the BitToggleButton.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text element of the BitToggleButton.",
               }
            ]
        },
        new()
        {
            Id = "change-args",
            Title = "BitToggleButtonChangeArgs",
            Description = "The arguments of the OnChanging callback of the BitToggleButton.",
            Parameters =
            [
                new()
                {
                    Name = "Value",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "The checked state the toggle button is about to move to.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the change and keep the current checked state.",
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "aria-mode-enum",
            Name = "BitToggleButtonAriaMode",
            Description = "Determines which ARIA state attribute the toggle button exposes to assistive technologies.",
            Items =
            [
                new()
                {
                    Name= "Auto",
                    Description="Renders aria-pressed, unless the accessible name of the toggle button changes between the checked and unchecked states, in which case no state attribute is rendered.",
                    Value="0",
                },
                new()
                {
                    Name= "Pressed",
                    Description="Always renders aria-pressed, even when the accessible name changes between the two states.",
                    Value="1",
                },
                new()
                {
                    Name= "Switch",
                    Description="Renders role=\"switch\" along with aria-checked instead of aria-pressed.",
                    Value="2",
                },
                new()
                {
                    Name= "None",
                    Description="Renders no state attribute at all, for content that already conveys the state.",
                    Value="3",
                },
                new()
                {
                    Name= "Expanded",
                    Description="Renders aria-expanded instead of aria-pressed, for a toggle button whose checked state is another part of the page being shown.",
                    Value="4",
                }
            ]
        },
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
            Id = "icon-position-enum",
            Name = "BitIconPosition",
            Description = "Describes the placement of an icon relative to other content.",
            Items =
            [
                new()
                {
                    Name= "Start",
                    Description="Icon renders before the content (default).",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="Icon renders after the content.",
                    Value="1",
                }
            ]
        },
        new()
        {
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "Determines the position of the loading label in regards to the spinner icon.",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label renders above the spinner.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label renders after the spinner.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label renders below the spinner.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label renders before the spinner.",
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
                    Description="The small size button.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size button.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size button.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        }
    ];



    private bool twoWayBoundValue;
    private bool onChangeValue;
    private BitToggleButton programmaticToggleRef = default!;

    private int clickCounter;
    private bool allowChange;
    private int cancelledCounter;
    private int containerClickCounter;

    private bool isLoading;
    private int reclickCounter;
    private int blockedClickCounter;

    private bool detailsVisible;
    private bool statusBarVisible;



    private async Task FocusTheToggleButton() => await programmaticToggleRef.FocusAsync();

    private void HandleOnChanging(BitToggleButtonChangeArgs args)
    {
        if (allowChange) return;

        args.Cancel = true;
        cancelledCounter++;
    }

    private async Task HandleAutoLoadingChange()
    {
        // stands in for persisting the new state somewhere slow
        await Task.Delay(2000);
    }
}
