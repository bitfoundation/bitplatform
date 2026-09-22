namespace Bit.BlazorUI;

// Which button of a file item the focus is to be moved to once the button that was pressed is gone.
internal enum BitFileUploadFocusTarget
{
    Upload,
    Pause,
    Cancel,
    Remove
}

public partial class _BitFileUploadItem : ComponentBase, IDisposable
{
    private const string ROOT_ELEMENT_CLASS = "bit-upl";

    // Which of the four action buttons this item drew in its last render, and the element of each one, so
    // that the parent can hand the focus to a button that is actually there instead of to a stale reference.
    private bool _hasUploadButton;
    private bool _hasPauseButton;
    private bool _hasCancelButton;
    private bool _hasRemoveButton;
    private ElementReference _uploadButtonRef;
    private ElementReference _pauseButtonRef;
    private ElementReference _cancelButtonRef;
    private ElementReference _removeButtonRef;
    private string? _registeredFileId;

    [Parameter] public BitFileUpload FileUpload { get; set; } = default!;
    [Parameter] public BitFileInfo Item { get; set; } = default!;

    protected override void OnParametersSet()
    {
        // the item is keyed by the file it renders, so a file id it has not registered under yet is a
        // registration of its own rather than a second one of the same item.
        if (_registeredFileId == Item.FileId) return;

        if (_registeredFileId is not null)
        {
            FileUpload.UnregisterItem(_registeredFileId, this);
        }

        _registeredFileId = Item.FileId;

        FileUpload.RegisterItem(_registeredFileId, this);
    }

    public void Dispose()
    {
        if (_registeredFileId is not null)
        {
            FileUpload.UnregisterItem(_registeredFileId, this);
            _registeredFileId = null;
        }
    }

    // The preferred button first, then whatever else the item has: an upload that fails the moment it is
    // started, for instance, leaves an item with a retry button where the pause button was expected. The
    // remove button comes last of the fallbacks, so the focus never lands on the destructive action of an
    // item while that item still has something else to offer.
    internal async Task<bool> TryFocus(BitFileUploadFocusTarget target)
    {
        // every button of a disabled component is disabled too, and none of them can take the focus.
        if (FileUpload.IsEnabled is false) return false;

        BitFileUploadFocusTarget[] order =
        [
            target,
            BitFileUploadFocusTarget.Upload,
            BitFileUploadFocusTarget.Pause,
            BitFileUploadFocusTarget.Cancel,
            BitFileUploadFocusTarget.Remove
        ];

        foreach (var candidate in order)
        {
            var (rendered, element) = candidate switch
            {
                BitFileUploadFocusTarget.Remove => (_hasRemoveButton, _removeButtonRef),
                BitFileUploadFocusTarget.Pause => (_hasPauseButton, _pauseButtonRef),
                BitFileUploadFocusTarget.Cancel => (_hasCancelButton, _cancelButtonRef),
                _ => (_hasUploadButton, _uploadButtonRef),
            };

            if (rendered is false) continue;

            await element.FocusAsync();

            return true;
        }

        return false;
    }

    private static int GetFileUploadPercent(BitFileInfo file)
    {
        // an empty file has no byte whose progress could be measured, so it is either done or not
        // started - reporting it as complete before it has been sent would be telling a story.
        if (file.Size == 0) return file.Status is BitFileUploadStatus.Completed ? 100 : 0;

        if (file.TotalUploadedSize >= file.Size) return 100;

        // the progress events count the bytes of the whole request body, multipart overhead included,
        // so the raw ratio can slightly overshoot and has to be capped.
        return Math.Min(100, (int)((file.TotalUploadedSize + file.LastChunkUploadedSize) / (float)file.Size * 100));
    }

    private static long GetFileUploadSize(BitFileInfo file)
    {
        // the progress events count the bytes of the whole request body, multipart overhead included, so the
        // running total can overshoot the file and has to be capped - "1.1 MB/1 MB" would read as a bug.
        return Math.Min(file.Size, file.TotalUploadedSize + file.LastChunkUploadedSize);
    }

    // The glyph standing in for a file that has no thumbnail of its own. The content type the browser
    // reports is asked first, since it is what the file really is, and the extension only answers for the
    // types a browser leaves blank - a .zip or a .csv on a machine that has nothing registered for them.
    private static string GetFileGlyph(BitFileInfo file)
    {
        var contentType = file.ContentType;

        if (contentType.HasValue())
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "Photo2";
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return "Video";
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "MusicInCollection";
            if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) return "PDF";
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return "TextDocument";
        }

        return Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".pdf" => "PDF",
            ".doc" or ".docx" or ".rtf" or ".odt" => "WordDocument",
            ".xls" or ".xlsx" or ".csv" or ".ods" => "ExcelDocument",
            ".ppt" or ".pptx" or ".odp" => "PowerPointDocument",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "ZipFolder",
            ".txt" or ".md" or ".log" => "TextDocument",
            ".json" or ".xml" or ".html" or ".htm" or ".css" or ".js" or ".ts" or ".cs" => "FileCode",
            _ => "Page"
        };
    }

    private string FormatSize(long size)
    {
        return FileUpload.FileSizeFormatter is null ? FileSizeHumanizer.Humanize(size) : FileUpload.FileSizeFormatter(size);
    }

    private static string GetFileElClass(BitFileUploadStatus status)
        => status switch
        {
            BitFileUploadStatus.Completed => $"{ROOT_ELEMENT_CLASS}-uld",
            BitFileUploadStatus.Failed or BitFileUploadStatus.NotAllowed or BitFileUploadStatus.RemoveFailed => $"{ROOT_ELEMENT_CLASS}-fld",
            BitFileUploadStatus.Paused or BitFileUploadStatus.Canceled => $"{ROOT_ELEMENT_CLASS}-psd",
            _ => $"{ROOT_ELEMENT_CLASS}-ip",
        };
}
