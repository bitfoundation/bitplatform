namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPdfViewer"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the viewers of an application agree on: their texts (which is where a localized app
/// sets them once), their toolbar and its custom content, what they show with no document or a failed one, their size, how they lay out, zoom and paint pages, which
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

        if (AllowDropFile.HasValue && bitPdfViewer.HasNotBeenSet(nameof(AllowDropFile)))
        {
            bitPdfViewer.AllowDropFile = AllowDropFile.Value;
        }

        if (BackgroundRendering.HasValue && bitPdfViewer.HasNotBeenSet(nameof(BackgroundRendering)))
        {
            bitPdfViewer.BackgroundRendering = BackgroundRendering.Value;
        }

        // This runs on every render of every viewer under the BitParams, so the values that drive the class or the
        // style of the root are only assigned - and the builder only reset - when they differ from the ones the
        // viewer already holds: an unchanged one would rebuild both strings on every render for nothing.
        if (Classes is not null && bitPdfViewer.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitPdfViewer.Classes, Classes) is false)
        {
            bitPdfViewer.Classes = Classes;

            bitPdfViewer.ClassBuilder.Reset();
        }

        if (CursorTool.HasValue && bitPdfViewer.HasNotBeenSet(nameof(CursorTool)))
        {
            bitPdfViewer.CursorTool = CursorTool.Value;
        }

        if (DefaultSidebar.HasValue && bitPdfViewer.HasNotBeenSet(nameof(DefaultSidebar)))
        {
            bitPdfViewer.DefaultSidebar = DefaultSidebar.Value;
        }

        if (EmptyTemplate is not null && bitPdfViewer.HasNotBeenSet(nameof(EmptyTemplate)))
        {
            bitPdfViewer.EmptyTemplate = EmptyTemplate;
        }

        if (EnableKeyboardShortcuts.HasValue && bitPdfViewer.HasNotBeenSet(nameof(EnableKeyboardShortcuts)))
        {
            bitPdfViewer.EnableKeyboardShortcuts = EnableKeyboardShortcuts.Value;
        }

        if (ErrorTemplate is not null && bitPdfViewer.HasNotBeenSet(nameof(ErrorTemplate)))
        {
            bitPdfViewer.ErrorTemplate = ErrorTemplate;
        }

        if (Height is not null && bitPdfViewer.HasNotBeenSet(nameof(Height)) && bitPdfViewer.Height != Height)
        {
            bitPdfViewer.Height = Height;

            bitPdfViewer.StyleBuilder.Reset();
        }

        if (InitialZoomMode.HasValue && bitPdfViewer.HasNotBeenSet(nameof(InitialZoomMode)))
        {
            bitPdfViewer.InitialZoomMode = InitialZoomMode.Value;
        }

        if (MaxOpenFileSize.HasValue && bitPdfViewer.HasNotBeenSet(nameof(MaxOpenFileSize)))
        {
            bitPdfViewer.MaxOpenFileSize = MaxOpenFileSize.Value;
        }

        if (MaxRenderedPageCount.HasValue && bitPdfViewer.HasNotBeenSet(nameof(MaxRenderedPageCount)))
        {
            bitPdfViewer.MaxRenderedPageCount = MaxRenderedPageCount.Value;
        }

        if (MaxRenderedThumbnailCount.HasValue && bitPdfViewer.HasNotBeenSet(nameof(MaxRenderedThumbnailCount)))
        {
            bitPdfViewer.MaxRenderedThumbnailCount = MaxRenderedThumbnailCount.Value;
        }

        if (MaxZoom.HasValue && bitPdfViewer.HasNotBeenSet(nameof(MaxZoom)))
        {
            bitPdfViewer.MaxZoom = MaxZoom.Value;
        }

        if (MinZoom.HasValue && bitPdfViewer.HasNotBeenSet(nameof(MinZoom)))
        {
            bitPdfViewer.MinZoom = MinZoom.Value;
        }

        if (OnPasswordRequested is not null && bitPdfViewer.HasNotBeenSet(nameof(OnPasswordRequested)))
        {
            bitPdfViewer.OnPasswordRequested = OnPasswordRequested;
        }

        if (RenderMode.HasValue && bitPdfViewer.HasNotBeenSet(nameof(RenderMode)))
        {
            bitPdfViewer.RenderMode = RenderMode.Value;
        }

        if (RespectPermissions.HasValue && bitPdfViewer.HasNotBeenSet(nameof(RespectPermissions)))
        {
            bitPdfViewer.RespectPermissions = RespectPermissions.Value;
        }

        if (ScrollMode.HasValue && bitPdfViewer.HasNotBeenSet(nameof(ScrollMode)))
        {
            bitPdfViewer.ScrollMode = ScrollMode.Value;
        }

        if (ShowPasswordPrompt.HasValue && bitPdfViewer.HasNotBeenSet(nameof(ShowPasswordPrompt)))
        {
            bitPdfViewer.ShowPasswordPrompt = ShowPasswordPrompt.Value;
        }

        if (ShowToolbar.HasValue && bitPdfViewer.HasNotBeenSet(nameof(ShowToolbar)))
        {
            bitPdfViewer.ShowToolbar = ShowToolbar.Value;
        }

        if (SpreadMode.HasValue && bitPdfViewer.HasNotBeenSet(nameof(SpreadMode)))
        {
            bitPdfViewer.SpreadMode = SpreadMode.Value;
        }

        if (Styles is not null && bitPdfViewer.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitPdfViewer.Styles, Styles) is false)
        {
            bitPdfViewer.Styles = Styles;

            bitPdfViewer.StyleBuilder.Reset();
        }

        if (TextCoalescing.HasValue && bitPdfViewer.HasNotBeenSet(nameof(TextCoalescing)))
        {
            bitPdfViewer.TextCoalescing = TextCoalescing.Value;
        }

        if (Texts is not null && bitPdfViewer.HasNotBeenSet(nameof(Texts)))
        {
            bitPdfViewer.Texts = Texts;
        }

        if (ToolbarEndTemplate is not null && bitPdfViewer.HasNotBeenSet(nameof(ToolbarEndTemplate)))
        {
            bitPdfViewer.ToolbarEndTemplate = ToolbarEndTemplate;
        }

        if (ToolbarItems.HasValue && bitPdfViewer.HasNotBeenSet(nameof(ToolbarItems)))
        {
            bitPdfViewer.ToolbarItems = ToolbarItems.Value;
        }

        if (ToolbarStartTemplate is not null && bitPdfViewer.HasNotBeenSet(nameof(ToolbarStartTemplate)))
        {
            bitPdfViewer.ToolbarStartTemplate = ToolbarStartTemplate;
        }

        if (Width is not null && bitPdfViewer.HasNotBeenSet(nameof(Width)) && bitPdfViewer.Width != Width)
        {
            bitPdfViewer.Width = Width;

            bitPdfViewer.StyleBuilder.Reset();
        }

        if (ZoomPresets is not null && bitPdfViewer.HasNotBeenSet(nameof(ZoomPresets)))
        {
            bitPdfViewer.ZoomPresets = ZoomPresets;
        }

        if (ZoomStep.HasValue && bitPdfViewer.HasNotBeenSet(nameof(ZoomStep)))
        {
            bitPdfViewer.ZoomStep = ZoomStep.Value;
        }
    }
}
