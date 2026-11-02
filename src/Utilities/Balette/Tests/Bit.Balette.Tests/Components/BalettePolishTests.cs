using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Balette.Tests;

/// <summary>Breadcrumbs that go back, shortcuts no page can have, and an inline palette that keeps its place when asked to.</summary>
[TestClass]
public class BalettePolishTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BaletteOptions _options;
    private readonly LogSink _log;

    public BalettePolishTests()
    {
        _log = LogSink.AddTo(Services);
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js").SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBaletteService>();
        _options = Services.GetRequiredService<BaletteOptions>();
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public async Task An_ancestor_breadcrumb_goes_back_to_its_page()
    {
        _service.Register(BaletteCmd.New("Settings").Children(BaletteCmd.New("Editor").Children(BaletteCmd.New("Fonts").Children(BaletteCmd.New("Size")))));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "settings.editor.fonts"));
        cut.WaitForAssertion(() => Assert.HasCount(3, cut.FindAll("[data-balette=crumb]")));

        var crumbs = cut.FindAll("[data-balette=crumb]");
        Assert.AreEqual("BUTTON", crumbs[0].TagName);
        Assert.AreEqual("SPAN", crumbs[2].TagName);
        Assert.AreEqual("page", crumbs[2].GetAttribute("aria-current"));

        crumbs[0].Click();

        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings"], cut.FindAll("[data-balette=crumb]").Select(c => c.TextContent.Trim())));
        Assert.Contains("Editor", cut.Find("[role=listbox]").TextContent);
    }

    [TestMethod]
    public void A_shortcut_browsers_keep_for_themselves_is_reported()
    {
        _service.Register(BaletteCmd.New("Close document").Shortcut("Mod+W"), BaletteCmd.New("Save").Shortcut("Mod+S"));

        Render<Balette>();

        var warnings = _log.Messages(LogLevel.Warning).ToList();
        Assert.ContainsSingle(warnings.Where(m => m.Contains("'mod+w'") && m.Contains("browsers")));
        Assert.IsFalse(warnings.Any(m => m.Contains("'mod+s'")));
    }

    [TestMethod]
    public async Task An_inline_palette_keeps_its_place_after_a_command_when_ResetOnClose_is_off()
    {
        _options.ResetOnClose = false;
        var ran = 0;
        _service.Register(BaletteCmd.Action("Export", () => ran++), BaletteCmd.New("Other"));
        var cut = Render<Balette>(p => p.Add(x => x.Inline, true));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("export"));

        await Press(cut, "Enter");

        Assert.AreEqual(1, ran);
        Assert.AreEqual("export", cut.Find("[data-balette=input]").GetAttribute("value"));
        Assert.ContainsSingle(cut.FindAll("[data-balette=item]"));
    }
}
