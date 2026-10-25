using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Balette.Tests;

[TestClass]
public class QueryParserTests
{
    [TestMethod]
    public void The_parser_is_public_and_reports_each_terms_operator_and_group()
    {
        var query = BaletteQuery.Parse("^go | 'open settings !debug \"word wrap\"$");

        AssertSequence.AreEqual(
            [
                new BaletteQueryTerm("go", BaletteQueryOperator.Prefix, 0),
                new BaletteQueryTerm("open", BaletteQueryOperator.Exact, 0),
                new BaletteQueryTerm("settings", BaletteQueryOperator.Fuzzy, 1),
                new BaletteQueryTerm("debug", BaletteQueryOperator.Negate, 2),
                new BaletteQueryTerm("word wrap", BaletteQueryOperator.Suffix, 3),
            ],
            query.Terms);
        Assert.IsEmpty(BaletteQuery.Parse("   ").Terms);
    }

    [TestMethod]
    public void A_bar_without_terms_on_both_sides_is_ignored_and_one_inside_a_word_is_text()
    {
        AssertSequence.AreEqual([new BaletteQueryTerm("a", BaletteQueryOperator.Fuzzy, 0), new BaletteQueryTerm("b", BaletteQueryOperator.Fuzzy, 1)],
            BaletteQuery.Parse("| a b |").Terms);
        AssertSequence.AreEqual([new BaletteQueryTerm("a|b", BaletteQueryOperator.Fuzzy, 0)], BaletteQuery.Parse("a|b").Terms);
    }

    [TestMethod]
    public void One_alternative_matching_is_enough_and_the_best_one_scores()
    {
        var matcher = BaletteFuzzyMatcher.Default;
        var settings = BaletteCmd.New("Go to Settings").Build();
        var open = BaletteCmd.New("Open settings JSON").Build();
        var other = BaletteCmd.New("Close window").Build();

        Assert.IsNotNull(matcher.Match(settings, "^go | ^open settings"));
        Assert.IsNotNull(matcher.Match(open, "^go | ^open settings"));
        Assert.IsNull(matcher.Match(other, "^go | ^open settings"));

        // Through the parsed overload too, which is how a tool matches one parsed query against many commands.
        var parsed = BaletteQuery.Parse("json | yaml");
        Assert.IsNotNull(matcher.Match(open, parsed));
        Assert.IsNull(matcher.Match(settings, parsed));
    }

    [TestMethod]
    public void A_negated_alternative_holds_when_its_text_is_absent()
    {
        var matcher = BaletteFuzzyMatcher.Default;

        // "is about git, or is not about docker"
        Assert.IsNotNull(matcher.Match(BaletteCmd.New("Git commit").Build(), "git | !docker"));
        Assert.IsNotNull(matcher.Match(BaletteCmd.New("Open file").Build(), "git | !docker"));
        Assert.IsNull(matcher.Match(BaletteCmd.New("Docker compose up").Build(), "git | !docker"));
    }
}

[TestClass]
public class PinOrderTests
{
    [TestMethod]
    public async Task Pins_are_listed_in_the_order_they_were_pinned()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var history = new BaletteHistory(new BaletteInMemoryHistoryStore(), new BaletteOptions(), time);

        foreach (var id in new[] { "zeta", "alpha", "mid" })
        {
            await history.TogglePinAsync(id);
            time.Advance(TimeSpan.FromMinutes(1));
        }

        AssertSequence.AreEqual(["zeta", "alpha", "mid"], history.Pinned());

        // Unpinned and pinned again: last.
        await history.TogglePinAsync("zeta");
        await history.TogglePinAsync("zeta");
        AssertSequence.AreEqual(["alpha", "mid", "zeta"], history.Pinned());
    }

    [TestMethod]
    public void The_pin_time_survives_storage_and_older_entries_without_it_still_load()
    {
        var at = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var usage = new Dictionary<string, BaletteCommandUsage> { ["a"] = new(1, at, true) { PinnedAt = at } };

        var json = JsonSerializer.Serialize(usage, BaletteHistoryJsonContext.Default.DictionaryStringBaletteCommandUsage);
        var back = JsonSerializer.Deserialize(json, BaletteHistoryJsonContext.Default.DictionaryStringBaletteCommandUsage)!;
        Assert.AreEqual(at, back["a"].PinnedAt);

        var old = JsonSerializer.Deserialize("""{"b":{"count":2,"lastUsed":"2026-01-01T00:00:00+00:00","pinned":true}}""", BaletteHistoryJsonContext.Default.DictionaryStringBaletteCommandUsage)!;
        Assert.IsTrue(old["b"].Pinned);
        Assert.IsNull(old["b"].PinnedAt);
    }
}

/// <summary>Source paging, the pending-sequence hint, clearing recents from the action panel and the spoken confirmation.</summary>
[TestClass]
public class BaletteRemainingGapsTests : BunitContext
{
    private readonly IBaletteService _service;

    public BaletteRemainingGapsTests()
    {
        Services.AddBitBaletteServices(o => o.DebounceMs = 0);
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js").SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBaletteService>();
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key, bool ctrl = false)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, ctrl, false, false, false));

    private static List<string?> ItemIds(IRenderedComponent<Balette> cut)
        => cut.FindAll("[data-balette=item]").Select(i => i.GetAttribute("data-value")).ToList();

    /// <summary>A directory of 25 people answered five at a time.</summary>
    private static BaletteDelegateSource PagedPeople(List<string?> cursors, bool total = true)
        => new("People", (query, cursor, _, _) =>
        {
            cursors.Add(cursor);
            var from = cursor is null ? 0 : int.Parse(cursor, System.Globalization.CultureInfo.InvariantCulture);
            IReadOnlyList<BaletteCommand> page = [.. Enumerable.Range(from, 5).Select(i => BaletteCmd.New($"Person {i:00}").Id($"p{i}").Build())];
            var next = from + 5 < 25 ? (from + 5).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            return Task.FromResult(new BaletteSourcePage(page, next, total ? 25 : null));
        });

    [TestMethod]
    public async Task A_paged_source_offers_the_rest_and_appends_it_where_the_reader_is()
    {
        var cursors = new List<string?>();
        _service.RegisterSource(PagedPeople(cursors));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("person"));

        cut.WaitForAssertion(() => Assert.HasCount(6, cut.FindAll("[data-balette=item]")));
        var more = cut.Find("[data-value='__source-more:People']");
        Assert.Contains("Show 20 more…", more.TextContent);

        // Choosing it loads the next page, after the first, and moves to its first row.
        for (var i = 0; i < 5; i++)
        {
            await Press(cut, "ArrowDown");
        }

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            AssertSequence.AreEqual(["p0", "p1", "p2", "p3", "p4", "p5", "p6", "p7", "p8", "p9", "__source-more:People"], ItemIds(cut));
            Assert.AreEqual("p5", cut.Find("[data-active=true]").GetAttribute("data-value"));
            Assert.Contains("Show 15 more…", cut.Find("[data-value='__source-more:People']").TextContent);
        });
        AssertSequence.AreEqual([null, "5"], cursors);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task Without_a_total_the_row_just_offers_more_and_the_last_page_has_none()
    {
        var cursors = new List<string?>();
        _service.RegisterSource(PagedPeople(cursors, total: false));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("person"));
        cut.WaitForAssertion(() => Assert.Contains("Show more…", cut.Find("[data-value='__source-more:People']").TextContent));

        for (var pages = 2; pages <= 5; pages++)
        {
            await Press(cut, "End");
            await Press(cut, "Enter");
            // A page loads in the background and a source is asked for one page at a time: wait for it before the next.
            cut.WaitForAssertion(() => Assert.HasCount(pages * 5, cut.FindAll("[data-balette=item]:not([data-value^='__source-more'])")));
        }

        cut.WaitForAssertion(() =>
        {
            Assert.HasCount(25, cut.FindAll("[data-balette=item]"));
            Assert.IsEmpty(cut.FindAll("[data-value='__source-more:People']"));
        });
    }

    [TestMethod]
    public async Task A_source_that_does_not_page_gets_no_row()
    {
        _service.RegisterSource(new BaletteDelegateSource("Docs", (_, _) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Getting started").Build()])));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync("getting"));

        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
        Assert.IsEmpty(cut.FindAll("[data-balette=item][data-value^='__source-more']"));
    }

    [TestMethod]
    public async Task A_pending_sequence_is_shown_announced_and_raised_until_it_ends()
    {
        var raised = new List<string?>();
        _service.SequencePending += chords => raised.Add(chords?.Normalized);
        var cut = Render<Balette>();
        var hint = () => cut.Find("[data-balette=sequence]");
        Assert.AreEqual("status", hint().GetAttribute("role"));
        Assert.IsNull(hint().GetAttribute("data-pending"));

        await cut.InvokeAsync(() => cut.Instance.OnSequencePending("g"));

        Assert.AreEqual("true", hint().GetAttribute("data-pending"));
        Assert.Contains("waiting for the next key", hint().TextContent);
        Assert.Contains("G", hint().QuerySelector(".bit-balette-sr-only")!.TextContent);

        await cut.InvokeAsync(() => cut.Instance.OnSequencePending(null));

        Assert.IsNull(hint().GetAttribute("data-pending"));
        Assert.AreEqual(string.Empty, hint().TextContent.Trim());
        AssertSequence.AreEqual(["g", null], raised);
    }

    [TestMethod]
    public void An_inline_palette_has_no_sequence_hint()
    {
        var cut = Render<Balette>(p => p.Add(x => x.Inline, true));

        Assert.IsEmpty(cut.FindAll("[data-balette=sequence]"));
    }

    [TestMethod]
    public async Task The_action_panel_can_clear_every_recent_command_and_keeps_the_pins()
    {
        _service.Register(BaletteCmd.Action("Alpha", () => { }), BaletteCmd.Action("Beta", () => { }));
        var history = Services.GetRequiredService<BaletteHistory>();
        await history.RecordAsync("alpha");
        await history.RecordAsync("beta");
        await history.TogglePinAsync("beta");
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("alpha"));

        await Press(cut, "Tab");
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-value='__clear-recents']")));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("clear"));
        await Press(cut, "Enter");

        Assert.IsEmpty(history.Recents(10));
        AssertSequence.AreEqual(["beta"], history.Pinned());
    }

    [TestMethod]
    public async Task An_armed_confirmation_tells_screen_readers_how_to_confirm_it()
    {
        _service.Register(BaletteCmd.New("Delete project").Destructive("This deletes the project").Handler(_ => { }));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("delete"));

        await Press(cut, "Enter");

        Assert.AreEqual("Confirm: This deletes the project. Press Enter again to confirm, Esc to cancel", cut.Find("[data-balette=status]").TextContent.Trim());
        // On screen, the footer keeps the message alone.
        Assert.AreEqual("Confirm: This deletes the project", cut.Find("[data-balette=toast]").TextContent.Trim());
    }
}
