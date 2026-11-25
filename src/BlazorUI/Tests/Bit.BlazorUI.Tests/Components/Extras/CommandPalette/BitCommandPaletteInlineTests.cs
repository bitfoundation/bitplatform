using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// The palette rendered in the page (<see cref="BitCommandPalette.Inline"/>): always open, <c>Esc</c> clearing rather than closing,
/// and staying out of the modal palette's way - no toggle, no global shortcuts, no answer to the service's requests -
/// so a layout's modal palette and a page's inline one never both run a command.
/// </summary>
[TestClass]
public class BitCommandPaletteInlineTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitCommandPaletteServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBitCommandPaletteService>();
    }

    private static Task Press(IRenderedComponent<BitCommandPalette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance._OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public void An_inline_palette_is_a_named_region_of_the_page_that_is_always_open()
    {
        _service.Register(BitCommandPaletteCmd.New("Alpha"), BitCommandPaletteCmd.New("Beta"));

        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));

        Assert.IsEmpty(cut.FindAll("dialog"));
        var section = cut.Find("section[data-bit-cpl=inline]");
        Assert.AreEqual("Command palette", section.GetAttribute("aria-label"));
        Assert.Contains("bit-cpl-inl", section.ClassList);
        Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]"));
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=close]"));
        Assert.IsTrue(cut.Instance.IsOpen);
        Assert.IsFalse(_service.IsOpen);
    }

    [TestMethod]
    public void It_claims_no_toggle_and_no_global_shortcuts()
    {
        _service.Register(BitCommandPaletteCmd.New("Rename").Shortcut("F2"));

        Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == BitCommandPaletteTestJs.Setup).Arguments;
        Assert.IsEmpty((IEnumerable<string>)setup[2]!);
        Assert.IsEmpty((IEnumerable<string>)setup[3]!);
        Assert.AreEqual(true, setup[7]);
    }

    [TestMethod]
    public async Task Next_to_a_modal_palette_a_command_run_by_id_runs_once_and_only_the_modal_opens()
    {
        var runs = 0;
        _service.Register(BitCommandPaletteCmd.Action("Count", () => runs++).Id("count"));
        var modal = Context.Render<BitCommandPalette>();
        var inline = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));

        await modal.InvokeAsync(() => _service.ExecuteAsync("count"));
        await modal.InvokeAsync(() => _service.OpenAsync("count"));

        Assert.AreEqual(1, runs);
        Assert.AreEqual("true", modal.Find("dialog").GetAttribute("data-open"));
        Assert.AreEqual(string.Empty, inline.Find("[data-bit-cpl=input]").GetAttribute("value"));
    }

    [TestMethod]
    public async Task Escape_clears_the_query_and_goes_back_a_page_but_never_closes()
    {
        _service.Register(BitCommandPaletteCmd.Page("Theme", BitCommandPaletteCmd.New("Light"), BitCommandPaletteCmd.New("Dark")), BitCommandPaletteCmd.New("Other"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("theme"));
        await Press(cut, "Enter");
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-bit-cpl=crumb]")));

        await Press(cut, "Escape");
        cut.WaitForAssertion(() => Assert.IsEmpty(cut.FindAll("[data-bit-cpl=crumb]")));
        Assert.AreEqual("theme", cut.Find("[data-bit-cpl=input]").GetAttribute("value"));

        await Press(cut, "Escape");
        Assert.AreEqual(string.Empty, cut.Find("[data-bit-cpl=input]").GetAttribute("value"));

        await Press(cut, "Escape");
        Assert.IsTrue(cut.Instance.IsOpen);
        Assert.IsNotEmpty(cut.FindAll("[data-bit-cpl=item]"));
    }

    [TestMethod]
    public async Task A_command_that_closes_resets_it_to_the_root_page()
    {
        var ran = false;
        _service.Register(BitCommandPaletteCmd.Action("Export", () => ran = true), BitCommandPaletteCmd.New("Other"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));
        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("export"));

        await Press(cut, "Enter");

        Assert.IsTrue(ran);
        Assert.IsTrue(cut.Instance.IsOpen);
        Assert.AreEqual(string.Empty, cut.Find("[data-bit-cpl=input]").GetAttribute("value"));
        Assert.HasCount(2, cut.FindAll("[data-bit-cpl=item]"));
    }

    [TestMethod]
    public void Without_the_registry_it_lists_only_its_own_commands()
    {
        _service.Register(BitCommandPaletteCmd.New("Registered"));
        _service.RegisterMode(new BitCommandPaletteCommandMode { Prefix = ">", Name = "Commands" });

        var cut = Context.Render<BitCommandPalette>(p => p
            .Add(x => x.Inline, true)
            .Add(x => x.IncludeRegisteredCommands, false)
            .Add(x => x.Commands, new[] { BitCommandPaletteCmd.New("Word wrap").Build(), BitCommandPaletteCmd.New("Minimap").Build() }));

        AssertSequence.AreEqual(["Word wrap", "Minimap"], cut.FindAll("[data-bit-cpl=label]").Select(l => l.TextContent));
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=group-heading]"));
    }
}
