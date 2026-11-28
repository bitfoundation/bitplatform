using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Time.Testing;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

[TestClass]
public class BitCommandPaletteCommandModelTests
{
    [TestMethod]
    public void Id_defaults_to_slug_of_title_group_and_parent()
    {
        var c = new BitCommandPaletteCommand { Title = "Go to Settings!", Group = "Navigation" };
        Assert.AreEqual("navigation.go-to-settings", c.Id);

        var child = new BitCommandPaletteCommand { Title = "Dark", Parent = "theme" };
        Assert.AreEqual("theme.dark", child.Id);

        var plain = new BitCommandPaletteCommand { Title = "  Hello   World " };
        Assert.AreEqual("hello-world", plain.Id);
    }

    [TestMethod]
    public void Builder_round_trips_through_With()
    {
        var c = BitCommandPaletteCmd.New("Rename").Shortcut("F2").Keywords("edit").Destructive("Sure?").Badge("New").Build();
        var copy = c.With(b => b.Title("Rename file"));
        Assert.AreEqual("Rename file", copy.Title);
        Assert.AreEqual(c.Id, copy.Id);
        Assert.AreEqual("f2", copy.Shortcut!.Normalized);
        AssertSequence.AreEqual(["edit"], copy.Keywords);
        Assert.IsTrue(copy.Destructive);
        Assert.AreEqual("Sure?", copy.ConfirmMessage);
        Assert.AreEqual("New", copy.Badge);
    }

    [TestMethod]
    public void Builder_infers_kind()
    {
        Assert.AreEqual(BitCommandPaletteCommandKind.Navigation, BitCommandPaletteCmd.Navigate("Home", "/").Build().Kind);
        Assert.AreEqual(BitCommandPaletteCommandKind.Page, BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Dark")).Build().Kind);
        Assert.AreEqual(BitCommandPaletteCommandKind.Toggle, BitCommandPaletteCmd.New("Wrap").Checked(true).Build().Kind);
        Assert.AreEqual(BitCommandPaletteCommandKind.Action, BitCommandPaletteCmd.Action("Save", () => { }).Build().Kind);
    }

    [TestMethod]
    public async Task Action_handler_closes_by_default()
    {
        var ran = false;
        var c = BitCommandPaletteCmd.Action("Save", () => ran = true).Build();
        var result = await c.Handler!(null!);
        Assert.IsTrue(ran);
        Assert.IsInstanceOfType<BitCommandPaletteCommandResult.CloseResult>(result);
    }

    [TestMethod]
    public void Mode_accepts_by_tag_and_group()
    {
        var mode = new BitCommandPaletteCommandMode { Prefix = ">", Name = "Commands", Tags = ["command"] };
        Assert.IsTrue(mode.Accepts(BitCommandPaletteCmd.New("A").Tags("command").Build()));
        Assert.IsFalse(mode.Accepts(BitCommandPaletteCmd.New("B").Build()));

        var groupMode = new BitCommandPaletteCommandMode { Prefix = "#", Name = "Issues", Groups = ["Issues"] };
        Assert.IsTrue(groupMode.Accepts(BitCommandPaletteCmd.New("C").Group("issues").Build()));
        Assert.IsFalse(groupMode.Accepts(BitCommandPaletteCmd.New("D").Group("Pages").Build()));

        var sourcesOnly = new BitCommandPaletteCommandMode { Prefix = "@", Name = "People", HideRegistryCommands = true };
        Assert.IsFalse(sourcesOnly.Accepts(BitCommandPaletteCmd.New("E").Build()));
    }
}

[TestClass]
public class BitCommandPaletteServiceTests
{
    private static BitCommandPaletteService NewService() => new(new BitCommandPaletteOptions());

    [TestMethod]
    public void Register_and_dispose_removes_commands()
    {
        var svc = NewService();
        var changed = 0;
        svc.CommandsChanged += () => changed++;

        var handle = svc.Register(BitCommandPaletteCmd.New("One"), BitCommandPaletteCmd.New("Two"));
        Assert.AreEqual(2, svc.Commands.Count);
        Assert.AreEqual(1, changed);

        handle.Dispose();
        Assert.IsEmpty(svc.Commands);
        Assert.AreEqual(2, changed);
    }

    [TestMethod]
    public void Nested_children_are_flattened_with_parent_ids()
    {
        var svc = NewService();
        svc.Register(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Light"), BitCommandPaletteCmd.New("Dark")));
        var ids = svc.Commands.Select(c => c.Id).OrderBy(x => x).ToList();
        AssertSequence.AreEqual(["theme", "theme.dark", "theme.light"], ids);
        foreach (var child in svc.Commands.Where(c => c.Id != "theme"))
        {
            Assert.AreEqual("theme", child.Parent);
        }
    }

    [TestMethod]
    public void Later_registration_overrides_same_id()
    {
        var svc = NewService();
        svc.Register(BitCommandPaletteCmd.New("Save").Id("save").Subtitle("global"));
        svc.Register(BitCommandPaletteCmd.New("Save").Id("save").Subtitle("page"));
        Assert.AreEqual("page", svc.Find("save")!.Subtitle);
        Assert.ContainsSingle(svc.Commands);
    }

    [TestMethod]
    public void Update_replaces_in_place()
    {
        var svc = NewService();
        svc.Register(BitCommandPaletteCmd.New("Wrap").Id("wrap").Checked(false));
        svc.Update(BitCommandPaletteCmd.New("Wrap").Id("wrap").Checked(true));
        Assert.IsTrue(svc.Find("wrap")!.Checked);
        Assert.ContainsSingle(svc.Commands);
    }

    [TestMethod]
    public void Update_replaces_the_static_children_too()
    {
        var svc = NewService();
        svc.Register(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Light"), BitCommandPaletteCmd.New("Dark")).Id("theme"));
        svc.Update(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Solarized")).Id("theme"));

        AssertSequence.AreEqual(["theme.solarized"], svc.Commands.Where(c => c.Parent == "theme").Select(c => c.Id));
    }

    [TestMethod]
    public void Update_flattens_a_command_it_adds()
    {
        var svc = NewService();
        svc.Update(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Dark")).Id("theme"));
        Assert.AreEqual("theme", svc.Find("theme.dark")?.Parent);
    }

    [TestMethod]
    public void Update_keeps_commands_registered_with_an_explicit_parent()
    {
        var svc = NewService();
        svc.Register(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Light")).Id("theme"), BitCommandPaletteCmd.New("Custom").Id("custom").Parent("theme"));
        svc.Update(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Dark")).Id("theme"));

        AssertSequence.AreEqual(["custom", "theme.dark"], svc.Commands.Where(c => c.Parent == "theme").Select(c => c.Id).Order());
    }

    [TestMethod]
    public void Destructive_without_a_message_keeps_an_earlier_confirmation()
    {
        var c = BitCommandPaletteCmd.New("Drop database").Confirm("Really drop?").Destructive().Build();
        Assert.IsTrue(c.Destructive);
        Assert.AreEqual("Really drop?", c.ConfirmMessage);
    }

    [TestMethod]
    public void Options_commands_are_preloaded()
    {
        var options = new BitCommandPaletteOptions();
        options.Commands.Add(BitCommandPaletteCmd.New("Preloaded"));
        options.Modes.Add(new BitCommandPaletteCommandMode { Prefix = ">", Name = "Commands" });
        var svc = new BitCommandPaletteService(options);
        Assert.ContainsSingle(svc.Commands);
        Assert.ContainsSingle(svc.Modes);
    }
}

[TestClass]
public class BitCommandPaletteCommandRankerTests
{
    private static readonly BitCommandPaletteFuzzyMatcher Matcher = new();

    [TestMethod]
    public void Empty_query_sorts_by_priority_then_frecency_then_title()
    {
        var ranker = new BitCommandPaletteRanker(new BitCommandPaletteOptions());
        var matches = new[]
        {
            Matcher.Match(BitCommandPaletteCmd.New("Zeta").Build(), "")!,
            Matcher.Match(BitCommandPaletteCmd.New("Alpha").Build(), "")!,
            Matcher.Match(BitCommandPaletteCmd.New("Important").Priority(BitCommandPaletteCommandPriority.High).Build(), "")!,
        };
        var ranked = ranker.Rank(matches, hasQuery: false, id => id == "zeta" ? 50 : 0);
        AssertSequence.AreEqual(["Important", "Zeta", "Alpha"], ranked.Select(r => r.Command.Title));
    }

    [TestMethod]
    public void Query_ranking_is_dominated_by_match_score()
    {
        var ranker = new BitCommandPaletteRanker(new BitCommandPaletteOptions());
        var exact = Matcher.Match(BitCommandPaletteCmd.New("Settings").Build(), "settings")!;
        var weak = Matcher.Match(BitCommandPaletteCmd.New("Set the ring tones").Priority(BitCommandPaletteCommandPriority.Highest).Build(), "settings")!;
        var ranked = ranker.Rank([weak, exact], hasQuery: true, _ => 0);
        Assert.AreEqual("Settings", ranked[0].Command.Title);
    }

    [TestMethod]
    [DataRow(25)]  // far below the match count: the best are kept in a heap
    [DataRow(200)] // just below it: everything is sorted and the front kept
    [DataRow(300)]
    public void Limited_ranking_keeps_the_same_order_as_a_full_sort(int limit)
    {
        var ranker = new BitCommandPaletteRanker(new BitCommandPaletteOptions());
        var matches = Enumerable.Range(0, 300)
            .Select(i => Matcher.Match(BitCommandPaletteCmd.New($"Item {i % 37}").Id($"item-{i}").Priority(i % 3).Build(), "item")!)
            .ToList();

        var full = ranker.Rank(matches, hasQuery: true, id => id.EndsWith('7') ? 4 : 0);
        var limited = ranker.Rank(matches, hasQuery: true, id => id.EndsWith('7') ? 4 : 0, limit: limit);

        AssertSequence.AreEqual(full.Take(limit).Select(m => m.Command.Id), limited.Select(m => m.Command.Id));
        AssertSequence.AreEqual(full.Take(limit).Select(m => m.Rank), limited.Select(m => m.Rank));
        Assert.IsEmpty(ranker.Rank(matches, hasQuery: false, _ => 0, limit: 0));
    }
}

[TestClass]
public class BitCommandPaletteCommandHistoryTests
{
    [TestMethod]
    public async Task Records_usage_and_computes_frecency()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var history = new BitCommandPaletteHistory(new BitCommandPaletteInMemoryHistoryStore(), new BitCommandPaletteOptions(), time);

        await history.RecordAsync("a");
        time.Advance(TimeSpan.FromMinutes(1));
        await history.RecordAsync("a");
        time.Advance(TimeSpan.FromMinutes(1));
        await history.RecordAsync("b");

        Assert.AreEqual(2, history.Get("a")!.Count);
        Assert.IsTrue(history.Frecency("a") > history.Frecency("b"));
        AssertSequence.AreEqual(["b", "a"], history.Recents(5));

        time.Advance(TimeSpan.FromDays(40));
        var old = history.Frecency("a");
        await history.RecordAsync("c");
        Assert.IsTrue(history.Frecency("c") > old);
    }

    [TestMethod]
    public async Task Pin_moves_out_of_recents_and_survives_clear()
    {
        var history = new BitCommandPaletteHistory(new BitCommandPaletteInMemoryHistoryStore(), new BitCommandPaletteOptions());
        await history.RecordAsync("a");
        await history.RecordAsync("b");
        await history.TogglePinAsync("a");

        AssertSequence.AreEqual(["a"], history.Pinned());
        AssertSequence.AreEqual(["b"], history.Recents(5));

        await history.ClearAsync();
        AssertSequence.AreEqual(["a"], history.Pinned());
        Assert.IsEmpty(history.Recents(5));
    }

    [TestMethod]
    public async Task Prunes_to_max_entries()
    {
        var options = new BitCommandPaletteOptions { MaxHistoryEntries = 3 };
        var time = new FakeTimeProvider();
        var history = new BitCommandPaletteHistory(new BitCommandPaletteInMemoryHistoryStore(), options, time);
        for (var i = 0; i < 6; i++)
        {
            await history.RecordAsync($"c{i}");
            time.Advance(TimeSpan.FromMinutes(1));
        }

        Assert.AreEqual(3, history.Recents(10).Count);
        AssertSequence.AreEqual(["c5", "c4", "c3"], history.Recents(10));
    }

    [TestMethod]
    public async Task Persists_through_store()
    {
        var store = new BitCommandPaletteInMemoryHistoryStore();
        var h1 = new BitCommandPaletteHistory(store, new BitCommandPaletteOptions());
        await h1.RecordAsync("x");

        var h2 = new BitCommandPaletteHistory(store, new BitCommandPaletteOptions());
        await h2.EnsureLoadedAsync();
        Assert.AreEqual(1, h2.Get("x")!.Count);
    }
}
