using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Balette.Tests;

/// <summary>
/// Guards: a command's own, its pages', and the registered <see cref="IBaletteCommandGuard"/>s, asked at execution time
/// on every path - a row, a shortcut, code, a confirmation's second Enter, a prompt or multi-select submit, a link opened
/// in a new tab - and a listed row whose <c>VisibleWhen</c> turned false since the list was built.
/// </summary>
[TestClass]
public class BaletteGuardTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly LogSink _log;
    private readonly List<BaletteCommandRefusedEventArgs> _refused = [];

    public BaletteGuardTests()
    {
        _log = LogSink.AddTo(Services);
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

    // ---- A command's own guard -------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_refused_row_does_not_run_and_says_so()
    {
        var ran = false;
        _service.Register(BaletteCmd.New("Delete workspace").Guard(_ => false).Handler(() => ran = true));
        var cut = await OpenAsync("delete");

        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("You can’t run “Delete workspace”.", Toast(cut));
            Assert.AreEqual("error", cut.Find("[data-balette=toast]").GetAttribute("data-kind"));
            Assert.AreEqual("You can’t run “Delete workspace”.", cut.Find("[data-balette=alert]").TextContent.Trim());
        });
        Assert.IsTrue(_service.IsOpen);
        var refused = Assert.ContainsSingle(_refused);
        Assert.AreEqual("delete-workspace", refused.Command.Id);
        Assert.AreEqual(BaletteExecutionTrigger.Palette, refused.Trigger);
        Assert.IsNull(refused.Message);
        Assert.Contains(m => m.Contains("delete-workspace") && m.Contains("refused"), _log.Messages(LogLevel.Information));
    }

    [TestMethod]
    public async Task A_guard_can_say_why_or_say_nothing()
    {
        _service.Register(
            BaletteCmd.New("Export").Guard(_ => false, "Exports need the Pro plan.").Handler(() => { }),
            BaletteCmd.New("Archive").Guard((_, _) => ValueTask.FromResult(BaletteGuardResult.Deny(""))).Handler(() => { }));
        var cut = await OpenAsync("export");

        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("Exports need the Pro plan.", Toast(cut)));
        Assert.AreEqual("Exports need the Pro plan.", _refused[^1].Message);

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("archive"));
        await Press(cut, "Enter");
        Assert.HasCount(2, _refused);
        Assert.IsNull(Toast(cut));
    }

    [TestMethod]
    public async Task An_allowed_guard_sees_the_command_and_what_started_it_and_the_command_runs()
    {
        var seen = new List<(string Id, BaletteExecutionTrigger Trigger)>();
        var ran = 0;
        _service.Register(BaletteCmd.New("Sync").Shortcut("Alt+Y")
            .Guard(async (ctx, ct) => { await Task.Yield(); seen.Add((ctx.Command.Id, ctx.Trigger)); return true; })
            .Handler(() => ran++));
        var cut = await OpenAsync("sync");

        await Press(cut, "Enter");
        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("alt+y"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("sync"));

        cut.WaitForAssertion(() => Assert.AreEqual(3, ran));
        Assert.AreSequenceEqual(new[]
        {
            ("sync", BaletteExecutionTrigger.Palette),
            ("sync", BaletteExecutionTrigger.Shortcut),
            ("sync", BaletteExecutionTrigger.Programmatic),
        }, seen);
        Assert.IsEmpty(_refused);
    }

    [TestMethod]
    public async Task A_guard_that_throws_refuses_and_is_logged()
    {
        var ran = false;
        _service.Register(BaletteCmd.New("Pay").Guard((_, _) => throw new HttpRequestException("503")).Handler(() => ran = true));
        var cut = await OpenAsync("pay");

        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("You can’t run “Pay”.", Toast(cut)));
        Assert.Contains(e => e.Level == LogLevel.Error && e.Exception is HttpRequestException, _log.Entries);
        Assert.ContainsSingle(_refused);
    }

    [TestMethod]
    public void The_builder_keeps_a_guard_on_copies()
    {
        var command = BaletteCmd.New("Sync").Guard(_ => false).Build();

        var copy = command.With(b => b.Title("Sync now"));

        Assert.AreSame(command.Guard, copy.Guard);
    }

    // ---- Every way a command runs ----------------------------------------------------------------------------

    [TestMethod]
    public async Task A_refused_shortcut_and_a_refused_call_from_code_run_nothing_and_throw_nothing()
    {
        var ran = 0;
        _service.Register(BaletteCmd.New("Deploy").Shortcut("Alt+D").Guard(_ => false).Handler(() => ran++));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("alt+d"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("deploy"));

        Assert.AreEqual(0, ran);
        Assert.IsFalse(_service.IsOpen);
        Assert.AreSequenceEqual(new[] { BaletteExecutionTrigger.Shortcut, BaletteExecutionTrigger.Programmatic }, _refused.Select(r => r.Trigger).ToList());
    }

    [TestMethod]
    public async Task A_confirmation_is_not_armed_for_a_refused_command_and_is_asked_again_on_the_second_enter()
    {
        var allowed = true;
        var ran = false;
        _service.Register(BaletteCmd.New("Delete project").Destructive("This cannot be undone").Guard(_ => allowed).Handler(() => ran = true));
        var cut = await OpenAsync("delete");

        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-confirming=true]")));

        // The permission went away between the two presses.
        allowed = false;
        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() =>
        {
            Assert.IsEmpty(cut.FindAll("[data-confirming=true]"));
            Assert.AreEqual("You can’t run “Delete project”.", Toast(cut));
        });

        // Refused from the start, nothing is armed at all.
        await Press(cut, "Enter");
        Assert.IsEmpty(cut.FindAll("[data-confirming=true]"));
        Assert.IsFalse(ran);
    }

    [TestMethod]
    public async Task A_prompt_and_a_multi_select_page_ask_again_when_submitted()
    {
        var allowed = true;
        string? renamed = null;
        IReadOnlyList<BaletteCommand>? applied = null;
        _service.Register(
            BaletteCmd.New("Rename…").Guard(_ => allowed)
                .Handler(_ => Task.FromResult(BaletteCommandResult.Prompt("New name", ctx => { renamed = ctx.PromptValue; return Task.FromResult(BaletteCommandResult.Close); }))),
            BaletteCmd.New("Labels").Guard(_ => allowed).MultiSelect()
                .Children(BaletteCmd.New("Bug"), BaletteCmd.New("Docs"))
                .Handler(ctx => { applied = ctx.SelectedCommands; return Task.FromResult(BaletteCommandResult.Close); }));
        var cut = await OpenAsync("rename");

        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-balette=prompt]")));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Roadmap"));
        allowed = false;
        await Press(cut, "Enter");
        Assert.IsNull(renamed);
        cut.WaitForAssertion(() => Assert.AreEqual("You can’t run “Rename…”.", Toast(cut)));

        allowed = true;
        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync(null, "labels"));
        await Press(cut, " ");
        allowed = false;
        await Press(cut, "Enter", ctrl: true);
        Assert.IsNull(applied);
        cut.WaitForAssertion(() => Assert.AreEqual("You can’t run “Labels”.", Toast(cut)));
    }

    [TestMethod]
    public async Task A_link_opened_in_a_new_tab_is_guarded_too()
    {
        _service.Register(BaletteCmd.New("Billing portal").Href("https://billing.example.com").Guard(_ => false));
        var cut = await OpenAsync("billing");

        await Press(cut, "Enter", ctrl: true);

        Assert.IsEmpty(JSInterop.Invocations.Where(i => i.Identifier == "openInNewTab"));
        Assert.ContainsSingle(_refused);
    }

    // ---- A page guards what is under it ----------------------------------------------------------------------

    [TestMethod]
    public async Task A_pages_guard_covers_its_registered_children_however_they_run()
    {
        var ran = new List<string>();
        _service.Register(BaletteCmd.New("Admin").Guard(ctx => ctx.Command.Id != "admin.purge-cache", "Admins only.")
            .Children(BaletteCmd.New("Purge cache").Shortcut("Alt+P").Handler(() => ran.Add("purge")), BaletteCmd.New("Audit log").Handler(() => ran.Add("audit"))));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("admin.purge-cache"));
        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("alt+p"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("admin.audit-log"));

        // Found from the root by nested search, too.
        await cut.InvokeAsync(() => _service.OpenAsync("purge"));
        await Press(cut, "Enter");

        Assert.AreSequenceEqual(new[] { "audit" }, ran);
        Assert.HasCount(3, _refused);
        Assert.IsTrue(_refused.All(r => r.Command.Id == "admin.purge-cache" && r.Message == "Admins only."));
    }

    [TestMethod]
    public async Task A_pages_guard_covers_what_its_lazy_loader_returned()
    {
        var ran = false;
        _service.Register(BaletteCmd.New("Servers").Guard(ctx => ctx.Command.Id == "servers", "Read only.")
            .Children((_, _) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Restart eu-1").Handler(() => ran = true).Build()])));
        var cut = await OpenAsync(page: "servers");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("Read only.", Toast(cut)));
    }

    [TestMethod]
    public async Task A_guard_still_deciding_when_the_palette_closes_refuses_quietly()
    {
        var decision = new TaskCompletionSource<BaletteGuardResult>();
        CancellationToken seen = default;
        var ran = false;
        _service.Register(BaletteCmd.New("Ship").Guard(async (_, ct) => { seen = ct; return await decision.Task.WaitAsync(ct); }).Handler(() => ran = true));
        var cut = await OpenAsync("ship");

        var run = Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("true", cut.Find("[data-value='ship']").GetAttribute("data-running")), TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(() => _service.CloseAsync());
        await run;

        Assert.IsTrue(seen.IsCancellationRequested);
        Assert.IsFalse(ran);
        Assert.IsEmpty(_refused);
    }

    // ---- Stale rows -------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_listed_row_that_stopped_being_visible_does_not_run()
    {
        var signedIn = true;
        var ran = false;
        _service.Register(BaletteCmd.New("Sign out").VisibleWhen(() => signedIn).Handler(() => ran = true));
        var cut = await OpenAsync("sign");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        // Signed out in another tab; nobody called Refresh().
        signedIn = false;
        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("“Sign out” is no longer available.", Toast(cut));
            Assert.IsEmpty(cut.FindAll("[data-balette=item]"));
        });
    }

    [TestMethod]
    public async Task A_row_whose_page_was_hidden_since_does_not_run()
    {
        var admin = true;
        var ran = false;
        _service.Register(BaletteCmd.New("Admin").VisibleWhen(() => admin).Children(BaletteCmd.New("Purge cache").Handler(() => ran = true)));
        var cut = await OpenAsync("purge");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        admin = false;
        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("“Purge cache” is no longer available.", Toast(cut)));
    }

    [TestMethod]
    public async Task Without_guards_a_hidden_command_still_runs_from_code()
    {
        var ran = false;
        _service.Register(BaletteCmd.New("Hidden helper").VisibleWhen(() => false).Handler(() => ran = true));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("hidden-helper"));

        Assert.IsTrue(ran);
    }
}

/// <summary>Guards registered with <see cref="BitBalette.AddBitBaletteCommandGuard{TGuard}"/>.</summary>
[TestClass]
public class BaletteRegisteredGuardTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly List<BaletteCommandRefusedEventArgs> _refused = [];

    public BaletteRegisteredGuardTests()
    {
        Services.AddBitBaletteServices(o => o.DebounceMs = 0);
        Services.AddBitBaletteInMemoryHistoryStore();
        Services.AddSingleton<RoleGuard.Roles>();
        Services.AddBitBaletteCommandGuard<RoleGuard>();
        Services.AddBitBaletteCommandGuard<RoleGuard>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        module.SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);

        _service = Services.GetRequiredService<IBaletteService>();
        _service.CommandRefused += (_, e) => _refused.Add(e);
    }

    [TestMethod]
    public async Task A_command_refused_by_its_own_guard_never_reaches_the_registered_ones()
    {
        var roles = Services.GetRequiredService<RoleGuard.Roles>();
        _service.Register(BaletteCmd.New("Billing").Guard(_ => false, "Not on this plan.").Handler(() => { }));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("billing"));

        Assert.AreEqual(0, roles.Asked);
        Assert.AreEqual("Not on this plan.", Assert.ContainsSingle(_refused).Message);
    }

    [TestMethod]
    public async Task A_registered_guard_is_resolved_from_the_scope_and_registered_once()
    {
        var roles = Services.GetRequiredService<RoleGuard.Roles>();
        var ran = new List<string>();
        _service.Register(
            BaletteCmd.New("Invite people").Tags("role:admin").Handler(() => ran.Add("invite")),
            BaletteCmd.New("Open docs").Handler(() => ran.Add("docs")));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("invite-people"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("open-docs"));
        roles.Add("admin");
        await cut.InvokeAsync(() => _service.ExecuteAsync("invite-people"));

        Assert.AreSequenceEqual(new[] { "docs", "invite" }, ran);
        Assert.AreEqual("Ask an admin to invite people.", Assert.ContainsSingle(_refused).Message);
        Assert.AreEqual(3, roles.Asked); // registered twice, asked once per run
    }

    /// <summary>Refuses commands tagged <c>role:x</c> unless the user has role x - an app's guard over its own convention.</summary>
    private sealed class RoleGuard(RoleGuard.Roles roles) : IBaletteCommandGuard
    {
        public ValueTask<BaletteGuardResult> CheckAsync(BaletteGuardContext context, CancellationToken cancellationToken)
        {
            roles.Asked++;
            foreach (var tag in context.Command.Tags)
            {
                if (tag.StartsWith("role:", StringComparison.Ordinal) && !roles.Contains(tag[5..]))
                {
                    return ValueTask.FromResult(BaletteGuardResult.Deny($"Ask an {tag[5..]} to {context.Command.Title.ToLowerInvariant()}."));
                }
            }

            return ValueTask.FromResult(BaletteGuardResult.Allow);
        }

        public sealed class Roles : HashSet<string>
        {
            public int Asked { get; set; }
        }
    }
}
