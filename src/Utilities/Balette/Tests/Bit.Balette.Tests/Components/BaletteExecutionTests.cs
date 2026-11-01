using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Balette.Tests;

/// <summary>
/// Commands that take time or need input: the busy state of a slow handler, async sources that fail or time out and
/// their retry, a page that searches a server as the user types, prompts that validate, errors a handler expects, and
/// the shortcuts to a link's new tab and a row's action panel.
/// </summary>
[TestClass]
public class BaletteExecutionTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BaletteOptions _options;
    private readonly BunitJSModuleInterop _jsHandle;
    private readonly LogSink _log;

    public BaletteExecutionTests()
    {
        _log = LogSink.AddTo(Services);
        Services.AddBitBaletteServices(o => o.DebounceMs = 0);
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        _jsHandle = module.SetupModule("init", _ => true);
        _jsHandle.Setup<bool>("isMac").SetResult(false);

        _service = Services.GetRequiredService<IBaletteService>();
        _options = Services.GetRequiredService<BaletteOptions>();
    }

    private async Task<IRenderedComponent<Balette>> OpenAsync(string? query = null)
    {
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync(query));
        return cut;
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key, bool ctrl = false)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, ctrl, false, false, false));

    private static List<string?> ItemIds(IRenderedComponent<Balette> cut)
        => cut.FindAll("[data-balette=item]").Select(i => i.GetAttribute("data-value")).ToList();

    // ---- Busy ------------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_slow_handler_shows_the_palette_busy_and_a_quick_one_does_not()
    {
        var gate = new TaskCompletionSource();
        _service.Register(
            BaletteCmd.New("Deploy").Handler(async _ => { await gate.Task; return BaletteCommandResult.Close; }),
            BaletteCmd.Action("Quick", () => { }));
        var cut = await OpenAsync("deploy");

        var run = Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-busy"));
            Assert.AreEqual("true", cut.Find("[role=listbox]").GetAttribute("aria-busy"));
            Assert.AreEqual("true", cut.Find("[data-value='deploy']").GetAttribute("data-running"));
            Assert.IsNotNull(cut.Find("[data-balette=spinner]"));
            Assert.AreEqual("Working…", cut.Find("[aria-live=polite]").TextContent.Trim());
        }, TimeSpan.FromSeconds(5));

        gate.SetResult();
        await run;
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
        Assert.IsNull(cut.Find("dialog").GetAttribute("data-busy"));
    }

    // ---- Sources ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_failing_source_leaves_a_retry_row_and_the_other_sources_results_stay()
    {
        var attempts = 0;
        _service.RegisterSource(new BaletteDelegateSource("People", (q, ct) => ++attempts == 1
            ? throw new HttpRequestException("503")
            : Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Ada Lovelace").Build()])));
        _service.RegisterSource(new BaletteDelegateSource("Docs", (q, ct) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Adapters").Build()])));
        var cut = await OpenAsync("ada");

        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("[data-error=true]");
            Assert.Contains("Couldn’t search People", row.TextContent);
            Assert.Contains("Try again", row.TextContent);
            Assert.Contains("Adapters", cut.Find("[role=listbox]").TextContent);
        });
        Assert.ContainsSingle(_log.Entries.Where(e => e.Level == LogLevel.Warning && e.Exception is HttpRequestException));

        // Choosing the row asks that source again, for the same query, and keeps the palette open.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Home", false, false, false, false));
        var errorIndex = ItemIds(cut).FindIndex(id => id == "__source-error:People");
        for (var i = 0; i < errorIndex; i++)
        {
            await Press(cut, "ArrowDown");
        }

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            Assert.IsEmpty(cut.FindAll("[data-error=true]"));
            Assert.Contains("Ada Lovelace", cut.Find("[role=listbox]").TextContent);
        });
        Assert.AreEqual(2, attempts);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task A_source_that_outlives_its_timeout_counts_as_failed()
    {
        _options.SourceTimeoutMs = 50;
        _service.RegisterSource(new BaletteDelegateSource("Slow", async (q, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        }));
        var cut = await OpenAsync("anything");

        cut.WaitForAssertion(() => Assert.Contains("Couldn’t search Slow", cut.Find("[data-error=true]").TextContent), TimeSpan.FromSeconds(5));
        Assert.ContainsSingle(_log.Messages(LogLevel.Warning).Where(m => m.Contains("'Slow'") && m.Contains("50 ms")));
    }

    [TestMethod]
    public void The_source_timeout_must_not_be_negative()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _options.SourceTimeoutMs = -1);

    [TestMethod]
    public async Task A_page_searches_its_own_sources_as_the_user_types()
    {
        var asked = new List<(string Query, string? Parent)>();
        _service.Register(BaletteCmd.New("Assign to…").Id("assign").Source("People", (query, ct) =>
        {
            asked.Add((query, null));
            IReadOnlyList<BaletteCommand> people = [.. new[] { "Ada", "Alan", "Grace" }
                .Where(p => p.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Select(p => BaletteCmd.New(p).Id($"person:{p}").Build())];
            return Task.FromResult(people);
        }));
        _service.RegisterSource(new BaletteDelegateSource("Root only", (q, ct) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Not here").Build()]), minQueryLength: 0));
        var cut = await OpenAsync("assign");
        await Press(cut, "Enter");

        // Asked as soon as the page opens (minQueryLength 0), then for what is typed.
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["person:Ada", "person:Alan", "person:Grace"], ItemIds(cut)));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("al"));
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["person:Alan"], ItemIds(cut)));
        Assert.DoesNotContain("Not here", cut.Find("[role=listbox]").TextContent);
        AssertSequence.AreEqual(["", "al"], asked.Select(a => a.Query));
    }

    [TestMethod]
    public async Task A_page_source_tells_the_source_which_page_asks()
    {
        string? parent = null;
        var source = new BaletteDelegateSource("People", (q, ctx, ct) =>
        {
            parent = ctx.ParentCommandId;
            return Task.FromResult<IReadOnlyList<BaletteCommand>>([]);
        }, minQueryLength: 0);
        _service.Register(BaletteCmd.New("Assign to…").Id("assign").Source(source));
        var cut = await OpenAsync("assign");

        await Press(cut, "Enter");

        cut.WaitForAssertion(() => Assert.AreEqual("assign", parent));
    }

    [TestMethod]
    public async Task A_multi_select_page_applies_what_was_checked_across_searches()
    {
        IReadOnlyList<BaletteCommand>? applied = null;
        _service.Register(BaletteCmd.New("Add reviewers").Id("reviewers").MultiSelect()
            .Source("People", (query, ct) => Task.FromResult<IReadOnlyList<BaletteCommand>>(
                [.. new[] { "Ada", "Alan", "Grace" }.Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)).Select(p => BaletteCmd.New(p).Id($"person:{p}").Build())]))
            .Handler(ctx =>
            {
                applied = ctx.SelectedCommands;
                return Task.FromResult(BaletteCommandResult.Close);
            }));
        var cut = await OpenAsync("reviewers");
        await Press(cut, "Enter");

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("ada"));
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["person:Ada"], ItemIds(cut)));
        await Press(cut, "Enter");
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("grace"));
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["person:Grace"], ItemIds(cut)));
        await Press(cut, "Enter");
        await Press(cut, "Enter", ctrl: true);

        CollectionAssert.AreEquivalent(new[] { "person:Ada", "person:Grace" }, applied!.Select(c => c.Id).ToArray());
    }

    // ---- Prompts and expected errors ----------------------------------------------------------------------------

    [TestMethod]
    public async Task A_prompt_refuses_an_invalid_value_and_its_message_follows_the_text()
    {
        string? renamed = null;
        _service.Register(BaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt(
            "New name",
            ctx => { renamed = ctx.PromptValue; return Task.FromResult(BaletteCommandResult.Close); },
            value => string.IsNullOrWhiteSpace(value) ? "A name is required." : value.Length > 10 ? "At most 10 characters." : null))));
        var cut = await OpenAsync();
        await Press(cut, "Enter");

        await Press(cut, "Enter");

        Assert.IsNull(renamed);
        var input = cut.Find("[data-balette=input]");
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual(cut.Find("[data-balette=prompt-error]").Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("A name is required.", cut.Find("[data-balette=prompt-error]").TextContent);
        Assert.AreEqual("A name is required.", cut.Find("[data-balette=alert]").TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("A much too long name"));
        Assert.AreEqual("At most 10 characters.", cut.Find("[data-balette=prompt-error]").TextContent);

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Short"));
        Assert.IsEmpty(cut.FindAll("[data-balette=prompt-error]"));
        Assert.IsNull(cut.Find("[data-balette=input]").GetAttribute("aria-invalid"));

        await Press(cut, "Enter");
        Assert.AreEqual("Short", renamed);
    }

    [TestMethod]
    public async Task A_query_too_long_to_match_still_reaches_sources_and_handlers_whole()
    {
        var question = string.Join(' ', Enumerable.Repeat("why does the arm64 build fail on the release branch", 6));
        string? asked = null;
        string? feedback = null;
        _service.RegisterSource(new BaletteDelegateSource("Answers", (q, ct) =>
        {
            asked = q;
            return Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Fixing arm64 builds").Id("answer").Build()]);
        }));
        _service.Register(BaletteCmd.New("Feedback: couldn't find a command?").Id("feedback").AlwaysShow()
            .Handler(ctx => { feedback = ctx.Query; return Task.FromResult(BaletteCommandResult.Close); }));
        var cut = await OpenAsync(question);

        // Nothing caps the input: the query matches no command, but the source searches it and the handler gets it whole.
        Assert.IsGreaterThan(BaletteQuery.MaxLength, question.Length);
        Assert.IsNull(cut.Find("[data-balette=input]").GetAttribute("maxlength"));
        cut.WaitForAssertion(() => CollectionAssert.AreEquivalent(new[] { "feedback", "answer" }, ItemIds(cut)));
        Assert.AreEqual(question, asked);

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Home", false, false, false, false));
        for (var i = 0; i < ItemIds(cut).IndexOf("feedback"); i++)
        {
            await Press(cut, "ArrowDown");
        }

        await Press(cut, "Enter");

        Assert.AreEqual(question, feedback);
    }

    [TestMethod]
    public async Task An_error_result_is_shown_and_keeps_the_prompt_for_a_correction()
    {
        var tries = 0;
        _service.Register(BaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt(
            "New name",
            ctx => Task.FromResult(++tries == 1 ? BaletteCommandResult.Error("That name is taken.") : BaletteCommandResult.Close)))));
        var cut = await OpenAsync();
        await Press(cut, "Enter");
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("Taken"));

        await Press(cut, "Enter");

        Assert.IsNotNull(cut.Find("[data-balette=prompt]"));
        Assert.AreEqual("That name is taken.", cut.Find("[data-balette=toast]").TextContent.Trim());
        Assert.AreEqual("That name is taken.", cut.Find("[data-balette=alert]").TextContent.Trim());
        Assert.AreEqual("Taken", cut.Find("[data-balette=input]").GetAttribute("value"));

        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
    }

    // ---- Links and the action panel ----------------------------------------------------------------------------

    [TestMethod]
    public async Task Mod_Enter_opens_a_link_in_a_new_tab()
    {
        _jsHandle.SetupVoid("openInNewTab", _ => true).SetVoidResult();
        _service.Register(BaletteCmd.Navigate("Docs", "https://example.com/docs"), BaletteCmd.Navigate("Evil", "javascript:alert(1)"));
        var navigation = Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;
        var cut = await OpenAsync("docs");

        await Press(cut, "Enter", ctrl: true);

        Assert.AreEqual(before, navigation.Uri);
        var opened = _jsHandle.Invocations.Single(i => i.Identifier == "openInNewTab");
        Assert.AreEqual("https://example.com/docs", opened.Arguments[0]);

        // The scheme check applies to new tabs as well.
        await cut.InvokeAsync(() => _service.OpenAsync("evil"));
        await Press(cut, "Enter", ctrl: true);
        Assert.ContainsSingle(_jsHandle.Invocations.Where(i => i.Identifier == "openInNewTab"));
    }

    [TestMethod]
    public async Task A_right_click_opens_a_rows_action_panel()
    {
        _service.Register(BaletteCmd.New("Report").Actions(BaletteCmd.New("Export as CSV"), BaletteCmd.New("Share")));
        var cut = await OpenAsync();

        cut.Find("[data-value='report']").ContextMenu();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("Report", cut.Find("[data-balette=crumb]").TextContent.Trim());
            Assert.Contains("Export as CSV", cut.Find("[role=listbox]").TextContent);
        });
    }
}
