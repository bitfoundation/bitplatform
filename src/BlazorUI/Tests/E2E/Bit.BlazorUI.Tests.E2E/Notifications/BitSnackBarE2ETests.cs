using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// BitSnackBar in a real browser: the host and every API of it on /e2e/snack-bar (configured per test through the
/// query string, with Position, Variant and Size changed at runtime), BitSnackBarService on /e2e/snack-bar/service,
/// the BitParams cascade on /e2e/snack-bar/cascade and Escape inside a dialog on /e2e/snack-bar/dialog. It covers the
/// prerendered region, the color shortcuts and Show, every position on the screen in both directions (explicit and
/// inherited) and the offset, the enter/exit transitions and reduced motion, auto dismiss with its bar (reversed,
/// hidden, per item) and every hold (hover, focus, Pause, a blurred or hidden page), persistence, swipe to dismiss,
/// stacking (MaxItems with each overflow behavior, NewestOnTop, PreventDuplicates), icons, actions, multiline and
/// max width, templates, events and dismiss reasons, loading items, Update and Track, Close/Clear/ClearOnNavigation,
/// Escape and the dismiss button with the focus handed on, the live regions, the hotkey and FocusAsync, every color,
/// variant and size, Style/Class/Styles/Classes, the public CSS variables, and unmounting without an error.
/// </summary>
[TestClass]
public class BitSnackBarE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private static readonly (string Name, string Class, string Vertical, string Horizontal, string RtlHorizontal)[] Positions =
    [
        ("TopStart", "bit-snb-tst", "top", "left", "right"),
        ("TopCenter", "bit-snb-tcn", "top", "center", "center"),
        ("TopEnd", "bit-snb-ten", "top", "right", "left"),
        ("TopLeft", "bit-snb-tlf", "top", "left", "left"),
        ("TopRight", "bit-snb-trg", "top", "right", "right"),
        ("CenterStart", "bit-snb-cst", "center", "left", "right"),
        ("Center", "bit-snb-ctr", "center", "center", "center"),
        ("CenterEnd", "bit-snb-cen", "center", "right", "left"),
        ("CenterLeft", "bit-snb-clf", "center", "left", "left"),
        ("CenterRight", "bit-snb-crg", "center", "right", "right"),
        ("BottomStart", "bit-snb-bst", "bottom", "left", "right"),
        ("BottomCenter", "bit-snb-bcn", "bottom", "center", "center"),
        ("BottomEnd", "bit-snb-ben", "bottom", "right", "left"),
        ("BottomLeft", "bit-snb-blf", "bottom", "left", "left"),
        ("BottomRight", "bit-snb-brg", "bottom", "right", "right"),
    ];

    private static readonly Dictionary<string, string> FgTokens = new()
    {
        ["pri"] = "--bit-clr-pri-fg", ["sec"] = "--bit-clr-sec-fg", ["ter"] = "--bit-clr-ter-fg", ["inf"] = "--bit-clr-inf-fg",
        ["suc"] = "--bit-clr-suc-fg", ["wrn"] = "--bit-clr-wrn-fg", ["swr"] = "--bit-clr-swr-fg", ["err"] = "--bit-clr-err-fg",
        ["pbg"] = "--bit-clr-fg-pri", ["sbg"] = "--bit-clr-fg-pri", ["tbg"] = "--bit-clr-fg-pri",
        ["pfg"] = "--bit-clr-fg-pri", ["sfg"] = "--bit-clr-fg-sec", ["tfg"] = "--bit-clr-fg-ter",
        ["pbr"] = "--bit-clr-fg-pri", ["sbr"] = "--bit-clr-fg-pri", ["tbr"] = "--bit-clr-fg-pri",
    };

    private static Regex Any => new(".*");

    private ILocator Items => Page.Locator("#snb .bit-snb-itm");

    private ILocator Item(string text) => Page.Locator("#snb .bit-snb-itm", new() { HasText = text });

    private Task OpenSnackBarAsync(string query = "") => OpenAsync($"/e2e/snack-bar{(query.Length > 0 ? "?" + query : "")}");

    private Task SettleAsync(ILocator item) => item.EvaluateAsync("el => Promise.all(el.getAnimations().map(a => a.finished.catch(() => null)))");

    private async Task<(double Width, double Height)> ScreenAsync()
    {
        var size = await Page.EvaluateAsync<double[]>("() => [document.documentElement.clientWidth, document.documentElement.clientHeight]");
        return (size[0], size[1]);
    }

    private static string Vertical(Box item, double height, double offset)
    {
        if (Math.Abs(item.Top - offset) <= 1.5) return "top";
        if (Math.Abs(height - offset - item.Bottom) <= 1.5) return "bottom";
        if (Math.Abs(item.CenterY - height / 2) <= 1.5) return "center";
        return $"? ({item.Top}-{item.Bottom} of {height})";
    }

    private static string Horizontal(Box item, double width, double offset)
    {
        if (Math.Abs(item.Left - offset) <= 1.5) return "left";
        if (Math.Abs(width - offset - item.Right) <= 1.5) return "right";
        if (Math.Abs(item.CenterX - width / 2) <= 1.5) return "center";
        return $"? ({item.Left}-{item.Right} of {width})";
    }

    private async Task<double> ScaleXAsync(ILocator element)
    {
        var transform = await element.EvaluateAsync<string>("el => getComputedStyle(el).transform");
        if (transform is "none") return 1;

        return double.Parse(transform[(transform.IndexOf('(') + 1)..transform.IndexOf(',')], CultureInfo.InvariantCulture);
    }

    [TestMethod]
    public async Task Prerender_TheRegionAndItsLiveRegions_AreInTheServerHtml()
    {
        var html = await Page.PrerenderedHtmlAsync($"{BaseUrl}/e2e/snack-bar");

        StringAssert.Contains(html, "id=\"snb\"");
        StringAssert.Contains(html, "role=\"region\"");
        StringAssert.Contains(html, "aria-label=\"Notifications\"");
        StringAssert.Contains(html, "aria-live=\"polite\"");
        StringAssert.Contains(html, "aria-live=\"assertive\"");

        await OpenSnackBarAsync();
        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Page.Locator("#snb")).ToHaveCSSAsync("position", "fixed");
        await Expect(Page.Locator("#snb")).ToHaveCSSAsync("pointer-events", "none");
    }

    [TestMethod]
    public async Task Basic_TheShortcutsAndShow_ShowItemsInTheirColor_ThatStayUntilDismissed()
    {
        await OpenSnackBarAsync();

        var expected = new (string Button, string Kind, string Suffix, string Token)[]
        {
            ("#btn-info", "Info", "inf", "--bit-clr-inf"), ("#btn-success", "Success", "suc", "--bit-clr-suc"),
            ("#btn-warning", "Warning", "wrn", "--bit-clr-wrn"), ("#btn-severe", "SevereWarning", "swr", "--bit-clr-swr"),
            ("#btn-error", "Error", "err", "--bit-clr-err"),
        };

        var n = 0;
        foreach (var (button, kind, suffix, token) in expected)
        {
            await Page.ClickAsync(button);
            var item = Item($"{kind} {++n}");
            await Expect(item).ToBeVisibleAsync();
            await Expect(item).ToHaveClassAsync(HasClass($"bit-snb-{suffix}"));
            await Expect(item).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync(token));
            await Expect(item.Locator(".bit-snb-ttl")).ToHaveTextAsync($"{kind} {n}");
            await Expect(item.Locator(".bit-snb-bdy")).ToHaveTextAsync($"{kind} body");
        }

        // Show takes any color, and none is the Info look.
        await Page.ClickAsync("#btn-plain");
        var plain = Item("Plain 6");
        await Expect(plain).ToBeVisibleAsync();
        await Expect(plain).Not.ToHaveClassAsync(new Regex("bit-snb-(pri|inf|suc|wrn|swr|err)\\b"));
        await Expect(plain).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-inf"));

        await Page.ClickAsync("#btn-title-only");
        await Expect(Item("Only title").Locator(".bit-snb-bdy")).ToHaveCountAsync(0);

        await Expect(Items).ToHaveCountAsync(7);
        await Expect(Page.Locator("#shown-count")).ToHaveTextAsync("7");
        await Expect(Page.Locator("#items-count")).ToHaveTextAsync("7");

        // Without AutoDismiss nothing counts down.
        await Expect(Page.Locator("#snb .bit-snb-prb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Position_TheDefault_IsTheBottomEndCorner_InTheOrderTheItemsArrive()
    {
        await OpenSnackBarAsync();

        await Expect(Page.Locator("#snb")).ToHaveClassAsync(HasClass("bit-snb-ben"));

        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-success");
        await SettleAsync(Item("Info 1"));
        await SettleAsync(Item("Success 2"));

        var (width, height) = await ScreenAsync();
        var newest = await Item("Success 2").BoxAsync();
        var oldest = await Item("Info 1").BoxAsync();
        Assert.AreEqual("bottom", Vertical(newest, height, 8));
        Assert.AreEqual("right", Horizontal(newest, width, 8));
        Assert.IsTrue(oldest.Bottom <= newest.Top, "The newest item joins the bottom of the stack.");
        Assert.AreEqual(10, newest.Top - oldest.Bottom, 0.5, "Items are stacked spacing(1.25) apart.");
    }

    [TestMethod]
    public async Task Position_EachOfTheFifteen_PinsTheStackToItsPlaceOnTheScreen()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-persistent-item");
        var item = Item("Persistent 1");
        await Expect(item).ToBeVisibleAsync();
        var (width, height) = await ScreenAsync();

        var failures = new List<string>();
        foreach (var (name, @class, vertical, horizontal, _) in Positions)
        {
            await Page.SelectOptionAsync("#sel-position", name);
            await Expect(Page.Locator("#snb")).ToHaveClassAsync(HasClass(@class));
            await SettleAsync(item);

            var box = await item.BoxAsync();
            var v = Vertical(box, height, 8);
            var h = Horizontal(box, width, 8);
            if (v != vertical || h != horizontal) failures.Add($"{name}: expected {vertical}/{horizontal}, got {v}/{h}");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public async Task Position_RightToLeft_StartAndEndFollowTheDirection_ExplicitAndInherited()
    {
        foreach (var query in new[] { "rtl=true", "irtl=true" })
        {
            await OpenSnackBarAsync(query);

            if (query == "rtl=true")
            {
                await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("dir", "rtl");
                await Expect(Page.Locator("#snb")).ToHaveClassAsync(HasClass("bit-rtl"));
            }

            await Page.ClickAsync("#btn-persistent-item");
            var item = Items.First;
            await Expect(item).ToBeVisibleAsync();
            var (width, height) = await ScreenAsync();

            var failures = new List<string>();
            foreach (var (name, @class, vertical, _, rtlHorizontal) in Positions)
            {
                await Page.SelectOptionAsync("#sel-position", name);
                await Expect(Page.Locator("#snb")).ToHaveClassAsync(HasClass(@class));
                await SettleAsync(item);

                var box = await item.BoxAsync();
                var v = Vertical(box, height, 8);
                var h = Horizontal(box, width, 8);
                if (v != vertical || h != rtlHorizontal) failures.Add($"{name}: expected {vertical}/{rtlHorizontal}, got {v}/{h}");
            }

            Assert.AreEqual(0, failures.Count, $"{query}:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }
    }

    [TestMethod]
    public async Task Offset_MovesTheStackAwayFromTheScreenEdges()
    {
        await OpenSnackBarAsync("off=40px");

        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("style", new Regex("--bit-snb-off:40px"));
        await Page.ClickAsync("#btn-info");
        await SettleAsync(Item("Info 1"));

        var (width, height) = await ScreenAsync();
        var box = await Item("Info 1").BoxAsync();
        Assert.AreEqual(width - 40, box.Right, 1);
        Assert.AreEqual(height - 40, box.Bottom, 1);
    }

    [TestMethod]
    public async Task Transition_ItemsSlideInFromTheirEdge_AndPlayTheirExitBeforeLeaving()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await Expect(item).ToHaveCSSAsync("animation-name", "bit-snb-ent-b");
        await Expect(item).ToHaveCSSAsync("animation-duration", "0.2s");
        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("style", new Regex("--bit-snb-dur-full:200ms"));

        await item.Locator(".bit-snb-cbt").ClickAsync();
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-dsm"));
        await Expect(item).ToHaveCSSAsync("animation-name", "bit-snb-exi");
        await Expect(item).ToHaveCSSAsync("pointer-events", "none");
        await Expect(item).ToHaveCountAsync(0);

        var entries = new Dictionary<string, string> { ["TopCenter"] = "bit-snb-ent-t", ["CenterStart"] = "bit-snb-ent-l", ["CenterEnd"] = "bit-snb-ent-r", ["CenterLeft"] = "bit-snb-ent-l", ["CenterRight"] = "bit-snb-ent-r", ["Center"] = "bit-snb-ent-b" };
        foreach (var (position, animation) in entries)
        {
            await Page.SelectOptionAsync("#sel-position", position);
            await Page.ClickAsync("#btn-persistent-item");
            await Expect(Items.Last).ToHaveCSSAsync("animation-name", animation);
            await Page.ClickAsync("#btn-clear");
            await Expect(Items).ToHaveCountAsync(0);
        }
    }

    [TestMethod]
    public async Task Transition_ADurationOfZero_RemovesTheItemAtOnce_AndALongerOneSlowsItDown()
    {
        await OpenSnackBarAsync("dur=0");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await Expect(item).ToBeVisibleAsync();
        await Page.EvaluateAsync(@"() => {
            window.__dsm = false;
            new MutationObserver(() => { if (document.querySelector('#snb .bit-snb-dsm')) window.__dsm = true; })
                .observe(document.getElementById('snb'), { subtree: true, attributes: true, childList: true });
        }");

        await item.Locator(".bit-snb-cbt").ClickAsync();
        await Expect(item).ToHaveCountAsync(0);
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__dsm"), "No exit phase is played.");

        await OpenSnackBarAsync("dur=600");
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToHaveCSSAsync("animation-duration", "0.6s");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheTransition()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenSnackBarAsync("dur=600");

        await Page.ClickAsync("#btn-info");
        var duration = await Item("Info 1").EvaluateAsync<string>("el => getComputedStyle(el).animationDuration");
        var seconds = double.Parse(duration.TrimEnd('s'), CultureInfo.InvariantCulture);
        Assert.IsTrue(seconds < 0.001, $"The 600ms slide collapses with the motion tokens ({duration}).");
    }

    [TestMethod]
    public async Task AutoDismiss_TheCountdown_DismissesTheItem_DrawnByAFillingBar()
    {
        await OpenSnackBarAsync("ad=true&t=900");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        var bar = item.Locator(".bit-snb-prb");
        await Expect(bar).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(bar).ToHaveCSSAsync("animation-name", "bit-snackbar-progress-animation");
        await Expect(bar).ToHaveCSSAsync("animation-duration", "0.9s");
        await Expect(bar).ToHaveCSSAsync("transform-origin", new Regex("^0px "));

        var early = await ScaleXAsync(bar);
        await Page.WaitForFunctionAsync("el => { const t = getComputedStyle(el).transform; return t !== 'none' && parseFloat(t.slice(7)) > 0.3; }", await bar.ElementHandleAsync());
        Assert.IsTrue(await ScaleXAsync(bar) > early, "The bar fills as the time runs.");

        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Timeout;");
    }

    [TestMethod]
    public async Task AutoDismiss_TheDefaultTimeIsThreeSeconds_AndAnItemsOwnTimeWins()
    {
        await OpenSnackBarAsync("ad=true");

        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1").Locator(".bit-snb-prb")).ToHaveCSSAsync("animation-duration", "3s");

        await Page.ClickAsync("#btn-item-time");
        await Expect(Item("Quick 2").Locator(".bit-snb-prb")).ToHaveCSSAsync("animation-duration", "0.7s");
        await Expect(Item("Quick 2")).ToHaveCountAsync(0, new() { Timeout = 10000 });
        await Expect(Item("Info 1")).ToBeVisibleAsync();
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Quick 2:Timeout;");
    }

    [TestMethod]
    public async Task AutoDismiss_ReverseProgressDrainsTheBar_AndHideProgressRemovesIt()
    {
        await OpenSnackBarAsync("ad=true&t=30000&rev=true");

        await Page.ClickAsync("#btn-info");
        var bar = Item("Info 1").Locator(".bit-snb-prb");
        await Expect(bar).ToHaveClassAsync(HasClass("bit-snb-rvp"));
        await Expect(bar).ToHaveCSSAsync("animation-name", "bit-snb-prb-rev");

        await OpenSnackBarAsync("ad=true&t=30000&hp=true");
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToBeVisibleAsync();
        await Expect(Item("Info 1").Locator(".bit-snb-prb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AutoDismiss_APointerOverTheItem_HoldsTheCountdownAndTheBar()
    {
        await OpenSnackBarAsync("ad=true&t=1000");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await item.HoverAsync();
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-pau"));
        await Expect(item.Locator(".bit-snb-prb")).ToHaveCSSAsync("animation-play-state", "paused");

        await Page.DwellAsync(1800);
        await Expect(item).ToBeVisibleAsync();

        await Page.Mouse.MoveAsync(5, 5);
        await Expect(item).Not.ToHaveClassAsync(HasClass("bit-snb-pau"));
        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Timeout;");
    }

    [TestMethod]
    public async Task AutoDismiss_PauseOnHoverOff_KeepsCountingUnderThePointer()
    {
        await OpenSnackBarAsync("ad=true&t=800&nohover=true");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await item.HoverAsync();
        await Expect(item).Not.ToHaveClassAsync(HasClass("bit-snb-pau"));
        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task AutoDismiss_TheFocusInsideTheItem_HoldsTheCountdown()
    {
        await OpenSnackBarAsync("ad=true&t=1000");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await item.Locator(".bit-snb-cbt").FocusAsync();
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-pau"));

        await Page.DwellAsync(1800);
        await Expect(item).ToBeVisibleAsync();

        await Page.Locator("#focus-anchor").FocusAsync();
        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task AutoDismiss_PauseAndResume_HoldTheCountdownFromCode()
    {
        await OpenSnackBarAsync("ad=true&t=1000");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await Page.ClickAsync("#btn-pause-last");
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-pau"));

        await Page.DwellAsync(1800);
        await Expect(item).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-resume-last");
        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task AutoDismiss_ABlurredWindowOrAHiddenPage_HoldsTheCountdown()
    {
        await OpenSnackBarAsync("ad=true&t=1500&pb=true&ph=true");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await Expect(item).ToBeVisibleAsync();
        await Page.EvaluateAsync("() => window.dispatchEvent(new Event('blur'))");
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-pau"));
        await Page.DwellAsync(2000);
        await Expect(item).ToBeVisibleAsync();

        await Page.EvaluateAsync(@"() => {
            Object.defineProperty(document, 'hidden', { configurable: true, get: () => true });
            document.dispatchEvent(new Event('visibilitychange'));
            window.dispatchEvent(new Event('focus'));
        }");
        await Page.DwellAsync(2000);
        await Expect(item).ToBeVisibleAsync();

        await Page.EvaluateAsync(@"() => {
            Object.defineProperty(document, 'hidden', { configurable: true, get: () => false });
            document.dispatchEvent(new Event('visibilitychange'));
        }");
        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task Persistent_AHostOfPersistentItems_HasNoButtonCountdownOrEscape_AndOnlyCloseRemovesThem()
    {
        await OpenSnackBarAsync("per=true&ad=true&t=500");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await Expect(item).ToBeVisibleAsync();
        await Expect(item.Locator(".bit-snb-cbt")).ToHaveCountAsync(0);
        await Expect(item.Locator(".bit-snb-prb")).ToHaveCountAsync(0);
        await Expect(item).Not.ToHaveAttributeAsync("data-bit-snb-id", Any);
        await Expect(item).Not.ToHaveAttributeAsync("data-bit-esc", Any);

        await Page.DwellAsync(1200);
        await Expect(item).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-close-last");
        await Expect(item).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Programmatic;");
    }

    [TestMethod]
    public async Task Persistent_PerItemPersistentAndHideDismiss_ApplyToThatItemOnly()
    {
        await OpenSnackBarAsync("ad=true&t=800");

        await Page.ClickAsync("#btn-persistent-item");
        await Page.ClickAsync("#btn-hide-dismiss-item");
        var persistent = Item("Persistent 1");
        var noButton = Item("NoButton 2");

        await Expect(persistent.Locator(".bit-snb-cbt")).ToHaveCountAsync(0);
        await Expect(persistent.Locator(".bit-snb-prb")).ToHaveCountAsync(0);

        // HideDismiss only takes the button away: the item still counts down.
        await Expect(noButton.Locator(".bit-snb-cbt")).ToHaveCountAsync(0);
        await Expect(noButton.Locator(".bit-snb-prb")).ToHaveCountAsync(1);
        await Expect(noButton).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(noButton).ToHaveCountAsync(0, new() { Timeout = 10000 });
        await Expect(persistent).ToBeVisibleAsync();

        await OpenSnackBarAsync("hd=true");
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToBeVisibleAsync();
        await Expect(Item("Info 1").Locator(".bit-snb-cbt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Swipe_ADragPastTheThreshold_ThrowsTheItemAway_AndAShortOneSpringsBack()
    {
        await OpenSnackBarAsync("sw=true&th=60");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await SettleAsync(item);
        await Expect(item).ToHaveAttributeAsync("data-bit-snb-id", new Regex(".+"));
        Assert.AreEqual("pan-y", await item.EvaluateAsync<string>("el => getComputedStyle(el).touchAction"));

        var box = await item.BoxAsync();
        var y = box.CenterY;

        // A short drag follows the pointer, then springs back.
        await Page.Mouse.MoveAsync((float)(box.CenterX), (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.CenterX - 25), (float)y, new() { Steps = 5 });
        StringAssert.Contains(await item.EvaluateAsync<string>("el => el.style.transform"), "translateX(-25px)");
        await Page.Mouse.UpAsync();
        await Expect(item).ToHaveAttributeAsync("style", new Regex("^(?!.*translateX).*$"));
        await Expect(item).ToBeVisibleAsync();
        await Expect(Page.Locator("#dismissed-count")).ToHaveTextAsync("0");

        // A long one throws it away. The short drag selected the text under it, and a drag that starts on a selection
        // is the browser's own drag and drop (see the KNOWN BUG test below), so the selection is cleared first.
        await Page.EvaluateAsync("() => window.getSelection().removeAllRanges()");
        await Page.Mouse.MoveAsync((float)(box.CenterX), (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.CenterX - 120), (float)y, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await Expect(item).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Swipe;");
    }

    [TestMethod]
    public async Task Swipe_APersistentItem_DoesNotFollowThePointer()
    {
        await OpenSnackBarAsync("sw=true");

        await Page.ClickAsync("#btn-persistent-item");
        var item = Item("Persistent 1");
        await SettleAsync(item);
        await Expect(item).Not.ToHaveAttributeAsync("data-bit-snb-id", Any);

        var box = await item.BoxAsync();
        await Page.Mouse.MoveAsync((float)(box.CenterX), (float)box.CenterY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.CenterX - 150), (float)box.CenterY, new() { Steps = 8 });
        Assert.AreEqual("", await item.EvaluateAsync<string>("el => el.style.transform"));
        await Page.Mouse.UpAsync();
        await Expect(item).ToBeVisibleAsync();
    }

    [TestMethod]
    [Ignore("KNOWN BUG: a mouse swipe selects the text it drags across, and the next swipe started on that selection becomes the browser's native drag and drop - pointercancel arrives after the first move and the item springs back. SnackBars.startSwipe neither suppresses the selection nor the native drag.")]
    public async Task Swipe_ASecondMouseSwipeOverTheTextTheFirstSelected_StillThrowsTheItemAway()
    {
        await OpenSnackBarAsync("sw=true&th=60");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await SettleAsync(item);
        var box = await item.BoxAsync();

        await Page.Mouse.MoveAsync((float)box.CenterX, (float)box.CenterY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.CenterX - 25), (float)box.CenterY, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        await Page.Mouse.MoveAsync((float)box.CenterX, (float)box.CenterY);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)(box.CenterX - 120), (float)box.CenterY, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Expect(item).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Stacking_MaxItemsWithDismissOldest_TakesTheOldestAway()
    {
        await OpenSnackBarAsync("max=2");

        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");

        await Expect(Items).ToHaveCountAsync(2);
        await Expect(Item("Plain 1")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Plain 1:Overflow;");
        await Expect(Items.Nth(0)).ToContainTextAsync("Plain 2");
        await Expect(Items.Nth(1)).ToContainTextAsync("Plain 3");
    }

    [TestMethod]
    public async Task Stacking_QueueHoldsTheNewcomer_UntilThereIsRoom()
    {
        await OpenSnackBarAsync("max=2&ovf=Queue");

        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-refresh");

        await Expect(Items).ToHaveCountAsync(2);
        await Expect(Page.Locator("#pending-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#shown-count")).ToHaveTextAsync("2");

        await Item("Plain 1").Locator(".bit-snb-cbt").ClickAsync();
        await Expect(Item("Plain 3")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#pending-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#shown-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Stacking_SkipDropsTheNewcomer()
    {
        await OpenSnackBarAsync("max=2&ovf=Skip");

        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-refresh");

        await Expect(Items).ToHaveCountAsync(2);
        await Expect(Item("Plain 3")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#pending-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#shown-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Stacking_NewestOnTop_PutsTheNewestItemFirst()
    {
        await OpenSnackBarAsync("not=true");

        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-plain");
        await Expect(Items.Nth(0)).ToContainTextAsync("Plain 2");
        await Expect(Items.Nth(1)).ToContainTextAsync("Plain 1");

        await SettleAsync(Items.Nth(0));
        await SettleAsync(Items.Nth(1));
        Assert.IsTrue((await Items.Nth(0).BoxAsync()).Bottom <= (await Items.Nth(1).BoxAsync()).Top, "The newest sits on top.");
    }

    [TestMethod]
    public async Task Stacking_PreventDuplicates_ReturnsTheShowingItem_AndCountsTheRepeat()
    {
        await OpenSnackBarAsync("dup=true");

        await Page.ClickAsync("#btn-dup");
        await Expect(Page.Locator("#dup-same")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#dup-count")).ToHaveTextAsync("1");
        await Expect(Item("Dup")).ToHaveCountAsync(1);

        await OpenSnackBarAsync();
        await Page.ClickAsync("#btn-dup");
        await Expect(Page.Locator("#dup-same")).ToHaveTextAsync("False");
        await Expect(Item("Dup")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Icon_ShowIcon_PicksTheGlyphFromTheColor_AndAnItemOrTheHostReplacesIt()
    {
        await OpenSnackBarAsync("icon=true");

        var expected = new (string Button, string Title, string Icon)[]
        {
            ("#btn-info", "Info 1", "Info"), ("#btn-success", "Success 2", "Completed"), ("#btn-warning", "Warning 3", "Info"),
            ("#btn-severe", "SevereWarning 4", "Warning"), ("#btn-error", "Error 5", "ErrorBadge"),
        };

        foreach (var (button, title, icon) in expected)
        {
            await Page.ClickAsync(button);
            await Expect(Item(title).Locator(".bit-snb-ico")).ToHaveClassAsync(HasClass($"bit-icon--{icon}"));
            await Expect(Item(title).Locator(".bit-snb-ict")).ToHaveAttributeAsync("aria-hidden", "true");
        }

        await Page.ClickAsync("#btn-icon-item");
        await Expect(Item("Ringer 6").Locator(".bit-snb-ico")).ToHaveClassAsync(HasClass("bit-icon--Ringer"));
        await Page.ClickAsync("#btn-hide-icon-item");
        await Expect(Item("NoIcon 7")).ToBeVisibleAsync();
        await Expect(Item("NoIcon 7").Locator(".bit-snb-ict")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-ext-icon-item");
        await Expect(Item("Bell 8").Locator(".bit-snb-ico")).ToHaveClassAsync(HasClass("fa-bell"));

        // The icon sits at the start of the header, before the title.
        var icon1 = await Item("Info 1").Locator(".bit-snb-ict").BoxAsync();
        var title1 = await Item("Info 1").Locator(".bit-snb-ttl").BoxAsync();
        Assert.IsTrue(icon1.Right <= title1.Left);

        await OpenSnackBarAsync("icon=true&ext=true");
        await Page.ClickAsync("#btn-success");
        await Expect(Item("Success 1").Locator(".bit-snb-ico")).ToHaveClassAsync(HasClass("fa-circle-info"));
        await Expect(Item("Success 1").Locator(".bit-snb-cbt i")).ToHaveClassAsync(HasClass("bi-x-lg"));

        await OpenSnackBarAsync("icon=true&iname=Ringer");
        await Page.ClickAsync("#btn-error");
        await Expect(Item("Error 1").Locator(".bit-snb-ico")).ToHaveClassAsync(HasClass("bit-icon--Ringer"));

        await OpenSnackBarAsync();
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToBeVisibleAsync();
        await Expect(Item("Info 1").Locator(".bit-snb-ict")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Actions_AnItemsActions_AndTheHostsActionsTemplate_RenderUnderTheBody()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-item-actions");
        var item = Item("Deleted 1");
        var action = item.Locator(".bit-snb-act #item-action-btn");
        await Expect(action).ToBeVisibleAsync();
        Assert.IsTrue((await action.BoxAsync()).Top >= (await item.Locator(".bit-snb-bdy").BoxAsync()).Bottom - 1, "The actions sit under the body.");

        await action.ClickAsync();
        await Expect(Page.Locator("#action-result")).ToHaveTextAsync("Deleted 1");
        await Expect(item).ToHaveCountAsync(0);

        await OpenSnackBarAsync("tpl=actions");
        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-item-actions");
        await Expect(Item("Info 1").Locator(".tpl-undo")).ToBeVisibleAsync();

        // An item's own actions replace the template.
        await Expect(Item("Deleted 2").Locator(".tpl-undo")).ToHaveCountAsync(0);
        await Expect(Item("Deleted 2").Locator("#item-action-btn")).ToBeVisibleAsync();

        await Item("Info 1").Locator(".tpl-undo").ClickAsync();
        await Expect(Page.Locator("#action-result")).ToHaveTextAsync("Info 1");
        await Expect(Item("Info 1")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task MultilineAndMaxWidth_ALongTextIsCutOffWithItsTooltip_OrWraps()
    {
        await OpenSnackBarAsync("mw=320px");

        await Page.ClickAsync("#btn-long");
        var item = Items.First;
        await SettleAsync(item);
        Assert.AreEqual(320, (await item.BoxAsync()).Width, 1, "MaxWidth caps the item.");

        var title = item.Locator(".bit-snb-ttl");
        await Expect(title).ToHaveClassAsync(HasClass("bit-snb-elp"));
        await Expect(title).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(title).ToHaveAttributeAsync("title", new Regex("^A notification whose title runs on"));
        await Expect(item.Locator(".bit-snb-bdy")).ToHaveAttributeAsync("title", new Regex("^And a body that keeps going"));
        Assert.IsTrue(await title.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The title is cut off.");

        await OpenSnackBarAsync("mw=320px&ml=true");
        await Page.ClickAsync("#btn-long");
        var wrapped = Items.First.Locator(".bit-snb-ttl");
        await Expect(wrapped).Not.ToHaveClassAsync(HasClass("bit-snb-elp"));
        await Expect(wrapped).Not.ToHaveAttributeAsync("title", Any);
        var lineHeight = NotificationsE2EHelpers.Px(await wrapped.EvaluateAsync<string>("el => getComputedStyle(el).fontSize"));
        Assert.IsTrue((await wrapped.BoxAsync()).Height > lineHeight * 2, "The title wraps onto several lines.");
    }

    [TestMethod]
    public async Task Templates_TitleBodyAndTheWholeItem_ReplaceWhatTheyAreFor()
    {
        await OpenSnackBarAsync("tpl=title");
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1").Locator(".tpl-title")).ToHaveTextAsync("[Info 1]");
        await Expect(Item("Info 1").Locator(".bit-snb-ttl")).ToHaveCountAsync(0);
        await Expect(Item("Info 1")).Not.ToHaveAttributeAsync("aria-labelledby", Any);

        await OpenSnackBarAsync("tpl=body");
        await Page.ClickAsync("#btn-info");
        await Expect(Items.First.Locator(".tpl-body")).ToHaveTextAsync("{Info body}");
        await Expect(Items.First.Locator(".bit-snb-bdy")).ToHaveCountAsync(0);
        await Expect(Items.First).Not.ToHaveAttributeAsync("aria-describedby", Any);

        await OpenSnackBarAsync("tpl=full&ad=true&t=30000");
        await Page.ClickAsync("#btn-info");
        var item = Items.First;
        await Expect(item.Locator(".tpl-full b")).ToHaveTextAsync("Info 1");
        await Expect(item.Locator(".bit-snb-hdr")).ToHaveCountAsync(0);
        await Expect(item.Locator(".bit-snb-cbt")).ToHaveCountAsync(0);
        await Expect(item.Locator(".bit-snb-prb")).ToHaveCountAsync(1);
        await item.Locator(".tpl-full-close").ClickAsync();
        await Expect(item).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Events_DismissOnClick_ReportsTheClickAndDismisses_WithTheItemsOwnCallbacksFirst()
    {
        await OpenSnackBarAsync("doc=true&oic=true");

        await Page.ClickAsync("#btn-item-events");
        var item = Item("Events 1");
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-clk"));
        await Expect(item).ToHaveAttributeAsync("tabindex", "0");
        Assert.AreEqual("pointer", await item.EvaluateAsync<string>("el => getComputedStyle(el).cursor"));

        await item.Locator(".bit-snb-bdy").ClickAsync();
        await Expect(Page.Locator("#item-onclick-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#item-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#item-click-last")).ToHaveTextAsync("Events 1");
        await Expect(item).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Events 1:Click;");
        await Expect(Page.Locator("#item-ondismiss")).ToHaveTextAsync("Click:42");
    }

    [TestMethod]
    public async Task Events_AClickableItem_IsReachedByTab_AndActivatedByEnterAndSpace()
    {
        await OpenSnackBarAsync("oic=true");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        await item.FocusAsync();
        await Expect(item).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#item-click-count")).ToHaveTextAsync("1");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#item-click-count")).ToHaveTextAsync("2");

        // Without DismissOnClick the item stays; the dismiss button's click does not reach the item.
        await Expect(item).ToBeVisibleAsync();
        await item.Locator(".bit-snb-cbt").ClickAsync();
        await Expect(item).ToHaveCountAsync(0);
        await Expect(Page.Locator("#item-click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:DismissButton;");

        // An item nobody listens to is no control.
        await OpenSnackBarAsync();
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).Not.ToHaveAttributeAsync("tabindex", Any);
        await Expect(Item("Info 1")).Not.ToHaveClassAsync(HasClass("bit-snb-clk"));
    }

    [TestMethod]
    public async Task Loading_AnItemSitsOutTheCountdownWithASpinner_UntilUpdateSettlesIt()
    {
        await OpenSnackBarAsync("ad=true&t=800");

        await Page.ClickAsync("#btn-loading");
        var item = Item("Uploading");
        await Expect(item).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(item.Locator(".bit-snb-spn")).ToBeVisibleAsync();
        await Expect(item.Locator(".bit-snb-prb")).ToHaveCountAsync(0);
        Assert.AreNotEqual("none", await item.Locator(".bit-snb-spn").EvaluateAsync<string>("el => getComputedStyle(el).animationName"));

        await Page.DwellAsync(1200);
        await Expect(item).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-complete");
        var done = Item("Uploaded");
        await Expect(done).ToHaveClassAsync(HasClass("bit-snb-suc"));
        await Expect(done).Not.ToHaveAttributeAsync("aria-busy", Any);
        await Expect(done.Locator(".bit-snb-spn")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#snb > .bit-snb-lvr[aria-live='polite']")).ToHaveTextAsync("Uploaded. report.pdf");
        await Expect(done).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task Track_ShowsLoadingThenTheOutcome_AndAFailureIsRethrown()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-track-ok");
        await Expect(Item("Exporting")).ToHaveAttributeAsync("aria-busy", "true");
        var ok = Item("Exported 42 rows");
        await Expect(ok).ToHaveClassAsync(HasClass("bit-snb-suc"));
        await Expect(Page.Locator("#track-result")).ToHaveTextAsync("ok:42");
        await Expect(Item("Exporting")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-track-fail");
        var failed = Item("Failed: disk full");
        await Expect(failed).ToHaveClassAsync(HasClass("bit-snb-err"));
        await Expect(Page.Locator("#track-result")).ToHaveTextAsync("thrown:disk full");
    }

    [TestMethod]
    public async Task Control_UpdateCloseClearAndItems_DriveTheStackFromCode()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-success");
        await Page.ClickAsync("#btn-update-last");
        await Expect(Items.Nth(1).Locator(".bit-snb-ttl")).ToHaveTextAsync("Updated title");
        await Expect(Items.Nth(1).Locator(".bit-snb-bdy")).ToHaveTextAsync("Updated body");

        await Page.ClickAsync("#btn-error");
        await Page.ClickAsync("#btn-close-last");
        await Expect(Item("Error 3")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Error 3:Programmatic;");

        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#items-count")).ToHaveTextAsync("2");

        await Page.ClickAsync("#btn-clear");
        await Expect(Items).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync(new Regex("Info 1:Clear;.*Updated title:Clear;"));
        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#items-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ClearOnNavigation_ANavigation_ClearsTheStack()
    {
        await OpenSnackBarAsync("con=true");

        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-success");
        await Expect(Items).ToHaveCountAsync(2);

        await Page.ClickAsync("#btn-navigate");
        await Expect(Page).ToHaveURLAsync(new Regex("nav=1"));
        await Expect(Items).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Clear;Success 2:Clear;");
    }

    [TestMethod]
    public async Task Escape_OnAFocusedItem_DismissesIt_AndHandsTheFocusToTheNextOne()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-success");
        var first = Item("Info 1");
        var second = Item("Success 2");
        await Expect(first).ToHaveAttributeAsync("data-bit-esc", "claim");

        await first.Locator(".bit-snb-cbt").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(first).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Escape;");
        await Expect(second.Locator(".bit-snb-cbt")).ToBeFocusedAsync();

        // The last focused item leaving hands the focus to the region, the next Tab carrying on from there.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(second).ToHaveCountAsync(0);
        await Expect(Page.Locator("#snb")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task DismissButton_IsNamedAndDressedByTheHost_AndTheKeyboardMovesOnToTheNextItem()
    {
        await OpenSnackBarAsync("dlabel=Dismiss%20it&dicon=Blocked2");

        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-success");
        var button = Item("Info 1").Locator(".bit-snb-cbt");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Dismiss it");
        await Expect(button).ToHaveAttributeAsync("title", "Dismiss it");
        await Expect(button).ToHaveAttributeAsync("type", "button");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Blocked2"));

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Item("Info 1")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:DismissButton;");
        await Expect(Item("Success 2").Locator(".bit-snb-cbt")).ToBeFocusedAsync();
        // Firefox does not count a focus a script moves after a key press as :focus-visible, so the ring is read in Chromium and WebKit.
        if (IsFirefox is false)
        {
            Assert.AreEqual("solid", await Item("Success 2").Locator(".bit-snb-cbt").EvaluateAsync<string>("el => getComputedStyle(el).outlineStyle"), "The focus ring is drawn.");
        }

        await OpenSnackBarAsync();
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1").Locator(".bit-snb-cbt")).ToHaveAttributeAsync("aria-label", "Close");
        await Expect(Item("Info 1").Locator(".bit-snb-cbt i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
    }

    [TestMethod]
    public async Task Accessibility_ItemsAreAnnouncedThroughTheLiveRegions_PoliteOrAssertive()
    {
        await OpenSnackBarAsync();

        var polite = Page.Locator("#snb > .bit-snb-lvr[aria-live='polite']");
        var assertive = Page.Locator("#snb > .bit-snb-lvr[aria-live='assertive']");
        await Expect(polite).ToHaveAttributeAsync("aria-atomic", "true");

        await Page.ClickAsync("#btn-info");
        await Expect(polite).ToHaveTextAsync("Info 1. Info body");

        // The text is taken out again, so the next arrival is a change the reader hears.
        await Expect(polite).ToHaveTextAsync("", new() { Timeout = 5000 });

        await Page.ClickAsync("#btn-error");
        await Expect(assertive).ToHaveTextAsync("Error 2. Error body");

        await Page.ClickAsync("#btn-item-announce");
        await Expect(polite).ToHaveTextAsync("Three files were saved");

        await Page.ClickAsync("#btn-item-role-alert");
        await Expect(assertive).ToHaveTextAsync("Loud 4");

        await Expect(polite).ToHaveTextAsync("", new() { Timeout = 5000 });
        await Expect(assertive).ToHaveTextAsync("", new() { Timeout = 5000 });
        await Page.ClickAsync("#btn-item-role-none");
        await Expect(Item("Silent 5")).ToHaveAttributeAsync("role", "none");
        await Expect(polite).ToHaveTextAsync("");
        await Expect(assertive).ToHaveTextAsync("");

        // The items themselves stay silent, and say what they are by their title and body.
        var item = Item("Info 1");
        await Expect(item).ToHaveAttributeAsync("aria-live", "off");
        await Expect(item).ToHaveAttributeAsync("role", "status");
        await Expect(Item("Error 2")).ToHaveAttributeAsync("role", "status");
        var labelledBy = await item.GetAttributeAsync("aria-labelledby");
        var describedBy = await item.GetAttributeAsync("aria-describedby");
        await Expect(Page.Locator($"[id='{labelledBy}']")).ToHaveTextAsync("Info 1");
        await Expect(Page.Locator($"[id='{describedBy}']")).ToHaveTextAsync("Info body");
    }

    [TestMethod]
    public async Task Accessibility_TheHostsRoleAndAriaLabel_NameTheRegionAndTheItems()
    {
        await OpenSnackBarAsync("role=log&label=Alerts");

        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("aria-label", "Alerts");
        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("role", "region");

        await Page.ClickAsync("#btn-error");
        await Expect(Item("Error 1")).ToHaveAttributeAsync("role", "log");
        await Expect(Page.Locator("#snb > .bit-snb-lvr[aria-live='polite']")).ToHaveTextAsync("Error 1. Error body");
    }

    [TestMethod]
    public async Task Hotkey_MovesTheFocusIntoTheRegion_AndEscapeHandsItBack()
    {
        await OpenSnackBarAsync("hk=F8");

        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("aria-label", "Notifications (F8)");

        // The hotkey is registered with the browser after the first render; an item that has made the round trip since
        // is one shown after it.
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToBeVisibleAsync();

        await Page.Locator("#focus-anchor").FocusAsync();
        await Page.Keyboard.PressAsync("F8");
        await Expect(Page.Locator("#snb")).ToBeFocusedAsync();

        // The next Tab lands inside the notifications; the region itself stays out of the Tab order.
        await NextFramesAsync();
        await Page.Keyboard.PressAsync("Tab");
        var landed = await Page.EvaluateAsync<string>("() => document.activeElement?.outerHTML.slice(0, 160) ?? 'nothing'");
        await Expect(Item("Info 1").Locator(".bit-snb-cbt")).ToBeFocusedAsync(new() { Timeout = 5000 }).ContinueWith(t =>
        {
            if (t.IsFaulted) Assert.Fail($"Tab from the region landed on {landed}");
        });
        await Page.Keyboard.PressAsync("F8");
        await Expect(Page.Locator("#snb")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#focus-anchor")).ToBeFocusedAsync();

        await OpenSnackBarAsync("hk=altKey,KeyT");
        await Expect(Page.Locator("#snb")).ToHaveAttributeAsync("aria-label", "Notifications (Alt+T)");
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToBeVisibleAsync();
        await Page.Locator("#focus-anchor").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+KeyT");
        await Expect(Page.Locator("#snb")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task FocusAsync_FocusesTheRegionFromCode_AndEscapeReturnsTheFocus()
    {
        await OpenSnackBarAsync();

        // Pressed from the keyboard, so the button holds the focus FocusAsync moves away from in every engine (WebKit
        // does not focus a button it clicks).
        await Page.Locator("#btn-focus").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#snb")).ToBeFocusedAsync();
        Assert.AreEqual("none", await Page.Locator("#snb").EvaluateAsync<string>("el => getComputedStyle(el).outlineStyle"));

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#btn-focus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Variant_EachVariantAtRuntime_PaintsTheItems()
    {
        await OpenSnackBarAsync();

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");
        var info = await Page.ResolveColorAsync("--bit-clr-inf");
        var surface = await Page.ResolveColorAsync("--bit-clr-bg-pri");

        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-fil"));
        await Expect(item).ToHaveCSSAsync("background-color", info);
        await Expect(item).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-inf-text"));
        await Expect(item).ToHaveCSSAsync("border-top-width", "0px");

        await Page.SelectOptionAsync("#sel-variant", "Outline");
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-otl"));
        await Expect(item).ToHaveCSSAsync("background-color", surface);
        await Expect(item).ToHaveCSSAsync("border-top-color", info);
        await Expect(item).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(item).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-inf-fg"));

        await Page.SelectOptionAsync("#sel-variant", "Text");
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-txt"));
        await Expect(item).ToHaveCSSAsync("background-color", surface);
        await Expect(item).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(item).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-inf-fg"));

        await Page.SelectOptionAsync("#sel-variant", "Fill");
        await Expect(item).ToHaveClassAsync(HasClass("bit-snb-fil"));
    }

    [TestMethod]
    public async Task Size_EachSizeAtRuntime_ScalesThePaddingTypeAndButton()
    {
        await OpenSnackBarAsync("icon=true");

        await Page.ClickAsync("#btn-info");
        var item = Item("Info 1");

        var titles = new List<double>();
        var paddings = new List<double>();
        var buttons = new List<double>();
        foreach (var (size, suffix) in new[] { ("Small", "sm"), ("Medium", "md"), ("Large", "lg") })
        {
            await Page.SelectOptionAsync("#sel-size", size);
            await Expect(item).ToHaveClassAsync(HasClass($"bit-snb-{suffix}"));
            titles.Add(NotificationsE2EHelpers.Px(await item.Locator(".bit-snb-ttl").EvaluateAsync<string>("el => getComputedStyle(el).fontSize")));
            paddings.Add(NotificationsE2EHelpers.Px(await item.EvaluateAsync<string>("el => getComputedStyle(el).paddingTop")));
            buttons.Add((await item.Locator(".bit-snb-cbt").BoxAsync()).Height);
        }

        foreach (var (what, values) in new[] { ("title", titles), ("padding", paddings), ("button", buttons) })
        {
            Assert.IsTrue(values[0] < values[1] && values[1] < values[2], $"The {what} grows with the size ({string.Join(", ", values)}).");
        }
    }

    [TestMethod]
    public async Task Color_EachColor_PaintsItsItemInTheFillAndOutlineVariants()
    {
        await OpenSnackBarAsync("max=1");

        foreach (var (name, suffix, token) in NotificationsE2EHelpers.Colors)
        {
            await Page.ClickAsync($"#btn-color-{name}");
            var item = Item(name).Last;
            await Expect(item).ToHaveClassAsync(HasClass($"bit-snb-{suffix}"));
            await Expect(item).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync(token));
        }

        await Page.SelectOptionAsync("#sel-variant", "Outline");
        foreach (var (name, suffix, token) in NotificationsE2EHelpers.Colors)
        {
            await Page.ClickAsync($"#btn-color-{name}");
            var item = Item(name).Last;
            await Expect(item).ToHaveClassAsync(HasClass("bit-snb-otl"));
            await Expect(item).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync(token));
            await Expect(item).ToHaveCSSAsync("color", await Page.ResolveColorAsync(FgTokens[suffix]));
        }
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPartOfEveryItem_AndAnItemTakesItsOwnCss()
    {
        await OpenSnackBarAsync("tpl=styles&icon=true&ad=true&t=30000");

        await Page.ClickAsync("#btn-item-actions");
        await Page.ClickAsync("#btn-loading");
        var item = Item("Deleted 1");

        var styles = new (ILocator Element, string Property, string Value)[]
        {
            (Page.Locator("#snb"), "row-gap", "13px"),
            (item, "letter-spacing", "1px"),
            (item.Locator(".bit-snb-hdr"), "padding-top", "3px"),
            (item.Locator(".bit-snb-ict"), "padding-left", "2px"),
            (item.Locator(".bit-snb-ico"), "color", "rgb(255, 0, 0)"),
            (Item("Uploading").Locator(".bit-snb-spn"), "opacity", "0.7"),
            (item.Locator(".bit-snb-cbt"), "padding-right", "5px"),
            (item.Locator(".bit-snb-cbt i"), "color", "rgb(0, 255, 0)"),
            (item.Locator(".bit-snb-ttl"), "color", "rgb(0, 0, 255)"),
            (item.Locator(".bit-snb-bdy"), "color", "rgb(1, 1, 1)"),
            (item.Locator(".bit-snb-act"), "padding-bottom", "4px"),
            (item.Locator(".bit-snb-prb"), "opacity", "0.5"),
        };

        foreach (var (element, property, value) in styles)
        {
            await Expect(element).ToHaveCSSAsync(property, value);
        }

        await OpenSnackBarAsync("tpl=classes&icon=true&ad=true&t=30000");
        await Page.ClickAsync("#btn-item-actions");
        await Page.ClickAsync("#btn-loading");
        item = Item("Deleted 1");

        var classes = new (ILocator Element, string Class)[]
        {
            (Page.Locator("#snb"), "c-root"), (item, "c-container"), (item.Locator(".bit-snb-hdr"), "c-header"),
            (item.Locator(".bit-snb-ict"), "c-ict"), (item.Locator(".bit-snb-ico"), "c-icon"),
            (Item("Uploading").Locator(".bit-snb-spn"), "c-spinner"), (item.Locator(".bit-snb-cbt"), "c-dismiss"),
            (item.Locator(".bit-snb-cbt i"), "c-dismiss-icon"), (item.Locator(".bit-snb-ttl"), "c-title"),
            (item.Locator(".bit-snb-bdy"), "c-body"), (item.Locator(".bit-snb-act"), "c-actions"),
            (item.Locator(".bit-snb-prb"), "c-progress"),
        };

        foreach (var (element, @class) in classes)
        {
            await Expect(element).ToHaveClassAsync(HasClass(@class));
        }

        await OpenSnackBarAsync();
        await Page.ClickAsync("#btn-item-css");
        var styled = Item("Styled 1");
        await Expect(styled).ToHaveClassAsync(HasClass("item-class"));
        await Expect(styled).ToHaveCSSAsync("outline-color", "rgb(255, 0, 0)");
        await Expect(styled).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
    }

    [TestMethod]
    public async Task CssVariables_OnTheHost_RestyleTheItems_AndAColorOrASizeWinsOverThem()
    {
        var style = Uri.EscapeDataString(
            "--bit-SnackBar-background: rgb(30, 27, 75); --bit-SnackBar-color: rgb(224, 231, 255); --bit-SnackBar-border-color: rgb(0, 128, 0); " +
            "--bit-SnackBar-border-width: 2px; --bit-SnackBar-radius: 16px; --bit-SnackBar-padding: 20px; --bit-SnackBar-gap: 18px; " +
            "--bit-SnackBar-title-font-size: 21px; --bit-SnackBar-title-font-weight: 700; --bit-SnackBar-body-font-size: 11px; " +
            "--bit-SnackBar-icon-color: rgb(165, 180, 252); --bit-SnackBar-icon-size: 25px; --bit-SnackBar-progress-color: rgb(129, 140, 248); " +
            "--bit-SnackBar-progress-height: 6px; --bit-SnackBar-z-index: 77; --bit-SnackBar-offset: 30px; --bit-SnackBar-min-width: 300px; " +
            "--bit-SnackBar-max-width: 400px; --bit-SnackBar-shadow: rgb(0, 0, 0) 0px 0px 7px 0px; --bit-SnackBar-outline-border-color: rgb(255, 0, 0);");
        await OpenSnackBarAsync($"style={style}&icon=true&ad=true&t=30000");

        await Page.ClickAsync("#btn-plain");
        await Page.ClickAsync("#btn-long");
        var item = Item("Plain 1");
        await SettleAsync(item);

        await Expect(item).ToHaveCSSAsync("background-color", "rgb(30, 27, 75)");
        await Expect(item).ToHaveCSSAsync("color", "rgb(224, 231, 255)");
        await Expect(item).ToHaveCSSAsync("border-top-color", "rgb(0, 128, 0)");
        await Expect(item).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(item).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(item).ToHaveCSSAsync("padding-top", "20px");
        await Expect(item).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 0) 0px 0px 7px 0px");
        await Expect(item.Locator(".bit-snb-ttl")).ToHaveCSSAsync("font-size", "21px");
        await Expect(item.Locator(".bit-snb-ttl")).ToHaveCSSAsync("font-weight", "700");
        await Expect(item.Locator(".bit-snb-bdy")).ToHaveCSSAsync("font-size", "11px");
        await Expect(item.Locator(".bit-snb-ico")).ToHaveCSSAsync("color", "rgb(165, 180, 252)");
        await Expect(item.Locator(".bit-snb-ico")).ToHaveCSSAsync("font-size", "25px");
        await Expect(item.Locator(".bit-snb-prb")).ToHaveCSSAsync("background-color", "rgb(129, 140, 248)");
        await Expect(item.Locator(".bit-snb-prb")).ToHaveCSSAsync("height", "6px");
        await Expect(Page.Locator("#snb")).ToHaveCSSAsync("z-index", "77");
        await Expect(Page.Locator("#snb")).ToHaveCSSAsync("row-gap", "18px");
        Assert.IsTrue((await item.BoxAsync()).Width >= 299.5, "The min-width holds.");
        Assert.AreEqual(400, (await Items.Nth(1).BoxAsync()).Width, 1, "The max-width caps a long item.");

        var (width, height) = await ScreenAsync();
        var box = await Items.Nth(1).BoxAsync();
        Assert.AreEqual(width - 30, box.Right, 1, "The offset variable moves the stack.");
        Assert.AreEqual(height - 30, box.Bottom, 1);

        // An explicit Color wins over the colors, an explicit Size over the type.
        await Page.ClickAsync("#btn-error");
        await Expect(Item("Error 2")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
        await Page.SelectOptionAsync("#sel-size", "Small");
        await Expect(item.Locator(".bit-snb-ttl")).Not.ToHaveCSSAsync("font-size", "21px");

        // The variant's own twin wins over the shared border color.
        await Page.SelectOptionAsync("#sel-variant", "Outline");
        await Expect(item).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 0)");
    }

    [TestMethod]
    public async Task BaseParameters_VisibilityHidesTheRegion()
    {
        await OpenSnackBarAsync("vis=true");

        await Expect(Page.Locator("#snb")).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-info");
        await Expect(Item("Info 1")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Unmount_NavigatingAwayWithLiveItems_LeavesNoErrorOnThePageOrTheServer()
    {
        await OpenSnackBarAsync("ad=true&t=30000&sw=true&hk=F8&pb=true&ph=true&con=true");

        await Page.ClickAsync("#btn-info");
        await Page.ClickAsync("#btn-loading");
        await Page.ClickAsync("#btn-focus");
        await Expect(Items).ToHaveCountAsync(2);

        await Page.ClickAsync("#link-home");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Bit.BlazorUI E2E Test Host");
        await Expect(Page.Locator("#snb")).ToHaveCountAsync(0);

        // The hotkey went with the host.
        await Page.Keyboard.PressAsync("F8");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Service_ReachesTheServiceHost_WithEveryShortcutCloseUpdateClearAndTrack()
    {
        await OpenAsync("/e2e/snack-bar/service");

        // The host registers as it renders, after the page has written its readouts: they are read again.
        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#host-available")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#host-id")).ToHaveTextAsync("svc-host");

        var svcItems = Page.Locator("#svc-host .bit-snb-itm");
        var expected = new (string Button, string Title, string Suffix)[]
        {
            ("#btn-svc-info", "Svc info", "inf"), ("#btn-svc-success", "Svc success", "suc"), ("#btn-svc-warning", "Svc warning", "wrn"),
            ("#btn-svc-severe", "Svc severe", "swr"), ("#btn-svc-error", "Svc error", "err"), ("#btn-svc-show", "Svc show", "ter"),
        };

        foreach (var (button, title, suffix) in expected)
        {
            await Page.ClickAsync(button);
            var item = Page.Locator("#svc-host .bit-snb-itm", new() { HasText = title });
            await Expect(item).ToHaveClassAsync(HasClass($"bit-snb-{suffix}"));
            await Expect(Page.Locator("#returned-title")).ToHaveTextAsync(title);
        }

        await Expect(Page.Locator("#svc-host .bit-snb-itm.svc-class")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#plain-host .bit-snb-itm")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-svc-update");
        await Expect(Page.Locator("#svc-host .bit-snb-itm", new() { HasText = "Svc updated" })).ToHaveCountAsync(1);
        await Page.ClickAsync("#btn-svc-close");
        await Expect(Page.Locator("#svc-host .bit-snb-itm", new() { HasText = "Svc updated" })).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-svc-item");
        await Expect(Page.Locator("#svc-host .bit-snb-itm", new() { HasText = "Svc item" }).Locator(".bit-snb-cbt")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-svc-clear");
        await Expect(svcItems).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-svc-track");
        await Expect(Page.Locator("#svc-host .bit-snb-itm", new() { HasText = "Svc done 7" })).ToHaveClassAsync(HasClass("bit-snb-suc"));
        await Expect(Page.Locator("#track-result")).ToHaveTextAsync("7");

        await Page.ClickAsync("#btn-svc-track-plain");
        await Expect(Page.Locator("#svc-host .bit-snb-itm", new() { HasText = "Svc plain done" })).ToHaveClassAsync(HasClass("bit-snb-suc"));
        await Expect(Page.Locator("#track-result")).ToHaveTextAsync("plain");
    }

    [TestMethod]
    public async Task Service_WithoutAHost_ShowsNothing_AndAHostRenderedLaterIsReached()
    {
        await OpenAsync("/e2e/snack-bar/service?host=false");

        await Expect(Page.Locator("#host-available")).ToHaveTextAsync("False");
        await Page.ClickAsync("#btn-svc-info");
        await Expect(Page.Locator("#returned-title")).ToHaveTextAsync("Svc info");
        await Expect(Page.Locator(".bit-snb-itm")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-toggle-host");
        await Expect(Page.Locator("#svc-host")).ToHaveCountAsync(1);
        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#host-available")).ToHaveTextAsync("True");
        await Page.ClickAsync("#btn-svc-success");
        await Expect(Page.Locator("#svc-host .bit-snb-itm", new() { HasText = "Svc success" })).ToBeVisibleAsync();

        // Taken away again, the host unregisters.
        await Page.ClickAsync("#btn-toggle-host");
        await Page.ClickAsync("#btn-refresh");
        await Expect(Page.Locator("#host-available")).ToHaveTextAsync("False");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Cascade_BitParams_SetsTheDefaults_AnOwnValueWins_AndOutsideKeepsTheLibraryDefaults()
    {
        await OpenAsync("/e2e/snack-bar/cascade");

        await Expect(Page.Locator("#cas-host")).ToHaveClassAsync(HasClass("bit-snb-tcn"));
        await Expect(Page.Locator("#cas-own")).ToHaveClassAsync(HasClass("bit-snb-bcn"));
        await Expect(Page.Locator("#cas-outside")).ToHaveClassAsync(HasClass("bit-snb-ben"));

        await Page.ClickAsync("#btn-cascaded");
        var cascaded = Page.Locator("#cas-host .bit-snb-itm");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-snb-otl"));
        await Expect(cascaded.Locator(".bit-snb-ico")).ToHaveClassAsync(HasClass("bit-icon--Completed"));
        await Expect(cascaded.Locator(".bit-snb-prb")).ToHaveCSSAsync("animation-duration", "30s");
        await Expect(cascaded.Locator(".bit-snb-cbt")).ToHaveAttributeAsync("aria-label", "Dismiss it");

        await Page.ClickAsync("#btn-own");
        var own = Page.Locator("#cas-own .bit-snb-itm");
        await Expect(own).ToHaveClassAsync(HasClass("bit-snb-otl"));
        await Expect(own.Locator(".bit-snb-ico")).ToHaveCountAsync(1);

        await Page.ClickAsync("#btn-outside");
        var outside = Page.Locator("#cas-outside .bit-snb-itm");
        await Expect(outside).ToHaveClassAsync(HasClass("bit-snb-fil"));
        await Expect(outside.Locator(".bit-snb-ico")).ToHaveCountAsync(0);
        await Expect(outside.Locator(".bit-snb-prb")).ToHaveCountAsync(0);
        await Expect(outside.Locator(".bit-snb-cbt")).ToHaveAttributeAsync("aria-label", "Close");
    }

    [TestMethod]
    public async Task Dialog_EscapeOnAnItem_DismissesIt_AndTheDialogClosesOnTheNextPress()
    {
        await OpenAsync("/e2e/snack-bar/dialog");

        await Page.ClickAsync("#btn-open-dialog");
        await Page.ClickAsync("#btn-dialog-show");
        var item = Page.Locator("#dlg-snb .bit-snb-itm");
        await Expect(item).ToBeVisibleAsync();

        await item.Locator(".bit-snb-cbt").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(item).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("In dialog:Escape;");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Page.Locator("#dlg-inner-btn").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }
}
