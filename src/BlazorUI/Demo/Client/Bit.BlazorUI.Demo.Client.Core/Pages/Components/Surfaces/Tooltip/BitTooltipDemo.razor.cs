namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Tooltip;

public partial class BitTooltipDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the Tooltip will apply to."
        },
        new()
        {
            Name = "ArrowSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size in pixels of the arrow that points at the anchor, which is the length of the side of the square it is drawn from. Leaving it unset keeps the size the theme gives it."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the Tooltip will apply to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTooltipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTooltip.",
            LinkType = LinkType.Link,
            Href = "#tooltip-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the tooltip, which colors its surface and the arrow along with it.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "DefaultIsShown",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value of the IsShown."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the tooltip's own element to 100% of the available width, so that the anchor inside it keeps the width it would have had without a tooltip around it. The tooltip wraps its anchor in an element laid out inline, which would otherwise shrink a block-level anchor to its content."
        },
        new()
        {
            Name = "HideArrow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the arrow of tooltip."
        },
        new()
        {
            Name = "HideDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "Delay (in milliseconds) before hiding the tooltip. It is the grace an interactive tooltip needs while the pointer crosses the gap between the anchor and the tooltip, and the pause that keeps a tooltip from flickering while the pointer skims across a row of anchors. Leaving it alone inside a BitTooltipGroup takes the delay the group sets."
        },
        new()
        {
            Name = "HideOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the tooltip when the anchor is clicked, which is what a tooltip on a control that does something when pressed owes the reader. A tooltip the click is meant to open and close instead is ShowOnClick, which takes over the click when it is on."
        },
        new()
        {
            Name = "Interactive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the pointer travel into the tooltip and stay there without it being hidden, which is what WCAG 1.4.13 asks of content shown on hover. The gap between the anchor and the tooltip is bridged, so the pointer never leaves the component on its way over."
        },
        new()
        {
            Name = "IsShown",
            Type = "bool",
            DefaultValue = "false",
            Description = "The visibility state of the tooltip."
        },
        new()
        {
            Name = "IsShownChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback that is called when the visibility state of the tooltip changes."
        },
        new()
        {
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the content of the tooltip out of the DOM until the tooltip is first shown, and keeps it rendered from then on."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the tooltip as a CSS value (e.g. \"20rem\"), beyond which its text wraps onto another line instead of the tooltip growing wider. A value of \"none\" takes the cap off."
        },
        new()
        {
            Name = "MirrorInRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Mirrors the position of the tooltip along the horizontal axis while the direction is right to left, so that a position named for one side of the anchor lands on the side the reader starts at. The positions are named for the sides of the screen rather than for the reading order, so Left is the left of the anchor in either direction unless this is turned on."
        },
        new()
        {
            Name = "NoAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the fade the tooltip is shown and hidden with, so that it simply appears."
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Escape key from dismissing the tooltip. Dismissing content shown on hover or focus without moving either of them is what WCAG 1.4.13 asks for, so only turn it off for a tooltip that obscures nothing."
        },
        new()
        {
            Name = "NoTouch",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps a touch or a pen from showing the tooltip at all, leaving the anchor to answer the tap alone. Turn it on for a tooltip that only repeats what a touch user can already read."
        },
        new()
        {
            Name = "Offset",
            Type = "int?",
            DefaultValue = "null",
            Description = "The distance in pixels between the anchor and the tooltip, which is also the room the arrow is drawn in. Leaving it unset keeps the distance the theme gives it."
        },
        new()
        {
            Name = "OnHide",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is called when the tooltip is hidden."
        },
        new()
        {
            Name = "OnShow",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is called when the tooltip is shown."
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback that is called when the tooltip is shown or hidden, with the new state."
        },
        new()
        {
            Name = "Position",
            Type = "BitTooltipPosition",
            DefaultValue = "BitTooltipPosition.Top",
            Description = "The position of tooltip around its anchor.",
            LinkType = LinkType.Link,
            Href = "#tooltip-position-enum"
        },
        new()
        {
            Name = "Relationship",
            Type = "BitTooltipRelationship",
            DefaultValue = "BitTooltipRelationship.Description",
            Description = "What the tooltip is to the anchor it belongs to, which decides whether the anchor is given an aria-describedby, an aria-labelledby or neither.",
            LinkType = LinkType.Link,
            Href = "#tooltip-relationship-enum"
        },
        new()
        {
            Name = "ShowDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "Delay (in milliseconds) before showing the tooltip. It applies to the pointer only: a tooltip reached with the keyboard or opened by a click is shown at once. Leaving it alone inside a BitTooltipGroup takes the delay the group sets, which the group also drops while another of its tooltips is still fresh in mind."
        },
        new()
        {
            Name = "ShowOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the tooltip is shown when the anchor is clicked."
        },
        new()
        {
            Name = "ShowOnFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines whether the tooltip is shown when the anchor takes the focus, so that a tooltip reached with the keyboard is shown the way it is to a pointer. A focus that follows a press of the pointer is left to the pointer, the way :focus-visible does it in CSS."
        },
        new()
        {
            Name = "ShowOnHover",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines shows tooltip on hover. The hover and the focus are kept apart, so a pointer leaving an anchor the keyboard is still on does not take the tooltip away with it."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the tooltip, which sets the size of its text and the padding around it.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitTooltipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitTooltip.",
            LinkType = LinkType.Link,
            Href = "#tooltip-class-styles"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content you want inside the tooltip."
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of tooltip to show."
        },
        new()
        {
            Name = "TouchHideDelay",
            Type = "int",
            DefaultValue = "1500",
            Description = "The time in milliseconds a tooltip shown by a touch stays before it hides itself. A touch leaves no pointer behind that can leave the anchor again. Zero leaves it shown until something else hides it."
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The stacking order of the tooltip surface and its arrow. Leaving it unset keeps the one the theme gives every popup surface in the library."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Show",
            Type = "Task",
            Description = "Shows the tooltip programmatically, at once and regardless of the triggers it is configured with, unless it is disabled."
        },
        new()
        {
            Name = "Hide",
            Type = "Task",
            Description = "Hides the tooltip programmatically, at once and regardless of the delays it is configured with."
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the tooltip to show/hide it."
        },
        new()
        {
            Name = "TooltipId",
            Type = "string",
            Description = "The id of the element the text of the tooltip is rendered in, which is what an anchor of your own points its aria-describedby or aria-labelledby at."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "tooltip-position-enum",
            Name = "BitTooltipPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Value = "0",
                    Description = "The position of tooltip top of its anchor"
                },
                new()
                {
                    Name = "TopLeft",
                    Value = "1",
                    Description = "The position of tooltip top left of its anchor"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2",
                    Description = "The position of tooltip top right of its anchor"
                },
                new()
                {
                    Name = "RightTop",
                    Value = "3",
                    Description = "The position of tooltip right top of its anchor"
                },
                new()
                {
                    Name = "Right",
                    Value = "4",
                    Description = "The position of tooltip right of its anchor"
                },
                new()
                {
                    Name = "RightBottom",
                    Value = "5",
                    Description = "The position of tooltip right bottom of its anchor"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "6",
                    Description = "The position of tooltip bottom right of its anchor"
                },
                new()
                {
                    Name = "Bottom",
                    Value = "7",
                    Description = "The position of tooltip bottom of its anchor"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "8",
                    Description = "The position of tooltip bottom left of its anchor"
                },
                new()
                {
                    Name = "LeftBottom",
                    Value = "9",
                    Description = "The position of tooltip left bottom of its anchor"
                },
                new()
                {
                    Name = "Left",
                    Value = "10",
                    Description = "The position of tooltip left of its anchor"
                },
                new()
                {
                    Name = "LeftTop",
                    Value = "11",
                    Description = "The position of tooltip left top of its anchor"
                }
            ]
        },
        new()
        {
            Id = "tooltip-relationship-enum",
            Name = "BitTooltipRelationship",
            Description = "Determines the accessible relationship between a tooltip and the anchor it belongs to.",
            Items =
            [
                new()
                {
                    Name = "Description",
                    Value = "0",
                    Description = "The tooltip adds information to an anchor that already has a name of its own, and is pointed at with aria-describedby."
                },
                new()
                {
                    Name = "Label",
                    Value = "1",
                    Description = "The tooltip is the name of an anchor that has none of its own - an icon-only button, above all - and is pointed at with aria-labelledby."
                },
                new()
                {
                    Name = "None",
                    Value = "2",
                    Description = "The tooltip is left out of the accessibility tree altogether, for the case where the anchor already carries the same text by another route."
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
                new() { Name = "Primary", Value = "0", Description = "Primary general color." },
                new() { Name = "Secondary", Value = "1", Description = "Secondary general color." },
                new() { Name = "Tertiary", Value = "2", Description = "Tertiary general color." },
                new() { Name = "Info", Value = "3", Description = "Info general color." },
                new() { Name = "Success", Value = "4", Description = "Success general color." },
                new() { Name = "Warning", Value = "5", Description = "Warning general color." },
                new() { Name = "SevereWarning", Value = "6", Description = "SevereWarning general color." },
                new() { Name = "Error", Value = "7", Description = "Error general color." },
                new() { Name = "PrimaryBackground", Value = "8", Description = "Primary background color." },
                new() { Name = "SecondaryBackground", Value = "9", Description = "Secondary background color." },
                new() { Name = "TertiaryBackground", Value = "10", Description = "Tertiary background color." },
                new() { Name = "PrimaryForeground", Value = "11", Description = "Primary foreground color." },
                new() { Name = "SecondaryForeground", Value = "12", Description = "Secondary foreground color." },
                new() { Name = "TertiaryForeground", Value = "13", Description = "Tertiary foreground color." },
                new() { Name = "PrimaryBorder", Value = "14", Description = "Primary border color." },
                new() { Name = "SecondaryBorder", Value = "15", Description = "Secondary border color." },
                new() { Name = "TertiaryBorder", Value = "16", Description = "Tertiary border color." }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new() { Name = "Small", Value = "0", Description = "The small size tooltip." },
                new() { Name = "Medium", Value = "1", Description = "The medium size tooltip." },
                new() { Name = "Large", Value = "2", Description = "The large size tooltip." }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "tooltip-group",
            Title = "BitTooltipGroup",
            Description = "Groups the tooltips inside it so that they share their delays, so that the second of them is shown without its delay while the first is still fresh in mind, and so that only one of them is on the screen at a time. It renders nothing of its own.",
            Parameters =
            [
                new()
                {
                    Name = "AllowMultiple",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Lets more than one tooltip of the group be on the screen at a time. A group shows one tooltip at a time by default, which is what a row of controls with a tooltip each needs."
                },
                new()
                {
                    Name = "ChildContent",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The tooltips the group is around."
                },
                new()
                {
                    Name = "HideDelay",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The delay in milliseconds before hiding, for every tooltip in the group that does not set one of its own."
                },
                new()
                {
                    Name = "ShowDelay",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The delay in milliseconds before showing, for every tooltip in the group that does not set one of its own."
                },
                new()
                {
                    Name = "SkipDelay",
                    Type = "int",
                    DefaultValue = "300",
                    Description = "How long in milliseconds after a tooltip of the group has been hidden another one of them is shown at once rather than waiting out the show delay. Zero makes every tooltip wait out its own delay."
                }
            ]
        },
        new()
        {
            Id = "tooltip-class-styles",
            Title = "BitTooltipClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTooltip."
               },
               new()
               {
                   Name = "TooltipWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tooltip wrapper of the BitTooltip."
               },
               new()
               {
                   Name = "Tooltip",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tooltip of the BitTooltip."
               },
               new()
               {
                   Name = "Arrow",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the arrow of the BitTooltip."
               }
            ]
        }
    ];



    private BitTooltipPosition tooltipPosition;
    private readonly List<BitDropdownItem<BitTooltipPosition>> tooltipPositionList = Enum.GetValues(typeof(BitTooltipPosition))
        .Cast<BitTooltipPosition>()
        .Select(enumValue => new BitDropdownItem<BitTooltipPosition>
        {
            Value = enumValue,
            Text = enumValue.ToString()
        })
        .ToList();

    private bool isShown = true;
    private bool isShownAdvanced = true;
    private bool showOnClick = true;
    private bool showOnHover;
    private bool showOnFocus;
    private bool hideOnClick;
    private bool hideArrow;
    private bool interactive;
    private double showDelay = 0;
    private double hideDelay = 800;

    private BitTooltip? tooltipRef;

    private readonly List<string> events = [];



    private readonly string example1RazorCode = @"
<BitTooltip Text=""This is the tooltip text"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip Text=""This tooltip never shows"" IsEnabled=""false"">
    <BitButton Variant=""BitVariant.Outline"">Disabled tooltip</BitButton>
</BitTooltip>";

    private readonly string example2RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Text=""Top"" Position=""BitTooltipPosition.Top"">
    <BitButton Variant=""BitVariant.Outline"">Top</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""TopLeft"" Position=""BitTooltipPosition.TopLeft"">
    <BitButton Variant=""BitVariant.Outline"">TopLeft</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""TopRight"" Position=""BitTooltipPosition.TopRight"">
    <BitButton Variant=""BitVariant.Outline"">TopRight</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""RightTop"" Position=""BitTooltipPosition.RightTop"">
    <BitButton Variant=""BitVariant.Outline"">RightTop</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Right"" Position=""BitTooltipPosition.Right"">
    <BitButton Variant=""BitVariant.Outline"">Right</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""RightBottom"" Position=""BitTooltipPosition.RightBottom"">
    <BitButton Variant=""BitVariant.Outline"">RightBottom</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""BottomRight"" Position=""BitTooltipPosition.BottomRight"">
    <BitButton Variant=""BitVariant.Outline"">BottomRight</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Bottom"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Bottom</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""BottomLeft"" Position=""BitTooltipPosition.BottomLeft"">
    <BitButton Variant=""BitVariant.Outline"">BottomLeft</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""LeftBottom"" Position=""BitTooltipPosition.LeftBottom"">
    <BitButton Variant=""BitVariant.Outline"">LeftBottom</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Left"" Position=""BitTooltipPosition.Left"">
    <BitButton Variant=""BitVariant.Outline"">Left</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""LeftTop"" Position=""BitTooltipPosition.LeftTop"">
    <BitButton Variant=""BitVariant.Outline"">LeftTop</BitButton>
</BitTooltip>";

    private readonly string example3RazorCode = @"
<BitTooltip Text=""Shown by the pointer only"" ShowOnFocus=""false"">
    <BitButton Variant=""BitVariant.Outline"">Hover</BitButton>
</BitTooltip>

<BitTooltip Text=""Shown by the keyboard only"" ShowOnHover=""false"">
    <BitButton Variant=""BitVariant.Outline"">Focus (tab to me)</BitButton>
</BitTooltip>

<BitTooltip Text=""Toggled by a click"" ShowOnClick ShowOnHover=""false"" ShowOnFocus=""false"">
    <BitButton Variant=""BitVariant.Outline"">Click</BitButton>
</BitTooltip>

<BitTooltip Text=""A click takes me away"" HideOnClick>
    <BitButton Variant=""BitVariant.Outline"">HideOnClick</BitButton>
</BitTooltip>

<BitTooltip Text=""A tap leaves me out of it"" NoTouch>
    <BitButton Variant=""BitVariant.Outline"">NoTouch</BitButton>
</BitTooltip>";

    private readonly string example4RazorCode = @"
<BitTooltip Text=""Waited 700ms for you"" ShowDelay=""700"">
    <BitButton Variant=""BitVariant.Outline"">ShowDelay</BitButton>
</BitTooltip>

<BitTooltip Text=""Staying for a second"" HideDelay=""1000"">
    <BitButton Variant=""BitVariant.Outline"">HideDelay</BitButton>
</BitTooltip>";

    private readonly string example5RazorCode = @"
<BitTooltipGroup ShowDelay=""700"" HideDelay=""100"" SkipDelay=""600"">
    <BitTooltip Text=""Waited for, like the first of a row"">
        <BitButton Variant=""BitVariant.Outline"">Bold</BitButton>
    </BitTooltip>

    <BitTooltip Text=""Shown at once, while the last one is fresh"">
        <BitButton Variant=""BitVariant.Outline"">Italic</BitButton>
    </BitTooltip>

    <BitTooltip Text=""And so is this one"">
        <BitButton Variant=""BitVariant.Outline"">Underline</BitButton>
    </BitTooltip>

    <BitTooltip Text=""I keep the delay I was given"" ShowDelay=""0"">
        <BitButton Variant=""BitVariant.Outline"">No delay of my own</BitButton>
    </BitTooltip>
</BitTooltipGroup>";

    private readonly string example6RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Text=""Default"">
    <BitButton Variant=""BitVariant.Outline"">Default</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""No arrow"" HideArrow>
    <BitButton Variant=""BitVariant.Outline"">HideArrow</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""A bigger arrow"" ArrowSize=""18"">
    <BitButton Variant=""BitVariant.Outline"">ArrowSize</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""Held further off"" Offset=""24"">
    <BitButton Variant=""BitVariant.Outline"">Offset</BitButton>
</BitTooltip>";

    private readonly string example7RazorCode = @"
<BitTooltip Interactive HideDelay=""200"" Position=""BitTooltipPosition.Bottom""
            Text=""Move onto me and I will stay. Select this text."">
    <BitButton Variant=""BitVariant.Outline"">Interactive</BitButton>
</BitTooltip>

<BitTooltip HideDelay=""200"" Position=""BitTooltipPosition.Bottom""
            Text=""Move onto me and I am gone."">
    <BitButton Variant=""BitVariant.Outline"">Not interactive</BitButton>
</BitTooltip>";

    private readonly string example8RazorCode = @"
<BitTooltip>
    <Template>
        <ul style=""padding: 0.5rem; margin: 0;"">
            <li>1. One</li>
            <li>2. Two</li>
        </ul>
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
    </Anchor>
</BitTooltip>";

    private readonly string example9RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Position=""BitTooltipPosition.Bottom"" MaxWidth=""10rem""
            Text=""A narrow tooltip wraps its text sooner."">
    <BitButton Variant=""BitVariant.Outline"">10rem</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Position=""BitTooltipPosition.Bottom""
            Text=""The default cap keeps a long line from running on across the whole screen."">
    <BitButton Variant=""BitVariant.Outline"">Default</BitButton>
</BitTooltip>";

    private readonly string example10RazorCode = @"
<BitTooltip Text=""Press Escape to dismiss me"">
    <BitButton Variant=""BitVariant.Outline"">Dismissible</BitButton>
</BitTooltip>

<BitTooltip Text=""Escape leaves me alone"" NoDismissOnEscape>
    <BitButton Variant=""BitVariant.Outline"">NoDismissOnEscape</BitButton>
</BitTooltip>";

    private readonly string example11RazorCode = @"
<BitTooltip Text=""Save the current document"" Relationship=""BitTooltipRelationship.Label"">
    <BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.Save"" />
</BitTooltip>

<BitTooltip Text=""Everything you have written since the last save"">
    <BitButton Variant=""BitVariant.Outline"">Save</BitButton>
</BitTooltip>

<BitTooltip Id=""discard-tip"" Text=""Discard"" Relationship=""BitTooltipRelationship.None"">
    <button class=""plain-anchor"" aria-describedby=""discard-tip-ttp"">Discard</button>
</BitTooltip>";

    private readonly string example12RazorCode = @"
<BitTooltip LazyRender Position=""BitTooltipPosition.Bottom"">
    <Template>
        <TooltipRenderStamp />
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">LazyRender</BitButton>
    </Anchor>
</BitTooltip>

<BitTooltip Position=""BitTooltipPosition.Bottom"">
    <Template>
        <TooltipRenderStamp />
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Rendered up front</BitButton>
    </Anchor>
</BitTooltip>

@* TooltipRenderStamp.razor - the stamp is taken once, while the content is first rendered, rather
   than on every rerender of the tooltip. *@
<div>Rendered at @renderedAt.ToString(""HH:mm:ss"")</div>

@code {
    private DateTime renderedAt;

    protected override void OnInitialized()
    {
        renderedAt = DateTime.Now;
    }
}";

    private readonly string example13RazorCode = @"
<BitToggle @bind-Value=""isShown"" Label=""IsShown"" />

<BitTooltip DefaultIsShown=""true"" Text=""Shown to begin with"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">DefaultIsShown</BitButton>
</BitTooltip>

<BitTooltip @bind-IsShown=""isShown"" Text=""Bound to the toggle"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">@bind-IsShown</BitButton>
</BitTooltip>";
    private readonly string example13CsharpCode = @"
private bool isShown = true;";

    private readonly string example14RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => tooltipRef?.Show())"">Show</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => tooltipRef?.Hide())"">Hide</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => tooltipRef?.Toggle())"">Toggle</BitButton>

<BitTooltip @ref=""tooltipRef"" Text=""Driven from the buttons above"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Anchor</BitButton>
</BitTooltip>";
    private readonly string example14CsharpCode = @"
private BitTooltip? tooltipRef;";

    private readonly string example15RazorCode = @"
<BitTooltip Text=""Watch the log below""
            OnShow=""@(() => events.Insert(0, $""OnShow at {DateTime.Now:HH:mm:ss}""))""
            OnHide=""@(() => events.Insert(0, $""OnHide at {DateTime.Now:HH:mm:ss}""))""
            OnToggle=""@(v => events.Insert(0, $""OnToggle({v}) at {DateTime.Now:HH:mm:ss}""))"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

@foreach (var item in events.Take(6))
{
    <div>@item</div>
}";
    private readonly string example15CsharpCode = @"
private readonly List<string> events = [];";

    private readonly string example16RazorCode = @"
<BitTooltip @bind-IsShown=""isShownAdvanced""
            Text=""Text""
            Interactive=""interactive""
            HideArrow=""hideArrow""
            ShowDelay=""(int)showDelay""
            HideDelay=""(int)hideDelay""
            ShowOnClick=""showOnClick""
            ShowOnHover=""showOnHover""
            ShowOnFocus=""showOnFocus""
            HideOnClick=""hideOnClick""
            Position=""tooltipPosition"">
    <BitButton Variant=""BitVariant.Outline"">Anchor</BitButton>
</BitTooltip>

<BitDropdown Label=""Tooltip positions"" Items=""tooltipPositionList"" @bind-Value=""tooltipPosition"" />
<BitNumberField Label=""Show delay"" @bind-Value=""showDelay"" Mode=""BitSpinButtonMode.Inline"" />
<BitNumberField Label=""Hide delay"" @bind-Value=""hideDelay"" Mode=""BitSpinButtonMode.Inline"" />
<BitToggle @bind-Value=""isShownAdvanced"" Text=""Toggle tooltip state"" />
<BitToggle @bind-Value=""hideArrow"" Text=""Hide tooltip arrow"" />
<BitToggle @bind-Value=""interactive"" Text=""Interactive tooltip"" />
<BitToggle @bind-Value=""showOnClick"" Text=""Show tooltip on click"" />
<BitToggle @bind-Value=""showOnHover"" Text=""Show tooltip on hover"" />
<BitToggle @bind-Value=""showOnFocus"" Text=""Show tooltip on focus"" />
<BitToggle @bind-Value=""hideOnClick"" Text=""Hide tooltip on click"" />";
    private readonly string example16CsharpCode = @"
private bool isShownAdvanced = true;
private bool showOnClick = true;
private bool showOnHover;
private bool showOnFocus;
private bool hideOnClick;
private bool hideArrow;
private bool interactive;
private double showDelay = 0;
private double hideDelay = 800;

private BitTooltipPosition tooltipPosition;

private readonly List<BitDropdownItem<BitTooltipPosition>> tooltipPositionList = Enum.GetValues(typeof(BitTooltipPosition))
    .Cast<BitTooltipPosition>()
    .Select(enumValue => new BitDropdownItem<BitTooltipPosition>
    {
        Value = enumValue,
        Text = enumValue.ToString()
    })
    .ToList();";

    private readonly string example17RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Primary"" Text=""Primary"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Primary</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Secondary"" Text=""Secondary"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Secondary</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Tertiary"" Text=""Tertiary"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Tertiary</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Info"" Text=""Info"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Info</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Success"" Text=""Success"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Success</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Warning"" Text=""Warning"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Warning</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SevereWarning"" Text=""SevereWarning"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SevereWarning</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Error"" Text=""Error"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Error</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.PrimaryBackground"" Text=""PrimaryBackground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">PrimaryBackground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SecondaryBackground"" Text=""SecondaryBackground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SecondaryBackground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.TertiaryBackground"" Text=""TertiaryBackground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">TertiaryBackground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.PrimaryForeground"" Text=""PrimaryForeground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">PrimaryForeground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SecondaryForeground"" Text=""SecondaryForeground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SecondaryForeground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.TertiaryForeground"" Text=""TertiaryForeground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">TertiaryForeground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.PrimaryBorder"" Text=""PrimaryBorder"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">PrimaryBorder</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SecondaryBorder"" Text=""SecondaryBorder"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SecondaryBorder</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.TertiaryBorder"" Text=""TertiaryBorder"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">TertiaryBorder</BitButton>
</BitTooltip>";

    private readonly string example18RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Size=""BitSize.Small"" Text=""Small"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Small</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Size=""BitSize.Medium"" Text=""Medium"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Medium</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Size=""BitSize.Large"" Text=""Large"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Large</BitButton>
</BitTooltip>";

    private readonly string example19RazorCode = @"
<style>
    .custom-tooltip {
        color: tomato;
        border: solid tomato;
        border-radius: 0.5rem;
    }

    .custom-arrow {
        border-right: solid tomato;
        border-bottom: solid tomato;
    }
</style>


<BitTooltip Text=""This is the tooltip text"" Styles=""@(new() { Tooltip = ""box-shadow: aqua 0 0 0.5rem;"" })"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip Text=""This is the tooltip text"" Classes=""@(new() { Tooltip = ""custom-tooltip"", Arrow = ""custom-arrow"" })"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip Text=""No fade in or out"" NoAnimation>
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip Text=""Lifted over what is around me"" ZIndex=""9999"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip FullWidth Text=""The field keeps the width it was given"">
    <BitTextField Label=""With FullWidth"" Placeholder=""Hover over me"" />
</BitTooltip>

<BitTooltip Text=""The field is shrunk to what it holds"">
    <BitTextField Label=""Without FullWidth"" Placeholder=""Hover over me"" />
</BitTooltip>";

    private readonly string example20RazorCode = @"
<BitTooltip Dir=""BitDir.Rtl"">
    <Template>
        <ul style=""padding: 0.5rem; margin: 0;"">
            <li>۱. یک</li>
            <li>۲. دو</li>
        </ul>
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">نشانگر ماوس را روی من بیاورید</BitButton>
    </Anchor>
</BitTooltip>

<BitTooltip Dir=""BitDir.Rtl"" DefaultIsShown=""true"" Position=""BitTooltipPosition.Left""
            Text=""سمت چپ لنگر"">
    <BitButton Variant=""BitVariant.Outline"">Left</BitButton>
</BitTooltip>

<BitTooltip Dir=""BitDir.Rtl"" DefaultIsShown=""true"" Position=""BitTooltipPosition.Left"" MirrorInRtl
            Text=""آینه‌شده به سمت راست"">
    <BitButton Variant=""BitVariant.Outline"">Left + MirrorInRtl</BitButton>
</BitTooltip>";
}
