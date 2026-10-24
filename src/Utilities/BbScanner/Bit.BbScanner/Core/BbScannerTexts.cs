using System.Globalization;

namespace Bit.BbScanner;

/// <summary>
/// Everything the scanner shows or says to its user - control labels, status, still image
/// notices, error messages and what it announces to assistive technology - for an application to
/// translate. Pass it to <see cref="BbScanner.Texts"/>.
/// </summary>
/// <remarks>
/// <para>
/// Anything left unset keeps its English default, so a translation can start with the controls
/// and grow. The texts that carry a value are functions, which leaves word order, number
/// formatting and plural forms to the translator. An instance is immutable once built, so one
/// per language can be shared by every scanner in the application:
/// </para>
/// <code>
/// private static readonly BbScannerTexts German = new()
/// {
///     StartButton = "Starten",
///     StopButton = "Beenden",
///     StatusScanning = "Scannen",
///     ScanAnnouncement = result => $"{result.Format} gelesen: {result.Text}",
/// };
/// </code>
/// <para>
/// The accessible name of the scanner itself is <see cref="BbScanner.AriaLabel"/>; the direction
/// of the layout follows the document's, so a right-to-left language needs no setting here.
/// </para>
/// </remarks>
public sealed class BbScannerTexts
{
    /// <summary>The English texts the scanner uses when no other set is given.</summary>
    public static BbScannerTexts Default { get; } = new();

    /// <summary>The button that opens the camera.</summary>
    public string StartButton { get; init; } = "Start";

    /// <summary>The button that releases the camera.</summary>
    public string StopButton { get; init; } = "Stop";

    /// <summary>The button that stops decoding but keeps the camera open.</summary>
    public string PauseButton { get; init; } = "Pause";

    /// <summary>The button that resumes decoding after a pause.</summary>
    public string ResumeButton { get; init; } = "Resume";

    /// <summary>The button that switches the torch on and off.</summary>
    public string TorchButton { get; init; } = "Torch";

    /// <summary>The button that opens the file picker to scan a still image.</summary>
    public string ScanImageButton { get; init; } = "Scan image";

    /// <summary>The accessible name of the camera picker.</summary>
    public string CameraLabel { get; init; } = "Camera";

    /// <summary>The accessible name of the zoom slider.</summary>
    public string ZoomLabel { get; init; } = "Zoom";

    /// <summary>
    /// The name of a camera the browser gives no name of its own, which every camera is until
    /// the user has granted permission once; the argument counts from one.
    /// </summary>
    public Func<int, string> UnnamedCamera { get; init; } = number => string.Create(CultureInfo.InvariantCulture, $"Camera {number}");

    /// <summary>The status of a scanner that has not been started.</summary>
    public string StatusIdle { get; init; } = "Ready";

    /// <summary>The status while the camera is being opened.</summary>
    public string StatusStarting { get; init; } = "Starting camera";

    /// <summary>The status while frames are being decoded; the camera's resolution is shown after it.</summary>
    public string StatusScanning { get; init; } = "Scanning";

    /// <summary>The status while decoding is paused with the camera open.</summary>
    public string StatusPaused { get; init; } = "Paused";

    /// <summary>The status once the camera has been released.</summary>
    public string StatusStopped { get; init; } = "Stopped";

    /// <summary>The status once the scanner has failed; the message says why.</summary>
    public string StatusFaulted { get; init; } = "Error";

    /// <summary>The message shown while the browser asks the user for the camera.</summary>
    public string RequestingCamera { get; init; } = "Requesting camera access...";

    /// <summary>The notice shown while a still image is being read.</summary>
    public string ReadingImage { get; init; } = "Reading image...";

    /// <summary>The notice for a still image that holds no barcode the scanner accepts.</summary>
    public string NoBarcodeFound { get; init; } = "No barcode found in that image.";

    /// <summary>The notice for a file that declares itself to be something other than an image.</summary>
    public string NotAnImage { get; init; } = "The selected file is not an image.";

    /// <summary>The notice for a file the browser could not read as an image.</summary>
    public string ImageUnreadable { get; init; } = "The file could not be read as an image.";

    /// <summary>The notice for an image that was read but could not be scanned.</summary>
    public string ImageNotScanned { get; init; } = "The image could not be decoded.";

    /// <summary>
    /// The notice for a file over <see cref="BbScanner.MaxImageFileSize"/>; the argument is that
    /// limit in megabytes.
    /// </summary>
    public Func<double, string> ImageTooLarge { get; init; } =
        megabytes => string.Create(CultureInfo.InvariantCulture, $"The image is larger than the {megabytes:0.#} MB allowed.");

    /// <summary>The notice for an image that decodes to more pixels than is safe to hold.</summary>
    public string ImageTooManyPixels { get; init; } = "The image is too large to scan.";

    /// <summary>
    /// The message shown when the scanner fails. The default is the exception's own message,
    /// which is written for a developer as much as for a user; an application that translates
    /// the rest usually switches on <see cref="BbScannerException.Kind"/> here.
    /// </summary>
    /// <remarks><see cref="BbScanner.OnError"/> still receives the exception, its message untouched.</remarks>
    public Func<BbScannerException, string> ErrorMessage { get; init; } = exception => exception.Message;

    /// <summary>
    /// What a screen reader announces for a scan. Barcode contents are arbitrary, so this is only
    /// ever rendered as text, never as markup.
    /// </summary>
    public Func<BbBarcodeResult, string> ScanAnnouncement { get; init; } = result => $"{DisplayName(result.Format)}: {result.Text}";

    /// <summary>The usual written name of a symbology, such as "QR Code" or "EAN-13".</summary>
    internal static string DisplayName(BbBarcodeFormat format) => format switch
    {
        BbBarcodeFormat.QrCode => "QR Code",
        BbBarcodeFormat.DataMatrix => "Data Matrix",
        BbBarcodeFormat.Pdf417 => "PDF417",
        BbBarcodeFormat.Code128 => "Code 128",
        BbBarcodeFormat.Code39 => "Code 39",
        BbBarcodeFormat.Code93 => "Code 93",
        BbBarcodeFormat.Codabar => "Codabar",
        BbBarcodeFormat.Ean13 => "EAN-13",
        BbBarcodeFormat.Ean8 => "EAN-8",
        BbBarcodeFormat.UpcA => "UPC-A",
        BbBarcodeFormat.UpcE => "UPC-E",
        BbBarcodeFormat.Itf => "ITF",
        BbBarcodeFormat.DataBar => "GS1 DataBar",
        BbBarcodeFormat.DataBarLimited => "GS1 DataBar Limited",
        BbBarcodeFormat.DataBarExpanded => "GS1 DataBar Expanded",
        BbBarcodeFormat.MaxiCode => "MaxiCode",
        BbBarcodeFormat.MicroQrCode => "Micro QR",
        BbBarcodeFormat.MicroPdf417 => "MicroPDF417",
        _ => format.ToString(),
    };
}
