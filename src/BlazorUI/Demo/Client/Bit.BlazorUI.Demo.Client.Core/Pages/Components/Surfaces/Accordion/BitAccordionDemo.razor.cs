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
            Description = "The color kind of the background of the accordion.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the accordion.",
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
            Name = "ExpanderIconPosition",
            Type = "BitIconPosition?",
            DefaultValue = "null",
            Description = "Gets or sets the side of the header the expander icon sits on. The default value is End.",
            LinkType = LinkType.Link,
            Href = "#icon-position-enum",
        },
        new()
        {
            Name = "ExpandOnPrint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the panel of the accordion while the page is being printed, so that a collapsed section is not left out of the paper as a bare header. The scroll cap of MaxHeight is lifted along with it. Content that is not in the DOM at all - a LazyContent panel that has never been opened, a collapsed UnmountOnCollapse panel - is still printed as a bare header."
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
            Description = "Delays the first render of the content of the accordion until it is expanded for the first time. The content stays in the DOM afterwards, so the state it holds survives a collapse."
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
            Description = "Removes the region role from the panel of the accordion, leaving it a plain container. The role names the panel as a landmark, which helps a screen reader user find their way back to the content of a panel that holds headings or another accordion; the WAI-ARIA authoring practices ask for it to be dropped where it would flood the page with landmarks instead - more than about six panels that can all be open at the same time."
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
            Description = "Callback invoked before the accordion expands or collapses, letting the change be cancelled. Since the callback is awaited, it can also run asynchronous work like loading the content of the panel or asking for a confirmation first, and nothing else toggles the accordion while it is running. A change that comes from the IsExpanded parameter itself is not offered here.",
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
            Description = "Title in the header of Accordion."
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
            Description = "Removes the content of the accordion from the DOM while it is collapsed, so that nothing it holds keeps running behind a closed header. The collapse of an accordion that unmounts its content is not animated, since there is nothing left to animate."
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
        new()
        {
            Id = "icon-position-enum",
            Name = "BitIconPosition",
            Description = "Describes the placement of an icon relative to other content.",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Description = "Icon renders before the content.",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Description = "Icon renders after the content (default).",
                    Value = "1",
                }
            ]
        },
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
                    Description = "What made the accordion expand or collapse: a click on its header, or a call to one of its Expand, Collapse and Toggle methods.",
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



    private int renameCount;

    private int controlledAccordionExpandedItem = 1;

    private bool accordionToggleIsEnabled = true;
    private bool accordionToggleIsExpanded;

    private int clickCount;
    private bool lastChange;
    private int expandCount;
    private int collapseCount;

    private bool lockAccordion;
    private int refusedCount;
    private void HandleOnToggling(BitAccordionToggleArgs args)
    {
        if (args.IsExpanding || lockAccordion is false) return;

        args.Cancel = true;
        refusedCount++;
    }

    private BitAccordion accordionRef = default!;

    private int readOnlyClickCount;

    private BitColorKind backgroundColorKind = BitColorKind.Primary;
    private BitColorKind borderColorKind = BitColorKind.Primary;



    private readonly string example1RazorCode = @"
<BitAccordion Title=""Accordion"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>

<BitAccordion Title=""Expanded by default"" DefaultIsExpanded>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example2RazorCode = @"
<BitAccordion Title=""Accordion 1"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
    when possibilities are limitless, waiting for content to emerge.
</BitAccordion>
<BitAccordion Title=""Accordion 2"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>
<BitAccordion Title=""Accordion 3"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come.
</BitAccordion>";

    private readonly string example3RazorCode = @"
<BitAccordion Title=""General settings"" Description=""The general settings of the application"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>";

    private readonly string example4RazorCode = @"
<BitAccordion Title=""General settings"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""Users"" IconName=""@BitIconName.People"" Description=""You are currently not an owner"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example5RazorCode = @"
<BitAccordion Title=""ExpanderIconName"" ExpanderIconName=""ChevronDown"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""ExpanderIcon"" ExpanderIcon=""@BitIconInfo.Bit(""ChevronDownEnd"")"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""Start"" ExpanderIconPosition=""BitIconPosition.Start"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Start with an icon"" IconName=""@BitIconName.Settings"" ExpanderIconPosition=""BitIconPosition.Start"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""HideExpanderIcon"" HideExpanderIcon>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""NoExpanderRotation"" ExpanderIconName=""ChevronDown"" NoExpanderRotation>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""ExpandedExpanderIconName"" ExpanderIconName=""Add"" ExpandedExpanderIconName=""Remove"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example6RazorCode = @"
<BitAccordion Title=""Project settings"" Description=""@($""Renamed {renameCount} times"")"">
    <Actions>
        <BitButton Variant=""BitVariant.Text""
                   IconName=""@BitIconName.Rename""
                   Title=""Rename""
                   OnClick=""() => renameCount++"" />
        <BitButton Variant=""BitVariant.Text""
                   Color=""BitColor.Error""
                   IconName=""@BitIconName.Delete""
                   Title=""Delete"" />
    </Actions>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>";
    private readonly string example6CsharpCode = @"
private int renameCount;";

    private readonly string example7RazorCode = @"
<BitAccordion Title=""General settings""
              Description=""I am an accordion""
              OnClick=""() => controlledAccordionExpandedItem = controlledAccordionExpandedItem == 1 ? 0 : 1""
              IsExpanded=""controlledAccordionExpandedItem == 1"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    Each word carried meaning, each pause brought understanding.
</BitAccordion>
<BitAccordion Title=""Users""
              Description=""You are currently not an owner""
              OnClick=""() => controlledAccordionExpandedItem = controlledAccordionExpandedItem == 2 ? 0 : 2""
              IsExpanded=""controlledAccordionExpandedItem == 2"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>
<BitAccordion Title=""Advanced settings""
              Description=""Filtering has been entirely disabled for whole web server""
              OnClick=""() => controlledAccordionExpandedItem = controlledAccordionExpandedItem == 3 ? 0 : 3""
              IsExpanded=""controlledAccordionExpandedItem == 3"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come.
</BitAccordion>";
    private readonly string example7CsharpCode = @"
private int controlledAccordionExpandedItem = 1;";

    private readonly string example8RazorCode = @"
<BitToggle @bind-Value=""accordionToggleIsEnabled"" OnText=""Enabled"" OffText=""Disabled"" />

<BitToggle @bind-Value=""accordionToggleIsExpanded"" OnText=""Expanded"" OffText=""Collapsed"" />

<BitAccordion Title=""Accordion""
              Description=""I am an accordion""
              IsEnabled=""accordionToggleIsEnabled""
              @bind-IsExpanded=""accordionToggleIsExpanded"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>";
    private readonly string example8CsharpCode = @"
private bool accordionToggleIsEnabled = true;
private bool accordionToggleIsExpanded;";

    private readonly string example9RazorCode = @"
<BitAccordion Title=""Accordion""
              Description=""I am an accordion""
              OnClick=""() => clickCount++""
              OnChange=""(bool v) => lastChange = v""
              OnExpand=""() => expandCount++""
              OnCollapse=""() => collapseCount++"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<div>Clicks: <b>@clickCount</b></div>
<div>Last OnChange: <b>@lastChange</b></div>
<div>Expanded: <b>@expandCount</b> times, collapsed: <b>@collapseCount</b> times</div>";
    private readonly string example9CsharpCode = @"
private int clickCount;
private bool lastChange;
private int expandCount;
private int collapseCount;";

    private readonly string example10RazorCode = @"
<BitToggle @bind-Value=""lockAccordion"" OnText=""Locked open"" OffText=""Unlocked"" />

<BitAccordion Title=""Unsaved changes""
              Description=""@(lockAccordion ? ""Unlock to close this panel"" : ""Free to close"")""
              DefaultIsExpanded
              OnToggling=""HandleOnToggling"">
    The collapse of this panel is refused while it is locked, the way a panel holding a form that has not
    been filled in yet would refuse to close on the reader.
</BitAccordion>

<div>Refused: <b>@refusedCount</b> times</div>";
    private readonly string example10CsharpCode = @"
private bool lockAccordion;
private int refusedCount;
private void HandleOnToggling(BitAccordionToggleArgs args)
{
    if (args.IsExpanding || lockAccordion is false) return;

    args.Cancel = true;
    refusedCount++;
}";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => accordionRef.Expand()"">Expand</BitButton>
<BitButton OnClick=""() => accordionRef.Collapse()"">Collapse</BitButton>
<BitButton OnClick=""() => accordionRef.Toggle()"">Toggle</BitButton>

<BitAccordion @ref=""accordionRef"" Title=""Accordion"" Description=""I am an accordion"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";
    private readonly string example11CsharpCode = @"
private BitAccordion accordionRef = default!;";

    private readonly string example12RazorCode = @"
<BitAccordion Title=""LazyContent"" LazyContent>
    <BitTextField Placeholder=""Kept after a collapse..."" />
</BitAccordion>

<BitAccordion Title=""UnmountOnCollapse"" UnmountOnCollapse>
    <BitTextField Placeholder=""Thrown away on a collapse..."" />
</BitAccordion>";

    private readonly string example13RazorCode = @"
<BitAccordion Title=""Accordion"" MaxHeight=""10rem"" DefaultIsExpanded>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>";

    private readonly string example14RazorCode = @"
<BitAccordion Title=""Slow (1000ms)"" TransitionDuration=""1000"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""Instant (0)"" TransitionDuration=""0"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example15RazorCode = @"
<BitAccordion IconName=""@BitIconName.Settings"" Description=""I am an accordion"">
    <TitleTemplate>
        <BitStack Horizontal FitWidth AutoHeight Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
            <span>Advanced settings</span>
            <BitIcon IconName=""@BitIconName.Info"" Color=""BitColor.Info"" />
        </BitStack>
    </TitleTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>

<style>
    .custom-header {
        gap: 1rem;
        flex-grow: 1;
        display: flex;
        line-height: 1.5;
        align-items: center;
    }

    .custom-title {
        color: #0054C6;
    }

    .custom-desc {
        color: brown;
    }
</style>

<BitAccordion>
    <HeaderTemplate Context=""isExpanded"">
        <BitIcon IconName=""@(isExpanded ? BitIconName.ChevronDown : BitIconName.ChevronRight)"" />
        <div class=""custom-header"">
            <span class=""custom-title"">Accordion 1</span>
            <span class=""custom-desc"">I am an accordion</span>
        </div>
    </HeaderTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
        These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </Body>
</BitAccordion>

<BitAccordion Title=""Nature"" Description=""I am an accordion"">
    <BitCarousel AnimationDuration=""1"">
        <BitCarouselItem>
            <img src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"">
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" />
        </BitCarouselItem>
    </BitCarousel>
</BitAccordion>";

    private readonly string example16RazorCode = @"
<BitAccordion Title=""Under an h2"" HeadingLevel=""3"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.

    <BitLink Href=""/components/accordion"">A link the Tab key only reaches while this panel is open.</BitLink>
</BitAccordion>

<BitAccordion Title=""Under an h3"" HeadingLevel=""4"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion HeaderAriaLabel=""Notifications"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Ringer"" />
    </HeaderTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </Body>
</BitAccordion>

<BitAccordion Title=""Nested (aria-level 3)"" NoContentRegion DefaultIsExpanded>
    The accordion below is announced one level under this one, and neither of the two panels is a landmark.

    <BitAccordion Title=""Nested (aria-level 4)"" NoContentRegion>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</BitAccordion>";

    private readonly string example17RazorCode = @"
<BitAccordion Title=""Accordion"" NoBorder>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>";

    private readonly string example18RazorCode = @"
<BitAccordion Title=""Printed with its content"" ExpandOnPrint>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>

<BitAccordion Title=""Printed as a bare header"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    These placeholder words symbolize the beginning-a moment of possibility where creativity has yet to take shape.
</BitAccordion>";

    private readonly string example19RazorCode = @"
<BitAccordion Title=""Read-only""
              Description=""@($""Clicked {readOnlyClickCount} times, still open"")""
              OnClick=""() => readOnlyClickCount++""
              ReadOnly
              DefaultIsExpanded>
    This panel answers no click and no key, but it is not greyed out: it is open on purpose and nothing
    about it is unavailable.
</BitAccordion>

<BitAccordion Title=""Disabled"" Description=""Turned off altogether"" IsEnabled=""false"" DefaultIsExpanded>
    This one is greyed out and its header is out of the tab order.
</BitAccordion>";

    private readonly string example19CsharpCode = @"
private int readOnlyClickCount;";

    private readonly string example20RazorCode = @"
<BitChoiceGroup @bind-Value=""backgroundColorKind"" Horizontal
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<div style=""padding:2rem;background:gray"">
    <BitAccordion Title=""Accordion"" Background=""backgroundColorKind"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordion>
</div>

<BitChoiceGroup @bind-Value=""borderColorKind"" Horizontal
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitAccordion Title=""Accordion"" Border=""borderColorKind"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";
    private readonly string example20CsharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitAccordion Title=""Chevron Down"" ExpanderIcon=""@(""fa-solid fa-chevron-down"")"">
    ExpanderIcon=@(""fa-solid fa-chevron-down"")
</BitAccordion>

<BitAccordion Title=""Chevron Right"" ExpanderIcon=""@BitIconInfo.Css(""fa-solid fa-chevron-right"")"">
    ExpanderIcon=""@BitIconInfo.Css(""fa-solid fa-chevron-right"")""
</BitAccordion>

<BitAccordion Title=""Angle Down"" ExpanderIcon=""@BitIconInfo.Fa(""solid angle-down"")"">
    ExpanderIcon=""@BitIconInfo.Fa(""solid angle-down"")""
</BitAccordion>

<BitAccordion Title=""Gear"" Icon=""@BitIconInfo.Fa(""solid gear"")"" ExpanderIcon=""@BitIconInfo.Fa(""solid caret-down"")"">
    Icon=""@BitIconInfo.Fa(""solid gear"")""
</BitAccordion>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitAccordion Title=""Chevron Down"" ExpanderIcon=""@(""bi bi-chevron-down"")"">
    ExpanderIcon=@(""bi bi-chevron-down"")
</BitAccordion>

<BitAccordion Title=""Chevron Right"" ExpanderIcon=""@BitIconInfo.Css(""bi bi-chevron-right"")"">
    ExpanderIcon=""@BitIconInfo.Css(""bi bi-chevron-right"")""
</BitAccordion>

<BitAccordion Title=""Arrow Down"" ExpanderIcon=""@BitIconInfo.Bi(""arrow-down"")"">
    ExpanderIcon=""@BitIconInfo.Bi(""arrow-down"")""
</BitAccordion>

<BitAccordion Title=""Gear"" Icon=""@BitIconInfo.Bi(""gear"")"" ExpanderIcon=""@BitIconInfo.Bi(""caret-down-fill"")"">
    Icon=""@BitIconInfo.Bi(""gear"")""
</BitAccordion>";

    private readonly string example22RazorCode = @"
<BitAccordion Title=""Small"" Size=""BitSize.Small"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""Medium"" Size=""BitSize.Medium"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""Large"" Size=""BitSize.Large"" IconName=""@BitIconName.Settings"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example23RazorCode = @"
<style>
    .custom-class {
        border-color: blueviolet;
        background-color: blanchedalmond;
    }

    .custom-acd-title {
        color: tomato;
        font-style: italic;
    }

    .custom-acd-content {
        color: darkslateblue;
    }
</style>

<BitAccordion Title=""Style"" Style=""border-color: var(--bit-clr-pri); border-width: 2px;"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Class"" Class=""custom-class"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>

<BitAccordion Title=""Styles""
              Description=""I am an accordion""
              Styles=""@(new() { Header = ""background: var(--bit-clr-bg-sec);"",
                                Title = ""color: tomato;"",
                                ExpanderIcon = ""color: tomato;"",
                                Content = ""font-style: italic;"" })"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>
<BitAccordion Title=""Classes""
              Description=""I am an accordion""
              Classes=""@(new() { Title = ""custom-acd-title"", Content = ""custom-acd-content"" })"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
</BitAccordion>";

    private readonly string example24RazorCode = @"
<BitAccordion Dir=""BitDir.Rtl""
              Title=""تنظیمات""
              IconName=""@BitIconName.Settings""
              Description=""من یک آکاردئون هستم!"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
</BitAccordion>";
}
