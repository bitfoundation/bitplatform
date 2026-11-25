namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette on a phone: a 390px touch viewport, where the dialog fills the screen, the detail pane is hidden
/// rather than squeezed next to the list, and nothing overflows sideways.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPalettePhoneE2ETests : BitCommandPaletteE2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        IsMobile = IsFirefox is false,
        HasTouch = true,
        ReducedMotion = ReducedMotion.Reduce,
    };

    /// <summary>How far the document is wider than the viewport, in CSS pixels.</summary>
    private Task<double> HorizontalOverflowAsync() =>
        Page.EvaluateAsync<double>("() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");

    [TestMethod]
    public async Task OnAPhone_TheDialogFillsTheScreen_AndNothingOverflowsSideways()
    {
        await OpenPageAsync();
        await Page.Locator("#cpl-detail").TapAsync();
        await Expect(Page.Locator("#cpl-detail")).ToBeCheckedAsync();
        Assert.AreEqual(0d, await HorizontalOverflowAsync(), "the page overflows the phone's width before the palette opens");

        await Page.Locator("#cpl-trigger").TapAsync();
        await ExpectOpenAsync();
        await TypeAsync("a");
        await Expect(Items.First).ToBeVisibleAsync();
        // At rest: the box of the dialog's pop-in is not its own.
        await Dialog.EvaluateAsync("d => Promise.all(d.getAnimations({ subtree: true }).map(a => a.finished.catch(() => {})))");

        var box = await Dialog.EvaluateAsync<double[]>("e => { const r = e.getBoundingClientRect(); return [r.left, r.top, r.width, r.height]; }");
        TestContext.WriteLine($"dialog box: {string.Join(", ", box)}");
        AssertInRange(box[0], -0.5, 0.5, "the dialog's left edge");
        AssertInRange(box[1], -0.5, 0.5, "the dialog's top edge");
        AssertInRange(box[2], 389.5, 390.5, "the dialog's width");
        AssertInRange(box[3], 843.5, 844.5, "the dialog's height");

        // Below the small breakpoint the detail pane is hidden rather than squeezed next to the list.
        await Expect(Dialog).ToHaveAttributeAsync("data-has-detail", "true");
        await Expect(Detail).ToBeHiddenAsync();
        Assert.AreEqual(0d, await HorizontalOverflowAsync(), "the page overflows the phone's width with the palette open");
        Assert.IsTrue(await Dialog.EvaluateAsync<bool>("d => d.scrollWidth <= d.clientWidth"), "the dialog's content is wider than the dialog");
    }
}
