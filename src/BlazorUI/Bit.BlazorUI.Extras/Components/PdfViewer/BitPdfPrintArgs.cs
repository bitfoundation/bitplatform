namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnPrinting callback of <see cref="BitPdfViewer"/>, raised before the pages are prepared for the
/// browser's print dialog - from the toolbar, the Ctrl+P shortcut or one of the Print methods. Set
/// <see cref="Cancel"/> to keep the document from being printed.
/// </summary>
public class BitPdfPrintArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitPdfPrintArgs"/>.
    /// </summary>
    /// <param name="fromPage">The first page about to be printed (1-based).</param>
    /// <param name="toPage">The last page about to be printed (1-based, inclusive).</param>
    public BitPdfPrintArgs(int fromPage, int toPage)
    {
        FromPage = fromPage;
        ToPage = toPage;
    }

    /// <summary>
    /// The first page about to be printed (1-based).
    /// </summary>
    public int FromPage { get; }

    /// <summary>
    /// The last page about to be printed (1-based, inclusive).
    /// </summary>
    public int ToPage { get; }

    /// <summary>
    /// Set to true to cancel the print.
    /// </summary>
    public bool Cancel { get; set; }
}
