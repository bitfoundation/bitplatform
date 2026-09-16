using System.Runtime.ExceptionServices;

namespace Bit.BlazorUI;

/// <summary>
/// The one child a <see cref="BitErrorBoundary"/> renders of its own: a component whose only job is to
/// throw the exception it was handed.
/// </summary>
/// <remarks>
/// A boundary cannot hand itself an exception. The state that says one was caught is private to
/// <see cref="ErrorBoundaryBase"/>, and the interface the renderer routes an exception through is
/// internal to the framework, so nothing a derived boundary can call puts it into the errored state -
/// which is what leaves the exceptions that never pass through the renderer at all (a fire-and-forget
/// task, a timer, a JS callback) with no way of reaching the boundary that should be showing them.
/// <br />
/// So <see cref="BitErrorBoundary.Capture(Exception)"/> renders this instead. The renderer routes an
/// exception thrown while a component renders to the nearest error boundary ABOVE it, and the nearest
/// one above this component is the boundary that rendered it - which is how the exception arrives
/// through the framework's own path, with everything that hangs off it (the error count, the logger,
/// <see cref="ErrorBoundaryBase.OnErrorAsync(Exception)"/>) working exactly as it does for an exception
/// that was thrown by the content.
/// </remarks>
internal sealed class _BitErrorBoundaryThrower : ComponentBase
{
    [Parameter] public Exception Exception { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Exception is null) return;

        ExceptionDispatchInfo.Capture(Exception).Throw();
    }
}
