namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drags the gutter of BitSplitter (/e2e/splitter) with a finger on a touch device: the touch the browser turns into
/// pointer events moves the split and is reported like a mouse drag, and the gutter keeps the page from scrolling
/// under it (touch-action: none). The touch points are dispatched through the Chromium DevTools protocol, which is
/// the one way to drive a real touch sequence; the other engines are skipped.
/// </summary>
[TestClass]
public class BitSplitterTouchE2ETests : E2ETestBase
{
    private const string Url = "/e2e/splitter";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 1024, Height = 768 },
        HasTouch = true,
    };

    [TestMethod]
    public async Task Touch_AFingerDragsTheGutter_AndTheResizeIsReported()
    {
        if (IsChromium is false) Assert.Inconclusive($"A touch drag is dispatched through the Chromium DevTools protocol, which {BrowserName} does not have.");

        await OpenAsync(Url);

        var gutter = Page.Locator("#spl-basic > .bit-spl-gtr");
        await Expect(gutter).ToHaveCSSAsync("touch-action", "none");
        var box = await gutter.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        var x = box[0] + box[2] / 2;
        var y = box[1] + 20;

        var session = await Page.Context.NewCDPSessionAsync(Page);
        await TouchAsync(session, "touchStart", x, y);
        for (var i = 1; i <= 8; i++)
        {
            await TouchAsync(session, "touchMove", x + i * 12.5, y);
        }

        await TouchAsync(session, "touchEnd", null, null);

        await Expect(Page.Locator("#ev-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ev-end")).ToHaveTextAsync("65.83");
        var width = await Page.Locator("#spl-basic > .bit-spl-fpn").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.AreEqual(395, width, 1.5);
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollX"));
    }

    private static Task TouchAsync(ICDPSession session, string type, double? x, double? y)
    {
        object[] points = x is null ? [] : [new Dictionary<string, object> { ["x"] = x.Value, ["y"] = y!.Value, ["id"] = 1 }];

        return session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = points });
    }
}
