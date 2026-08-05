namespace Bit.BlazorUI;

public partial class _BitFileUploadItem : ComponentBase
{
    private const string ROOT_ELEMENT_CLASS = "bit-upl";

    [Parameter] public BitFileUpload FileUpload { get; set; } = default!;
    [Parameter] public BitFileInfo Item { get; set; } = default!;

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
