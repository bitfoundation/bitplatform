namespace Bit.Bmage;

/// <summary>
/// Turns pointer drags into annotations. Implementing this interface and registering it with the editor is how a
/// developer adds a custom tool without modifying the library.
/// </summary>
public interface IBmageEditorTool
{
    /// <summary>The tool this handler implements.</summary>
    BmageEditorToolKind Kind { get; }

    /// <summary>
    /// Builds a preview annotation for the drag in progress, or null when there is nothing to show yet. Called on every
    /// pointer move, so it must be cheap.
    /// </summary>
    BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings);

    /// <summary>
    /// Builds the annotation to commit when the drag ends, or null to commit nothing (for example a zero-size shape).
    /// </summary>
    BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings);
}
