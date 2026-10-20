using System.Drawing;
using Bit.Bmage.Annotations;
using Bit.Bmage.Capabilities;
using Bit.Bmage.Codecs;
using Bit.Bmage.Editing;
using Bit.Bmage.Editor.Localization;
using Bit.Bmage.Editor.Tools;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Editor.Components;

/// <summary>
/// Everything a custom toolbar or panel needs to drive the editor. Handed to the <c>Toolbar</c>, <c>Panel</c> and
/// <c>Footer</c> render fragments so a host can build its own UI without reimplementing any editing logic.
/// </summary>
/// <example>
/// <code>
/// &lt;BmageEditor&gt;
///   &lt;Toolbar Context="editor"&gt;
///     &lt;button @onclick="() =&gt; editor.SetTool(EditorToolKind.Crop)"&gt;Crop&lt;/button&gt;
///     &lt;button disabled="@(!editor.CanUndo)" @onclick="editor.Undo"&gt;Undo&lt;/button&gt;
///   &lt;/Toolbar&gt;
/// &lt;/BmageEditor&gt;
/// </code>
/// </example>
public sealed class BmageEditorContext
{
    private readonly BmageEditor _editor;

    internal BmageEditorContext(BmageEditor editor) => _editor = editor;

    /// <summary>Localised strings.</summary>
    public BmageEditorStrings Strings => _editor.Strings;

    /// <summary>The live editing session, or null when no image is loaded.</summary>
    public ImageEditSession? Session => _editor.Session;

    /// <summary>The current document, or null when no image is loaded.</summary>
    public ImageDocument? Document => _editor.Document;

    /// <summary>True when an image is loaded.</summary>
    public bool HasImage => _editor.Session is not null;

    /// <summary>True while an operation is running.</summary>
    public bool IsBusy => _editor.IsBusy;

    /// <summary>The active tool.</summary>
    public EditorToolKind Tool => _editor.Tool;

    /// <summary>The current drawing settings.</summary>
    public EditorToolSettings Settings => _editor.Settings;

    /// <summary>The detected browser capabilities, or null before detection completes.</summary>
    public ImageCapabilities? Capabilities => _editor.BrowserCapabilities;

    /// <summary>Current zoom factor.</summary>
    public double Zoom => _editor.Zoom;

    /// <summary>The id of the selected annotation, if any.</summary>
    public Guid? SelectedAnnotationId => _editor.SelectedAnnotationId;

    /// <summary>The selected annotation, if any.</summary>
    public Annotation? SelectedAnnotation => SelectedAnnotationId is { } id ? Document?.FindAnnotation(id) : null;

    /// <summary>Annotations in the document, in draw order.</summary>
    public IReadOnlyList<Annotation> Annotations => Document?.Annotations ?? [];

    /// <summary>True when there is something to undo.</summary>
    public bool CanUndo => Session?.History.CanUndo ?? false;

    /// <summary>True when there is something to redo.</summary>
    public bool CanRedo => Session?.History.CanRedo ?? false;

    /// <summary>The history entries, oldest first.</summary>
    public IReadOnlyList<HistoryEntry> History => Session?.History.Entries ?? [];

    /// <summary>Index of the current history entry.</summary>
    public int HistoryPosition => Session?.History.Position ?? 0;

    /// <summary>
    /// Size of the image the editor will export, or <see cref="Size.Empty"/> when the current edit cannot be applied
    /// (for example a crop that no longer intersects the image after a rotation was inserted before it).
    /// </summary>
    public Size OutputSize => Session is { } s && s.Document.TryGetOutputSize(out var size) ? size : Size.Empty;

    /// <summary>Size of the source image.</summary>
    public Size SourceSize => Session?.SourceSize ?? Size.Empty;

    /// <summary>True when editing happens on a downscaled proxy because the source is large.</summary>
    public bool UsesProxy => Session?.UsesProxy ?? false;

    // ---- Actions ----

    /// <summary>Switches the active tool.</summary>
    public void SetTool(EditorToolKind tool) => _editor.SetTool(tool);

    /// <summary>Updates the drawing settings.</summary>
    public void SetSettings(EditorToolSettings settings) => _editor.SetSettings(settings);

    /// <summary>Updates one field of the drawing settings.</summary>
    public void UpdateSettings(Func<EditorToolSettings, EditorToolSettings> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        _editor.SetSettings(update(_editor.Settings));
    }

    /// <summary>Sets the aspect ratio the crop tool enforces, or null for freeform.</summary>
    public void SetCropAspect(AspectRatio? aspect) => _editor.SetCropAspect(aspect);

    /// <summary>Applies the pending crop.</summary>
    public Task ApplyCropAsync() => _editor.ApplyCropAsync();

    /// <summary>Discards the pending crop.</summary>
    public void CancelCrop() => _editor.CancelCrop();

    /// <summary>Rotates by a multiple of 90 degrees.</summary>
    public Task RotateAsync(int degrees) => _editor.RotateAsync(degrees);

    /// <summary>Mirrors the image.</summary>
    public Task FlipAsync(bool horizontal) => _editor.FlipAsync(horizontal);

    /// <summary>
    /// Sets an adjustment, replacing any existing one of the same type. Pass <paramref name="transient"/> while a slider
    /// is being dragged so the history gets one entry rather than hundreds.
    /// </summary>
    public Task SetAdjustmentAsync<TOperation>(TOperation? operation, string label, bool transient = false) where TOperation : class, IImageOperation
        => _editor.SetAdjustmentAsync(operation, label, transient);

    /// <summary>Adds an arbitrary operation to the pipeline.</summary>
    public Task AddOperationAsync(IImageOperation operation, string? label = null) => _editor.AddOperationAsync(operation, label);

    /// <summary>Adds an annotation.</summary>
    public Task AddAnnotationAsync(Annotation annotation, string? label = null) => _editor.AddAnnotationAsync(annotation, label);

    /// <summary>Replaces an annotation, matched by id.</summary>
    public Task UpdateAnnotationAsync(Annotation annotation, bool transient = false) => _editor.UpdateAnnotationAsync(annotation, transient);

    /// <summary>Removes an annotation.</summary>
    public Task DeleteAnnotationAsync(Guid id) => _editor.DeleteAnnotationAsync(id);

    /// <summary>Selects an annotation, or clears the selection.</summary>
    public void SelectAnnotation(Guid? id) => _editor.SelectAnnotation(id);

    /// <summary>Undoes the last edit.</summary>
    public void Undo() => _editor.Undo();

    /// <summary>Redoes the last undone edit.</summary>
    public void Redo() => _editor.Redo();

    /// <summary>Discards every edit.</summary>
    public void Reset() => _editor.Reset();

    /// <summary>Jumps to an entry in the history and shows it.</summary>
    public Task GoToHistoryAsync(int index) => _editor.GoToHistoryAsync(index);

    /// <summary>Sets the zoom factor.</summary>
    public void SetZoom(double zoom) => _editor.SetZoom(zoom);

    /// <summary>Resets zoom and pan.</summary>
    public void FitToWindow() => _editor.FitToWindow();

    /// <summary>Loads an image from any source.</summary>
    public Task LoadAsync(Input.ImageSource source) => _editor.LoadAsync(source);

    /// <summary>Loads the image on the clipboard.</summary>
    public Task PasteAsync() => _editor.PasteAsync();

    /// <summary>Renders at full resolution and encodes.</summary>
    public Task<EncodedImage?> ExportAsync(ImageExportOptions? options = null) => _editor.ExportAsync(options);

    /// <summary>Renders, encodes and downloads.</summary>
    public Task DownloadAsync(string fileName, ImageExportOptions? options = null) => _editor.DownloadAsync(fileName, options);

    /// <summary>Copies the result to the clipboard as PNG.</summary>
    public Task CopyToClipboardAsync() => _editor.CopyToClipboardAsync();

    /// <summary>Finds the current value of an adjustment in the pipeline, if present.</summary>
    public TOperation? FindAdjustment<TOperation>() where TOperation : class, IImageOperation
    {
        var pipeline = Document?.Pipeline;
        if (pipeline is null) return null;
        for (var i = 0; i < pipeline.Count; i++)
            if (pipeline[i] is TOperation match) return match;
        return null;
    }
}
