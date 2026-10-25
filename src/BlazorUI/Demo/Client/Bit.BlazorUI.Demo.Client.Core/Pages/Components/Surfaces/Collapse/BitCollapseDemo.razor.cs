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
            Name = "CollapseDuration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The duration of the collapse transition in ms, which overrides Duration while the collapse is closing and leaves the opening alone."
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
            Description = "The duration of the expand/collapse transition in ms. Leaving it unset keeps the duration of the motion theme (or --bit-Collapse-duration). The reduced motion preference still collapses it unless ForceAnimation is set. OnExpanded, OnCollapsed, NoClip, HiddenUntilFound and UnmountOnCollapse wait for the transition the browser actually plays."
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
            Name = "ExpandDuration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The duration of the expand transition in ms, which overrides Duration while the collapse is opening and leaves the closing alone."
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
            Name = "ExpandedChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback of the two-way binding of the Expanded parameter, raised with the new state."
        },
        new()
        {
            Name = "ExpandOnPrint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prints the collapse expanded, whatever its state on the screen. Such a collapse ignores LazyRender and UnmountOnCollapse, since the content has to be in the DOM to be printed."
        },
        new()
        {
            Name = "HiddenUntilFound",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hands the closed content to the browser as hidden=\"until-found\", so find-in-page and a navigation to a fragment inside the section reach into it and open it. Such a collapse ignores LazyRender and UnmountOnCollapse, since the content has to stay in the DOM to be found, and one that cannot open - disabled, or with a one-way Expanded - is not offered to find-in-page at all."
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
            Description = "Keeps the content out of the DOM until the collapse is expanded for the first time. A collapse that keeps a CollapsedSize, is searchable through HiddenUntilFound or prints expanded through ExpandOnPrint ignores it."
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
            Name = "NoClip",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops clipping the content once the collapse has finished opening, so a focus ring, a shadow or a menu that reaches past the edges of the section is drawn in full. The clipping is put back the moment the collapse starts closing."
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
            Name = "OnCollapsed",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback that is called once the collapse has finished closing, which is the end of the collapse transition."
        },
        new()
        {
            Name = "OnCollapsing",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback that is called as the collapse starts closing, which is the start of the collapse transition. It is the place to move the focus back to the trigger when it is inside the section, since the closed content can no longer hold it."
        },
        new()
        {
            Name = "OnExpanded",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback that is called once the collapse has finished opening, which is the end of the expand transition."
        },
        new()
        {
            Name = "OnExpanding",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback that is called as the collapse starts opening, which is the start of the expand transition."
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role of the content region of the collapse, which is region by default. An empty string renders no role at all, and with it no aria-label or aria-labelledby, since ARIA prohibits naming an element with no role - as it does under none, presentation and generic, which drop the name the same way."
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
            Description = "Takes the content back out of the DOM once the collapse has closed, after the transition has had time to finish. A collapse that keeps a CollapsedSize, is searchable through HiddenUntilFound or prints expanded through ExpandOnPrint ignores it."
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
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Moves the focus to the content region of the collapse, which is focusable while it is on the screen, even when a TabIndex of -1 takes it out of the tab order. Worth pairing with OnExpanded so the focus lands once the section has finished opening."
        },
        new()
        {
            Name = "ToggleAsync",
            Type = "Task",
            Description = "Flips the collapse between expanded and collapsed, reporting the change through ExpandedChanged and OnChange."
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Collapse-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the text.",
        },
        new()
        {
            Name = "--bit-Collapse-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the section. The Background parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Collapse-padding",
            DefaultValue = "spacing(1.5) (12px)",
            Description = "Room between the edges of the section and its content. NoPadding wins over it.",
        },
        new()
        {
            Name = "--bit-Collapse-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Size of the text.",
        },
        new()
        {
            Name = "--bit-Collapse-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Color of the focus ring of the content region.",
        },
        new()
        {
            Name = "--bit-Collapse-duration",
            DefaultValue = "--bit-mot-duration-long",
            Description = "Pace of the expand/collapse transition. Duration, ExpandDuration and CollapseDuration win over it, and reduced motion collapses it unless ForceAnimation is set.",
        },
        new()
        {
            Name = "--bit-Collapse-easing",
            DefaultValue = "--bit-mot-easing",
            Description = "Timing function of the transition. The Easing parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Collapse-peek-fade",
            DefaultValue = "unset",
            Description = "Length of the fade on the trailing edge of a closed CollapsedSize peek, which shows there is more to read. No fade by default.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColorKind()
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
                    Description = "Custom CSS classes/styles for the content region of the BitCollapse, which is the element that fades between the two states and clips what is outside the collapsed size."
                },
                new()
                {
                    Name = "Wrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper the BitCollapse puts around its content, which is the element that carries the padding."
                }
            ]
        }
    ];



    private bool expanded = true;

    private bool boundExpanded = true;
    private string changeLog = string.Empty;
    private BitCollapse? collapseRef;
    private bool collapseDisabled;

    private bool surfaceExpanded = true;

    private bool horizontalExpanded = true;

    private bool peekExpanded;

    private bool transitionExpanded = true;

    private bool eventsExpanded = true;
    private BitCollapse? eventsCollapseRef;
    private readonly List<string> eventsLog = [];

    private bool clipExpanded = true;

    private bool renderingExpanded;
    private int lazyOpenCount;

    private bool findExpanded = true;

    private bool a11yExpanded;
    private bool focusExpanded;
    private BitButton? focusTriggerRef;
    private BitCollapse? focusCollapseRef;

    private bool cascadingExpanded = true;
    private readonly BitCollapseParams[] collapseParams =
    [
        new()
        {
            Background = BitColorKind.Secondary,
            NoPadding = true,
            Duration = 700,
        }
    ];

    private bool styleExpanded = true;

    private bool rtlExpanded = true;



    private void HandleChange(bool value) => changeLog = $"OnChange({value.ToString().ToLower()})";

    private void HandleEventsChange(bool value) => LogCollapseEvent($"OnChange({value.ToString().ToLower()})");
    private void HandleEventsExpanding() => LogCollapseEvent("OnExpanding");
    private void HandleEventsCollapsing() => LogCollapseEvent("OnCollapsing");
    private void HandleEventsExpanded() => LogCollapseEvent("OnExpanded");
    private void HandleEventsCollapsed() => LogCollapseEvent("OnCollapsed");

    private void LogCollapseEvent(string name)
    {
        eventsLog.Insert(0, name);

        if (eventsLog.Count > 8)
        {
            eventsLog.RemoveAt(eventsLog.Count - 1);
        }
    }

    private async Task HandleFocusExpanded()
    {
        if (focusCollapseRef is not null)
        {
            await focusCollapseRef.FocusAsync();
        }
    }

    private async Task HandleFocusCollapsing()
    {
        if (focusTriggerRef is not null)
        {
            await focusTriggerRef.FocusAsync();
        }
    }
}
