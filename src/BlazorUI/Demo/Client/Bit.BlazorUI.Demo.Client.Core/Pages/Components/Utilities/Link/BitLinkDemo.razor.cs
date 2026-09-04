namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Link;

public partial class BitLinkDemo
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;


    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the disabled link focusable and discoverable by assistive technologies, conveying the disabled state using the aria-disabled attribute.",
        },
        new()
        {
            Name = "AriaCurrent",
            Type = "BitNavAriaCurrent?",
            DefaultValue = "null",
            Description = "Reports the link as the current item of the set it belongs to, through the aria-current attribute. Only one link of a set is ever the current one.",
            LinkType = LinkType.Link,
            Href = "#nav-aria-current-enum",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "A longer description of the link for the benefit of screen readers, rendered as visually hidden text the link points at through aria-describedby. It is read out after the name rather than as part of it.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives the link the focus as soon as it is rendered, through the autofocus attribute. The browser honors it once per document, on the first element that asks for it.",
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
            Description = "The general color of the link.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the download attribute of the link when the Href parameter is provided. Instructs the browser to download the linked resource instead of navigating to it, using the provided value (if any) as the suggested file name.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL the link points to. If provided, the component renders an anchor tag, otherwise a button. A value starting with the # character makes the link smooth-scroll the element with that id into view and move the focus to it.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon rendered beside the link content, using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon rendered beside the link content, from the built-in Fluent UI icons. The glyph is decorative and hidden from assistive technologies.",
        },
        new()
        {
            Name = "IconPosition",
            Type = "BitIconPosition?",
            DefaultValue = "null",
            Description = "The position of the icon relative to the link content. The icon goes in front of the text by default; End puts it after the text.",
            LinkType = LinkType.Link,
            Href = "#icon-position-enum",
        },
        new()
        {
            Name = "NewTabHint",
            Type = "string?",
            DefaultValue = "null",
            Description = "Replaces the text a new-tab link is announced with. A _blank link carries \"(opens in a new tab)\" as visually hidden text after its content, or appended to its AriaLabel when it has one. An empty value takes the announcement off.",
        },
        new()
        {
            Name = "NoColor",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes applying any foreground color to the link content, letting it keep its own color.",
        },
        new()
        {
            Name = "NoNewTabHint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops a new-tab link from announcing that it opens in a new tab. Only set it where the page already says so.",
        },
        new()
        {
            Name = "NoUnderline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Styles the link to have no underline at any state. It wins over Underlined when both are set.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the link is clicked. It is invoked in every render mode of the link: on anchor links it runs alongside the navigation, and on button links (no Href) it is the sole click action.",
        },
        new()
        {
            Name = "PreventDefault",
            Type = "bool",
            DefaultValue = "false",
            Description = "Suppresses the navigation a click on the link would otherwise perform, leaving OnClick as the whole of what the click does. The anchor keeps its Href, so a middle click and \"copy link address\" still reach the destination.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "If Href provided, specifies the relationship between the current document and the linked document. Ignored for empty or hash-only (#) hrefs. When Target is _blank and no opener-related rel (NoOpener, NoReferrer or Opener) is provided, noopener is added automatically.",
            LinkType = LinkType.Link,
            Href = "#link-rels",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Sets the preset size of the link text. With nothing set the link takes the font size of whatever it sits in.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, stops the propagation of the click event to the parent elements. Useful when the link is placed inside clickable containers like rows or cards.",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "If Href provided, specifies how to open the link (e.g. _blank to open it in a new tab). When set to _blank and no opener-related Rel is provided, noopener is added to the rel attribute automatically.",
            LinkType = LinkType.Link,
            Href = "#link-target",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the link. Neither touch nor the keyboard reaches it, so nothing the reader has to have belongs only here.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Styles the link with a fixed underline at all states. NoUnderline wins over it when both are set.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Gives focus to the root element of the link. A disabled link is only focusable when AllowDisabledFocus keeps it in the tab order.",
        },
        new()
        {
            Name = "FocusAsync(bool preventScroll)",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Gives focus to the root element of the link. Passing true keeps the page scrolled where it is; passing false lets the browser scroll the link into view.",
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
                    Description="Info Primary general color.",
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
            Id = "icon-position-enum",
            Name = "BitIconPosition",
            Description = "Describes the placement of an icon relative to other content.",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                    Description = "The icon renders before the content."
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                    Description = "The icon renders after the content."
                }
            ]
        },
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



    private int clickCount;
    private int linkClickCount;
    private int containerClickCount;
    private string? guardMessage;

    private void HandleOnClick()
    {
        Navigation.NavigateTo("https://github.com/bitfoundation/bitplatform");
    }

    private void HandleGuardedClick()
    {
        // The browser did not navigate, so what happens next is entirely up to this handler:
        // confirm, save a draft, track the click, and then navigate from here if it should happen.
        guardMessage = "The navigation was suppressed. This is where a confirmation would go.";
    }
}
