namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Accordion;

public partial class BitAccordionDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Actions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered beside the header, outside of the toggle button and of the heading it sits in, so that it can hold its own interactive elements (a menu, a delete button, a switch)."
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the accordion. Wins over an inherited --bit-Accordion-background.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the accordion. Wins over an inherited --bit-Accordion-border-color.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for the ChildContent parameter."
        },
        new()
        {
            Name = "Busy",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reports the header as busy - a spinner in the expander's slot, aria-busy and a busy cursor, and no click toggles it - while something the page is doing on the accordion's behalf is still running. An accordion whose own OnToggling is being awaited reports itself as busy without being told to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitAccordionClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the accordion.",
            LinkType = LinkType.Link,
            Href = "#accordion-class-styles"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the accordion."
        },
        new()
        {
            Name = "DefaultIsExpanded",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value for the IsExpanded parameter."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short description in the header of the accordion."
        },
        new()
        {
            Name = "ExpandedExpanderIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to show in place of the expander icon while the accordion is expanded, using custom CSS classes for external icon libraries. Takes precedence over ExpandedExpanderIconName when both are set. Setting either of them also turns the rotation of the expander icon off."
        },
        new()
        {
            Name = "ExpandedExpanderIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon, from the built-in Fluent UI icons, to show in place of the expander icon while the accordion is expanded. Setting it also turns the rotation of the expander icon off."
        },
        new()
        {
            Name = "ExpanderIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display as expander using custom CSS classes for external icon libraries. Takes precedence over ExpanderIconName when both are set. Defaults to the ChevronRight icon if neither property is set."
        },
        new()
        {
            Name = "ExpanderIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display as expander from the built-in Fluent UI icons. Defaults to ChevronRight if not set."
        },
        new()
        {
            Name = "ExpanderIconPlacement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "Gets or sets the side of the header the expander icon sits on. The default value is End. Only Start and End are honoured, and they follow the reading direction; any other value leaves the icon at the end.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
        },
        new()
        {
            Name = "ExpanderTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "Custom content in place of the expander icon, receiving the expanded state. It still turns over unless NoExpanderRotation is set, and HideExpanderIcon still removes it; HeaderTemplate replaces it with the rest of the header."
        },
        new()
        {
            Name = "ExpandOnPrint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the panel on paper, so a collapsed section is not printed as a bare header, and lifts the MaxHeight scroll cap. Content not in the DOM (a never-opened LazyContent panel, a collapsed UnmountOnCollapse one) still cannot print."
        },
        new()
        {
            Name = "HeaderAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the accessible label of the toggle button in the header, for a header whose own content does not name it - an icon-only HeaderTemplate, most of all."
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "Used to customize the header of the accordion. It replaces the whole default header, the expander icon included, and receives the current expanded state."
        },
        new()
        {
            Name = "HeadingLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the heading level (aria-level) reported for the header of the accordion, so that it takes its right place in the heading outline of the page. The default value is 3 - or one level below the accordion this one is nested in - and the value is clamped to the 1..6 range."
        },
        new()
        {
            Name = "HiddenUntilFound",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hands the collapsed panel to the browser as hidden=\"until-found\", so find-in-page and a navigation to a fragment inside it reach the text and expand the accordion around the match (reported to OnToggling with the Reveal reason). The panel stays in the DOM, so LazyContent and UnmountOnCollapse are ignored; a disabled, read-only or one-way bound accordion is not offered to find-in-page."
        },
        new()
        {
            Name = "HideExpanderIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the expander icon from the header of the accordion."
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display at the start of the header using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set."
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display at the start of the header from the built-in Fluent UI icons."
        },
        new()
        {
            Name = "IsExpanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the accordion is expanded or collapsed. (two-way bound)"
        },
        new()
        {
            Name = "LazyContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Delays the first render of the content of the accordion until it is expanded for the first time. The content stays in the DOM afterwards, so the state it holds survives a collapse. Ignored while HiddenUntilFound is on."
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the maximum height of the content of the accordion (any CSS length), beyond which the content scrolls inside the accordion instead of growing it. The scrolling region is focusable, so that it can be scrolled by the keyboard as well."
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of the accordion and gives a background color to the body."
        },
        new()
        {
            Name = "NoContentRegion",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the region role (a landmark) from the panel, leaving a plain container. The WAI-ARIA authoring practices ask for it where more than about six panels can be open at once, so the landmarks do not flood the page."
        },
        new()
        {
            Name = "NoExpanderRotation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the expander icon still instead of turning it over when the accordion is expanded."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback that is called when the header is clicked."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the IsExpanded value has changed."
        },
        new()
        {
            Name = "OnCollapse",
            Type = "EventCallback",
            Description = "Callback that is called when the accordion is collapsed."
        },
        new()
        {
            Name = "OnExpand",
            Type = "EventCallback",
            Description = "Callback that is called when the accordion is expanded."
        },
        new()
        {
            Name = "OnToggling",
            Type = "EventCallback<BitAccordionToggleArgs>",
            Description = "Called before the accordion expands or collapses; set Cancel to refuse the change. It is awaited, so it can load the panel's content or ask for a confirmation first, and the header reports busy meanwhile. A change made through the IsExpanded parameter itself is not offered here.",
            LinkType = LinkType.Link,
            Href = "#accordion-toggle-args",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the accordion where it is: the header keeps its colors and its place in the tab order, and reports itself as aria-disabled, but it no longer answers the pointer or the keyboard. OnClick still reports the click, and the Expand, Collapse and Toggle methods still drive the accordion."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Gets or sets the size of the accordion, which drives the padding of the header and of the panel and the type scale of the whole component. The default value is Medium.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAccordionClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the accordion.",
            LinkType = LinkType.Link,
            Href = "#accordion-class-styles"
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title in the header of the accordion, which also names the header button and the panel for assistive technologies (the Description describes them)."
        },
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content to render in place of the Title, leaving the rest of the header - the icon, the description and the expander - as it is. Unlike HeaderTemplate, which replaces the whole header, this only takes the place of the title text."
        },
        new()
        {
            Name = "TransitionDuration",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the duration of the expand/collapse transition in milliseconds, overriding the duration the theme provides. A reduced-motion preference still collapses it, unless the ForceAnimation parameter opts out of that."
        },
        new()
        {
            Name = "UnmountOnCollapse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the content of the accordion from the DOM while it is collapsed, so that nothing it holds keeps running behind a closed header. The collapse of an accordion that unmounts its content is not animated, since there is nothing left to animate. Ignored while HiddenUntilFound is on."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Expand",
            Type = "Task",
            Description = "Expands the accordion. Does nothing if it is already expanded, and reports the change through the IsExpanded binding, OnChange and OnExpand."
        },
        new()
        {
            Name = "Collapse",
            Type = "Task",
            Description = "Collapses the accordion. Does nothing if it is already collapsed, and reports the change through the IsExpanded binding, OnChange and OnCollapse."
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Expands the accordion if it is collapsed and collapses it if it is expanded, reporting the change through the IsExpanded binding, OnChange and OnExpand/OnCollapse."
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives the focus to the header of the accordion, so that a panel the app has just opened is also where the keyboard is standing. The overload taking a bool prevents the header from being scrolled into view."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
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
        SharedSubEnums.BitPlacement,
        new()
        {
            Id = "accordion-toggle-reason-enum",
            Name = "BitAccordionToggleReason",
            Description = "What made a BitAccordion expand or collapse.",
            Items =
            [
                new()
                {
                    Name = "Click",
                    Description = "The header of the accordion was clicked, or activated by the Enter or the Space key.",
                    Value = "0",
                },
                new()
                {
                    Name = "Method",
                    Description = "The Expand, Collapse or Toggle method of the accordion was called.",
                    Value = "1",
                },
                new()
                {
                    Name = "Reveal",
                    Description = "The browser revealed the collapsed panel of a HiddenUntilFound accordion, because find-in-page or a navigation to a fragment landed inside it.",
                    Value = "2",
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
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "accordion-toggle-args",
            Title = "BitAccordionToggleArgs",
            Parameters =
            [
                new()
                {
                    Name = "IsExpanding",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "The state the accordion is about to move to: true while it is expanding, false while it is collapsing."
                },
                new()
                {
                    Name = "Reason",
                    Type = "BitAccordionToggleReason",
                    DefaultValue = "",
                    Description = "What made the accordion expand or collapse: a click on its header, a call to one of its Expand, Collapse and Toggle methods, or a find-in-page match the browser revealed.",
                    LinkType = LinkType.Link,
                    Href = "#accordion-toggle-reason-enum",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the expansion or the collapse and leave the accordion as it is."
                }
            ]
        },
        new()
        {
            Id = "accordion-class-styles",
            Title = "BitAccordionClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAccordion."
                },
                new()
                {
                    Name = "Expanded",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the expanded state of the BitAccordion."
                },
                new()
                {
                    Name = "HeaderWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header wrapper of the BitAccordion, which holds the heading and the actions."
                },
                new()
                {
                    Name = "Heading",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the heading element of the BitAccordion that wraps the header button."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitAccordion."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon at the start of the header of the BitAccordion."
                },
                new()
                {
                    Name = "HeaderContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header content of the BitAccordion."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitAccordion."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description of the BitAccordion."
                },
                new()
                {
                    Name = "ExpanderIconWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the expander icon wrapper of the BitAccordion."
                },
                new()
                {
                    Name = "ExpanderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the expander icon of the BitAccordion."
                },
                new()
                {
                    Name = "ExpandedIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitAccordion in expanded state."
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spinner that stands in the expander's slot while the BitAccordion is busy."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions of the BitAccordion, rendered beside the header."
                },
                new()
                {
                    Name = "ContentContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content container of the BitAccordion."
                },
                new()
                {
                    Name = "ContentWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content wrapper of the BitAccordion, which clips the content while it collapses."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the BitAccordion."
                }
            ]
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Accordion-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the accordion.",
        },
        new()
        {
            Name = "--bit-Accordion-background",
            DefaultValue = "Per Background, --bit-clr-bg-pri (--bit-clr-bg-sec with NoBorder)",
            Description = "Fill of the accordion. The Background parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Accordion-border-color",
            DefaultValue = "Per Border, --bit-clr-brd-pri",
            Description = "Color of the outline. The Border parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Accordion-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Thickness of the outline.",
        },
        new()
        {
            Name = "--bit-Accordion-radius",
            DefaultValue = "--bit-shp-radius-surface",
            Description = "Corner radius of the accordion, which the header and the panel follow.",
        },
        new()
        {
            Name = "--bit-Accordion-shadow",
            DefaultValue = "none",
            Description = "Elevation of the accordion (e.g. var(--bit-shd-card)).",
        },
        new()
        {
            Name = "--bit-Accordion-font-size",
            DefaultValue = "Per Size, --bit-tpg-fs-xs/sm/md",
            Description = "Text size of the panel and the description.",
        },
        new()
        {
            Name = "--bit-Accordion-header-padding",
            DefaultValue = "Per Size",
            Description = "Padding of the header (any padding shorthand).",
        },
        new()
        {
            Name = "--bit-Accordion-header-hover-background",
            DefaultValue = "Per Background, its hover shade",
            Description = "Fill of the header under the pointer.",
        },
        new()
        {
            Name = "--bit-Accordion-header-active-background",
            DefaultValue = "The hover fill, then the Background's active shade",
            Description = "Fill of the header while pressed.",
        },
        new()
        {
            Name = "--bit-Accordion-header-expanded-background",
            DefaultValue = "transparent",
            Description = "Fill of the header while the accordion is expanded; once set, the hover and pressed shades no longer replace it.",
        },
        new()
        {
            Name = "--bit-Accordion-header-expanded-color",
            DefaultValue = "inherit",
            Description = "Text of the header while the accordion is expanded. The title, icon and expander colors win over it; nested accordions do not inherit it.",
        },
        new()
        {
            Name = "--bit-Accordion-title-color",
            DefaultValue = "inherit",
            Description = "Color of the title.",
        },
        new()
        {
            Name = "--bit-Accordion-title-font-size",
            DefaultValue = "Per Size, --bit-tpg-fs-sm/md/lg",
            Description = "Size of the title.",
        },
        new()
        {
            Name = "--bit-Accordion-title-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the title.",
        },
        new()
        {
            Name = "--bit-Accordion-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the description.",
        },
        new()
        {
            Name = "--bit-Accordion-icon-size",
            DefaultValue = "Per Size, --bit-siz-icon-sm/md/lg",
            Description = "Size of the icon, of the expander icon and of the busy spinner.",
        },
        new()
        {
            Name = "--bit-Accordion-icon-color",
            DefaultValue = "inherit",
            Description = "Color of the icon at the start of the header.",
        },
        new()
        {
            Name = "--bit-Accordion-expander-color",
            DefaultValue = "inherit",
            Description = "Color of the expander icon (or of the ExpanderTemplate content) and of the busy spinner.",
        },
        new()
        {
            Name = "--bit-Accordion-content-padding",
            DefaultValue = "Per Size",
            Description = "Padding of the open panel (any padding shorthand); its block padding animates from 0.",
        },
        new()
        {
            Name = "--bit-Accordion-content-background",
            DefaultValue = "transparent",
            Description = "Fill of the panel, drawn over the accordion's own fill.",
        },
        new()
        {
            Name = "--bit-Accordion-divider-color",
            DefaultValue = "transparent",
            Description = "Rule between the header and the open panel, as thick as the outline.",
        },
        new()
        {
            Name = "--bit-Accordion-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Keyboard focus ring of the header and of a scrolling panel.",
        },
        new()
        {
            Name = "--bit-Accordion-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text of a disabled accordion.",
        },
        new()
        {
            Name = "--bit-Accordion-disabled-background",
            DefaultValue = "--bit-clr-bg-dis",
            Description = "Fill of a disabled accordion.",
        },
    ];



    private int renameCount;

    private bool bindingIsEnabled = true;
    private bool bindingIsExpanded;
    private int controlledExpandedItem = 1;

    private int clickCount;
    private bool lastChange;
    private int expandCount;
    private int collapseCount;
    private int refusedCount;
    private bool lockAccordion;
    private void HandleOnToggling(BitAccordionToggleArgs args)
    {
        if (args.IsExpanding || lockAccordion is false) return;

        args.Cancel = true;
        refusedCount++;
    }

    private BitAccordion accordionRef = default!;

    private string[] orders = [];
    private async Task LoadOrders(BitAccordionToggleArgs args)
    {
        if (args.IsExpanding is false || orders.Length > 0) return;

        await Task.Delay(1500); // e.g. await Http.GetFromJsonAsync<string[]>("api/orders")
        orders = ["#1001 - 2 items", "#1002 - 5 items", "#1003 - 1 item"];
    }

    private int readOnlyClickCount;

    private readonly BitAccordionParams[] accordionParams =
    [
        new()
        {
            HiddenUntilFound = true,
            ExpanderIconName = BitIconName.Add,
            ExpandedExpanderIconName = BitIconName.Remove,
            ExpanderIconPlacement = BitPlacement.Start,
        }
    ];

    private BitColorKind backgroundColorKind = BitColorKind.Primary;
    private BitColorKind borderColorKind = BitColorKind.Primary;
}
