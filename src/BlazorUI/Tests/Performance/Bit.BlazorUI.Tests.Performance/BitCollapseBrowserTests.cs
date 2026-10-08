using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the end of a BitCollapse transition, which has to be the one the browser plays -
/// whatever set its pace - rather than the 300ms the component would guess from its parameters.
///
/// Each test closes a collapse from inside the page and samples it on every animation frame, so what is asserted
/// is what was on the screen, frame by frame, in each engine: run them with BROWSER=firefox and BROWSER=webkit as
/// well. WebKit is the one that tells the approaches apart - it lays out content mid-way through a discrete
/// content-visibility transition as already hidden, which snaps a searchable section shut the moment it closes.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitCollapseBrowserTests
public class BitCollapseBrowserTests : PerformanceTestBase
{
    // The pace every collapse on the page closes at, and what an end that landed early or late would be told by:
    // a guess of 300ms, or a hidden attribute that took the content off the screen as the close started.
    private const double CloseDuration = 1500;
    private const double Margin = 250;

    private const string PageUrl = "/regression/collapse-transition";

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public async Task BitCollapse_HiddenUntilFound_Close_StaysDrawnAndInertUntilItHasPlayed()
    {
        await Open();

        var samples = await ClickAndSample("btn-huf", "col-huf", null, CloseDuration + 1000);
        var start = StartOfClose(samples);

        // The content stays drawn - but inert - for the whole of the close, while the track shrinks, and only then is
        // it handed to hidden="until-found".
        foreach (var sample in samples.Where(s => s.Collapsed && s.T < start + CloseDuration - Margin))
        {
            Assert.IsNull(sample.Hidden, $"hidden=\"until-found\" landed {sample.T - start:0}ms into a {CloseDuration}ms close.");
            Assert.IsTrue(sample.Inert, $"The closing content has to be inert {sample.T - start:0}ms into the close.");
            Assert.IsTrue(sample.Drawn, $"The content was hidden {sample.T - start:0}ms into a {CloseDuration}ms close.");
        }

        var midway = samples.First(s => s.T >= start + CloseDuration / 2);
        Assert.IsTrue(midway.Height > 0 && midway.Height < samples[0].Height, $"The track is not shrinking midway ({midway.Height}px).");

        // Once the close has played, the attribute is what hides the content, and inert comes off so it can be found.
        var last = samples[^1];
        Assert.AreEqual("until-found", last.Hidden);
        Assert.IsFalse(last.Drawn, "The content is still drawn after the close.");
        Assert.IsFalse(last.Inert, "A searchable section has to come out of inert once it is closed.");
        Assert.AreEqual(0, last.Height, 0.5);

        var hiddenAt = samples.First(s => s.T > start && s.Drawn is false).T - start;
        Assert.IsTrue(hiddenAt >= CloseDuration - Margin, $"The content was hidden {hiddenAt:0}ms into the close.");

        var inertOffAt = samples.First(s => s.T > start && s.Inert is false).T - start;
        Assert.IsTrue(inertOffAt >= CloseDuration - Margin, $"inert came off {inertOffAt:0}ms into the close.");
    }

    [TestMethod]
    public async Task BitCollapse_HiddenUntilFound_ClosingContent_CannotTakeTheFocus()
    {
        await Open();

        await Page.ClickAsync("#btn-huf");
        await Page.WaitForSelectorAsync("#col-huf.bit-col-col", new() { State = WaitForSelectorState.Attached });

        var focused = await Page.EvaluateAsync<bool>(@"() => {
            const button = document.getElementById('huf-inner');
            button.focus();
            return document.activeElement === button;
        }");

        Assert.IsFalse(focused, "A button inside a closing searchable section took the focus.");
    }

    [TestMethod]
    public async Task BitCollapse_HiddenUntilFound_ClosedSection_OpensForAFragmentInsideIt()
    {
        await Open();

        if (await Page.EvaluateAsync<bool>("() => 'onbeforematch' in document.body") is false)
        {
            Assert.Inconclusive("This engine does not know hidden=\"until-found\", so there is nothing to search.");
        }

        await Page.ClickAsync("#btn-huf");
        await WaitUntilClosedForGood("col-huf");

        await Page.EvaluateAsync("() => location.hash = 'huf-target'");

        await Page.WaitForSelectorAsync("#col-huf.bit-col-exp", new() { State = WaitForSelectorState.Attached, Timeout = 5000 });
    }

    [TestMethod]
    public async Task BitCollapse_HiddenUntilFound_Open_DrawsTheContentAtOnce()
    {
        await Open();

        await Page.ClickAsync("#btn-huf");
        await WaitUntilClosedForGood("col-huf");

        var samples = await ClickAndSample("btn-huf", "col-huf", null, 400);

        // From the first frame the track has any size, the content is drawn and reachable.
        var opening = samples.Where(s => s.Collapsed is false).ToList();

        Assert.IsTrue(opening.Count > 0);

        foreach (var sample in opening)
        {
            Assert.IsFalse(sample.Inert, $"at {sample.T:0}ms");
            Assert.IsNull(sample.Hidden, $"at {sample.T:0}ms");

            if (sample.Height > 0)
            {
                Assert.IsTrue(sample.Drawn, $"The content is not drawn {sample.T:0}ms into the open, with the track at {sample.Height}px.");
            }
        }

        Assert.IsTrue(opening[^1].Height > 0, "The track is not growing.");
    }

    [TestMethod]
    public async Task BitCollapse_UnmountOnCollapse_UnderASlowerPace_UnmountsOnlyOnceTheCloseHasPlayed()
    {
        await Open();

        var samples = await ClickAndSample("btn-unmount", "col-unmount", "unmount-content", CloseDuration + 1000);
        var start = StartOfClose(samples);

        var midway = samples.First(s => s.T >= start + CloseDuration / 2);
        Assert.IsTrue(midway.Height > 0 && midway.Height < samples[0].Height, $"The track is not shrinking midway ({midway.Height}px).");

        var unmountedAt = samples.First(s => s.HasContent is false).T - start;
        Assert.IsTrue(unmountedAt >= CloseDuration - Margin, $"The content was unmounted {unmountedAt:0}ms into a {CloseDuration}ms close.");

        await Expect(Page.Locator("#unmount-collapsed-at")).Not.ToHaveTextAsync("-");
        var collapsedAt = int.Parse(await Page.Locator("#unmount-collapsed-at").InnerTextAsync());
        Assert.IsTrue(collapsedAt >= CloseDuration - Margin, $"OnCollapsed fired {collapsedAt}ms into a {CloseDuration}ms close.");
    }

    [TestMethod]
    public async Task BitCollapse_OnCollapsedHandedDownMidClose_FiresAtThePlayedEnd()
    {
        await Open();

        // The handler is handed down from inside the page on the frame the close is drawn, well before the 300ms the
        // component estimates: one that arrives after the estimate has run out is one nothing was waiting with.
        await Page.EvaluateAsync(@"() => new Promise(resolve => {
            document.getElementById('btn-late').click();
            const root = document.getElementById('col-late');
            const tick = () => {
                if (root.classList.contains('bit-col-col')) {
                    document.getElementById('btn-late-handler').click();
                    resolve();
                } else {
                    requestAnimationFrame(tick);
                }
            };
            requestAnimationFrame(tick);
        })");

        await Expect(Page.Locator("#late-collapsed-at")).Not.ToHaveTextAsync("-", new() { Timeout = 5000 });
        var collapsedAt = int.Parse(await Page.Locator("#late-collapsed-at").InnerTextAsync());

        // Timed off the 300ms estimate it would have fired more than a second early.
        Assert.IsTrue(collapsedAt >= CloseDuration - Margin, $"OnCollapsed fired {collapsedAt}ms into a {CloseDuration}ms close.");
        Assert.IsTrue(collapsedAt <= CloseDuration + 1000, $"OnCollapsed fired {collapsedAt}ms into a {CloseDuration}ms close.");
    }

    [TestMethod]
    public async Task BitCollapse_ReducedMotion_EndsTheCloseAtOnce()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Open();

        var samples = await ClickAndSample("btn-huf", "col-huf", null, 800);
        var start = StartOfClose(samples);

        var hiddenAt = samples.First(s => s.T > start && s.Drawn is false).T - start;
        Assert.IsTrue(hiddenAt < 400, $"The content was hidden {hiddenAt:0}ms into a close reduced motion collapsed.");

        var inertOffAt = samples.First(s => s.T > start && s.Inert is false).T - start;
        Assert.IsTrue(inertOffAt < 400, $"inert came off {inertOffAt:0}ms into a close reduced motion collapsed.");

        var unmount = await ClickAndSample("btn-unmount", "col-unmount", "unmount-content", 800);
        var unmountedAt = unmount.First(s => s.HasContent is false).T - StartOfClose(unmount);
        Assert.IsTrue(unmountedAt < 400, $"The content was unmounted {unmountedAt:0}ms into a close reduced motion collapsed.");
    }

    [TestMethod]
    public async Task BitCollapse_ReportsWhatTheEngineSupports()
    {
        await Open();

        // Printed for the run log: what the engine knows of the features the collapse leans on.
        var support = await Page.EvaluateAsync<string>(@"() => JSON.stringify({
            userAgent: navigator.userAgent,
            untilFound: 'onbeforematch' in document.body,
            allowDiscrete: CSS.supports('transition-behavior', 'allow-discrete'),
            contentVisibility: CSS.supports('content-visibility', 'hidden'),
            getAnimations: typeof document.body.getAnimations === 'function'
        })");

        Console.WriteLine(support);
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    // A searchable section whose close has played: out of inert, and hidden by the attribute.
    private Task WaitUntilClosedForGood(string root)
    {
        return Page.WaitForFunctionAsync(@"(root) => {
            const r = document.getElementById(root);
            const con = r.querySelector(':scope > .bit-col-con');
            const style = getComputedStyle(con);
            return r.classList.contains('bit-col-col') && con.hasAttribute('inert') === false &&
                   (style.contentVisibility === 'hidden' || style.display === 'none');
        }", root, new() { Timeout = DefaultTimeout });
    }

    // When the browser applied the new state, which is where the transition it plays starts.
    private static double StartOfClose(List<Sample> samples)
    {
        var collapsed = samples.First(s => s.Collapsed);

        return collapsed.Start ?? collapsed.T;
    }

    // Clicks the button from inside the page and records the collapse on every frame from then on, for the given ms.
    private async Task<List<Sample>> ClickAndSample(string button, string root, string? content, double duration)
    {
        var json = await Page.EvaluateAsync<string>(@"async ([button, root, content, duration]) => {
            const r = document.getElementById(root);
            const con = r.querySelector(':scope > .bit-col-con');
            const samples = [];
            const t0 = performance.now();
            // When the size transition the browser is playing started, on the clock t is read off - the
            // document timeline and performance.now() share their origin. It times the close from the moment the
            // browser started it rather than from the first frame the page got to look, which a busy engine can
            // hand out late.
            const trackStart = () => {
                const track = r.getAnimations().find(a => (a.transitionProperty || '').startsWith('grid-template'));
                return track && track.startTime !== null ? track.startTime - t0 : null;
            };
            const sample = () => {
                const style = getComputedStyle(con);
                samples.push({
                    t: performance.now() - t0,
                    start: trackStart(),
                    collapsed: r.classList.contains('bit-col-col'),
                    height: r.getBoundingClientRect().height,
                    contentVisibility: style.contentVisibility || '',
                    display: style.display,
                    inert: con.hasAttribute('inert'),
                    hidden: con.getAttribute('hidden'),
                    hasContent: content ? document.getElementById(content) !== null : true
                });
            };
            sample();
            document.getElementById(button).click();
            await new Promise(resolve => {
                const tick = () => {
                    sample();
                    if (performance.now() - t0 < duration) requestAnimationFrame(tick); else resolve();
                };
                requestAnimationFrame(tick);
            });
            return JSON.stringify(samples);
        }", new object?[] { button, root, content, duration });

        return JsonSerializer.Deserialize<List<Sample>>(json, _json)!;
    }

    private sealed record Sample(double T, double? Start, bool Collapsed, double Height, string ContentVisibility, string Display, bool Inert, string? Hidden, bool HasContent)
    {
        public bool Drawn => ContentVisibility != "hidden" && Display != "none";
    }
}
