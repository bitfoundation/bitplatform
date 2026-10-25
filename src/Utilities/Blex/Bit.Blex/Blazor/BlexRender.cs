namespace Bit.Blex.Blazor;

/// <summary>Shared plumbing for the components that re-render when a store notifies.</summary>
internal static class BlexRender
{
    /// <summary>
    /// Observes a render requested from a store notification (<c>InvokeAsync(StateHasChanged)</c>).
    /// </summary>
    /// <remarks>
    /// Discarding the task would turn a teardown race -- a store notifying just after the renderer
    /// went away -- into an unobserved task exception. The shutdown cases are ignored; anything else
    /// goes to the component's error boundary, the way Blazor expects.
    /// </remarks>
    public static void Observe(Task render, Func<Exception, Task> dispatchException)
    {
        if (render.IsCompletedSuccessfully)
            return;

        _ = ObserveAsync(render, dispatchException);

        static async Task ObserveAsync(Task pending, Func<Exception, Task> dispatchException)
        {
            try
            {
                await pending;
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                // Renderer or circuit gone; the render is moot.
            }
            catch (Exception ex)
            {
                await dispatchException(ex);
            }
        }
    }
}
