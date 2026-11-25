using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// The palette acts on a change of its Open parameter, never on its value alone: a parent that renders again while the
/// palette opened or closed itself (its shortcut, a trigger, the service) still passes the value it held before
/// OpenChanged reached it - a layout re-rendering on IBitCommandPaletteService.Opened does exactly that - and that value
/// must neither close what just opened nor reopen what the user just dismissed.
/// </summary>
[TestClass]
public class BitCommandPaletteOpenParameterTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitCommandPaletteServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var handle = Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBitCommandPaletteService>();
        _service.Register(BitCommandPaletteCmd.New("Alpha"));
    }

    private static bool IsOpen(IRenderedComponent<BitCommandPalette> cut) => cut.Find("dialog").GetAttribute("data-open") == "true";

    [TestMethod]
    public async Task A_parent_rendering_again_with_the_value_it_held_does_not_close_a_palette_that_opened_itself()
    {
        // A bound Open whose parent has not taken the palette's OpenChanged(true) in yet.
        var cut = Context.Render<BitCommandPalette>(p => p
            .Add(x => x.Open, false)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.IsTrue(IsOpen(cut)));

        // The parent renders again - a layout counting the openings - still passing false.
        cut.Render(p => p.Add(x => x.Open, false));

        Assert.IsTrue(IsOpen(cut), "A render that did not change Open closed the palette.");
        Assert.IsTrue(_service.IsOpen);
    }

    [TestMethod]
    public async Task A_changed_Open_still_opens_and_closes_the_palette()
    {
        var open = false;
        var cut = Context.Render<BitCommandPalette>(p => p
            .Add(x => x.Open, false)
            .Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, value => open = value)));

        cut.Render(p => p.Add(x => x.Open, true));
        cut.WaitForAssertion(() => Assert.IsTrue(IsOpen(cut)));
        Assert.IsTrue(open);

        cut.Render(p => p.Add(x => x.Open, false));
        cut.WaitForAssertion(() => Assert.IsFalse(IsOpen(cut)));
        Assert.IsFalse(open);

        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task A_parent_rendering_again_with_an_unbound_Open_does_not_reopen_a_dismissed_palette()
    {
        // Open set once, with no OpenChanged: the palette opens, and the user closes it.
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Open, true));
        cut.WaitForAssertion(() => Assert.IsTrue(IsOpen(cut)));

        await cut.InvokeAsync(() => _service.CloseAsync());
        cut.WaitForAssertion(() => Assert.IsFalse(IsOpen(cut)));

        cut.Render(p => p.Add(x => x.Open, true));

        Assert.IsFalse(IsOpen(cut), "A render that did not change Open reopened a palette the user dismissed.");
    }
}
