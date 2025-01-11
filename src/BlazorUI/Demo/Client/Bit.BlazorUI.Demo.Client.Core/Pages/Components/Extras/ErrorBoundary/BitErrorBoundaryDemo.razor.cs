namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ErrorBoundary;

public partial class BitErrorBoundaryDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "AdditionalButtons",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer content of the boundary.",
         },
         new()
         {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent.",
         },
         new()
         {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer content of the boundary.",
         },
         new()
         {
            Name = "HomeText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Home button.",
         },
         new()
         {
            Name = "HomeUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The url of the home page for the Home button.",
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
            Name = "RecoverText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Recover button.",
         },
         new()
         {
            Name = "RefreshText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Refresh button.",
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
