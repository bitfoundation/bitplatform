using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// An interaction mode: what pointer input does. Tools are stateful for the duration of a gesture and are
/// driven by the host, which routes pointer batches to the active tool.
/// </summary>
/// <remarks>
/// Implement this to add a tool. A tool receives samples in view coordinates and uses
/// <see cref="IBinkToolContext.Viewport"/> to convert them. Tools mutate the document exclusively through
/// operations so everything they do is undoable and can be synchronized.
/// </remarks>
public interface IBinkTool
{
    /// <summary>Stable identifier, e.g. <c>"pen"</c> or <c>"select"</c>.</summary>
    string Id { get; }

    /// <summary>Display name for toolbars.</summary>
    string DisplayName { get; }

    /// <summary>The CSS cursor to show when this tool is active and idle.</summary>
    string Cursor { get; }

    /// <summary>
    /// Whether the tool wants raw high-frequency (coalesced) samples. Drawing tools do; selection tools do not,
    /// which lets the host skip forwarding intermediate samples.
    /// </summary>
    bool WantsCoalescedInput { get; }

    /// <summary>
    /// Whether the tool changes the document. Tools that only move the viewport, such as panning and zooming,
    /// return false and keep working on a read-only canvas.
    /// </summary>
    bool ModifiesDocument { get; }

    /// <summary>
    /// Whether the tool wants the pointer's movement while no button is pressed, delivered to
    /// <see cref="OnPointerMove"/> with <see cref="BinkPointerPhase.Hover"/>, for a preview that follows the pointer
    /// such as the next segment of a polyline. Off by default: on Blazor Server every hovering frame is a message.
    /// </summary>
    bool WantsHover => false;

    /// <summary>Called when the tool becomes active.</summary>
    void Activate(IBinkToolContext context);

    /// <summary>
    /// Called when the tool is replaced. Implementations must cancel any gesture in progress and release every
    /// resource it holds, in particular a history group: leaving one open would silently disable undo.
    /// </summary>
    void Deactivate(IBinkToolContext context);

    /// <summary>A pointer went down. Return true to take the gesture (the host then captures the pointer).</summary>
    bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample);

    /// <summary>A pointer moved during a gesture this tool took.</summary>
    void OnPointerMove(IBinkToolContext context, in BinkInputSample sample);

    /// <summary>The pointer was released, ending the gesture.</summary>
    void OnPointerUp(IBinkToolContext context, in BinkInputSample sample);

    /// <summary>The gesture was cancelled by the browser or the application.</summary>
    void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample);

    /// <summary>
    /// Draws the tool's live preview on the overlay, above the document, in document coordinates.
    /// Called on every animation frame while <see cref="IBinkToolContext.InvalidateOverlay"/> has been requested.
    /// </summary>
    void DrawOverlay(IBinkToolContext context, IBinkRenderContext render);

    /// <summary>Handles a keyboard shortcut while the tool is active. Return true when handled.</summary>
    bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key);
}
