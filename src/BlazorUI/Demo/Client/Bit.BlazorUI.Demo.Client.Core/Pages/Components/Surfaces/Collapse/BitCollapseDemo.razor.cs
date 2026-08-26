namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Collapse;

public partial class BitCollapseDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the collapse.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
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
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the collapse."
        },
        new()
        {
            Name = "Classes",
            Type = "BitCollapseClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the collapse.",
            LinkType = LinkType.Link,
            Href = "#collapse-class-styles"
        },
        new()
        {
            Name = "CollapsedSize",
            Type = "string?",
            DefaultValue = "null",
            Description = "The size the collapse keeps while it is collapsed, as any CSS length, which leaves a peek of the content on the page instead of closing it all the way. It is a width instead of a height while Horizontal is on."
        },
        new()
        {
            Name = "DefaultExpanded",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The default value of the Expanded parameter, applied once at initialization and only while Expanded itself has not been set."
        },
        new()
        {
            Name = "Delay",
            Type = "int?",
            DefaultValue = "null",
            Description = "The delay of the expand/collapse transition in ms."
        },
        new()
        {
            Name = "Duration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The duration of the expand/collapse transition in ms. Leaving it unset keeps the duration of the motion theme, which is also what the reduced motion preference collapses to nothing."
        },
        new()
        {
            Name = "Easing",
            Type = "string?",
            DefaultValue = "null",
            Description = "The timing function of the expand/collapse transition, as any CSS easing value."
        },
        new()
        {
            Name = "Expanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the collapse is expanded or collapsed."
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Collapses the content along the inline axis instead of the block one, so it opens sideways from the start edge."
        },
        new()
        {
            Name = "LabelledBy",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element that names the content region of the collapse, rendered as aria-labelledby."
        },
        new()
        {
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content out of the DOM until the collapse is expanded for the first time."
        },
        new()
        {
            Name = "NoAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the expand/collapse transition, so the content appears and disappears at once."
        },
        new()
        {
            Name = "NoFade",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the fade of the content, leaving the size on its own to open and close the collapse."
        },
        new()
        {
            Name = "NoPadding",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the padding the collapse puts around its content."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback that is called when the Expanded value has changed by the component itself."
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role of the content region of the collapse, which is region by default. An empty string renders no role at all."
        },
        new()
        {
            Name = "Styles",
            Type = "BitCollapseClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the collapse.",
            LinkType = LinkType.Link,
            Href = "#collapse-class-styles"
        },
        new()
        {
            Name = "UnmountOnCollapse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Takes the content back out of the DOM once the collapse has closed, after the transition has had time to finish."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ContentId",
            Type = "string",
            Description = "The id of the content element of the collapse, which is the id of the root element with -content after it, so a trigger elsewhere on the page can point its aria-controls at the section it opens."
        },
        new()
        {
            Name = "CollapseAsync",
            Type = "Task",
            Description = "Collapses the collapse, reporting the change through ExpandedChanged and OnChange."
        },
        new()
        {
            Name = "ExpandAsync",
            Type = "Task",
            Description = "Expands the collapse, reporting the change through ExpandedChanged and OnChange."
        },
        new()
        {
            Name = "ToggleAsync",
            Type = "Task",
            Description = "Flips the collapse between expanded and collapsed, reporting the change through ExpandedChanged and OnChange."
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
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "collapse-class-styles",
            Title = "BitCollapseClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCollapse."
                },
                new()
                {
                    Name = "Expanded",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCollapse in the expanded state."
                },
                new()
                {
                    Name = "Collapsed",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCollapse in the collapsed state."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content region of the BitCollapse, which is the element that animates between the two states."
                },
                new()
                {
                    Name = "Wrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper the BitCollapse puts around its content, which is the element that carries the padding and clips what is outside the collapsed size."
                }
            ]
        }
    ];



    private bool expanded = true;

    private bool boundExpanded = true;
    private string defaultChangeLog = string.Empty;
    private BitCollapse? defaultCollapseRef;
    private BitCollapse? imperativeCollapseRef;

    private bool horizontalExpanded = true;

    private bool peekExpanded;

    private bool transitionExpanded = true;
    private bool noFadeExpanded = true;
    private bool noAnimationExpanded = true;

    private bool surfaceExpanded = true;

    private bool lazyExpanded;
    private int lazyRenderCount;
    private bool unmountExpanded = true;

    private bool a11yExpanded;

    private bool expandedClass = true;
    private bool expandedStyle = true;

    private bool expandedRtl = true;
    private bool expandedRtlHorizontal = true;



    private void HandleDefaultChange(bool value)
    {
        defaultChangeLog = $"OnChange reported {value}.";
    }
}
