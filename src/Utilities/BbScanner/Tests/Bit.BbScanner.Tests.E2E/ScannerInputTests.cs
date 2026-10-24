using Microsoft.Playwright;
using Bit.BbScanner.Tests.E2E.Infrastructure;
using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Tests.E2E;

/// <summary>
/// The ways an image reaches the scanner other than the live camera - dropped onto it, handed over by the
/// application through <c>ScanImageAsync</c>, or captured from the camera at full resolution - and what the scanner
/// does with a scan once it has one.
/// </summary>
[TestClass]
public class ScannerInputTests : BbScannerTest
{
    [TestMethod]
    public async Task AnImageDroppedOntoTheScannerIsScanned()
    {
        await using var session = await OpenAsync(BlankClip);
        await session.GoToAsync("image");
        var png = await RenderPngAsync(session, 260, 260, (QrEncoder.Encode("DROPPED"), 20, 20));

        var dataTransfer = await session.Page.EvaluateHandleAsync(
            @"b64 => {
                const bytes = Uint8Array.from(atob(b64), c => c.charCodeAt(0));
                const transfer = new DataTransfer();
                transfer.items.add(new File([bytes], 'dropped.png', { type: 'image/png' }));
                return transfer;
            }",
            Convert.ToBase64String(png));

        await session.Page.DispatchEventAsync(".bit-bbscanner", "dragenter", new { dataTransfer });
        await session.Page.DispatchEventAsync(".bit-bbscanner", "dragover", new { dataTransfer });
        Assert.AreEqual("true", await session.Page.GetAttributeAsync(".bit-bbscanner", "data-drop"), "The scanner shows it will take the file.");

        await session.Page.DispatchEventAsync(".bit-bbscanner", "drop", new { dataTransfer });
        await session.WaitForResultsAsync(1, timeoutMs: 30_000);

        Assert.AreEqual("DROPPED", (await session.ResultValuesAsync())[0]);
        Assert.IsNull(await session.Page.GetAttributeAsync(".bit-bbscanner", "data-drop"));
        Assert.AreEqual(0, await session.TracksOpenedAsync(), "Scanning an image must not open the camera.");
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task AnImageFromInputFileIsScannedWithoutTheCamera()
    {
        await using var session = await OpenAsync(BlankClip);
        await session.GoToAsync("image");
        var png = await RenderPngAsync(session, 260, 260, (QrEncoder.Encode("UPLOADED"), 20, 20));

        await session.Page.SetInputFilesAsync("#scan-image-input", new FilePayload { Name = "upload.png", MimeType = "image/png", Buffer = png });
        await session.WaitForResultsAsync(1, timeoutMs: 30_000);

        Assert.AreEqual("UPLOADED", (await session.ResultValuesAsync())[0]);
        Assert.AreEqual(0, await session.TracksOpenedAsync(), "ScanImageAsync needs no camera, and so no permission prompt.");
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task EverySymbolInAPickedImageIsReportedWhenAskedFor()
    {
        await using var session = await OpenAsync(BlankClip);
        await session.GoToAsync("image");
        await session.Page.CheckAsync("#every-symbol");
        var png = await RenderPngAsync(session, 560, 260, (QrEncoder.Encode("LEFT-CODE"), 20, 20), (QrEncoder.Encode("RIGHT-CODE"), 300, 20));

        await session.Page.SetInputFilesAsync(".bit-bbscanner input[type=file]", new FilePayload { Name = "two.png", MimeType = "image/png", Buffer = png });
        await session.WaitForResultsAsync(2, timeoutMs: 30_000);

        CollectionAssert.AreEquivalent(new[] { "LEFT-CODE", "RIGHT-CODE" }, (await session.ResultValuesAsync()).ToArray());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task StopAfterAScanReleasesTheCamera()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);

        await session.Page.SelectOptionAsync("#after-scan", "Stop");
        var before = (await session.ResultValuesAsync()).Count;

        // The clip moves on to the next symbol within a couple of seconds, and that scan stops the scanner.
        await session.WaitForStatusAsync(["stopped"], timeoutMs: 60_000);

        Assert.IsGreaterThan(before, (await session.ResultValuesAsync()).Count);
        Assert.AreEqual(0, await session.TracksLiveAsync(), "Stopping after the scan released the camera.");
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task OnlyResultsTheFilterAcceptsAreReported()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("lifecycle");

        // Every frame that decodes is reported, so anything the filter let through would show up many times over.
        await SetValueAsync(session, "input[type=range]", "0");
        await SetValueAsync(session, "#accept-prefix", FakeCameraVideo.DataMatrixPayload);
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);

        await session.WaitForResultsAsync(5, timeoutMs: 60_000);

        var values = await session.ResultValuesAsync();
        Assert.IsTrue(values.All(v => v == FakeCameraVideo.DataMatrixPayload), $"Reported: {string.Join(", ", values.Distinct())}");
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task OncePerSessionReportsEachValueOnceHoweverOftenItComesBack()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("lifecycle");
        await session.Page.CheckAsync("#once-per-session");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);

        // The clip shows six symbols in turn; once all six are in, give it another full loop - about ten
        // seconds - in which nothing new may be reported.
        await session.WaitForResultsAsync(6, timeoutMs: 60_000);
        await session.Page.WaitForTimeoutAsync(11_000);

        var values = await session.ResultValuesAsync();
        Assert.HasCount(6, values, $"Reported: {string.Join(", ", values)}");
        Assert.HasCount(6, values.Distinct().ToList());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task TheCurrentFrameCanBeScannedAtFullResolution()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("camera");
        await session.WaitForStatusAsync(["scanning"]);

        await session.Page.ClickAsync("#scan-full-frame");
        var result = session.Page.Locator("#full-frame-result");
        await result.Filter(new() { HasText = "SINGLE-QR" }).WaitForAsync(new() { Timeout = 30_000 });

        Assert.IsEmpty(session.PageErrors);
    }

    /// <summary>Sets an input's value and raises the change event Blazor's bindings listen for.</summary>
    private static Task SetValueAsync(ScannerSession session, string selector, string value) =>
        session.Page.Locator(selector).First.EvaluateAsync(
            "(element, value) => { element.value = value; element.dispatchEvent(new Event('change', { bubbles: true })); }",
            value);

    /// <summary>
    /// Draws symbols on a white canvas in the page and returns it as a PNG, so the suite needs no image library.
    /// Each symbol is drawn at six pixels a module from its top-left position, quiet zone included.
    /// </summary>
    private static async Task<byte[]> RenderPngAsync(ScannerSession session, int width, int height, params (BbBitMatrix Matrix, int X, int Y)[] symbols)
    {
        var shapes = symbols.Select(symbol =>
        {
            var rows = new List<string>(symbol.Matrix.Height);
            for (var y = 0; y < symbol.Matrix.Height; y++)
            {
                rows.Add(string.Concat(Enumerable.Range(0, symbol.Matrix.Width).Select(x => symbol.Matrix[x, y] ? '1' : '0')));
            }

            symbol.Matrix.Dispose();
            return new { x = symbol.X, y = symbol.Y, rows };
        }).ToArray();

        var base64 = await session.Page.EvaluateAsync<string>(
            @"({ width, height, shapes }) => {
                const scale = 6, quiet = 4;
                const canvas = document.createElement('canvas');
                canvas.width = width; canvas.height = height;
                const ctx = canvas.getContext('2d');
                ctx.fillStyle = '#fff'; ctx.fillRect(0, 0, width, height);
                ctx.fillStyle = '#000';
                for (const shape of shapes) {
                    for (let y = 0; y < shape.rows.length; y++) {
                        for (let x = 0; x < shape.rows[y].length; x++) {
                            if (shape.rows[y][x] === '1') {
                                ctx.fillRect(shape.x + (x + quiet) * scale, shape.y + (y + quiet) * scale, scale, scale);
                            }
                        }
                    }
                }
                return canvas.toDataURL('image/png').split(',')[1];
            }",
            new { width, height, shapes });

        return Convert.FromBase64String(base64);
    }
}
