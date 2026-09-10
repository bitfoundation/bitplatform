namespace Bit.BlazorUI;

/// <summary>
/// The texts of the <see cref="BitPdfViewer"/> UI. All strings default to English;
/// override individual properties to localize the viewer.
/// </summary>
public class BitPdfViewerTexts
{
    /// <summary>The accessible name of the toolbar.</summary>
    public string ToolbarAriaLabel { get; set; } = "PDF viewer toolbar";

    /// <summary>The accessible name of the indeterminate loading bar.</summary>
    public string LoadingAriaLabel { get; set; } = "Loading document";

    /// <summary>The label of the page-thumbnails toggle.</summary>
    public string Thumbnails { get; set; } = "Page thumbnails";

    /// <summary>The label of the bookmarks (outline) toggle.</summary>
    public string Bookmarks { get; set; } = "Bookmarks";

    /// <summary>The label of the attachments toggle.</summary>
    public string Attachments { get; set; } = "Attachments";

    /// <summary>The label of the optional-content (layers) toggle.</summary>
    public string Layers { get; set; } = "Layers";

    /// <summary>The label of the first-page button.</summary>
    public string FirstPage { get; set; } = "First page";

    /// <summary>The label of the previous-page button.</summary>
    public string PreviousPage { get; set; } = "Previous page";

    /// <summary>The label of the next-page button.</summary>
    public string NextPage { get; set; } = "Next page";

    /// <summary>The label of the last-page button.</summary>
    public string LastPage { get; set; } = "Last page";

    /// <summary>The accessible name of the page-number box.</summary>
    public string PageNumber { get; set; } = "Page number";

    /// <summary>The accessible name of the page-label indicator.</summary>
    public string PageLabel { get; set; } = "Page label";

    /// <summary>The label of the zoom-out button.</summary>
    public string ZoomOut { get; set; } = "Zoom out";

    /// <summary>The label of the zoom-in button.</summary>
    public string ZoomIn { get; set; } = "Zoom in";

    /// <summary>The accessible name of the zoom-level dropdown.</summary>
    public string ZoomLevel { get; set; } = "Zoom level";

    /// <summary>The fit-width option of the zoom dropdown.</summary>
    public string FitWidth { get; set; } = "Fit width";

    /// <summary>The fit-page option of the zoom dropdown.</summary>
    public string FitPage { get; set; } = "Fit page";

    /// <summary>The actual-size (100%) option of the zoom dropdown.</summary>
    public string ActualSize { get; set; } = "Actual size";

    /// <summary>The accessible name of the scroll-mode dropdown.</summary>
    public string ScrollMode { get; set; } = "Scroll mode";

    /// <summary>The vertical-scrolling option.</summary>
    public string ScrollVertical { get; set; } = "Vertical scrolling";

    /// <summary>The horizontal-scrolling option.</summary>
    public string ScrollHorizontal { get; set; } = "Horizontal scrolling";

    /// <summary>The wrapped-scrolling option.</summary>
    public string ScrollWrapped { get; set; } = "Wrapped scrolling";

    /// <summary>The single-page (no scrolling) option.</summary>
    public string ScrollPage { get; set; } = "Page scrolling";

    /// <summary>The accessible name of the spread-mode dropdown.</summary>
    public string SpreadMode { get; set; } = "Spread mode";

    /// <summary>The no-spreads option.</summary>
    public string SpreadNone { get; set; } = "No spreads";

    /// <summary>The odd-spreads option.</summary>
    public string SpreadOdd { get; set; } = "Odd spreads";

    /// <summary>The even-spreads option.</summary>
    public string SpreadEven { get; set; } = "Even spreads";

    /// <summary>The label of the pan (hand) tool toggle.</summary>
    public string PanTool { get; set; } = "Pan tool";

    /// <summary>The label of the find toggle.</summary>
    public string Find { get; set; } = "Find in document";

    /// <summary>The placeholder of the find box.</summary>
    public string FindPlaceholder { get; set; } = "Find in document";

    /// <summary>The label of the previous-match button.</summary>
    public string PreviousMatch { get; set; } = "Previous match";

    /// <summary>The label of the next-match button.</summary>
    public string NextMatch { get; set; } = "Next match";

    /// <summary>The label of the match-case find option.</summary>
    public string MatchCase { get; set; } = "Match case";

    /// <summary>The label of the whole-word find option.</summary>
    public string WholeWord { get; set; } = "Whole words";

    /// <summary>The match counter format ({0} = the current match, {1} = the total).</summary>
    public string MatchCountFormat { get; set; } = "{0}/{1}";

    /// <summary>The label of the rotate-clockwise button.</summary>
    public string RotateClockwise { get; set; } = "Rotate clockwise";

    /// <summary>The label of the rotate-counter-clockwise button.</summary>
    public string RotateCounterClockwise { get; set; } = "Rotate counter clockwise";

    /// <summary>The label of the download button.</summary>
    public string Download { get; set; } = "Download document";

    /// <summary>The label of the print button.</summary>
    public string Print { get; set; } = "Print document";

    /// <summary>The label of the fullscreen toggle.</summary>
    public string Fullscreen { get; set; } = "Toggle fullscreen";

    /// <summary>The label of the presentation-mode toggle.</summary>
    public string Presentation { get; set; } = "Presentation mode";

    /// <summary>The label of the document-properties button.</summary>
    public string Properties { get; set; } = "Document properties";

    /// <summary>The label of any close button.</summary>
    public string Close { get; set; } = "Close";

    /// <summary>The label of any cancel button.</summary>
    public string Cancel { get; set; } = "Cancel";

    /// <summary>The title of the password dialog.</summary>
    public string PasswordTitle { get; set; } = "Password required";

    /// <summary>Shown in the password dialog before any attempt.</summary>
    public string PasswordPrompt { get; set; } = "This document is protected. Enter its password to open it.";

    /// <summary>Shown in the password dialog after a wrong password.</summary>
    public string PasswordRejected { get; set; } = "That password was not accepted. Try again.";

    /// <summary>The submit button of the password dialog.</summary>
    public string PasswordSubmit { get; set; } = "Open";

    /// <summary>Shown on the surface before any source is assigned.</summary>
    public string NoDocument { get; set; } = "No document loaded.";

    /// <summary>Shown while all pages are being rendered for printing.</summary>
    public string PreparingPrint { get; set; } = "Preparing all pages for printing...";

    /// <summary>Shown when a page fails to render during a print pass.</summary>
    public string PrintAborted { get; set; } = "Printing aborted: a page failed to render.";

    /// <summary>Shown when a URL source is used without a registered HttpClient.</summary>
    public string HttpClientRequired { get; set; } = "URL sources require a registered HttpClient.";

    /// <summary>The fetch-failure message format ({0} = the underlying error).</summary>
    public string FetchFailedFormat { get; set; } = "Failed to fetch document: {0}";

    /// <summary>The load-failure message format ({0} = the underlying error).</summary>
    public string ErrorFormat { get; set; } = "Error: {0}";

    /// <summary>The loaded-document status format ({0} = the page count).</summary>
    public string PageCountFormat { get; set; } = "{0} page(s).";

    // ----- Document properties dialog -----

    /// <summary>The file-name row of the properties dialog.</summary>
    public string PropertyFileName { get; set; } = "File name";

    /// <summary>The file-size row of the properties dialog.</summary>
    public string PropertyFileSize { get; set; } = "File size";

    /// <summary>The title row of the properties dialog.</summary>
    public string PropertyTitle { get; set; } = "Title";

    /// <summary>The author row of the properties dialog.</summary>
    public string PropertyAuthor { get; set; } = "Author";

    /// <summary>The subject row of the properties dialog.</summary>
    public string PropertySubject { get; set; } = "Subject";

    /// <summary>The keywords row of the properties dialog.</summary>
    public string PropertyKeywords { get; set; } = "Keywords";

    /// <summary>The creation-date row of the properties dialog.</summary>
    public string PropertyCreationDate { get; set; } = "Created";

    /// <summary>The modification-date row of the properties dialog.</summary>
    public string PropertyModificationDate { get; set; } = "Modified";

    /// <summary>The creator row of the properties dialog.</summary>
    public string PropertyCreator { get; set; } = "Creator";

    /// <summary>The producer row of the properties dialog.</summary>
    public string PropertyProducer { get; set; } = "Producer";

    /// <summary>The PDF-version row of the properties dialog.</summary>
    public string PropertyVersion { get; set; } = "PDF version";

    /// <summary>The page-count row of the properties dialog.</summary>
    public string PropertyPageCount { get; set; } = "Page count";

    /// <summary>The page-size row of the properties dialog.</summary>
    public string PropertyPageSize { get; set; } = "Page size";

    /// <summary>Shown for a property the document does not declare.</summary>
    public string PropertyUnknown { get; set; } = "-";
}
