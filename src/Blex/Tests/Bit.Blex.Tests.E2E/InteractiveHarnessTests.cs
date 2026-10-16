using Bit.Blex.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blex.Tests.E2E;

/// <summary>
/// Everything Blex promises once it runs interactively, asserted by its effect in a real browser. Each
/// interactive host - Server, WebAssembly and Auto, with and without prerendering, and BlazorWebView - runs
/// this same list, so a feature that works in one host and not another shows up as exactly that.
/// </summary>
/// <remarks>
/// The unit tests in Bit.Blex.Tests drive the bridges against a fake IJSRuntime. What only a browser shows
/// is the other half: that bit-blex-storage.js and bit-blex-devtools.js load from the package's static web
/// assets, that the browser's storage and its <c>storage</c> event behave the way the persistor relies on,
/// and that <c>&lt;BlexProvider&gt;</c> sequences hydration, history and the DevTools connection correctly
/// against a real renderer in every host.
/// </remarks>
public abstract class InteractiveHarnessTests : HarnessTest
{
    /// <summary>
    /// Stands in for the Redux DevTools browser extension: records what the bridge sends it, and keeps the
    /// listener the bridge subscribes so a test can send messages back the way the extension does.
    /// </summary>
    private const string FakeDevToolsExtension = """
        (() => {
            const recorder = { name: null, features: null, inits: [], sends: [], listener: null };
            window.__blexDevTools = recorder;
            window.__REDUX_DEVTOOLS_EXTENSION__ = {
                connect(options) {
                    recorder.name = options.name;
                    recorder.features = options.features;
                    return {
                        init(state) { recorder.inits.push(state); },
                        send(action, state) { recorder.sends.push({ type: action.type, payload: action.payload, state }); },
                        subscribe(listener) { recorder.listener = listener; return () => { recorder.listener = null; }; }
                    };
                }
            };
        })();
        """;

    /// <summary><c>RendererInfo.Name</c> values the host may render with (checked from .NET 9 on).</summary>
    protected abstract IReadOnlyList<string> ExpectedRenderers { get; }

    /// <summary><c>data-platform</c> values the host may render on: "dotnet" or "browser".</summary>
    protected abstract IReadOnlyList<string> ExpectedPlatforms { get; }

    /// <summary>Whether the suite can open a second page against the same origin and storage.</summary>
    protected virtual bool SupportsSecondPage => true;

    /// <summary>Opens a second page sharing the first one's origin and storage (another tab).</summary>
    protected virtual Task<IPage> OpenSecondPageAsync() => throw new NotSupportedException();

    [TestMethod]
    public async Task The_harness_runs_on_the_expected_runtime()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();

        var status = Page.Locator("#status");
        CollectionAssert.Contains(ExpectedPlatforms.ToList(), await status.GetAttributeAsync("data-platform"));

        if (E2EEnvironment.FrameworkReportsRendererName(Framework))
        {
            CollectionAssert.Contains(ExpectedRenderers.ToList(), await status.GetAttributeAsync("data-renderer"));
        }
    }

    [TestMethod]
    public async Task An_action_updates_the_store_and_re_renders_its_subscribers()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await ExpectCountAsync(0);

        await IncrementAsync(3);

        await Page.Locator("#reset").ClickAsync();
        await ExpectCountAsync(0);
    }

    [TestMethod]
    public async Task A_persisted_store_is_written_to_localStorage_and_rehydrated_after_a_reload()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await IncrementAsync(3);

        // Written by bit-blex-storage.js under the configured key prefix and the store's name.
        await Expect(Page.Locator("#count")).ToHaveAttributeAsync("data-count", "3");
        var stored = await PollAsync(() => LocalStorageAsync(CounterStorageKey), value => value?.Contains('3') is true);
        StringAssert.Contains(stored, "3", $"The counter store was not persisted under '{CounterStorageKey}'.");

        await GotoAsync("/");
        await WaitForInteractiveAsync();

        await ExpectCountAsync(3);
    }

    [TestMethod]
    public async Task The_storage_bridge_is_served_from_the_packages_static_web_assets()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await IncrementAsync(1);

        var requests = await PollAsync(BlexModuleRequestsAsync, found => found.Any(r => r.Url.Contains("bit-blex-storage.js", StringComparison.Ordinal)));

        var storage = requests.FirstOrDefault(r => r.Url.Contains("/_content/Bit.Blex.Blazor/bit-blex-storage.js", StringComparison.Ordinal));
        Assert.IsNotNull(storage, $"bit-blex-storage.js was never requested from _content/Bit.Blex.Blazor: {string.Join(", ", requests.Select(r => r.Url))}");

        // 0 is what a host that serves the asset from memory (BlazorWebView) reports.
        Assert.IsTrue(storage.Status is 200 or 0, $"bit-blex-storage.js answered {storage.Status}.");
    }

    [TestMethod]
    public async Task A_change_made_in_another_tab_reaches_this_one()
    {
        if (SupportsSecondPage is false) Assert.Inconclusive("This host has a single window, so there is no second tab to sync with.");

        await GotoAsync("/");
        await WaitForInteractiveAsync();

        var other = await OpenSecondPageAsync();
        await other.GotoAsync(BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForInteractiveAsync(other);
        await ExpectCountAsync(other, 0);

        // The browser fires `storage` in every other tab of the origin, never in the one that wrote:
        // SyncAcrossTabs rehydrates the store from it.
        await IncrementAsync(2);

        await ExpectCountAsync(other, 2);
    }

    [TestMethod]
    public async Task Undo_and_redo_step_through_the_recorded_history()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await IncrementAsync(2);

        await Page.Locator("#undo").ClickAsync();
        await ExpectCountAsync(1);

        await Page.Locator("#undo").ClickAsync();
        await ExpectCountAsync(0);

        await Page.Locator("#redo").ClickAsync();
        await ExpectCountAsync(1);

        await Page.Locator("#redo").ClickAsync();
        await ExpectCountAsync(2);
        await Expect(Page.Locator("#redo")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task A_selector_subscription_ignores_changes_to_state_it_does_not_read()
    {
        await GotoAsync("/selector");
        await WaitForInteractiveAsync();

        var watcher = Page.Locator("#name-watcher");
        await Expect(watcher).ToHaveAttributeAsync("data-name", "ada");
        var renders = await watcher.GetAttributeAsync("data-renders");

        for (var i = 1; i <= 3; i++)
        {
            await Page.Locator("#visit").ClickAsync();
            await Expect(Page.Locator("#visits")).ToHaveAttributeAsync("data-visits", i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        await Expect(watcher).ToHaveAttributeAsync("data-renders", renders!, new() { Timeout = 1_000 });

        await Page.Locator("#rename").ClickAsync();
        await Expect(watcher).ToHaveAttributeAsync("data-name", "ada!");
        Assert.AreNotEqual(renders, await watcher.GetAttributeAsync("data-renders"), "The selector's own field changed and its subscriber did not re-render.");
    }

    [TestMethod]
    public async Task A_latest_effect_supersedes_its_previous_run_and_can_be_cancelled()
    {
        await GotoAsync("/effects");
        await WaitForInteractiveAsync();

        var loading = Page.Locator("#loading");
        var result = Page.Locator("#result");

        // Cancelled outright: the loading flag drops and the body never gets to write its result.
        await Page.Locator("#load-slow").ClickAsync();
        await Expect(loading).ToHaveAttributeAsync("data-loading", "true");
        await Page.Locator("#cancel").ClickAsync();
        await Expect(loading).ToHaveAttributeAsync("data-loading", "false");

        // Superseded: Latest cancels the slow run when the fast one starts, so only the fast one finishes.
        await Page.Locator("#load-slow").ClickAsync();
        await Expect(loading).ToHaveAttributeAsync("data-loading", "true");
        await Page.Locator("#load-fast").ClickAsync();
        await Expect(result).ToHaveAttributeAsync("data-result", "loaded after 50ms");
        await Expect(loading).ToHaveAttributeAsync("data-loading", "false");

        // Past the point where the slow run would have finished, had it survived.
        await Page.WaitForTimeoutAsync(4_500);
        await Expect(result).ToHaveAttributeAsync("data-result", "loaded after 50ms");
    }

    [TestMethod]
    public async Task The_devtools_bridge_streams_actions_and_applies_time_travel()
    {
        await Page.AddInitScriptAsync(FakeDevToolsExtension);

        await GotoAsync("/");
        await WaitForInteractiveAsync();

        // Connected under the configured name, advertising only the features Blex implements.
        await Page.WaitForFunctionAsync("() => window.__blexDevTools && window.__blexDevTools.inits.length > 0 && window.__blexDevTools.listener", null, new() { Timeout = 30_000 });
        Assert.AreEqual("Blex harness", await Page.EvaluateAsync<string>("() => window.__blexDevTools.name"));
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__blexDevTools.features.jump === true && window.__blexDevTools.features.dispatch === false"));

        await IncrementAsync(2);

        // Every action arrives with the global state that followed it, keyed by store name.
        await Page.WaitForFunctionAsync("""
            () => window.__blexDevTools.sends.some(s => s.type === 'counter/Increment' && s.state.counter && s.state.counter.Count === 2)
            """, null, new() { Timeout = 30_000 });

        // Jump back to the state the bridge was initialized with, the way the extension's slider does.
        await Page.EvaluateAsync("""
            () => {
                const devtools = window.__blexDevTools;
                const initial = devtools.inits[0];
                devtools.listener({ type: 'DISPATCH', payload: { type: 'JUMP_TO_STATE' }, state: JSON.stringify(initial) });
            }
            """);

        await ExpectCountAsync(0);
    }

    /// <summary>Re-reads a value until it satisfies <paramref name="done"/>, or hands back the last one read.</summary>
    protected static async Task<T> PollAsync<T>(Func<Task<T>> read, Func<T, bool> done, int timeoutMilliseconds = 20_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);

        while (true)
        {
            var value = await read();
            if (done(value) || DateTime.UtcNow > deadline) return value;

            await Task.Delay(100);
        }
    }
}
