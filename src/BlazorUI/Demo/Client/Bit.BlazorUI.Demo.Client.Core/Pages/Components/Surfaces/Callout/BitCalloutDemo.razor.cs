namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Callout;

public partial class BitCalloutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the anchor element of the callout. The anchor is rendered as a plain container, so the content given here should hold the focusable element the user activates.",
        },
        new()
        {
            Name = "AnchorEl",
            Type = "Func<ElementReference>?",
            DefaultValue = "null",
            Description = "The setter function for element reference to the external anchor element."
        },
        new()
        {
            Name = "AnchorId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the external anchor element."
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "Closes the callout as soon as a click lands anywhere inside it."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the focus into the callout as soon as it opens, to its first focusable element, or to the callout itself when it holds none."
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the callout.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the callout.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the callout."
        },
        new()
        {
            Name = "Classes",
            Type = "BitCalloutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the callout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for ChildContent."
        },
        new()
        {
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial opening state of the callout in the uncontrolled mode, which is when the IsOpen parameter is not set."
        },
        new()
        {
            Name = "Direction",
            Type = "BitDropDirection?",
            DefaultValue = "null",
            Description = "Determines the allowed directions in which the callout should decide to be opened.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum"
        },
        new()
        {
            Name = "FixedCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Forces the callout to preserve its component's original width."
        },
        new()
        {
            Name = "FooterId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the footer element that renders at the end of the scrolling container of the callout content."
        },
        new()
        {
            Name = "Gap",
            Type = "int",
            DefaultValue = "0",
            Description = "The distance in pixels between the anchor and the callout, on whichever side the callout ends up being placed."
        },
        new()
        {
            Name = "HeaderId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the header element that renders at the top of the scrolling container of the callout content."
        },
        new()
        {
            Name = "HoverCloseDelay",
            Type = "int",
            DefaultValue = "150",
            Description = "The delay in milliseconds before the callout closes once the pointer leaves the callout and its anchor in the OpenOnHover mode."
        },
        new()
        {
            Name = "HoverOpenDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the callout opens once the pointer enters the anchor in the OpenOnHover mode."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout."
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum height of the callout as a CSS value, beyond which its content scrolls."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the callout as a CSS value, beyond which its content wraps."
        },
        new()
        {
            Name = "MaxWindowWidth",
            Type = "int?",
            DefaultValue = "null",
            Description = "The max window width to consider when calculating the position of the callout before opening."
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The minimum width of the callout as a CSS value, so that a narrow content does not end up in a cramped callout."
        },
        new()
        {
            Name = "Modal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Dims the page behind the callout, so that the callout reads as the only thing in play."
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Escape key from dismissing the callout."
        },
        new()
        {
            Name = "NoDismissOnOutsideClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the callout open when a click lands outside of it, and when the page is scrolled or resized under it."
        },
        new()
        {
            Name = "NoShadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the box-shadow from the callout."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is called when the callout is dismissed."
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is called when the callout is opened."
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback that is called when the callout opens or closes."
        },
        new()
        {
            Name = "OpenOnHover",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the callout when the pointer enters the anchor and closes it when the pointer leaves both the anchor and the callout."
        },
        new()
        {
            Name = "PanelPosition",
            Type = "BitPanelPosition?",
            DefaultValue = "null",
            Description = "The edge of the screen the responsive panel slides in from, for a ResponsiveMode of Panel. It defaults to End.",
            LinkType = LinkType.Link,
            Href = "#panel-position-enum"
        },
        new()
        {
            Name = "ResponsiveMode",
            Type = "BitResponsiveMode?",
            DefaultValue = "null",
            Description = "Configures the responsive mode of the callout for the small screens.",
            LinkType = LinkType.Link,
            Href = "#responsive-mode-enum"
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role of the callout. It defaults to dialog for a callout that traps the focus, and to nothing for the others."
        },
        new()
        {
            Name = "ScrollContainerId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element which needs to be scrollable in the content of the callout."
        },
        new()
        {
            Name = "ScrollOffset",
            Type = "int?",
            DefaultValue = "null",
            Description = "The vertical offset of the scroll container to consider in the positioning and height calculation of the callout."
        },
        new()
        {
            Name = "SetCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Forces the callout to set its content container width while opening based on the available space and actual content."
        },
        new()
        {
            Name = "ShowArrow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws an arrow on the edge of the callout that faces the anchor, pointing at it."
        },
        new()
        {
            Name = "Side",
            Type = "BitCalloutSide?",
            DefaultValue = "null",
            Description = "The side of the anchor the callout is placed on when there is room for it there. It wins over Direction, falls back to the opposite side, and then to Direction.",
            LinkType = LinkType.Link,
            Href = "#callout-side-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitCalloutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the callout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "TrapFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the keyboard inside the callout while it is open and reports it as a modal dialog to the screen readers. It implies AutoFocus."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the callout as a CSS value. SetCalloutWidth and FixedCalloutWidth take precedence over it."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCalloutClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCallout."
                },
                new()
                {
                    Name = "AnchorContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the anchor container element of the BitCallout."
                },
                new()
                {
                    Name = "Arrow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the arrow (beak) element of the BitCallout."
                },
                new()
                {
                    Name = "Opened",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the opened callout state of the BitCallout."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the BitCallout."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitCallout."
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "All",
                    Value = "0",
                    Description = "The direction determined automatically based on the available spaces in all directions."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "1",
                    Description = "The direction determined automatically based on the available spaces in only top and bottom directions."
                },
            ]
        },
        new()
        {
            Id = "responsive-mode-enum",
            Name = "BitResponsiveMode",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "Disables the responsive mode."
                },
                new()
                {
                    Name = "Panel",
                    Value = "1",
                    Description = "Enables the panel responsive mode, whose edge comes from the PanelPosition parameter."
                },
                new()
                {
                    Name = "Top",
                    Value = "2",
                    Description = "Enables the responsive mode as a sheet that comes down from the top of the screen."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "3",
                    Description = "Enables the responsive mode as a sheet that comes up from the bottom of the screen."
                },
            ]
        },
        new()
        {
            Id = "callout-side-enum",
            Name = "BitCalloutSide",
            Description = "",
            Items =
            [
                new() { Name = "Top", Value = "0", Description = "Above the anchor." },
                new() { Name = "Bottom", Value = "1", Description = "Below the anchor." },
                new() { Name = "Start", Value = "2", Description = "Beside the anchor, on the side the content starts from - the left in a left-to-right layout." },
                new() { Name = "End", Value = "3", Description = "Beside the anchor, on the side the content ends at - the right in a left-to-right layout." },
            ]
        },
        new()
        {
            Id = "panel-position-enum",
            Name = "BitPanelPosition",
            Description = "",
            Items =
            [
                new() { Name = "Start", Value = "0", Description = "The panel slides in from the start edge of the screen." },
                new() { Name = "End", Value = "1", Description = "The panel slides in from the end edge of the screen." },
                new() { Name = "Top", Value = "2", Description = "The panel slides in from the top edge of the screen." },
                new() { Name = "Bottom", Value = "3", Description = "The panel slides in from the bottom edge of the screen." },
            ]
        },
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "",
            Items =
            [
                new() { Name = "Primary", Value = "0", Description = "The primary color kind." },
                new() { Name = "Secondary", Value = "1", Description = "The secondary color kind." },
                new() { Name = "Tertiary", Value = "2", Description = "The tertiary color kind." },
                new() { Name = "Transparent", Value = "3", Description = "The transparent color kind." },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the callout programmatically, unless it is disabled.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the callout programmatically.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the callout to open/close it.",
        }
    ];



    private ElementReference anchorEl = default!;
    private BitCallout callout1 = default!;
    private BitCallout callout2 = default!;
    private BitCallout callout3 = default!;

    private bool isOpen;
    private int openCount;
    private int toggleCount;
    private int dismissCount;
    private string autoCloseAction = "none";



    private readonly string example1RazorCode = @"
<BitCallout>
    <Anchor>
        <BitButton>Show callout</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This is the callout content.
        </div>
    </Content>
</BitCallout>

<BitCallout IsEnabled=""false"">
    <Anchor>
        <BitButton IsEnabled=""false"">Disabled</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This is the callout content.
        </div>
    </Content>
</BitCallout>";

    private readonly string example2RazorCode = @"
<BitButton Id=""anchor_id"" OnClick=""() => callout1.Toggle()"">AnchorId</BitButton>
<BitCallout AnchorId=""anchor_id"" @ref=""callout1"">
    <div class=""callout-content"">
        <BitCalendar />
    </div>
</BitCallout>

<button @ref=""anchorEl"" @onclick=""() => callout2.Toggle()"">AnchorEl</button>
<BitCallout AnchorEl=""() => anchorEl"" @ref=""callout2"">
    <div class=""callout-content"">
        <BitCalendar />
    </div>
</BitCallout>";
    private readonly string example2CsharpCode = @"
private ElementReference anchorEl;
private BitCallout callout1;
private BitCallout callout2;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => isOpen = true"">Show callout</BitButton>

<BitCallout @bind-IsOpen=""isOpen"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Anchor</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This is the callout content.
            <br />
            You can even close it from here!
            <br /><br />
            <div>
                <BitButton OnClick=""() => isOpen = false"">Done</BitButton>
                <BitButton OnClick=""() => isOpen = false"" Variant=""BitVariant.Outline"">Cancel</BitButton>
            </div>
        </div>
    </Content>
</BitCallout>

<BitCallout DefaultIsOpen=""false"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">DefaultIsOpen</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This callout owns its own open state.
        </div>
    </Content>
</BitCallout>";
    private readonly string example3CsharpCode = @"
private bool isOpen;";

    private readonly string example4RazorCode = @"
<BitCallout OpenOnHover Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Hover me</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This callout opened on hover.
        </div>
    </Content>
</BitCallout>

<BitCallout OpenOnHover Gap=""8"" HoverOpenDelay=""500"" HoverCloseDelay=""500"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Delayed (500ms)</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Both the opening and the closing waited half a second.
        </div>
    </Content>
</BitCallout>";

    private readonly string example5RazorCode = @"
<BitCallout Direction=""BitDropDirection.TopAndBottom"">
    <Anchor>
        <BitButton>TopAndBottom</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 13; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout Direction=""BitDropDirection.All"">
    <Anchor>
        <BitButton>All</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 13; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout Side=""BitCalloutSide.Top"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Side: Top</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Placed above the anchor whenever there is room above it.
        </div>
    </Content>
</BitCallout>

<BitCallout Side=""BitCalloutSide.End"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Side: End</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Placed beside the anchor, on the side the content ends at.
        </div>
    </Content>
</BitCallout>

<BitCallout Gap=""16"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Gap of 16px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This callout keeps 16px away from its anchor.
        </div>
    </Content>
</BitCallout>";

    private readonly string example6RazorCode = @"
<BitCallout ShowArrow Gap=""8"">
    <Anchor>
        <BitButton>With an arrow</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This callout points at its anchor.
        </div>
    </Content>
</BitCallout>

<BitCallout ShowArrow Gap=""8"" Border=""BitColorKind.Secondary"" Background=""BitColorKind.Secondary"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Arrow with a border</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            The beak takes the border of the callout too.
        </div>
    </Content>
</BitCallout>";

    private readonly string example7RazorCode = @"
<BitCallout Width=""20rem"">
    <Anchor>
        <BitButton>Width</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A callout of a fixed width.</div>
    </Content>
</BitCallout>

<BitCallout MaxWidth=""16rem"">
    <Anchor>
        <BitButton>MaxWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            A long line of text that wraps inside the callout instead of stretching it across the page.
        </div>
    </Content>
</BitCallout>

<BitCallout MaxHeight=""12rem"" MinWidth=""14rem"">
    <Anchor>
        <BitButton>MaxHeight</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 25; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout SetCalloutWidth>
    <Anchor>
        <BitButton>A wide anchor with SetCalloutWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Short content, anchor-wide callout.</div>
    </Content>
</BitCallout>

<BitCallout FixedCalloutWidth>
    <Anchor>
        <BitButton>A wide anchor with FixedCalloutWidth</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">The callout is exactly as wide as its anchor.</div>
    </Content>
</BitCallout>";

    private readonly string example8RazorCode = @"
<style>
    .scroller {
        display: flex;
        max-width: 16rem;
        flex-direction: column;
    }

    .scroller-body {
        overflow: auto;
    }

    .scroller-bar {
        padding: 0.5rem 0;
        font-weight: 600;
    }
</style>

<BitCallout ScrollContainerId=""scroller-container"" HeaderId=""scroller-header"" FooterId=""scroller-footer"">
    <Anchor>
        <BitButton>Show callout</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content scroller"">
            <div id=""scroller-header"" class=""scroller-bar"">A header that stays put</div>
            <div id=""scroller-container"" class=""scroller-body"">
                @for (int i = 1; i < 69; i++)
                {
                    <div>Callout content @i</div>
                }
            </div>
            <div id=""scroller-footer"" class=""scroller-bar"">A footer that stays put</div>
        </div>
    </Content>
</BitCallout>";

    private readonly string example9RazorCode = @"
<BitCallout ResponsiveMode=""BitResponsiveMode.Panel"" PanelPosition=""BitPanelPosition.End"">
    <Anchor>
        <BitButton>End panel</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle1"">Filters</BitText>
                <BitCheckbox Label=""Active"" />
                <BitCheckbox Label=""Archived"" />
                <BitCheckbox Label=""Draft"" />
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout ResponsiveMode=""BitResponsiveMode.Panel"" PanelPosition=""BitPanelPosition.Start"">
    <Anchor>
        <BitButton>Start panel</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content""> ... </div>
    </Content>
</BitCallout>

<BitCallout ResponsiveMode=""BitResponsiveMode.Top"">
    <Anchor>
        <BitButton>Top sheet</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content""> ... </div>
    </Content>
</BitCallout>

<BitCallout ResponsiveMode=""BitResponsiveMode.Bottom"">
    <Anchor>
        <BitButton>Bottom sheet</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content""> ... </div>
    </Content>
</BitCallout>";

    private readonly string example10RazorCode = @"
<BitCallout Background=""BitColorKind.Secondary"">
    <Anchor>
        <BitButton>Background</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A secondary background.</div>
    </Content>
</BitCallout>

<BitCallout Border=""BitColorKind.Primary"">
    <Anchor>
        <BitButton>Border</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">A primary border.</div>
    </Content>
</BitCallout>

<BitCallout NoShadow Border=""BitColorKind.Secondary"">
    <Anchor>
        <BitButton>NoShadow</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">No elevation, only a border.</div>
    </Content>
</BitCallout>";

    private readonly string example11RazorCode = @"
<BitCallout Modal>
    <Anchor>
        <BitButton>Modal</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">The page behind is dimmed.</div>
    </Content>
</BitCallout>

<BitCallout Modal TrapFocus>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Modal with TrapFocus</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <BitTextField Label=""Name"" />
                <BitTextField Label=""Email"" />
                <BitButton>Submit</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>";

    private readonly string example12RazorCode = @"
<BitCallout AutoFocus>
    <Anchor>
        <BitButton>AutoFocus</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <BitTextField Label=""Name"" />
                <BitButton>Submit</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout TrapFocus>
    <Anchor>
        <BitButton>TrapFocus</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <BitTextField Label=""Name"" />
                <BitTextField Label=""Email"" />
                <BitButton>Submit</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout Role=""status"" AriaLabel=""Sync status"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Role & AriaLabel</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Everything is up to date.</div>
    </Content>
</BitCallout>";

    private readonly string example13RazorCode = @"
<BitCallout AutoClose>
    <Anchor>
        <BitButton>AutoClose (@autoCloseAction)</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.25rem"">
                <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Renamed"")"">Rename</BitButton>
                <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Duplicated"")"">Duplicate</BitButton>
                <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Deleted"")"">Delete</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout NoDismissOnEscape NoDismissOnOutsideClick @ref=""callout3"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">No auto dismiss</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Neither Escape nor an outside click closes this one.
            <br /><br />
            <BitButton OnClick=""() => callout3.Close()"">Close</BitButton>
        </div>
    </Content>
</BitCallout>";
    private readonly string example13CsharpCode = @"
private BitCallout callout3;
private string autoCloseAction = ""none"";";

    private readonly string example14RazorCode = @"
<BitCallout OnToggle=""v => toggleCount++"" OnOpen=""() => openCount++"" OnDismiss=""() => dismissCount++"">
    <Anchor>
        <BitButton>Show callout</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<div>Toggled: @toggleCount, Opened: @openCount, Dismissed: @dismissCount</div>";
    private readonly string example14CsharpCode = @"
private int openCount;
private int toggleCount;
private int dismissCount;";

    private readonly string example15RazorCode = @"
<style>
    .custom-class {
        border-radius: 4px;
        box-shadow: dodgerblue 0 0 8px;
    }

    .custom-content {
        padding: 1rem;
        color: white;
        border-radius: 4px;
        background-color: darkviolet;
    }

    .custom-arrow {
        background-color: darkviolet;
    }

    .custom-anchor {
        color: white;
        cursor: pointer;
        padding: 8px 16px;
        border-radius: 4px;
        background-color: darkviolet;
    }
</style>


<BitCallout Style=""background-color: #ff634733; border-radius: 4px;"">
    <Anchor>
        <BitButton Color=""BitColor.Error"">Component's Style</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<BitCallout Class=""custom-class"">
    <Anchor>
        <BitButton>Component's Class</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>


<BitCallout Styles=""@(new() { Root = ""--anchor-color: #2e8b5775;"",
                              Opened = ""--anchor-color: #04cb5b75;"",
                              AnchorContainer = ""background-color: var(--anchor-color); border-radius: 4px;"",
                              Content = ""border: 2px solid #04cb5b75;"" })"">
    <Anchor>
        <BitActionButton>Styles</BitActionButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<BitCallout ShowArrow Gap=""8"" Classes=""@(new() { Content = ""custom-content"",
                                                 Arrow = ""custom-arrow"",
                                                 AnchorContainer = ""custom-anchor"" })"">
    <Anchor>
        Classes
    </Anchor>
    <Content>
        This is the callout content.
    </Content>
</BitCallout>";

    private readonly string example16RazorCode = @"
<BitCallout Dir=""BitDir.Rtl"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton>نمایش کال‌اوت</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            این محتوای کال‌اوت است.
        </div>
    </Content>
</BitCallout>

<BitCallout Dir=""BitDir.Rtl"" Direction=""BitDropDirection.All"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">همه جهت‌ها</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 13; i++)
            {
                <div>محتوای کال‌اوت @i</div>
            }
        </div>
    </Content>
</BitCallout>";
}
