using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// The palette rendered in the page (<see cref="Balette.Inline"/>): always open, <c>Esc</c> clearing rather than closing,
/// and staying out of the modal palette's way - no toggle, no global shortcuts, no answer to the service's requests -
/// so a layout's modal palette and a page's inline one never both run a command.
/// </summary>
[TestClass]
public class BaletteInlineTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BunitJSModuleInterop _module;

    public BaletteInlineTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        _module.SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBaletteService>();
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public void An_inline_palette_is_a_named_region_of_the_page_that_is_always_open()
    {
        _service.Register(BaletteCmd.New("Alpha"), BaletteCmd.New("Beta"));

        var cut = Render<Balette>(p => p.Add(x => x.Inline, true));

        Assert.IsEmpty(cut.FindAll("dialog"));
        var section = cut.Find("section[data-balette=inline]");
        Assert.AreEqual("Command palette", section.GetAttribute("aria-label"));
        Assert.Contains("bit-balette-inline", section.ClassList);
        Assert.HasCount(2, cut.FindAll("[data-balette=item]"));
        Assert.IsEmpty(cut.FindAll("[data-balette=close]"));
        Assert.IsTrue(cut.Instance.IsOpen);
        Assert.IsFalse(_service.IsOpen);
    }

    [TestMethod]
    public void It_claims_no_toggle_and_no_global_shortcuts()
    {
        _service.Register(BaletteCmd.New("Rename").Shortcut("F2"));

        Render<Balette>(p => p.Add(x => x.Inline, true));

        var init = _module.Invocations.Single(i => i.Identifier == "init").Arguments;
        Assert.IsEmpty((IEnumerable<string>)init[2]!);
        Assert.IsEmpty((IEnumerable<string>)init[3]!);
        Assert.AreEqual(true, init[7]);
    }

    [TestMethod]
    public async Task Next_to_a_modal_palette_a_command_run_by_id_runs_once_and_only_the_modal_opens()
    {
        var runs = 0;
        _service.Register(BaletteCmd.Action("Count", () => runs++).Id("count"));
        var modal = Render<Balette>();
        var inline = Render<Balette>(p => p.Add(x => x.Inline, true));

        await modal.InvokeAsync(() => _service.ExecuteAsync("count"));
        await modal.InvokeAsync(() => _service.OpenAsync("count"));

        Assert.AreEqual(1, runs);
        Assert.AreEqual("true", modal.Find("dialog").GetAttribute("data-open"));
        Assert.AreEqual(string.Empty, inline.Find("[data-balette=input]").GetAttribute("value"));
    }

    [TestMethod]
    public async Task Escape_clears_the_query_and_goes_back_a_page_but_never_closes()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light"), BaletteCmd.New("Dark")), BaletteCmd.New("Other"));
        var cut = Render<Balette>(p => p.Add(x => x.Inline, true));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("theme"));
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=crumb]")));

        await Press(cut, "Escape");
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("[data-balette=crumb]")));
        Assert.AreEqual("theme", cut.Find("[data-balette=input]").GetAttribute("value"));

        await Press(cut, "Escape");
        Assert.AreEqual(string.Empty, cut.Find("[data-balette=input]").GetAttribute("value"));

        await Press(cut, "Escape");
        Assert.IsTrue(cut.Instance.IsOpen);
        Assert.IsNotEmpty(cut.FindAll("[data-balette=item]"));
    }

    [TestMethod]
    public async Task A_command_that_closes_resets_it_to_the_root_page()
    {
        var ran = false;
        _service.Register(BaletteCmd.Action("Export", () => ran = true), BaletteCmd.New("Other"));
        var cut = Render<Balette>(p => p.Add(x => x.Inline, true));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("export"));

        await Press(cut, "Enter");

        Assert.IsTrue(ran);
        Assert.IsTrue(cut.Instance.IsOpen);
        Assert.AreEqual(string.Empty, cut.Find("[data-balette=input]").GetAttribute("value"));
        Assert.HasCount(2, cut.FindAll("[data-balette=item]"));
    }

    [TestMethod]
    public void Without_the_registry_it_lists_only_its_own_commands()
    {
        _service.Register(BaletteCmd.New("Registered"));
        _service.RegisterMode(new BaletteCommandMode { Prefix = ">", Name = "Commands" });

        var cut = Render<Balette>(p => p
            .Add(x => x.Inline, true)
            .Add(x => x.IncludeRegisteredCommands, false)
            .Add(x => x.Commands, new[] { BaletteCmd.New("Word wrap").Build(), BaletteCmd.New("Minimap").Build() }));

        AssertSequence.AreEqual(["Word wrap", "Minimap"], cut.FindAll("[data-balette=label]").Select(l => l.TextContent));
        Assert.IsEmpty(cut.FindAll("[data-balette=group-heading]"));
    }

    [TestMethod]
    public void Density_is_exposed_to_the_stylesheet()
    {
        var compact = Render<Balette>(p => p.Add(x => x.Density, "compact"));
        Assert.AreEqual("compact", compact.Find("dialog").GetAttribute("data-density"));

        var byDefault = Render<Balette>();
        Assert.AreEqual("comfortable", byDefault.Find("dialog").GetAttribute("data-density"));
    }
}
