using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the close of a HiddenUntilFound BitAccordion, whose panel is handed to
/// hidden="until-found" only once the close the browser plays has finished - whatever the parameter or the theme made
/// its pace.
///
/// Each test closes a panel from inside the page and samples it on every animation frame, so what is asserted is what
/// was on the screen, frame by frame, in each engine: run them with BROWSER=firefox and BROWSER=webkit as well.
/// WebKit is the one that tells the approaches apart - it lays out a panel whose content-visibility is mid-way
/// through a transition as already hidden, which snaps the panel shut the moment the attribute lands.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitAccordionBrowserTests
public class BitAccordionBrowserTests : PerformanceTestBase
{
    private const double CloseDuration = 1500;
    private const double Margin = 250;

    private const string PageUrl = "/regression/accordion-transition";

    // The root of each accordion on the page, and the id of a span in its panel a fragment can point at.
    private const string ParamAccordion = "#acd-param";
    private const string ThemeAccordion = "#acd-theme";
    private const string ListAccordion = "#list .bit-acd";

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    [DataTestMethod]
    [DataRow(ParamAccordion)]
    [DataRow(ThemeAccordion)]
    [DataRow(ListAccordion)]
    public async Task BitAccordion_HiddenUntilFound_Close_PlaysOutBeforeThePanelIsHidden(string accordion)
    {
        await Open();

        var samples = await ClickAndSample(accordion, CloseDuration + 1000);
        var start = StartOfClose(samples);

        // The panel closes like any other - drawn, its row shrinking - and only then is it hidden until found.
        foreach (var sample in samples.Where(s => s.Collapsed && s.T < start + CloseDuration - Margin))
        {
            Assert.IsNull(sample.Hidden, $"hidden=\"until-found\" landed {sample.T - start:0}ms into a {CloseDuration}ms close.");
            Assert.IsTrue(sample.Drawn, $"The panel was hidden {sample.T - start:0}ms into a {CloseDuration}ms close.");
        }

        var midway = samples.First(s => s.T >= start + CloseDuration / 2);
        Assert.IsTrue(midway.Height > 0 && midway.Height < samples[0].Height, $"The panel is not shrinking midway ({midway.Height}px).");

        var last = samples[^1];
        Assert.AreEqual("until-found", last.Hidden);
        Assert.IsFalse(last.Drawn, "The panel is still drawn after the close.");
        Assert.AreEqual(0, last.Height, 0.5);

        var hiddenAt = samples.First(s => s.T > start && s.Hidden is not null).T - start;
        Assert.IsTrue(hiddenAt >= CloseDuration - Margin, $"hidden=\"until-found\" landed {hiddenAt:0}ms into the close.");
    }

    [DataTestMethod]
    [DataRow(ParamAccordion, "param-target")]
    [DataRow(ThemeAccordion, "theme-target")]
    [DataRow(ListAccordion, "list-target")]
    public async Task BitAccordion_HiddenUntilFound_ClosedPanel_OpensForAFragmentInsideIt(string accordion, string target)
    {
        await Open();

        await ClickAndSample(accordion, 100);
        await WaitUntilHiddenUntilFound(accordion);

        await Page.EvaluateAsync("(target) => location.hash = target", target);

        await Page.WaitForFunctionAsync("(root) => document.querySelector(root).classList.contains('bit-acd-exp')", accordion,
                                        new() { Timeout = 5000 });
    }

    [TestMethod]
    public async Task BitAccordion_HiddenUntilFound_Open_DrawsThePanelAtOnce()
    {
        await Open();

        await ClickAndSample(ParamAccordion, 100);
        await WaitUntilHiddenUntilFound(ParamAccordion);

        var samples = await ClickAndSample(ParamAccordion, 400);
        var opening = samples.Where(s => s.Collapsed is false).ToList();

        Assert.IsTrue(opening.Count > 0);

        foreach (var sample in opening)
        {
            Assert.IsNull(sample.Hidden, $"at {sample.T:0}ms");

            if (sample.Height > 0)
            {
                Assert.IsTrue(sample.Drawn, $"The panel is not drawn {sample.T:0}ms into the open, with the row at {sample.Height}px.");
            }
        }

        Assert.IsTrue(opening[^1].Height > 0, "The panel is not growing.");
    }

    [TestMethod]
    public async Task BitAccordion_HiddenUntilFound_ReducedMotion_HidesThePanelAtOnce()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Open();

        var samples = await ClickAndSample(ParamAccordion, 800);
        var start = StartOfClose(samples);

        var hiddenAt = samples.First(s => s.T > start && s.Hidden is not null).T - start;
        Assert.IsTrue(hiddenAt < 400, $"hidden=\"until-found\" landed {hiddenAt:0}ms into a close reduced motion collapsed.");
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    private Task WaitUntilHiddenUntilFound(string accordion)
    {
        return Page.WaitForFunctionAsync(@"(root) => {
            const con = document.querySelector(root).querySelector(':scope > .bit-acd-cnt > .bit-acd-cwr > .bit-acd-con');
            return con.getAttribute('hidden') === 'until-found' && getComputedStyle(con).contentVisibility === 'hidden';
        }", accordion, new() { Timeout = DefaultTimeout });
    }

    // When the browser applied the new state, which is where the transition it plays starts.
    private static double StartOfClose(List<Sample> samples)
    {
        var collapsed = samples.First(s => s.Collapsed);

        return collapsed.Start ?? collapsed.T;
    }

    // Clicks the header from inside the page and records the panel on every frame from then on, for the given ms.
    private async Task<List<Sample>> ClickAndSample(string accordion, double duration)
    {
        var json = await Page.EvaluateAsync<string>(@"async ([root, duration]) => {
            const r = document.querySelector(root);
            const row = r.querySelector(':scope > .bit-acd-cnt');
            const con = row.querySelector(':scope > .bit-acd-cwr > .bit-acd-con');
            const samples = [];
            const t0 = performance.now();
            // When the size transition the browser is playing started, on the clock t is read off - the
            // document timeline and performance.now() share their origin. It times the close from the moment the
            // browser started it rather than from the first frame the page got to look, which a busy engine can
            // hand out late.
            const trackStart = () => {
                const track = row.getAnimations().find(a => (a.transitionProperty || '').startsWith('grid-template'));
                return track && track.startTime !== null ? track.startTime - t0 : null;
            };
            const sample = () => {
                const style = getComputedStyle(con);
                samples.push({
                    t: performance.now() - t0,
                    start: trackStart(),
                    collapsed: r.classList.contains('bit-acd-exp') === false,
                    height: row.getBoundingClientRect().height,
                    contentVisibility: style.contentVisibility || '',
                    display: style.display,
                    hidden: con.getAttribute('hidden')
                });
            };
            sample();
            r.querySelector(':scope > .bit-acd-hwr .bit-acd-hdr').click();
            await new Promise(resolve => {
                const tick = () => {
                    sample();
                    if (performance.now() - t0 < duration) requestAnimationFrame(tick); else resolve();
                };
                requestAnimationFrame(tick);
            });
            return JSON.stringify(samples);
        }", new object[] { accordion, duration });

        return JsonSerializer.Deserialize<List<Sample>>(json, _json)!;
    }

    private sealed record Sample(double T, double? Start, bool Collapsed, double Height, string ContentVisibility, string Display, string? Hidden)
    {
        public bool Drawn => ContentVisibility != "hidden" && Display != "none";
    }
}
