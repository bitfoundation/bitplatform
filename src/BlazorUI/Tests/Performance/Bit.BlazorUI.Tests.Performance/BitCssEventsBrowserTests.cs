using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the CSS animation and transition directives Bit.BlazorUI.Events registers, whose
/// arguments only bit.blazorui.js can supply: the bUnit tests hand the handlers ready-made arguments, so a field the
/// script misnames, a registration that never happens or one that throws on an app's own are only seen here, with the
/// browser playing the animations. Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitCssEventsBrowserTests
public class BitCssEventsBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/css-events";

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public async Task Animation_HandsTheHandlersTheArgumentsOfTheBrowsersEvents()
    {
        await Open();

        await Run("ev-animated");
        var log = await WaitForLog("ev-outer animationend");

        CollectionAssert.Contains(log, "ev-animated animationstart name=css-events-pulse target=ev-animated pseudo= elapsed=0");
        CollectionAssert.Contains(log, "ev-animated animationiteration name=css-events-pulse target=ev-animated pseudo= elapsed=0.2");
        CollectionAssert.Contains(log, "ev-animated animationend name=css-events-pulse target=ev-animated pseudo= elapsed=0.4");

        // The end bubbles, and the target id is what tells an ancestor its content's end from its own.
        CollectionAssert.Contains(log, "ev-outer animationend name=css-events-pulse target=ev-animated pseudo= elapsed=0.4");
    }

    [TestMethod]
    public async Task Animation_StopPropagation_KeepsTheEndFromTheAncestors()
    {
        await Open();

        await Run("ev-stopped");
        var log = await WaitForLog("ev-stopped animationend");

        CollectionAssert.Contains(log, "ev-stopped animationend name=css-events-pulse target=ev-stopped-inner pseudo= elapsed=0.1");

        // A later end that does bubble is what shows the stopped one had its chance to reach the ancestor first.
        await Run("ev-animated");
        log = await WaitForLog("ev-outer animationend");

        Assert.IsFalse(log.Any(l => l.StartsWith("ev-outer") && l.Contains("target=ev-stopped-inner")), string.Join("\n", log));
    }

    [TestMethod]
    public async Task Animation_OfAPseudoElement_NamesThePseudoElement()
    {
        await Open();

        await Run("ev-pseudo");
        var log = await WaitForLog("ev-pseudo animationend");

        CollectionAssert.Contains(log, "ev-pseudo animationend name=css-events-pulse target=ev-pseudo pseudo=::before elapsed=0.1");
    }

    [TestMethod]
    public async Task Animation_OfNoDuration_StillStartsAndEnds()
    {
        await Open();

        await Run("ev-zero-animation");
        var log = await WaitForLog("ev-zero-animation animationend");

        CollectionAssert.Contains(log, "ev-zero-animation animationstart name=css-events-pulse target=ev-zero-animation pseudo= elapsed=0");
        CollectionAssert.Contains(log, "ev-zero-animation animationend name=css-events-pulse target=ev-zero-animation pseudo= elapsed=0");
    }

    [TestMethod]
    public async Task Animation_TakenOffMidway_IsCanceled()
    {
        await Open();

        if (await Page.EvaluateAsync<bool>("() => 'onanimationcancel' in window") is false)
        {
            Assert.Inconclusive("This engine does not fire animationcancel.");
        }

        await Run("ev-cancel");
        // Only an animation that has started is canceled: one taken off while still pending fires nothing.
        await Page.WaitForFunctionAsync("() => document.getElementById('ev-cancel').getAnimations().some(a => a.currentTime > 50)");
        await Page.EvaluateAsync("() => document.getElementById('ev-cancel').classList.remove('run')");

        var log = await WaitForLog("ev-cancel animationcancel");

        Assert.IsTrue(log.Any(l => l.StartsWith("ev-cancel animationcancel name=css-events-pulse target=ev-cancel pseudo= elapsed=")), string.Join("\n", log));
    }

    [TestMethod]
    public async Task Transition_HandsTheHandlersTheArgumentsOfTheBrowsersEvents()
    {
        await Open();

        await Run("ev-transitioned");
        var log = await WaitForLog("ev-outer transitionend");

        CollectionAssert.Contains(log, "ev-transitioned transitionrun property=opacity target=ev-transitioned pseudo= elapsed=0");
        CollectionAssert.Contains(log, "ev-transitioned transitionstart property=opacity target=ev-transitioned pseudo= elapsed=0");
        CollectionAssert.Contains(log, "ev-transitioned transitionend property=opacity target=ev-transitioned pseudo= elapsed=0.15");
        CollectionAssert.Contains(log, "ev-outer transitionend property=opacity target=ev-transitioned pseudo= elapsed=0.15");
    }

    [TestMethod]
    public async Task Transition_OfNoDuration_FiresNothing()
    {
        await Open();

        await Run("ev-zero-transition");

        // A transition that does run, started after it, is what shows the page had time to report one.
        await Run("ev-transitioned");
        var log = await WaitForLog("ev-outer transitionend");

        Assert.IsFalse(log.Any(l => l.Contains("ev-zero-transition")), string.Join("\n", log));
    }

    [TestMethod]
    public async Task AppRegistration_AfterTheLibrarys_IsTakenInRatherThanThrown()
    {
        await Open();

        var outcome = await Page.EvaluateAsync<string>(@"() => {
            try {
                Blazor.registerCustomEventType('transitionrun', { createEventArgs: e => ({ propertyName: 'app:' + e.propertyName }) });
            } catch (e) {
                return 'first threw: ' + e.message;
            }
            try {
                Blazor.registerCustomEventType('transitionrun', { createEventArgs: e => ({}) });
                return 'second accepted';
            } catch (e) {
                return 'second threw: ' + e.message;
            }
        }");

        // The app's first registration is taken in; a second one still throws the way Blazor always has.
        StringAssert.StartsWith(outcome, "second threw:");
        StringAssert.Contains(outcome, "already registered");

        await Run("ev-app");
        var log = await WaitForLog("ev-app transitionrun");

        // The app's fields win a clash, and the library's are still there beside them.
        CollectionAssert.Contains(log, "ev-app transitionrun property=app:opacity target=ev-app pseudo= elapsed=0");
    }

    [TestMethod]
    public async Task Registration_HappensWhenBlazorLoadsAfterTheLibrarysScript()
    {
        var state = await OpenBarePage("""
            <script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>
            <script src="_framework/blazor.web.js" autostart="false"></script>
            """);

        Assert.AreEqual("registered=true owned=8 plain=true", state);
    }

    [TestMethod]
    public async Task Registration_HappensWhenBlazorIsInjectedAfterThePageHasLoaded()
    {
        var state = await OpenBarePage("""
            <script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>
            <script>
                window.addEventListener('load', () => setTimeout(() => {
                    const script = document.createElement('script');
                    script.src = '_framework/blazor.web.js';
                    script.setAttribute('autostart', 'false');
                    document.body.appendChild(script);
                }, 200));
            </script>
            """);

        Assert.AreEqual("registered=true owned=8 plain=true", state);
    }

    [TestMethod]
    public async Task Registration_FromAScriptLoadedAheadOfBlazor_StillHandsTheHandlersTheirArguments()
    {
        // The page as the host serves it, with the library's script moved into the head, ahead of Blazor's.
        await Page.RouteAsync($"{BaseUrl}{PageUrl}", async route =>
        {
            var response = await route.FetchAsync();
            var script = "<script src=\"_content/Bit.BlazorUI/scripts/bit.blazorui.js\"></script>";
            var html = (await response.TextAsync()).Replace(script, "").Replace("</head>", $"{script}</head>");

            await route.FulfillAsync(new() { Response = response, Body = html });
        });

        await Open();

        var order = await Page.EvaluateAsync<string>(@"() => [...document.scripts].map(s => s.getAttribute('src') || '').filter(s => s).join(' ')");
        StringAssert.StartsWith(order, "_content/Bit.BlazorUI/scripts/bit.blazorui.js _framework/blazor.web.js");

        await Run("ev-transitioned");
        var log = await WaitForLog("ev-transitioned transitionend");

        CollectionAssert.Contains(log, "ev-transitioned transitionend property=opacity target=ev-transitioned pseudo= elapsed=0.15");
    }

    [TestMethod]
    public async Task BitImage_FadeIn_LetsThePlaceholderGoOnceItsFadeHasPlayed()
    {
        await Open();

        var samples = await ShowImagesAndSample(3000);
        var fade = samples["img-fade"];

        Assert.IsNotNull(fade.LeavingAt, "The placeholder never started to leave.");
        Assert.IsNotNull(fade.GoneAt, "The placeholder was never taken out of the DOM.");
        Assert.AreEqual("bit-img-fade-out", fade.Animation);

        // Its fade is 600ms long: taken away early it would cut the cross-fade short.
        var stayed = fade.GoneAt!.Value - fade.LeavingAt!.Value;
        Assert.IsTrue(stayed >= 400, $"The placeholder was taken away {stayed:0}ms into a 600ms fade.");
    }

    [TestMethod]
    public async Task BitImage_FadeIn_LetsThePlaceholderGoOnceAFadeARestyleRenamedHasPlayed()
    {
        await Open();

        var samples = await ShowImagesAndSample(3000);
        var restyled = samples["img-restyled"];

        Assert.AreEqual("css-events-crossfade", restyled.Animation, "The restyle did not replace the fade.");
        Assert.IsNotNull(restyled.GoneAt, "A placeholder whose fade a restyle renamed was left over the image.");

        var stayed = restyled.GoneAt!.Value - restyled.LeavingAt!.Value;
        Assert.IsTrue(stayed >= 400, $"The placeholder was taken away {stayed:0}ms into a 600ms fade.");
    }

    [TestMethod]
    public async Task BitImage_FadeIn_UnderReducedMotion_LetsThePlaceholderGoAtOnce()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Open();

        var samples = await ShowImagesAndSample(2000);
        var reduced = samples["img-default"];

        Assert.IsNotNull(reduced.GoneAt, "A placeholder whose fade reduced motion collapsed was left over the image.");
        Assert.IsTrue(reduced.GoneAt < 1500, $"The placeholder was taken away {reduced.GoneAt:0}ms after the images were asked for.");
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
    }

    // Adds the class that starts the element's animation or transition, from inside the page.
    private Task Run(string id)
    {
        return Page.EvaluateAsync("(id) => document.getElementById(id).classList.add('run')", id);
    }

    // Waits for a line of what .NET received starting with the given text, and returns every line received so far.
    private async Task<List<string>> WaitForLog(string startOfLine)
    {
        await Page.WaitForFunctionAsync(@"(start) => document.getElementById('log').textContent
                                                     .split('\n').some(l => l.startsWith(start))",
                                        startOfLine, new() { Timeout = 10000 });

        var text = await Page.Locator("#log").TextContentAsync() ?? "";

        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // Serves a page of the host's origin that holds nothing but the given scripts, and reports what the library's
    // script made of the Blazor object once there is one.
    private async Task<string> OpenBarePage(string scripts)
    {
        var url = $"{BaseUrl}/regression/bare-page";

        await Page.RouteAsync(url, route => route.FulfillAsync(new()
        {
            ContentType = "text/html",
            Body = $"<!DOCTYPE html><html><head><base href=\"/\" /></head><body>{scripts}</body></html>"
        }));

        await Page.GotoAsync(url);
        await Page.WaitForFunctionAsync("() => window.Blazor && typeof window.Blazor.registerCustomEventType === 'function'",
                                        null, new() { Timeout = 10000 });

        return await Page.EvaluateAsync<string>(@"() => {
            const events = BitBlazorUI.Events;
            const descriptor = Object.getOwnPropertyDescriptor(window, 'Blazor');
            return `registered=${events._isRegistered} owned=${events._ownedEvents.length} plain=${'value' in descriptor && descriptor.writable}`;
        }");
    }

    // Asks for the images and follows each one's placeholder on every frame: when it began to leave, the animation it
    // left with, and when it was taken out of the DOM, in ms after the images were asked for.
    private async Task<Dictionary<string, ImageSample>> ShowImagesAndSample(double duration)
    {
        var json = await Page.EvaluateAsync<string>(@"async (duration) => {
            const ids = ['img-fade', 'img-restyled', 'img-default'];
            const samples = {};
            ids.forEach(id => samples[id] = { leavingAt: null, goneAt: null, animation: null, seen: false });
            const t0 = performance.now();
            const sample = () => {
                const t = performance.now() - t0;
                ids.forEach(id => {
                    const root = document.getElementById(id);
                    if (!root) return;
                    const s = samples[id];
                    const placeholder = root.querySelector('.bit-img-plc');
                    if (placeholder) {
                        s.seen = true;
                        if (placeholder.classList.contains('bit-img-pfo') && s.leavingAt === null) s.leavingAt = t;
                        const animation = placeholder.getAnimations().find(a => a.animationName);
                        if (animation && s.animation === null) s.animation = animation.animationName;
                    } else if (s.seen && s.goneAt === null) {
                        s.goneAt = t;
                    }
                });
            };
            document.getElementById('btn-images').click();
            await new Promise(resolve => {
                const tick = () => {
                    sample();
                    if (performance.now() - t0 < duration) requestAnimationFrame(tick); else resolve();
                };
                requestAnimationFrame(tick);
            });
            return JSON.stringify(samples);
        }", duration);

        return JsonSerializer.Deserialize<Dictionary<string, ImageSample>>(json, _json)!;
    }

    private sealed record ImageSample(double? LeavingAt, double? GoneAt, string? Animation, bool Seen);
}
