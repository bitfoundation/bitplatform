namespace Bit.BlazorUI;

/// <summary>
/// The error boundary a <see cref="BitAiRendererBoundary"/> renders its content in: Blazor's own, telling its owner
/// whether it currently stands in for content that failed.
/// </summary>
/// <remarks>
/// The owner retries the content by replacing a failed boundary with a new one rather than through
/// <see cref="ErrorBoundaryBase.Recover"/>. Recovering queues a render of the boundary, and the parent's render that
/// asked for the retry queues it again with the new content, so a content that throws in its own render again is
/// handled while that render is still pending: the boundary's request to draw its error content is folded into the
/// pending one, which runs ahead of the empty render the renderer forces on a failed subtree - and the empty render
/// wins, leaving neither the content nor the notice. A new boundary has no such render pending, so it fails, and
/// recovers, the way it did the first time.
/// </remarks>
internal sealed class BitAiRendererErrorBoundary : ErrorBoundary
{
    /// <summary>
    /// Whether the boundary currently shows its error content in place of the content it protects.
    /// </summary>
    internal bool HasFailed => CurrentException is not null;
}
