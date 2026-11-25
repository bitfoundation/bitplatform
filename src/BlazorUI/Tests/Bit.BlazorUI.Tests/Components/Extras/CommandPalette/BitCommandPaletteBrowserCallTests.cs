using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// The calls that cross between the palette and its browser layer at the edges of its life and of the application's code:
/// a palette removed while its first calls are on their way, an exception thrown by the application's code that a key or a
/// shortcut reached, and the name its trigger is announced by.
/// </summary>
[TestClass]
public class BitCommandPaletteBrowserCallTests : BunitTestContext
{
    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitBlazorUIExtrasServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // Resolved when first used: the tests that replace the JS runtime register it before anything is resolved.
    private IBitCommandPaletteService Service => Services.GetRequiredService<IBitCommandPaletteService>();

    // ---- Removed while starting ---------------------------------------------------------------------------

    [TestMethod]
    public async Task A_palette_removed_while_setup_is_on_its_way_releases_the_browser_layer_setup_brings_back()
    {
        var js = new PendingJSRuntime();
        Services.AddSingleton<IJSRuntime>(js);
        Context.Render<BitCommandPalette>();
        await WaitUntilAsync(() => js.Calls.Contains(BitCommandPaletteTestJs.Setup));

        // The page is left while setup crosses to the browser, and its listeners arrive after DisposeAsync ran.
        await Context.DisposeComponentsAsync();
        js.Setup.SetResult(js.Handle);

        // They are removed, so they neither outlive the palette nor call into it, and nothing else is asked of them.
        await WaitUntilAsync(() => js.Handle.Calls.Contains("DisposeAsync"));
        AssertSequence.AreEqual(["dispose", "DisposeAsync"], js.Handle.Calls);
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task A_palette_disposed_twice_releases_the_browser_layer_once()
    {
        var handle = Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        var cut = Context.Render<BitCommandPalette>();

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();

        Assert.ContainsSingle(handle.Invocations.Where(i => i.Identifier == "dispose"));
    }

    // ---- The application's exceptions -----------------------------------------------------------------------

    [TestMethod]
    public async Task A_listener_that_throws_after_a_command_run_from_a_key_reaches_the_renderer_and_the_command_still_closes_the_palette()
    {
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        var ran = false;
        Service.Register(BitCommandPaletteCmd.Action("Deploy", () => ran = true));
        Service.CommandExecuted += (_, _) => throw new InvalidOperationException("listener");
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => Service.OpenAsync("deploy"));

        // The browser layer's call does not fail: the exception goes where one thrown from a click goes.
        await cut.InvokeAsync(() => cut.Instance._OnKeyDown("Enter", false, false, false, false));

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
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();
        var ran = false;
        Service.Register(BitCommandPaletteCmd.Action("Deploy", () => ran = true));
        Service.CommandExecuted += (_, _) => stopping.Token.ThrowIfCancellationRequested();
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => Service.OpenAsync("deploy"));

        // The cancellation is not dispatched to the renderer, as one ending a click is not.
        await cut.InvokeAsync(() => cut.Instance._OnKeyDown("Enter", false, false, false, false));

        Assert.IsTrue(ran);
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
        Assert.IsFalse(Service.IsOpen);
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task A_listener_that_times_out_after_a_command_run_from_a_key_reaches_the_renderer()
    {
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        Service.Register(BitCommandPaletteCmd.Action("Deploy", () => { }));
        // How HttpClient reports a timeout: a cancellation around a TimeoutException, which is a failure, not a stop.
        Service.CommandExecuted += (_, _) => throw new TaskCanceledException("timed out", new TimeoutException());
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => Service.OpenAsync("deploy"));

        await cut.InvokeAsync(() => cut.Instance._OnKeyDown("Enter", false, false, false, false));

        var exception = await Renderer.UnhandledException;
        Assert.IsInstanceOfType<TaskCanceledException>(exception);
        Assert.IsInstanceOfType<TimeoutException>(exception.InnerException);
        Assert.IsFalse(Service.IsOpen);
    }

    [TestMethod]
    public async Task A_listener_that_times_out_after_a_global_shortcut_reaches_the_renderer()
    {
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        Service.Register(BitCommandPaletteCmd.Action("Save", () => { }).Shortcut("Mod+S"));
        Service.CommandExecuted += (_, _) => throw new TaskCanceledException("timed out", new TimeoutException());
        var cut = Context.Render<BitCommandPalette>();

        // The shortcut's work runs on the dispatcher, which would hand back a bare cancelled task: the timeout survives it.
        await cut.InvokeAsync(() => cut.Instance._OnGlobalShortcut("mod+s"));

        Assert.IsInstanceOfType<TimeoutException>((await Renderer.UnhandledException).InnerException);
    }

    [TestMethod]
    public async Task A_listener_and_a_close_callback_that_both_throw_both_reach_the_renderer()
    {
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        Service.Register(BitCommandPaletteCmd.Action("Deploy", () => { }));
        Service.CommandExecuted += (_, _) => throw new InvalidOperationException("listener");
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.OnClosed, () => throw new InvalidOperationException("closed")));
        await cut.InvokeAsync(() => Service.OpenAsync("deploy"));

        // The command closes the palette, whose OnClosed throws as well: neither failure hides the other.
        await cut.InvokeAsync(() => cut.Instance._OnKeyDown("Enter", false, false, false, false));

        var exception = await Renderer.UnhandledException;
        Assert.IsInstanceOfType<AggregateException>(exception);
        AssertSequence.AreEqual(["listener", "closed"], ((AggregateException)exception).InnerExceptions.Select(e => e.Message).ToList());
        Assert.IsFalse(Service.IsOpen);
    }

    [TestMethod]
    public async Task A_listener_that_throws_after_a_global_shortcut_reaches_the_renderer()
    {
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        var runs = 0;
        Service.Register(BitCommandPaletteCmd.Action("Save", () => runs++).Shortcut("Mod+S"));
        Service.CommandExecuted += (_, _) => throw new InvalidOperationException("listener");
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => cut.Instance._OnGlobalShortcut("mod+s"));

        Assert.AreEqual(1, runs);
        Assert.AreEqual("listener", (await Renderer.UnhandledException).Message);
    }

    [TestMethod]
    public async Task A_callback_that_throws_when_a_key_closes_the_palette_reaches_the_renderer()
    {
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.OnClosed, () => throw new InvalidOperationException("closed")));
        await cut.InvokeAsync(() => Service.OpenAsync());

        await cut.InvokeAsync(() => cut.Instance._OnKeyDown("Escape", false, false, false, false));

        Assert.AreEqual("closed", (await Renderer.UnhandledException).Message);
        Assert.IsFalse(Service.IsOpen);
    }

    // ---- The trigger's name --------------------------------------------------------------------------------

    [TestMethod]
    public void The_trigger_is_named_by_its_visible_label_first()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p.Add(x => x.Label, "Search or run a command…"));

        var button = trigger.Find("button");
        Assert.AreEqual("Search or run a command…", trigger.Find(".bit-cpt-lbl").TextContent);
        Assert.AreEqual("Search or run a command…, Open command palette", button.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void A_trigger_label_that_already_says_what_it_does_is_its_whole_name()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p.Add(x => x.Label, "Open command palette"));

        Assert.AreEqual("Open command palette", trigger.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void A_trigger_without_a_label_is_named_by_the_string_alone()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p.Add(x => x.Label, " "));

        // Nothing shown to start the name with, so it does not start with the separator.
        Assert.AreEqual("Open command palette", trigger.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void An_open_palette_string_that_holds_the_label_is_the_whole_name()
    {
        // An app that wrote the string as a whole name before the label started it.
        Context.Render<BitCommandPalette>(p => p.Add(x => x.Strings, new BitCommandPaletteStrings { OpenPalette = "Search commands" }));
        var trigger = Context.Render<BitCommandPaletteTrigger>();

        Assert.AreEqual("Search commands", trigger.Find("button").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void A_trigger_with_its_own_icon_and_an_empty_label_is_named_by_the_string()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p
            .Add(x => x.Label, "")
            .AddChildContent("<span aria-hidden=\"true\">⌘</span>"));

        var button = trigger.Find("button");
        Assert.AreEqual("Open command palette", button.GetAttribute("aria-label"));
        Assert.IsNull(button.QuerySelector(".bit-cpt-vhd"));
    }

    [TestMethod]
    public void A_trigger_with_its_own_text_is_named_by_that_text_first()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p.AddChildContent("<kbd>&gt;</kbd> Commands mode"));

        // The name the browser computes from the content starts with the text it shows, and goes on to say what it does,
        // joined as the default trigger's name is.
        var button = trigger.Find("button");
        Assert.IsFalse(button.HasAttribute("aria-label"));
        Assert.AreEqual("> Commands mode, Open command palette", NormalizeSpace(button.TextContent));
    }

    [TestMethod]
    public void A_trigger_with_its_own_content_and_its_label_is_named_as_the_default_one()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p
            .Add(x => x.Label, "Commands mode")
            .AddChildContent("<kbd>&gt;</kbd> Commands mode"));

        var button = trigger.Find("button");
        Assert.AreEqual("Commands mode, Open command palette", button.GetAttribute("aria-label"));
        Assert.IsNull(button.QuerySelector(".bit-cpt-vhd"));
    }

    [TestMethod]
    public void An_AriaLabel_the_app_gives_a_trigger_with_its_own_content_is_its_name()
    {
        var trigger = Context.Render<BitCommandPaletteTrigger>(p => p
            .Add(x => x.AriaLabel, "Commands mode")
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

    /// <summary>A JS runtime whose setup of the browser layer answers only when the test says so.</summary>
    private sealed class PendingJSRuntime : IJSRuntime
    {
        public TaskCompletionSource<IJSObjectReference> Setup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingReference Handle { get; } = new();

        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            lock (Calls)
            {
                Calls.Add(identifier);
            }

            return identifier == BitCommandPaletteTestJs.Setup ? Answer<TValue>(Setup.Task) : ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>A browser-side object that records what .NET asks of it, and answers each call with a default value.</summary>
    private sealed class RecordingReference : IJSObjectReference
    {
        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
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
