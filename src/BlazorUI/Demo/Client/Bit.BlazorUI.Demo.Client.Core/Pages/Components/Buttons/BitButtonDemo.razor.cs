namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the button for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, adds an aria-hidden attribute instructing screen readers to ignore the element.",
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
            Description = "The value of the type attribute of the button.",
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
            Type = "bool",
            DefaultValue = "false",
            Description = "Specifies the position of the floating button.",
            LinkType = LinkType.Link,
            Href = "#button-position"
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
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to render inside the button."
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
            Description = "The custom template used to replace the default loading text inside the button in the loading state.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback for the click event of the button with a bool argument passing the current loading state.",
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
            Description = "Enables re-clicking in loading state when AutoLoading is enabled.",
        },
        new()
        {
            Name = "ReversedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the positions of the icon and the main content of the button.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRel?",
            DefaultValue = "null",
            Description = "If Href provided, specifies the relationship between the current document and the linked document.",
            LinkType = LinkType.Link,
            Href = "#button-rel",
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
            Description = "The size of the button.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the button.",
            LinkType = LinkType.Link,
            Href = "#button-class-styles",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies target attribute of the link when the button renders as an anchor (by providing the Href parameter).",
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
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitButton."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitButton."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the internal container of the BitButton."
               },
               new()
               {
                   Name = "Primary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the primary section of the BitButton."
               },
               new()
               {
                   Name = "Secondary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary section of the BitButton."
               },
               new()
               {
                   Name = "LoadingContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading container of the BitButton."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner section of the BitButton."
               },
               new()
               {
                   Name = "LoadingLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading label section of the BitButton."
               },
            ]
        }
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
            Id = "button-rel",
            Name = "BitLinkRel",
            Description = "",
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
                    Name = "CenterLeft",
                    Value = "3"
                },
                new()
                {
                    Name = "Center",
                    Value = "4"
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "5"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "6"
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "7"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "8"
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

    private string? floatOffset;
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

    [Inject] private IJSRuntime _js { get; set; } = default!;
    private async Task ScrollToFloat() => await _js.ScrollToElement("example9");
}
