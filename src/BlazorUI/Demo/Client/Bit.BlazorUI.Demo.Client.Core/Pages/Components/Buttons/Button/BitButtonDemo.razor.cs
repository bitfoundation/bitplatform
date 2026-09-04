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
            Description = "If true, the button automatically receives focus when the page renders (rendered as the autofocus attribute).",
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
            Description = "Makes the Float/FloatAbsolute button draggable on the page.",
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
            Description = "Determines that only the icon should be rendered."
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
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The loading label text to show next to the spinner icon."
        },
        new()
        {
            Name = "LoadingLabelPosition",
            Type = "BitSide",
            DefaultValue = "BitSide.End",
            Description = "The position of the loading Label in regards to the spinner icon.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum"
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template used to replace the default loading text inside the button in the loading state.",
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
                   Description = "Custom class or style applied to the icon element."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the internal container."
               },
               new()
               {
                   Name = "Primary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the primary section."
               },
               new()
               {
                   Name = "Secondary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the secondary section."
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
                   Description = "Custom class or style applied to the loading container."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the spinner element."
               },
               new()
               {
                   Name = "LoadingLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom class or style applied to the loading label."
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
            Id = "label-position-enum",
            Name = "BitSide",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Top",
                    Value = "0",
                    Description = "The top edge."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "1",
                    Description = "The bottom edge."
                },
                new()
                {
                    Name = "Start",
                    Value = "2",
                    Description = "The edge the reading direction starts from - the left in LTR, the right in RTL."
                },
                new()
                {
                    Name = "End",
                    Value = "3",
                    Description = "The edge the reading direction ends at - the right in LTR, the left in RTL."
                },
                new()
                {
                    Name = "Left",
                    Value = "4",
                    Description = "The left edge, in both reading directions."
                },
                new()
                {
                    Name = "Right",
                    Value = "5",
                    Description = "The right edge, in both reading directions."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "6",
                    Description = "Both edges of the block axis at once."
                },
                new()
                {
                    Name = "StartAndEnd",
                    Value = "7",
                    Description = "Both edges of the inline axis at once, following the reading direction the way Start and End do."
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
                    Value = "0",
                    Description = "The top left corner, in both reading directions."
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1",
                    Description = "The top edge, centered horizontally."
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2",
                    Description = "The top right corner, in both reading directions."
                },
                new()
                {
                    Name = "TopStart",
                    Value = "3",
                    Description = "The top edge, on the side the reading direction starts from."
                },
                new()
                {
                    Name = "TopEnd",
                    Value = "4",
                    Description = "The top edge, on the side the reading direction ends at."
                },
                new()
                {
                    Name = "CenterLeft",
                    Value = "5",
                    Description = "The left edge, centered vertically, in both reading directions."
                },
                new()
                {
                    Name = "Center",
                    Value = "6",
                    Description = "Centered both ways."
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "7",
                    Description = "The right edge, centered vertically, in both reading directions."
                },
                new()
                {
                    Name = "CenterStart",
                    Value = "8",
                    Description = "Centered vertically, on the side the reading direction starts from."
                },
                new()
                {
                    Name = "CenterEnd",
                    Value = "9",
                    Description = "Centered vertically, on the side the reading direction ends at."
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "10",
                    Description = "The bottom left corner, in both reading directions."
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "11",
                    Description = "The bottom edge, centered horizontally."
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "12",
                    Description = "The bottom right corner, in both reading directions."
                },
                new()
                {
                    Name = "BottomStart",
                    Value = "13",
                    Description = "The bottom edge, on the side the reading direction starts from."
                },
                new()
                {
                    Name = "BottomEnd",
                    Value = "14",
                    Description = "The bottom edge, on the side the reading direction ends at."
                }
            ]
        },
    ];

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
    private async Task ScrollToFloat() => await _js.ScrollToElement("example12");
}
