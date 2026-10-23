using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Balette.Tests;

/// <summary>
/// Regressions for what an independent review of the changes found: DI sources owned by whichever palette started first,
/// a timeout only a well-behaved source obeyed, timeouts swallowed as cancellations, a handler's answer applied to a page
/// it was not meant for, a stale search keeping a new page loading, and an inline palette speaking while unused.
/// </summary>
[TestClass]
public class BaletteReviewFixTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BaletteOptions _options;
    private readonly LogSink _log;

    public BaletteReviewFixTests()
    {
        _log = LogSink.AddTo(Services);
        Services.AddBitBaletteServices(o => o.DebounceMs = 0);
        Services.AddBitBaletteInMemoryHistoryStore();
        Services.AddBitBaletteSource<DirectorySource>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js").SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBaletteService>();
        _options = Services.GetRequiredService<BaletteOptions>();
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public async Task A_source_registered_in_DI_is_searched()
    {
        // AddBitBaletteSource used to crash every palette on its first render: registering the source rebuilt the list
        // before the palette had a page.
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync("ada"));

        cut.WaitForAssertion(() => Assert.Contains("Ada Lovelace", cut.Find("[role=listbox]").TextContent));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task The_DI_sources_stay_when_the_palette_that_registered_them_goes()
    {
        var first = Render<Balette>(p => p.Add(x => x.Inline, true));
        var second = Render<Balette>();
        Assert.ContainsSingle(_service.Sources.OfType<DirectorySource>());

        await first.InvokeAsync(() => first.Instance.DisposeAsync().AsTask());

        Assert.ContainsSingle(_service.Sources.OfType<DirectorySource>());
        Assert.IsNotNull(second.Instance);
    }

    [TestMethod]
    public async Task A_source_that_ignores_its_token_still_times_out()
    {
        _options.SourceTimeoutMs = 50;
        _service.RegisterSource(new BaletteDelegateSource("Stubborn", (_, _) => new TaskCompletionSource<IReadOnlyList<BaletteCommand>>().Task));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync("anything"));

        cut.WaitForAssertion(() => Assert.Contains("Couldn’t search Stubborn", cut.Find("[data-error=true]").TextContent), TimeSpan.FromSeconds(5));
        Assert.IsNull(cut.Find("[role=listbox]").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task A_handler_that_times_out_has_failed_while_one_that_stops_on_purpose_has_not()
    {
        _service.Register(
            BaletteCmd.New("Fetch").Handler(_ => Task.FromException<BaletteCommandResult>(new TaskCanceledException("timed out", new TimeoutException()))),
            BaletteCmd.New("Stop").Handler(_ => Task.FromException<BaletteCommandResult>(new OperationCanceledException())));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync("stop"));
        await Press(cut, "Enter");
        Assert.IsEmpty(cut.FindAll("[data-balette=toast]"));
        Assert.IsEmpty(_log.Messages(LogLevel.Error));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("fetch"));
        await Press(cut, "Enter");
        Assert.AreEqual("error", cut.Find("[data-balette=toast]").GetAttribute("data-kind"));
        Assert.ContainsSingle(_log.Entries.Where(e => e.Level == LogLevel.Error && e.Exception is TaskCanceledException));
    }

    [TestMethod]
    public async Task An_answer_about_pages_is_dropped_when_its_page_was_left_while_it_ran()
    {
        var submitted = new TaskCompletionSource<BaletteCommandResult>();
        _service.Register(BaletteCmd.New("Project").Children(
            BaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt("New name", _ => submitted.Task)))));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "project"));
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-balette=prompt]")));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Orion"));

        var running = Press(cut, "Enter");
        await Press(cut, "Escape");
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Project"], cut.FindAll("[data-balette=crumb]").Select(c => c.TextContent.Trim())));

        // Meant for the prompt, which is gone: going back from the page the reader is on now would be one page too many.
        submitted.SetResult(BaletteCommandResult.Back);
        await running;

        AssertSequence.AreEqual(["Project"], cut.FindAll("[data-balette=crumb]").Select(c => c.TextContent.Trim()));
    }

    [TestMethod]
    public async Task A_page_without_sources_does_not_wait_for_the_previous_pages_search()
    {
        var never = new TaskCompletionSource<IReadOnlyList<BaletteCommand>>();
        _service.RegisterSource(new BaletteDelegateSource("Slow", (_, _) => never.Task));
        _service.Register(BaletteCmd.Page("Abc page", BaletteCmd.New("Child")));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("abc"));
        cut.WaitForAssertion(() => Assert.AreEqual("true", cut.Find("[role=listbox]").GetAttribute("aria-busy")));

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(cut.FindAll("[data-balette=crumb]"));
            Assert.IsNull(cut.Find("[role=listbox]").GetAttribute("aria-busy"));
            Assert.IsEmpty(cut.FindAll("[data-balette=spinner]"));
        });
    }

    [TestMethod]
    public async Task An_inline_palette_announces_only_while_it_has_the_focus()
    {
        _service.Register(BaletteCmd.New("Alpha"), BaletteCmd.New("Beta"));
        var cut = Render<Balette>(p => p.Add(x => x.Inline, true).Add(x => x.IncludeRegisteredCommands, true));
        var live = () => cut.Find("[aria-live=polite]").TextContent.Trim();

        Assert.AreEqual(string.Empty, live());

        await cut.InvokeAsync(() => cut.Instance.OnInlineFocusChanged(true));
        Assert.AreEqual("2 results available", live());

        await cut.InvokeAsync(() => cut.Instance.OnInlineFocusChanged(false));
        Assert.AreEqual(string.Empty, live());
    }

    private sealed class DirectorySource : IBaletteSource
    {
        public string Name => "Directory";

        public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<BaletteCommand>>(query == "ada" ? [BaletteCmd.New("Ada Lovelace").Build()] : []);
    }
}

[TestClass]
public class HistoryLoadCancellationTests
{
    [TestMethod]
    public async Task A_caller_that_gives_up_stops_its_own_wait_and_not_the_shared_load()
    {
        var history = new BaletteHistory(new SlowStore(), new BaletteOptions());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => history.RecordAsync("a", cancelled.Token));

        // The load it started goes on for everyone else, and the history works afterwards.
        await history.EnsureLoadedAsync();
        await history.RecordAsync("b");
        Assert.IsTrue(history.IsLoaded);
        Assert.AreEqual(1, history.Get("b")!.Count);
    }

    private sealed class SlowStore : IBaletteHistoryStore
    {
        public async ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(30, cancellationToken);
            return new Dictionary<string, BaletteCommandUsage>();
        }

        public ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
