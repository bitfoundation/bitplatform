namespace Bit.BlazorUI;

internal static class DotNetObjectReferenceExtensions
{
    /// <summary>
    /// Runs the JS teardown of a component and only then disposes the reference its script calls back through.
    /// </summary>
    /// <remarks>
    /// Disposed first, the reference leaves the listeners that are still attached on the page holding a dangling one
    /// for as long as the teardown takes to reach them, and a pointer event, a scroll or a callout dismissed from
    /// outside in that window is refused with a "no tracked object" error. Kept alive, that call reaches the component
    /// while it is being disposed instead, so every [JSInvokable] of a component disposed through here returns early
    /// on IsDisposing (or on the disposal flag of its own when it is not a BitComponentBase) - a contract the
    /// BitJsInvokableDisposalContractTests hold each of them to.
    /// </remarks>
    internal static async ValueTask DisposeAfterAsync<T>(this DotNetObjectReference<T>? reference, Func<ValueTask> teardown) where T : class
    {
        try
        {
            await teardown();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the script along with it
        catch (OperationCanceledException) { } // the renderer is being torn down along with the component
        catch (ObjectDisposedException) { } // the JS runtime of a WebAssembly host may go first
        finally
        {
            reference?.Dispose();
        }
    }
}
