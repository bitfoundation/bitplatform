using Bit.Bmage;
using Bit.Bmage.Input;
using Bit.Bmage.Codecs;
using Bit.Bmage.Geometry;
using Bit.Bmage.Editor.Tools;
using Bit.Bmage.Editor.Components;
using Bit.Bmage.Editor.Localization;

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
    private BmageEditorStrings editorStrings = BmageEditorStrings.Default;

    [AutoInject] private IStringLocalizer<BitBlazorUIStrings> bitBlazorUILocalizer = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // Every label the editor shows, from BitBlazorUIStrings.resx (an ImageEditor key per BmageEditorStrings property).
        editorStrings = BmageEditorStrings.Default with
        {
            Select = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSelect)],
            Crop = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorCrop)],
            Draw = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDraw)],
            Arrow = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorArrow)],
            Line = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorLine)],
            Rectangle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRectangle)],
            Ellipse = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorEllipse)],
            Text = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorText)],
            Highlight = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorHighlight)],
            Redact = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRedact)],
            Step = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorStep)],
            Eraser = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorEraser)],
            Pan = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorPan)],
            Adjust = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorAdjust)],
            Filters = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFilters)],
            Transform = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorTransform)],
            Layers = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorLayers)],
            History = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorHistory)],
            Export = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorExport)],
            Tools = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorTools)],
            Brightness = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorBrightness)],
            Contrast = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorContrast)],
            Saturation = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSaturation)],
            Exposure = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorExposure)],
            Temperature = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorTemperature)],
            Tint = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorTint)],
            Hue = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorHue)],
            Gamma = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorGamma)],
            Vibrance = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorVibrance)],
            Shadows = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorShadows)],
            Highlights = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorHighlights)],
            Sharpen = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSharpen)],
            Blur = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorBlur)],
            Vignette = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorVignette)],
            None = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorNone)],
            Grayscale = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorGrayscale)],
            Sepia = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSepia)],
            Invert = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorInvert)],
            Posterize = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorPosterize)],
            Threshold = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorThreshold)],
            RotateLeft = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRotateLeft)],
            RotateRight = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRotateRight)],
            FlipHorizontal = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFlipHorizontal)],
            FlipVertical = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFlipVertical)],
            Straighten = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorStraighten)],
            AspectRatio = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorAspectRatio)],
            Freeform = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFreeform)],
            Original = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorOriginal)],
            Square = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSquare)],
            ApplyCrop = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorApplyCrop)],
            CancelCrop = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorCancelCrop)],
            Undo = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorUndo)],
            Redo = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRedo)],
            Reset = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorReset)],
            Save = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSave)],
            Cancel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorCancel)],
            Done = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDone)],
            Delete = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDelete)],
            Duplicate = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDuplicate)],
            BringToFront = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorBringToFront)],
            SendToBack = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSendToBack)],
            Download = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDownload)],
            CopyToClipboard = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorCopyToClipboard)],
            ZoomIn = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorZoomIn)],
            ZoomOut = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorZoomOut)],
            ZoomToFit = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorZoomToFit)],
            ZoomTo100 = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorZoomTo100)],
            OpenImage = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorOpenImage)],
            PasteImage = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorPasteImage)],
            Color = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorColor)],
            FillColor = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFillColor)],
            StrokeColor = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorStrokeColor)],
            StrokeWidth = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorStrokeWidth)],
            Opacity = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorOpacity)],
            FontSize = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFontSize)],
            Dashed = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDashed)],
            Filled = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFilled)],
            RedactionStyle = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRedactionStyle)],
            Solid = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSolid)],
            Pixelate = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorPixelate)],
            Format = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFormat)],
            Quality = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorQuality)],
            Width = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorWidth)],
            Height = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorHeight)],
            Metadata = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorMetadata)],
            KeepMetadata = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorKeepMetadata)],
            RemoveMetadata = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorRemoveMetadata)],
            StripSensitiveMetadata = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorStripSensitiveMetadata)],
            EstimatedSize = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorEstimatedSize)],
            Loading = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorLoading)],
            Processing = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorProcessing)],
            NoImage = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorNoImage)],
            DropImageHere = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorDropImageHere)],
            ImageTooLarge = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorImageTooLarge)],
            UnsupportedFormat = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorUnsupportedFormat)],
            LoadFailed = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorLoadFailed)],
            ExportFailed = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorExportFailed)],
            TextNeedsFont = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorTextNeedsFont)],
            FormatNotSupported = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorFormatNotSupported)],
            EditorLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorEditorLabel)],
            CanvasLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorCanvasLabel)],
            ToolbarLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorToolbarLabel)],
            PanelLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorPanelLabel)],
            CropHandleLabel = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorCropHandleLabel)],
            SelectedAnnotation = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorSelectedAnnotation)],
            KeyboardHint = bitBlazorUILocalizer[nameof(BitBlazorUIStrings.ImageEditorKeyboardHint)]
        };
    }

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
