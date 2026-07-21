namespace Bit.BlazorUI;

public class BitFileInputClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitFileInput.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the root element while files are being dragged over the BitFileInput.
    /// </summary>
    public string? Dragging { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the browse button (label) of the BitFileInput.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the file list container of the BitFileInput.
    /// </summary>
    public string? FileList { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each file item of the BitFileInput.
    /// </summary>
    public string? FileItem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image preview thumbnail of each file item of the BitFileInput.
    /// </summary>
    public string? Preview { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the file name of each file item of the BitFileInput.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the file size of each file item of the BitFileInput.
    /// </summary>
    public string? FileSize { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the validation error message of each invalid file item of the BitFileInput.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the remove button of each file item of the BitFileInput.
    /// </summary>
    public string? RemoveButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the remove button icon of each file item of the BitFileInput.
    /// </summary>
    public string? RemoveIcon { get; set; }
}
