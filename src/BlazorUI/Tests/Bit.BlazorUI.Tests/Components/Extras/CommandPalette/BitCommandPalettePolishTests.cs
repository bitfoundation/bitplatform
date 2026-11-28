using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>Breadcrumbs that go back, shortcuts no page can have, and an inline palette that keeps its place when asked to.</summary>
[TestClass]
public class BitCommandPalettePolishTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;
    private BitCommandPaletteOptions _options = default!;
    private LogSink _log = default!;

    [TestInitialize]
    public void Initialize()
    {
        _log = LogSink.AddTo(Services);
        Services.AddBitBlazorUIExtrasServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBitCommandPaletteService>();
        _options = Services.GetRequiredService<BitCommandPaletteOptions>();
    }

    private static Task Press(IRenderedComponent<BitCommandPalette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance._OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public async Task An_ancestor_breadcrumb_goes_back_to_its_page()
    {
        _service.Register(BitCommandPaletteCmd.New("Settings").Children(BitCommandPaletteCmd.New("Editor").Children(BitCommandPaletteCmd.New("Fonts").Children(BitCommandPaletteCmd.New("Size")))));
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "settings.editor.fonts"));
        cut.WaitForAssertion(() => Assert.HasCount(3, cut.FindAll("[data-bit-cpl=crumb]")));

        var crumbs = cut.FindAll("[data-bit-cpl=crumb]");
        Assert.AreEqual("BUTTON", crumbs[0].TagName);
        Assert.AreEqual("SPAN", crumbs[2].TagName);
        Assert.AreEqual("page", crumbs[2].GetAttribute("aria-current"));

        crumbs[0].Click();

        cut.WaitForAssertion(() => AssertSequence.AreEqual(["Settings"], cut.FindAll("[data-bit-cpl=crumb]").Select(c => c.TextContent.Trim())));
        Assert.Contains("Editor", cut.Find("[role=listbox]").TextContent);
    }

    [TestMethod]
    public void A_shortcut_browsers_keep_for_themselves_is_reported()
    {
        _service.Register(BitCommandPaletteCmd.New("Close document").Shortcut("Mod+W"), BitCommandPaletteCmd.New("Save").Shortcut("Mod+S"));

        Context.Render<BitCommandPalette>();

        var warnings = _log.Messages(LogLevel.Warning).ToList();
        Assert.ContainsSingle(warnings.Where(m => m.Contains("'mod+w'") && m.Contains("browsers")));
        Assert.IsFalse(warnings.Any(m => m.Contains("'mod+s'")));
    }

    [TestMethod]
    public async Task An_inline_palette_keeps_its_place_after_a_command_when_ResetOnClose_is_off()
    {
        _options.ResetOnClose = false;
        var ran = 0;
        _service.Register(BitCommandPaletteCmd.Action("Export", () => ran++), BitCommandPaletteCmd.New("Other"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("export"));

        await Press(cut, "Enter");

        Assert.AreEqual(1, ran);
        Assert.AreEqual("export", cut.Find("[data-bit-cpl=input]").GetAttribute("value"));
        Assert.ContainsSingle(cut.FindAll("[data-bit-cpl=item]"));
    }
}
