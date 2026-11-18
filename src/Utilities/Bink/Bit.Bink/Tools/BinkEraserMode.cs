namespace Bit.Bink;

/// <summary>How the eraser removes ink. The three modes behave differently on purpose; pick the one that matches the application.</summary>
public enum BinkEraserMode : byte
{
    /// <summary>
    /// Deletes any stroke the eraser touches, whole. Predictable and cheap, and the behaviour users expect from
    /// note-taking apps. Erasing is exactly reversible.
    /// </summary>
    Stroke = 0,

    /// <summary>
    /// Removes only the touched portion, splitting strokes into fragments. Feels like a real eraser and keeps the
    /// result fully vector, at the cost of creating additional elements.
    /// </summary>
    Segment = 1,

    /// <summary>
    /// Draws an eraser stroke that masks out everything below it within the layer (destination-out compositing).
    /// Nothing is deleted, so the erase is itself an object that can be undone, re-ordered or removed later, and it
    /// can erase parts of images. Exports as an SVG mask.
    /// </summary>
    Pixel = 2,
}
