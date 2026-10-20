using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class ImageEditorService
{
    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private BitModalService modalService = default!;
    [AutoInject] private LazyAssemblyLoader lazyAssemblyLoader = default!;

    public async Task<ImageEditorResult> Edit(string imageUrl, string fileName, string? contentType, double? cropAspectRatio, int minWidth, int minHeight)
    {
        await lazyAssemblyLoader.LoadAssembliesAsync(["Bit.Bmage.wasm"]);

        return await ShowEditor(imageUrl, fileName, contentType, cropAspectRatio, minWidth, minHeight);
    }

    public async Task<string?> Upload(ImageEditorResult result, string uploadUrl, CancellationToken cancellationToken)
    {
        using var file = new ByteArrayContent(result.Content!);
        file.Headers.ContentType = new MediaTypeHeaderValue(result.ContentType!);

        using var content = new MultipartFormDataContent { { file, "file", result.FileName! } };

        using var response = await httpClient.PostAsync(uploadUrl, content, cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        return string.IsNullOrEmpty(responseText) ? null : responseText;
    }

    private async Task<ImageEditorResult> ShowEditor(string imageUrl, string fileName, string? contentType, double? cropAspectRatio, int minWidth, int minHeight)
    {
        TaskCompletionSource<ImageEditorResult> tcs = new();
        BitModalReference? modalReference = null;

        void Complete(ImageEditorResult result)
        {
            tcs.TrySetResult(result);
            modalReference?.Close();
        }

        Dictionary<string, object> modalComponentParameters = new()
        {
            { nameof(ImageEditorModal.ImageUrl), imageUrl },
            { nameof(ImageEditorModal.FileName), fileName },
            { nameof(ImageEditorModal.ContentType), contentType ?? string.Empty },
            { nameof(ImageEditorModal.MinWidth), minWidth },
            { nameof(ImageEditorModal.MinHeight), minHeight },
            { nameof(ImageEditorModal.OnResult), (Action<ImageEditorResult>)Complete }
        };

        if (cropAspectRatio is not null)
        {
            modalComponentParameters.Add(nameof(ImageEditorModal.CropAspectRatio), cropAspectRatio.Value);
        }

        var modalParameters = new BitModalParameters
        {
            TitleAriaId = "image-editor-title",
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => Complete(ImageEditorResult.Cancelled))
        };

        modalReference = await modalService.Show<ImageEditorModal>(modalComponentParameters, modalParameters);

        return await tcs.Task;
    }
}

public enum ImageEditorOutcome
{
    Cancelled,
    Edited,
    KeepOriginal
}

public record ImageEditorResult(ImageEditorOutcome Outcome, byte[]? Content = null, string? ContentType = null, string? FileName = null)
{
    public static ImageEditorResult Cancelled { get; } = new(ImageEditorOutcome.Cancelled);

    public static ImageEditorResult KeepOriginal { get; } = new(ImageEditorOutcome.KeepOriginal);
}
