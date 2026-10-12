using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for a direction that <c>Dir</c> does not name (#13567): a component with no <c>Dir</c>
/// inherits the page's direction and one with <c>Dir="BitDir.Auto"</c> takes its content's, which only the browser
/// knows. Every part a right-to-left rule turns around is read off the computed styles - with no direction, with
/// <c>Dir="BitDir.Rtl"</c>, inside a box that reads right to left, and with <c>Dir="BitDir.Ltr"</c> inside a right-to-left
/// component - and what the components compute from the direction (the arrow keys, the pointer on a mirrored axis, the
/// placement of a callout, the layout of a carousel) is driven in a box that reads each way, with no <c>Dir</c> at all.
/// Run them with BROWSER=firefox and BROWSER=webkit too.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitDirResolvedBrowserTests
public class BitDirResolvedBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/dir-resolved";

    private static readonly string[] Probes =
    [
        "colorpicker", "colorpicker-thumb", "pagination", "badge", "progress-gap", "progress-segments", "shimmer", "dialog",
        "callout", "dropmenu", "swiper", "searchbox", "phoneinput", "persona"
    ];

    // Each probe's part, read the way its right-to-left rule changes it.
    private const string ReadSet = @"(set) => {
        const root = document.querySelector(set);
        const part = (probe, selector) => root.querySelector(`[data-probe=""${probe}""] ${selector}`);
        const style = (probe, selector) => {
            const el = part(probe, selector);
            return el ? getComputedStyle(el) : null;
        };
        const variable = (probe, selector, name) => style(probe, selector)?.getPropertyValue(name).trim();
        const mask = (probe, selector) => {
            const s = style(probe, selector);
            if (!s) return null;
            const image = s.maskImage && s.maskImage !== 'none' ? s.maskImage : s.webkitMaskImage;
            return /to left/.test(image) ? 'to left' : /to right/.test(image) ? 'to right' : image;
        };
        // Which of the two initials is drawn on the right: the first one in a persona that reads right to left.
        const initials = () => {
            const el = part('persona', '.bit-prs-ini');
            const text = el?.firstChild;
            if (!text || text.nodeType !== Node.TEXT_NODE) return null;
            const x = letter => {
                const index = text.textContent.indexOf(letter);
                if (index < 0) return null;
                const range = document.createRange();
                range.setStart(text, index);
                range.setEnd(text, index + 1);
                return range.getBoundingClientRect().left;
            };
            const s = x('S'), k = x('K');
            return s === null || k === null ? null : (s > k ? 'first on the right' : 'first on the left');
        };
        const read = {
            'colorpicker': () => style('colorpicker', '.bit-clp-light')?.backgroundImage,
            'colorpicker-thumb': () => style('colorpicker', '.bit-clp-thumb')?.transform,
            'pagination': () => variable('pagination', '.bit-pgn', '--bit-pgn-ico-transform-end'),
            'badge': () => style('badge', '.bit-bdg-inl')?.flexDirection,
            'progress-gap': () => variable('progress-gap', '.bit-prb-gps', '--bit-prb-gap-base'),
            'progress-segments': () => mask('progress-segments', '.bit-prb-bcn'),
            'shimmer': () => style('shimmer', '.bit-smr-wav')?.animationDirection,
            'dialog': () => style('dialog', '.bit-dlg-doc')?.justifyContent,
            'callout': () => variable('callout', '.bit-clo-cal', '--bit-clo-transform-factor'),
            'dropmenu': () => variable('dropmenu', '.bit-drm', '--bit-drm-transform-factor'),
            'swiper': () => style('swiper', '.bit-swp-rbt .bit-ico-frt')?.scale,
            'searchbox': () => style('searchbox', 'i.bit-srb-dsi')?.transform,
            'phoneinput': () => style('phoneinput', '.bit-phi-inp')?.textAlign,
            'persona': initials
        };
        const result = {};
        for (const [probe, reader] of Object.entries(read)) {
            const value = reader();
            result[probe] = value === undefined || value === null ? null : String(value);
        }
        return JSON.stringify(result);
    }";

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public async Task EveryPartFollowsTheDirectionItIsLaidOutInWhateverMadeIt()
    {
        await Open();

        var ltr = await Read("#ltr");
        var rtl = await Read("#own");

        foreach (var probe in Probes)
        {
            Assert.IsNotNull(ltr[probe], $"{probe}: the part is not on the page.");
            Assert.IsNotNull(rtl[probe], $"{probe}: the part is not on the page with Dir=Rtl.");
            Assert.AreNotEqual(ltr[probe], rtl[probe], $"{probe}: Dir=Rtl does not turn the part around ({rtl[probe]}).");
        }

        AssertSet(await Read("#inherited"), ltr, rtl, "rtl", "a direction inherited from a box");
        AssertSet(await Read("#nested"), ltr, rtl, "ltr", "Dir=Ltr inside a right-to-left component");
    }

    [TestMethod,
        DataRow("#b-ltr", 4d),
        DataRow("#b-rtl", 2d)]
    public async Task TheRatingArrowsFollowTheDirectionItIsLaidOutIn(string set, double expected)
    {
        await Open();

        await Page.Locator($"{set} [data-probe=rating] [role=radio][tabindex='0']").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Page.WaitForFunctionAsync($"() => document.querySelector('{set} [data-probe=rating] .value').textContent.trim() === '{expected}'",
            null, new() { Timeout = DefaultTimeout });
    }

    [TestMethod,
        DataRow("#b-ltr", 0),
        DataRow("#b-rtl", 2)]
    public async Task TheOtpArrowLeftFollowsTheDirectionTheRowIsLaidOutIn(string set, int expected)
    {
        await Open();

        var inputs = Page.Locator($"{set} [data-probe=otp] .bit-otp-inp");

        await inputs.Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Page.WaitForFunctionAsync($"() => document.activeElement === document.querySelectorAll('{set} [data-probe=otp] .bit-otp-inp')[{expected}]",
            null, new() { Timeout = DefaultTimeout });
    }

    // The saturation axis runs from the inline-start edge, which the script measures the pointer from: a press near the
    // left edge is next to no saturation reading left to right, and nearly all of it reading right to left.
    [TestMethod,
        DataRow("#b-ltr", false),
        DataRow("#b-rtl", true)]
    public async Task TheColorPickerPointerFollowsTheDirectionTheAreaIsLaidOutIn(string set, bool rtl)
    {
        await Open();

        var area = Page.Locator($"{set} [data-probe=colorpicker] .bit-clp-rec");

        Assert.AreEqual("50", await area.GetAttributeAsync("aria-valuenow"));

        // The press is aimed through the locator, which scrolls the area into view first.
        var box = (await area.BoundingBoxAsync())!;
        await area.ClickAsync(new() { Position = new() { X = box.Width * 0.05f, Y = box.Height / 2 } });

        var saturation = 50d;
        for (var attempt = 0; attempt < 50 && saturation == 50; attempt++)
        {
            await Task.Delay(100);
            saturation = double.Parse((await area.GetAttributeAsync("aria-valuenow"))!, System.Globalization.CultureInfo.InvariantCulture);
        }

        Assert.IsTrue(rtl ? saturation > 80 : saturation < 20, $"A press near the left edge picked a saturation of {saturation}.");
    }

    // The slides are placed with physical transforms, so the carousel lays itself out - the buttons and their names
    // among the rest - in the direction its container reads, which it measures along with its size.
    [TestMethod,
        DataRow("#b-ltr", "Next slide"),
        DataRow("#b-rtl", "Previous slide")]
    public async Task TheCarouselIsLaidOutInTheDirectionItReads(string set, string leftLabel)
    {
        await Open();

        await Page.WaitForFunctionAsync($"() => document.querySelector('{set} [data-probe=carousel] .bit-csl-lbt')?.getAttribute('aria-label') === '{leftLabel}'",
            null, new() { Timeout = DefaultTimeout });
    }

    // A callout placed on the Start side of its anchor sits on its left reading left to right and on its right reading
    // right to left - a page's direction (no Dir), or the anchor's Persian content (Dir=Auto) on a left-to-right page.
    [TestMethod,
        DataRow("#b-ltr", false),
        DataRow("#b-rtl", true),
        DataRow("#b-auto", true)]
    public async Task TheCalloutStartSideFollowsTheDirectionTheAnchorIsLaidOutIn(string set, bool rtl)
    {
        await Open();

        await Page.Locator($"{set} [data-probe=callout] .anchor").ClickAsync();

        var content = Page.Locator(".content").Filter(new() { Visible = true });
        await content.WaitForAsync(new() { Timeout = DefaultTimeout });

        var anchor = (await Page.Locator($"{set} [data-probe=callout] .anchor").BoundingBoxAsync())!;
        var callout = (await content.BoundingBoxAsync())!;

        // The sides are told apart by the centres: the callout's surface reaches a pixel or two over the anchor's edge.
        var anchorCenter = anchor.X + anchor.Width / 2;
        var calloutCenter = callout.X + callout.Width / 2;

        Assert.AreEqual(rtl, calloutCenter > anchorCenter, $"The callout (centre {calloutCenter}) is on the wrong side of its anchor (centre {anchorCenter}).");
        Assert.IsTrue(rtl ? callout.X > anchor.X + anchor.Width / 2 : callout.X + callout.Width < anchor.X + anchor.Width / 2, "The callout covers its anchor.");
    }

    // A horizontal arrow moves along a row of items the way it points, so it walks the row backwards in one that reads
    // right to left - with no Dir on the component, only the direction of the box it sits in.
    [TestMethod,
        DataRow("#b-ltr", "buttongroup", "button", "Three"),
        DataRow("#b-rtl", "buttongroup", "button", "One"),
        DataRow("#b-ltr", "navbar", "[role=menuitem], [role=link], a, button", "Three"),
        DataRow("#b-rtl", "navbar", "[role=menuitem], [role=link], a, button", "One"),
        DataRow("#b-ltr", "pivot", "[role=tab]", "Three"),
        DataRow("#b-rtl", "pivot", "[role=tab]", "One")]
    public async Task TheArrowsWalkARowTheWayItReads(string set, string probe, string items, string expected)
    {
        await Open();

        await Page.Locator($"{set} [data-probe={probe}]").Locator(items).Filter(new() { HasTextString = "Two" }).First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await WaitForFocus(set, probe, expected);
    }

    // The arrow pointing away from the start of the line opens a collapsed group of the nav: the right one reading left
    // to right, the left one reading right to left.
    [TestMethod,
        DataRow("#b-ltr", "ArrowRight"),
        DataRow("#b-rtl", "ArrowLeft")]
    public async Task TheNavOpensAGroupWithTheArrowPointingAwayFromTheStart(string set, string key)
    {
        await Open();

        var parent = Page.Locator($"{set} [data-probe=nav] [aria-expanded]").First;

        Assert.AreEqual("false", await parent.GetAttributeAsync("aria-expanded"));

        await parent.FocusAsync();
        await Page.Keyboard.PressAsync(key);

        await Page.WaitForFunctionAsync($"() => document.querySelector('{set} [data-probe=nav] [aria-expanded]').getAttribute('aria-expanded') === 'true'",
            null, new() { Timeout = DefaultTimeout });
    }

    // Walking backwards out of an empty input lands on the last tag, and backwards is the arrow pointing at the start.
    [TestMethod,
        DataRow("#b-ltr", "ArrowLeft"),
        DataRow("#b-rtl", "ArrowRight")]
    public async Task TheTagsInputArrowBackFollowsTheDirectionItReads(string set, string key)
    {
        await Open();

        await Page.Locator($"{set} [data-probe=tagsinput] .bit-tgi-inp").FocusAsync();
        await Page.Keyboard.PressAsync(key);

        await Page.WaitForFunctionAsync($"() => document.activeElement?.getAttribute('role') === 'listitem' && document.querySelector('{set} [data-probe=tagsinput]').contains(document.activeElement)",
            null, new() { Timeout = DefaultTimeout });
    }

    // The right arrow moves to the next day reading left to right and to the previous one reading right to left.
    [TestMethod,
        DataRow("#b-ltr", "16"),
        DataRow("#b-rtl", "14")]
    public async Task TheCalendarArrowsFollowTheDirectionTheGridReads(string set, string expected)
    {
        await Open();

        await Page.Locator($"{set} [data-probe=calendar] .bit-cal-dbt[tabindex='0']").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await WaitForFocus(set, "calendar", expected);
    }

    // The arrow pointing away from the start moves to the next column: the right one reading left to right, the left
    // one reading right to left.
    [TestMethod,
        DataRow("#b-ltr", "ArrowRight"),
        DataRow("#b-rtl", "ArrowLeft")]
    public async Task TheDataGridCellArrowsFollowTheDirectionTheGridReads(string set, string key)
    {
        await Open();

        await Page.Locator($"{set} [data-probe=datagrid] [role=gridcell]").First.FocusAsync();
        await Page.Keyboard.PressAsync(key);

        await WaitForFocus(set, "datagrid", "one");
    }

    // The AM/PM segments lead the time in a picker laid out right to left, whatever made it so: the page's direction,
    // or a culture that writes right to left on a page that does not.
    [TestMethod,
        DataRow("#b-ltr", "datepicker", ".bit-dtp-am-pm", "0"),
        DataRow("#b-rtl", "datepicker", ".bit-dtp-am-pm", "-1"),
        DataRow("#b-culture", "datepicker", ".bit-dtp-am-pm", "-1"),
        DataRow("#b-ltr", "daterangepicker", ".bit-dtrp-am-pm", "0"),
        DataRow("#b-rtl", "daterangepicker", ".bit-dtrp-am-pm", "-1"),
        DataRow("#b-culture", "daterangepicker", ".bit-dtrp-am-pm", "-1")]
    public async Task TheAmPmSegmentLeadsTheTimeInAPickerThatReadsRightToLeft(string set, string probe, string part, string expected)
    {
        await Open();

        var orders = await Page.EvaluateAsync<string[]>($"() => [...document.querySelectorAll('{set} [data-probe={probe}] {part}')].map(e => getComputedStyle(e).order)");

        Assert.IsTrue(orders.Length > 0, $"{probe}: no AM/PM segment in {set}.");
        CollectionAssert.AreEqual(Enumerable.Repeat(expected, orders.Length).ToArray(), orders, $"{probe} in {set}: {string.Join(", ", orders)}");
    }

    // The drawer is docked to the edge its Start placement names in the direction it reads - the left reading left to
    // right, the right reading right to left - and a swipe towards that edge closes it, while one the other way leaves
    // it open. The direction is read as the swipe starts, and the moves and the trigger of a quick flick that arrive
    // before that read returns wait for it.
    [TestMethod,
        DataRow("ltr", -1, true),
        DataRow("ltr", 1, false),
        DataRow("rtl", 1, true),
        DataRow("rtl", -1, false)]
    public async Task TheNavPanelDrawerClosesWithASwipeTowardsTheEdgeItIsDockedTo(string dir, int sign, bool closes)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/dir-resolved-drawer?dir={dir}");
        await WaitForStatus("Ready");
        await Page.WaitForTimeoutAsync(500);

        var panel = Page.Locator(".bit-npn");
        await panel.WaitForAsync(new() { Timeout = DefaultTimeout });

        var box = (await panel.BoundingBoxAsync())!;
        Assert.AreEqual(dir == "rtl", box.X > 100, $"The drawer is docked to the wrong edge (x = {box.X}).");

        // The press starts at the side of the drawer the swipe moves away from, so the whole drag stays on the screen:
        // an engine sends no pointerup for a release past the edge of the viewport.
        var x = sign > 0 ? box.X + 5 : box.X + box.Width - 5;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + sign * box.Width * 0.6f, y, new() { Steps = 4 });
        await Page.Mouse.UpAsync();

        if (closes)
        {
            await Page.WaitForFunctionAsync("() => document.querySelector('#open').textContent === 'false'", null, new() { Timeout = DefaultTimeout });
        }
        else
        {
            await Task.Delay(1000);
            Assert.AreEqual("true", await Page.Locator("#open").TextContentAsync());
        }
    }

    // A map's marker popup sits over its marker in a map that reads either way, with the auto-pan the open asks for and
    // without it (#13592): the anchor is moved by the provider's projected point, which is in pixels from the
    // container's left edge whatever the direction, so an inline-start origin put it a map's width to the right on a
    // right-to-left map - out of sight without the pan, and away from its marker with it.
    [TestMethod,
        DataRow("ltr", false),
        DataRow("ltr", true),
        DataRow("rtl", false),
        DataRow("rtl", true)]
    public async Task TheMapPopupSitsOverItsMarkerInEitherDirection(string dir, bool pan)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/dir-resolved-map?dir={dir}&pan={(pan ? "true" : "false")}");
        await WaitForStatus("Ready");

        var marker = Page.Locator(".bit-map .leaflet-marker-icon").First;
        await marker.WaitForAsync(new() { Timeout = DefaultTimeout });
        await Page.WaitForTimeoutAsync(500);

        await marker.ClickAsync();

        var popup = Page.Locator(".bit-map-popup");
        await popup.WaitForAsync(new() { Timeout = DefaultTimeout });

        // The anchor follows the marker every frame and a pan is spent once the popup has laid out, so the two are read
        // once they have both stopped moving.
        var placement = await Page.WaitForFunctionAsync(@"() => {
            const popup = document.querySelector('.bit-map-popup');
            const marker = document.querySelector('.bit-map .leaflet-marker-icon');
            const map = document.querySelector('.bit-map');
            if (!popup || !marker || !map) return null;
            const p = popup.getBoundingClientRect(), m = marker.getBoundingClientRect(), f = map.getBoundingClientRect();
            const now = JSON.stringify([Math.round(p.left), Math.round(p.top), Math.round(m.left), Math.round(m.top)]);
            const last = window.__mapPlacement;
            window.__mapPlacement = now;
            if (now !== last) return null;
            return JSON.stringify({
                popupCentre: (p.left + p.right) / 2, markerCentre: (m.left + m.right) / 2,
                popupLeft: p.left, popupRight: p.right, popupBottom: p.bottom, markerBottom: m.bottom,
                mapLeft: f.left, mapRight: f.right,
                direction: getComputedStyle(popup).direction,
            });
        }", null, new() { Timeout = DefaultTimeout, PollingInterval = 200 });

        var read = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await placement.JsonValueAsync<string>())!;
        double Value(string name) => read[name].GetDouble();

        Assert.AreEqual(Value("markerCentre"), Value("popupCentre"), 2, $"The popup is not centred over its marker ({dir}, pan {pan}).");
        Assert.IsTrue(Value("popupBottom") < Value("markerBottom"), $"The popup is not above its marker ({dir}, pan {pan}).");
        Assert.IsTrue(Value("popupLeft") >= Value("mapLeft") && Value("popupRight") <= Value("mapRight"),
            $"The popup is outside the map ({Value("popupLeft")}..{Value("popupRight")} in {Value("mapLeft")}..{Value("mapRight")}; {dir}, pan {pan}).");

        // Only the anchor is placed physically: the popup's content still reads the way the map does.
        Assert.AreEqual(dir, read["direction"].GetString());
    }

    private async Task WaitForFocus(string set, string probe, string text)
        => await Page.WaitForFunctionAsync($"() => document.querySelector('{set} [data-probe={probe}]').contains(document.activeElement) && document.activeElement.textContent.trim() === '{text}'",
            null, new() { Timeout = DefaultTimeout });

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        // The components set up their keys and gestures in calls to the browser of their own once they have rendered,
        // which nothing on the page reports; a key pressed before they land (WebKit drives the page fast enough to
        // press one) is a key nothing was listening for yet.
        await Page.WaitForTimeoutAsync(500);
    }

    private async Task<Dictionary<string, string?>> Read(string set)
        => JsonSerializer.Deserialize<Dictionary<string, string?>>(await Page.EvaluateAsync<string>(ReadSet, set), _json)!;

    private static void AssertSet(Dictionary<string, string?> actual, Dictionary<string, string?> ltr, Dictionary<string, string?> rtl, string expected, string where)
    {
        var wrong = Probes.Select(probe =>
        {
            var value = actual[probe];
            var reads = value == ltr[probe] ? "ltr" : value == rtl[probe] ? "rtl" : $"neither ({value ?? "missing"})";

            return reads == expected ? null : $"{probe} reads {reads}, expected {expected}";
        }).Where(message => message is not null).ToArray();

        Assert.AreEqual(0, wrong.Length, $"{where}:\n{string.Join("\n", wrong)}");
    }
}
