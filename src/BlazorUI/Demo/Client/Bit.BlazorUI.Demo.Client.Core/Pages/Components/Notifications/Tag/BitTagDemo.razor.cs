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
            Description = "The general color of the tag. An explicit value wins over the --bit-Tag-* color variables for every color its role paints; left unset, the tag is primary unless they say otherwise.",
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
            Name = "NoNewTabHint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops a Target=\"_blank\" link from announcing that it opens a new tab - only for where a visible label or heading already says so."
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
            Description = "The rel of the link. A _blank link gets noopener added to it, unless it already says NoOpener, NoReferrer or Opener.",
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
            Type = "BitShape?",
            DefaultValue = "null",
            Description = "The corner shape of the tag. A tag takes its box from its own content, so Circle has no proportions to impose and rounds the ends fully, the same as Pill. An explicit value wins over --bit-Tag-radius; left unset, the tag takes the chip corner unless it says otherwise.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the tag. An explicit value wins over the --bit-Tag-* size variables; left unset, the tag is medium unless they say otherwise.",
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
        DemoSharedEnums.BitNavAriaCurrent(),
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitShape(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitVariant(),
        DemoSharedEnums.BitLinkRels(),
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Tag-color",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Text and glyphs at rest. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-background",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Background at rest. The Color parameter wins over it in Fill; the transparent background of Outline and Text is its alone.",
        },
        new()
        {
            Name = "--bit-Tag-border-color",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Rule at rest. The Color parameter wins over it, except over the transparent rule of Text.",
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
            Description = "Background of a hovered clickable content or dismiss button. The Color parameter wins over it in Outline and Text; the overlay a Fill tag hovers with is its alone.",
        },
        new()
        {
            Name = "--bit-Tag-active-background",
            DefaultValue = "Per Variant, a step deeper while selected",
            Description = "Background of a pressed clickable content or dismiss button. The Color parameter wins over it in Outline and Text; the overlay a Fill tag is pressed with is its alone.",
        },
        new()
        {
            Name = "--bit-Tag-selected-color",
            DefaultValue = "--bit-Tag-color",
            Description = "Text and glyphs of a selected tag. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-selected-background",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Background of a selected tag. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-selected-border-color",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Rule of a selected tag. The Color parameter wins over it, except over the transparent rule of Text.",
        },
        new()
        {
            Name = "--bit-Tag-disabled-color",
            DefaultValue = "--bit-clr-pri-dis-text",
            Description = "Text and glyphs of a disabled tag. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-disabled-background",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Background of a disabled tag. The Color parameter wins over it in Fill; the transparent background of Outline and Text is its alone.",
        },
        new()
        {
            Name = "--bit-Tag-disabled-border-color",
            DefaultValue = "Per Variant, from the primary role",
            Description = "Rule of a disabled tag. The Color parameter wins over it, except over the transparent rule of Text.",
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
            Description = "Corner of the tag. The Shape parameter wins over it.",
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
            DefaultValue = "--bit-siz-chip-md",
            Description = "Smallest height inside the rule; the tag still grows with wrapped text. The Size parameter wins over it.",
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
            DefaultValue = "Half of --bit-siz-ctrl-pad-x-md",
            Description = "Inline inset of the content and the outer inset of the dismiss button. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-padding-y",
            DefaultValue = "spacing(0.375)",
            Description = "Block inset of the content. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the icon, the label and the other parts of the content. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tag-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size. The Size parameter wins over it.",
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
            DefaultValue = "--bit-tpg-fs-xs",
            Description = "Size of the SecondaryText line. The Size parameter wins over it.",
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
            DefaultValue = "spacing(2)",
            Description = "Size of the IconUrl picture. The Size parameter wins over it.",
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
            Shape = BitShape.Pill,
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
