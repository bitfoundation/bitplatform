namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// BitDateRangePicker on a phone (/e2e/date-range-picker/responsive, a 390x844 touch viewport): the Responsive callout
/// is a full-width panel slid in at the top, a range is picked from it by two taps and a swipe up dismisses it; a
/// picker that is not Responsive still opens against its field, with its month picker, its time picker and its extra
/// months folded away to fit the narrow viewport.
/// </summary>
[TestClass]
public class BitDateRangePickerResponsiveE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-range-picker/responsive";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsChromium
    };

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtrp-inp");

    private async Task<string> TapOpenAsync(string rootId)
    {
        await NoCalloutOpenAsync();

        var pid = await PickerIdAsync(rootId);

        await Input(rootId).TapAsync();
        await Expect(Input(rootId)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Callout(pid)).ToHaveCSSAsync("opacity", "1");

        return pid;
    }

    [TestMethod]
    public async Task Responsive_TheCalloutIsAFullWidthPanelAtTheTopPickedByTaps()
    {
        await OpenAsync(Route);

        var pid = await TapOpenAsync("drp-resp");

        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dtrp-res"));
        await Expect(Callout(pid)).ToHaveCSSAsync("transform", "matrix(1, 0, 0, 1, 0, 0)");
        await SettleAsync(Callout(pid));
        var box = await BoxAsync(Callout(pid));
        Assert.AreEqual(0, box.X, 1);
        Assert.AreEqual(0, box.Y, 1);
        Assert.AreEqual(390, box.Width, 1);

        var day = await BoxAsync(Day(pid, Today));
        Assert.IsTrue(day.Width > 36, $"A day of the panel is a touch-sized cell ({day.Width}px).");

        await Day(pid, 2024, 6, 10).TapAsync();
        await Day(pid, 2024, 6, 13).TapAsync();
        await Expect(Page.Locator("#resp-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-13 23:59");
        await Expect(Input("drp-resp")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Callout(pid)).ToHaveCSSAsync("opacity", "0");
    }

    [TestMethod]
    public async Task Responsive_ASwipeUpDismissesThePanel()
    {
        if (IsFirefox || IsWebKit) Assert.Inconclusive("Firefox and WebKit have no Touch constructor to build the swipe's touch events with.");

        await OpenAsync(Route);

        var pid = await TapOpenAsync("drp-resp");

        await Page.EvaluateAsync(@"id => {
            const callout = document.getElementById(id);
            const target = callout.querySelector('.bit-dtrp-grp');
            const touch = (y) => new Touch({ identifier: 1, target, clientX: 195, clientY: y, screenX: 195, screenY: y, pageX: 195, pageY: y });
            const fire = (type, y) => target.dispatchEvent(new TouchEvent(type, {
                bubbles: true, cancelable: true,
                touches: type === 'touchend' ? [] : [touch(y)],
                targetTouches: type === 'touchend' ? [] : [touch(y)],
                changedTouches: [touch(y)]
            }));
            fire('touchstart', 400);
            fire('touchmove', 390);
            fire('touchmove', 40);
            fire('touchend', 40);
        }", $"{pid}-callout");

        await Expect(Input("drp-resp")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#resp-closes")).ToHaveTextAsync("1");
        await Expect(Callout(pid)).ToHaveCSSAsync("opacity", "0");
    }

    [TestMethod]
    public async Task Narrow_TheMonthPickerTimePickerAndExtraMonthsFoldAway()
    {
        await OpenAsync(Route);

        var pid = await TapOpenAsync("drp-narrow");
        await Expect(Callout(pid)).Not.ToHaveClassAsync(HasClass("bit-dtrp-res"));
        await Expect(Callout(pid).Locator(".bit-dtrp-mwp")).ToHaveCountAsync(0);
        var field = await BoxAsync(Page.Locator("#drp-narrow .bit-dtrp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        Assert.IsTrue(Math.Abs(callout.Y - (field.Y + field.Height + 1)) <= 2, "Not Responsive, the callout still drops from its field.");
        Assert.IsTrue(callout.X >= 0 && callout.X + callout.Width <= 390 + 1, "The callout fits the screen.");

        var title = Callout(pid).Locator(".bit-dtrp-dwp .bit-dtrp-ptb");
        await title.TapAsync();
        await Expect(Callout(pid).Locator(".bit-dtrp-mwp")).ToBeVisibleAsync();
        await MonthCell(pid, 9).TapAsync();
        await Expect(title).ToHaveTextAsync("September 2024");
        await Page.WaitForFunctionAsync("sel => document.querySelector(sel)?.contains(document.activeElement)", $"#{pid}-callout");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Input("drp-narrow")).ToHaveAttributeAsync("aria-expanded", "false");

        var time = await TapOpenAsync("drp-narrow-time");
        await Expect(Callout(time).Locator(".bit-dtrp-twp")).ToHaveCountAsync(0);
        await Callout(time).Locator(".bit-dtrp-nbt[title='Show time picker']").TapAsync();
        await Expect(Callout(time).Locator(".bit-dtrp-twp")).ToBeVisibleAsync();

        // The clock button went away with the picker it headed; Escape is pressed once the focus has been handed on.
        await Page.WaitForFunctionAsync("sel => document.querySelector(sel)?.contains(document.activeElement)", $"#{time}-callout");
        await Page.Keyboard.PressAsync("Escape");

        var two = await TapOpenAsync("drp-narrow-two");
        await Expect(Callout(two).Locator(".bit-dtrp-dwp")).ToHaveCountAsync(1);
    }
}
