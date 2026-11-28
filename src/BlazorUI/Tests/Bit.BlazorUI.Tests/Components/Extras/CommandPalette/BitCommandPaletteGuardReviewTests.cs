using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// Regression tests for what the independent review of the guards found: paths that skipped a guard (a handler's
/// multi-select page, its rows, an app id starting with "__", secondary actions, opening a page by id), a guard's answer
/// applied after the user moved on, a guard that ignores its token, a page's guard asked about an unrelated
/// confirmation, a hidden lazy page, and exception text in traces.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BitCommandPaletteGuardReviewTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;
    private readonly List<BitCommandPaletteCommandRefusedEventArgs> _refused = [];

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitBlazorUIExtrasServices(commandPalette: o => o.DebounceMs = 0);
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBitCommandPaletteService>();
        _service.CommandRefused += (_, e) => _refused.Add(e);
    }

    private async Task<IRenderedComponent<BitCommandPalette>> OpenAsync(string? query = null, string? page = null)
    {
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync(query, page));
        return cut;
    }

    private static Task Press(IRenderedComponent<BitCommandPalette> cut, string key, bool ctrl = false)
        => cut.InvokeAsync(() => cut.Instance._OnKeyDown(key, ctrl, false, false, false));

    private static string? Toast(IRenderedComponent<BitCommandPalette> cut)
        => cut.FindAll("[data-bit-cpl=toast]").SingleOrDefault()?.TextContent.Trim();

    [TestMethod]
    public async Task Submitting_a_multi_select_page_a_handler_returned_asks_the_guards_of_the_pages_it_came_from()
    {
        var allowed = true;
        IReadOnlyList<BitCommandPaletteCommand>? applied = null;
        _service.Register(BitCommandPaletteCmd.New("Admin").Guard(_ => allowed, "Admins only.").Children(
            BitCommandPaletteCmd.New("Bulk").Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page("Pick", [BitCommandPaletteCmd.New("Ada").Build(), BitCommandPaletteCmd.New("Linus").Build()], multiSelect: true,
                onSubmit: ctx => { applied = ctx.SelectedCommands; return Task.FromResult(BitCommandPaletteCommandResult.Close); })))));
        var cut = await OpenAsync(page: "admin");
        await Press(cut, "Enter"); // Bulk
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]")));
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
        _service.Register(BitCommandPaletteCmd.New("Danger zone").Guard(ctx => ctx.Command.Id == "danger-zone", "Look, don't touch.")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page("Danger zone", [BitCommandPaletteCmd.New("Wipe").Handler(() => wiped = true).Build()]))));
        var cut = await OpenAsync("danger");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("wipe", cut.Find("[data-bit-cpl=item]").GetAttribute("data-value")));

        await Press(cut, "Enter");

        Assert.IsFalse(wiped);
        cut.WaitForAssertion(() => Assert.AreEqual("Look, don't touch.", Toast(cut)));
    }

    [TestMethod]
    public async Task An_app_command_whose_id_starts_with_two_underscores_is_still_guarded()
    {
        var ran = false;
        _service.Register(BitCommandPaletteCmd.New("Internal").Id("__internal").Guard(_ => false).Handler(() => ran = true));
        var cut = Context.Render<BitCommandPalette>();

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
        _service.Register(BitCommandPaletteCmd.New("Delete project").VisibleWhen(() => visible).Guard(_ => allowed, "Owners only.")
            .Handler(() => { }).Actions(BitCommandPaletteCmd.New("Delete without asking").Handler(() => ran++)));
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
        cut.WaitForAssertion(() => Assert.IsTrue(cut.FindAll("[data-bit-cpl=item]").Any(i => i.TextContent.Contains("Delete without asking"))));
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
        _service.Register(BitCommandPaletteCmd.New("Archive").Disabled(true, "Read only").Actions(BitCommandPaletteCmd.New("Archive now").Handler(() => ran = true)));
        var cut = await OpenAsync("archive");

        await Press(cut, "Enter", ctrl: true);

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("Read only", Toast(cut)));
    }

    [TestMethod]
    public async Task Going_back_while_a_guard_decides_drops_its_answer()
    {
        var decision = new TaskCompletionSource<BitCommandPaletteGuardResult>();
        var ran = false;
        _service.Register(BitCommandPaletteCmd.New("Ops").Children(BitCommandPaletteCmd.New("Restart").Guard((_, _) => new ValueTask<BitCommandPaletteGuardResult>(decision.Task)).Handler(() => ran = true)));
        var cut = await OpenAsync(page: "ops");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-bit-cpl=item]")));

        var run = Press(cut, "Enter");
        await Press(cut, "Escape"); // back to the root, the guard still deciding
        decision.SetResult(BitCommandPaletteGuardResult.Allow);
        await run;

        Assert.IsFalse(ran);
        Assert.IsEmpty(_refused);
        Assert.IsTrue(_service.IsOpen);
    }

    [TestMethod]
    public async Task Opening_a_guarded_page_by_id_asks_its_guard_before_loading_it()
    {
        var loads = 0;
        _service.Register(BitCommandPaletteCmd.New("Secrets").Guard(_ => false)
            .Children((_, _) => { loads++; return Task.FromResult<IReadOnlyList<BitCommandPaletteCommand>>([BitCommandPaletteCmd.New("API key").Build()]); }));
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.OpenAsync(null, "secrets"));

        Assert.AreEqual(0, loads);
        Assert.IsFalse(_service.IsOpen);
        Assert.AreEqual(BitCommandPaletteExecutionTrigger.Programmatic, Assert.ContainsSingle(_refused).Trigger);
    }

    [TestMethod]
    public async Task A_guard_that_ignores_its_token_does_not_keep_a_closed_palette_from_running_commands()
    {
        var ran = false;
        _service.Register(
            BitCommandPaletteCmd.New("Stuck").Guard(async (_, _) => { await Task.Delay(Timeout.Infinite, CancellationToken.None); return true; }).Handler(() => { }),
            BitCommandPaletteCmd.New("Next").Handler(() => ran = true));
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
            BitCommandPaletteCmd.New("Admin").Guard(ctx => ctx.Command.Id.StartsWith("admin", StringComparison.Ordinal), "Admin only.").Children(BitCommandPaletteCmd.New("Audit log")),
            BitCommandPaletteCmd.New("Reset sample data").Destructive("Sample data will be recreated").Handler(() => reset = true));
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
        _service.Register(BitCommandPaletteCmd.New("Servers").VisibleWhen(() => shown)
            .Children((_, _) => Task.FromResult<IReadOnlyList<BitCommandPaletteCommand>>([BitCommandPaletteCmd.New("Restart eu-1").Handler(() => ran = true).Build()])));
        var cut = await OpenAsync("servers");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("restart-eu-1", cut.Find("[data-bit-cpl=item]").GetAttribute("data-value")));

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
            ShouldListenTo = s => s.Name == BitCommandPaletteDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);
        _service.Register(BitCommandPaletteCmd.New("Rename…").Id("probe-rename")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New name", ctx => throw new InvalidOperationException($"bad name {ctx.PromptValue}")))));
        var cut = await OpenAsync("rename");
        await Press(cut, "Enter");
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("top-secret-value"));

        await Press(cut, "Enter");

        var failed = stopped.Single(a => (string?)a.GetTagItem("bit.command_palette.outcome") == "failed");
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
            ShouldListenTo = s => s.Name == BitCommandPaletteDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);
        _service.Register(BitCommandPaletteCmd.Action("Deploy", () => { }));
        var cut = await OpenAsync("deploy");

        await Press(cut, "Tab");          // the action panel: Pin to top
        cut.WaitForAssertion(() => Assert.IsNotEmpty(cut.FindAll("[data-bit-cpl=item]")));
        await Press(cut, "Enter");

        Assert.IsEmpty(stopped);
    }
}
