namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Tag;

public partial class BitTagDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaCurrent",
            Type = "BitNavAriaCurrent",
            DefaultValue = "BitNavAriaCurrent.True",
            Description = "What a selected tag that is a link reports itself as through aria-current. It only ever reaches the anchor the tag becomes while Href is set - a tag that is a button reports its selection through aria-pressed instead.",
            LinkType = LinkType.Link,
            Href = "#nav-aria-current-enum"
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "The detailed description of the tag for the benefit of screen readers, rendered into a visually hidden element the tag points at with aria-describedby."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the tag will apply to. It replaces the Text and the SecondaryText only; an icon, an image, the checkmark of a selected tag and a trailing SecondaryIcon all keep rendering around it. Use the PrefixTemplate and the SuffixTemplate for markup that belongs beside the label rather than in place of it."
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
            Description = "The initial value of Selected for a tag that keeps its own selection: setting it is the whole of what an uncontrolled filter chip needs. A Selected that is set one way, without binding, is one the tag itself is not allowed to change."
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to use for the dismiss button using custom CSS classes for external icon libraries. Takes precedence over DismissIconName when both are set. Defaults to the built-in Cancel icon when neither is set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to use for the dismiss button from the built-in Fluent UI icons. Defaults to Cancel when not set. For external icon libraries, use DismissIcon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "DismissLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name and the tooltip of the dismiss button. With no value it is named after the Text of the tag through the DismissLabelFormat, and falls back to \"Dismiss\" on a tag that carries no text of its own."
        },
        new()
        {
            Name = "DismissLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format the dismiss button is named by while it has no DismissLabel of its own, where {0} is the Text of the tag. Defaults to \"Remove {0}\", which is also what translating that name goes through."
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "Prompts the browser to download the Href of the tag rather than to navigate to it, using the value as the suggested file name. An empty string keeps the name the server suggests."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the tag to fill the width of whatever holds it, instead of shrinking to its content."
        },
        new()
        {
            Name = "HideSelectedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the checkmark a selected tag shows in front of its content."
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL the tag navigates to, which also turns the tag into a link. A disabled tag drops the href and leaves the tab order."
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "IconAlt",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text alternative of the IconUrl picture, which is decorative and renders with an empty alt by default."
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons. For external icon libraries, use Icon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "IconUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL of a picture to show in place of the icon, cropped to a circle the height of the label. It is only rendered while neither Icon nor IconName is set."
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of the tag on a single line and ends it with an ellipsis where it does not fit."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback for when the Selected value of the tag has changed. Setting it - or binding Selected - is what turns the tag into a toggle."
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitTagChangeArgs>",
            DefaultValue = "",
            Description = "Callback invoked before the Selected value changes, letting the change be cancelled by setting Cancel on the args. Since it is awaited, it can also run asynchronous work first.",
            LinkType = LinkType.Link,
            Href = "#tag-change-args"
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Click event handler of the tag, which also turns the tag into a real button: focusable, activated with Enter and Space, and disabled along with the tag."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Dismiss button click event, if set the dismiss icon will show up. It can also be triggered with the Delete and the Backspace keys from any control the tag renders, and with no DismissLabel of its own the button takes the Text of the tag for its name."
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom markup rendered at the head of the tag, ahead of the icon or the picture. It is added rather than substituted, so an icon, an image and the checkmark of a selected tag all keep rendering alongside it."
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The relationship between the current document and the one the Href of the tag leads to. With no value of its own, a tag opening in a new browsing context gets rel=\"noopener\" automatically.",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum"
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the direction flow of the content of the tag: the icon, the label, the trailing SecondaryIcon, the two templates and the dismiss button all swap ends with it."
        },
        new()
        {
            Name = "SecondaryIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The trailing icon of the tag, rendered after the label and before the dismiss button, using custom CSS classes for external icon libraries. Takes precedence over SecondaryIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "SecondaryIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the trailing icon of the tag, from the built-in Fluent UI icons. It is rendered after the label and before the dismiss button, inside whatever the tag became, and it mirrors with the rest of the row when the tag is Reversed.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The secondary text of the tag, rendered under the Text in a quieter type."
        },
        new()
        {
            Name = "Selected",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the tag as selected, which paints it in its selected colors and shows a checkmark in front of its content. Binding it - or setting OnChange - turns the tag into a toggle button reporting aria-pressed; a tag that is a link reports aria-current instead."
        },
        new()
        {
            Name = "SelectedChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback for when the Selected value changes, which is what binding Selected assigns."
        },
        new()
        {
            Name = "SelectedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the checkmark a selected tag shows, using custom CSS classes for external icon libraries. Takes precedence over SelectedIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "SelectedIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the checkmark a selected tag shows, from the built-in Fluent UI icons. Defaults to Accept when not set.",
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
            Description = "Stops the click of the tag from bubbling any further up the DOM, for a tag sitting inside something else that reacts to a click."
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
            Description = "Custom markup rendered at the end of the tag, after the trailing SecondaryIcon and before the dismiss button. The mirror of the PrefixTemplate, and subject to the same rules."
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "The browsing context the Href of the tag is opened in, for example _blank."
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
            Description = "The tooltip to show when the mouse is placed on the tag, which is what spells out whatever a NoWrap ellipsis has cut off."
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
            Description = "Gives the keyboard focus to the tag: the anchor or the button it becomes while it is a control, the dismiss button of a tag that has only that, and the root element otherwise."
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
                   Description = "Custom CSS classes/styles for the content element of the BitTag, which is the anchor or the button the tag becomes while it is a link or a control, and a plain span otherwise."
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
                }
            ]
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

    private List<string> dismissibleTags = ["Design", "Research", "Docs"];

    private readonly string[] filters = ["Open", "In progress", "Done"];
    private readonly List<string> selectedFilters = ["In progress"];

    private void ResetDismissibleTags()
    {
        dismissibleTags = ["Design", "Research", "Docs"];
    }

    private List<string> focusTags = ["Design", "Research", "Docs"];
    private readonly Dictionary<string, BitTag> focusTagRefs = [];

    private async Task DismissFocusTag(string tag)
    {
        var index = focusTags.IndexOf(tag);

        focusTags.Remove(tag);
        focusTagRefs.Remove(tag);

        if (focusTags.Count == 0) return;

        // the tag that took its place, or the last one when the end of the list was removed
        var next = focusTags[Math.Min(index, focusTags.Count - 1)];

        if (focusTagRefs.TryGetValue(next, out var nextRef))
        {
            await nextRef.FocusAsync();
        }
    }

    private void ResetFocusTags()
    {
        focusTags = ["Design", "Research", "Docs"];
    }

    private BitTag? plainFocusTag;

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



    private readonly string example1RazorCode = @"
<BitTag Text=""Basic tag"" />
<BitTag Text=""Design"" Color=""BitColor.Info"" />
<BitTag Text=""Archived"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" />";

    private readonly string example2RazorCode = @"
<BitTag Text=""Fill"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Outline"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Text"" Variant=""BitVariant.Text"" />

<BitTag Text=""Fill"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitTag Text=""Outline"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />
<BitTag Text=""Text"" Variant=""BitVariant.Text"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitTag Text=""Rounded"" Shape=""BitTagShape.Rounded"" />
<BitTag Text=""Circular"" Shape=""BitTagShape.Circular"" />
<BitTag Text=""Square"" Shape=""BitTagShape.Square"" />";

    private readonly string example4RazorCode = @"
<BitTag Text=""Calendar"" IconName=""@BitIconName.Calendar"" />
<BitTag Text=""Reversed"" IconName=""@BitIconName.Calendar"" Reversed />
<BitTag IconName=""@BitIconName.Pinned"" AriaLabel=""Pinned"" />

<BitTag Text=""Status"" SecondaryIconName=""@BitIconName.ChevronDown"" Variant=""BitVariant.Outline"" OnClick=""() => { }"" />

<BitTag Text=""Read the docs"" IconName=""@BitIconName.Documentation"" SecondaryIconName=""@BitIconName.OpenInNewWindow""
        Color=""BitColor.Info"" Href=""https://blazorui.bitplatform.dev"" Target=""_blank"" />

<BitTag Text=""Both ends reversed"" IconName=""@BitIconName.Tag"" SecondaryIconName=""@BitIconName.ChevronDown""
        Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Reversed />";

    private readonly string example5RazorCode = @"
<BitTag Text=""Annie Lindqvist"" IconUrl=""/images/persona-female.png"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Annie Lindqvist"" SecondaryText=""Software engineer"" IconUrl=""/images/persona-female.png""
        Color=""BitColor.Tertiary"" Size=""BitSize.Large"" />

<BitTag Text=""Awaiting review"" Color=""BitColor.Warning""
        IconUrl=""/images/persona-female.png"" IconAlt=""Assigned to Annie Lindqvist"" />";

    private readonly string example6RazorCode = @"
<BitTag Text=""Alex Parker"" SecondaryText=""Product designer"" IconName=""@BitIconName.Contact"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Storage"" SecondaryText=""12.4 GB used"" Color=""BitColor.Info"" Size=""BitSize.Large"" />";

    private readonly string example7RazorCode = @"
@foreach (var tag in dismissibleTags)
{
    <BitTag Text=""@tag""
            IconName=""@BitIconName.Tag""
            Variant=""BitVariant.Outline""
            OnDismiss=""() => dismissibleTags.Remove(tag)"" />
}

<BitTag Text=""Custom glyph"" Color=""BitColor.Error"" DismissIconName=""@BitIconName.ChromeClose""
        DismissLabel=""Clear the custom glyph tag"" OnDismiss=""() => { }"" />

<BitTag Text=""Formatted label"" Color=""BitColor.Info"" DismissLabelFormat=""Take the {0} tag off the list"" OnDismiss=""() => { }"" />

<BitTag Text=""Disabled"" IsEnabled=""false"" OnDismiss=""() => { }"" />

<BitButton Variant=""BitVariant.Outline"" IsEnabled=""@(dismissibleTags.Count < 3)"" OnClick=""ResetDismissibleTags"">Reset</BitButton>";
    private readonly string example7CsharpCode = @"
private List<string> dismissibleTags = [""Design"", ""Research"", ""Docs""];

private void ResetDismissibleTags()
{
    dismissibleTags = [""Design"", ""Research"", ""Docs""];
}";

    private readonly string example8RazorCode = @"
<BitTag Text=""Add to filters"" IconName=""@BitIconName.Add"" OnClick=""() => clickCount++"" />

<BitTag Text=""Outline"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" OnClick=""() => clickCount++"" />

<BitTag Text=""Click then dismiss"" Variant=""BitVariant.Text"" Color=""BitColor.Success""
        OnClick=""() => clickCount++"" OnDismiss=""() => dismissCount++"" />

<BitTag Text=""Disabled"" IsEnabled=""false"" OnClick=""() => clickCount++"" />

<div class=""example-card"" @onclick=""() => cardClickCount++"">
    The card counts every click that reaches it. The second tag stops its own.

    <BitTag Text=""Bubbles"" Variant=""BitVariant.Outline"" OnClick=""() => clickCount++"" />

    <BitTag Text=""Stops"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" StopPropagation OnClick=""() => clickCount++"" />
</div>

<div>Clicked <b>@clickCount</b> times, dismissed <b>@dismissCount</b> times, card clicked <b>@cardClickCount</b> times.</div>";
    private readonly string example8CsharpCode = @"
private int clickCount;
private int dismissCount;
private int cardClickCount;";

    private readonly string example9RazorCode = @"
<BitTag Text=""Iconography"" IconName=""@BitIconName.Ribbon"" Href=""/iconography"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Docs"" Color=""BitColor.Info"" Href=""https://blazorui.bitplatform.dev"" Target=""_blank"" />

<BitTag Text=""Source"" Color=""BitColor.Secondary"" IconName=""@BitIconName.OpenInNewWindow""
        Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank""
        Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" />

<BitTag Text=""Logo"" IconName=""@BitIconName.Download"" Color=""BitColor.Success"" Variant=""BitVariant.Outline""
        Href=""/images/bit-logo-blue.png"" Download=""bit-logo.png"" />

<BitTag Text=""Disabled"" Href=""https://bitplatform.dev"" IsEnabled=""false"" />

<BitTag Text=""This page"" Href=""#example9"" Selected AriaCurrent=""BitNavAriaCurrent.Page""
        Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />";

    private readonly string example10RazorCode = @"
@foreach (var filter in filters)
{
    <BitTag Text=""@filter""
            Variant=""BitVariant.Outline""
            Color=""BitColor.Info""
            Selected=""selectedFilters.Contains(filter)""
            SelectedChanged=""v => ToggleFilter(filter, v)"" />
}

<BitTag Text=""Checkmark hidden"" @bind-Selected=""isPinned"" HideSelectedIcon />

<BitTag Text=""Custom glyph"" IconName=""@BitIconName.FavoriteStar"" Color=""BitColor.Warning"" Variant=""BitVariant.Text""
        SelectedIconName=""@BitIconName.FavoriteStarFill"" @bind-Selected=""isStarred"" />

<BitTag Text=""Static selection"" Selected Color=""BitColor.Success"" />

<BitTag Text=""Current link"" Selected Href=""#example10"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Keeps its own state"" DefaultSelected=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" />

<BitTag Text=""Asks first"" DefaultSelected=""false"" Color=""BitColor.Warning""
        OnChanging=""args => args.Cancel = allowSelectionChange is false"" />

<BitToggle @bind-Value=""allowSelectionChange"" Label=""Allow the change"" Inline />

<div>Selected: <b>@(selectedFilters.Count == 0 ? ""none"" : string.Join("", "", selectedFilters))</b></div>";
    private readonly string example10CsharpCode = @"
private bool isPinned;
private bool isStarred = true;
private bool allowSelectionChange;

private readonly string[] filters = [""Open"", ""In progress"", ""Done""];
private readonly List<string> selectedFilters = [""In progress""];

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
}";

    private readonly string example11RazorCode = @"
<BitTag>
    <BitStack Horizontal Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
        <BitLabel>Custom content</BitLabel>
        <BitRollerLoading CustomSize=""24"" Color=""BitColor.Tertiary"" />
    </BitStack>
</BitTag>

<BitTag IconName=""@BitIconName.Contact"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">
    <b>Alex</b>&nbsp;<span style=""opacity:0.7"">(owner)</span>
</BitTag>

<BitTag IconName=""@BitIconName.Filter"" Color=""BitColor.Info"" OnClick=""() => { }"">
    Status<BitIcon IconName=""@BitIconName.ChevronDown"" />
</BitTag>

<BitTag Text=""Alex Parker"" SecondaryText=""Product designer"" Variant=""BitVariant.Outline"" Size=""BitSize.Large"">
    <PrefixTemplate>
        <span class=""example-initials"">AP</span>
    </PrefixTemplate>
</BitTag>

<BitTag Text=""Open issues"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">
    <SuffixTemplate>
        <span class=""example-count"">24</span>
    </SuffixTemplate>
</BitTag>

<BitTag Text=""Deploying"" Color=""BitColor.Success"" SecondaryIconName=""@BitIconName.ChevronRight"" OnClick=""() => { }"">
    <PrefixTemplate>
        <BitRollerLoading CustomSize=""16"" Color=""BitColor.TertiaryBackground"" />
    </PrefixTemplate>
</BitTag>";

    private readonly string example12RazorCode = @"
<BitTag Text=""A tag with a label long enough to wrap onto a second line"" Variant=""BitVariant.Outline"" />

<BitTag NoWrap
        IconName=""@BitIconName.Tag""
        Variant=""BitVariant.Outline""
        Text=""A tag with a label long enough to wrap onto a second line""
        Title=""A tag with a label long enough to wrap onto a second line"" />";

    private readonly string example13RazorCode = @"
<BitTag FullWidth Text=""Full width"" IconName=""@BitIconName.Tag"" Variant=""BitVariant.Outline"" />

<BitTag FullWidth Text=""Full width and dismissible"" IconName=""@BitIconName.Tag"" Color=""BitColor.Info"" OnDismiss=""() => { }"" />

<BitTag FullWidth Text=""Full width with a trailing glyph"" IconName=""@BitIconName.Tag"" Color=""BitColor.Success""
        Variant=""BitVariant.Outline"" SecondaryIconName=""@BitIconName.ChevronRight"" OnClick=""() => { }"" />";

    private readonly string example14RazorCode = @"
@foreach (var tag in focusTags)
{
    <BitTag @ref=""focusTagRefs[tag]""
            Text=""@tag""
            IconName=""@BitIconName.Tag""
            Variant=""BitVariant.Outline""
            OnDismiss=""() => DismissFocusTag(tag)"" />
}

<BitButton Variant=""BitVariant.Outline"" IsEnabled=""@(focusTags.Count < 3)"" OnClick=""ResetFocusTags"">Reset</BitButton>

<BitTag @ref=""plainFocusTag"" Text=""A plain tag with a TabIndex"" TabIndex=""0"" Variant=""BitVariant.Outline"" />
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => plainFocusTag?.FocusAsync()"">Focus it</BitButton>";
    private readonly string example14CsharpCode = @"
private List<string> focusTags = [""Design"", ""Research"", ""Docs""];
private readonly Dictionary<string, BitTag> focusTagRefs = [];

private async Task DismissFocusTag(string tag)
{
    var index = focusTags.IndexOf(tag);

    focusTags.Remove(tag);
    focusTagRefs.Remove(tag);

    if (focusTags.Count == 0) return;

    // the tag that took its place, or the last one when the end of the list was removed
    var next = focusTags[Math.Min(index, focusTags.Count - 1)];

    if (focusTagRefs.TryGetValue(next, out var nextRef))
    {
        await nextRef.FocusAsync();
    }
}

private void ResetFocusTags()
{
    focusTags = [""Design"", ""Research"", ""Docs""];
}

private BitTag? plainFocusTag;";

    private readonly string example15RazorCode = @"
<BitTag IconName=""@BitIconName.Filter"" AriaLabel=""Show the filters"" OnClick=""() => { }"" />

<BitTag IconName=""@BitIconName.Pinned"" AriaLabel=""Pinned to the top"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Design"" DismissLabel=""Remove the Design tag"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />

<BitTag Text=""Only mine"" @bind-Selected=""isOnlyMine"" Variant=""BitVariant.Outline""
        AriaDescription=""Shows only the items you own"" />";
    private readonly string example15CsharpCode = @"
private bool isOnlyMine;";

    private readonly string example16RazorCode = @"
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Text"" />

<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Text"" />

<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"" />

<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" />

<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" />

<BitTag Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" />

<BitTag Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" />

<BitTag Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" />

<BitTag Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" />

<BitTag Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" />

<BitTag Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" />

<BitTag Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" />
<BitTag Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" />
<BitTag Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" />

<BitTag Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" />

<BitTag Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" />
<BitTag Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" />
<BitTag Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" />


<div><b>Disabled</b>:</div>

<BitTag IsEnabled=""false"" Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" />
<BitTag IsEnabled=""false"" Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" />
<BitTag IsEnabled=""false"" Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" />
<BitTag IsEnabled=""false"" Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" />
<BitTag IsEnabled=""false"" Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" />
<BitTag IsEnabled=""false"" Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" />
<BitTag IsEnabled=""false"" Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" />
<BitTag IsEnabled=""false"" Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" />

<BitTag IsEnabled=""false"" Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" />
<BitTag IsEnabled=""false"" Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" />
<BitTag IsEnabled=""false"" Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" />

<BitTag IsEnabled=""false"" Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" />
<BitTag IsEnabled=""false"" Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" />
<BitTag IsEnabled=""false"" Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" />

<BitTag IsEnabled=""false"" Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" />
<BitTag IsEnabled=""false"" Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" />
<BitTag IsEnabled=""false"" Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" />";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTag Text=""House"" Icon=""@(""fa-solid fa-house"")"" />

<BitTag Text=""Heart"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" />

<BitTag Text=""GitHub"" Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" />

<BitTag Text=""Rocket"" Icon=""@BitIconInfo.Fa(""solid rocket"")"" />

<BitTag Text=""Dismiss"" Icon=""@BitIconInfo.Fa(""solid tag"")"" DismissIcon=""@BitIconInfo.Fa(""solid xmark"")"" OnDismiss=""() => { }"" />

<BitTag Text=""Selected"" Icon=""@BitIconInfo.Fa(""solid star"")"" SelectedIcon=""@BitIconInfo.Fa(""solid check"")"" Selected />
<BitTag Text=""Trailing"" Icon=""@BitIconInfo.Fa(""solid filter"")"" SecondaryIcon=""@BitIconInfo.Fa(""solid chevron-down"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTag Text=""House"" Icon=""@(""bi bi-house-fill"")"" />

<BitTag Text=""Heart"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" />

<BitTag Text=""GitHub"" Icon=""@BitIconInfo.Bi(""github"")"" />

<BitTag Text=""Gear"" Icon=""@BitIconInfo.Bi(""gear-fill"")"" />

<BitTag Text=""Dismiss"" Icon=""@BitIconInfo.Bi(""tag-fill"")"" DismissIcon=""@BitIconInfo.Bi(""x-lg"")"" OnDismiss=""() => { }"" />

<BitTag Text=""Selected"" Icon=""@BitIconInfo.Bi(""star-fill"")"" SelectedIcon=""@BitIconInfo.Bi(""check-lg"")"" Selected />
<BitTag Text=""Trailing"" Icon=""@BitIconInfo.Bi(""funnel-fill"")"" SecondaryIcon=""@BitIconInfo.Bi(""chevron-down"")"" />";

    private readonly string example18RazorCode = @"
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Text"" />
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />

<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Text"" />
<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />

<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Text"" />
<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />";

    private readonly string example19RazorCode = @"
<style>
    .custom-class {
        border-radius: 0.25rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root {
        color: mediumpurple;
        border-radius: 0.5rem;
        border-color: mediumpurple;
        background-color: transparent;
        box-shadow: mediumpurple 0 0 0.5rem;
    }

    .custom-icon {
        font-size: 1.25rem;
        font-weight: bolder;
    }

    .custom-selected {
        border-color: deeppink;
        background-color: deeppink;
    }
</style>


<BitTag Text=""Styled Tag""
        IconName=""@BitIconName.People""
        Style=""border-radius: 1rem; font-weight:bold"" />

<BitTag Text=""Classed Tag""
        IconName=""@BitIconName.People""
        Class=""custom-class"" Variant=""BitVariant.Outline"" />


<BitTag Text=""Styles""
        SecondaryText=""with a second line""
        IconName=""@BitIconName.People""
        Styles=""@(new() { Root = ""border-color: red; background-color: transparent;"",
                          Text = ""color: tomato; font-weight: bold;"",
                          SecondaryText = ""color: tomato;"",
                          Icon = ""color: tomato;"" })"" />

<BitTag Text=""Classes""
        IconName=""@BitIconName.People""
        Classes=""@(new() { Root = ""custom-root"",
                           Icon = ""custom-icon"" })"" />

<BitTag Text=""Selected""
        @bind-Selected=""isStyledSelected""
        Classes=""@(new() { Selected = ""custom-selected"" })"" />";
    private readonly string example19CsharpCode = @"
private bool isStyledSelected = true;";

    private readonly string example20RazorCode = @"
<div dir=""rtl"">
    <BitTag Dir=""BitDir.Rtl"" Text=""برچسب"" IconName=""@BitIconName.Calendar"" />

    <BitTag Dir=""BitDir.Rtl"" Text=""طراحی"" IconName=""@BitIconName.Tag"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />

    <BitTag Dir=""BitDir.Rtl"" Text=""حذف کنید"" IconName=""@BitIconName.Tag"" Color=""BitColor.Error""
            DismissLabelFormat=""حذف {0}"" OnDismiss=""() => { }"" />

    <BitTag Dir=""BitDir.Rtl"" Text=""معکوس"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success""
            Reversed OnDismiss=""() => { }"" />
</div>";
}
