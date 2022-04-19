using Microsoft.AspNetCore.Components.Rendering;

namespace TodoTemplate.App.Shared;

public class TodoTemplateErrorBoundary : ErrorBoundaryBase
{
    [Inject] private IExceptionHandler ExceptionHandler { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        ExceptionHandler.Handle(exception);

        return Task.CompletedTask;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}
