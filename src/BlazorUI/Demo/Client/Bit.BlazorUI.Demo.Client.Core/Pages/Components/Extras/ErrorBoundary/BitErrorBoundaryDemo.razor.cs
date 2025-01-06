namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ErrorBoundary;

public partial class BitErrorBoundaryDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer content of the boundary.",
         },
         new()
         {
            Name = "OnError",
            Type = "EventCallback<Exception>",
            DefaultValue = "",
            Description = "The callback for when an error get caught by the boundary.",
         },
         new()
         {
            Name = "ShowException",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the actual exception information should be shown or not.",
         },
         new()
         {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The header title of the boundary.",
         },
    ];


    private void ThrowException()
    {
        throw new Exception("This is an exception!");
    }



    private readonly string example1RazorCode = @"
<BitErrorBoundary>
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example1CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";
}
