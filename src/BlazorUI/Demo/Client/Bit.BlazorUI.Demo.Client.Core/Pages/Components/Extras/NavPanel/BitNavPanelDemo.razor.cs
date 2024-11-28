
namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
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
            ]
        }
    ];


    
    private readonly string example1RazorCode = @"
";
    private readonly string example1CsharpCode = @"
";
}
