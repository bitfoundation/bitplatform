namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ImagePartView;

public partial class BitAiImagePartViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiImagePartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the image part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiImagePart",
            DefaultValue = "",
            Description = "The image to render (required): its URL, its alternative text, its caption and its intrinsic size. The view reads it each time it renders rather than subscribing to it, so a change to a part already shown appears when the parent renders again. A source the user loaded under a policy that asks first stays loaded until the view is handed another one.",
            LinkType = LinkType.Link,
            Href = "#image-part",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiImagePartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the image part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiImagePartViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element (the figure) of the BitAiImagePartView.",
                },
                new()
                {
                    Name = "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image of the BitAiImagePartView.",
                },
                new()
                {
                    Name = "ImagePlaceholder",
                    Type = "BitActionButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button (a BitActionButton) the BitAiImagePartView shows in place of an image the image policy loads only on request.",
                    LinkType = LinkType.Link,
                    Href = "/components/actionbutton/#class-styles",
                },
                new()
                {
                    Name = "BlockedImage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the alternative text the BitAiImagePartView shows in place of an image the image policy blocks.",
                },
                new()
                {
                    Name = "Caption",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the caption of the BitAiImagePartView.",
                },
            ]
        },
        new()
        {
            Id = "image-part",
            Title = "BitAiImagePart",
            Description = "An image inside a message - one a model generated or a tool returned. Created with new BitAiImagePart(url, partId = null), where url is an absolute, relative or data URL (required) and partId the identifier a transport addresses the part by (one is generated when it is null). A transport sends one as a BitAiImageEvent. It is a BitAiMessagePart, so it also has the Message, Version and Changed of every part (see AiMessagePartView).",
            Parameters =
            [
                new()
                {
                    Name = "Url",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The source of the image: an absolute, relative or data URL, loaded as the image policy of the options decides. Set through the constructor.",
                },
                new()
                {
                    Name = "Alt",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The alternative text, which becomes the accessible name of the image. Without it the image is marked decorative (an empty alt). It is also what the part contributes to the plain text of its message ([image: alt]).",
                },
                new()
                {
                    Name = "Caption",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A caption rendered under the image.",
                },
                new()
                {
                    Name = "Width",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The intrinsic width of the image in pixels, which (with Height) reserves its room before it has loaded.",
                },
                new()
                {
                    Name = "Height",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The intrinsic height of the image in pixels.",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "\"image\"",
                    Description = "The part kind (BitAiMessagePartKinds.Image), which picks the renderer of the part in a message.",
                },
                new()
                {
                    Name = "PartId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the part, given to the constructor or generated.",
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiImagePartView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the image." },
        new() { Name = "--bit-AiImagePartView-caption-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Text color of the caption." },
        new() { Name = "--bit-AiImagePartView-caption-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Text size of the caption." },
        new() { Name = "--bit-AiImagePartView-muted-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the alternative text shown in place of an image the image policy blocks." },
    ];



    private readonly BitAiImagePart auroraImage = new("_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg")
    {
        Alt = "Green northern lights over a snowy ridge at night",
        Width = 1000,
        Height = 350,
    };

    private readonly BitAiImagePart mountainImage = new("_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg")
    {
        Alt = "A range of snow-covered peaks under a blue sky",
        Caption = "Generated for the prompt \"a winter landscape in the Alps\".",
        Width = 1000,
        Height = 350,
    };

    private readonly BitAiImagePart decorativeImage = new("_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg");

    private readonly BitAiImagePart chartImage = new("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='320' height='120' viewBox='0 0 320 120'%3E%3Crect width='320' height='120' rx='12' fill='%232563eb'/%3E%3Crect x='30' y='70' width='40' height='30' fill='%23bfdbfe'/%3E%3Crect x='90' y='45' width='40' height='55' fill='%23bfdbfe'/%3E%3Crect x='150' y='30' width='40' height='70' fill='%23bfdbfe'/%3E%3Crect x='210' y='55' width='40' height='45' fill='%23bfdbfe'/%3E%3Crect x='270' y='20' width='20' height='80' fill='%23ffffff'/%3E%3C/svg%3E")
    {
        Alt = "A bar chart of five values, the last one the highest",
        Caption = "Drawn by a tool and returned inline.",
        Width = 320,
        Height = 120,
    };

    private readonly BitAiImagePart policyImage = new("_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg")
    {
        Alt = "A range of snow-covered peaks under a blue sky",
        Caption = "Shown only as the image policy allows.",
        Width = 1000,
        Height = 350,
    };

    private readonly BitAiUiOptions clickToLoadOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
    private readonly BitAiUiOptions blockOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };

    private readonly BitAiImagePartViewParams[] imagePartViewParams =
    [
        new()
        {
            Style = "max-width: 24rem;",
            Styles = new() { Image = "border: 2px solid var(--bit-clr-pri);", Caption = "font-style: italic;" },
        },
    ];

    private readonly BitAiImagePart styledImage = new("_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg")
    {
        Alt = "A winding road above a lake between hills, snowy mountains behind",
        Caption = "A caption",
        Width = 1000,
        Height = 350,
    };

    private readonly BitAiImagePart rtlImage = new("_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg")
    {
        Alt = "شفق قطبی سبز بر فراز تپه‌ای برفی در شب",
        Caption = "تصویری که برای درخواست «شفق قطبی» ساخته شد.",
        Width = 1000,
        Height = 350,
    };
}
