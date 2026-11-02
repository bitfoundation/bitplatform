using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>Search across pages - nested commands found from the root, with their ancestors - and the second chance a query that found nothing gets.</summary>
[TestClass]
public class BaletteSearchFeatureTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BaletteOptions _options;

    public BaletteSearchFeatureTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        module.SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);

        _service = Services.GetRequiredService<IBaletteService>();
        _options = Services.GetRequiredService<BaletteOptions>();
    }

    private async Task<IRenderedComponent<Balette>> OpenAsync(string? query = null, Action<ComponentParameterCollectionBuilder<Balette>>? parameters = null)
    {
        var cut = parameters is null ? Render<Balette>() : Render<Balette>(parameters);
        await cut.InvokeAsync(() => _service.OpenAsync(query));
        return cut;
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, false, false, false, false));

    private static List<string?> ItemIds(IRenderedComponent<Balette> cut)
        => cut.FindAll("[data-balette=item]").Select(i => i.GetAttribute("data-value")).ToList();

    private void RegisterTheme(Action<string>? ran = null) => _service.Register(
        BaletteCmd.New("Theme").Group("Preferences").Children(
            BaletteCmd.Action("Light", () => ran?.Invoke("light")).Id("theme:light"),
            BaletteCmd.Action("Dark", () => ran?.Invoke("dark")).Id("theme:dark")),
        BaletteCmd.New("Home").Group("Navigation"));

    [TestMethod]
    public async Task A_nested_command_is_found_from_the_root_with_its_ancestors_under_their_group()
    {
        var ran = new List<string>();
        RegisterTheme(ran.Add);
        var cut = await OpenAsync("dark");

        var row = cut.Find("[data-value='theme:dark']");
        AssertSequence.AreEqual(["Theme"], row.QuerySelectorAll("[data-balette=path] .bit-balette-path-segment").Select(s => s.TextContent));
        Assert.AreEqual("Preferences", cut.Find("[data-balette=group-heading]").TextContent.Trim());

        await Press(cut, "Enter");

        AssertSequence.AreEqual(["dark"], ran);
    }

    [TestMethod]
    public async Task An_option_that_goes_back_stays_on_the_root_when_chosen_from_it()
    {
        var chosen = new List<string>();
        _service.Register(BaletteCmd.New("Theme").Children(
            BaletteCmd.New("Dark").Id("theme:dark").Handler(_ =>
            {
                chosen.Add("dark");
                return Task.FromResult(BaletteCommandResult.Back);
            })));
        var cut = await OpenAsync("dark");

        await Press(cut, "Enter");

        AssertSequence.AreEqual(["dark"], chosen);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
        Assert.AreEqual("dark", cut.Find("[data-balette=input]").GetAttribute("value"));
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
        _service.Register(BaletteCmd.New("Export"), BaletteCmd.New("Data").Children(BaletteCmd.New("Export").Id("data:export")));
        var cut = await OpenAsync("export");

        AssertSequence.AreEqual(["export", "data:export"], ItemIds(cut));
    }

    [TestMethod]
    public async Task Commands_under_a_hidden_disabled_or_multi_select_ancestor_are_not_found()
    {
        _service.Register(
            BaletteCmd.New("Secret").VisibleWhen(() => false).Children(BaletteCmd.New("Hidden child").Id("a")),
            BaletteCmd.New("Locked").Disabled().Children(BaletteCmd.New("Locked child").Id("b")),
            BaletteCmd.New("Labels").MultiSelect().Children(BaletteCmd.New("Label child").Id("c")),
            BaletteCmd.New("Open").Children(BaletteCmd.New("Open child").Id("d")));
        var cut = await OpenAsync("child");

        AssertSequence.AreEqual(["d"], ItemIds(cut));
    }

    [TestMethod]
    public async Task Nested_search_can_be_turned_off_and_modes_never_use_it()
    {
        RegisterTheme();
        _service.RegisterMode(new BaletteCommandMode { Prefix = ">", Name = "Commands" });

        var off = await OpenAsync("dark", p => p.Add(x => x.SearchNestedCommands, false));
        Assert.IsEmpty(off.FindAll("[data-balette=item]"));
        await off.InvokeAsync(() => _service.CloseAsync());

        _options.SearchNestedCommands = true;
        var cut = await OpenAsync(">dark");
        Assert.IsEmpty(cut.FindAll("[data-balette=item]"));
    }

    [TestMethod]
    public async Task A_nested_page_opened_from_the_root_has_its_ancestors_behind_it()
    {
        _service.Register(BaletteCmd.New("Settings").Children(BaletteCmd.New("Editor").Children(BaletteCmd.New("Font size"), BaletteCmd.New("Tab width"))));
        var cut = await OpenAsync("editor");

        await Press(cut, "Enter");

        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings", "Editor"], cut.FindAll("[data-balette=crumb]").Select(c => c.TextContent.Trim())));
        Assert.HasCount(2, cut.FindAll("[data-balette=item]"));

        // Back goes up one level, not to the root.
        await Press(cut, "Escape");
        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings"], cut.FindAll("[data-balette=crumb]").Select(c => c.TextContent.Trim())));
        await Press(cut, "Escape");
        cut.WaitForAssertion(() => Assert.AreEqual("editor", cut.Find("[data-balette=input]").GetAttribute("value")));
    }

    [TestMethod]
    public async Task Opening_on_a_nested_page_by_id_shows_the_whole_path()
    {
        _service.Register(BaletteCmd.New("Settings").Children(BaletteCmd.New("Editor").Children(BaletteCmd.New("Font size"))));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "settings.editor"));

        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings", "Editor"], cut.FindAll("[data-balette=crumb]").Select(c => c.TextContent.Trim())));
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
            Assert.Contains("Theme", recent.QuerySelector("[data-balette=path]")!.TextContent);
        });
    }

    [TestMethod]
    public async Task A_pinned_command_is_listed_even_beyond_the_result_limit()
    {
        _service.Register(Enumerable.Range(0, 30).Select(i => BaletteCmd.New($"Command {i:00}").Build()));
        await Services.GetRequiredService<BaletteHistory>().TogglePinAsync("command-29");
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
        _service.Register(BaletteCmd.New("Settings"), BaletteCmd.New("Sign out"), BaletteCmd.New("Search docs"));
        var cut = await OpenAsync("settigns");

        AssertSequence.AreEqual(["settings"], ItemIds(cut));
        Assert.AreEqual("No exact matches. Showing close ones.", cut.Find("[data-balette=notice]").TextContent.Trim());
        Assert.AreEqual("No exact matches. Showing close ones. 1 result available", cut.Find("[aria-live=polite]").TextContent.Trim());
        Assert.IsNotEmpty(cut.FindAll("[data-balette=mark]"));

        // A query that works never gets the second chance.
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("sett"));
        Assert.IsEmpty(cut.FindAll("[data-balette=notice]"));
    }

    [TestMethod]
    public async Task Typo_tolerance_can_be_turned_off()
    {
        _service.Register(BaletteCmd.New("Settings"));
        var cut = await OpenAsync("settigns", p => p.Add(x => x.TypoTolerance, false));

        Assert.IsNotNull(cut.Find("[data-balette=empty]"));
        Assert.IsEmpty(cut.FindAll("[data-balette=notice]"));
    }
}
