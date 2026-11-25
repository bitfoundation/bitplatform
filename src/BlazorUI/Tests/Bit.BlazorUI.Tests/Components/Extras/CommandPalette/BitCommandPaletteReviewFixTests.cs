using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// Regressions for what an independent review of the changes found: DI sources owned by whichever palette started first,
/// a timeout only a well-behaved source obeyed, timeouts swallowed as cancellations, a handler's answer applied to a page
/// it was not meant for, a stale search keeping a new page loading, and an inline palette speaking while unused.
/// </summary>
[TestClass]
public class BitCommandPaletteReviewFixTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;
    private BitCommandPaletteOptions _options = default!;
    private LogSink _log = default!;

    [TestInitialize]
    public void Initialize()
    {
        _log = LogSink.AddTo(Services);
        Services.AddBitCommandPaletteServices(o => o.DebounceMs = 0);
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Services.AddBitCommandPaletteSource<DirectorySource>();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBitCommandPaletteService>();
        _options = Services.GetRequiredService<BitCommandPaletteOptions>();
    }

    private static Task Press(IRenderedComponent<BitCommandPalette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance._OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public async Task A_source_registered_in_DI_is_searched()
    {
        // AddBitCommandPaletteSource used to crash every palette on its first render: registering the source rebuilt the list
        // before the palette had a page.
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.OpenAsync("ada"));

        cut.WaitForAssertion(() => Assert.Contains("Ada Lovelace", cut.Find("[role=listbox]").TextContent));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task The_DI_sources_stay_when_the_palette_that_registered_them_goes()
    {
        var first = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));
        var second = Context.Render<BitCommandPalette>();
        Assert.ContainsSingle(_service.Sources.OfType<DirectorySource>());

        await first.InvokeAsync(() => first.Instance.DisposeAsync().AsTask());

        Assert.ContainsSingle(_service.Sources.OfType<DirectorySource>());
        Assert.IsNotNull(second.Instance);
    }

    [TestMethod]
    public async Task A_source_that_ignores_its_token_still_times_out()
    {
        _options.SourceTimeoutMs = 50;
        _service.RegisterSource(new BitCommandPaletteDelegateSource("Stubborn", (_, _) => new TaskCompletionSource<IReadOnlyList<BitCommandPaletteCommand>>().Task));
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.OpenAsync("anything"));

        cut.WaitForAssertion(() => Assert.Contains("Couldn’t search Stubborn", cut.Find("[data-error=true]").TextContent), TimeSpan.FromSeconds(5));
        Assert.IsNull(cut.Find("[role=listbox]").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task A_handler_that_times_out_has_failed_while_one_that_stops_on_purpose_has_not()
    {
        _service.Register(
            BitCommandPaletteCmd.New("Fetch").Handler(_ => Task.FromException<BitCommandPaletteCommandResult>(new TaskCanceledException("timed out", new TimeoutException()))),
            BitCommandPaletteCmd.New("Stop").Handler(_ => Task.FromException<BitCommandPaletteCommandResult>(new OperationCanceledException())));
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.OpenAsync("stop"));
        await Press(cut, "Enter");
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=toast]"));
        Assert.IsEmpty(_log.Messages(LogLevel.Error));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("fetch"));
        await Press(cut, "Enter");
        Assert.AreEqual("error", cut.Find("[data-bit-cpl=toast]").GetAttribute("data-kind"));
        Assert.ContainsSingle(_log.Entries.Where(e => e.Level == LogLevel.Error && e.Exception is TaskCanceledException));
    }

    [TestMethod]
    public async Task An_answer_about_pages_is_dropped_when_its_page_was_left_while_it_ran()
    {
        var submitted = new TaskCompletionSource<BitCommandPaletteCommandResult>();
        _service.Register(BitCommandPaletteCmd.New("Project").Children(
            BitCommandPaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New name", _ => submitted.Task)))));
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "project"));
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-bit-cpl=prompt]")));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Orion"));

        var running = Press(cut, "Enter");
        await Press(cut, "Escape");
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Project"], cut.FindAll("[data-bit-cpl=crumb]").Select(c => c.TextContent.Trim())));

        // Meant for the prompt, which is gone: going back from the page the reader is on now would be one page too many.
        submitted.SetResult(BitCommandPaletteCommandResult.Back);
        await running;

        AssertSequence.AreEqual(["Project"], cut.FindAll("[data-bit-cpl=crumb]").Select(c => c.TextContent.Trim()));
    }

    [TestMethod]
    public async Task A_page_without_sources_does_not_wait_for_the_previous_pages_search()
    {
        var never = new TaskCompletionSource<IReadOnlyList<BitCommandPaletteCommand>>();
        _service.RegisterSource(new BitCommandPaletteDelegateSource("Slow", (_, _) => never.Task));
        _service.Register(BitCommandPaletteCmd.Page("Abc page", BitCommandPaletteCmd.New("Child")));
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync("abc"));
        cut.WaitForAssertion(() => Assert.AreEqual("true", cut.Find("[role=listbox]").GetAttribute("aria-busy")));

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(cut.FindAll("[data-bit-cpl=crumb]"));
            Assert.IsNull(cut.Find("[role=listbox]").GetAttribute("aria-busy"));
            Assert.IsEmpty(cut.FindAll("[data-bit-cpl=spinner]"));
        });
    }

    [TestMethod]
    public async Task An_inline_palette_announces_only_while_it_has_the_focus()
    {
        _service.Register(BitCommandPaletteCmd.New("Alpha"), BitCommandPaletteCmd.New("Beta"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true).Add(x => x.IncludeRegisteredCommands, true));
        var live = () => cut.Find("[aria-live=polite]").TextContent.Trim();

        Assert.AreEqual(string.Empty, live());

        await cut.InvokeAsync(() => cut.Instance._OnInlineFocusChanged(true));
        Assert.AreEqual("2 results available", live());

        await cut.InvokeAsync(() => cut.Instance._OnInlineFocusChanged(false));
        Assert.AreEqual(string.Empty, live());
    }

    private sealed class DirectorySource : IBitCommandPaletteSource
    {
        public string Name => "Directory";

        public Task<IReadOnlyList<BitCommandPaletteCommand>> SearchAsync(string query, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<BitCommandPaletteCommand>>(query == "ada" ? [BitCommandPaletteCmd.New("Ada Lovelace").Build()] : []);
    }
}

[TestClass]
public class HistoryLoadCancellationTests
{
    [TestMethod]
    public async Task A_caller_that_gives_up_stops_its_own_wait_and_not_the_shared_load()
    {
        var history = new BitCommandPaletteHistory(new SlowStore(), new BitCommandPaletteOptions());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => history.RecordAsync("a", cancelled.Token));

        // The load it started goes on for everyone else, and the history works afterwards.
        await history.EnsureLoadedAsync();
        await history.RecordAsync("b");
        Assert.IsTrue(history.IsLoaded);
        Assert.AreEqual(1, history.Get("b")!.Count);
    }

    private sealed class SlowStore : IBitCommandPaletteHistoryStore
    {
        public async ValueTask<IDictionary<string, BitCommandPaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(30, cancellationToken);
            return new Dictionary<string, BitCommandPaletteCommandUsage>();
        }

        public ValueTask SaveAsync(IReadOnlyDictionary<string, BitCommandPaletteCommandUsage> usage, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
