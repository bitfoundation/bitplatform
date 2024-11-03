
namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfReader;

public partial class BitPdfReaderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "RenderAllPages",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether render all pages at start.",
         },
         new()
         {
            Name = "ScrollElement",
            Type = "string",
            DefaultValue = "body",
            Description = "The CSS selector of the scroll element that is the parent of the pdf reader.",
         },
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
            ]
        }
    ];


    private BitPdfReader pdfReaderRef = default!;

    private BitPdfReaderConfig publicApiConfig = new () { Url="/_content/Bit.BlazorUI.Demo.Client.Core/samples/1.pdf" };



    private readonly string example1RazorCode = @"
";
    private readonly string example1CsharpCode = @"
";
}
