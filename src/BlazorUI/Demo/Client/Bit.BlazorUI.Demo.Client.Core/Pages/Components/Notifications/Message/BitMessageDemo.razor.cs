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
            Description = "Enables the auto-dismiss feature and sets the time to automatically dismiss the message. It runs wherever dismissing would do something - an OnDismiss handler, Dismissible, or a Dismissed binding - and is held while the pointer is over the message, the focus is inside it, or PauseAutoDismiss was called (and, with PauseOnPageHidden / PauseOnWindowBlur, while the page is not being looked at).",
        },
        new()
        {
            Name = "AutoMultiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Switches a single-line message to the Multiline layout (wrapped text, actions on their own row) for as long as its content does not fit on one line, instead of cutting it off. Truncate wins where both are set, and MaxLines only caps an explicitly Multiline message.",
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
            Description = "Determines if the message is multi-lined. If false, and the text overflows over buttons or to another line, it is clipped - unless Truncate or AutoMultiline says otherwise.",
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
            Name = "PauseOnPageHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the AutoDismissTime countdown while the page is hidden (a background tab, a minimized window). Needs the bit BlazorUI services (AddBitBlazorUIServices).",
        },
        new()
        {
            Name = "PauseOnWindowBlur",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the AutoDismissTime countdown while the window does not have the focus. Needs the bit BlazorUI services (AddBitBlazorUIServices).",
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
            Description = "The size of the message: it scales the type, the icon, the insets, the buttons and the countdown track together.",
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
            Name = "Tinted",
            Type = "bool",
            DefaultValue = "false",
            Description = "Washes the surface an Outline or a Text message leaves to the page with a faint tint of its color (the theme's --bit-clr-<role>-tint token); a Fill message ignores it.",
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
            Description = "Determines if the message text is truncated. If true, the content is clipped to a single line and a button unfolds it, rendered only while something is actually clipped. On a Multiline message it unfolds the content past the MaxLines cap instead, and does nothing without one.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The variant of the message. Outline and Text shade the role color toward the foreground for the text, so it keeps a 4.5:1 contrast on the page; Tinted washes their surface with the role.",
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
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitVariant(),
        DemoSharedEnums.BitAlignment(),
        DemoSharedEnums.BitPoliteness(),
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
        DemoSharedEnums.BitSize(),
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Message-color",
            DefaultValue = "The role's on-color (Fill); its main color shaded toward the foreground for 4.5:1 contrast (Outline, Text)",
            Description = "Color of the text, the icon and the buttons.",
        },
        new()
        {
            Name = "--bit-Message-background",
            DefaultValue = "The role's main color (Fill), its tint (Outline and Text with Tinted), transparent (Outline, Text)",
            Description = "Background of the surface.",
        },
        new()
        {
            Name = "--bit-Message-border-color",
            DefaultValue = "The role's main color (Fill, Outline), transparent (Text)",
            Description = "Color of the border.",
        },
        new()
        {
            Name = "--bit-Message-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Thickness of the border; four values draw a single side, e.g. an accent bar.",
        },
        new()
        {
            Name = "--bit-Message-radius",
            DefaultValue = "--bit-shp-radius-surface",
            Description = "Corner radius of the surface and of the countdown track. Square wins over it.",
        },
        new()
        {
            Name = "--bit-Message-shadow",
            DefaultValue = "none",
            Description = "Shadow of the surface. The Elevation parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Message-focus-color",
            DefaultValue = "The role's focus color",
            Description = "Focus ring of the message itself and of its buttons.",
        },
        new()
        {
            Name = "--bit-Message-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Size of the title and the content.",
        },
        new()
        {
            Name = "--bit-Message-line-height",
            DefaultValue = "normal",
            Description = "Line height of the title and the content, e.g. a roomier 1.5 for multiline text.",
        },
        new()
        {
            Name = "--bit-Message-title-color",
            DefaultValue = "The message's own color",
            Description = "Color of the title.",
        },
        new()
        {
            Name = "--bit-Message-title-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the title.",
        },
        new()
        {
            Name = "--bit-Message-icon-color",
            DefaultValue = "The message's own color",
            Description = "Color of the severity icon, or of what IconTemplate renders.",
        },
        new()
        {
            Name = "--bit-Message-icon-size",
            DefaultValue = "Per Size, --bit-siz-icon-*",
            Description = "Size of the severity icon.",
        },
        new()
        {
            Name = "--bit-Message-progress-color",
            DefaultValue = "The message's own color",
            Description = "Color of the auto-dismiss countdown bar.",
        },
        new()
        {
            Name = "--bit-Message-progress-height",
            DefaultValue = "Per Size, --bit-siz-track-*",
            Description = "Thickness of the auto-dismiss countdown track.",
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

    private double autoMultilineWidth = 50;

    private BitVariant colorVariant = BitVariant.Fill;
    private bool colorTinted;

    private readonly BitMessageParams[] messageParams =
    [
        new()
        {
            Variant = BitVariant.Outline,
            Square = true,
            Truncate = true,
        }
    ];
}
