using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the theme API with <c>bit-theme-view-transition</c> on the document element (#13680).
///
/// There the <c>bit-theme</c> attribute is written by the View Transition's callback, which the browser runs a frame
/// after <c>Theme.set</c> returns. In that window reading the theme back has to return the theme on its way in, a
/// toggle has to act on it (so two in a row end where they started), and a later swap has to win over the waiting
/// one whichever write lands first - a write from outside the library, an OS appearance change and another tab's
/// choice included. The change is announced once the attribute is written, so a handler reading the page finds the
/// new theme there. bUnit has no View Transitions, so this is pinned down in a real browser; <c>BitThemeManager</c>
/// reads and writes through the same <c>BitBlazorUI.Theme</c> calls driven here. Run them with BROWSER=firefox and
/// BROWSER=webkit too.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitThemeViewTransitionBrowserTests
public class BitThemeViewTransitionBrowserTests : PerformanceTestBase
{
    private const string Light = "fluent2-light";
    private const string Dark = "fluent2-dark";

    // Sets the page up the way an app that opts into the transition is, on the given theme, and records every View
    // Transition the library starts, every one it skips itself, and every change it announces with the theme painted
    // when it was announced, so a test can wait for each transition's callback to have run and read what happened.
    private const string Setup = @"(theme) => {
        document.documentElement.setAttribute('bit-theme-view-transition', '');
        BitBlazorUI.Theme.init({ lightTheme: 'fluent2-light', darkTheme: 'fluent2-dark', theme });

        window.__bitTransitions = [];
        window.__bitSkipped = [];
        const start = document.startViewTransition.bind(document);
        document.startViewTransition = callback => {
            const transition = start(callback);
            const index = window.__bitTransitions.push(transition) - 1;
            const skip = transition.skipTransition.bind(transition);
            transition.skipTransition = () => {
                window.__bitSkipped.push(index);
                skip();
            };
            return transition;
        };

        window.__bitChanges = [];
        document.addEventListener('bit-theme-change', e => window.__bitChanges.push({
            ...e.detail,
            painted: document.documentElement.getAttribute('bit-theme')
        }));
    }";

    // Waits for the callback of every transition started so far - a skipped one's included, which the browser still
    // runs - and reads the theme painted and the theme reported after them, the transitions the library skipped and
    // the changes it announced.
    private const string Settle = @"async () => {
        await Promise.allSettled(window.__bitTransitions.map(t => t.updateCallbackDone));
        return JSON.stringify({
            painted: document.documentElement.getAttribute('bit-theme'),
            reported: BitBlazorUI.Theme.get(),
            skipped: window.__bitSkipped,
            changes: window.__bitChanges
        });
    }";

    [TestMethod]
    public async Task Get_RightAfterSet_ReturnsTheNewTheme()
    {
        await SetUp(Light);

        var during = await Read(@"() => {
            const set = BitBlazorUI.Theme.set('fluent2-dark');
            return JSON.stringify({
                set,
                reported: BitBlazorUI.Theme.get(),
                painted: document.documentElement.getAttribute('bit-theme'),
                started: window.__bitTransitions.length
            });
        }");

        // The window the issue is about: a transition is running and has not written the attribute yet.
        Assert.AreEqual(1, during.GetProperty("started").GetInt32(), "The swap should run inside a View Transition.");
        Assert.AreEqual(Light, during.GetProperty("painted").GetString(), "The transition's callback writes the attribute a frame later.");

        Assert.AreEqual(Dark, during.GetProperty("set").GetString());
        Assert.AreEqual(Dark, during.GetProperty("reported").GetString(), "get() should return the theme on its way in.");

        // And reading it in the window must not have made the outgoing theme the current one again.
        var after = await Read(Settle);
        Assert.AreEqual(Dark, after.GetProperty("painted").GetString());
        Assert.AreEqual(Dark, after.GetProperty("reported").GetString());
    }

    [TestMethod]
    public async Task Set_AnnouncesTheChangeOnceTheThemeIsOnThePage()
    {
        await SetUp(Light);

        var during = await Read(@"() => {
            BitBlazorUI.Theme.set('fluent2-dark');
            return JSON.stringify(window.__bitChanges.length);
        }");

        Assert.AreEqual(0, during.GetInt32(), "The change is announced once the transition's callback has written it.");

        var changes = (await Read(Settle)).GetProperty("changes");
        Assert.AreEqual(1, changes.GetArrayLength());
        Assert.AreEqual(Dark, changes[0].GetProperty("newTheme").GetString());
        Assert.AreEqual(Light, changes[0].GetProperty("oldTheme").GetString());
        Assert.AreEqual(Dark, changes[0].GetProperty("painted").GetString(), "A handler reading the page should find the new theme there.");
    }

    [TestMethod]
    public async Task ToggleDarkLight_TwiceInARow_EndsWhereItStarted()
    {
        await SetUp(Dark);

        var toggles = await Read(@"() => JSON.stringify([
            BitBlazorUI.Theme.toggleDarkLight(),
            BitBlazorUI.Theme.toggleDarkLight()
        ])");

        Assert.AreEqual(Light, toggles[0].GetString());
        Assert.AreEqual(Dark, toggles[1].GetString(), "The second toggle should act on the theme the first one is applying.");

        var after = await Read(Settle);
        Assert.AreEqual(Dark, after.GetProperty("painted").GetString(), "The first toggle's transition must not write over the second toggle.");
        Assert.AreEqual(Dark, after.GetProperty("reported").GetString());
    }

    [TestMethod]
    public async Task Set_BackToThePaintedThemeBeforeTheTransitionRuns_EndsOnThatThemeAndSkipsTheTransition()
    {
        await SetUp(Light);

        var during = await Read(@"() => {
            BitBlazorUI.Theme.set('fluent2-dark');
            const back = BitBlazorUI.Theme.set('fluent2-light');
            return JSON.stringify({ back, reported: BitBlazorUI.Theme.get() });
        }");

        Assert.AreEqual(Light, during.GetProperty("back").GetString());
        Assert.AreEqual(Light, during.GetProperty("reported").GetString());

        // The second set finds its theme already painted and writes it at once; the first one's callback runs after
        // that, and must write nothing.
        var after = await Read(Settle);
        Assert.AreEqual(Light, after.GetProperty("painted").GetString(), "The superseded transition must not write its theme.");
        Assert.AreEqual(Light, after.GetProperty("reported").GetString());
        AssertSkipped(after, 0);
    }

    [TestMethod]
    public async Task Set_TwiceInARow_EndsOnTheSecondAndAnnouncesTheFirstAsReplaced()
    {
        await SetUp(Light);

        await Read(@"() => {
            BitBlazorUI.Theme.set('fluent2-dark');
            BitBlazorUI.Theme.set('fluent-dark');
            return JSON.stringify(null);
        }");

        var after = await Read(Settle);
        Assert.AreEqual("fluent-dark", after.GetProperty("painted").GetString());
        Assert.AreEqual("fluent-dark", after.GetProperty("reported").GetString());

        var changes = after.GetProperty("changes");
        Assert.AreEqual(2, changes.GetArrayLength());

        Assert.AreEqual(Dark, changes[0].GetProperty("newTheme").GetString());
        Assert.AreEqual(Light, changes[0].GetProperty("oldTheme").GetString());

        Assert.AreEqual("fluent-dark", changes[1].GetProperty("newTheme").GetString());
        Assert.AreEqual(Dark, changes[1].GetProperty("oldTheme").GetString(), "The theme being replaced is the one the first set applied.");
    }

    [TestMethod]
    public async Task Set_TheThemeOnItsWayIn_LeavesTheTransitionRunningAndAnnouncesOnce()
    {
        await SetUp(Light);

        var started = await Read(@"() => {
            BitBlazorUI.Theme.set('fluent2-dark');
            BitBlazorUI.Theme.set('fluent2-dark');
            return JSON.stringify(window.__bitTransitions.length);
        }");

        Assert.AreEqual(1, started.GetInt32(), "Re-setting the theme on its way in should start no second transition.");

        var after = await Read(Settle);
        Assert.AreEqual(Dark, after.GetProperty("painted").GetString());
        AssertSkipped(after);

        var changes = after.GetProperty("changes");
        Assert.AreEqual(1, changes.GetArrayLength(), "Re-setting the theme on its way in is no change.");
        Assert.AreEqual(Light, changes[0].GetProperty("oldTheme").GetString());
    }

    [TestMethod]
    public async Task ExternalWrite_WhileTheTransitionWaits_IsReportedAndKept()
    {
        await SetUp(Light);

        var during = await Read(@"() => {
            BitBlazorUI.Theme.set('fluent2-dark');
            document.documentElement.setAttribute('bit-theme', 'fluent-light');
            return JSON.stringify(BitBlazorUI.Theme.get());
        }");

        Assert.AreEqual("fluent-light", during.GetString(), "get() should report what a write from outside put on the page.");

        var after = await Read(Settle);
        Assert.AreEqual("fluent-light", after.GetProperty("painted").GetString(), "The transition must not write over the later write from outside.");
        Assert.AreEqual("fluent-light", after.GetProperty("reported").GetString());
    }

    [TestMethod]
    public async Task OsAppearanceChange_BackBeforeTheTransitionRuns_EndsOnTheOsThemeAndSkipsTheTransition()
    {
        await SetUp(Light);

        // A stand-in for the OS appearance the library follows, which a test can flip twice within one frame.
        await Read(@"() => {
            const scheme = new EventTarget();
            scheme.media = '(prefers-color-scheme: dark)';
            scheme.matches = false;
            window.__bitScheme = scheme;
            const matchMedia = window.matchMedia.bind(window);
            window.matchMedia = query => query === scheme.media ? scheme : matchMedia(query);

            BitBlazorUI.Theme.useSystem();
            return JSON.stringify(null);
        }");

        await Read(@"() => {
            window.__bitScheme.matches = true;
            window.__bitScheme.dispatchEvent(new Event('change'));
            window.__bitScheme.matches = false;
            window.__bitScheme.dispatchEvent(new Event('change'));
            return JSON.stringify(null);
        }");

        var after = await Read(Settle);
        Assert.AreEqual(Light, after.GetProperty("painted").GetString(), "The OS is back on light, so the dark transition must not write.");
        Assert.AreEqual(Light, after.GetProperty("reported").GetString());
        AssertSkipped(after, 0);
    }

    [TestMethod]
    public async Task OtherTabChoice_BeforeTheTransitionRuns_EndsOnThatChoiceAndSkipsTheTransition()
    {
        await SetUp(Light);

        await Read(@"() => {
            localStorage.removeItem('bit-current-theme');
            BitBlazorUI.Theme.init({ persist: true });

            BitBlazorUI.Theme.set('fluent2-dark');
            window.dispatchEvent(new StorageEvent('storage', { key: 'bit-current-theme', newValue: 'fluent2-light' }));
            return JSON.stringify(null);
        }");

        var after = await Read(Settle);
        Assert.AreEqual(Light, after.GetProperty("painted").GetString(), "The other tab's choice came last, so the dark transition must not write.");
        Assert.AreEqual(Light, after.GetProperty("reported").GetString());
        AssertSkipped(after, 0);
    }

    [TestMethod]
    public async Task Set_WithAWrapperThatDropsTheCallback_StillWritesTheTheme()
    {
        await SetUp(Light);

        await Read(@"() => {
            const start = document.startViewTransition;
            document.startViewTransition = () => start(() => { });

            BitBlazorUI.Theme.set('fluent2-dark');
            return JSON.stringify(null);
        }");

        var after = await Read(Settle);
        Assert.AreEqual(Dark, after.GetProperty("painted").GetString(), "The theme should be written once the transition's update is done.");
        Assert.AreEqual(Dark, after.GetProperty("reported").GetString());
        Assert.AreEqual(1, after.GetProperty("changes").GetArrayLength());
    }

    [TestMethod]
    public async Task Set_WithASynchronousPolyfill_WritesAtOnceAndKeepsWorking()
    {
        await SetUp(Light);

        var result = await Read(@"() => {
            document.startViewTransition = callback => { callback(); };

            BitBlazorUI.Theme.set('fluent2-dark');
            const first = document.documentElement.getAttribute('bit-theme');
            BitBlazorUI.Theme.set('fluent2-light');
            return JSON.stringify({
                first,
                second: document.documentElement.getAttribute('bit-theme'),
                reported: BitBlazorUI.Theme.get()
            });
        }");

        Assert.AreEqual(Dark, result.GetProperty("first").GetString());
        Assert.AreEqual(Light, result.GetProperty("second").GetString());
        Assert.AreEqual(Light, result.GetProperty("reported").GetString());
    }

    private static void AssertSkipped(JsonElement settled, params int[] expected)
    {
        var skipped = settled.GetProperty("skipped").EnumerateArray().Select(i => i.GetInt32()).ToArray();
        CollectionAssert.AreEqual(expected, skipped, "The library should skip exactly the transitions a direct write superseded.");
    }

    private async Task SetUp(string theme)
    {
        // The library falls back to a plain write under reduced motion, which would leave no window to test.
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });

        if (await Page.EvaluateAsync<bool>("() => typeof document.startViewTransition === 'function'") is false)
        {
            Assert.Inconclusive("This engine has no View Transitions, so the theme is written at once and there is no window to test.");
        }

        await Page.EvaluateAsync(Setup, theme);
    }

    private async Task<JsonElement> Read(string script)
    {
        var json = await Page.EvaluateAsync<string>(script);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
