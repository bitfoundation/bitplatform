namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnDownloading callback of <see cref="BitPdfViewer"/>, raised before the document is saved -
/// from the toolbar, the Ctrl+S shortcut or <see cref="BitPdfViewer.Download"/>. Change <see cref="FileName"/> to
/// offer the file under another name, or set <see cref="Cancel"/> to keep it from being saved.
/// </summary>
public class BitPdfDownloadArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitPdfDownloadArgs"/>.
    /// </summary>
    /// <param name="fileName">The name the file is about to be offered under.</param>
    public BitPdfDownloadArgs(string fileName)
    {
        FileName = fileName;
    }

    /// <summary>
    /// The name the file is offered under: the source's own name, then the document title, then "document.pdf".
    /// A blank value keeps that default.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Set to true to cancel the download.
    /// </summary>
    public bool Cancel { get; set; }
}
