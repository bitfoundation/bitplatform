namespace Bit.Bmage.Editor.Localization;

/// <summary>
/// Every piece of user-visible text in the editor. Replace the whole object, or individual properties, to translate the
/// UI or reword it. No string is hard-coded into a component.
/// </summary>
/// <example>
/// <code>
/// var german = BmageEditorStrings.Default with { Crop = "Zuschneiden", Rotate = "Drehen", Done = "Fertig" };
/// &lt;BmageEditor Strings="german" /&gt;
/// </code>
/// </example>
public sealed record BmageEditorStrings
{
    /// <summary>The built-in English strings.</summary>
    public static BmageEditorStrings Default { get; } = new();

    // Tools
    public string Select { get; init; } = "Select";
    public string Crop { get; init; } = "Crop";
    public string Draw { get; init; } = "Draw";
    public string Arrow { get; init; } = "Arrow";
    public string Line { get; init; } = "Line";
    public string Rectangle { get; init; } = "Rectangle";
    public string Ellipse { get; init; } = "Ellipse";
    public string Text { get; init; } = "Text";
    public string Highlight { get; init; } = "Highlight";
    public string Redact { get; init; } = "Redact";
    public string Step { get; init; } = "Step number";
    public string Eraser { get; init; } = "Eraser";
    public string Pan { get; init; } = "Pan";

    // Panels
    public string Adjust { get; init; } = "Adjust";
    public string Filters { get; init; } = "Filters";
    public string Transform { get; init; } = "Transform";
    public string Layers { get; init; } = "Layers";
    public string History { get; init; } = "History";
    public string Export { get; init; } = "Export";
    public string Tools { get; init; } = "Tools";

    // Adjustments
    public string Brightness { get; init; } = "Brightness";
    public string Contrast { get; init; } = "Contrast";
    public string Saturation { get; init; } = "Saturation";
    public string Exposure { get; init; } = "Exposure";
    public string Temperature { get; init; } = "Temperature";
    public string Tint { get; init; } = "Tint";
    public string Hue { get; init; } = "Hue";
    public string Gamma { get; init; } = "Gamma";
    public string Vibrance { get; init; } = "Vibrance";
    public string Shadows { get; init; } = "Shadows";
    public string Highlights { get; init; } = "Highlights";
    public string Sharpen { get; init; } = "Sharpen";
    public string Blur { get; init; } = "Blur";
    public string Vignette { get; init; } = "Vignette";

    // Filters
    public string None { get; init; } = "None";
    public string Grayscale { get; init; } = "Grayscale";
    public string Sepia { get; init; } = "Sepia";
    public string Invert { get; init; } = "Invert";
    public string Posterize { get; init; } = "Posterize";
    public string Threshold { get; init; } = "Threshold";

    // Transform
    public string RotateLeft { get; init; } = "Rotate left";
    public string RotateRight { get; init; } = "Rotate right";
    public string FlipHorizontal { get; init; } = "Flip horizontal";
    public string FlipVertical { get; init; } = "Flip vertical";
    public string Straighten { get; init; } = "Straighten";
    public string AspectRatio { get; init; } = "Aspect ratio";
    public string Freeform { get; init; } = "Freeform";
    public string Original { get; init; } = "Original";
    public string Square { get; init; } = "Square";
    public string ApplyCrop { get; init; } = "Apply crop";
    public string CancelCrop { get; init; } = "Cancel crop";

    // Actions
    public string Undo { get; init; } = "Undo";
    public string Redo { get; init; } = "Redo";
    public string Reset { get; init; } = "Reset";
    public string Save { get; init; } = "Save";
    public string Cancel { get; init; } = "Cancel";
    public string Done { get; init; } = "Done";
    public string Delete { get; init; } = "Delete";
    public string Duplicate { get; init; } = "Duplicate";
    public string BringToFront { get; init; } = "Bring to front";
    public string SendToBack { get; init; } = "Send to back";
    public string Download { get; init; } = "Download";
    public string CopyToClipboard { get; init; } = "Copy to clipboard";
    public string ZoomIn { get; init; } = "Zoom in";
    public string ZoomOut { get; init; } = "Zoom out";
    public string ZoomToFit { get; init; } = "Fit to window";
    public string ZoomTo100 { get; init; } = "Actual size";
    public string OpenImage { get; init; } = "Open image";
    public string PasteImage { get; init; } = "Paste from clipboard";

    // Style controls
    public string Color { get; init; } = "Colour";
    public string FillColor { get; init; } = "Fill colour";
    public string StrokeColor { get; init; } = "Outline colour";
    public string StrokeWidth { get; init; } = "Line width";
    public string Opacity { get; init; } = "Opacity";
    public string FontSize { get; init; } = "Text size";
    public string Dashed { get; init; } = "Dashed";
    public string Filled { get; init; } = "Filled";
    public string RedactionStyle { get; init; } = "Redaction style";
    public string Solid { get; init; } = "Solid";
    public string Pixelate { get; init; } = "Pixelate";

    // Export
    public string Format { get; init; } = "Format";
    public string Quality { get; init; } = "Quality";
    public string Width { get; init; } = "Width";
    public string Height { get; init; } = "Height";
    public string Metadata { get; init; } = "Metadata";
    public string KeepMetadata { get; init; } = "Keep all";
    public string RemoveMetadata { get; init; } = "Remove all";
    public string StripSensitiveMetadata { get; init; } = "Remove location and device";
    public string EstimatedSize { get; init; } = "Estimated size";

    // Status and messages
    public string Loading { get; init; } = "Loading image";
    public string Processing { get; init; } = "Processing";
    public string NoImage { get; init; } = "No image loaded";
    public string DropImageHere { get; init; } = "Drop an image here, or click to choose one";
    public string ImageTooLarge { get; init; } = "That image is too large to open here.";
    public string UnsupportedFormat { get; init; } = "That file format is not supported.";
    public string LoadFailed { get; init; } = "The image could not be opened.";
    public string ExportFailed { get; init; } = "The image could not be exported.";
    public string TextNeedsFont { get; init; } = "Text cannot be drawn until a font is registered with the text rasteriser.";
    public string FormatNotSupported { get; init; } = "This browser cannot save that format. A supported format was used instead.";

    // Accessibility
    public string EditorLabel { get; init; } = "Image editor";
    public string CanvasLabel { get; init; } = "Image canvas";
    public string ToolbarLabel { get; init; } = "Editor tools";
    public string PanelLabel { get; init; } = "Editor options";
    public string CropHandleLabel { get; init; } = "Crop handle";
    public string SelectedAnnotation { get; init; } = "Selected annotation";
    public string KeyboardHint { get; init; } = "Use the arrow keys to move the selection, and Delete to remove it.";

    /// <summary>Formats a pixel dimension pair for display, e.g. "1920 × 1080".</summary>
    public Func<int, int, string> FormatDimensions { get; init; } = static (w, h) => $"{w} × {h}";

    /// <summary>Formats a byte count for display, e.g. "1.4 MB".</summary>
    public Func<long, string> FormatFileSize { get; init; } = static bytes => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
    };

    /// <summary>Formats a percentage for display, e.g. "85%".</summary>
    public Func<double, string> FormatPercent { get; init; } = static value => $"{value * 100:0}%";
}
