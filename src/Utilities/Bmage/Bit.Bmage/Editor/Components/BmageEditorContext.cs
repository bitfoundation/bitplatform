using System.Drawing;
using Bit.Bmage.Annotations;
using Bit.Bmage.Capabilities;
using Bit.Bmage.Codecs;
using Bit.Bmage.Editing;
using Bit.Bmage.Editor.Localization;
using Bit.Bmage.Editor.Tools;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
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

    /// <summary>The document's layers, bottom first.</summary>
    public IReadOnlyList<Layer> Layers => Document?.Layers ?? [];

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

    /// <summary>
    /// Applies any change to the document as one history entry and one render, for edits that touch several steps at
    /// once. Pass <paramref name="transient"/> while a control is being dragged, as for <see cref="SetAdjustmentAsync"/>.
    /// </summary>
    public Task ApplyAsync(Func<ImageDocument, ImageDocument> change, string label, bool transient = false) => _editor.ApplyAsync(change, label, transient);

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

    /// <summary>
    /// Adds a layer on top of the others: an image placed on the photo, a colour fill, or an adjustment layer, each with
    /// a blend mode, an opacity and an optional mask. Coordinates are those of the edited image at full resolution.
    /// </summary>
    public Task AddLayerAsync(Layer layer, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return ApplyAsync(d => d.AddLayer(layer), label ?? $"Add {layer.Kind.ToLowerInvariant()} layer");
    }

    /// <summary>
    /// Replaces a layer, matched by id. Pass <paramref name="transient"/> while a control is being dragged, as for
    /// <see cref="SetAdjustmentAsync"/>.
    /// </summary>
    public Task UpdateLayerAsync(Layer layer, bool transient = false)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return ApplyAsync(d => d.UpdateLayer(layer), $"Edit {layer.DisplayName.ToLowerInvariant()}", transient);
    }

    /// <summary>Removes a layer.</summary>
    public Task RemoveLayerAsync(Guid id) => ApplyAsync(d => d.RemoveLayer(id), "Remove layer");

    /// <summary>Moves a layer to <paramref name="index"/> in the stack, 0 being the bottom.</summary>
    public Task MoveLayerAsync(Guid id, int index) => ApplyAsync(d => d.MoveLayer(id, index), "Reorder layers");

    /// <summary>The id of the layer selected in the layer panel, if any. Painting goes to it when it is a paint layer.</summary>
    public Guid? ActiveLayerId => _editor.ActiveLayerId;

    /// <summary>The layer selected in the layer panel, if any.</summary>
    public Layer? ActiveLayer => ActiveLayerId is { } id ? Document?.FindLayer(id) : null;

    /// <summary>Selects a layer, or clears the selection.</summary>
    public void SelectLayer(Guid? id) => _editor.SelectLayer(id);

    /// <summary>Duplicates a layer above itself and selects the copy.</summary>
    public Task DuplicateLayerAsync(Guid id) => _editor.DuplicateLayerAsync(id);

    /// <summary>Groups a layer with the one below it, or moves it into the group below it. See <see cref="BmageEditor.GroupWithLayerBelowAsync"/>.</summary>
    public Task GroupWithLayerBelowAsync(Guid id) => _editor.GroupWithLayerBelowAsync(id);

    /// <summary>Replaces a group by its layers.</summary>
    public Task UngroupAsync(Guid id) => _editor.UngroupAsync(id);

    /// <summary>Moves a layer out of its group, directly above it.</summary>
    public Task MoveLayerOutOfGroupAsync(Guid id) => _editor.MoveLayerOutOfGroupAsync(id);

    /// <summary>Adds an empty paint layer on top, selects it and picks the brush.</summary>
    public Task AddPaintLayerAsync() => _editor.AddPaintLayerAsync();

    /// <summary>Loads an image and places it as a layer in the middle of the image.</summary>
    public Task AddImageLayerAsync(Input.ImageSource source) => _editor.AddImageLayerAsync(source);

    /// <summary>The pixel selection, in output coordinates, or null when nothing is selected.</summary>
    public Masks.Mask? Selection => _editor.Selection;

    /// <summary>True when there is a pixel selection.</summary>
    public bool HasSelection => _editor.Selection is not null;

    /// <summary>Sets the pixel selection, or clears it with null.</summary>
    public void SetSelection(Masks.Mask? selection) => _editor.SetSelection(selection);

    /// <summary>Selects the whole image.</summary>
    public void SelectAll() => _editor.SelectAll();

    /// <summary>Clears the pixel selection.</summary>
    public void Deselect() => _editor.SetSelection(null);

    /// <summary>Selects everything that is not selected.</summary>
    public void InvertSelection() => _editor.InvertSelection();

    /// <summary>Makes the selected pixels transparent.</summary>
    public Task ClearSelectionAsync() => _editor.ClearSelectionAsync();

    /// <summary>Fills the selection with the fill colour, as a layer.</summary>
    public Task FillSelectionAsync() => _editor.FillSelectionAsync();

    /// <summary>Crops the image to the selection's bounds.</summary>
    public Task CropToSelectionAsync() => _editor.CropToSelectionAsync();

    /// <summary>Copies the selected pixels, as the image is composited, to a new image layer in the same place.</summary>
    public Task CopySelectionToLayerAsync() => _editor.CopySelectionToLayerAsync();

    /// <summary>True while the canvas shows the original instead of the edit.</summary>
    public bool IsComparing => _editor.IsComparing;

    /// <summary>Shows the original instead of the edit, or the edit again.</summary>
    public Task SetCompareAsync(bool compare) => _editor.SetCompareAsync(compare);

    /// <summary>Levels the image by up to 45 degrees; a later call replaces it, 0 removes it.</summary>
    public Task SetStraightenAsync(double degrees, bool transient = false) => _editor.SetStraightenAsync(degrees, transient);

    /// <summary>Resizes the image to exactly the given size.</summary>
    public Task ResizeAsync(int width, int height) => _editor.ResizeAsync(width, height);

    /// <summary>Sets the canvas size without scaling the image.</summary>
    public Task SetCanvasSizeAsync(int width, int height, Geometry.Anchor anchor, Rgba32 background) => _editor.SetCanvasSizeAsync(width, height, anchor, background);

    /// <summary>Crops away plain or transparent edges.</summary>
    public Task TrimAsync() => _editor.TrimAsync();

    /// <summary>Turns automatic white balance on or off.</summary>
    public Task ToggleAutoWhiteBalanceAsync() => _editor.ToggleAutoWhiteBalanceAsync();

    /// <summary>Turns automatic levels on or off.</summary>
    public Task ToggleAutoLevelsAsync() => _editor.ToggleAutoLevelsAsync();

    /// <summary>Makes the next eyedropper pick set the white balance.</summary>
    public void PickWhiteBalance() => _editor.PickWhiteBalance();

    /// <summary>True while the next eyedropper pick sets the white balance.</summary>
    public bool IsPickingWhiteBalance => _editor.IsPickingWhiteBalance;

    /// <summary>
    /// Replaces the edit with a saved one, such as a document read back with
    /// <see cref="Serialization.BmageJson.DeserializeDocument"/>, as one undoable step. The document must have been made
    /// for an image of the size loaded now.
    /// </summary>
    /// <exception cref="ArgumentException">The document was made for an image of another size.</exception>
    public Task RestoreDocumentAsync(ImageDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (Session is { } session && document.SourceSize != session.SourceSize)
            throw new ArgumentException($"The document was made for a {document.SourceSize.Width}x{document.SourceSize.Height} image, but a {session.SourceSize.Width}x{session.SourceSize.Height} one is loaded.", nameof(document));
        return ApplyAsync(_ => document, "Restore edit");
    }

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

    /// <summary>Downloads the original, the edit and its images as one project file.</summary>
    public Task SaveProjectAsync(string fileName = "edit") => _editor.SaveProjectAsync(fileName);

    /// <summary>Opens a project file, replacing the image and its history.</summary>
    public Task OpenProjectAsync(System.IO.Stream stream) => _editor.OpenProjectAsync(stream);

    /// <summary>Opens a project file the user picked, replacing the image and its history.</summary>
    public Task OpenProjectAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file) => _editor.OpenProjectAsync(file);

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
