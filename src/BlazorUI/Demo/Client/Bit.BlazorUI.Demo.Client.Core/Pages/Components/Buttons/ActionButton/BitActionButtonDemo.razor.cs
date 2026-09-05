namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ActionButton;

public partial class BitActionButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the disabled action button focusable and discoverable by assistive technologies, conveying the disabled state using aria-disabled instead of the native disabled attribute.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the button for the benefit of screen readers (rendered into aria-describedby).",
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
            Description = "If true, the action button automatically receives focus when the page renders (rendered as the autofocus attribute).",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, enters the loading state automatically while awaiting the OnClick event and prevents subsequent clicks by default.",
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            DefaultValue = "null",
            Description = "The type of the button element; defaults to submit inside an EditForm otherwise button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for ChildContent, the custom body of the action button (text and/or any render fragment).",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom body of the action button (text and/or any render fragment).",
        },
        new()
        {
            Name = "Classes",
            Type = "BitActionButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the root, icon, content, loading label, and spinner of the action button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color role of the action button. At rest it paints the icon and the spinner while the text keeps the neutral foreground; on hover and press it takes over the text as well, and it also picks the focus ring color.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the download attribute of the link rendered by the button when the Href parameter is provided. Instructs the browser to download the linked resource instead of navigating to it, using the provided value (if any) as the suggested file name.",
        },
        new()
        {
            Name = "EditContext",
            Type = "EditContext?",
            DefaultValue = "null",
            Description = "The EditContext, which is set if the button is inside an EditForm. The value is coming from the cascading value provided by the EditForm.",
        },
        new()
        {
            Name = "FormId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the form element that the action button is associated with (rendered as the form attribute). Allows a submit/reset button to be placed outside of its form element.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the action button across the full available width and spreads its icon and content to the two ends.",
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
            Description = "Gets or sets a value indicating whether only the icon is displayed, without accompanying text.",
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
            Description = "The url of a custom image to render as the icon of the action button, used when neither Icon nor IconName is set.",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the action button is in loading mode or not (two-way bindable).",
        },
        new()
        {
            Name = "LoadingDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the loading indicator appears after entering the loading state, useful to avoid a spinner flash for fast operations. The click-guard of the loading state applies immediately regardless of this delay.",
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show next to the spinner while the action button is in the loading state, replacing the button body. It is also announced by screen readers through a status live region when the loading state starts.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template used to replace the default loading indicator inside the action button in the loading state.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Gets or sets the callback that is invoked when the component is clicked.",
        },
        new()
        {
            Name = "Reclickable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables re-clicking the action button while it is in the loading state. By default, clicks are ignored while the button is loading to protect against double submissions.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "Gets or sets the relationship type between the current element and the linked resource, as defined by the link's rel attribute.",
            LinkType = LinkType.Link,
            Href = "#link-rels",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Sets the preset size (Small, Medium, Large) for typography and padding of the action button.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, stops the propagation of the click event to the parent elements. Useful when the action button is placed inside clickable containers like rows or cards.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitActionButtonClassStyles?",
            DefaultValue = "null",
            Description = "Gets or sets the custom CSS inline styles to apply to the action button component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the target frame or window for the navigation action when the action button renders as an anchor (by providing the Href parameter). When set to _blank and no opener-related Rel is provided, noopener is added to the rel attribute automatically.",
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
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Underlines the text of the action button, which thickens on hover, for the link-style use inside running text.",
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-ActionButton-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color at rest. The role color only reaches the text on hover and press, so this is what colorizes the label permanently.",
        },
        new()
        {
            Name = "--bit-ActionButton-icon-color",
            DefaultValue = "The Color role's main color",
            Description = "Icon and spinner color at rest.",
        },
        new()
        {
            Name = "--bit-ActionButton-hover-color",
            DefaultValue = "The Color role's hover color",
            Description = "Text and icon color while hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-ActionButton-active-color",
            DefaultValue = "The Color role's active color",
            Description = "Text and icon color while pressed.",
        },
        new()
        {
            Name = "--bit-ActionButton-disabled-color",
            DefaultValue = "--bit-clr-fg-dis (text), the Color role's disabled text color (icon)",
            Description = "Text and icon color when IsEnabled is false; also the focus ring color of a disabled button kept focusable with AllowDisabledFocus.",
        },
        new()
        {
            Name = "--bit-ActionButton-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the keyboard focus ring.",
        },
        new()
        {
            Name = "--bit-ActionButton-background",
            DefaultValue = "transparent",
            Description = "Background at rest, and the fallback of the two state backgrounds below.",
        },
        new()
        {
            Name = "--bit-ActionButton-hover-background",
            DefaultValue = "--bit-ActionButton-background",
            Description = "Background while hovered. A translucent tint of the role color, such as color-mix(in srgb, var(--bit-clr-pri) 12%, transparent), gives the Material-style state layer.",
        },
        new()
        {
            Name = "--bit-ActionButton-active-background",
            DefaultValue = "--bit-ActionButton-hover-background",
            Description = "Background while pressed.",
        },
        new()
        {
            Name = "--bit-ActionButton-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of the box, which the backgrounds and the focus ring follow.",
        },
        new()
        {
            Name = "--bit-ActionButton-padding",
            DefaultValue = "Per Size: the control's y padding and one step below the standalone button's x padding",
            Description = "Padding of the box. Set it to 0 for a button that sits flush inside running text or a table cell.",
        },
        new()
        {
            Name = "--bit-ActionButton-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the icon (or spinner) and the content.",
        },
        new()
        {
            Name = "--bit-ActionButton-font-size",
            DefaultValue = "Per Size: --bit-tpg-fs-xs / -sm / -md",
            Description = "Font size of the text and the loading label.",
        },
        new()
        {
            Name = "--bit-ActionButton-icon-size",
            DefaultValue = "Per Size: --bit-siz-icon-sm / -md / -lg",
            Description = "Size of the icon, the IconUrl image and the spinner, which share one slot so entering the loading state moves nothing.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitActionButtonClassStyles",
            Description = "Defines per-part CSS class/style values for BitActionButton.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom class or style applied to the root element of the BitActionButton."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom class or style applied to the icon element of the BitActionButton (the glyph, or the image rendered for IconUrl)."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom class or style applied to the content container of the BitActionButton."
                },
                new()
                {
                    Name = "LoadingLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom class or style applied to the loading label element of the BitActionButton."
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom class or style applied to the loading spinner element of the BitActionButton."
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
    ];



    private bool isLoading;
    private bool templateIsLoading;

    private int clickCounter;
    private int guardedClickCount;
    private int reclickableClickCount;
    private int rowClickCount;
    private int innerClickCount;

    private async Task HandleAutoLoadingClick()
    {
        await Task.Delay(2000);
    }

    private async Task HandleGuardedClick()
    {
        guardedClickCount++;

        await Task.Delay(2000);
    }

    private async Task HandleReclickableClick()
    {
        reclickableClickCount++;

        await Task.Delay(2000);
    }
    private bool formIsValidSubmit;
    private ButtonValidationModel buttonValidationModel = new();

    private async Task HandleValidSubmit()
    {
        formIsValidSubmit = true;

        await Task.Delay(2000);

        buttonValidationModel = new();

        formIsValidSubmit = false;
    }

    private void HandleInvalidSubmit()
    {
        formIsValidSubmit = false;
    }
}
