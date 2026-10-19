using Bit.Bmage;
using Bit.Bmage.Input;
using Bit.Bmage.Codecs;
using Bit.Bmage.Geometry;
using Bit.Bmage.Editor.Tools;
using Bit.Bmage.Editor.Components;

namespace Boilerplate.Client.Core.Components.Common;

public partial class ImageEditorModal
{
    private const long MaxFileSize = 1024 * 1024 * 10;

    [Parameter] public string ImageUrl { get; set; } = default!;

    [Parameter] public string FileName { get; set; } = default!;

    [Parameter] public string? ContentType { get; set; }

    [Parameter] public double? CropAspectRatio { get; set; }

    [Parameter] public int MinWidth { get; set; }

    [Parameter] public int MinHeight { get; set; }

    [Parameter] public Action<ImageEditorResult>? OnResult { get; set; }

    [CascadingParameter] public BitDir? CurrentDir { get; set; }


    private bool isSaving;
    private string? errorMessage;
    private bool couldNotOpenImage;
    private BmageEditor? editor;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (editor is null) return;

        await editor.LoadAsync(ImageSource.FromUrl(ImageUrl));

        if (editor.Session is null)
        {
            couldNotOpenImage = true;
            StateHasChanged();
            return;
        }

        editor.SetTool(EditorToolKind.Crop);
        editor.SetCropAspect(CropAspect);
        StateHasChanged();
    }

    private AspectRatio? CropAspect => CropAspectRatio is double ratio ? AspectRatio.FromValue(ratio) : null;

    private void StartCrop(BmageEditorContext context)
    {
        context.SetTool(EditorToolKind.Crop);
        context.SetCropAspect(CropAspect);
    }

    private async Task Save()
    {
        if (isSaving || editor?.Session is null) return;

        isSaving = true;
        errorMessage = null;

        try
        {
            if (editor.Tool is EditorToolKind.Crop)
            {
                await editor.ApplyCropAsync();
            }

            var exportOptions = ContentType is "image/jpeg" ? ImageExportOptions.Jpeg with { Quality = 0.92 } : ImageExportOptions.Png;

            var image = await editor.ExportAsync(exportOptions);

            if (image is not null && image.Length > MaxFileSize && image.Format is not ImageFormat.Jpeg)
            {
                image = await editor.ExportAsync(ImageExportOptions.Jpeg with { Quality = 0.92, MaxFileSize = MaxFileSize });
            }

            if (image is null) return;

            if (image.Width < MinWidth || image.Height < MinHeight)
            {
                errorMessage = Localizer[nameof(AppStrings.ImageTooSmall), MinWidth, MinHeight, image.Width, image.Height];
                return;
            }

            OnResult?.Invoke(new(ImageEditorOutcome.Edited, image.Data, image.MimeType, Path.ChangeExtension(FileName, image.Extension)));
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleEditorError(Exception exception)
    {
        if (editor?.Session is null)
        {
            couldNotOpenImage = true;
            return;
        }

        errorMessage = exception.Message;
    }

    private void Cancel() => OnResult?.Invoke(ImageEditorResult.Cancelled);

    private void KeepOriginal() => OnResult?.Invoke(ImageEditorResult.KeepOriginal);
}
