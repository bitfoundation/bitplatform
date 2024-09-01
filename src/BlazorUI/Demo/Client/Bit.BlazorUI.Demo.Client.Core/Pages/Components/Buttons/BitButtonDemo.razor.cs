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
            Description = "If true, shows the loading state while the OnClick event is in progress.",
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
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "The callback for the click event of the button.",
        },
        new()
        {
            Name = "PrimaryTemplate",
            Type = "RenderFragment?",
            DefaultValue="",
            Description = "The content of the primary section of the button (alias of the ChildContent).",
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
        }
    ];

    private readonly List<SideRailItem> sideRailItems =
    [
        new() { Id="example1", Title="Basic" },
        new() { Id="example2", Title="Primary & Secondary" },
        new() { Id="example3", Title="Variant" },
        new() { Id="example4", Title="Icon" },
        new() { Id="example5", Title="Loading" },
        new() { Id="example6", Title="Loading Label" },
        new() { Id="example7", Title="Href" },
        new() { Id="example8", Title="Button Type" },
        new() { Id="example9", Title="Templates" },
        new() { Id="example10", Title="Events" },
        new() { Id="example11", Title="Size" },
        new() { Id="example12", Title="Color" },
        new() { Id="example13", Title="Style & Class" },
        new() { Id="example14", Title="RTL" },
    ];

    private bool fillIsLoading;
    private bool outlineIsLoading;
    private bool textIsLoading;

    private bool stylesIsLoading;
    private bool classesIsLoading;

    private bool templateIsLoading;

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

    private async Task AutoLoadingClick()
    {
        await Task.Delay(3000);
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
}
