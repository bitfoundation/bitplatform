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

    /// <summary>
    /// How moving, resizing and drawing shapes snap. Defaults to no snapping, so a host written before snapping
    /// existed keeps compiling and behaving as it did.
    /// </summary>
    BinkSnapOptions Snapping => BinkSnapOptions.None;

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
