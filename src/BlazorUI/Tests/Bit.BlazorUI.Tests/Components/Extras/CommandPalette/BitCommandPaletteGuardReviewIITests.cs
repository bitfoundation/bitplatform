using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// Regression tests for the second review of the guards: a prompt's submit, a secondary action's confirmation and the
/// rows checked on a multi-select page are guarded as what they act on; guards deciding do not hold up navigation or code;
/// a missing ancestor fails closed; Mod+Enter asks each guard once; and logs keep ids out when traces do.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BitCommandPaletteGuardReviewIITests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;
    private readonly List<BitCommandPaletteCommandRefusedEventArgs> _refused = [];
    private readonly CountingGuard.Counter _registered = new();
    private readonly CapturingLogger _log = new();

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitBlazorUIExtrasServices(commandPalette: o => { o.DebounceMs = 0; o.RecordCommandIdsInTraces = false; });
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Services.AddSingleton(_registered);
        Services.AddBitCommandPaletteCommandGuard<CountingGuard>();
        Services.AddSingleton<ILogger<BitCommandPalette>>(_log);
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
    public async Task Submitting_a_prompt_asks_the_guard_of_the_command_that_opened_it()
    {
        var allowed = true;
        string? submitted = null;
        _service.Register(BitCommandPaletteCmd.New("Rename").Guard(_ => allowed, "Owners only.")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New name", ctx => { submitted = ctx.PromptValue; return Task.FromResult(BitCommandPaletteCommandResult.Close); }))));
        var cut = await OpenAsync("rename");
        await Press(cut, "Enter");
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Apollo"));

        allowed = false;
        await Press(cut, "Enter");

        Assert.IsNull(submitted);
        Assert.AreEqual("rename", Assert.ContainsSingle(_refused).Command.Id);
        cut.WaitForAssertion(() => Assert.AreEqual("Owners only.", Toast(cut)));
    }

    [TestMethod]
    public async Task Submitting_a_prompt_opened_from_code_asks_the_guard_of_the_command_that_opened_it()
    {
        var allowed = true;
        string? submitted = null;
        _service.Register(BitCommandPaletteCmd.New("Rename").Guard(_ => allowed)
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New name", ctx => { submitted = ctx.PromptValue; return Task.FromResult(BitCommandPaletteCommandResult.Close); }))));
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.ExecuteAsync("rename"));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Apollo"));

        allowed = false;
        await Press(cut, "Enter");

        Assert.IsNull(submitted);
        Assert.AreEqual("rename", Assert.ContainsSingle(_refused).Command.Id);
    }

    [TestMethod]
    public async Task Confirming_a_rows_secondary_action_asks_the_rows_guard_again()
    {
        var allowed = true;
        var deleted = false;
        _service.Register(BitCommandPaletteCmd.New("Delete project").Guard(_ => allowed, "Owners only.").Handler(() => { })
            .Actions(BitCommandPaletteCmd.New("Delete forever").Destructive("The project is gone for good").Handler(() => deleted = true)));
        var cut = await OpenAsync("delete project");

        await Press(cut, "Enter", ctrl: true);
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-confirming=true]")));
        allowed = false;
        await Press(cut, "Enter");

        Assert.IsFalse(deleted);
        cut.WaitForAssertion(() => Assert.AreEqual("Owners only.", Toast(cut)));
    }

    [TestMethod]
    public async Task Confirming_a_rows_secondary_action_is_refused_once_the_row_is_hidden()
    {
        var visible = true;
        var deleted = false;
        _service.Register(BitCommandPaletteCmd.New("Delete project").VisibleWhen(() => visible).Handler(() => { })
            .Actions(BitCommandPaletteCmd.New("Delete forever").Destructive("The project is gone for good").Handler(() => deleted = true)));
        var cut = await OpenAsync("delete project");

        await Press(cut, "Enter", ctrl: true);
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-confirming=true]")));
        visible = false;
        await Press(cut, "Enter");

        Assert.IsFalse(deleted);
        cut.WaitForAssertion(() => Assert.AreEqual("“Delete forever” is no longer available.", Toast(cut)));
    }

    [TestMethod]
    public async Task Mod_enter_on_a_row_asks_each_guard_once()
    {
        var rowAsked = 0;
        var ran = false;
        _service.Register(BitCommandPaletteCmd.New("Deploy").Guard(_ => { rowAsked++; return true; }).Handler(() => { })
            .Actions(BitCommandPaletteCmd.New("Deploy now").Handler(() => ran = true)));
        var cut = await OpenAsync("deploy");

        await Press(cut, "Enter", ctrl: true);

        Assert.IsTrue(ran);
        Assert.AreEqual(1, rowAsked);
        Assert.AreEqual(1, _registered.Asked);
    }

    [TestMethod]
    public async Task An_action_of_a_nested_row_found_from_the_root_asks_the_ancestor_pages_guard()
    {
        var allowed = true;
        var ran = false;
        _service.Register(BitCommandPaletteCmd.New("Admin").Guard(_ => allowed, "Admins only.").Children(
            BitCommandPaletteCmd.New("Rotate keys").Handler(() => { }).Actions(BitCommandPaletteCmd.New("Rotate now").Handler(() => ran = true))));
        var cut = await OpenAsync("rotate keys");
        cut.WaitForAssertion(() => Assert.AreEqual("admin.rotate-keys", cut.Find("[data-bit-cpl=item]").GetAttribute("data-value")));

        // Tab opens the row's actions straight from the root, with no page of Admin under them.
        await Press(cut, "Tab");
        cut.WaitForAssertion(() => Assert.AreEqual("Rotate now", cut.Find("[data-bit-cpl=item]").TextContent.Trim()));
        allowed = false;
        await Press(cut, "Enter");

        Assert.IsFalse(ran);
        cut.WaitForAssertion(() => Assert.AreEqual("Admins only.", Toast(cut)));
    }

    [TestMethod]
    public async Task Submitting_a_multi_select_page_asks_the_guards_of_the_rows_checked()
    {
        IReadOnlyList<BitCommandPaletteCommand>? applied = null;
        _service.Register(BitCommandPaletteCmd.New("Delete users").Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page("Pick",
            [BitCommandPaletteCmd.New("Root").Guard(_ => false, "The root account stays.").Build(), BitCommandPaletteCmd.New("Ada").Build()], multiSelect: true,
            onSubmit: ctx => { applied = ctx.SelectedCommands; return Task.FromResult(BitCommandPaletteCommandResult.Close); }))));
        var cut = await OpenAsync("delete users");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]")));
        await Press(cut, " "); // Root

        await Press(cut, "Enter", ctrl: true);

        Assert.IsNull(applied);
        Assert.AreEqual("root", Assert.ContainsSingle(_refused).Command.Id);
        cut.WaitForAssertion(() => Assert.AreEqual("The root account stays.", Toast(cut)));
    }

    [TestMethod]
    public async Task The_registered_guards_are_asked_about_each_row_checked()
    {
        IReadOnlyList<BitCommandPaletteCommand>? applied = null;
        _service.Register(BitCommandPaletteCmd.New("Tag").Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page("Pick",
            [BitCommandPaletteCmd.New("Ada").Build(), BitCommandPaletteCmd.New("Linus").Tags(CountingGuard.Refuse).Build()], multiSelect: true,
            onSubmit: ctx => { applied = ctx.SelectedCommands; return Task.FromResult(BitCommandPaletteCommandResult.Close); }))));
        var cut = await OpenAsync("tag");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]")));
        await Press(cut, " ");          // Ada
        await Press(cut, "ArrowDown");
        await Press(cut, " ");          // Linus

        await Press(cut, "Enter", ctrl: true);

        Assert.IsNull(applied);
        Assert.AreEqual("linus", Assert.ContainsSingle(_refused).Command.Id);
    }

    [TestMethod]
    public async Task Submitting_a_multi_select_page_with_a_row_hidden_since_it_was_checked_is_refused()
    {
        var visible = true;
        IReadOnlyList<BitCommandPaletteCommand>? applied = null;
        _service.Register(BitCommandPaletteCmd.New("Archive").Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page("Pick",
            [BitCommandPaletteCmd.New("Ada").VisibleWhen(() => visible).Build(), BitCommandPaletteCmd.New("Linus").Build()], multiSelect: true,
            onSubmit: ctx => { applied = ctx.SelectedCommands; return Task.FromResult(BitCommandPaletteCommandResult.Close); }))));
        var cut = await OpenAsync("archive");
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]")));
        await Press(cut, " "); // Ada

        visible = false;
        await Press(cut, "Enter", ctrl: true);

        Assert.IsNull(applied);
        cut.WaitForAssertion(() => Assert.AreEqual("“Ada” is no longer available.", Toast(cut)));
    }

    [TestMethod]
    public async Task Navigating_while_a_guard_decides_still_closes_the_palette_and_drops_the_command()
    {
        var decision = new TaskCompletionSource<BitCommandPaletteGuardResult>();
        var ran = false;
        _service.Register(BitCommandPaletteCmd.New("Restart").Guard((_, _) => new ValueTask<BitCommandPaletteGuardResult>(decision.Task)).Handler(() => ran = true));
        var cut = await OpenAsync("restart");

        var run = Press(cut, "Enter");
        await cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere"));
        cut.WaitForAssertion(() => Assert.IsFalse(_service.IsOpen));
        decision.SetResult(BitCommandPaletteGuardResult.Allow);
        await run;

        Assert.IsFalse(ran);
    }

    [TestMethod]
    public async Task Code_runs_its_command_while_a_guard_decides_about_a_row()
    {
        var decision = new TaskCompletionSource<BitCommandPaletteGuardResult>();
        var ran = new List<string>();
        _service.Register(
            BitCommandPaletteCmd.New("Restart").Guard((_, _) => new ValueTask<BitCommandPaletteGuardResult>(decision.Task)).Handler(() => ran.Add("restart")),
            BitCommandPaletteCmd.New("Refresh").Handler(_ => { ran.Add("refresh"); return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen); }));
        var cut = await OpenAsync("restart");

        var run = Press(cut, "Enter");
        await Press(cut, "Enter"); // a second Enter is turned away
        await cut.InvokeAsync(() => _service.ExecuteAsync("refresh"));
        decision.SetResult(BitCommandPaletteGuardResult.Allow);
        await run;

        CollectionAssert.AreEqual(new[] { "refresh", "restart" }, ran);
    }

    [TestMethod]
    public async Task A_command_whose_ancestor_cannot_be_found_is_refused()
    {
        var ran = false;
        _service.Register(BitCommandPaletteCmd.New("Orphan").Id("orphan").Parent("gone").Handler(() => ran = true));
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("orphan"));

        Assert.IsFalse(ran);
        Assert.AreEqual("orphan", Assert.ContainsSingle(_refused).Command.Id);
    }

    [TestMethod]
    public async Task Logs_keep_ids_and_titles_out_when_traces_do()
    {
        _service.Register(BitCommandPaletteCmd.New("Ada Lovelace").Id("person:ada@example.com").Guard(_ => false).Handler(() => { }));
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("person:ada@example.com"));

        Assert.ContainsSingle(_refused);
        Assert.IsNotEmpty(_log.Messages);
        Assert.IsFalse(_log.Messages.Any(m => m.Contains("ada", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Counts what it is asked, and refuses commands tagged <see cref="Refuse"/>.</summary>
    private sealed class CountingGuard(CountingGuard.Counter counter) : IBitCommandPaletteCommandGuard
    {
        public const string Refuse = "refuse";

        public ValueTask<BitCommandPaletteGuardResult> CheckAsync(BitCommandPaletteGuardContext context, CancellationToken cancellationToken)
        {
            counter.Asked++;
            return ValueTask.FromResult(context.Command.Tags.Contains(Refuse) ? BitCommandPaletteGuardResult.Deny() : BitCommandPaletteGuardResult.Allow);
        }

        public sealed class Counter
        {
            public int Asked { get; set; }
        }
    }

    private sealed class CapturingLogger : ILogger<BitCommandPalette>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
