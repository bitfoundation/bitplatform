using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bmotion;

/// <summary>
/// Starts a drag on a Bmotion element from anywhere - motion.dev's <c>useDragControls</c>.
/// Assign an instance to the element's <c>DragControls</c> parameter, then call
/// <see cref="StartAsync"/> from any other element's <c>@onpointerdown</c>:
/// <code>
/// &lt;div class="track" @onpointerdown="e =&gt; _controls.StartAsync(e)"&gt;
///     &lt;Bmotion Drag="BmDrag.X" DragControls="_controls" DragListener="false"&gt;
///         &lt;div class="thumb" /&gt;
///     &lt;/Bmotion&gt;
/// &lt;/div&gt;
/// </code>
/// Set <c>DragListener="false"</c> on the target to make the controls the only way to start
/// the drag. For the simpler "drag by a handle inside the element" case, prefer the
/// <c>DragHandle</c> selector parameter instead.
/// </summary>
public sealed class BmDragControls
{
    private Bmotion? _target;

    internal void Attach(Bmotion target) => _target = target;

    internal void Detach(Bmotion target)
    {
        if (ReferenceEquals(_target, target)) _target = null;
    }

    /// <summary>
    /// Starts dragging the attached element from the given pointer event. No-op when no
    /// element is attached (or it hasn't rendered yet).
    /// </summary>
    public ValueTask StartAsync(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return _target?.StartDragAsync(e) ?? ValueTask.CompletedTask;
    }
}
