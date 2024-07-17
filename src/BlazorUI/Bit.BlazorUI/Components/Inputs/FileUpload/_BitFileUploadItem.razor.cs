namespace Bit.BlazorUI;

public partial class _BitFileUploadItem : ComponentBase
{
    private const string ROOT_ELEMENT_CLASS = "bit-upl";

    [Parameter] public BitFileUpload FileUpload { get; set; } = default!;
    [Parameter] public BitFileInfo Item { get; set; } = default!;

    private static int GetFileUploadPercent(BitFileInfo file)
    {
        int uploadedPercent;
        if (file.TotalUploadedSize >= file.Size)
        {
            uploadedPercent = 100;
        }
        else
        {
            uploadedPercent = (int)((file.TotalUploadedSize + file.LastChunkUploadedSize) / (float)file.Size * 100);
        }

        return uploadedPercent;
    }

    private static string GetFileUploadSize(BitFileInfo file)
    {
        long uploadedSize;
        if (file.TotalUploadedSize >= file.Size)
        {
            uploadedSize = file.Size;
        }
        else
        {
            uploadedSize = file.TotalUploadedSize + file.LastChunkUploadedSize;
        }

        return FileSizeHumanizer.Humanize(uploadedSize);
    }

    private string GetFileElClass(BitFileUploadStatus status)
        => status switch
        {
            BitFileUploadStatus.Completed => $"{ROOT_ELEMENT_CLASS}-uld",
            BitFileUploadStatus.Failed or BitFileUploadStatus.NotAllowed => $"{ROOT_ELEMENT_CLASS}-fld",
            BitFileUploadStatus.Paused => $"{ROOT_ELEMENT_CLASS}-psd",
            _ => $"{ROOT_ELEMENT_CLASS}-ip",
        };

    private string GetUploadMessage(BitFileInfo file)
        => file.Status switch
        {
            BitFileUploadStatus.Completed => FileUpload.SuccessfulUploadMessage,
            BitFileUploadStatus.Failed => FileUpload.FailedUploadMessage,
            BitFileUploadStatus.RemoveFailed => FileUpload.FailedRemoveMessage,
            BitFileUploadStatus.NotAllowed => FileUpload.IsFileTypeNotAllowed(file) ? FileUpload.NotAllowedExtensionErrorMessage : FileUpload.MaxSizeErrorMessage,
            _ => string.Empty,
        };
}
