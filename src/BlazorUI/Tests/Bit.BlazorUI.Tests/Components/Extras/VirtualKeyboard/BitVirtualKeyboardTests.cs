using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

[TestClass]
public class BitVirtualKeyboardTests : BunitTestContext
{
    private static BitVirtualKeyboardLayout Small() => new()
    {
        Id = "small",
        Name = "Small",
        Layers =
        [
            new() { Id = "base", Rows = ["q w e {bksp}", "{shift} {gap} {blank} {zwnj} \\u{64E}", "{sym} {space} {hide} {lang} {enter}"] },
            new() { Id = "symbols", Rows = ["1 2 3 {abc}"] },
        ],
    };

    [TestMethod]
    public void TheRootShouldBeANamedGroupWithTheRootClass()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Form, BitVirtualKeyboardForm.Full));

        var root = component.Find(".bit-vkb");

        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("virtual keyboard", root.GetAttribute("aria-roledescription"));
        Assert.AreEqual("English (US)", root.GetAttribute("aria-label"));
        Assert.AreEqual("claim", root.GetAttribute("data-bit-esc"), "a keyboard typing into its own value acts on Escape");
    }

    [TestMethod]
    public void EveryLayerShouldBeRenderedWithTheFirstMarkedAsShownBeforeTheScript()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()));

        var layers = component.FindAll(".bit-vkb-lyr");

        CollectionAssert.AreEqual(new[] { "base", "symbols", "shift" }, layers.Select(l => l.GetAttribute("data-bit-vkb-layer")).ToArray(), "a Shift key derives a shift layer");
        Assert.IsTrue(layers[0].ClassList.Contains("bit-vkb-lyb"));
        Assert.IsFalse(layers[1].ClassList.Contains("bit-vkb-lyb"));
        Assert.IsFalse(layers.Any(l => l.HasAttribute("data-bit-vkb-on")), "which layer shows is the script's");
        Assert.AreEqual("group", layers[0].GetAttribute("role"));
        Assert.AreEqual("Letters", layers[0].GetAttribute("aria-label"));
        Assert.AreEqual("Symbols", layers[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void EveryKeyButGapsAndBlanksShouldBeANamedButton()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()));

        var buttons = component.FindAll("button.bit-vkb-key");

        Assert.IsTrue(buttons.All(b => b.GetAttribute("type") == "button"));
        Assert.IsTrue(buttons.All(b => string.IsNullOrEmpty(b.GetAttribute("aria-label")) is false));
        Assert.AreEqual(1, component.FindAll("[data-bit-vkb-layer=base] > span.bit-vkb-gap[aria-hidden=true]").Count);
        Assert.AreEqual(1, component.FindAll("[data-bit-vkb-layer=base] > span.bit-vkb-blk[aria-hidden=true]").Count);

        string Name(string selector) => component.Find(selector).GetAttribute("aria-label")!;
        var keys = component.FindAll("[data-bit-vkb-layer=base] button").ToList();

        Assert.AreEqual("Backspace", keys[3].GetAttribute("aria-label"));
        Assert.AreEqual("Shift", keys[4].GetAttribute("aria-label"));
        Assert.AreEqual("ZWNJ, zero-width non-joiner", keys[5].GetAttribute("aria-label"), "the name holds the visible text");
        Assert.AreEqual("?123, Symbols", keys[7].GetAttribute("aria-label"));
        Assert.AreEqual("Space", keys[8].GetAttribute("aria-label"));
        Assert.AreEqual("ABC, Letters", Name("[data-bit-vkb-layer=symbols] button:last-child"));
    }

    [TestMethod]
    public void ModifierKeysShouldBeTogglesAndTheOthersNot()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()));

        var shift = component.FindAll("[data-bit-vkb-layer=base] > button").Single(b => b.GetAttribute("aria-label") == "Shift");

        Assert.AreEqual("false", shift.GetAttribute("aria-pressed"));
        Assert.IsTrue(shift.ClassList.Contains("bit-vkb-mod"));
        Assert.AreEqual(1, component.FindAll("[data-bit-vkb-layer=base] > button[aria-pressed]").Count);
    }

    [TestMethod]
    public void LabelsShouldBeIsolatedAndALoneMarkDrawnOnADottedCircle()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()));

        var labels = component.FindAll(".bit-vkb-lbl > bdi").Select(b => b.TextContent).ToList();

        CollectionAssert.Contains(labels, "q");
        CollectionAssert.Contains(labels, "\U000025CC\U0000064E");
    }

    [TestMethod]
    public void TheHideAndLanguageKeysShouldOnlyBeThereWhereTheyAct()
    {
        var inline = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()));
        Assert.IsFalse(inline.FindAll("button").Any(b => b.GetAttribute("aria-label") == "Hide keyboard"));
        Assert.IsFalse(inline.FindAll("button").Any(b => b.GetAttribute("aria-label")!.Contains("Next language")));

        var docked = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small())
                                                              .Add(c => c.Mode, BitVirtualKeyboardMode.Docked)
                                                              .Add(c => c.Languages, ["en", "fa"]));
        Assert.IsTrue(docked.FindAll("button").Any(b => b.GetAttribute("aria-label") == "Hide keyboard"));
        Assert.IsTrue(docked.FindAll("button").Any(b => b.GetAttribute("aria-label")!.Contains("Next language")));
        Assert.IsTrue(docked.Find(".bit-vkb").ClassList.Contains("bit-vkb-dck"));
        Assert.AreEqual(1, docked.FindAll(".bit-vkb-rsv").Count);
    }

    [TestMethod]
    [DataRow("fa-IR")]
    [DataRow("ar-SA")]
    [DataRow("de-DE")]
    public void TheGridShouldBeWrittenTheSameInEveryCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

            var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()));

            var first = component.Find("[data-bit-vkb-layer=base] button");
            StringAssert.Matches(first.GetAttribute("style"), new System.Text.RegularExpressions.Regex("^grid-area:1 / [0-9]+ / span 1 / span 4$"));
            StringAssert.Contains(component.Find(".bit-vkb-lyr").GetAttribute("style"), "grid-template-columns:repeat(");
            Assert.AreEqual("0", first.GetAttribute("data-bit-vkb-k"));
            Assert.AreEqual("0", first.GetAttribute("tabindex"));
            Assert.AreEqual("-1", component.FindAll("[data-bit-vkb-layer=base] button")[1].GetAttribute("tabindex"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void ADisabledKeyboardShouldDisableEveryKey()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()).Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-vkb").ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.FindAll("button").All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));
        Assert.IsTrue(component.FindAll("button").All(b => b.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void TypingIntoAnInputShouldKeepEveryKeyOutOfTheTabOrderAndClaimNoEscapeOnTheRoot()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small()).Add(c => c.FollowFocus, true));

        Assert.IsTrue(component.FindAll("button").All(b => b.GetAttribute("tabindex") == "-1"));
        Assert.IsFalse(component.Find(".bit-vkb").HasAttribute("data-bit-esc"));
    }

    [TestMethod]
    public void TheLanguageShouldFollowTheFormAndTheCulture()
    {
        var full = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Form, BitVirtualKeyboardForm.Full));
        Assert.AreEqual("en", full.Instance.ActiveLayout!.Id);

        var compact = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Form, BitVirtualKeyboardForm.Compact));
        Assert.AreEqual("en-compact", compact.Instance.ActiveLayout!.Id);

        var auto = RenderComponent<BitVirtualKeyboard>();
        Assert.AreEqual("en-compact", auto.Instance.ActiveLayout!.Id, "Auto is Compact until the script measures the keyboard");
    }

    [TestMethod]
    public void AKeyTemplateShouldRenderTheContentOfEveryKey()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small())
                                                                  .Add(c => c.KeyTemplate, context => $"[{context.Label}]"));

        Assert.AreEqual("[q]", component.Find("[data-bit-vkb-layer=base] button").TextContent);
    }

    [TestMethod]
    public void ClassesAndStylesShouldReachTheirParts()
    {
        var component = RenderComponent<BitVirtualKeyboard>(p => p.Add(c => c.Layout, Small())
            .Add(c => c.Classes, new() { Root = "r", Panel = "p", Layer = "l", Key = "k", FunctionKey = "f", PrimaryKey = "e", Gap = "g", LiveRegion = "v" })
            .Add(c => c.Styles, new() { Key = "color:red", Popup = "color:blue" }));

        Assert.IsTrue(component.Find(".bit-vkb").ClassList.Contains("r"));
        Assert.IsTrue(component.Find(".bit-vkb-pnl").ClassList.Contains("p"));
        Assert.IsTrue(component.Find(".bit-vkb-lyr").ClassList.Contains("l"));
        Assert.IsTrue(component.Find("button.bit-vkb-key").ClassList.Contains("k"));
        Assert.IsTrue(component.Find("button.bit-vkb-fnc").ClassList.Contains("f"));
        Assert.IsTrue(component.Find("button.bit-vkb-pri").ClassList.Contains("e"));
        Assert.IsTrue(component.Find(".bit-vkb-gap").ClassList.Contains("g"));
        Assert.IsTrue(component.Find(".bit-vkb-liv").ClassList.Contains("v"));
        StringAssert.Contains(component.Find("button.bit-vkb-key").GetAttribute("style"), "color:red");
        Assert.AreEqual("color:blue", component.Find(".bit-vkb-pop").GetAttribute("style"));
    }
}