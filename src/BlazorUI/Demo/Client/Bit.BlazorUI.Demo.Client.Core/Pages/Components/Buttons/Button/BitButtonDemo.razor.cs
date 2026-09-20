namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.Button;

public partial class BitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Keeps the disabled button focusable and discoverable by screen readers, rendering aria-disabled instead of the native disabled attribute when IsEnabled is false, preserving a consistent tab order. Set it to false to render the native disabled attribute and remove the button from the tab order.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the button for the benefit of screen readers, rendered as visually hidden text beside the button and read after its name, not as part of it. An aria-describedby written on the component by hand is kept and this description is added to it, since the attribute is a list of ids.",
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, adds an aria-hidden attribute instructing screen readers to ignore the button.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the button automatically receives focus when the page renders (rendered as the autofocus attribute).",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, enters the loading state automatically while awaiting the OnClick event and prevents subsequent clicks by default. The state is left even when the handler throws.",
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType?",
            DefaultValue = "null",
            Description = "The type of the button element; defaults to submit inside an EditForm otherwise button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of primary section of the button.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the button.",
            LinkType = LinkType.Link,
            Href = "#button-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the button.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the download attribute of the link rendered by the button when Href is provided. Instructs the browser to download the linked resource instead of navigating to it, using the provided value as the file name.",
        },
        new()
        {
            Name = "Draggable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the Float/FloatAbsolute button draggable on the page, by pointer or with the arrow keys while it has the focus; ignored when neither is set.",
        },
        new()
        {
            Name = "FixedColor",
            Type = "bool",
            DefaultValue = "false",
            Description = "Preserves the foreground color of the button through hover and focus.",
        },
        new()
        {
            Name = "Float",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables floating behavior for the button, allowing it to be positioned relative to the viewport.",
        },
        new()
        {
            Name = "FloatAbsolute",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables floating behavior for the button, allowing it to be positioned relative to its container.",
        },
        new()
        {
            Name = "FloatOffset",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the offset of the floating button.",
        },
        new()
        {
            Name = "FloatPosition",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Specifies the position of the floating button.",
            LinkType = LinkType.Link,
            Href = "#button-position"
        },
        new()
        {
            Name = "FormId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the form element that the button is associated with (rendered as the form attribute). Allows a submit/reset button to be placed outside of its form element.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expand the button width to 100% of the available width.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the href attribute of the link rendered by the button. If provided, the component will be rendered as an anchor tag instead of button.",
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
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IconOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines that only the icon should be rendered; the text is kept as screen-reader-only content, so the button keeps its accessible name unless an AriaLabel replaces it."
        },
        new()
        {
            Name = "IconPosition",
            Type = "BitIconPosition?",
            DefaultValue = "null",
            Description = "Gets or sets the position of the icon relative to the component's content.",
            LinkType = LinkType.Link,
            Href = "#icon-position-enum",
        },
        new()
        {
            Name = "IconUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The url of the custom icon to render inside the button."
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the button is in loading mode or not."
        },
        new()
        {
            Name = "LoadingDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the spinner appears after the button enters the loading state, so an operation that finishes inside the delay never flashes one. Only the visuals wait: the click is blocked and aria-busy is rendered as soon as the loading starts.",
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The loading label text to show next to the spinner icon. It is also announced by assistive technologies from a live region beside the button, so that it is heard as a change of state instead of changing the name of the button itself."
        },
        new()
        {
            Name = "LoadingLabelPosition",
            Type = "BitLabelPosition",
            DefaultValue = "BitLabelPosition.End",
            Description = "The position of the loading Label in regards to the spinner icon.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum"
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template used to replace the default spinner and loading label inside the button in the loading state. Like the spinner it replaces, it is hidden from assistive technologies; use LoadingLabel for what should be announced.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps each line of the button's text on a single line and ends it with an ellipsis where it does not fit.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Raised when the button is clicked; receives a bool indicating the current loading state.",
        },
        new()
        {
            Name = "PrimaryTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "The content of the primary section of the button (alias of the ChildContent).",
        },
        new()
        {
            Name = "Reclickable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables re-clicking while the button is in the loading state.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "Sets the rel attribute for link-rendered buttons when Href is a non-anchor URL; ignored for empty or hash-only hrefs.",
            LinkType = LinkType.Link,
            Href = "#link-rels",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the button with fully rounded (pill shaped) corners, and an icon-only one as a circle. It sets what --bit-Button-radius falls back to, so a radius of your own still wins.",
        },
        new()
        {
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the secondary section of the button.",
        },
        new()
        {
            Name = "SecondaryTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "The custom template for the secondary section of the button.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Sets the preset size for typography and padding of the button.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, stops the click event from bubbling up to the parent elements.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom inline styles for different parts of the button.",
            LinkType = LinkType.Link,
            Href = "#button-class-styles",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies target attribute of the link when the button renders as an anchor (by providing the Href parameter). When set to _blank and no Rel is provided, rel=\"noopener\" gets added automatically for security.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the button.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the button.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "button-class-styles",
            Title = "BitButtonClassStyles",
            Description = "Defines per-part CSS class/style values for BitButton.",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the root element."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the icon element (the glyph, or the image rendered for IconUrl)."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the column that holds the primary and secondary lines of text."
               },
               new()
               {
                   Name = "Primary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the primary line of text."
               },
               new()
               {
                   Name = "Secondary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the secondary line of text."
               },
               new()
               {
                   Name = "HiddenContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the wrapper of the content that keeps the button size while it is hidden in the loading state."
               },
               new()
               {
                   Name = "LoadingContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the container of the spinner and its label in the loading state."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the loading spinner element."
               },
               new()
               {
                   Name = "LoadingLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the loading label element."
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
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
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
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size button.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size button.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size button.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
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
                    Description = "Icon renders before the content (default).",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Description = "Icon renders after the content.",
                    Value = "1",
                }
            ]
        },
        new()
        {
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the button.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the button.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the button.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows on the start of the button.",
                    Value="3",
                },
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
        },
        new()
        {
            Id = "button-position",
            Name = "BitPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "TopLeft",
                    Value = "0"
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2"
                },
                new()
                {
                    Name = "TopStart",
                    Value = "3"
                },
                new()
                {
                    Name = "TopEnd",
                    Value = "4"
                },
                new()
                {
                    Name = "CenterLeft",
                    Value = "5"
                },
                new()
                {
                    Name = "Center",
                    Value = "6"
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "7"
                },
                new()
                {
                    Name = "CenterStart",
                    Value = "8"
                },
                new()
                {
                    Name = "CenterEnd",
                    Value = "9"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "10"
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "11"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "12"
                },
                new()
                {
                    Name = "BottomStart",
                    Value = "13"
                },
                new()
                {
                    Name = "BottomEnd",
                    Value = "14"
                }
            ]
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the root element of the button.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Button-color",
            DefaultValue = "Per variant: the role's on-color when filled, its main color otherwise",
            Description = "Foreground (text and icon) in the rest state. FixedColor holds this color through hover and press as well.",
        },
        new()
        {
            Name = "--bit-Button-background",
            DefaultValue = "The role's main color when filled, transparent otherwise",
            Description = "Background in the rest state.",
        },
        new()
        {
            Name = "--bit-Button-border-color",
            DefaultValue = "The role's main color when filled or outlined, transparent otherwise",
            Description = "Border color in the rest state. The border is drawn on every variant, so a Text button can take one without changing its size.",
        },
        new()
        {
            Name = "--bit-Button-hover-color",
            DefaultValue = "The role's on-color, and the rest color for the Fill variant",
            Description = "Foreground while hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Button-hover-background",
            DefaultValue = "The role's hover color",
            Description = "Background while hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Button-hover-border-color",
            DefaultValue = "The rest border color, and the hover background for the Fill variant",
            Description = "Border color while hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-Button-active-color",
            DefaultValue = "As the hover foreground",
            Description = "Foreground while pressed.",
        },
        new()
        {
            Name = "--bit-Button-active-background",
            DefaultValue = "The role's active color",
            Description = "Background while pressed.",
        },
        new()
        {
            Name = "--bit-Button-active-border-color",
            DefaultValue = "As the hover border color",
            Description = "Border color while pressed.",
        },
        new()
        {
            Name = "--bit-Button-disabled-color",
            DefaultValue = "The role's disabled text color",
            Description = "Foreground when disabled. It also draws the focus ring of a disabled button that AllowDisabledFocus keeps in the tab order.",
        },
        new()
        {
            Name = "--bit-Button-disabled-background",
            DefaultValue = "The role's disabled color when filled, transparent otherwise",
            Description = "Background when disabled.",
        },
        new()
        {
            Name = "--bit-Button-disabled-border-color",
            DefaultValue = "As the disabled background",
            Description = "Border color when disabled.",
        },
        new()
        {
            Name = "--bit-Button-focus-color",
            DefaultValue = "The role's focus color",
            Description = "Color of the focus ring drawn around the button on keyboard focus.",
        },
        new()
        {
            Name = "--bit-Button-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of the box, which the focus ring follows. Rounded moves the default to --bit-shp-radius-full.",
        },
        new()
        {
            Name = "--bit-Button-border-width",
            DefaultValue = "--bit-shp-border-width",
            Description = "Thickness of the border on every variant.",
        },
        new()
        {
            Name = "--bit-Button-padding",
            DefaultValue = "Per size, from --bit-siz-ctrl-pad-*",
            Description = "Padding of the box. An icon-only button takes its vertical padding on all four sides instead.",
        },
        new()
        {
            Name = "--bit-Button-min-width",
            DefaultValue = "--bit-siz-ctrl-min-width",
            Description = "Smallest width of a labeled button, for lining up a row of buttons whose labels differ in length. FullWidth and NoWrap drop it to zero unless it is set, since both size the button by its container.",
        },
        new()
        {
            Name = "--bit-Button-min-height",
            DefaultValue = "Per size, from --bit-siz-ctrl-*",
            Description = "Smallest height of the box, and the smallest width of an icon-only one. It is a floor, not a height: the box still grows with a wrapped label or a secondary line.",
        },
        new()
        {
            Name = "--bit-Button-gap",
            DefaultValue = "Per size: 0.25rem for Small, 0.5rem otherwise",
            Description = "Room between the icon and the text, and between the loading spinner and its label.",
        },
        new()
        {
            Name = "--bit-Button-font-size",
            DefaultValue = "Per size, from the type ramp",
            Description = "Size of the primary text.",
        },
        new()
        {
            Name = "--bit-Button-secondary-font-size",
            DefaultValue = "One ramp step below the primary text",
            Description = "Size of the secondary text.",
        },
        new()
        {
            Name = "--bit-Button-secondary-color",
            DefaultValue = "The button's own foreground",
            Description = "Color of the secondary line of text. It is not muted by default, since on a filled button the contrast of the smaller of the two lines is what a muted color spends first.",
        },
        new()
        {
            Name = "--bit-Button-font-weight",
            DefaultValue = "--bit-tg-font-weight",
            Description = "Weight of both lines of text.",
        },
        new()
        {
            Name = "--bit-Button-icon-size",
            DefaultValue = "Per size, from --bit-siz-icon-*",
            Description = "Size of the icon, for a glyph and an IconUrl image alike.",
        },
        new()
        {
            Name = "--bit-Button-spinner-size",
            DefaultValue = "As the icon size",
            Description = "Diameter of the loading spinner.",
        },
        new()
        {
            Name = "--bit-Button-spinner-color",
            DefaultValue = "The button's own foreground",
            Description = "The spinner's moving arc. Reading the foreground by default is what keeps it legible over the filled background of one variant and the transparent one of the others.",
        },
        new()
        {
            Name = "--bit-Button-spinner-track-color",
            DefaultValue = "The button's own foreground at 25%",
            Description = "The ring the arc travels on.",
        },
        new()
        {
            Name = "--bit-Button-float-offset",
            DefaultValue = "1rem",
            Description = "Inset of a Float or FloatAbsolute button from the edge it is pinned to. The FloatOffset parameter writes this variable on the instance.",
        },
        new()
        {
            Name = "--bit-Button-float-shadow",
            DefaultValue = "--bit-shd-card",
            Description = "Elevation of a Float or FloatAbsolute button, which is what lifts it off the content it sits over. The focus ring is a shadow too and replaces it while the button is focused.",
        },
    ];

    private bool noWrap = true;

    private bool fillIsLoading;
    private bool outlineIsLoading;
    private bool textIsLoading;

    private bool stylesIsLoading;
    private bool classesIsLoading;

    private bool templateIsLoading;

    private string? floatOffset = "63px";
    private BitPosition floatPosition = BitPosition.BottomRight;
    private readonly List<BitDropdownItem<BitPosition>> floatPositionList = Enum.GetValues<BitPosition>()
                                                                                .Cast<BitPosition>()
                                                                                .Select(enumValue => new BitDropdownItem<BitPosition>
                                                                                {
                                                                                    Value = enumValue,
                                                                                    Text = enumValue.ToString()
                                                                                })
                                                                                .ToList();

    private async Task LoadingFillClick()
    {
        fillIsLoading = true;
        await Task.Delay(3000);
        fillIsLoading = false;
    }

    private async Task LoadingOutlineClick()
    {
        outlineIsLoading = true;
        await Task.Delay(3000);
        outlineIsLoading = false;
    }

    private async Task LoadingTextClick()
    {
        textIsLoading = true;
        await Task.Delay(3000);
        textIsLoading = false;
    }

    // Finishes inside the 500ms delay of the second button, so only the first one ever shows a spinner.
    private async Task FastOperation() => await Task.Delay(250);

    private int autoLoadCount;
    private async Task AutoLoadingClick()
    {
        autoLoadCount++;
        await Task.Delay(3000);
    }

    private int reclickableAutoLoadCount;
    private TaskCompletionSource clickTsc = new();
    private CancellationTokenSource delayCts = new();
    private Task AutoLoadingReclick(bool isLoading)
    {
        if (isLoading)
        {
            clickTsc.TrySetException(new TaskCanceledException());
            delayCts.Cancel();
        }

        delayCts = new();
        clickTsc = new();

        reclickableAutoLoadCount++;

        _ = Task.Delay(3000, delayCts.Token).ContinueWith(async delayTask =>
        {
            await delayTask;
            clickTsc.TrySetResult();
        });

        return clickTsc.Task;
    }


    private async Task LoadingStylesClick()
    {
        stylesIsLoading = true;
        await Task.Delay(3000);
        stylesIsLoading = false;
    }

    private async Task LoadingClassesClick()
    {
        classesIsLoading = true;
        await Task.Delay(3000);
        classesIsLoading = false;
    }

    private async Task LoadingTemplateClick()
    {
        templateIsLoading = true;
        await Task.Delay(3000);
        templateIsLoading = false;
    }

    private int clickCounter;

    private int parentClickCounter;
    private int buttonClickCounter;

    private BitButton focusButtonRef = default!;

    private bool formIsValidSubmit;
    private ButtonValidationModel buttonValidationModel = new();

    private async Task HandleValidSubmit()
    {
        formIsValidSubmit = true;

        await Task.Delay(2000);

        buttonValidationModel = new();

        formIsValidSubmit = false;

        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        formIsValidSubmit = false;
    }

    private bool externalFormSubmitted;
    private ButtonValidationModel externalFormModel = new();

    private async Task HandleExternalFormValidSubmit()
    {
        externalFormSubmitted = true;

        await Task.Delay(2000);

        externalFormModel = new();

        externalFormSubmitted = false;

        StateHasChanged();
    }

    [Inject] private IJSRuntime _js { get; set; } = default!;
    private async Task ScrollToFloat() => await _js.ScrollToElement("example11");
}
