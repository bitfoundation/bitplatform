using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfViewer;

public partial class BitPdfViewerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDropFile",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a pdf dropped onto the viewer opens in it. The dropped file goes through the same path as the toolbar's open-file button, so OnFileOpened and MaxOpenFileSize apply to it too.",
        },
        new()
        {
            Name = "BackgroundRendering",
            Type = "bool",
            DefaultValue = "false",
            Description = "Offloads document parsing and page rendering to a background thread so scrolling and navigation stay responsive while a complex page renders. Only has an effect when the runtime provides a spare thread (Blazor Server, or a Blazor WebAssembly app built with WasmEnableThreads); on the default single-threaded WebAssembly runtime it is a safe no-op.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPdfViewerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the viewer.",
            LinkType = LinkType.Link,
            Href = "#pdf-class-styles"
        },
        new()
        {
            Name = "CursorTool",
            Type = "BitPdfCursorTool",
            DefaultValue = "BitPdfCursorTool.Select",
            Description = "What dragging on the document surface does: select text (the default) or pan the document, as the hand tool of a desktop viewer does.",
            LinkType = LinkType.Link,
            Href = "#pdf-cursor-tool-enum"
        },
        new()
        {
            Name = "CurrentPage",
            Type = "int",
            DefaultValue = "1",
            Description = "The focused page (1-based), two-way bindable. Reading it gives the page the reader is on; assigning it navigates there. Bound one way (without CurrentPageChanged) the page becomes the host's to control: the viewer then reports navigation through OnPageChanged but does not move the value itself.",
        },
        new()
        {
            Name = "DefaultSidebar",
            Type = "BitPdfSidebar",
            DefaultValue = "BitPdfSidebar.None",
            Description = "The side panel open when a document first loads. The panel can be changed afterwards from the toolbar or through the ShowSidebar method.",
            LinkType = LinkType.Link,
            Href = "#pdf-sidebar-enum"
        },
        new()
        {
            Name = "EnableKeyboardShortcuts",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the viewer handles keyboard shortcuts while it has focus: page navigation (n/j, p/k, PageUp/PageDown, Home/End, plus the arrow keys and Space while one page is shown at a time), zoom (Ctrl +, Ctrl -, Ctrl 0), rotation (r, Shift+r), find (Ctrl+F, Ctrl+G, Shift+Ctrl+G), print (Ctrl+P), download (Ctrl+S), presentation mode (Ctrl+Alt+P) and the sidebar (F4).",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS height of the viewer container. When not set, the viewer height is responsive: capped at 780px and shrinking to fit the viewport on small screens.",
        },
        new()
        {
            Name = "InitialZoomMode",
            Type = "BitPdfZoomMode",
            DefaultValue = "BitPdfZoomMode.FitWidth",
            Description = "The initial zoom behavior.",
            LinkType = LinkType.Link,
            Href = "#pdf-zoom-mode-enum"
        },
        new()
        {
            Name = "MaxOpenFileSize",
            Type = "long",
            DefaultValue = "67108864",
            Description = "The largest file the toolbar's open-file button - and a drop, when AllowDropFile allows one - accepts, in bytes (64 MB by default). The whole file is read into memory, and on Blazor Server it also travels the circuit.",
        },
        new()
        {
            Name = "MaxZoom",
            Type = "double",
            DefaultValue = "8",
            Description = "The largest zoom factor the viewer allows (1 means 100%).",
        },
        new()
        {
            Name = "MaxRenderedPageCount",
            Type = "int",
            DefaultValue = "24",
            Description = "How many pages stay materialized in the DOM at once. Pages outside the window centered on the current one revert to placeholders and are re-rendered when scrolled back to, which is what keeps a long document from growing the DOM (and a Blazor Server circuit's memory) without bound.",
        },
        new()
        {
            Name = "MaxRenderedThumbnailCount",
            Type = "int",
            DefaultValue = "40",
            Description = "How many thumbnails stay materialized in the sidebar at once. A thumbnail fragment is as heavy as a full page, so this bounds the sidebar the way MaxRenderedPageCount bounds the document surface.",
        },
        new()
        {
            Name = "MinZoom",
            Type = "double",
            DefaultValue = "0.1",
            Description = "The smallest zoom factor the viewer allows (1 means 100%).",
        },
        new()
        {
            Name = "OnDocumentLoaded",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when a document has finished loading.",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback for when loading or rendering fails, with the error message.",
        },
        new()
        {
            Name = "OnPageChanged",
            Type = "EventCallback<int>",
            DefaultValue = "",
            Description = "The callback for when the focused page changes (with the 1-based page number).",
        },
        new()
        {
            Name = "OnFileOpened",
            Type = "EventCallback<BitPdfSource>",
            Description = "The callback for when the reader picks a file with the toolbar's open-file button, with the source built from it. Handle it to drive Source yourself; when unset the viewer opens the file on its own.",
            LinkType = LinkType.Link,
            Href = "#pdf-source"
        },
        new()
        {
            Name = "OnPageRendered",
            Type = "EventCallback<int>",
            Description = "The callback for when a page has been rendered into the document surface, with its 1-based page number. Lazy rendering means this is raised as the reader reaches a page, not once per page up front.",
        },
        new()
        {
            Name = "OnPasswordRequested",
            Type = "Func<Task<string?>>?",
            DefaultValue = "null",
            Description = "Invoked when an encrypted document needs a password. Return the password to retry, or null/empty to cancel. When unset, the viewer's own password dialog asks instead (see ShowPasswordPrompt).",
        },
        new()
        {
            Name = "OnProgress",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "The callback for the download progress of a URL source, as a fraction from 0 to 1. Only raised when the server declares a content length; a chunked response has no total to report against.",
        },
        new()
        {
            Name = "OnRotationChanged",
            Type = "EventCallback<int>",
            DefaultValue = "",
            Description = "The callback for when the page rotation changes, with the new angle in degrees (0, 90, 180 or 270).",
        },
        new()
        {
            Name = "OnSidebarChanged",
            Type = "EventCallback<BitPdfSidebar>",
            DefaultValue = "",
            Description = "The callback for when the open side panel changes.",
            LinkType = LinkType.Link,
            Href = "#pdf-sidebar-enum"
        },
        new()
        {
            Name = "OnWarnings",
            Type = "EventCallback<IReadOnlyList<string>>",
            DefaultValue = "",
            Description = "The callback raised after a document loads with any non-fatal diagnostics (e.g. a damaged file whose cross-reference table had to be rebuilt).",
        },
        new()
        {
            Name = "OnZoomChanged",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "The callback for when the zoom factor changes (1 means 100%), whatever caused it: the toolbar, a fit mode, Ctrl+wheel or the public API.",
        },
        new()
        {
            Name = "RespectPermissions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the document's own user access permissions are enforced. With it set, a document that forbids printing or copying has the corresponding toolbar control disabled, Print and Download refuse, and its text cannot be selected. Off by default, as in every browser pdf viewer: the flags are advisory, not a security boundary.",
        },
        new()
        {
            Name = "RenderMode",
            Type = "BitPdfRenderMode",
            DefaultValue = "BitPdfRenderMode.Html",
            Description = "How page content is painted. Canvas replays a display list onto a per-page canvas, while Html (the default) renders prerenderable positioned DOM.",
            LinkType = LinkType.Link,
            Href = "#pdf-render-mode-enum"
        },
        new()
        {
            Name = "ScrollMode",
            Type = "BitPdfScrollMode",
            DefaultValue = "BitPdfScrollMode.Vertical",
            Description = "How the pages are laid out on the scrollable surface: stacked vertically (the default), side by side on one horizontally scrolling row, wrapped into rows that fill the width, or one page (or spread) at a time.",
            LinkType = LinkType.Link,
            Href = "#pdf-scroll-mode-enum"
        },
        new()
        {
            Name = "ShowPasswordPrompt",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the viewer asks for the password of an encrypted document with a dialog of its own. Ignored when OnPasswordRequested is set, which takes over the asking. Set to false to let a password failure surface through OnError instead.",
        },
        new()
        {
            Name = "ShowToolbar",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the toolbar is shown.",
        },
        new()
        {
            Name = "Source",
            Type = "BitPdfSource?",
            DefaultValue = "null",
            Description = "The document to display.",
            LinkType = LinkType.Link,
            Href = "#pdf-source"
        },
        new()
        {
            Name = "SpreadMode",
            Type = "BitPdfSpreadMode",
            DefaultValue = "BitPdfSpreadMode.None",
            Description = "How pages are paired into spreads, the way a printed book falls open.",
            LinkType = LinkType.Link,
            Href = "#pdf-spread-mode-enum"
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width of the viewer container. When not set, the viewer fills the width its host gives it.",
        },
        new()
        {
            Name = "ZoomPresets",
            Type = "IEnumerable<double>?",
            DefaultValue = "null",
            Description = "The explicit zoom factors the toolbar's zoom dropdown offers (1 means 100%). Values outside MinZoom..MaxZoom are dropped. Defaults to 50%, 75%, 100%, 125%, 150%, 200%, 300% and 400%.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPdfViewerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the viewer.",
            LinkType = LinkType.Link,
            Href = "#pdf-class-styles"
        },
        new()
        {
            Name = "TextCoalescing",
            Type = "BitPdfTextCoalescing",
            DefaultValue = "BitPdfTextCoalescing.Exact",
            Description = "How painted text is emitted. Compact merges same-line, same-style runs into one span per visual line (far fewer DOM nodes on per-glyph pdfs).",
            LinkType = LinkType.Link,
            Href = "#pdf-text-coalescing-enum"
        },
        new()
        {
            Name = "Texts",
            Type = "BitPdfViewerTexts?",
            DefaultValue = "null",
            Description = "The texts of the viewer UI. Defaults to English; assign an instance with the properties you want to override to localize the toolbar, the sidebars and the status messages.",
            LinkType = LinkType.Link,
            Href = "#pdf-texts"
        },
        new()
        {
            Name = "ToolbarItems",
            Type = "BitPdfToolbarItems",
            DefaultValue = "BitPdfToolbarItems.All",
            Description = "Which controls the toolbar offers. Combine the flags to build a reduced toolbar (e.g. navigation and zoom only).",
            LinkType = LinkType.Link,
            Href = "#pdf-toolbar-items-enum"
        },
        new()
        {
            Name = "Rotation",
            Type = "int",
            DefaultValue = "0",
            Description = "The rotation applied to every page, in degrees, two-way bindable. Assigned values are normalized to the nearest quarter turn (0, 90, 180 or 270).",
        },
        new()
        {
            Name = "Zoom",
            Type = "double",
            DefaultValue = "1",
            Description = "The zoom factor (1 means 100%), two-way bindable. Assigning it switches the viewer to Custom zoom mode and clamps the value to MinZoom..MaxZoom.",
        },
        new()
        {
            Name = "ZoomStep",
            Type = "double",
            DefaultValue = "1.2",
            Description = "The multiplier applied by ZoomIn and ZoomOut (and by the toolbar's zoom buttons), i.e. 20% a step by default.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Attachments",
            Type = "IReadOnlyList<BitPdfAttachment>",
            Description = "The files embedded in the document (the /EmbeddedFiles name tree plus any /FileAttachment annotation), empty when it carries none.",
            LinkType = LinkType.Link,
            Href = "#pdf-attachment"
        },
        new()
        {
            Name = "CurrentCursorTool",
            Type = "BitPdfCursorTool",
            Description = "What dragging on the document surface currently does.",
        },
        new()
        {
            Name = "CurrentScrollMode",
            Type = "BitPdfScrollMode",
            Description = "The current page layout on the scrollable surface.",
        },
        new()
        {
            Name = "CurrentSpreadMode",
            Type = "BitPdfSpreadMode",
            Description = "The current spread pairing.",
        },
        new()
        {
            Name = "Document",
            Type = "BitPdfDocument?",
            Description = "The parsed document model, or null when nothing is loaded. Exposes the full engine surface (catalog, pages, cross-reference table) for callers that need more than the viewer's own API.",
        },
        new()
        {
            Name = "FailedPages",
            Type = "IReadOnlyList<int>",
            Description = "The 1-based numbers of the pages whose rendering failed, in ascending order. Such a page shows an error note in place of its content and is not retried; a reload, rotation or render-mode change gives every page a fresh attempt.",
        },
        new()
        {
            Name = "FileSize",
            Type = "long",
            Description = "The size in bytes of the loaded document, or 0 when nothing is loaded.",
        },
        new()
        {
            Name = "FormFields",
            Type = "IReadOnlyList<BitPdfFormField>",
            Description = "The interactive form fields (/AcroForm) of the loaded document as a flat list of name/type/value, or an empty list when there is no form.",
        },
        new()
        {
            Name = "HasAttachments",
            Type = "bool",
            Description = "Whether the document carries any embedded file.",
        },
        new()
        {
            Name = "HasLayers",
            Type = "bool",
            Description = "Whether the document declares any optional-content group (layer).",
        },
        new()
        {
            Name = "HasOutline",
            Type = "bool",
            Description = "Whether the document exposes any bookmarks.",
        },
        new()
        {
            Name = "IsEncrypted",
            Type = "bool",
            Description = "Whether the loaded document declares an encryption dictionary.",
        },
        new()
        {
            Name = "IsFullscreen",
            Type = "bool",
            Description = "Whether the viewer currently fills the screen.",
        },
        new()
        {
            Name = "IsPresenting",
            Type = "bool",
            Description = "Whether the viewer is in presentation mode: fullscreen, one page at a time, scaled to fit, with the chrome out of the way.",
        },
        new()
        {
            Name = "IsSearchOpen",
            Type = "bool",
            Description = "Whether the find box is open.",
        },
        new()
        {
            Name = "Layers",
            Type = "IReadOnlyList<BitPdfLayer>",
            Description = "The optional-content groups (layers) the document declares, empty when it declares none.",
            LinkType = LinkType.Link,
            Href = "#pdf-layer"
        },
        new()
        {
            Name = "Metadata",
            Type = "BitPdfMetadata?",
            Description = "The document metadata (/Info fields plus the raw XMP packet), or null when nothing is loaded.",
        },
        new()
        {
            Name = "Outline",
            Type = "IReadOnlyList<BitPdfOutlineItem>",
            Description = "The document outline (bookmarks) as a tree, empty when the document has none.",
        },
        new()
        {
            Name = "PageCount",
            Type = "int",
            Description = "The number of pages of the current document.",
        },
        new()
        {
            Name = "PageLabels",
            Type = "IReadOnlyList<string>",
            Description = "The document-defined page labels (e.g. \"i\", \"ii\", \"1\", \"A-1\"), one per page in document order, or an empty list when nothing is loaded.",
        },
        new()
        {
            Name = "PdfVersion",
            Type = "string?",
            Description = "The pdf version the loaded document declares (e.g. \"1.7\"), or null.",
        },
        new()
        {
            Name = "Permissions",
            Type = "BitPdfPermissions",
            Description = "The user access permissions of the loaded document. Every permission is granted for an unencrypted document, and for no document at all.",
        },
        new()
        {
            Name = "SearchHighlightAll",
            Type = "bool",
            Description = "Whether the find box paints every match, not just the current one.",
        },
        new()
        {
            Name = "SearchMatchCase",
            Type = "bool",
            Description = "Whether the find box compares case-sensitively.",
        },
        new()
        {
            Name = "SearchMatchDiacritics",
            Type = "bool",
            Description = "Whether the find box tells an accented letter apart from its bare form.",
        },
        new()
        {
            Name = "SearchMatchIndex",
            Type = "int",
            Description = "The 1-based ordinal of the find match the reader is on, or 0 when there is no match.",
        },
        new()
        {
            Name = "SearchQuery",
            Type = "string",
            Description = "The current find query, or an empty string when nothing is being searched for.",
        },
        new()
        {
            Name = "SearchWholeWord",
            Type = "bool",
            Description = "Whether the find box matches whole words only.",
        },
        new()
        {
            Name = "SearchMatchCount",
            Type = "int",
            Description = "The number of matches of the current find query (0 when there is no query or no match, -1 when the browser cannot highlight matches).",
        },
        new()
        {
            Name = "Sidebar",
            Type = "BitPdfSidebar",
            Description = "Which side panel is currently open.",
        },
        new()
        {
            Name = "StructureTree",
            Type = "IReadOnlyList<BitPdfStructElement>",
            Description = "The tagged-pdf logical structure tree of the loaded document, or an empty list when the document is untagged.",
        },
        new()
        {
            Name = "Warnings",
            Type = "IReadOnlyList<string>",
            Description = "The non-fatal diagnostics collected while the current document was parsed (e.g. a damaged cross-reference table that had to be rebuilt), empty when there were none or nothing is loaded.",
        },
        new()
        {
            Name = "ZoomMode",
            Type = "BitPdfZoomMode",
            Description = "The current zoom behavior (fit-width, fit-page, actual size or custom).",
        },
        new()
        {
            Name = "ClearSearch",
            Type = "Task ClearSearch()",
            Description = "Clears the current find query and its highlights.",
        },
        new()
        {
            Name = "DownloadAttachment",
            Type = "Task DownloadAttachment(BitPdfAttachment attachment)",
            Description = "Saves an embedded file to the reader's machine.",
        },
        new()
        {
            Name = "Download",
            Type = "Task Download()",
            Description = "Downloads the original document bytes. Works for URL sources too: the bytes fetched for the current document are reused, so nothing is downloaded twice.",
        },
        new()
        {
            Name = "ClearSelection",
            Type = "Task ClearSelection()",
            Description = "Drops the reader's selection inside the document. A selection made elsewhere on the hosting page is left alone.",
        },
        new()
        {
            Name = "GetSelectedText",
            Type = "Task<string> GetSelectedText()",
            Description = "The text the reader currently has selected in the document, or an empty string when nothing inside the viewer is selected. Reads the live DOM selection over the page's text layer, so it is the words the reader sees, in reading order.",
        },
        new()
        {
            Name = "ExtractPageText",
            Type = "string ExtractPageText(int pageNumber)",
            Description = "Extracts the visible text of a single page (1-based) for search or copy, or an empty string when unavailable.",
        },
        new()
        {
            Name = "ExtractText",
            Type = "string ExtractText(string pageSeparator = \"\\n\\n\")",
            Description = "Extracts the visible text of the whole document, one page per entry joined by the separator. Reuses the index the find box builds.",
        },
        new()
        {
            Name = "IsLayerVisible",
            Type = "bool IsLayerVisible(BitPdfLayer layer)",
            Description = "Whether the layer is currently painted.",
        },
        new()
        {
            Name = "SetLayerVisible",
            Type = "Task SetLayerVisible(BitPdfLayer layer, bool visible)",
            Description = "Shows or hides an optional-content group and re-renders the pages, the way a desktop viewer's layers panel does.",
        },
        new()
        {
            Name = "FindNext",
            Type = "Task FindNext()",
            Description = "Moves to the next find match, wrapping around at the end.",
        },
        new()
        {
            Name = "FindPrevious",
            Type = "Task FindPrevious()",
            Description = "Moves to the previous find match, wrapping around at the start.",
        },
        new()
        {
            Name = "FirstPage",
            Type = "Task FirstPage()",
            Description = "Navigates to the first page.",
        },
        new()
        {
            Name = "GetBytes",
            Type = "byte[]? GetBytes()",
            Description = "The raw bytes of the loaded document (fetched ones included), or null when nothing is loaded.",
        },
        new()
        {
            Name = "GoToDestination",
            Type = "Task GoToDestination(BitPdfDestination? destination)",
            Description = "Navigates to a destination: its page, and - when the destination names a vertical position - that position within the page. The in-page offset is applied only while the pages are unrotated.",
        },
        new()
        {
            Name = "GoToNamedDestination",
            Type = "Task GoToNamedDestination(string name)",
            Description = "Navigates to a named destination (the /Dests entry a link or an external anchor refers to). Does nothing when the document does not declare it.",
        },
        new()
        {
            Name = "GoToPage",
            Type = "Task GoToPage(int pageNumber)",
            Description = "Navigates to the provided page number (1-based).",
        },
        new()
        {
            Name = "LastPage",
            Type = "Task LastPage()",
            Description = "Navigates to the last page.",
        },
        new()
        {
            Name = "NextPage",
            Type = "Task NextPage()",
            Description = "Navigates to the next page.",
        },
        new()
        {
            Name = "PrevPage",
            Type = "Task PrevPage()",
            Description = "Navigates to the previous page.",
        },
        new()
        {
            Name = "OpenAsync",
            Type = "Task OpenAsync(BitPdfSource? source)",
            Description = "Loads a document without going through the Source parameter. Passing null closes the current one. The Source parameter still wins: a later host render that changes it replaces whatever was opened this way.",
            LinkType = LinkType.Link,
            Href = "#pdf-source"
        },
        new()
        {
            Name = "PrintCurrentPage",
            Type = "Task PrintCurrentPage()",
            Description = "Opens the browser print dialog with just the page the reader is on.",
        },
        new()
        {
            Name = "Print",
            Type = "Task Print()",
            Description = "Opens the browser print dialog with all pages of the document. An overload takes a page range: Print(int from, int to).",
        },
        new()
        {
            Name = "RenderPageHtml",
            Type = "string RenderPageHtml(int pageNumber)",
            Description = "Renders a single page (1-based) to self-contained HTML, or an empty string when no document is loaded or the number is out of range.",
        },
        new()
        {
            Name = "RotateClockwise",
            Type = "Task RotateClockwise()",
            Description = "Rotates all pages 90 degrees clockwise.",
        },
        new()
        {
            Name = "RotateCounterClockwise",
            Type = "Task RotateCounterClockwise()",
            Description = "Rotates all pages 90 degrees counter-clockwise.",
        },
        new()
        {
            Name = "Search",
            Type = "Task Search(string? query)",
            Description = "Opens the find box (when it is closed) and searches the document for the query. An empty query just clears the current matches.",
        },
        new()
        {
            Name = "SetSearchOptions",
            Type = "Task SetSearchOptions(bool? matchCase, bool? wholeWord, bool? matchDiacritics, bool? highlightAll)",
            Description = "Sets the find options and re-runs the current query against them. A null leaves that option as it is.",
        },
        new()
        {
            Name = "SetRotation",
            Type = "Task SetRotation(int degrees)",
            Description = "Rotates all pages to the given absolute angle in degrees, normalized to the nearest quarter turn.",
        },
        new()
        {
            Name = "SetCursorTool",
            Type = "void SetCursorTool(BitPdfCursorTool tool)",
            Description = "Changes what dragging on the document surface does.",
        },
        new()
        {
            Name = "SetScrollMode",
            Type = "Task SetScrollMode(BitPdfScrollMode mode)",
            Description = "Changes how the pages are laid out on the scrollable surface.",
        },
        new()
        {
            Name = "SetSpreadMode",
            Type = "Task SetSpreadMode(BitPdfSpreadMode mode)",
            Description = "Changes how pages are paired into spreads.",
        },
        new()
        {
            Name = "SetZoom",
            Type = "Task SetZoom(double zoom)",
            Description = "Sets an explicit zoom factor (1 means 100%), switching the viewer to Custom zoom mode. The value is clamped to MinZoom..MaxZoom.",
        },
        new()
        {
            Name = "SetZoomMode",
            Type = "Task SetZoomMode(BitPdfZoomMode mode)",
            Description = "Sets the zoom mode (fit-width, fit-page, actual size or custom).",
        },
        new()
        {
            Name = "ShowSidebar",
            Type = "Task ShowSidebar(BitPdfSidebar sidebar)",
            Description = "Opens the given side panel, or closes the open one when None is passed.",
        },
        new()
        {
            Name = "EnterPresentationMode",
            Type = "Task EnterPresentationMode()",
            Description = "Shows the document fullscreen, one page at a time, scaled to fit the screen.",
        },
        new()
        {
            Name = "ExitPresentationMode",
            Type = "Task ExitPresentationMode()",
            Description = "Leaves presentation mode and restores the layout it replaced.",
        },
        new()
        {
            Name = "TogglePresentationMode",
            Type = "Task TogglePresentationMode()",
            Description = "Enters or leaves presentation mode. Entering remembers the layout the reader had, so leaving - however it happens, including the browser's own Escape - puts it back.",
        },
        new()
        {
            Name = "ToggleFullscreen",
            Type = "Task ToggleFullscreen()",
            Description = "Toggles the fullscreen mode of the viewer.",
        },
        new()
        {
            Name = "ToggleProperties",
            Type = "void ToggleProperties()",
            Description = "Opens or closes the document-properties dialog.",
        },
        new()
        {
            Name = "ZoomIn",
            Type = "Task ZoomIn()",
            Description = "Zooms in by one ZoomStep (20% by default).",
        },
        new()
        {
            Name = "ZoomOut",
            Type = "Task ZoomOut()",
            Description = "Zooms out by one ZoomStep (20% by default).",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pdf-source",
            Title = "BitPdfSource",
            Description = "Identifies where a pdf document is loaded from. A source is either a byte buffer already in memory (BitPdfSource.FromBytes), or a URL the document can be fetched from (BitPdfSource.FromUrl).",
            Parameters =
            [
                new()
                {
                    Name = "Bytes",
                    Type = "byte[]?",
                    DefaultValue = "null",
                    Description = "Raw document bytes, when the source is an in-memory buffer.",
                },
                new()
                {
                    Name = "Url",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The URL to fetch the document from, when the source is remote.",
                },
                new()
                {
                    Name = "FileName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An optional display name (e.g. the original file name).",
                },
                new()
                {
                    Name = "Password",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The password to open an encrypted document, if known up front (also see the WithPassword method).",
                },
                new()
                {
                    Name = "Headers",
                    Type = "IReadOnlyDictionary<string, string>?",
                    DefaultValue = "null",
                    Description = "Extra HTTP request headers sent when the document is fetched from Url - an Authorization header, for instance. Ignored for an in-memory source (also see the WithHeaders method).",
                },
                new()
                {
                    Name = "IsBytes",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True when this source carries an in-memory byte buffer.",
                },
                new()
                {
                    Name = "FromStreamAsync",
                    Type = "static Task<BitPdfSource> FromStreamAsync(Stream stream, string? fileName = null, CancellationToken cancellationToken = default)",
                    DefaultValue = "",
                    Description = "Reads the stream to the end and creates an in-memory source from it - the shape an upload, a database blob or an embedded resource arrives in. The stream is read, not owned: the caller still disposes it.",
                },
                new()
                {
                    Name = "FromBase64",
                    Type = "static BitPdfSource FromBase64(string base64, string? fileName = null)",
                    DefaultValue = "",
                    Description = "Creates an in-memory source from a base64-encoded document - the shape a document arrives in from a JSON API, a data URI or a database text column. A data:application/pdf;base64, prefix is accepted and stripped.",
                },
            ]
        },
        new()
        {
            Id = "pdf-attachment",
            Title = "BitPdfAttachment",
            Description = "A file embedded in the document, either document-wide through the catalog's /Names /EmbeddedFiles name tree or pinned to a page through a /FileAttachment annotation.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The file name the document gives the attachment.",
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The description (/Desc) the document gives it, when present.",
                },
                new()
                {
                    Name = "MimeType",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The declared MIME type of the embedded stream, when present.",
                },
                new()
                {
                    Name = "PageNumber",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The page the attachment is pinned to (1-based), or null for a document-wide attachment.",
                },
                new()
                {
                    Name = "Content",
                    Type = "byte[]",
                    DefaultValue = "",
                    Description = "The decoded bytes of the attachment. Empty when the embedded stream could not be decoded.",
                },
                new()
                {
                    Name = "Size",
                    Type = "long",
                    DefaultValue = "0",
                    Description = "The size in bytes of Content.",
                },
            ]
        },
        new()
        {
            Id = "pdf-class-styles",
            Title = "BitPdfViewerClassStyles",
            Description = "Custom CSS classes/styles for the different parts of the BitPdfViewer.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitPdfViewer.",
                },
                new()
                {
                    Name = "ProgressBar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the indeterminate loading bar of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Toolbar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toolbar of the BitPdfViewer.",
                },
                new()
                {
                    Name = "ToolbarButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toolbar buttons of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the document title shown in the toolbar of the BitPdfViewer.",
                },
                new()
                {
                    Name = "SearchBox",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the find box of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body (sidebar plus surface) of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Thumbnails",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the thumbnails sidebar of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Thumbnail",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each thumbnail of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Outline",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the bookmarks sidebar of the BitPdfViewer.",
                },
                new()
                {
                    Name = "OutlineItem",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each bookmark of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Layers",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the layers sidebar of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Layer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each layer row of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Surface",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the scrollable document surface of the BitPdfViewer.",
                },
                new()
                {
                    Name = "Page",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each rendered page of the BitPdfViewer.",
                },
                new()
                {
                    Name = "PropertiesDialog",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the document properties dialog of the BitPdfViewer.",
                },
            ]
        },
        new()
        {
            Id = "pdf-layer",
            Title = "BitPdfLayer",
            Description = "An optional-content group (a layer) declared by the document - the switchable content a CAD drawing, a map overlay, multilingual artwork or a watermark ships as.",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identity of the group: a stable key derived from the indirect reference that names it. Content marked with this group is what the layer switches.",
                },
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The group's display name (/Name).",
                },
                new()
                {
                    Name = "VisibleByDefault",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the document's default configuration shows the layer. The viewer starts from this and the reader takes it from there.",
                },
            ]
        },
        new()
        {
            Id = "pdf-texts",
            Title = "BitPdfViewerTexts",
            Description = "The texts of the BitPdfViewer UI. All strings default to English; override individual properties to localize the viewer. The table lists the groups; every property is a plain settable string.",
            Parameters =
            [
                new()
                {
                    Name = "ToolbarAriaLabel, LoadingAriaLabel",
                    Type = "string",
                    DefaultValue = "\"PDF viewer toolbar\", \"Loading document\"",
                    Description = "The accessible names of the toolbar and of the indeterminate loading bar.",
                },
                new()
                {
                    Name = "Thumbnails, Bookmarks, Attachments, Layers",
                    Type = "string",
                    DefaultValue = "\"Page thumbnails\", \"Bookmarks\", \"Attachments\"",
                    Description = "The labels of the side panel toggles.",
                },
                new()
                {
                    Name = "FirstPage, PreviousPage, NextPage, LastPage, PageNumber, PageLabel",
                    Type = "string",
                    DefaultValue = "\"First page\", \"Previous page\", ...",
                    Description = "The labels of the page navigation group.",
                },
                new()
                {
                    Name = "ZoomIn, ZoomOut, ZoomLevel, FitWidth, FitPage, ActualSize",
                    Type = "string",
                    DefaultValue = "\"Zoom in\", \"Zoom out\", ...",
                    Description = "The labels of the zoom group and of the zoom dropdown options.",
                },
                new()
                {
                    Name = "Find, FindPlaceholder, PreviousMatch, NextMatch, MatchCase, WholeWord, MatchCountFormat",
                    Type = "string",
                    DefaultValue = "\"Find in document\", ..., \"{0}/{1}\"",
                    Description = "The labels of the find box. MatchCountFormat receives the current match and the total.",
                },
                new()
                {
                    Name = "ScrollMode, ScrollVertical, ScrollHorizontal, ScrollWrapped, ScrollPage, SpreadMode, SpreadNone, SpreadOdd, SpreadEven, PanTool",
                    Type = "string",
                    DefaultValue = "\"Scroll mode\", \"Vertical scrolling\", ...",
                    Description = "The labels of the page-layout dropdowns and of the pan (hand) tool toggle.",
                },
                new()
                {
                    Name = "RotateClockwise, RotateCounterClockwise, Download, Print, Fullscreen, Presentation, Properties, Close",
                    Type = "string",
                    DefaultValue = "\"Rotate clockwise\", ...",
                    Description = "The labels of the remaining toolbar actions.",
                },
                new()
                {
                    Name = "NoDocument, PreparingPrint, PrintAborted, HttpClientRequired, FetchFailedFormat, ErrorFormat, PageCountFormat",
                    Type = "string",
                    DefaultValue = "\"No document loaded.\", ...",
                    Description = "The status messages shown on the surface. The *Format strings receive the underlying error or the page count.",
                },
                new()
                {
                    Name = "PasswordTitle, PasswordPrompt, PasswordRejected, PasswordSubmit, Cancel",
                    Type = "string",
                    DefaultValue = "\"Password required\", \"This document is protected...\", ...",
                    Description = "The texts of the built-in password dialog.",
                },
                new()
                {
                    Name = "PropertyFileName, PropertyFileSize, PropertyTitle, PropertyAuthor, PropertySubject, PropertyKeywords, PropertyCreationDate, PropertyModificationDate, PropertyCreator, PropertyProducer, PropertyVersion, PropertyPageCount, PropertyPageSize, PropertyUnknown",
                    Type = "string",
                    DefaultValue = "\"File name\", \"File size\", ...",
                    Description = "The row labels of the document properties dialog, and the placeholder shown for a property the document does not declare.",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "pdf-zoom-mode-enum",
            Name = "BitPdfZoomMode",
            Description = "How the viewer scales pages to the available space.",
            Items =
            [
                new()
                {
                    Name = "Custom",
                    Value = "0",
                    Description = "An explicit zoom factor is applied (the user picked a percentage).",
                },
                new()
                {
                    Name = "FitWidth",
                    Value = "1",
                    Description = "Each page is scaled so its width fills the viewport.",
                },
                new()
                {
                    Name = "FitPage",
                    Value = "2",
                    Description = "Each page is scaled so the whole page fits in the viewport.",
                },
                new()
                {
                    Name = "ActualSize",
                    Value = "3",
                    Description = "Pages are shown at their natural size (one CSS pixel per point).",
                },
                new()
                {
                    Name = "FitHeight",
                    Value = "4",
                    Description = "Each page is scaled so its height fills the viewport, letting a wide page overflow horizontally rather than shrinking it to fit.",
                },
                new()
                {
                    Name = "Automatic",
                    Value = "5",
                    Description = "Fit-width, but never magnified past 125% - the behavior a desktop viewer calls \"automatic zoom\", which keeps a narrow page readable without blowing it up to fill a wide screen.",
                },
            ]
        },
        new()
        {
            Id = "pdf-render-mode-enum",
            Name = "BitPdfRenderMode",
            Description = "How page content is rendered.",
            Items =
            [
                new()
                {
                    Name = "Html",
                    Value = "0",
                    Description = "Pages render to positioned HTML/CSS DOM (the default). Fully prerenderable and crisp at any zoom.",
                },
                new()
                {
                    Name = "Canvas",
                    Value = "1",
                    Description = "Page content is painted onto a per-page canvas by replaying a display list produced by the C# engine. Far fewer DOM nodes; selection, search and links still work through the DOM text layer, and zoom changes re-rasterize the canvases so text stays crisp. Requires JavaScript, so no prerender.",
                },
            ]
        },
        new()
        {
            Id = "pdf-text-coalescing-enum",
            Name = "BitPdfTextCoalescing",
            Description = "How painted text runs are emitted into the page HTML.",
            Items =
            [
                new()
                {
                    Name = "Exact",
                    Value = "0",
                    Description = "Every show-text run keeps its own positioned span, so each glyph run lands at its exact pdf-computed position. Highest fidelity, but per-glyph pdfs emit one span per character.",
                },
                new()
                {
                    Name = "Compact",
                    Value = "1",
                    Description = "Adjacent runs on the same baseline with identical style are merged into one span per visual line. Dramatically fewer DOM nodes on per-glyph pdfs, at the cost of small intra-line position drift. Rotated text is never coalesced and stays exact.",
                },
            ]
        },
        new()
        {
            Id = "pdf-sidebar-enum",
            Name = "BitPdfSidebar",
            Description = "Which side panel of the viewer is open.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "No side panel is open; the document surface fills the viewer.",
                },
                new()
                {
                    Name = "Thumbnails",
                    Value = "1",
                    Description = "The page-thumbnails panel is open.",
                },
                new()
                {
                    Name = "Bookmarks",
                    Value = "2",
                    Description = "The bookmarks (document outline) panel is open.",
                },
                new()
                {
                    Name = "Attachments",
                    Value = "3",
                    Description = "The embedded-files (attachments) panel is open.",
                },
                new()
                {
                    Name = "Layers",
                    Value = "4",
                    Description = "The optional-content (layers) panel is open.",
                },
            ]
        },
        new()
        {
            Id = "pdf-scroll-mode-enum",
            Name = "BitPdfScrollMode",
            Description = "How the pages of a document are laid out on the scrollable surface.",
            Items =
            [
                new()
                {
                    Name = "Vertical",
                    Value = "0",
                    Description = "Pages are stacked top to bottom and the surface scrolls vertically (the default).",
                },
                new()
                {
                    Name = "Horizontal",
                    Value = "1",
                    Description = "Pages are placed side by side on one row and the surface scrolls horizontally.",
                },
                new()
                {
                    Name = "Wrapped",
                    Value = "2",
                    Description = "Pages flow left to right and wrap onto the next row, filling the width of the surface.",
                },
                new()
                {
                    Name = "Page",
                    Value = "3",
                    Description = "Only the current page (or spread) is shown; navigation replaces it rather than scrolling to it.",
                },
            ]
        },
        new()
        {
            Id = "pdf-spread-mode-enum",
            Name = "BitPdfSpreadMode",
            Description = "How pages are paired into spreads, the way a printed book falls open.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "No pairing: one page per row (the default).",
                },
                new()
                {
                    Name = "Odd",
                    Value = "1",
                    Description = "Odd-numbered pages start a spread, pairing 1-2, 3-4 and so on.",
                },
                new()
                {
                    Name = "Even",
                    Value = "2",
                    Description = "Even-numbered pages start a spread, so page 1 stands alone and 2-3, 4-5 and so on are paired.",
                },
            ]
        },
        new()
        {
            Id = "pdf-cursor-tool-enum",
            Name = "BitPdfCursorTool",
            Description = "What dragging on the document surface does.",
            Items =
            [
                new()
                {
                    Name = "Select",
                    Value = "0",
                    Description = "Dragging selects text (the default).",
                },
                new()
                {
                    Name = "Pan",
                    Value = "1",
                    Description = "Dragging pans the document, as the hand tool of a desktop viewer does.",
                },
            ]
        },
        new()
        {
            Id = "pdf-toolbar-items-enum",
            Name = "BitPdfToolbarItems",
            Description = "The individually toggleable groups of controls in the viewer toolbar. This is a [Flags] enum, so the members combine with the | operator.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "No toolbar control at all (the toolbar bar itself still renders).",
                },
                new()
                {
                    Name = "Thumbnails",
                    Value = "1",
                    Description = "The page-thumbnails sidebar toggle.",
                },
                new()
                {
                    Name = "Bookmarks",
                    Value = "2",
                    Description = "The bookmarks (document outline) sidebar toggle.",
                },
                new()
                {
                    Name = "Navigation",
                    Value = "4",
                    Description = "The previous/next page buttons and the page-number box.",
                },
                new()
                {
                    Name = "FirstLastPage",
                    Value = "8",
                    Description = "The first/last page buttons.",
                },
                new()
                {
                    Name = "Zoom",
                    Value = "16",
                    Description = "The zoom in/out buttons and the zoom-level dropdown.",
                },
                new()
                {
                    Name = "Title",
                    Value = "32",
                    Description = "The document title.",
                },
                new()
                {
                    Name = "Search",
                    Value = "64",
                    Description = "The find-in-document control.",
                },
                new()
                {
                    Name = "Rotate",
                    Value = "128",
                    Description = "The rotate clockwise/counter-clockwise buttons.",
                },
                new()
                {
                    Name = "Download",
                    Value = "256",
                    Description = "The download button.",
                },
                new()
                {
                    Name = "Print",
                    Value = "512",
                    Description = "The print button.",
                },
                new()
                {
                    Name = "Fullscreen",
                    Value = "1024",
                    Description = "The fullscreen toggle.",
                },
                new()
                {
                    Name = "Properties",
                    Value = "2048",
                    Description = "The document-properties button and its dialog.",
                },
                new()
                {
                    Name = "Layout",
                    Value = "4096",
                    Description = "The scroll-mode and spread-mode dropdowns.",
                },
                new()
                {
                    Name = "CursorTool",
                    Value = "8192",
                    Description = "The pan (hand) tool toggle.",
                },
                new()
                {
                    Name = "Attachments",
                    Value = "16384",
                    Description = "The embedded-files (attachments) sidebar toggle.",
                },
                new()
                {
                    Name = "Presentation",
                    Value = "32768",
                    Description = "The presentation-mode toggle.",
                },
                new()
                {
                    Name = "Layers",
                    Value = "65536",
                    Description = "The optional-content (layers) sidebar toggle.",
                },
                new()
                {
                    Name = "OpenFile",
                    Value = "131072",
                    Description = "The button that opens a pdf file from the reader's own machine.",
                },
                new()
                {
                    Name = "All",
                    Value = "131071",
                    Description = "Every toolbar control (the default).",
                },
            ]
        },
    ];



    private BitPdfSource basicSource = BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/sample-pdf.pdf", "sample.pdf");

    // The remaining examples load on demand through their "Load document" buttons:
    // parsing a document blocks the single WASM thread, so loading every viewer on
    // this page at once would freeze it for several seconds at startup.
    private BitPdfSource? heightSource;
    private BitPdfSource? toolbarSource;
    private BitPdfSource? sidebarSource;
    private BitPdfSource? zoomSource;
    private BitPdfSource? layoutSource;
    private BitPdfSource? searchSource;
    private BitPdfSource? keyboardSource;
    private BitPdfSource? canvasSource;
    private BitPdfSource? coalescingSource;
    private BitPdfSource? infoSource;
    private BitPdfSource? passwordSource;
    private BitPdfSource? eventsSource;
    private BitPdfSource? bindingSource;
    private BitPdfSource? publicApiSource;
    private BitPdfSource? localizedSource;
    private BitPdfSource? printSource;
    private BitPdfSource? a11ySource;
    private BitPdfSource? styleSource;
    private BitPdfSource? rtlSource;

    private string searchTerm = "the";
    private string? passwordError;
    private int boundPage = 1;
    private double boundZoom = 1;
    private int boundRotation;
    private bool panTool;
    private BitPdfScrollMode scrollMode = BitPdfScrollMode.Vertical;
    private BitPdfSpreadMode spreadMode = BitPdfSpreadMode.None;
    private BitPdfRenderMode renderMode = BitPdfRenderMode.Html;
    private BitPdfTextCoalescing textCoalescing = BitPdfTextCoalescing.Exact;

    private readonly List<string> eventsLog = [];

    private BitPdfViewer pdfViewerRef = default!;
    private BitPdfViewer searchViewerRef = default!;
    private BitPdfViewer printViewerRef = default!;
    private BitPdfViewer? infoViewerRef;

    // The zoom dropdown's percentages, replacing the defaults for the zoom example.
    private readonly double[] zoomPresets = [0.5, 1, 1.5, 2];

    private string? selectedText;

    /// <summary>Reads back what the reader has highlighted in the document, which is
    /// the starting point of any "quote this" or "look this up" action.</summary>
    private async Task ShowSelectedText()
    {
        selectedText = await pdfViewerRef.GetSelectedText();
    }

    /// <summary>The destination of the document's first bookmark, which is what the
    /// public-API example navigates to - a destination lands on the exact spot the
    /// bookmark points at, not just the top of its page.</summary>
    private BitPdfDestination? FirstBookmarkDestination()
        => pdfViewerRef?.Outline.FirstOrDefault()?.Destination;

    private readonly BitPdfViewerTexts persianTexts = new()
    {
        Thumbnails = "بندانگشتی صفحات",
        Bookmarks = "نشانک‌ها",
        FirstPage = "صفحه اول",
        PreviousPage = "صفحه قبل",
        NextPage = "صفحه بعد",
        LastPage = "صفحه آخر",
        PageNumber = "شماره صفحه",
        ZoomIn = "بزرگ‌نمایی",
        ZoomOut = "کوچک‌نمایی",
        ZoomLevel = "میزان بزرگ‌نمایی",
        FitWidth = "اندازه عرض",
        FitPage = "اندازه صفحه",
        FitHeight = "اندازه ارتفاع",
        Automatic = "بزرگ‌نمایی خودکار",
        ActualSize = "اندازه واقعی",
        Find = "جستجو در سند",
        FindPlaceholder = "جستجو در سند",
        PreviousMatch = "مورد قبلی",
        NextMatch = "مورد بعدی",
        MatchCase = "حساس به حروف",
        WholeWord = "کلمه کامل",
        MatchDiacritics = "حساس به اعراب",
        HighlightAll = "برجسته‌سازی همه",
        PhraseNotFound = "موردی یافت نشد",
        RotateClockwise = "چرخش ساعتگرد",
        RotateCounterClockwise = "چرخش پادساعتگرد",
        Download = "دانلود سند",
        Print = "چاپ سند",
        Fullscreen = "تمام صفحه",
        ExitFullscreen = "خروج از تمام صفحه",
        OpenFile = "باز کردن فایل",
        Properties = "مشخصات سند",
        Close = "بستن",
        NoDocument = "سندی بارگذاری نشده است.",
        PageCountFormat = "{0} صفحه",
    };

    private static BitPdfSource CreateSampleSource()
        => BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/sample-pdf.pdf", "sample.pdf");

    private static BitPdfSource CreateArticleSource()
        => BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf", "article.pdf");

    private async Task OnBasicFileChange(InputFileChangeEventArgs e)
    {
        basicSource = await ReadFileSource(e) ?? basicSource;
    }

    private async Task OnCanvasFileChange(InputFileChangeEventArgs e)
    {
        canvasSource = await ReadFileSource(e) ?? canvasSource;
    }

    private async Task OnPasswordFileChange(InputFileChangeEventArgs e)
    {
        passwordError = null;
        passwordSource = await ReadFileSource(e) ?? passwordSource;
    }

    private static async Task<BitPdfSource?> ReadFileSource(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0) return null;

        const long maxSize = 512 * 1024 * 1024;
        if (e.File.Size <= 0 || e.File.Size > maxSize) return null; // reject empty/oversized files before buffering

        using var stream = e.File.OpenReadStream(maxAllowedSize: maxSize);
        var bytes = new byte[(int)e.File.Size];
        await stream.ReadExactlyAsync(bytes);

        return BitPdfSource.FromBytes(bytes, e.File.Name);
    }
}
