namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPdfViewer"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the viewers of an application agree on: their texts (which is where a localized app
/// sets them once), their toolbar and its custom content, what they show while loading, with no document or with a failed one, their size, how they lay out, zoom and paint pages, which
/// of the reader's conveniences they offer (shortcuts, dropping a file, the password prompt) and how they look.
/// The document, the page, zoom and rotation the reader is on and the events are left out on purpose: they are
/// what makes one viewer the one it is.
/// </remarks>
public class BitPdfViewerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPdfViewer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPdfViewer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPdfViewer)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether a pdf dropped onto the viewer opens in it.
    /// </summary>
    public bool? AllowDropFile { get; set; }

    /// <summary>
    /// Offloads document parsing and page rendering to a background thread where the runtime provides one.
    /// </summary>
    public bool? BackgroundRendering { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the viewer.
    /// </summary>
    public BitPdfViewerClassStyles? Classes { get; set; }

    /// <summary>
    /// What dragging on the document surface does: select text or pan the document.
    /// </summary>
    public BitPdfCursorTool? CursorTool { get; set; }

    /// <summary>
    /// The side panel open when a document first loads.
    /// </summary>
    public BitPdfSidebar? DefaultSidebar { get; set; }

    /// <summary>
    /// Custom content shown in place of the pages while no document is loaded.
    /// </summary>
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// Whether the viewer handles keyboard shortcuts while it has focus.
    /// </summary>
    public bool? EnableKeyboardShortcuts { get; set; }

    /// <summary>
    /// Custom content shown in place of the pages when a document fails to load, with the error message.
    /// </summary>
    public RenderFragment<string>? ErrorTemplate { get; set; }

    /// <summary>
    /// The CSS height of the viewer.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// The initial zoom behavior.
    /// </summary>
    public BitPdfZoomMode? InitialZoomMode { get; set; }

    /// <summary>
    /// Custom content shown in place of the pages while a document is being fetched and parsed.
    /// </summary>
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The largest file (in bytes) the open-file button and a drop accept.
    /// </summary>
    public long? MaxOpenFileSize { get; set; }

    /// <summary>
    /// How many pages stay materialized in the DOM at once.
    /// </summary>
    public int? MaxRenderedPageCount { get; set; }

    /// <summary>
    /// How many thumbnails stay materialized in the sidebar at once.
    /// </summary>
    public int? MaxRenderedThumbnailCount { get; set; }

    /// <summary>
    /// The largest zoom factor the viewer allows (1 means 100%).
    /// </summary>
    public double? MaxZoom { get; set; }

    /// <summary>
    /// The smallest zoom factor the viewer allows (1 means 100%).
    /// </summary>
    public double? MinZoom { get; set; }

    /// <summary>
    /// Asks for the password of an encrypted document in place of the built-in dialog.
    /// </summary>
    public Func<Task<string?>>? OnPasswordRequested { get; set; }

    /// <summary>
    /// How page content is painted: positioned HTML or a per-page canvas.
    /// </summary>
    public BitPdfRenderMode? RenderMode { get; set; }

    /// <summary>
    /// Whether the document's own user access permissions are enforced.
    /// </summary>
    public bool? RespectPermissions { get; set; }

    /// <summary>
    /// How the pages are laid out on the scrollable surface.
    /// </summary>
    public BitPdfScrollMode? ScrollMode { get; set; }

    /// <summary>
    /// Whether the viewer asks for the password of an encrypted document with a dialog of its own.
    /// </summary>
    public bool? ShowPasswordPrompt { get; set; }

    /// <summary>
    /// Whether the toolbar is shown.
    /// </summary>
    public bool? ShowToolbar { get; set; }

    /// <summary>
    /// How pages are paired into spreads.
    /// </summary>
    public BitPdfSpreadMode? SpreadMode { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the viewer.
    /// </summary>
    public BitPdfViewerClassStyles? Styles { get; set; }

    /// <summary>
    /// How painted text is emitted: one span per run, or one per visual line.
    /// </summary>
    public BitPdfTextCoalescing? TextCoalescing { get; set; }

    /// <summary>
    /// The texts of the viewer UI.
    /// </summary>
    public BitPdfViewerTexts? Texts { get; set; }

    /// <summary>
    /// Custom content rendered at the end of the toolbar, after the built-in controls.
    /// </summary>
    public RenderFragment<BitPdfViewer>? ToolbarEndTemplate { get; set; }

    /// <summary>
    /// Which controls the toolbar offers.
    /// </summary>
    public BitPdfToolbarItems? ToolbarItems { get; set; }

    /// <summary>
    /// Custom content rendered at the start of the toolbar, before the built-in controls.
    /// </summary>
    public RenderFragment<BitPdfViewer>? ToolbarStartTemplate { get; set; }

    /// <summary>
    /// The CSS width of the viewer.
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// The explicit zoom factors the toolbar's zoom dropdown offers (1 means 100%).
    /// </summary>
    public IEnumerable<double>? ZoomPresets { get; set; }

    /// <summary>
    /// The multiplier applied by a zoom in or out step.
    /// </summary>
    public double? ZoomStep { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPdfViewer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPdfViewer"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPdfViewer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPdfViewer"/>.
    /// </remarks>
    /// <param name="bitPdfViewer">
    /// The <see cref="BitPdfViewer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPdfViewer bitPdfViewer)
    {
        if (bitPdfViewer is null) return;

        UpdateBaseParameters(bitPdfViewer);

        if (AllowDropFile.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(AllowDropFile), AllowDropFile.Value, static p => p.AllowDropFile, static (p, v) => p.AllowDropFile = v);
        }

        if (BackgroundRendering.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(BackgroundRendering), BackgroundRendering.Value, static p => p.BackgroundRendering, static (p, v) => p.BackgroundRendering = v);
        }

        if (Classes is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (CursorTool.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(CursorTool), CursorTool.Value, static p => p.CursorTool, static (p, v) => p.CursorTool = v);
        }

        if (DefaultSidebar.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(DefaultSidebar), DefaultSidebar.Value, static p => p.DefaultSidebar, static (p, v) => p.DefaultSidebar = v);
        }

        if (EmptyTemplate is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(EmptyTemplate), EmptyTemplate, static p => p.EmptyTemplate, static (p, v) => p.EmptyTemplate = v);
        }

        if (EnableKeyboardShortcuts.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(EnableKeyboardShortcuts), EnableKeyboardShortcuts.Value, static p => p.EnableKeyboardShortcuts, static (p, v) => p.EnableKeyboardShortcuts = v);
        }

        if (ErrorTemplate is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(ErrorTemplate), ErrorTemplate, static p => p.ErrorTemplate, static (p, v) => p.ErrorTemplate = v);
        }

        if (Height is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(Height), Height, static p => p.Height, static (p, v) => p.Height = v);
        }

        if (InitialZoomMode.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(InitialZoomMode), InitialZoomMode.Value, static p => p.InitialZoomMode, static (p, v) => p.InitialZoomMode = v);
        }

        if (LoadingTemplate is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(LoadingTemplate), LoadingTemplate, static p => p.LoadingTemplate, static (p, v) => p.LoadingTemplate = v);
        }

        if (MaxOpenFileSize.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(MaxOpenFileSize), MaxOpenFileSize.Value, static p => p.MaxOpenFileSize, static (p, v) => p.MaxOpenFileSize = v);
        }

        if (MaxRenderedPageCount.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(MaxRenderedPageCount), MaxRenderedPageCount.Value, static p => p.MaxRenderedPageCount, static (p, v) => p.MaxRenderedPageCount = v);
        }

        if (MaxRenderedThumbnailCount.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(MaxRenderedThumbnailCount), MaxRenderedThumbnailCount.Value, static p => p.MaxRenderedThumbnailCount, static (p, v) => p.MaxRenderedThumbnailCount = v);
        }

        if (MaxZoom.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(MaxZoom), MaxZoom.Value, static p => p.MaxZoom, static (p, v) => p.MaxZoom = v);
        }

        if (MinZoom.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(MinZoom), MinZoom.Value, static p => p.MinZoom, static (p, v) => p.MinZoom = v);
        }

        if (OnPasswordRequested is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(OnPasswordRequested), OnPasswordRequested, static p => p.OnPasswordRequested, static (p, v) => p.OnPasswordRequested = v);
        }

        if (RenderMode.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(RenderMode), RenderMode.Value, static p => p.RenderMode, static (p, v) => p.RenderMode = v);
        }

        if (RespectPermissions.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(RespectPermissions), RespectPermissions.Value, static p => p.RespectPermissions, static (p, v) => p.RespectPermissions = v);
        }

        if (ScrollMode.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(ScrollMode), ScrollMode.Value, static p => p.ScrollMode, static (p, v) => p.ScrollMode = v);
        }

        if (ShowPasswordPrompt.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(ShowPasswordPrompt), ShowPasswordPrompt.Value, static p => p.ShowPasswordPrompt, static (p, v) => p.ShowPasswordPrompt = v);
        }

        if (ShowToolbar.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(ShowToolbar), ShowToolbar.Value, static p => p.ShowToolbar, static (p, v) => p.ShowToolbar = v);
        }

        if (SpreadMode.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(SpreadMode), SpreadMode.Value, static p => p.SpreadMode, static (p, v) => p.SpreadMode = v);
        }

        if (Styles is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (TextCoalescing.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(TextCoalescing), TextCoalescing.Value, static p => p.TextCoalescing, static (p, v) => p.TextCoalescing = v);
        }

        if (Texts is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(Texts), Texts, static p => p.Texts, static (p, v) => p.Texts = v);
        }

        if (ToolbarEndTemplate is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(ToolbarEndTemplate), ToolbarEndTemplate, static p => p.ToolbarEndTemplate, static (p, v) => p.ToolbarEndTemplate = v);
        }

        if (ToolbarItems.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(ToolbarItems), ToolbarItems.Value, static p => p.ToolbarItems, static (p, v) => p.ToolbarItems = v);
        }

        if (ToolbarStartTemplate is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(ToolbarStartTemplate), ToolbarStartTemplate, static p => p.ToolbarStartTemplate, static (p, v) => p.ToolbarStartTemplate = v);
        }

        if (Width is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(Width), Width, static p => p.Width, static (p, v) => p.Width = v);
        }

        if (ZoomPresets is not null)
        {
            bitPdfViewer.TakeFromCascade(nameof(ZoomPresets), ZoomPresets, static p => p.ZoomPresets, static (p, v) => p.ZoomPresets = v);
        }

        if (ZoomStep.HasValue)
        {
            bitPdfViewer.TakeFromCascade(nameof(ZoomStep), ZoomStep.Value, static p => p.ZoomStep, static (p, v) => p.ZoomStep = v);
        }
    }
}
