namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives the ResponsiveMode of BitCallout on a phone (/e2e/callout/responsive, a 390x844 touch viewport): the Panel
/// sliding in full height from the PanelPlacement edge (End by default, Start, and the physical Left and Right, which
/// stay put in RTL while Start and End follow it), the Top and Bottom sheets, MaxHeight lifted for the panel, the arrow
/// left out, and the panel dismissed by a swipe towards its edge, kept by a swipe the other way, and dismissed by a tap
/// outside it.
/// </summary>
[TestClass]
public class BitCalloutResponsiveE2ETests : E2ETestBase
{
    private const string Url = "/e2e/callout/responsive";
    private const double Width = 390;
    private const double Height = 844;

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = (int)Width, Height = (int)Height },
        HasTouch = true,
    };

    private ILocator Callout => Page.Locator(".bit-clo-cal:has(> #res-body)");

    [TestMethod]
    public async Task Panel_ByDefault_SlidesInFullHeightFromTheEndEdge()
    {
        await OpenAsync($"{Url}?mode=Panel");

        await OpenPanelAsync();

        await Expect(Callout).ToHaveClassAsync(HasClass("bit-clo-res"));
        await Expect(Callout).ToHaveClassAsync(HasClass("bit-clo-end"));
        var box = await BoxAsync(Callout);
        Near(Width, box.X + box.Width, "on the right edge");
        Near(0, box.Y, "from the top");
        Near(Height, box.Height, "full height");
        Assert.IsTrue(box.X > 0, "The panel covers the whole width.");

        // A panel is sized against the screen, so there is nothing for an arrow to point at.
        await Expect(Page.Locator(".bit-clo-arw")).ToBeHiddenAsync();
    }

    [TestMethod]
    [DataRow("Start", false, "left")]
    [DataRow("End", false, "right")]
    [DataRow("Start", true, "right")]
    [DataRow("End", true, "left")]
    [DataRow("Left", true, "left")]
    [DataRow("Right", true, "right")]
    [DataRow("Left", false, "left")]
    [DataRow("Right", false, "right")]
    public async Task PanelPlacement_PutsThePanelOnItsEdge(string placement, bool rtl, string edge)
    {
        await OpenAsync($"{Url}?mode=Panel&panel={placement}&rtl={rtl.ToString().ToLowerInvariant()}");

        await OpenPanelAsync();

        var box = await BoxAsync(Callout);
        if (edge == "left")
        {
            Near(0, box.X, "on the left edge");
        }
        else
        {
            Near(Width, box.X + box.Width, "on the right edge");
        }

        Near(Height, box.Height, "full height");
    }

    [TestMethod]
    public async Task TopSheet_ComesDownFullWidthAndAsTallAsItsContent()
    {
        await OpenAsync($"{Url}?mode=Top");

        await OpenPanelAsync();

        await Expect(Callout).ToHaveClassAsync(HasClass("bit-clo-top"));
        var box = await BoxAsync(Callout);
        Near(0, box.Y, "at the top");
        Near(0, box.X, "from the left");
        Near(Width, box.Width, "full width");
        Assert.IsTrue(box.Height < Height / 2, $"The sheet is {box.Height}px tall.");
    }

    [TestMethod]
    public async Task BottomSheet_ComesUpFullWidthAndAsTallAsItsContent()
    {
        await OpenAsync($"{Url}?mode=Bottom");

        await OpenPanelAsync();

        await Expect(Callout).ToHaveClassAsync(HasClass("bit-clo-btm"));
        var box = await BoxAsync(Callout);
        Near(Height, box.Y + box.Height, "at the bottom");
        Near(Width, box.Width, "full width");
        Assert.IsTrue(box.Height < Height / 2, $"The sheet is {box.Height}px tall.");
    }

    [TestMethod]
    public async Task MaxHeight_IsLiftedForThePanel()
    {
        await OpenAsync($"{Url}?mode=Panel&capped=true");

        await OpenPanelAsync();

        await Expect(Callout).ToHaveClassAsync(HasClass("bit-clo-mxh"));
        Near(Height, (await BoxAsync(Callout)).Height, "full height despite MaxHeight");
    }

    [TestMethod]
    public async Task Swipe_TheEndPanelTowardsItsEdge_DismissesIt()
    {
        await OpenAsync($"{Url}?mode=Panel");
        await OpenPanelAsync();
        await SkipWithoutTouchEventsAsync();

        var box = await BoxAsync(Callout);
        await SwipeAsync(box.Width * 0.6, 0);

        await Expect(Page.Locator("#res-dismissed")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#res")).Not.ToHaveClassAsync(HasClass("bit-clo-opn"));
        await PollAsync(async () => (await BoxAsync(Callout)).X, x => x >= Width - 1, "the panel did not slide out");
    }

    [TestMethod]
    public async Task Swipe_TheEndPanelAwayFromItsEdge_KeepsItOpen()
    {
        await OpenAsync($"{Url}?mode=Panel");
        await OpenPanelAsync();
        await SkipWithoutTouchEventsAsync();

        var before = await BoxAsync(Callout);
        await SwipeAsync(-before.Width * 0.6, 0);
        await NextFramesAsync();

        await Expect(Page.Locator("#res-dismissed")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#res")).ToHaveClassAsync(HasClass("bit-clo-opn"));
        await PollAsync(async () => (await BoxAsync(Callout)).X, x => Math.Abs(x - before.X) < 1, "the panel did not settle back");
    }

    [TestMethod]
    public async Task Swipe_TheBottomSheetDown_DismissesIt()
    {
        await OpenAsync($"{Url}?mode=Bottom");
        await OpenPanelAsync();
        await SkipWithoutTouchEventsAsync();

        var box = await BoxAsync(Callout);
        await SwipeAsync(0, box.Height * 0.8);

        await Expect(Page.Locator("#res-dismissed")).ToHaveTextAsync("1");
        await PollAsync(async () => (await BoxAsync(Callout)).Y, y => y >= Height - 1, "the sheet did not slide out");
    }

    [TestMethod]
    public async Task Tap_OutsideThePanel_DismissesIt()
    {
        await OpenAsync($"{Url}?mode=Panel");
        await OpenPanelAsync();

        var box = await BoxAsync(Callout);
        await Page.Touchscreen.TapAsync((float)(box.X / 2), (float)(Height / 2));

        await Expect(Page.Locator("#res-dismissed")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#res")).Not.ToHaveClassAsync(HasClass("bit-clo-opn"));
    }

    private async Task OpenPanelAsync()
    {
        await Page.Locator("#res-trigger").TapAsync();

        await Expect(Page.Locator("#res")).ToHaveClassAsync(HasClass("bit-clo-opn"));
        await Expect(Page.Locator("#res-opened")).ToHaveTextAsync("1");
        await PollAsync(() => Callout.EvaluateAsync<bool>("el => el.getAnimations().length === 0 && getComputedStyle(el).opacity === '1'"), done => done, "the panel never settled");
    }

    private async Task SkipWithoutTouchEventsAsync()
    {
        if (await Page.EvaluateAsync<bool>("() => typeof Touch === 'function' && typeof TouchEvent === 'function'") is false)
        {
            Assert.Inconclusive($"{BrowserName} cannot construct the touch events a swipe is driven with.");
        }
    }

    // A touch drag across the callout, as the swipe code hears it on a touch device: touchstart, a run of touchmoves
    // a frame apart and a touchend.
    private Task SwipeAsync(double dx, double dy)
    {
        return Callout.EvaluateAsync(@"async (el, [dx, dy]) => {
            const r = el.getBoundingClientRect();
            const x0 = r.left + r.width / 2;
            const y0 = r.top + Math.min(r.height / 2, 40);
            const touch = (x, y) => new Touch({ identifier: 1, target: el, clientX: x, clientY: y, screenX: x, screenY: y, pageX: x, pageY: y });
            const fire = (type, x, y, ended) => {
                const t = touch(x, y);
                el.dispatchEvent(new TouchEvent(type, {
                    bubbles: true, cancelable: true,
                    touches: ended ? [] : [t], targetTouches: ended ? [] : [t], changedTouches: [t]
                }));
            };
            fire('touchstart', x0, y0);
            const steps = 10;
            for (let i = 1; i <= steps; i++) {
                await new Promise(resolve => requestAnimationFrame(resolve));
                fire('touchmove', x0 + dx * i / steps, y0 + dy * i / steps);
            }
            await new Promise(resolve => requestAnimationFrame(resolve));
            fire('touchend', x0 + dx, y0 + dy, true);
        }", new[] { dx, dy });
    }

    private static async Task<LayoutBox> BoxAsync(ILocator locator)
    {
        var box = await locator.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x, r.y, r.width, r.height]; }");
        return new(box[0], box[1], box[2], box[3]);
    }

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance, $"{what}: expected {expected}, got {actual}.");
    }

    private async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> done, string message)
    {
        var started = DateTime.UtcNow;
        T value;
        do
        {
            value = await read();
            if (done(value)) return value;
            await NextFramesAsync();
        } while (DateTime.UtcNow - started < TimeSpan.FromMilliseconds(DefaultTimeout));

        Assert.Fail($"{message} (last value: {value})");
        return value;
    }

    private sealed record LayoutBox(double X, double Y, double Width, double Height);
}
