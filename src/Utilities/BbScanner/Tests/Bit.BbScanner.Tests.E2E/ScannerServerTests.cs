using Bit.BbScanner.Tests.E2E.Infrastructure;
using Bit.BbScanner.TestKit;
using Microsoft.Playwright;

namespace Bit.BbScanner.Tests.E2E;

/// <summary>
/// The scanner on a Blazor Server circuit, in the Blazor Server sample: the camera opens in the browser, the page sends
/// each frame as the luminance of the searched region, in slices a circuit message each, and the frame loop and the
/// decoder run on the server. What the WebAssembly suites prove of the camera's lifecycle is proved again here over a
/// network connection - and that a camera the circuit leaves behind is closed by the page.
/// </summary>
[TestClass]
public class ScannerServerTests : BbScannerTest
{
    [TestMethod]
    public async Task OnACircuitTheCameraOpensInTheBrowserAndTheServerDecodesItsFrames()
    {
        await using var session = await OpenOnServerAsync(SingleQrClip);
        await GoToAsync(session, "/");

        await session.WaitForStatusAsync(["scanning"], 60_000);
        await session.Page.Locator("table.results td.value", new() { HasText = "SINGLE-QR" }).First.WaitForAsync(new() { Timeout = 60_000 });

        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.Contains("Server", await session.Page.TextContentAsync(".facts") ?? "", "The page runs on the Server renderer.");
        Assert.IsFalse(await session.Page.EvaluateAsync<bool>("() => performance.getEntriesByType('resource').some(e => e.name.includes('dotnet.native'))"),
            "No WebAssembly runtime is downloaded: the scanner runs on the server.");
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task StoppingOnACircuitReleasesTheCameraAndStartingOpensItAgain()
    {
        await using var session = await OpenOnServerAsync(SingleQrClip);
        await GoToAsync(session, "/");
        await session.WaitForStatusAsync(["scanning"], 60_000);

        await session.Page.ClickAsync(".bit-bbscanner__button:has-text('Stop')");
        await session.WaitForStatusAsync(["stopped"]);
        await WaitForLiveTracksAsync(session, 0);

        await session.Page.ClickAsync(".bit-bbscanner__button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.AreEqual(2, await session.TracksOpenedAsync());
    }

    [TestMethod]
    public async Task NavigatingAwayOnACircuitDisposesTheScannerAndReleasesTheCamera()
    {
        await using var session = await OpenOnServerAsync(SingleQrClip);
        await GoToAsync(session, "/");
        await session.WaitForStatusAsync(["scanning"], 60_000);

        await session.Page.ClickAsync("a[href='decoder']");
        await session.Page.WaitForURLAsync("**/decoder", new() { Timeout = 30_000 });

        await WaitForLiveTracksAsync(session, 0);
    }

    [TestMethod]
    public async Task ACameraTheCircuitLeftBehindIsClosedByThePage()
    {
        await using var session = await OpenOnServerAsync(SingleQrClip);
        await GoToAsync(session, "/");
        await session.WaitForStatusAsync(["scanning"], 60_000);
        Assert.AreEqual(1, await session.TracksLiveAsync());

        // The circuit goes away without the component being disposed - a server that went down, a connection that
        // never comes back - so nothing on the server can close the camera any more.
        await session.Page.EvaluateAsync("() => Blazor.disconnect()");

        await WaitForLiveTracksAsync(session, 0, timeoutMs: 45_000);
    }

    [TestMethod]
    public async Task TheScanImageButtonOpensTheFilePickerOnACircuit()
    {
        await using var session = await OpenOnServerAsync(BlankClip);
        await GoToAsync(session, "/image");
        await session.WaitForStatusAsync(["idle"], 60_000);

        // The click reaches the server and comes back a round trip later; the picker is opened in the page itself, in
        // the click's own event, so the browser still counts the click as the user's.
        var chooser = await session.Page.RunAndWaitForFileChooserAsync(
            () => session.Page.ClickAsync(".bit-bbscanner__button:has-text('Scan image')"),
            new() { Timeout = 10_000 });

        var png = await ScannerInputTests.RenderPngAsync(session, 260, 260, (QrEncoder.Encode("PICKED-ON-SERVER"), 20, 20));
        await chooser.SetFilesAsync(new FilePayload { Name = "picked.png", MimeType = "image/png", Buffer = png });

        await session.Page.Locator("table.results td.value", new() { HasText = "PICKED-ON-SERVER" }).WaitForAsync(new() { Timeout = 30_000 });
        Assert.AreEqual(0, await session.TracksOpenedAsync(), "Scanning an image needs no camera.");
    }

    [TestMethod]
    public async Task AnUploadIsScannedOnACircuitThroughInputFile()
    {
        await using var session = await OpenOnServerAsync(BlankClip);
        await GoToAsync(session, "/image");
        await session.WaitForStatusAsync(["idle"], 60_000);

        var png = await ScannerInputTests.RenderPngAsync(session, 260, 260, (QrEncoder.Encode("UPLOADED-TO-SERVER"), 20, 20));
        await session.Page.SetInputFilesAsync("#image-to-scan", new FilePayload { Name = "upload.png", MimeType = "image/png", Buffer = png });

        await session.Page.Locator("table.results td.value", new() { HasText = "UPLOADED-TO-SERVER" }).WaitForAsync(new() { Timeout = 30_000 });
    }

    /// <summary>
    /// Opens a page of the sample and waits for its circuit: the pages are prerendered, and a click on the prerendered
    /// markup goes nowhere. The layout's <c>data-interactive</c> turns true once the page runs on the circuit.
    /// </summary>
    private static async Task GoToAsync(ScannerSession session, string route)
    {
        await session.Page.GotoAsync($"{session.BaseUrl}/{route.TrimStart('/')}", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await session.Page.WaitForSelectorAsync("main[data-interactive='true']", new() { Timeout = 90_000 });
    }

    private static async Task WaitForLiveTracksAsync(ScannerSession session, int count, int timeoutMs = 15_000)
    {
        await session.Page.WaitForFunctionAsync("n => window.__bscanTracks.live.size === n", count, new() { Timeout = timeoutMs });
    }
}
