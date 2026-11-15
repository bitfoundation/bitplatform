using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Balette.Tests;

/// <summary>
/// The calls that cross between the palette and its browser layer at the edges of its life and of the application's code:
/// a palette removed while its first calls are on their way, an exception thrown by the application's code that a key or a
/// shortcut reached, and the name its trigger is announced by.
/// </summary>
[TestClass]
public class BaletteBrowserCallTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Balette/bit-balette.js";

    public BaletteBrowserCallTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // Resolved when first used: the tests that replace the JS runtime register it before anything is resolved.
    private IBaletteService Service => Services.GetRequiredService<IBaletteService>();

    // ---- Removed while starting ---------------------------------------------------------------------------

    [TestMethod]
    public async Task A_palette_removed_while_its_module_downloads_never_starts_the_browser_layer()
    {
        var js = new PendingJSRuntime();
        Services.AddSingleton<IJSRuntime>(js);
        Render<Balette>();

        // The page is left while the module is on its way: DisposeAsync finds nothing to release yet.
        await DisposeComponentsAsync();
        js.Import.SetResult(js.Module);

        await WaitUntilAsync(() => js.Module.Calls.Contains("DisposeAsync"));
        Assert.DoesNotContain("init", js.Module.Calls);
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task A_palette_removed_while_init_is_on_its_way_releases_the_browser_layer_init_brings_back()
    {
        var js = new PendingJSRuntime();
        js.Import.SetResult(js.Module);
        Services.AddSingleton<IJSRuntime>(js);
        Render<Balette>();
        await WaitUntilAsync(() => js.Module.Calls.Contains("init"));

        // The page is left while init crosses to the browser, and its listeners arrive after DisposeAsync ran.
        await DisposeComponentsAsync();
        js.Module.Init.SetResult(js.Handle);

        // They are removed, so they neither outlive the palette nor call into it, and nothing else is asked of them.
        await WaitUntilAsync(() => js.Handle.Calls.Contains("DisposeAsync"));
        AssertSequence.AreEqual(["dispose", "DisposeAsync"], js.Handle.Calls);
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task A_palette_disposed_twice_releases_the_browser_layer_once()
    {
        var handle = JSInterop.SetupModule(ModulePath).SetupModule("init", _ => true);
        var cut = Render<Balette>();

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();

        Assert.ContainsSingle(handle.Invocations.Where(i => i.Identifier == "dispose"));
    }

    // ---- The application's exceptions -----------------------------------------------------------------------

    [TestMethod]
    public async Task A_listener_that_throws_after_a_command_run_from_a_key_reaches_the_renderer_and_the_command_still_closes_the_palette()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("init", _ => true);
        var ran = false;
        Service.Register(BaletteCmd.Action("Deploy", () => ran = true));
        Service.CommandExecuted += (_, _) => throw new InvalidOperationException("listener");
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => Service.OpenAsync("deploy"));

        // The browser layer's call does not fail: the exception goes where one thrown from a click goes.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        Assert.IsTrue(ran);
        var exception = await Renderer.UnhandledException;
        Assert.IsInstanceOfType<InvalidOperationException>(exception);
        Assert.AreEqual("listener", exception.Message);
        Assert.IsFalse(Service.IsOpen);
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task A_listener_stopped_by_the_application_s_cancelled_token_after_a_command_run_from_a_key_is_no_failure()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("init", _ => true);
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();
        var ran = false;
        Service.Register(BaletteCmd.Action("Deploy", () => ran = true));
        Service.CommandExecuted += (_, _) => stopping.Token.ThrowIfCancellationRequested();
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => Service.OpenAsync("deploy"));

        // The cancellation is not dispatched to the renderer, as one ending a click is not.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        Assert.IsTrue(ran);
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
        Assert.IsFalse(Service.IsOpen);
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task A_listener_that_throws_after_a_global_shortcut_reaches_the_renderer()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("init", _ => true);
        var runs = 0;
        Service.Register(BaletteCmd.Action("Save", () => runs++).Shortcut("Mod+S"));
        Service.CommandExecuted += (_, _) => throw new InvalidOperationException("listener");
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+s"));

        Assert.AreEqual(1, runs);
        Assert.AreEqual("listener", (await Renderer.UnhandledException).Message);
    }

    [TestMethod]
    public async Task A_callback_that_throws_when_a_key_closes_the_palette_reaches_the_renderer()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("init", _ => true);
        var cut = Render<Balette>(p => p.Add(x => x.OnClosed, () => throw new InvalidOperationException("closed")));
        await cut.InvokeAsync(() => Service.OpenAsync());

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Escape", false, false, false, false));

        Assert.AreEqual("closed", (await Renderer.UnhandledException).Message);
        Assert.IsFalse(Service.IsOpen);
    }

    // ---- The trigger's name --------------------------------------------------------------------------------

    [TestMethod]
    public void The_trigger_is_named_by_its_visible_label_first()
    {
        var trigger = Render<BaletteTrigger>(p => p.Add(x => x.Label, "Search or run a command…"));

        var button = trigger.Find("button");
        Assert.AreEqual("Search or run a command…", trigger.Find(".bit-balette-trigger-label").TextContent);
        Assert.AreEqual("Search or run a command…, Open command palette", button.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void A_trigger_label_that_already_says_what_it_does_is_its_whole_name()
    {
        var trigger = Render<BaletteTrigger>(p => p.Add(x => x.Label, "Open command palette"));

        Assert.AreEqual("Open command palette", trigger.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void A_trigger_with_its_own_icon_is_named_by_the_string()
    {
        var trigger = Render<BaletteTrigger>(p => p.AddChildContent("<span aria-hidden=\"true\">⌘</span>"));

        // No aria-label: the browser names the button from its content, of which the icon is hidden and the string is not.
        var button = trigger.Find("button");
        Assert.IsFalse(button.HasAttribute("aria-label"));
        Assert.AreEqual("Open command palette", button.QuerySelector(".bit-balette-sr-only")!.TextContent.Trim());
    }

    [TestMethod]
    public void A_trigger_with_its_own_text_is_named_by_that_text_first()
    {
        var trigger = Render<BaletteTrigger>(p => p.AddChildContent("<kbd>&gt;</kbd> Commands mode"));

        // The name the browser computes from the content starts with the text it shows, and goes on to say what it does.
        var button = trigger.Find("button");
        Assert.IsFalse(button.HasAttribute("aria-label"));
        Assert.AreEqual("> Commands mode Open command palette", NormalizeSpace(button.TextContent));
    }

    [TestMethod]
    public void An_aria_label_the_app_gives_a_trigger_with_its_own_content_is_its_name()
    {
        var trigger = Render<BaletteTrigger>(p => p
            .AddUnmatched("aria-label", "Commands mode")
            .AddChildContent("<kbd>&gt;</kbd> Commands mode"));

        Assert.AreEqual("Commands mode", trigger.Find("button").GetAttribute("aria-label"));
    }

    private static string NormalizeSpace(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(condition());
    }

    /// <summary>A JS runtime whose module import, and the module's init, answer only when the test says so.</summary>
    private sealed class PendingJSRuntime : IJSRuntime
    {
        public TaskCompletionSource<IJSObjectReference> Import { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingModule Module { get; } = new();

        public RecordingReference Handle { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => identifier == "import" ? Answer<TValue>(Import.Task) : ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class PendingModule : RecordingReference
    {
        public TaskCompletionSource<IJSObjectReference> Init { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(identifier);
            return identifier == "init" ? Answer<TValue>(Init.Task) : ValueTask.FromResult(default(TValue)!);
        }
    }

    /// <summary>A browser-side object that records what .NET asks of it, and answers each call with a default value.</summary>
    private class RecordingReference : IJSObjectReference
    {
        public List<string> Calls { get; } = [];

        public virtual ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Calls.Add("DisposeAsync");
            return ValueTask.CompletedTask;
        }
    }

    private static async ValueTask<TValue> Answer<TValue>(Task<IJSObjectReference> answer) => (TValue)(object)await answer;
}
