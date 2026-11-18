using Bit.Bmage;

namespace Boilerplate.Client.Core.Components.Common;

public partial class ImageEditorModal
{
    private const long MaxFileSize = 1024 * 1024 * 10;

    [Parameter] public string ImageUrl { get; set; } = default!;

    [Parameter] public string FileName { get; set; } = default!;

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

        await editor.LoadAsync(BmageImageSource.FromUrl(ImageUrl));

        if (editor.Session is null)
        {
            couldNotOpenImage = true;
            StateHasChanged();
            return;
        }

        editor.SetTool(BmageEditorToolKind.Crop);
        editor.SetCropAspect(CropAspect);
        StateHasChanged();
    }

    private BmageAspectRatio? CropAspect => CropAspectRatio is double ratio ? BmageAspectRatio.FromValue(ratio) : null;

    private void StartCrop(BmageEditorContext context)
    {
        context.SetTool(BmageEditorToolKind.Crop);
        context.SetCropAspect(CropAspect);
    }

    private async Task Save()
    {
        if (isSaving || editor?.Session is null) return;

        isSaving = true;
        errorMessage = null;

        try
        {
            if (editor.Tool is BmageEditorToolKind.Crop)
            {
                await editor.ApplyCropAsync();
            }

            // Nothing was edited, so the picked file goes up as it is instead of a re-encoded copy of it.
            if (editor.Session.History.CanUndo is false)
            {
                KeepOriginal();
                return;
            }

            var format = editor.BrowserCapabilities?.ChooseEncodableFormat([BmageImageFormat.WebP]) ?? BmageImageFormat.Jpeg;

            var image = await editor.ExportAsync(new BmageImageExportOptions { Format = format, Quality = 1, MaxFileSize = MaxFileSize, MaxFileSizeStrategy = BmageMaxFileSizeStrategy.QualityOnly });

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
