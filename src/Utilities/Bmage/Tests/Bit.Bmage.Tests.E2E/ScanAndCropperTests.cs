using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// The document scanning page, which measures, levels and thresholds a photographed page and reads its QR code through
/// Bit.Bmage.BbScanner, and the editor reduced to a cropper by its Tools and Panels parameters.
/// </summary>
[TestClass]
public class ScanAndCropperTests : DemoTest
{
    private static async Task AssertCleanAsync(IPage page, List<string> errors)
    {
        Assert.IsEmpty(errors, string.Join(" | ", errors));
        Assert.IsEmpty(await page.Locator(".demo-error").AllInnerTextsAsync());
    }

    [TestMethod]
    public async Task ASamplePageIsLevelledCleanedReadAndSavedSmall()
    {
        var page = await OpenAsync("scan");
        var errors = CollectErrors(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Use a sample page" }).ClickAsync(new LocatorClickOptions { Timeout = 60_000 });

        // The sample is turned four degrees anticlockwise, and its QR code reads.
        var codes = page.Locator("[data-testid=codes]");
        await codes.GetByText("https://bitplatform.dev/bmage/scan").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
        StringAssert.Contains(await page.Locator("[data-testid=tilt]").InnerTextAsync(), "anticlockwise");
        var tilt = double.Parse((await page.Locator("[data-testid=tilt]").InnerTextAsync()).Split('°')[0], System.Globalization.CultureInfo.InvariantCulture);
        Assert.AreEqual(4.0, tilt, 0.3);

        // Black and white in a 1-bit PNG: a small fraction of the 850 x 1100 page's 3.7 MB of pixels.
        await page.WaitForSelectorAsync("img.scan-preview", new PageWaitForSelectorOptions { Timeout = 60_000 });
        var size = await page.Locator("[data-testid=scan-size]").InnerTextAsync();
        StringAssert.Contains(size, "KB");
        Assert.IsLessThan(120, double.Parse(size.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture), size);

        // Turning the clean-up off shows the photo in grey, and on again the scan comes back.
        await page.Locator("#clean").UncheckAsync();
        await page.WaitForFunctionAsync("() => !document.querySelector('.working')", null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        await page.Locator("#clean").CheckAsync();
        await page.WaitForFunctionAsync("() => !document.querySelector('.working')", null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        await AssertCleanAsync(page, errors);
    }

    [TestMethod]
    public async Task TheEditorReducedToACropperOffersOnlyItsToolsAndNoPanel()
    {
        var page = await OpenAsync("editor");
        var errors = CollectErrors(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Load a sample image" }).ClickAsync(new LocatorClickOptions { Timeout = 60_000 });
        var cropper = page.GetByRole(AriaRole.Region, new() { Name = "Cropper", Exact = true });
        await cropper.Locator(".bmg-canvas").WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });

        var tools = cropper.GetByRole(AriaRole.Group, new() { Name = "Tools", Exact = true }).GetByRole(AriaRole.Button);
        Assert.AreEqual(2, await tools.CountAsync());
        Assert.AreEqual("Crop", await tools.Nth(0).GetAttributeAsync("aria-label"));
        Assert.AreEqual("Pan", await tools.Nth(1).GetAttributeAsync("aria-label"));
        Assert.AreEqual("true", await tools.Nth(0).GetAttributeAsync("aria-pressed"), "The crop tool is chosen once the image loads.");
        Assert.AreEqual(0, await cropper.Locator(".bmg-panel").CountAsync(), "No panel.");

        // The crop it makes keeps to the square it was given.
        await cropper.GetByRole(AriaRole.Button, new() { Name = "Apply crop" }).ClickAsync();
        await page.WaitForFunctionAsync("""
            () => {
                const c = document.querySelector('[aria-label="Cropper"] .bmg-canvas');
                return c && Math.abs(c.width - c.height) <= 1;
            }
            """, null, new PageWaitForFunctionOptions { Timeout = 60_000 });

        // The full editor above it still has every tool.
        var full = page.GetByRole(AriaRole.Region, new() { Name = "Image editor", Exact = true });
        Assert.AreEqual(0, await full.Locator(".bmg-canvas").CountAsync(), "Loading the cropper leaves the editor above it empty.");
        await AssertCleanAsync(page, errors);
    }
}
