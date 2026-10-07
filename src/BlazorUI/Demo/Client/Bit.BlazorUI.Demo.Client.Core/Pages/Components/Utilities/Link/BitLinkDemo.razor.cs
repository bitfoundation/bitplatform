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
            Name = "IconPlacement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "Where the icon sits: before the content (default) or after it (End).",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
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
            Href = "#link-rels",
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
            Description = "Where to open the link, e.g. _blank for a new tab - which also adds noopener (unless Rel already says NoOpener, NoReferrer or Opener) and the new-tab announcement.",
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
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
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
            Id = "nav-aria-current-enum",
            Name = "BitNavAriaCurrent",
            Description = "Defines the value of the aria-current attribute reported by the current link of a set.",
            Items =
            [
                new()
                {
                    Name = "Page",
                    Description = "Represents the current page within a set of pages.",
                    Value = "0",
                },
                new()
                {
                    Name = "Step",
                    Description = "Represents the current step within a process.",
                    Value = "1",
                },
                new()
                {
                    Name = "Location",
                    Description = "Represents the current location within an environment or context.",
                    Value = "2",
                },
                new()
                {
                    Name = "Date",
                    Description = "Represents the current date within a collection of dates.",
                    Value = "3",
                },
                new()
                {
                    Name = "Time",
                    Description = "Represents the current time within a set of times.",
                    Value = "4",
                },
                new()
                {
                    Name = "True",
                    Description = "Represents the current item within a set, without saying which kind of set it is.",
                    Value = "5",
                }
            ]
        },
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
        SharedSubEnums.BitPlacement,
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the preset sizes available in the bit BlazorUI.",
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
        },
        new()
        {
            Id = "link-rels",
            Name = "BitLinkRels",
            Description = "The rel attribute defines the relationship between a linked resource and the current document.",
            Items =
            [
                new()
                {
                    Name = "Alternate",
                    Value = "1",
                    Description = "Provides a link to an alternate representation of the document. (i.e. print page, translated or mirror)"
                },
                new()
                {
                    Name = "Author",
                    Value = "2",
                    Description = "Provides a link to the author of the document."
                },
                new()
                {
                    Name = "Bookmark",
                    Value = "4",
                    Description = "Permanent URL used for bookmarking."
                },
                new()
                {
                    Name = "External",
                    Value = "8",
                    Description = "Indicates that the referenced document is not part of the same site as the current document."
                },
                new()
                {
                    Name = "Help",
                    Value = "16",
                    Description = "Provides a link to a help document."
                },
                new()
                {
                    Name = "License",
                    Value = "32",
                    Description = "Provides a link to licensing information for the document."
                },
                new()
                {
                    Name = "Next",
                    Value = "64",
                    Description = "Provides a link to the next document in the series."
                },
                new()
                {
                    Name = "NoFollow",
                    Value = "128",
                    Description = @"Links to an unendorsed document, like a paid link. (""NoFollow"" is used by Google, to specify that the Google search spider should not follow that link)"
                },
                new()
                {
                    Name = "NoOpener",
                    Value = "256",
                    Description = "Requires that any browsing context created by following the hyperlink must not have an opener browsing context."
                },
                new()
                {
                    Name = "NoReferrer",
                    Value = "512",
                    Description = "Makes the referrer unknown. No referrer header will be included when the user clicks the hyperlink."
                },
                new()
                {
                    Name = "Prev",
                    Value = "1024",
                    Description = "The previous document in a selection."
                },
                new()
                {
                    Name = "Search",
                    Value = "2048",
                    Description = "Links to a search tool for the document."
                },
                new()
                {
                    Name = "Tag",
                    Value = "4096",
                    Description = "A tag (keyword) for the current document."
                },
                new()
                {
                    Name = "Me",
                    Value = "8192",
                    Description = "Indicates that the linked document represents the person who owns the current content. (used for identity verification)"
                },
                new()
                {
                    Name = "Opener",
                    Value = "16384",
                    Description = "Requires that any browsing context created by following the hyperlink keeps its opener browsing context. (reverses the implicit noopener modern browsers apply to _blank targets)"
                },
                new()
                {
                    Name = "PrivacyPolicy",
                    Value = "32768",
                    Description = "Links to the privacy policy that applies to the current document. (rendered as privacy-policy)"
                },
                new()
                {
                    Name = "Sponsored",
                    Value = "65536",
                    Description = "Marks the link as an advertisement or paid placement, so search engines do not count it as an organic endorsement."
                },
                new()
                {
                    Name = "TermsOfService",
                    Value = "131072",
                    Description = "Links to the terms of service that apply to the current document. (rendered as terms-of-service)"
                },
                new()
                {
                    Name = "Ugc",
                    Value = "262144",
                    Description = "Marks the link as user-generated content, like forum posts or comments, for search engines."
                }
            ]
        }
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
            IconPlacement = BitPlacement.End,
        }
    ];

    private void HandleGuardedClick()
    {
        // The browser did not navigate, so what happens next is entirely up to this handler:
        // confirm, save a draft, track the click, and then navigate from here if it should happen.
        guardMessage = "The navigation was suppressed. This is where a confirmation would go.";
    }
}
