namespace Bit.Bink;

/// <summary>Which canvas layer a command buffer is drawn onto.</summary>
public enum BinkCanvasLayer
{
    /// <summary>The document surface: committed elements. Redrawn only when the document or viewport changes.</summary>
    Document,
    /// <summary>The overlay surface: the live stroke, selection handles and tool previews. Redrawn every frame of a gesture.</summary>
    Overlay,
}
