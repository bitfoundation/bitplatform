namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Message;

public partial class BitMessageDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Actions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the action to show on the message.",
        },
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Determines the alignment of the content section of the message.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "AutoDismissTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "Enables the auto-dismiss feature and sets the time to automatically dismiss the message. It runs wherever dismissing would do something - an OnDismiss handler, Dismissible, or a Dismissed binding - and is held while the pointer is over the message, the focus is inside it, or PauseAutoDismiss was called.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the focus to the message as soon as it is rendered. The root is made focusable (tabindex=\"-1\") while no explicit TabIndex is given.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of message.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitMessageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitMessage.",
            LinkType = LinkType.Link,
            Href = "#message-class-styles",
        },
        new()
        {
            Name = "CollapseAriaLabel",
            Type = "string",
            DefaultValue = "\"Collapse\"",
            Description = "The aria-label and the tooltip of the expander button of the message in Truncate mode while it is expanded.",
        },
        new()
        {
            Name = "CollapseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the collapse button in Truncate mode using custom CSS classes for external icon libraries. Takes precedence over CollapseIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CollapseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the collapse icon in Truncate mode from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the message.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The alias for ChildContent.",
        },
        new()
        {
            Name = "DelayedAnnouncement",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the content of the message back for one render, so its live region is already on the page when the text lands in it, which is what makes the announcement reliable.",
        },
        new()
        {
            Name = "DismissAriaLabel",
            Type = "string",
            DefaultValue = "\"Dismiss\"",
            Description = "The aria-label and the tooltip of the dismiss button of the message.",
        },
        new()
        {
            Name = "Dismissed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the message has been dismissed, which is two-way bindable. A dismissed message renders nothing, and setting it back to false brings the message back and re-arms its AutoDismissTime countdown. The message only sets it itself while Dismissible is set or the parameter is bound.",
        },
        new()
        {
            Name = "DismissedChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "null",
            Description = "The callback that is called when the Dismissed value changes, for subscribing to the change without binding to Dismissed.",
        },
        new()
        {
            Name = "Dismissible",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the dismiss button and lets the message dismiss itself by setting Dismissed, without an OnDismiss handler having to take it off the page.",
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the dismiss button using custom CSS classes for external icon libraries. Takes precedence over DismissIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the dismiss icon from the built-in Fluent UI icons. If unset, default will be the Fluent UI Cancel icon.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "DismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Dismisses the message when the Escape key is pressed while the focus is inside it. Only wired up while dismissing would do something - that is, while OnDismiss has a handler, Dismissible is set, or Dismissed is bound.",
        },
        new()
        {
            Name = "Elevation",
            Type = "int?",
            DefaultValue = "null",
            Description = "Determines the elevation of the message, a scale from 1 to 24.",
        },
        new()
        {
            Name = "ExpandAriaLabel",
            Type = "string",
            DefaultValue = "\"Expand\"",
            Description = "The aria-label and the tooltip of the expander button of the message in Truncate mode while it is collapsed.",
        },
        new()
        {
            Name = "Expanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the truncated content of the message is expanded, which is two-way bindable. Only meaningful together with Truncate.",
        },
        new()
        {
            Name = "ExpandedChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "null",
            Description = "The callback that is called when the Expanded value changes, for subscribing to the change without binding to Expanded.",
        },
        new()
        {
            Name = "ExpandIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the expand button in Truncate mode using custom CSS classes for external icon libraries. Takes precedence over ExpandIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ExpandIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the expand icon in Truncate mode from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "HideIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the icon of the message.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text that says out loud what the icon of the message means, rendered invisibly at the start of the announced region. Set it where the text of the message does not already say what kind of message it is.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons. If unset, the icon will be selected automatically based on Color.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render in place of the icon of the message, which takes precedence over Icon and IconName.",
        },
        new()
        {
            Name = "MaxLines",
            Type = "int?",
            DefaultValue = "null",
            Description = "Caps how many lines the content of the message may wrap over in Multiline mode, ending the last of them in an ellipsis. Pair it with Truncate to give the reader the expander button that unfolds the rest.",
        },
        new()
        {
            Name = "Multiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message is multi-lined. If false, and the text overflows over buttons or to another line, it is clipped.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            Description = "Reports that the message was dismissed - by its button, the Escape key, the countdown or a DismissAsync call - and renders the dismiss button that does it. Taking the message off the page is left to this callback; use Dismissible to have the message do that itself.",
        },
        new()
        {
            Name = "OnDismissing",
            Type = "EventCallback<BitMessageDismissArgs>",
            Description = "Callback invoked before the message is dismissed, letting the dismissal be cancelled. Set Cancel on the provided args to keep the message where it is, and read its Reason to tell the dismiss button, the Escape key, the countdown and a DismissAsync call apart. Refusing a countdown gives the message its AutoDismissTime over again.",
            LinkType = LinkType.Link,
            Href = "#message-dismiss-args",
        },
        new()
        {
            Name = "Politeness",
            Type = "BitPoliteness?",
            DefaultValue = "null",
            Description = "How urgently the message interrupts a screen reader (aria-live), independently of the role it is announced under. Left unset, the role carries the urgency on its own: alert interrupts, status waits its turn.",
            LinkType = LinkType.Link,
            Href = "#politeness-enum",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom role to apply to the message text. If unset, Warning, SevereWarning and Error announce as \"alert\" and every other color as \"status\". Set it to \"none\" for a message that should not be announced at all.",
        },
        new()
        {
            Name = "ShowAutoDismissProgress",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a bar along the bottom edge of the message that runs down as its AutoDismissTime does, holding wherever the countdown holds. It only renders where there is a countdown to show.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of Message, Possible values: Small | Medium | Large.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Square",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the rounded corners of the message so it can sit flush against the edges of its container as a banner.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitMessageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitMessage.",
            LinkType = LinkType.Link,
            Href = "#message-class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (heading) of the message, rendered above the content in multiline mode and ahead of it otherwise.",
        },
        new()
        {
            Name = "TitleElement",
            Type = "string?",
            DefaultValue = "null",
            Description = "The HTML element the title of the message is rendered as. The default is a div; set it to a heading (h2 ... h6) where the message is a part of the page a reader should be able to jump to.",
        },
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render as the title (heading) of the message, which takes precedence over Title.",
        },
        new()
        {
            Name = "Truncate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message text is truncated. If true, the content is clipped to a single line and a button renders to unfold it, for a message that has to fit in a tight space. On a Multiline message it unfolds the content past the MaxLines cap instead, and does nothing without one.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The variant of the message.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "CollapseAsync",
            Type = "Task",
            Description = "Folds the truncated content of the message back into a single line, the way its expander button does.",
        },
        new()
        {
            Name = "DismissAsync",
            Type = "Task",
            Description = "Dismisses the message the same way its dismiss button does: the countdown is stopped, the message takes itself off the page while it owns its dismissal, and OnDismiss is invoked.",
        },
        new()
        {
            Name = "ExpandAsync",
            Type = "Task",
            Description = "Unfolds the truncated content of the message, the way its expander button does.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Moves the focus to the message, which has to be focusable for the focus to land: either give it a TabIndex or set AutoFocus.",
        },
        new()
        {
            Name = "PauseAutoDismiss",
            Type = "void",
            Description = "Holds the AutoDismissTime countdown where it is, the way hovering the message does, for holding it over something the message cannot see.",
        },
        new()
        {
            Name = "ResumeAutoDismiss",
            Type = "void",
            Description = "Lets the AutoDismissTime countdown spend its time again after a PauseAutoDismiss, from wherever it was held.",
        },
        new()
        {
            Name = "ToggleExpandAsync",
            Type = "Task",
            Description = "Turns the truncated content of the message over: unfolds it while it is folded, folds it while it is not.",
        },
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
                    Name = "Primary",
                    Description = "Primary general color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "Secondary general color.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "Tertiary general color.",
                    Value = "2",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info general color.",
                    Value = "3",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success general color.",
                    Value = "4",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning general color.",
                    Value = "5",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning general color.",
                    Value = "6",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error general color.",
                    Value = "7",
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
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Value = "7",
                }
            ]
        },
        new()
        {
            Id = "politeness-enum",
            Name = "BitPoliteness",
            Description = "How urgently a live region interrupts a screen reader, which is what the aria-live attribute carries.",
            Items =
            [
                new()
                {
                    Name = "Off",
                    Description = "The region is not a live region: nothing in it is announced as it changes (aria-live=\"off\").",
                    Value = "0",
                },
                new()
                {
                    Name = "Polite",
                    Description = "The change waits its turn and is announced once the screen reader has finished what it was saying (aria-live=\"polite\").",
                    Value = "1",
                },
                new()
                {
                    Name = "Assertive",
                    Description = "The change interrupts the screen reader and is announced right away (aria-live=\"assertive\").",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "message-dismiss-reason-enum",
            Name = "BitMessageDismissReason",
            Description = "What made the message dismiss, handed to the OnDismissing callback.",
            Items =
            [
                new()
                {
                    Name = "Button",
                    Description = "The dismiss button of the message was pressed.",
                    Value = "0",
                },
                new()
                {
                    Name = "Escape",
                    Description = "The Escape key was pressed while the focus was inside the message.",
                    Value = "1",
                },
                new()
                {
                    Name = "AutoDismiss",
                    Description = "The AutoDismissTime countdown of the message ran out.",
                    Value = "2",
                },
                new()
                {
                    Name = "Programmatic",
                    Description = "The DismissAsync method of the message was called.",
                    Value = "3",
                },
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
                    Description="The small size message.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size message.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size message.",
                    Value="2",
                }
            ]
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "message-class-styles",
            Title = "BitMessageClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitMessage."
                },
                new()
                {
                    Name = "RootContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root container of the BitMessage."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon and content container of the BitMessage."
                },
                new()
                {
                    Name = "IconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon container of the BitMessage."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon element of the BitMessage."
                },
                new()
                {
                    Name = "IconLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden icon label of the BitMessage."
                },
                new()
                {
                    Name = "ContentContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content container of the BitMessage."
                },
                new()
                {
                    Name = "ContentWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content wrapper element of the BitMessage."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title element of the BitMessage."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content element of the BitMessage."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions element of the BitMessage."
                },
                new()
                {
                    Name = "ExpanderButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the truncate expander button of the BitMessage."
                },
                new()
                {
                    Name = "ExpanderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the truncate expander icon of the BitMessage."
                },
                new()
                {
                    Name = "DismissButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss button of the BitMessage."
                },
                new()
                {
                    Name = "DismissIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss icon of the BitMessage."
                },
                new()
                {
                    Name = "AutoDismissProgress",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the auto-dismiss progress track of the BitMessage."
                },
                new()
                {
                    Name = "AutoDismissProgressBar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the auto-dismiss progress bar of the BitMessage."
                },
            ]
        },
        new()
        {
            Id = "message-dismiss-args",
            Title = "BitMessageDismissArgs",
            Parameters =
            [
                new()
                {
                    Name = "Reason",
                    Type = "BitMessageDismissReason",
                    DefaultValue = "",
                    Description = "What made the message dismiss: its dismiss button, the Escape key, the auto-dismiss countdown, or a call to the DismissAsync method.",
                    LinkType = LinkType.Link,
                    Href = "#message-dismiss-reason-enum",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the dismissal and keep the message where it is."
                },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon."
                },
                new()
                {
                    Name = "BaseClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
                },
            ]
        }
    ];



    private bool isDismissed;
    private bool isSelfDismissed;
    private bool isAutoDismissed;
    private bool isProgressDismissed;
    private bool isPausedDismissed;
    private BitMessage? pausableMessage;
    private bool isEscapeDismissed;
    private bool isMethodDismissed;
    private BitMessage? dismissableMessage;
    private bool isDelayedDismissed = true;

    private int dismissAttempts;
    private bool isGuardedDismissed;
    private BitMessageDismissReason? lastDismissReason;

    private void HandleDismissing(BitMessageDismissArgs args)
    {
        dismissAttempts++;
        lastDismissReason = args.Reason;

        // The first attempt is refused; the next one goes through.
        args.Cancel = dismissAttempts < 2;
    }

    private void ResetGuardedMessage()
    {
        dismissAttempts = 0;
        lastDismissReason = null;
        isGuardedDismissed = false;
    }

    private bool isTruncateExpanded;
    private BitMessage? truncatedMessage;

    private BitMessage? focusableMessage;
    private bool isAutoFocusDismissed = true;

    private bool isMessageEnabled = true;
    private bool isDisabledSampleDismissed;

    private double elevation = 7;
    private bool isErrorDismissed;
    private bool isWarningDismissed;
}
