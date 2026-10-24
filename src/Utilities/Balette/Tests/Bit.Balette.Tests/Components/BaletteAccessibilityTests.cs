using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// What assistive technology is told: the listbox's grouping, the state of an option, the words behind a shortcut's key
/// caps, the announcements of pages and toasts, and the localizable texts that only screen readers hear.
/// </summary>
[TestClass]
public class BaletteAccessibilityTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BaletteOptions _options;

    public BaletteAccessibilityTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        var handle = module.SetupModule("init", _ => true);
        handle.Setup<bool>("isMac").SetResult(false);

        _service = Services.GetRequiredService<IBaletteService>();
        _options = Services.GetRequiredService<BaletteOptions>();
    }

    private async Task<IRenderedComponent<Balette>> OpenAsync(Action<ComponentParameterCollectionBuilder<Balette>>? parameters = null, string? query = null)
    {
        var cut = parameters is null ? Render<Balette>() : Render<Balette>(parameters);
        await cut.InvokeAsync(() => _service.OpenAsync(query));
        return cut;
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key, bool ctrl = false)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, ctrl, false, false, false));

    [TestMethod]
    public async Task Each_group_is_a_listbox_group_named_by_its_heading()
    {
        _service.Register(
            BaletteCmd.New("Home").Group("Navigation"),
            BaletteCmd.New("Settings").Group("Navigation"),
            BaletteCmd.New("Dark mode").Group("Preferences"));
        var cut = await OpenAsync();

        var list = cut.Find("[role=listbox]");
        Assert.AreEqual("Results", list.GetAttribute("aria-label"));

        // A listbox owns options and groups only (WAI-ARIA); each group is named by the heading inside it.
        var children = list.Children.ToList();
        Assert.IsTrue(children.All(c => c.GetAttribute("role") is "group" or "option"), string.Join(", ", children.Select(c => c.GetAttribute("role"))));
        var groups = cut.FindAll("[role=group]");
        Assert.HasCount(2, groups);
        foreach (var group in groups)
        {
            var heading = group.QuerySelector("[data-balette=group-heading]")!;
            Assert.AreEqual(heading.Id, group.GetAttribute("aria-labelledby"));
            Assert.IsTrue(group.QuerySelectorAll("[role=option]").Length > 0);
        }

        AssertSequence.AreEqual(["Navigation", "Preferences"], groups.Select(g => g.QuerySelector("[data-balette=group-heading]")!.TextContent.Trim()));
    }

    [TestMethod]
    public async Task Ungrouped_commands_below_the_recents_get_a_heading_of_their_own()
    {
        _service.Register(BaletteCmd.Action("Alpha", () => { }), BaletteCmd.New("Beta"), BaletteCmd.New("Home").Group("Navigation"));
        var cut = await OpenAsync();
        await Press(cut, "Enter");

        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() => AssertSequence.AreEqual(
            ["Recent", "Suggestions", "Navigation"],
            cut.FindAll("[data-balette=group-heading]").Select(h => h.TextContent.Trim())));
        Assert.IsTrue(cut.FindAll("[role=listbox] > [role=option]").Count == 0);
    }

    [TestMethod]
    public async Task A_single_select_list_marks_the_active_row_and_a_multi_select_one_the_checked_rows()
    {
        _service.Register(
            BaletteCmd.New("Single"),
            BaletteCmd.New("Labels").MultiSelect().Children(BaletteCmd.New("bug"), BaletteCmd.New("docs")));
        var cut = await OpenAsync(query: "single");

        var active = cut.Find("[data-active=true]");
        Assert.AreEqual("true", active.GetAttribute("aria-selected"));
        Assert.IsNull(active.GetAttribute("aria-checked"));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("labels"));
        await Press(cut, "Enter");
        await Press(cut, "Enter");

        // Never both states in one list: aria-activedescendant says which row is active.
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("[role=option]");
            Assert.HasCount(2, rows);
            Assert.IsTrue(rows.All(r => r.GetAttribute("aria-selected") is null));
            AssertSequence.AreEqual(["true", "false"], rows.Select(r => r.GetAttribute("aria-checked")));
        });
        Assert.AreEqual("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
    }

    [TestMethod]
    public async Task A_toggles_state_and_a_pin_are_spoken_not_only_drawn()
    {
        _service.Register(BaletteCmd.New("Word wrap").Checked(true), BaletteCmd.New("Minimap").Checked(false));
        var history = Services.GetRequiredService<BaletteHistory>();
        await history.TogglePinAsync("minimap");
        var cut = await OpenAsync();

        cut.WaitForAssertion(() =>
        {
            var wrap = cut.Find("[data-value='word-wrap']");
            Assert.AreEqual("On", wrap.QuerySelector(".bit-balette-item-trailing .bit-balette-sr-only")!.TextContent);
            var minimap = cut.Find("[data-value='minimap']");
            Assert.AreEqual("Off", minimap.QuerySelector(".bit-balette-item-trailing .bit-balette-sr-only")!.TextContent);
            Assert.AreEqual("Pinned", minimap.QuerySelector(".bit-balette-pin .bit-balette-sr-only")!.TextContent);
            Assert.IsNull(minimap.QuerySelector(".bit-balette-pin")!.GetAttribute("aria-label"));
        });
    }

    [TestMethod]
    public void A_shortcut_hint_reads_left_to_right_and_speaks_its_words_in_the_palettes_language()
    {
        var cut = Render<BaletteShortcutHint>(p => p
            .Add(x => x.Shortcut, BaletteKeyboardShortcut.Parse("G S"))
            .Add(x => x.Strings, new BaletteStrings { ShortcutThen = "dann" }));

        var hint = cut.Find("[data-balette=shortcut]");
        Assert.AreEqual("ltr", hint.GetAttribute("dir"));
        Assert.AreEqual("G dann S", hint.QuerySelector(".bit-balette-sr-only")!.TextContent);
        var caps = hint.QuerySelector(".bit-balette-shortcut-keys")!;
        Assert.AreEqual("true", caps.GetAttribute("aria-hidden"));
        Assert.AreEqual(2, caps.QuerySelectorAll("kbd").Length);
        Assert.Contains("dann", caps.TextContent);
    }

    [TestMethod]
    public void Without_strings_a_hint_uses_the_options_strings()
    {
        _options.Strings = new BaletteStrings { ShortcutThen = "سپس" };

        var cut = Render<BaletteShortcutHint>(p => p.Add(x => x.Shortcut, BaletteKeyboardShortcut.Parse("G I")));

        Assert.AreEqual("G سپس I", cut.Find(".bit-balette-sr-only").TextContent);
    }

    [TestMethod]
    public async Task Breadcrumbs_are_localized_and_mark_the_current_page()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.Page("Accent", BaletteCmd.New("Indigo"))));
        var cut = await OpenAsync(p => p.Add(x => x.Strings, new BaletteStrings { Breadcrumb = "مسیر" }));
        await Press(cut, "Enter");
        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("مسیر", cut.Find("[data-balette=breadcrumbs]").GetAttribute("aria-label"));
            var crumbs = cut.FindAll("[data-balette=crumb]");
            AssertSequence.AreEqual(["Theme", "Accent"], crumbs.Select(c => c.TextContent.Trim()));
            AssertSequence.AreEqual([null, "page"], crumbs.Select(c => c.GetAttribute("aria-current")));
            Assert.IsTrue(crumbs.All(c => c.GetAttribute("dir") == "auto"));
            Assert.AreEqual("Accent", cut.Find("[role=listbox]").GetAttribute("aria-label"));
        });
    }

    [TestMethod]
    public async Task Moving_to_a_page_announces_where_the_palette_went_and_then_only_counts()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light"), BaletteCmd.New("Dark")), BaletteCmd.New("Other"));
        var cut = await OpenAsync(query: "theme");
        var live = () => cut.Find("[aria-live=polite]").TextContent.Trim();
        // Theme, and Light and Dark found through it (SearchNestedCommands).
        Assert.AreEqual("3 results available", live());

        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.AreEqual("Theme. 2 results available", live()));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("dar"));
        Assert.AreEqual("1 result available", live());

        await Press(cut, "Escape");
        cut.WaitForAssertion(() => Assert.AreEqual("Home. 3 results available", live()));
    }

    [TestMethod]
    public async Task Counts_can_be_announced_with_the_plural_rules_of_any_language()
    {
        _service.Register(BaletteCmd.New("One"), BaletteCmd.New("Two"), BaletteCmd.New("Three"));
        var cut = await OpenAsync(p => p.Add(x => x.Strings, new PluralStrings()));

        Assert.AreEqual("3 wyniki", cut.Find("[aria-live=polite]").TextContent.Trim());
    }

    [TestMethod]
    public async Task Toasts_are_announced_politely_even_without_a_footer()
    {
        _service.Register(BaletteCmd.New("Copy link").Handler(_ => Task.FromResult(BaletteCommandResult.Toast("Link copied", keepOpen: true))));
        var cut = await OpenAsync(p => p.Add(x => x.ShowFooter, false));

        await Press(cut, "Enter");

        cut.WaitForAssertion(() => Assert.AreEqual("Link copied", cut.Find("[data-balette=status]").TextContent.Trim()));
        Assert.AreEqual(string.Empty, cut.Find("[data-balette=alert]").TextContent.Trim());
        Assert.IsEmpty(cut.FindAll("[data-balette=footer]"));
    }

    [TestMethod]
    public async Task A_loading_list_is_busy()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<BaletteCommand>>();
        _service.Register(BaletteCmd.New("People").Children((_, _) => pending.Task));
        var cut = await OpenAsync();

        _ = Press(cut, "Enter");

        // Nothing listed yet: a status says it is loading, and there is no listbox - which may hold only options - to be busy.
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("status", cut.Find("[data-balette=loading]").GetAttribute("role"));
            Assert.IsNull(cut.Find("[data-balette=list]").GetAttribute("role"));
            Assert.AreEqual("false", cut.Find("[data-balette=input]").GetAttribute("aria-expanded"));
            Assert.AreEqual("People. Searching…", cut.Find("[aria-live=polite]").TextContent.Trim());
        });

        pending.SetResult([BaletteCmd.New("Ada").Build()]);
        cut.WaitForAssertion(() =>
        {
            Assert.IsNull(cut.Find("[role=listbox]").GetAttribute("aria-busy"));
            Assert.AreEqual("true", cut.Find("[data-balette=input]").GetAttribute("aria-expanded"));
        });
    }

    [TestMethod]
    public async Task A_prompt_is_a_text_field_named_by_its_question_and_not_a_combobox()
    {
        _service.Register(BaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt("New project name", _ => Task.FromResult(BaletteCommandResult.Close)))));
        var cut = await OpenAsync();

        await Press(cut, "Enter");

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("[data-balette=input]");
            Assert.IsNull(input.GetAttribute("role"));
            Assert.AreEqual("New project name", input.GetAttribute("aria-label"));
            Assert.IsNull(input.GetAttribute("aria-expanded"));
            Assert.IsNull(input.GetAttribute("aria-controls"));
            Assert.IsEmpty(cut.FindAll("[role=listbox]"));
        });
    }

    [TestMethod]
    public async Task Virtualized_options_carry_their_group_position_and_count()
    {
        _service.Register(BaletteCmd.New("Home").Group("Navigation"), BaletteCmd.New("Settings").Group("Navigation"));
        var cut = await OpenAsync(p => p.Add(x => x.Virtualize, true));

        cut.WaitForAssertion(() =>
        {
            var heading = cut.Find("[data-balette=group-heading]");
            Assert.AreEqual("true", heading.GetAttribute("aria-hidden"));
            var options = cut.FindAll("[role=option]");
            Assert.HasCount(2, options);
            Assert.IsTrue(options.All(o => o.GetAttribute("aria-describedby") == heading.Id));
            AssertSequence.AreEqual(["1", "2"], options.Select(o => o.GetAttribute("aria-posinset")));
            Assert.IsTrue(options.All(o => o.GetAttribute("aria-setsize") == "2"));
        });
    }

    [TestMethod]
    public async Task User_text_isolates_its_own_direction_and_arrows_mirror_in_right_to_left_text()
    {
        _service.Register(BaletteCmd.Navigate("تنظیمات", "/settings").Subtitle("Settings").Badge("جدید"), BaletteCmd.Page("Theme", BaletteCmd.New("Dark")));
        var cut = await OpenAsync();

        var row = cut.Find("[data-value='تنظیمات']");
        Assert.AreEqual("auto", row.QuerySelector("[data-balette=label]")!.GetAttribute("dir"));
        Assert.AreEqual("auto", row.QuerySelector(".bit-balette-item-subtitle")!.GetAttribute("dir"));
        Assert.AreEqual("auto", row.QuerySelector("[data-balette=badge]")!.GetAttribute("dir"));
        Assert.AreEqual("true", row.QuerySelector(".bit-balette-item-icon svg")!.GetAttribute("data-mirror"));
        Assert.AreEqual("true", cut.Find("[data-value='theme'] .bit-balette-chevron svg").GetAttribute("data-mirror"));
    }

    private sealed class PluralStrings : BaletteStrings
    {
        // Polish: 2-4 take "wyniki", 5+ "wyników".
        public override string ResultsAnnouncementFor(int count) => count % 10 is >= 2 and <= 4 && count % 100 is < 12 or > 14
            ? $"{count} wyniki"
            : $"{count} wyników";
    }
}

[TestClass]
public class HighlightedTextTests
{
    [TestMethod]
    public void A_match_on_part_of_a_grapheme_marks_all_of_it()
    {
        // A decomposed é: the combining acute goes with its e.
        AssertSequence.AreEqual([false, false, false, true, true], BaletteHighlightedText.HitMask("café", [3]));
        // Both halves of a surrogate pair.
        AssertSequence.AreEqual([true, true, false], BaletteHighlightedText.HitMask("🚀x", [1]));
        // Plain text is left as it was, and out-of-range positions are ignored.
        AssertSequence.AreEqual([true, false, true], BaletteHighlightedText.HitMask("abc", [0, 2, 9, -1]));
    }

    [TestMethod]
    public void No_mark_splits_a_grapheme()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<BaletteHighlightedText>(p => p.Add(x => x.Text, "café bar").Add(x => x.Positions, [3]));

        Assert.AreEqual("é", cut.Find("mark").TextContent);
    }
}
