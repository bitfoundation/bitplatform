using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// The scanner in another language: what the built-in interface shows and announces comes from
/// <see cref="BbScannerTexts"/>, and what the application is told does not change with it.
/// </summary>
[TestClass]
public class BbScannerTextsTests : BunitContext
{
    private static readonly BbScannerTexts Spanish = new()
    {
        StartButton = "Iniciar",
        ScanImageButton = "Escanear imagen",
        StatusIdle = "Listo",
        StatusFaulted = "Fallo",
        ErrorMessage = exception => exception.Kind == BbScannerErrorKind.NotSupported ? "El escáner no está disponible aquí." : exception.Message,
    };

    public BbScannerTextsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
#endif
    }

    [TestMethod]
    public void The_controls_and_the_status_are_in_the_application_language()
    {
        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.AutoStart, false)
            .Add(p => p.Texts, Spanish));

        var controls = scanner.Find(".bit-bbscanner__controls").TextContent;
        Assert.Contains("Iniciar", controls);
        Assert.Contains("Escanear imagen", controls);
        Assert.DoesNotContain("Scan image", controls);

        // .NET 8 cannot tell a prerender from an interactive render, so there the scanner has already reported that
        // it cannot run here, which is a status of its own.
        Assert.Contains(OnNet8() ? "Fallo" : "Listo", scanner.Find(".bit-bbscanner__status").TextContent);
    }

    [TestMethod]
    public void Without_texts_the_interface_is_english()
    {
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.AutoStart, false));

        var controls = scanner.Find(".bit-bbscanner__controls").TextContent;
        Assert.Contains("Start", controls);
        Assert.Contains("Scan image", controls);
    }

    [TestMethod]
    public void A_failure_is_shown_and_alerted_in_the_application_language_while_the_application_gets_the_original()
    {
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
#endif
        BbScannerException? error = null;

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Texts, Spanish)
            .Add(p => p.OnError, e => error = e));

        Assert.AreEqual("El escáner no está disponible aquí.", scanner.Find(".bit-bbscanner__message--error").TextContent.Trim());
        Assert.AreEqual("El escáner no está disponible aquí.", scanner.Find("[role=alert]").TextContent);
        Assert.IsNotNull(error);
        Assert.Contains("WebAssembly", error.Message, "OnError receives the exception with its message untouched.");
    }

    [TestMethod]
    public void The_live_regions_are_in_the_document_before_there_is_anything_to_say()
    {
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.AutoStart, false));

        Assert.AreEqual("polite", scanner.Find("[role=status].bit-bbscanner__sr").GetAttribute("aria-live"));
        Assert.IsNotNull(scanner.Find("[role=alert].bit-bbscanner__sr"));
    }

    [TestMethod]
    public void Start_and_stop_are_one_button()
    {
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.AutoStart, false));

        // Not running: one button offers to start, and nothing offers to stop or pause.
        var buttons = scanner.FindAll(".bit-bbscanner__controls > button").Select(b => b.TextContent.Trim()).ToList();
        CollectionAssert.Contains(buttons, "Start");
        CollectionAssert.DoesNotContain(buttons, "Stop");
        CollectionAssert.DoesNotContain(buttons, "Pause");
    }

    [TestMethod]
    public void The_english_announcement_names_the_symbology_as_it_is_written()
    {
        Assert.AreEqual("QR Code: HELLO", BbScannerTexts.Default.ScanAnnouncement(Decode(QrEncoder.Encode("HELLO"))));
        Assert.AreEqual("Code 128: CODE-128", BbScannerTexts.Default.ScanAnnouncement(DecodeLinear(LinearEncoders.Code128("CODE-128"))));
        Assert.AreEqual("EAN-13: 4006381333931", BbScannerTexts.Default.ScanAnnouncement(DecodeLinear(LinearEncoders.Ean13("4006381333931"))));
    }

    [TestMethod]
    public void The_english_defaults_say_what_the_scanner_always_said()
    {
        var texts = BbScannerTexts.Default;

        Assert.AreEqual("Camera 2", texts.UnnamedCamera(2));
        Assert.AreEqual("The image is larger than the 32 MB allowed.", texts.ImageTooLarge(32));
        Assert.AreEqual("The image is larger than the 1.5 MB allowed.", texts.ImageTooLarge(1.5));

        var exception = new BbScannerException(BbScannerErrorKind.NoCamera, "No camera matching the request was found.");
        Assert.AreEqual(exception.Message, texts.ErrorMessage(exception));
    }

    [TestMethod]
    public void Texts_left_out_keep_their_english_default()
    {
        var partial = new BbScannerTexts { StartButton = "Iniciar" };

        Assert.AreEqual("Iniciar", partial.StartButton);
        Assert.AreEqual(BbScannerTexts.Default.StopButton, partial.StopButton);
        Assert.AreEqual(BbScannerTexts.Default.NoBarcodeFound, partial.NoBarcodeFound);
    }

    private static BbBarcodeResult Decode(BbBitMatrix matrix)
    {
        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { DuplicateSuppressionWindow = TimeSpan.Zero });
        return decoder.Decode(image.View, suppressDuplicates: false) ?? throw new AssertFailedException("The symbol did not decode.");
    }

    private static BbBarcodeResult DecodeLinear(bool[] modules)
    {
        using var image = SyntheticImage.FromLinear(modules, scale: 3, height: 40);
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { DuplicateSuppressionWindow = TimeSpan.Zero });
        return decoder.Decode(image.View, suppressDuplicates: false) ?? throw new AssertFailedException("The symbol did not decode.");
    }

    private static bool OnNet8()
    {
#if NET9_0_OR_GREATER
        return false;
#else
        return true;
#endif
    }
}
