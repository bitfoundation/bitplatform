using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>Search across pages - nested commands found from the root, with their ancestors - and the second chance a query that found nothing gets.</summary>
[TestClass]
public class BitCommandPaletteSearchFeatureTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;
    private BitCommandPaletteOptions _options = default!;

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitCommandPaletteServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true).Setup<bool>("isMac").SetResult(false);

        _service = Services.GetRequiredService<IBitCommandPaletteService>();
        _options = Services.GetRequiredService<BitCommandPaletteOptions>();
    }

    private async Task<IRenderedComponent<BitCommandPalette>> OpenAsync(string? query = null, Action<ComponentParameterCollectionBuilder<BitCommandPalette>>? parameters = null)
    {
        var cut = parameters is null ? Context.Render<BitCommandPalette>() : Context.Render<BitCommandPalette>(parameters);
        await cut.InvokeAsync(() => _service.OpenAsync(query));
        return cut;
    }

    private static Task Press(IRenderedComponent<BitCommandPalette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance._OnKeyDown(key, false, false, false, false));

    private static List<string?> ItemIds(IRenderedComponent<BitCommandPalette> cut)
        => cut.FindAll("[data-bit-cpl=item]").Select(i => i.GetAttribute("data-value")).ToList();

    private void RegisterTheme(Action<string>? ran = null) => _service.Register(
        BitCommandPaletteCmd.New("Theme").Group("Preferences").Children(
            BitCommandPaletteCmd.Action("Light", () => ran?.Invoke("light")).Id("theme:light"),
            BitCommandPaletteCmd.Action("Dark", () => ran?.Invoke("dark")).Id("theme:dark")),
        BitCommandPaletteCmd.New("Home").Group("Navigation"));

    [TestMethod]
    public async Task A_nested_command_is_found_from_the_root_with_its_ancestors_under_their_group()
    {
        var ran = new List<string>();
        RegisterTheme(ran.Add);
        var cut = await OpenAsync("dark");

        var row = cut.Find("[data-value='theme:dark']");
        AssertSequence.AreEqual(["Theme"], row.QuerySelectorAll("[data-bit-cpl=path] .bit-cpl-path-segment").Select(s => s.TextContent));
        Assert.AreEqual("Preferences", cut.Find("[data-bit-cpl=group-heading]").TextContent.Trim());

        await Press(cut, "Enter");

        AssertSequence.AreEqual(["dark"], ran);
    }

    [TestMethod]
    public async Task An_option_that_goes_back_stays_on_the_root_when_chosen_from_it()
    {
        var chosen = new List<string>();
        _service.Register(BitCommandPaletteCmd.New("Theme").Children(
            BitCommandPaletteCmd.New("Dark").Id("theme:dark").Handler(_ =>
            {
                chosen.Add("dark");
                return Task.FromResult(BitCommandPaletteCommandResult.Back);
            })));
        var cut = await OpenAsync("dark");

        await Press(cut, "Enter");

        AssertSequence.AreEqual(["dark"], chosen);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
        Assert.AreEqual("dark", cut.Find("[data-bit-cpl=input]").GetAttribute("value"));
        AssertSequence.AreEqual(["theme:dark"], ItemIds(cut));
    }

    [TestMethod]
    public async Task The_ancestors_titles_are_searched_too()
    {
        RegisterTheme();
        var cut = await OpenAsync("theme li");

        AssertSequence.AreEqual(["theme:light"], ItemIds(cut));
    }

    [TestMethod]
    public async Task A_root_command_is_listed_before_a_nested_one_that_matches_as_well()
    {
        _service.Register(BitCommandPaletteCmd.New("Export"), BitCommandPaletteCmd.New("Data").Children(BitCommandPaletteCmd.New("Export").Id("data:export")));
        var cut = await OpenAsync("export");

        AssertSequence.AreEqual(["export", "data:export"], ItemIds(cut));
    }

    [TestMethod]
    public async Task Commands_under_a_hidden_disabled_or_multi_select_ancestor_are_not_found()
    {
        _service.Register(
            BitCommandPaletteCmd.New("Secret").VisibleWhen(() => false).Children(BitCommandPaletteCmd.New("Hidden child").Id("a")),
            BitCommandPaletteCmd.New("Locked").Disabled().Children(BitCommandPaletteCmd.New("Locked child").Id("b")),
            BitCommandPaletteCmd.New("Labels").MultiSelect().Children(BitCommandPaletteCmd.New("Label child").Id("c")),
            BitCommandPaletteCmd.New("Open").Children(BitCommandPaletteCmd.New("Open child").Id("d")));
        var cut = await OpenAsync("child");

        AssertSequence.AreEqual(["d"], ItemIds(cut));
    }

    [TestMethod]
    public async Task Nested_search_can_be_turned_off_and_modes_never_use_it()
    {
        RegisterTheme();
        _service.RegisterMode(new BitCommandPaletteCommandMode { Prefix = ">", Name = "Commands" });

        var off = await OpenAsync("dark", p => p.Add(x => x.SearchNestedCommands, false));
        Assert.IsEmpty(off.FindAll("[data-bit-cpl=item]"));
        await off.InvokeAsync(() => _service.CloseAsync());

        _options.SearchNestedCommands = true;
        var cut = await OpenAsync(">dark");
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=item]"));
    }

    [TestMethod]
    public async Task A_nested_page_opened_from_the_root_has_its_ancestors_behind_it()
    {
        _service.Register(BitCommandPaletteCmd.New("Settings").Children(BitCommandPaletteCmd.New("Editor").Children(BitCommandPaletteCmd.New("Font size"), BitCommandPaletteCmd.New("Tab width"))));
        var cut = await OpenAsync("editor");

        await Press(cut, "Enter");

        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings", "Editor"], cut.FindAll("[data-bit-cpl=crumb]").Select(c => c.TextContent.Trim())));
        Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]"));

        // Back goes up one level, not to the root.
        await Press(cut, "Escape");
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings"], cut.FindAll("[data-bit-cpl=crumb]").Select(c => c.TextContent.Trim())));
        await Press(cut, "Escape");
        cut.WaitForAssertion(() => Assert.AreEqual("editor", cut.Find("[data-bit-cpl=input]").GetAttribute("value")));
    }

    [TestMethod]
    public async Task Opening_on_a_nested_page_by_id_shows_the_whole_path()
    {
        _service.Register(BitCommandPaletteCmd.New("Settings").Children(BitCommandPaletteCmd.New("Editor").Children(BitCommandPaletteCmd.New("Font size"))));
        var cut = Context.Render<BitCommandPalette>();

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "settings.editor"));

        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings", "Editor"], cut.FindAll("[data-bit-cpl=crumb]").Select(c => c.TextContent.Trim())));
    }

    [TestMethod]
    public async Task A_recently_used_nested_command_is_listed_on_the_root_with_its_path()
    {
        RegisterTheme();
        var cut = await OpenAsync("dark");
        await Press(cut, "Enter");

        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() =>
        {
            var recent = cut.Find("[role=group]:first-child [data-value='theme:dark']");
            Assert.Contains("Theme", recent.QuerySelector("[data-bit-cpl=path]")!.TextContent);
        });
    }

    [TestMethod]
    public async Task A_pinned_command_is_listed_even_beyond_the_result_limit()
    {
        _service.Register(Enumerable.Range(0, 30).Select(i => BitCommandPaletteCmd.New($"Command {i:00}").Build()));
        await Services.GetRequiredService<BitCommandPaletteHistory>().TogglePinAsync("command-29");
        var cut = await OpenAsync(parameters: null);

        // Pinned first, although MaxResults leaves "Command 29" out of the ranking of an empty query.
        _options.MaxResults = 10;
        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() => Assert.AreEqual("command-29", ItemIds(cut)[0]));
    }

    [TestMethod]
    public async Task A_query_that_found_nothing_is_retried_allowing_a_typo_and_says_so()
    {
        _service.Register(BitCommandPaletteCmd.New("Settings"), BitCommandPaletteCmd.New("Sign out"), BitCommandPaletteCmd.New("Search docs"));
        var cut = await OpenAsync("settigns");

        AssertSequence.AreEqual(["settings"], ItemIds(cut));
        Assert.AreEqual("No exact matches. Showing close ones.", cut.Find("[data-bit-cpl=notice]").TextContent.Trim());
        Assert.AreEqual("No exact matches. Showing close ones. 1 result available", cut.Find("[aria-live=polite]").TextContent.Trim());
        Assert.IsNotEmpty(cut.FindAll("[data-bit-cpl=mark]"));

        // A query that works never gets the second chance.
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("sett"));
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=notice]"));
    }

    [TestMethod]
    public async Task Typo_tolerance_can_be_turned_off()
    {
        _service.Register(BitCommandPaletteCmd.New("Settings"));
        var cut = await OpenAsync("settigns", p => p.Add(x => x.TypoTolerance, false));

        Assert.IsNotNull(cut.Find("[data-bit-cpl=empty]"));
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=notice]"));
    }
}
