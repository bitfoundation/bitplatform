using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// What a tool is allowed to do and needs to know. Supplied by the host (the canvas component or a headless engine).
/// </summary>
public interface IBinkToolContext
{
    /// <summary>The document being edited.</summary>
    BinkDocument Document { get; }

    /// <summary>The current selection.</summary>
    BinkSelection Selection { get; }

    /// <summary>Undo/redo, so tools can group a gesture into one step.</summary>
    BinkHistoryManager History { get; }

    /// <summary>The current viewport (document to view mapping).</summary>
    BinkViewport Viewport { get; set; }

    /// <summary>The layer new elements go to.</summary>
    Guid ActiveLayerId { get; }

    /// <summary>The current brush.</summary>
    BinkBrush Brush { get; }

    /// <summary>Style used for newly created shapes and annotations.</summary>
    BinkShapeStyle ShapeStyle { get; }

    /// <summary>Size of the drawing surface in view (CSS pixel) units.</summary>
    Vector2 ViewSize { get; }

    /// <summary>Requests that the overlay (live stroke, selection handles, marquee) be redrawn on the next frame.</summary>
    void InvalidateOverlay();

    /// <summary>
    /// Requests that the document surface be redrawn. The region is a hint: the canvas renderer redraws the
    /// visible area as a whole, so passing one is never wrong but does not currently narrow the work.
    /// </summary>
    /// <param name="region">The changed area in document coordinates, or null for the whole document.</param>
    void InvalidateDocument(BinkRect? region = null);

    /// <summary>
    /// Declares that the tool has taken the gesture and wants it to continue outside the canvas. The browser
    /// layer already captures on contact, so on the canvas host this records intent rather than doing more work;
    /// a different host may need it.
    /// </summary>
    void CapturePointer(int pointerId);

    /// <summary>Releases a pointer taken with <see cref="CapturePointer"/>.</summary>
    void ReleasePointer(int pointerId);

    /// <summary>Reports a cursor hint (a CSS cursor value such as <c>crosshair</c> or <c>grabbing</c>).</summary>
    void SetCursor(string cursor);

    /// <summary>Announces a message to assistive technology through the canvas's live region.</summary>
    void Announce(string message);
}

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

/// <summary>A keyboard event forwarded to tools.</summary>
/// <param name="Key">The <c>KeyboardEvent.key</c> value, e.g. <c>"Escape"</c> or <c>"a"</c>.</param>
/// <param name="Ctrl">Control (or Command on macOS) held.</param>
/// <param name="Shift">Shift held.</param>
/// <param name="Alt">Alt/Option held.</param>
public readonly record struct BinkToolKeyEvent(string Key, bool Ctrl, bool Shift, bool Alt);

/// <summary>Base class with no-op implementations so tools only override what they need.</summary>
public abstract class BinkToolBase : IBinkTool
{
    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public virtual string Cursor => "crosshair";

    /// <inheritdoc />
    public virtual bool WantsCoalescedInput => false;

    /// <inheritdoc />
    public virtual bool ModifiesDocument => true;

    /// <inheritdoc />
    public virtual void Activate(IBinkToolContext context) { }

    /// <inheritdoc />
    public virtual void Deactivate(IBinkToolContext context) { }

    /// <inheritdoc />
    public abstract bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample);

    /// <inheritdoc />
    public virtual void OnPointerMove(IBinkToolContext context, in BinkInputSample sample) { }

    /// <inheritdoc />
    public virtual void OnPointerUp(IBinkToolContext context, in BinkInputSample sample) { }

    /// <inheritdoc />
    public virtual void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample) { }

    /// <inheritdoc />
    public virtual void DrawOverlay(IBinkToolContext context, IBinkRenderContext render) { }

    /// <inheritdoc />
    public virtual bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key) => false;
}
