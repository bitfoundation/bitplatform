namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Callout;

public partial class BitCalloutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitSideAlignment?",
            DefaultValue = "null",
            Description = "How the callout is lined up with its anchor along the axis it is not placed on. It defaults to Start.",
            LinkType = LinkType.Link,
            Href = "#side-alignment-enum"
        },
        new()
        {
            Name = "AlignmentOffset",
            Type = "int",
            DefaultValue = "0",
            Description = "The distance in pixels the callout is slid along the axis it is aligned on, inwards from the edge of the anchor the Alignment lined it up with. A centered callout has no edge for it to run from."
        },
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
            Name = "ArrowPadding",
            Type = "int?",
            DefaultValue = "null",
            Description = "The distance in pixels the arrow drawn by ShowArrow is kept away from the corners of the callout, so that the rounding never cuts it. It defaults to 16, and never drops below the size of the arrow itself."
        },
        new()
        {
            Name = "ArrowSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size in pixels of the arrow drawn by ShowArrow, which is the length of the side of the square the beak is cut out of. It defaults to 12."
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
            Name = "CollisionPadding",
            Type = "int",
            DefaultValue = "0",
            Description = "The distance in pixels the callout keeps from the edges of the screen when it is placed and when it is slid back onto it."
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
            Description = "Holds the callout to the width of its anchor, so that a content wider than the anchor wraps inside it instead of stretching it."
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of a footer that stays at the bottom of the callout while the rest of it scrolls."
        },
        new()
        {
            Name = "FooterId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the footer element that renders at the end of the scrolling container of the callout content. It wins over the Footer parameter."
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
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of a header that stays at the top of the callout while the rest of it scrolls."
        },
        new()
        {
            Name = "HeaderId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the header element that renders at the top of the scrolling container of the callout content. It wins over the Header parameter."
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
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of the callout out of the page until the callout is opened for the first time. Once rendered it stays, so whatever state the content holds survives the callout closing."
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
            Description = "The window width in pixels below which the callout is allowed to hang off the end of the screen rather than being slid back onto it."
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
            Description = "Dims the page behind the callout and holds it still while the callout is open, so that the callout reads as the only thing in play."
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
            Name = "NoFlip",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the callout on the Side it was asked for even when there is not enough room for it there, instead of flipping it to the opposite side."
        },
        new()
        {
            Name = "NoOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page its own clicks while the callout is open, by not rendering the overlay that otherwise covers it. A Modal callout keeps its overlay."
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
            Type = "BitSide?",
            DefaultValue = "null",
            Description = "The edge of the screen the responsive panel slides in from, for a ResponsiveMode of Panel. It defaults to End.",
            LinkType = LinkType.Link,
            Href = "#side-enum"
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
            Description = "Widens the callout to at least the width of its anchor, so that a callout with little in it still reads as belonging to what it was opened from."
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
            Type = "BitSide?",
            DefaultValue = "null",
            Description = "The side of the anchor the callout is placed on when there is room for it there. It wins over Direction, falls back to the opposite side, and then to Direction.",
            LinkType = LinkType.Link,
            Href = "#side-enum"
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
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header element of the BitCallout, which is rendered when the Header parameter is set."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the scrolling body element of the BitCallout, which is rendered when the Header or the Footer parameter is set."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer element of the BitCallout, which is rendered when the Footer parameter is set."
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
            Id = "side-enum",
            Name = "BitSide",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Value = "0",
                    Description = "The top edge."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "1",
                    Description = "The bottom edge."
                },
                new()
                {
                    Name = "Start",
                    Value = "2",
                    Description = "The edge the reading direction starts from - the left in LTR, the right in RTL."
                },
                new()
                {
                    Name = "End",
                    Value = "3",
                    Description = "The edge the reading direction ends at - the right in LTR, the left in RTL."
                },
                new()
                {
                    Name = "Left",
                    Value = "4",
                    Description = "The left edge, in both reading directions."
                },
                new()
                {
                    Name = "Right",
                    Value = "5",
                    Description = "The right edge, in both reading directions."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "6",
                    Description = "Both edges of the block axis at once."
                },
                new()
                {
                    Name = "StartAndEnd",
                    Value = "7",
                    Description = "Both edges of the inline axis at once, following the reading direction the way Start and End do."
                }
            ]
        },
        new()
        {
            Id = "side-alignment-enum",
            Name = "BitSideAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                    Description = "Lined up with the start of the side: the left edge in LTR (the right in RTL) for a surface above or below its anchor, the top edge for one beside it."
                },
                new()
                {
                    Name = "Center",
                    Value = "1",
                    Description = "Centered along the side."
                },
                new()
                {
                    Name = "End",
                    Value = "2",
                    Description = "Lined up with the end of the side: the right edge in LTR (the left in RTL) for a surface above or below its anchor, the bottom edge for one beside it."
                }
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
            Name = "OpenAt",
            Type = "Task",
            Description = "Opens the callout at a point on the screen rather than against an anchor, which is what a context menu needs. It takes the coordinates (double x, double y) or the MouseEventArgs they came from, and moves an already open callout to the new point.",
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
        },
        new()
        {
            Name = "Reposition",
            Type = "Task",
            Description = "Lays the open callout out again against what it is placed on, without reopening it or replaying its entry animation. It is for what the callout cannot see on its own: a content that has grown or shrunk, or an anchor moved by something other than a resize of it.",
        }
    ];



    private ElementReference anchorEl = default!;
    private BitCallout callout1 = default!;
    private BitCallout callout2 = default!;
    private BitCallout callout3 = default!;
    private BitCallout callout4 = default!;
    private BitCallout contextCallout = default!;

    private bool isOpen;
    private DateTimeOffset? lazyDate;
    private DateTimeOffset? eagerDate;
    private int openCount;
    private int toggleCount;
    private int dismissCount;
    private string autoCloseAction = "none";
    private string contextAction = "none";
    private int repositionRows = 2;
    private bool repositionAfterRender;

    private void AddRepositionRow()
    {
        repositionRows++;

        // The callout is laid out against what is actually in it, so the reposition waits for the render
        // that puts the new row there rather than measuring the content the callout still holds.
        repositionAfterRender = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (repositionAfterRender)
        {
            repositionAfterRender = false;

            await callout4.Reposition();
        }
    }



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

<BitCallout NoOverlay>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">NoOverlay</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            The page keeps its own clicks while this callout is open.
        </div>
    </Content>
</BitCallout>

<BitCallout DefaultIsOpen=""true"" NoOverlay>
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">DefaultIsOpen</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            This callout owns its own open state.
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
<style>
    .context-area {
        padding: 2rem;
        border-radius: 4px;
        border: 1px dashed gray;
    }
</style>

<div class=""context-area"" @oncontextmenu=""e => contextCallout.OpenAt(e)"" @oncontextmenu:preventDefault>
    Right-click anywhere in here
</div>

<BitCallout AutoClose NoOverlay MinWidth=""12rem"" @ref=""contextCallout"">
    <div class=""callout-content"">
        <BitStack Gap=""0.25rem"">
            <BitButton FullWidth Variant=""BitVariant.Text"" OnClick=""@(() => contextAction = ""Cut"")"">Cut</BitButton>
            <BitButton FullWidth Variant=""BitVariant.Text"" OnClick=""@(() => contextAction = ""Copied"")"">Copy</BitButton>
            <BitButton FullWidth Variant=""BitVariant.Text"" OnClick=""@(() => contextAction = ""Pasted"")"">Paste</BitButton>
        </BitStack>
    </div>
</BitCallout>

<div>Last action: @contextAction</div>";
    private readonly string example3CsharpCode = @"
private BitCallout contextCallout;
private string contextAction = ""none"";";

    private readonly string example4RazorCode = @"
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
</BitCallout>";
    private readonly string example4CsharpCode = @"
private bool isOpen;";

    private readonly string example5RazorCode = @"
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
</BitCallout>

<BitCallout ShowArrow ArrowSize=""20"" Gap=""12"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">ArrowSize of 20px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            A beak twice the size of the default one.
        </div>
    </Content>
</BitCallout>

<BitCallout ShowArrow ArrowPadding=""64"" Gap=""8"" MinWidth=""16rem"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">ArrowPadding of 64px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            The beak is held 64px away from the corners of the callout.
        </div>
    </Content>
</BitCallout>";

    private readonly string example7RazorCode = @"
<BitCallout Direction=""BitDropDirection.TopAndBottom"">
    <Anchor>
        <BitButton>TopAndBottom</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 23; i++)
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
            @for (int i = 1; i < 23; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout Side=""BitSide.Top"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Side: Top</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Placed above the anchor whenever there is room above it.
        </div>
    </Content>
</BitCallout>

<BitCallout Side=""BitSide.End"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Side: End</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Placed beside the anchor, on the side the content ends at.
        </div>
    </Content>
</BitCallout>

<BitCallout Side=""BitSide.Top"" NoFlip ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Side: Top, NoFlip</BitButton>
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

<BitCallout Alignment=""BitSideAlignment.Center"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">A wide anchor, centered alignment</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Centered on the anchor.</div>
    </Content>
</BitCallout>

<BitCallout Alignment=""BitSideAlignment.End"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">A wide anchor, end alignment</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">Lined up with the end edge.</div>
    </Content>
</BitCallout>

<BitCallout Side=""BitSide.End"" Alignment=""BitSideAlignment.Center"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">Beside, centered</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 5; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>

<BitCallout AlignmentOffset=""32"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">AlignmentOffset of 32px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            Slid 32px in from the start edge of the anchor.
        </div>
    </Content>
</BitCallout>

<BitCallout Alignment=""BitSideAlignment.End"" AlignmentOffset=""32"" ShowArrow Gap=""8"">
    <Anchor>
        <BitButton Variant=""BitVariant.Text"">End alignment, offset of 32px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            The same value, running in from the end edge instead.
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
</BitCallout>

<BitCallout @ref=""callout4"" Side=""BitSide.Top"" ShowArrow Gap=""8"" MinWidth=""14rem"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Reposition</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                @for (int i = 1; i <= repositionRows; i++)
                {
                    <div>Callout content @i</div>
                }
                <BitButton OnClick=""AddRepositionRow"">Add a row</BitButton>
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout CollisionPadding=""24"" Direction=""BitDropDirection.All"" MinWidth=""14rem"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">CollisionPadding of 24px</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 25; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
</BitCallout>";

    private readonly string example7CsharpCode = @"
private BitCallout callout4 = default!;

private int repositionRows = 2;
private bool repositionAfterRender;

private void AddRepositionRow()
{
    repositionRows++;

    // The callout is laid out against what is actually in it, so the reposition waits for the render
    // that puts the new row there rather than measuring the content the callout still holds.
    repositionAfterRender = true;
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    await base.OnAfterRenderAsync(firstRender);

    if (repositionAfterRender)
    {
        repositionAfterRender = false;

        await callout4.Reposition();
    }
}";

    private readonly string example8RazorCode = @"
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
        <div class=""callout-content"">
            A long line of text that wraps rather than making the callout wider than its anchor.
        </div>
    </Content>
</BitCallout>";

    private readonly string example9RazorCode = @"
<style>
    .section-bar {
        font-weight: 600;
        padding: 0.75rem 1rem;
    }

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

<BitCallout MaxWidth=""16rem"">
    <Anchor>
        <BitButton>Header & Footer</BitButton>
    </Anchor>
    <Header>
        <div class=""section-bar"">A header that stays put</div>
    </Header>
    <Content>
        <div class=""callout-content"">
            @for (int i = 1; i < 69; i++)
            {
                <div>Callout content @i</div>
            }
        </div>
    </Content>
    <Footer>
        <div class=""section-bar"">A footer that stays put</div>
    </Footer>
</BitCallout>

<BitCallout ScrollContainerId=""scroller-container"" HeaderId=""scroller-header"" FooterId=""scroller-footer"">
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Wired up by hand</BitButton>
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

    private readonly string example10RazorCode = @"
<BitCallout ResponsiveMode=""BitResponsiveMode.Panel"" PanelPosition=""BitSide.End"">
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

<BitCallout ResponsiveMode=""BitResponsiveMode.Panel"" PanelPosition=""BitSide.Start"">
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

    private readonly string example11RazorCode = @"
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

    private readonly string example12RazorCode = @"
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

    private readonly string example13RazorCode = @"
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

<BitCallout AutoFocus>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">AutoFocus (data-autofocus)</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""1rem"">
                <BitButton Variant=""BitVariant.Text"">Dismiss</BitButton>
                <BitTextField Label=""Name"" InputHtmlAttributes=""@(new() { { ""data-autofocus"", """" } })"" />
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

    private readonly string example14RazorCode = @"
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
    private readonly string example14CsharpCode = @"
private BitCallout callout3;
private string autoCloseAction = ""none"";";

    private readonly string example15RazorCode = @"
<BitCallout MinWidth=""15rem"">
    <Anchor>
        <BitButton>Filters</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle2"">Filters</BitText>
                <BitCheckbox Label=""Active"" />
                <BitCheckbox Label=""Archived"" />

                <BitCallout ShowArrow Gap=""8"" Side=""BitSide.End"">
                    <Anchor>
                        <BitButton Variant=""BitVariant.Outline"">More options</BitButton>
                    </Anchor>
                    <Content>
                        <div class=""callout-content"">
                            <BitStack Gap=""0.25rem"">
                                <BitText>The panel behind is still open.</BitText>

                                <BitCallout ShowArrow Gap=""8"" AutoClose Side=""BitSide.End"">
                                    <Anchor>
                                        <BitButton Variant=""BitVariant.Text"">One more level</BitButton>
                                    </Anchor>
                                    <Content>
                                        <div class=""callout-content"">And so is this one.</div>
                                    </Content>
                                </BitCallout>
                            </BitStack>
                        </div>
                    </Content>
                </BitCallout>
            </BitStack>
        </div>
    </Content>
</BitCallout>";

    private readonly string example16RazorCode = @"
<BitCallout LazyRender>
    <Anchor>
        <BitButton>LazyRender</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle2"">Pick a date</BitText>
                <BitCalendar @bind-Value=""lazyDate"" />
            </BitStack>
        </div>
    </Content>
</BitCallout>

<BitCallout>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Rendered up front</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">
            <BitStack Gap=""0.5rem"">
                <BitText Typography=""BitTypography.Subtitle2"">Pick a date</BitText>
                <BitCalendar @bind-Value=""eagerDate"" />
            </BitStack>
        </div>
    </Content>
</BitCallout>";
    private readonly string example16CsharpCode = @"
private DateTimeOffset? lazyDate;
private DateTimeOffset? eagerDate;";

    private readonly string example17RazorCode = @"
<BitCallout OnToggle=""v => toggleCount++"" OnOpen=""() => openCount++"" OnDismiss=""() => dismissCount++"">
    <Anchor>
        <BitButton>Show callout</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>

<div>Toggled: @toggleCount, Opened: @openCount, Dismissed: @dismissCount</div>";
    private readonly string example17CsharpCode = @"
private int openCount;
private int toggleCount;
private int dismissCount;";

    private readonly string example18RazorCode = @"
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
        <BitButton Variant=""BitVariant.Text"">Classes</BitButton>
    </Anchor>
    <Content>
        <div class=""callout-content"">This is the callout content.</div>
    </Content>
</BitCallout>";

    private readonly string example19RazorCode = @"
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
