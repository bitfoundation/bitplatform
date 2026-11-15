using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// Regression tests for what the independent review of the guards found: paths that skipped a guard (a handler's
/// multi-select page, its rows, an app id starting with "__", secondary actions, opening a page by id), a guard's answer
/// applied after the user moved on, a guard that ignores its token, a page's guard asked about an unrelated
/// confirmation, a hidden lazy page, and exception text in traces.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BaletteGuardReviewTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly List<BaletteCommandRefusedEventArgs> _refused = [];

    public BaletteGuardReviewTests()
    {
        Services.AddBitBaletteServices(o => o.DebounceMs = 0);
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        module.SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBaletteService>();
        _service.CommandRefused += (_, e) => _refused.Add(e);
    }

    private async Task<IRenderedComponent<Balette>> OpenAsync(string? query = null, string? page = null)
    {
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync(query, page));
        return cut;
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key, bool ctrl = false)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, ctrl, false, false, false));

    private static string? Toast(IRenderedComponent<Balette> cut)
        => cut.FindAll("[data-balette=toast]").SingleOrDefault()?.TextContent.Trim();

    [TestMethod]
    public async Task Submitting_a_multi_select_page_a_handler_returned_asks_the_guards_of_the_pages_it_came_from()
    {
        var allowed = true;
        IReadOnlyList<BaletteCommand>? applied = null;
        _service.Register(BaletteCmd.New("Admin").Guard(_ => allowed, "Admins only.").Children(
            BaletteCmd.New("Bulk").Handler(_ => Task.FromResult(BaletteCommandResult.Page("Pick", [BaletteCmd.New("Ada").Build(), BaletteCmd.New("Linus").Build()], multiSelect: true,
                onSubmit: ctx => { applied = ctx.SelectedCommands; return Task.FromResult(BaletteCommandResult.Close); })))));
        var cut = await OpenAsync(page: "admin");
        await Press(cut, "Enter"); // Bulk
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-balette=item]")));
        await Press(cut, " ");

        allowed = false;
        await Press(cut, "Enter", ctrl: true);

        Assert.IsNull(applied);
        cut.WaitForAssertion(() => Assert.AreEqual("Admins only.", Toast(cut)));
    }

    [TestMethod]
    public async Task The_rows_of_a_page_a_handler_returned_are_under_that_commands_guard()
    {
        var wiped = false;
        _service.Register(BaletteCmd.New("Danger zone").Guard(ctx => ctx.Command.Id == "danger-zone", "Look, don't touch.")
            .Handler(_ => Task.FromResult(BaletteCommandResult.Page("Danger zone", [BaletteCmd.New("Wipe").Handler(() => wiped = true).Build()]))));
        var cut = await OpenAsync("danger");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("wipe", cut.Find("[data-balette=item]").GetAttribute("data-value")));

        await Press(cut, "Enter");

        Assert.IsFalse(wiped);
        cut.WaitForAssertion(() => Assert.AreEqual("Look, don't touch.", Toast(cut)));
    }

    [TestMethod]
    public async Task An_app_command_whose_id_starts_with_two_underscores_is_still_guarded()
    {
        var ran = false;
        _service.Register(BaletteCmd.New("Internal").Id("__internal").Guard(_ => false).Handler(() => ran = true));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("__internal"));

        Assert.IsFalse(ran);
        Assert.ContainsSingle(_refused);
    }

    [TestMethod]
    public async Task A_secondary_action_is_refused_when_its_row_is_refused_hidden_or_disabled()
    {
        var allowed = true;
        var visible = true;
        var ran = 0;
        _service.Register(BaletteCmd.New("Delete project").VisibleWhen(() => visible).Guard(_ => allowed, "Owners only.")
            .Handler(() => { }).Actions(BaletteCmd.New("Delete without asking").Handler(() => ran++)));
        var cut = await OpenAsync("delete project");

        // Mod+Enter on a row hidden since it was listed.
        visible = false;
        await Press(cut, "Enter", ctrl: true);
        Assert.AreEqual(0, ran);
        cut.WaitForAssertion(() => Assert.AreEqual("“Delete project” is no longer available.", Toast(cut)));

        // Mod+Enter on a row its guard refuses.
        visible = true;
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("delete project"));
        allowed = false;
        await Press(cut, "Enter", ctrl: true);
        Assert.AreEqual(0, ran);
        cut.WaitForAssertion(() => Assert.AreEqual("Owners only.", Toast(cut)));

        // Tab, then Enter on the action: the action panel is under its row.
        await Press(cut, "Tab");
        cut.WaitForAssertion(() => Assert.IsTrue(cut.FindAll("[data-balette=item]").Any(i => i.TextContent.Contains("Delete without asking"))));
        await Press(cut, "Enter");
        Assert.AreEqual(0, ran);

        allowed = true;
        await Press(cut, "Enter");
        Assert.AreEqual(1, ran);
    }

    [TestMethod]
    public async Task Mod_enter_on_a_disabled_row_does_not_run_its_first_action()
    {
        var ran = false;
        _service.Register(BaletteCmd.New("Archive").Disabled(true, "Read only").Actions(BaletteCmd.New("Archive now").Handler(() => ran = true)));
        var cut = await OpenAsync("archive");

        await Press(cut, "Enter", ctrl: true);

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("Read only", Toast(cut)));
    }

    [TestMethod]
    public async Task Going_back_while_a_guard_decides_drops_its_answer()
    {
        var decision = new TaskCompletionSource<BaletteGuardResult>();
        var ran = false;
        _service.Register(BaletteCmd.New("Ops").Children(BaletteCmd.New("Restart").Guard((_, _) => new ValueTask<BaletteGuardResult>(decision.Task)).Handler(() => ran = true)));
        var cut = await OpenAsync(page: "ops");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        var run = Press(cut, "Enter");
        await Press(cut, "Escape"); // back to the root, the guard still deciding
        decision.SetResult(BaletteGuardResult.Allow);
        await run;

        Assert.IsFalse(ran);
        Assert.IsEmpty(_refused);
        Assert.IsTrue(_service.IsOpen);
    }

    [TestMethod]
    public async Task Opening_a_guarded_page_by_id_asks_its_guard_before_loading_it()
    {
        var loads = 0;
        _service.Register(BaletteCmd.New("Secrets").Guard(_ => false)
            .Children((_, _) => { loads++; return Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("API key").Build()]); }));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync(null, "secrets"));

        Assert.AreEqual(0, loads);
        Assert.IsFalse(_service.IsOpen);
        Assert.AreEqual(BaletteExecutionTrigger.Programmatic, Assert.ContainsSingle(_refused).Trigger);
    }

    [TestMethod]
    public async Task A_guard_that_ignores_its_token_does_not_keep_a_closed_palette_from_running_commands()
    {
        var ran = false;
        _service.Register(
            BaletteCmd.New("Stuck").Guard(async (_, _) => { await Task.Delay(Timeout.Infinite, CancellationToken.None); return true; }).Handler(() => { }),
            BaletteCmd.New("Next").Handler(() => ran = true));
        var cut = await OpenAsync("stuck");

        var run = Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("true", cut.Find("[data-value='stuck']").GetAttribute("data-running")), TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(() => _service.CloseAsync());
        await run;
        await cut.InvokeAsync(() => _service.ExecuteAsync("next"));

        Assert.IsTrue(ran);
    }

    [TestMethod]
    public async Task A_confirmation_started_from_code_is_not_under_the_guard_of_the_page_it_was_pushed_on()
    {
        var reset = false;
        _service.Register(
            BaletteCmd.New("Admin").Guard(ctx => ctx.Command.Id.StartsWith("admin", StringComparison.Ordinal), "Admin only.").Children(BaletteCmd.New("Audit log")),
            BaletteCmd.New("Reset sample data").Destructive("Sample data will be recreated").Handler(() => reset = true));
        var cut = await OpenAsync(page: "admin");

        await cut.InvokeAsync(() => _service.ExecuteAsync("reset-sample-data"));
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-confirming=true]")));
        await Press(cut, "Enter");

        Assert.IsTrue(reset);
        Assert.IsEmpty(_refused);
    }

    [TestMethod]
    public async Task The_rows_of_a_lazy_page_hidden_since_it_opened_do_not_run()
    {
        var shown = true;
        var ran = false;
        _service.Register(BaletteCmd.New("Servers").VisibleWhen(() => shown)
            .Children((_, _) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Restart eu-1").Handler(() => ran = true).Build()])));
        var cut = await OpenAsync("servers");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("restart-eu-1", cut.Find("[data-balette=item]").GetAttribute("data-value")));

        shown = false;
        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("“Restart eu-1” is no longer available.", Toast(cut)));
    }

    [TestMethod]
    public async Task A_failure_traced_records_the_exception_type_but_not_its_message()
    {
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == BaletteDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);
        _service.Register(BaletteCmd.New("Rename…").Id("probe-rename")
            .Handler(_ => Task.FromResult(BaletteCommandResult.Prompt("New name", ctx => throw new InvalidOperationException($"bad name {ctx.PromptValue}")))));
        var cut = await OpenAsync("rename");
        await Press(cut, "Enter");
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("top-secret-value"));

        await Press(cut, "Enter");

        var failed = stopped.Single(a => (string?)a.GetTagItem("balette.outcome") == "failed");
        Assert.AreEqual(ActivityStatusCode.Error, failed.Status);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, failed.GetTagItem("exception.type"));
        Assert.IsEmpty(failed.Events);
        Assert.IsFalse(stopped.Any(a => a.TagObjects.Any(t => t.Value is string s && s.Contains("top-secret-value", StringComparison.Ordinal))));
        Assert.IsFalse(stopped.Any(a => (a.StatusDescription ?? string.Empty).Contains("top-secret-value", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_palettes_own_rows_are_not_counted_as_commands()
    {
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == BaletteDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);
        _service.Register(BaletteCmd.Action("Deploy", () => { }));
        var cut = await OpenAsync("deploy");

        await Press(cut, "Tab");          // the action panel: Pin to top
        cut.WaitForAssertion(() => Assert.IsNotEmpty(cut.FindAll("[data-balette=item]")));
        await Press(cut, "Enter");

        Assert.IsEmpty(stopped);
    }
}
