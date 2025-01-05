namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ErrorBoundary;

public partial class BitErrorBoundaryDemo
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


    private void ThrowException()
    {
        throw new Exception("This is an exception!");
    }



    private readonly string example1RazorCode = @"
<BitPdfReader Config=""basicConfig"" />";
    private readonly string example1CsharpCode = @"
private readonly BitPdfReaderConfig basicConfig = new() { Url = ""url-to-the-pdf-file.pdf"" };";
}
