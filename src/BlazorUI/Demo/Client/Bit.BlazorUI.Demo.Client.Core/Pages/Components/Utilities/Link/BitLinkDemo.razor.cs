namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Link;

public partial class BitLinkDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps a disabled link in the tab order, conveying the disabled state through aria-disabled instead.",
        },
        new()
        {
            Name = "AriaCurrent",
            Type = "BitNavAriaCurrent?",
            DefaultValue = "null",
            Description = "Reports the link as the current item of the set it belongs to, through the aria-current attribute, and keeps it underlined at rest. With a Match, the URL decides whether the link is current and this only which kind (Page when not set).",
            LinkType = LinkType.Link,
            Href = "#nav-aria-current-enum",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Visually hidden text the link points at through aria-describedby, read after its name - the place for a file's size or format.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives the link the focus on its first render, in static and interactive rendering alike. A disabled link only takes it with AllowDisabledFocus.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the link, can be any custom tag or a text.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the link, Primary when not set. Accent colors use the role's readable text shade.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "Makes the browser save the linked resource instead of opening it; a value suggests the file name. Honored for same-origin, blob: and data: URLs only.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL the link points to; without one the link renders a button. A value starting with # scrolls the element with that id into view and moves the focus there.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "An icon from an external library (FontAwesome, Bootstrap Icons, ...) rendered beside the content. Wins over IconName.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of a built-in Fluent UI icon rendered beside the content. The icon is hidden from assistive technologies.",
        },
        new()
        {
            Name = "IconPosition",
            Type = "BitIconPosition?",
            DefaultValue = "null",
            Description = "Where the icon sits: before the content (default) or after it (End).",
            LinkType = LinkType.Link,
            Href = "#icon-position-enum",
        },
        new()
        {
            Name = "Match",
            Type = "BitNavMatch?",
            DefaultValue = "null",
            Description = "Follows the current URL like Blazor's NavLink: while the Href matches it, the link renders aria-current (the AriaCurrent kind, page when not set) and stays underlined. Exact matches the page, Prefix every page under it too; Regex and Wildcard read the Href as the pattern. In-page (#) links never match.",
            LinkType = LinkType.Link,
            Href = "#nav-match-enum",
        },
        new()
        {
            Name = "NewTabHint",
            Type = "string?",
            DefaultValue = "null",
            Description = "Replaces the visually hidden \"(opens in a new tab)\" a _blank link is announced with, e.g. to translate it. An empty value removes it.",
        },
        new()
        {
            Name = "NoColor",
            Type = "bool",
            DefaultValue = "false",
            Description = "Drops the link color, so the content keeps its own.",
        },
        new()
        {
            Name = "NoNewTabHint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops a _blank link from announcing that it opens a new tab. Only for where the page already says so.",
        },
        new()
        {
            Name = "NoUnderline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the underline at every state. Wins over Underlined.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Invoked on click: alongside the navigation on an anchor, as the whole action on a button (no Href).",
        },
        new()
        {
            Name = "PreventDefault",
            Type = "bool",
            DefaultValue = "false",
            Description = "Cancels the navigation of a click, leaving OnClick as the whole action. The href stays, so a middle click and \"copy link address\" still work.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The relationship to the linked document, as combinable flags. Ignored for # hrefs. A _blank link without NoOpener, NoReferrer or Opener gets noopener added.",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The preset text size. Unset, the link takes the font size of whatever it sits in.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the click from reaching the parent elements, e.g. a clickable row or card.",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "Where to open the link, e.g. _blank for a new tab - which also adds noopener (unless Rel says otherwise) and the new-tab announcement.",
            LinkType = LinkType.Link,
            Href = "#link-target",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "A tooltip shown on hover. Touch and the keyboard never reach it, so it only adds to what the text says.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the underline at every state - the right choice inside body text.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Link-color",
            DefaultValue = "from the Color role",
            Description = "Text color at rest. Wins over the Color parameter.",
        },
        new()
        {
            Name = "--bit-Link-hover-color",
            DefaultValue = "from the Color role",
            Description = "Text color under the pointer and while focused from the keyboard.",
        },
        new()
        {
            Name = "--bit-Link-active-color",
            DefaultValue = "from the Color role",
            Description = "Text color while pressed.",
        },
        new()
        {
            Name = "--bit-Link-visited-color",
            DefaultValue = "--bit-Link-color",
            Description = "Text color of a link whose destination was already visited.",
        },
        new()
        {
            Name = "--bit-Link-disabled-color",
            DefaultValue = "from the Color role",
            Description = "Text color and focus ring color of a disabled link.",
        },
        new()
        {
            Name = "--bit-Link-current-color",
            DefaultValue = "--bit-Link-color",
            Description = "Text color of the current link (aria-current, or a Match on the URL) at rest.",
        },
        new()
        {
            Name = "--bit-Link-focus-color",
            DefaultValue = "from the Color role",
            Description = "Color of the keyboard focus ring.",
        },
        new()
        {
            Name = "--bit-Link-font-family",
            DefaultValue = "--bit-tpg-font-family",
            Description = "Typeface of the text. Set it to inherit where a link has to match the typeface around it.",
        },
        new()
        {
            Name = "--bit-Link-font-size",
            DefaultValue = "inherit",
            Description = "Size of the text. Wins over the Size parameter.",
        },
        new()
        {
            Name = "--bit-Link-font-weight",
            DefaultValue = "inherit",
            Description = "Weight of the text.",
        },
        new()
        {
            Name = "--bit-Link-current-font-weight",
            DefaultValue = "--bit-Link-font-weight",
            Description = "Weight of the text of the current link.",
        },
        new()
        {
            Name = "--bit-Link-underline-color",
            DefaultValue = "currentColor",
            Description = "Color of the underline.",
        },
        new()
        {
            Name = "--bit-Link-underline-thickness",
            DefaultValue = "auto",
            Description = "Thickness of the underline.",
        },
        new()
        {
            Name = "--bit-Link-underline-offset",
            DefaultValue = "auto",
            Description = "Gap between the text and its underline.",
        },
        new()
        {
            Name = "--bit-Link-icon-gap",
            DefaultValue = "spacing(0.375) (3px)",
            Description = "Room between the icon and the text.",
        },
        new()
        {
            Name = "--bit-Link-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the focus ring.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Moves the focus onto the link. A disabled link only takes it with AllowDisabledFocus.",
        },
        new()
        {
            Name = "FocusAsync(bool preventScroll)",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Moves the focus onto the link; true keeps the page scrolled where it is.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "link-target",
            Title = "BitLinkTarget",
            Parameters =
            [
                new()
                {
                    Name = "Self",
                    Description = "The current browsing context. (Default)",
                    DefaultValue = "_self",
                },
                new()
                {
                    Name = "Blank",
                    Description = "Usually a new tab, but users can configure browsers to open a new window instead.",
                    DefaultValue = "_blank",
                },
                new()
                {
                    Name = "Parent",
                    Description = "The parent browsing context of the current one. If no parent, behaves as _self.",
                    DefaultValue = "_parent",
                },
                new()
                {
                    Name = "Top",
                    Description = "The topmost browsing context. To be specific, this means the 'highest' context that's an ancestor of the current one. If no ancestors, behaves as _self.",
                    DefaultValue = "_top",
                },
                new()
                {
                    Name = "UnfencedTop",
                    Description = "Allows embedded fenced frames to navigate the top-level frame.",
                    DefaultValue = "_unfencedTop",
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitNavAriaCurrent(),
        new()
        {
            Id = "nav-match-enum",
            Name = "BitNavMatch",
            Description = "Defines how the Href of a link is matched against the current URL.",
            Items =
            [
                new()
                {
                    Name = "Exact",
                    Description = "Matches the page the Href points at. The case, a trailing slash and a query the Href does not carry are ignored.",
                    Value = "0",
                },
                new()
                {
                    Name = "Prefix",
                    Description = "Matches the page the Href points at and every page under it.",
                    Value = "1",
                },
                new()
                {
                    Name = "Regex",
                    Description = "Reads the Href as a regular expression matched against the current URL.",
                    Value = "2",
                },
                new()
                {
                    Name = "Wildcard",
                    Description = "Reads the Href as a wildcard pattern (* within a segment, ** across segments) matched against the current URL.",
                    Value = "3",
                }
            ]
        },
        DemoSharedEnums.BitIconPosition(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitLinkRels()
    ];



    private int buttonClickCount;
    private int anchorClickCount;
    private int innerClickCount;
    private int containerClickCount;
    private string? guardMessage;

    private int currentStep;
    private readonly string[] checkoutSteps = ["Cart", "Shipping", "Payment"];

    private BitLink focusTargetRef = default!;

    private readonly BitLinkParams[] linkParams =
    [
        new()
        {
            Underlined = true,
            Target = BitLinkTarget.Blank,
            IconName = BitIconName.OpenInNewWindow,
            IconPosition = BitIconPosition.End,
        }
    ];

    private void HandleGuardedClick()
    {
        // The browser did not navigate, so what happens next is entirely up to this handler:
        // confirm, save a draft, track the click, and then navigate from here if it should happen.
        guardMessage = "The navigation was suppressed. This is where a confirmation would go.";
    }
}
