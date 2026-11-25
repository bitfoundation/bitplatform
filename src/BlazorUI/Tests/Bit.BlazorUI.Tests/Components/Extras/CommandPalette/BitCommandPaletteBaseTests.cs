using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// What the palette takes from <see cref="BitComponentBase"/> as a component of the library: its root class and id, the
/// ids of its rows and list, the page's scroll held while the modal palette is open, and a Disabled palette that neither
/// opens nor claims a key.
/// </summary>
[TestClass]
public class BitCommandPaletteBaseTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;
    private BunitJSModuleInterop _jsHandle = default!;

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitCommandPaletteServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        _jsHandle = Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        _jsHandle.Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBitCommandPaletteService>();
    }

    [TestMethod]
    public async Task The_root_carries_the_class_and_the_id_its_rows_and_list_are_named_after()
    {
        _service.Register(BitCommandPaletteCmd.New("Alpha"), BitCommandPaletteCmd.New("Beta"));
        var cut = Context.Render<BitCommandPalette>(p => p
            .Add(x => x.Id, "cmds")
            .Add(x => x.Class, "mine")
            .Add(x => x.HtmlAttributes, new Dictionary<string, object> { ["data-test"] = "palette" }));

        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() =>
        {
            var dialog = cut.Find("dialog");
            Assert.AreEqual("cmds", dialog.GetAttribute("id"));
            Assert.Contains("bit-cpl", dialog.ClassList);
            Assert.Contains("mine", dialog.ClassList);
            Assert.DoesNotContain("bit-cpl-inl", dialog.ClassList);
            Assert.AreEqual("palette", dialog.GetAttribute("data-test"));

            Assert.AreEqual("cmds-list", cut.Find("[data-bit-cpl=list]").GetAttribute("id"));
            Assert.AreEqual("cmds-list", cut.Find("[data-bit-cpl=input]").GetAttribute("aria-controls"));
            var rows = cut.FindAll("[data-bit-cpl=item]");
            Assert.HasCount(2, rows);
            Assert.IsTrue(rows.All(r => r.Id!.StartsWith("cmds-i-", StringComparison.Ordinal)));
            Assert.AreEqual(rows[0].Id, cut.Find("[data-bit-cpl=input]").GetAttribute("aria-activedescendant"));
        });
    }

    [TestMethod]
    public void Without_an_Id_the_root_gets_a_unique_one()
    {
        var first = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));
        var second = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));

        var a = first.Find("section").GetAttribute("id");
        var b = second.Find("section").GetAttribute("id");
        Assert.IsFalse(string.IsNullOrEmpty(a));
        Assert.AreNotEqual(a, b);
        Assert.Contains("bit-cpl", first.Find("section").ClassList);
        Assert.Contains("bit-cpl-inl", first.Find("section").ClassList);
    }

    [TestMethod]
    public async Task The_modal_palette_holds_the_page_still_while_it_is_open()
    {
        _service.Register(BitCommandPaletteCmd.New("Alpha"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Id, "cmds"));

        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.AreEqual("cmds", Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.lockScroll").Arguments[0]));
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.unlockScroll");

        await cut.InvokeAsync(() => _service.CloseAsync());
        cut.WaitForAssertion(() => Assert.AreEqual("cmds", Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.unlockScroll").Arguments[0]));
    }

    [TestMethod]
    public async Task An_inline_palette_never_holds_the_page_still()
    {
        _service.Register(BitCommandPaletteCmd.New("Alpha"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Inline, true));

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("al"));

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.lockScroll");
    }

    [TestMethod]
    public async Task A_disabled_palette_neither_opens_nor_claims_a_key()
    {
        _service.Register(BitCommandPaletteCmd.New("Rename").Shortcut("F2"));
        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Disabled, true));

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == BitCommandPaletteTestJs.Setup).Arguments;
        Assert.IsEmpty((IEnumerable<string>)setup[2]!);
        Assert.IsEmpty((IEnumerable<string>)setup[3]!);

        await cut.InvokeAsync(() => _service.OpenAsync());
        await cut.InvokeAsync(() => cut.Instance._OnToggleShortcut());

        var dialog = cut.Find("dialog");
        Assert.AreEqual("false", dialog.GetAttribute("data-open"));
        Assert.Contains("bit-dis", dialog.ClassList);
        Assert.IsFalse(cut.Instance.IsOpen);
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Utils.lockScroll");
    }
}
