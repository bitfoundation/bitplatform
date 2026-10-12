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
            'swiper': () => style('swiper', '.bit-swp-rbt .bit-swp-dch')?.scale,
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

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
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
