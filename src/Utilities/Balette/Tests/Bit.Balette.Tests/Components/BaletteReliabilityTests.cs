using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Balette.Tests;

/// <summary>
/// What happens when something goes wrong: a handler or a page loader that throws, a page left while it loads, a link
/// that would run script, two commands claiming one shortcut or one id, and state the predicates read that changed
/// behind the palette's back. None of it may escape into rendering, and all of it must be reported.
/// </summary>
[TestClass]
public class BaletteReliabilityTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BunitJSModuleInterop _jsHandle;
    private readonly LogSink _log;

    public BaletteReliabilityTests()
    {
        _log = LogSink.AddTo(Services);
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        var handle = _jsHandle = module.SetupModule("init", _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
        handle.SetupVoid("open", _ => true).SetVoidResult();
        handle.SetupVoid("close").SetVoidResult();
        handle.SetupVoid("focusInput", _ => true).SetVoidResult();
        handle.SetupVoid("scrollIntoView", _ => true).SetVoidResult();
        handle.SetupVoid("setCommandShortcuts", _ => true).SetVoidResult();
        handle.SetupVoid("dispose").SetVoidResult();

        _service = Services.GetRequiredService<IBaletteService>();
    }

    private Task Press(IRenderedComponent<Balette> cut, string key, bool ctrl = false)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, ctrl, false, false, false));

    private IEnumerable<string> LastSyncedShortcuts()
        => (IEnumerable<string>)_jsHandle.Invocations.Last(i => i.Identifier == "setCommandShortcuts").Arguments[0]!;

    private async Task<IRenderedComponent<Balette>> OpenAsync(string? query = null)
    {
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync(query));
        return cut;
    }

    // ---- Handler failures ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_handler_that_throws_is_logged_and_shown_without_its_exception_text()
    {
        _service.Register(BaletteCmd.Action("Explode", () => throw new InvalidOperationException("connection string: password=hunter2")));
        BaletteCommandErrorEventArgs? raised = null;
        _service.CommandFailed += (_, e) => raised = e;
        var cut = await OpenAsync();

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            var toast = cut.Find("[data-balette=toast]");
            Assert.AreEqual("error", toast.GetAttribute("data-kind"));
            Assert.AreEqual("Couldn’t run “Explode”.", toast.TextContent.Trim());
            // Announced assertively, by a region that was there before the message.
            Assert.AreEqual("Couldn’t run “Explode”.", cut.Find("[data-balette=alert]").TextContent.Trim());
            Assert.AreEqual(string.Empty, cut.Find("[data-balette=status]").TextContent.Trim());
        });
        Assert.IsNotNull(raised);
        Assert.IsInstanceOfType<InvalidOperationException>(raised.Exception);
        Assert.ContainsSingle(_log.Entries.Where(e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException));
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task A_subscriber_that_handles_a_failure_chooses_the_message_and_nothing_is_logged()
    {
        _service.Register(BaletteCmd.Action("Rename", () => throw new InvalidOperationException("That name is taken.")));
        _service.CommandFailed += (_, e) =>
        {
            e.Handled = true;
            e.Message = e.Exception.Message;
        };
        var cut = await OpenAsync();

        await Press(cut, "Enter");

        cut.WaitForAssertion(() => Assert.AreEqual("That name is taken.", cut.Find("[data-balette=toast]").TextContent.Trim()));
        Assert.IsEmpty(_log.Messages(LogLevel.Error));
    }

    [TestMethod]
    public async Task An_empty_message_shows_nothing()
    {
        _service.Register(BaletteCmd.Action("Quiet", () => throw new InvalidOperationException("x")));
        _service.CommandFailed += (_, e) => e.Message = string.Empty;
        var cut = await OpenAsync();

        await Press(cut, "Enter");

        Assert.IsEmpty(cut.FindAll("[data-balette=toast]"));
        Assert.ContainsSingle(_log.Messages(LogLevel.Error));
    }

    [TestMethod]
    public async Task Clicking_a_row_whose_handler_throws_does_not_throw_into_the_renderer()
    {
        _service.Register(BaletteCmd.Action("Explode", () => throw new InvalidOperationException("boom")));
        var cut = await OpenAsync();

        cut.Find("[data-balette=item]").Click();

        cut.WaitForAssertion(() => Assert.AreEqual("error", cut.Find("[data-balette=toast]").GetAttribute("data-kind")));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task ExecuteAsync_rethrows_an_unhandled_failure_to_its_caller_and_not_a_handled_one()
    {
        var handle = false;
        _service.Register(BaletteCmd.Action("Explode", () => throw new InvalidOperationException("boom")).Id("explode"));
        _service.CommandFailed += (_, e) => e.Handled = handle;
        var cut = Render<Balette>();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => cut.InvokeAsync(() => _service.ExecuteAsync("explode")));

        handle = true;
        await cut.InvokeAsync(() => _service.ExecuteAsync("explode"));
    }

    [TestMethod]
    public async Task A_failure_while_the_palette_is_closed_is_logged_but_not_kept_for_the_next_open()
    {
        _service.Register(BaletteCmd.Action("Explode", () => throw new InvalidOperationException("boom")).Shortcut("F9"));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("f9"));
        await cut.InvokeAsync(() => _service.OpenAsync());

        Assert.IsEmpty(cut.FindAll("[data-balette=toast]"));
        Assert.ContainsSingle(_log.Messages(LogLevel.Error));
    }

    // ---- Lazily loaded pages -----------------------------------------------------------------------------

    [TestMethod]
    public async Task Leaving_a_loading_page_cancels_its_load_and_an_abandoned_load_cannot_end_a_newer_one()
    {
        var calls = new List<(TaskCompletionSource<IReadOnlyList<BaletteCommand>> Result, CancellationToken Token)>();
        _service.Register(BaletteCmd.New("People").Children((_, ct) =>
        {
            var result = new TaskCompletionSource<IReadOnlyList<BaletteCommand>>();
            calls.Add((result, ct));
            return result.Task;
        }));
        var cut = await OpenAsync();

        var firstEnter = Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-balette=loading]")));
        await Press(cut, "Escape");
        Assert.IsTrue(calls[0].Token.IsCancellationRequested);

        var secondEnter = Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.HasCount(2, calls));
        Assert.IsFalse(calls[1].Token.IsCancellationRequested);

        // The abandoned request answers first: it neither fills the page nor ends its loading state.
        calls[0].Result.SetResult([BaletteCmd.New("Stale").Build()]);
        await firstEnter;
        Assert.IsNotNull(cut.Find("[data-balette=loading]"));
        Assert.IsEmpty(cut.FindAll("[data-balette=item]"));

        calls[1].Result.SetResult([BaletteCmd.New("Ada").Build()]);
        await secondEnter;
        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll("[data-balette=item]");
            Assert.ContainsSingle(items);
            Assert.Contains("Ada", items[0].TextContent);
        });
    }

    [TestMethod]
    public async Task Closing_cancels_a_page_load()
    {
        var token = CancellationToken.None;
        _service.Register(BaletteCmd.New("People").Children((_, ct) =>
        {
            token = ct;
            return new TaskCompletionSource<IReadOnlyList<BaletteCommand>>().Task;
        }));
        var cut = await OpenAsync();

        _ = Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.IsTrue(token.CanBeCanceled));
        await cut.InvokeAsync(() => _service.CloseAsync());

        Assert.IsTrue(token.IsCancellationRequested);
    }

    [TestMethod]
    public async Task A_page_that_fails_to_load_shows_an_error_and_loads_again_on_retry()
    {
        var attempts = 0;
        _service.Register(BaletteCmd.New("People").Children((_, _) =>
        {
            attempts++;
            return attempts == 1
                ? Task.FromException<IReadOnlyList<BaletteCommand>>(new HttpRequestException("503"))
                : Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Ada").Build()]);
        }));
        var cut = await OpenAsync();

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find("[data-balette=error]");
            Assert.AreEqual("alert", error.GetAttribute("role"));
            Assert.Contains("Couldn’t load this list.", error.TextContent);
        });
        Assert.ContainsSingle(_log.Entries.Where(e => e.Level == LogLevel.Error && e.Exception is HttpRequestException));

        // Enter on the error retries, as the button does.
        await Press(cut, "Enter");

        cut.WaitForAssertion(() => Assert.Contains("Ada", cut.Find("[data-balette=item]").TextContent));
        Assert.IsEmpty(cut.FindAll("[data-balette=error]"));
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    public async Task The_retry_button_loads_the_page_again()
    {
        var attempts = 0;
        _service.Register(BaletteCmd.New("People").Children((_, _) => ++attempts == 1
            ? throw new HttpRequestException("503")
            : Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Ada").Build()])));
        var cut = await OpenAsync();
        await Press(cut, "Enter");

        cut.WaitForElement("[data-balette=retry]").Click();

        cut.WaitForAssertion(() => Assert.Contains("Ada", cut.Find("[data-balette=item]").TextContent));
    }

    // ---- Links -------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_link_with_a_scheme_outside_the_allow_list_is_refused()
    {
        _service.Register(
            BaletteCmd.Navigate("Evil", " java\tscript:alert(1)"),
            BaletteCmd.Navigate("Docs", "/docs"));
        var navigation = Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;
        var cut = await OpenAsync("evil");

        await Press(cut, "Enter");

        Assert.AreEqual(before, navigation.Uri);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
        Assert.AreEqual("error", cut.Find("[data-balette=toast]").GetAttribute("data-kind"));
        Assert.ContainsSingle(_log.Messages(LogLevel.Warning).Where(m => m.Contains("'javascript'")));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("docs"));
        await Press(cut, "Enter");

        Assert.AreEqual(navigation.ToAbsoluteUri("/docs").ToString(), navigation.Uri);
    }

    // ---- Shortcuts, ids and refresh -------------------------------------------------------------------------

    [TestMethod]
    public async Task Refresh_reevaluates_visibility_for_the_global_shortcuts()
    {
        var selection = 0;
        var merged = false;
        _service.Register(BaletteCmd.Action("Merge", () => merged = true).Shortcut("Mod+M").VisibleWhen(() => selection > 1));
        var cut = Render<Balette>();

        selection = 2;
        await cut.InvokeAsync(_service.Refresh);

        cut.WaitForAssertion(() => Assert.Contains("mod+m", LastSyncedShortcuts()));
        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+m"));
        Assert.IsTrue(merged);

        selection = 0;
        await cut.InvokeAsync(_service.Refresh);
        cut.WaitForAssertion(() => Assert.DoesNotContain("mod+m", LastSyncedShortcuts()));
    }

    [TestMethod]
    public async Task When_two_commands_claim_one_shortcut_the_one_registered_last_runs_and_the_conflict_is_logged()
    {
        var ran = new List<string>();
        _service.Register(BaletteCmd.Action("Save all", () => ran.Add("layout")).Id("save-all").Shortcut("Mod+S"));
        var page = _service.Register(BaletteCmd.Action("Save document", () => ran.Add("page")).Id("save-document").Shortcut("Mod+S"));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+s"));
        AssertSequence.AreEqual(["page"], ran);
        Assert.ContainsSingle(_log.Messages(LogLevel.Warning).Where(m => m.Contains("'mod+s'") && m.Contains("'save-document'")));

        // The page's registration is gone: the layout's command answers again.
        await cut.InvokeAsync(page.Dispose);
        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+s"));
        AssertSequence.AreEqual(["page", "layout"], ran);
    }

    [TestMethod]
    public void A_shortcut_the_toggle_takes_and_a_sequence_that_cannot_complete_are_logged()
    {
        _service.Register(
            BaletteCmd.New("Search").Shortcut("Mod+K"),
            BaletteCmd.New("Go").Shortcut("G"),
            BaletteCmd.New("Go to issues").Shortcut("G I"));

        Render<Balette>();

        var warnings = _log.Messages(LogLevel.Warning).ToList();
        Assert.ContainsSingle(warnings.Where(m => m.Contains("'mod+k'") && m.Contains("toggle")));
        Assert.ContainsSingle(warnings.Where(m => m.Contains("'g i'") && m.Contains("'g'")));
    }

    [TestMethod]
    public void Two_commands_with_one_id_in_one_registration_are_logged()
    {
        _service.Register(BaletteCmd.New("C#"), BaletteCmd.New("C++"));

        Assert.ContainsSingle(_log.Messages(LogLevel.Warning).Where(m => m.Contains("'c'") && m.Contains("'C#'") && m.Contains("'C++'")));
    }
}

/// <summary>A history store holding what a hand-edited or corrupt <c>localStorage</c> entry can hold.</summary>
[TestClass]
public class BaletteCorruptHistoryTests : BunitContext
{
    [TestMethod]
    public async Task A_history_with_entries_it_cannot_use_does_not_break_the_palette()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteHistoryStore<CorruptHistoryStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var service = Services.GetRequiredService<IBaletteService>();
        service.Register(BaletteCmd.New("Alpha"), BaletteCmd.New("Beta"));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => service.OpenAsync());

        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-balette=item]")));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    private sealed class CorruptHistoryStore : IBaletteHistoryStore
    {
        public ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
            => new(new Dictionary<string, BaletteCommandUsage>
            {
                ["alpha"] = null!,
                [" "] = new(3, DateTimeOffset.UtcNow, false),
                ["beta"] = new(-5, DateTimeOffset.UtcNow, true),
            });

        public ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
