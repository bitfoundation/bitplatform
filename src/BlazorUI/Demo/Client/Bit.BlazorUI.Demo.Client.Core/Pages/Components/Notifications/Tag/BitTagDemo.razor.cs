namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Tag;

public partial class BitTagDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps a disabled button, link or dismiss button in the tab order, reporting aria-disabled instead of disabled."
        },
        new()
        {
            Name = "AriaCurrent",
            Type = "BitNavAriaCurrent",
            DefaultValue = "BitNavAriaCurrent.True",
            Description = "The aria-current value a selected link tag reports. A toggle tag reports aria-pressed instead.",
            LinkType = LinkType.Link,
            Href = "#nav-aria-current-enum"
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "A description screen readers read after the name, through a visually hidden element referenced by aria-describedby."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content in place of Text and SecondaryText; the icons, the image and the checkmark still render around it."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the tag.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "DefaultSelected",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial selection of a tag that keeps its own state (an uncontrolled toggle)."
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The dismiss glyph from an external icon library. Takes precedence over DismissIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The dismiss glyph from the built-in icons. Defaults to Cancel.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "DismissLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name and tooltip of the dismiss button. Defaults to DismissLabelFormat applied to Text (or AriaLabel), else \"Dismiss\"."
        },
        new()
        {
            Name = "DismissLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the dismiss button name, where {0} is the Text. Defaults to \"Remove {0}\"; use it to reword or translate."
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "Downloads the Href instead of navigating to it, suggesting this file name (empty keeps the server's)."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the tag to the width of its container."
        },
        new()
        {
            Name = "HideSelectedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the checkmark of a selected tag."
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "Makes the tag a link to this URL. A disabled link drops the href and leaves the tab order."
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The leading glyph from an external icon library. Takes precedence over IconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "IconAlt",
            Type = "string?",
            DefaultValue = "null",
            Description = "The alt text of the IconUrl picture, which is decorative (empty alt) by default."
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The leading glyph from the built-in icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "IconUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "A picture shown in place of the icon, cropped to a circle. Ignored while Icon or IconName is set."
        },
        new()
        {
            Name = "NewTabHint",
            Type = "string?",
            DefaultValue = "null",
            Description = "The announcement of a Target=\"_blank\" link, \"(opens in a new tab)\" by default; an empty value removes it."
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the text on one line, ending it with an ellipsis where it does not fit."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Called after Selected changes. Setting it makes the tag a toggle (except a link, whose click navigates)."
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitTagChangeArgs>",
            DefaultValue = "",
            Description = "Called (and awaited) before Selected changes; set Cancel on the args to keep the current state.",
            LinkType = LinkType.Link,
            Href = "#tag-change-args"
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Called on click; setting it makes the tag a button."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Called on dismiss; setting it shows the dismiss button. Delete and Backspace trigger it too."
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom markup added at the start of the tag, before the icon."
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The rel of the link. A _blank link without one gets rel=\"noopener\".",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum"
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Mirrors the order of the whole row, dismiss button included."
        },
        new()
        {
            Name = "SecondaryIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The trailing glyph from an external icon library. Takes precedence over SecondaryIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "SecondaryIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The trailing glyph from the built-in icons, after the label and before the dismiss button.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "A smaller second line under the Text."
        },
        new()
        {
            Name = "Selected",
            Type = "bool",
            DefaultValue = "false",
            Description = "The selected state: selected colors and a checkmark. Binding it makes the tag a toggle reporting aria-pressed (aria-current on a link)."
        },
        new()
        {
            Name = "SelectedChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Called when Selected changes; what @bind-Selected uses."
        },
        new()
        {
            Name = "SelectedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The checkmark glyph from an external icon library. Takes precedence over SelectedIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "SelectedIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The checkmark glyph from the built-in icons. Defaults to Accept.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "Shape",
            Type = "BitTagShape?",
            DefaultValue = "null",
            Description = "The corner shape of the tag.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the tag.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the click from reaching the parent elements."
        },
        new()
        {
            Name = "Styles",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom markup added at the end of the tag, before the dismiss button."
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "Where the link opens, e.g. _blank."
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the tag."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The native tooltip of the tag, e.g. the full text behind a NoWrap ellipsis."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the tag.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Focuses the tag's button or link, else its dismiss button, else the tag itself (which needs a TabIndex)."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "tag-class-styles",
            Title = "BitTagClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTag."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content element of the BitTag: the anchor or button of a link or clickable tag, a span otherwise."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitTag, which is the element holding its text and secondary text."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitTag."
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary text of the BitTag."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitTag."
               },
               new()
               {
                   Name = "Image",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image of the BitTag."
               },
               new()
               {
                   Name = "SecondaryIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the trailing icon of the BitTag, which is rendered after the label."
               },
               new()
               {
                   Name = "Selected",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTag while it is selected."
               },
               new()
               {
                   Name = "SelectedIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the checkmark icon a selected BitTag shows."
               },
               new()
               {
                   Name = "DismissButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss button of the BitTag."
               },
               new()
               {
                   Name = "DismissIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss icon of the BitTag."
               },
            ]
        },
        new()
        {
            Id = "tag-change-args",
            Title = "BitTagChangeArgs",
            Description = "The arguments of the OnChanging callback of the BitTag.",
            Parameters =
            [
               new()
               {
                   Name = "Value",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "The selection state the tag is about to move to."
               },
               new()
               {
                   Name = "Cancel",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Set to true to cancel the change and keep the current selection state."
               }
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
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "nav-aria-current-enum",
            Name = "BitNavAriaCurrent",
            Description = "Defines the value of the aria-current attribute reported by a selected link.",
            Items =
            [
                new()
                {
                    Name= "Page",
                    Description="Represents the current page within a set of pages.",
                    Value="0",
                },
                new()
                {
                    Name= "Step",
                    Description="Represents the current step within a process.",
                    Value="1",
                },
                new()
                {
                    Name= "Location",
                    Description="Represents the current location within an environment or context.",
                    Value="2",
                },
                new()
                {
                    Name= "Date",
                    Description="Represents the current date within a collection of dates.",
                    Value="3",
                },
                new()
                {
                    Name= "Time",
                    Description="Represents the current time within a set of times.",
                    Value="4",
                },
                new()
                {
                    Name= "True",
                    Description="Represents the current item within a set, without saying which kind of set it is.",
                    Value="5",
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
            Id = "shape-enum",
            Name = "BitTagShape",
            Description = "Determines the corner shape of the BitTag.",
            Items =
            [
                new()
                {
                    Name= "Rounded",
                    Description="Takes the chip corner of the current theme, which is a pill in Cupertino and a small radius in Fluent and Material.",
                    Value="0",
                },
                new()
                {
                    Name= "Circular",
                    Description="Rounds the corner fully, so the tag is always a pill whatever the theme says.",
                    Value="1",
                },
                new()
                {
                    Name= "Square",
                    Description="Drops the corner altogether, so the tag is a rectangle.",
                    Value="2",
                }
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
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
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
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "link-rels-enum",
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
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Tag-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Text and glyphs at rest.",
        },
        new()
        {
            Name = "--bit-Tag-background",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Background at rest.",
        },
        new()
        {
            Name = "--bit-Tag-border-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Rule at rest.",
        },
        new()
        {
            Name = "--bit-Tag-icon-color",
            DefaultValue = "The text color",
            Description = "Leading, trailing and checkmark glyphs (not the dismiss glyph). Disabled tags ignore it.",
        },
        new()
        {
            Name = "--bit-Tag-hover-background",
            DefaultValue = "Per Variant, a step deeper while selected",
            Description = "Background of a hovered clickable content or dismiss button.",
        },
        new()
        {
            Name = "--bit-Tag-active-background",
            DefaultValue = "Per Variant, a step deeper while selected",
            Description = "Background of a pressed clickable content or dismiss button.",
        },
        new()
        {
            Name = "--bit-Tag-selected-color",
            DefaultValue = "--bit-Tag-color",
            Description = "Text and glyphs of a selected tag.",
        },
        new()
        {
            Name = "--bit-Tag-selected-background",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Background of a selected tag.",
        },
        new()
        {
            Name = "--bit-Tag-selected-border-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Rule of a selected tag.",
        },
        new()
        {
            Name = "--bit-Tag-disabled-color",
            DefaultValue = "The Color role's disabled text color",
            Description = "Text and glyphs of a disabled tag.",
        },
        new()
        {
            Name = "--bit-Tag-disabled-background",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Background of a disabled tag.",
        },
        new()
        {
            Name = "--bit-Tag-disabled-border-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Rule of a disabled tag.",
        },
        new()
        {
            Name = "--bit-Tag-focus-color",
            DefaultValue = "The text color",
            Description = "Color of the inset focus outline.",
        },
        new()
        {
            Name = "--bit-Tag-radius",
            DefaultValue = "--bit-shp-radius-chip",
            Description = "Corner of a Rounded (default Shape) tag; Circular and Square keep their own.",
        },
        new()
        {
            Name = "--bit-Tag-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Width of the rule.",
        },
        new()
        {
            Name = "--bit-Tag-shadow",
            DefaultValue = "none",
            Description = "Elevation of the tag, e.g. var(--bit-shd-card) for an elevated chip.",
        },
        new()
        {
            Name = "--bit-Tag-min-height",
            DefaultValue = "--bit-siz-chip-{sm,md,lg}",
            Description = "Smallest height inside the rule; the tag still grows with wrapped text.",
        },
        new()
        {
            Name = "--bit-Tag-max-width",
            DefaultValue = "100%",
            Description = "Widest the tag gets before it wraps or, with NoWrap, ellipsizes.",
        },
        new()
        {
            Name = "--bit-Tag-padding-x",
            DefaultValue = "Per Size (half of --bit-siz-ctrl-pad-x-*)",
            Description = "Inline inset of the content and the outer inset of the dismiss button.",
        },
        new()
        {
            Name = "--bit-Tag-padding-y",
            DefaultValue = "Per Size",
            Description = "Block inset of the content.",
        },
        new()
        {
            Name = "--bit-Tag-gap",
            DefaultValue = "Per Size",
            Description = "Room between the icon, the label and the other parts of the content.",
        },
        new()
        {
            Name = "--bit-Tag-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size.",
        },
        new()
        {
            Name = "--bit-Tag-font-weight",
            DefaultValue = "--bit-tpg-font-weight",
            Description = "Text weight (a Text with a SecondaryText under it is semibold).",
        },
        new()
        {
            Name = "--bit-Tag-secondary-font-size",
            DefaultValue = "Per Size, one step under the text",
            Description = "Size of the SecondaryText line.",
        },
        new()
        {
            Name = "--bit-Tag-icon-size",
            DefaultValue = "1em",
            Description = "Size of every glyph, the dismiss glyph included.",
        },
        new()
        {
            Name = "--bit-Tag-image-size",
            DefaultValue = "Per Size",
            Description = "Size of the IconUrl picture.",
        },
    ];



    private int clickCount;
    private int dismissCount;
    private int cardClickCount;
    private bool isPinned;
    private bool isStarred = true;
    private bool isOnlyMine;
    private bool allowSelectionChange;
    private bool isStyledSelected = true;
    private BitTag? plainFocusTag;

    private readonly string[] filters = ["Open", "In progress", "Done"];
    private readonly List<string> selectedFilters = ["In progress"];

    private List<string> dismissibleTags = ["Design", "Research", "Docs"];
    private readonly Dictionary<string, BitTag> dismissibleTagRefs = [];

    private readonly BitTagParams[] tagParams =
    [
        new()
        {
            Color = BitColor.Info,
            Variant = BitVariant.Outline,
            Shape = BitTagShape.Circular,
            IconName = BitIconName.Filter,
            DismissLabelFormat = "Remove the {0} filter",
        }
    ];

    private async Task DismissTag(string tag)
    {
        var index = dismissibleTags.IndexOf(tag);

        dismissibleTags.Remove(tag);
        dismissibleTagRefs.Remove(tag);

        if (dismissibleTags.Count == 0) return;

        // the tag that took its place, or the last one when the end of the list was removed
        var next = dismissibleTags[Math.Min(index, dismissibleTags.Count - 1)];

        if (dismissibleTagRefs.TryGetValue(next, out var nextRef))
        {
            await nextRef.FocusAsync();
        }
    }

    private void ResetDismissibleTags()
    {
        dismissibleTags = ["Design", "Research", "Docs"];
    }

    private void ToggleFilter(string filter, bool selected)
    {
        if (selected)
        {
            if (selectedFilters.Contains(filter) is false)
            {
                selectedFilters.Add(filter);
            }
        }
        else
        {
            selectedFilters.Remove(filter);
        }
    }
}
