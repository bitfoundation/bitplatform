using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Preview;

/// <summary>
/// An error boundary that also tells its owner when it caught an exception, so the owner can react before the error shows.
/// </summary>
public class PreviewErrorBoundary : ErrorBoundary
{
    [Parameter] public Func<Task>? OnError { get; set; }

    protected override async Task OnErrorAsync(Exception exception)
    {
        await base.OnErrorAsync(exception);

        if (OnError is not null)
        {
            await OnError();
        }
    }
}
