namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfReader;

public partial class BitPdfReaderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "CanvasClass",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS class of the canvas element(s).",
         },
         new()
         {
            Name = "CanvasStyle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS style of the canvas element(s).",
         },
         new()
         {
            Name = "Config",
            Type = "BitPdfReaderConfig",
            DefaultValue = "",
            Description = "The configuration of the pdf reader.",
            LinkType = LinkType.Link,
            Href = "#pdf-reader-config"
         },
         new()
         {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the pages horizontally.",
         },
         new()
         {
            Name = "InitialPageNumber",
            Type = "int",
            DefaultValue = "1",
            Description = "The page number to render initially.",
         },
         new()
         {
            Name = "OnPdfPageRendered",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the pdf page is done rendering.",
         },
         new()
         {
            Name = "OnPdfLoaded",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the pdf document is done loading and processing.",
         },
         new()
         {
            Name = "RenderAllPages",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether render all pages at start.",
         }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "pdf-reader-config",
            Title = "BitPdfReaderConfig",
            Parameters=
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "Guid.NewGuid().ToString()",
                    Description = "The id of the pdf reader instance and its canvas element(s).",
                },
                new()
                {
                    Name = "Url",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The URL of the pdf file.",
                },
                new()
                {
                    Name = "Scale",
                    Type = "decimal",
                    DefaultValue = "1",
                    Description = "The scale in which the pdf document gets rendered on the page.",
                },
                new()
                {
                    Name = "AutoScale",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Automatically scales the pdf based on the device pixel-ratio.",
                },
            ]
        }
    ];

    private readonly BitPdfReaderConfig basicConfig = new() { Url = "/_content/Bit.BlazorUI.Demo.Client.Core/samples/hello-world.pdf" };

    private readonly BitPdfReaderConfig renderAllConfig = new() { Url = "/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf" };


    private double scale = 1;
    private BitPdfReader publicApiPdfReaderRef = default!;
    private BitPdfReaderConfig publicApiConfig = new() { Url = "/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf" };

    private async Task ZoomIn()
    {
        scale += 0.25;
        publicApiConfig.Scale = scale;
        await publicApiPdfReaderRef.Refresh();
    }

    private async Task ZoomOut()
    {
        if (scale > 0.25)
        {
            scale -= 0.25;
        }
        publicApiConfig.Scale = scale;
        await publicApiPdfReaderRef.Refresh();
    }



    private readonly string example1RazorCode = @"
<BitPdfReader Config=""basicConfig"" />";
    private readonly string example1CsharpCode = @"
private readonly BitPdfReaderConfig basicConfig = new() { Url = ""url-to-the-pdf-file.pdf"" };";

    private readonly string example2RazorCode = @"
<BitPdfReader RenderAllPages Horizontal Config=""renderAllConfig"" />";
    private readonly string example2CsharpCode = @"
private readonly BitPdfReaderConfig renderAllConfig = new() { Url = ""url-to-the-pdf-file.pdf"" };";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => publicApiPdfReaderRef!.First()"">First</BitButton>
<BitButton OnClick=""() => publicApiPdfReaderRef!.Prev()"">Prev</BitButton>
<BitTag Variant=""BitVariant.Outline"" Text=""@($""{publicApiPdfReaderRef?.CurrentPageNumber.ToString()}/{publicApiPdfReaderRef?.NumberOfPages.ToString()}"")"" Color=""BitColor.Info"" />
<BitButton OnClick=""() => publicApiPdfReaderRef!.Next()"">Next</BitButton>
<BitButton OnClick=""() => publicApiPdfReaderRef!.Last()"">Last</BitButton>
<BitButton OnClick=""ZoomOut"">Zoom -</BitButton>
<BitButton OnClick=""ZoomIn"">Zoom +</BitButton>

<BitPdfReader @ref=""publicApiPdfReaderRef"" Config=""publicApiConfig"" />";
    private readonly string example3CsharpCode = @"
private double scale = 1;
private BitPdfReader publicApiPdfReaderRef = default!;
private BitPdfReaderConfig publicApiConfig = new() { Url = ""url-to-the-pdf-file.pdf"" };

private async Task ZoomIn()
{
    scale += 0.25;
    publicApiConfig.Scale = scale;
    await publicApiPdfReaderRef.Refresh();
}

private async Task ZoomOut()
{
    if (scale > 0.25)
    {
        scale -= 0.25;
    }
    publicApiConfig.Scale = scale;
    await publicApiPdfReaderRef.Refresh();
}";
}
