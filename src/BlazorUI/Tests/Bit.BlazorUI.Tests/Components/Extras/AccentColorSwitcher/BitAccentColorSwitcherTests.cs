using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

[TestClass]
public class BitAccentColorSwitcherTests : BunitTestContext
{
    private void RegisterServices()
    {
        // The switcher resolves the scoped BitAccentColorService, which itself needs the core
        // theme services - the same registration an app gets from AddBitBlazorUIExtrasServices.
        Context.Services.AddBitBlazorUIExtrasServices();
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRenderTheSixDefaultSwatches()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>();

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.AreEqual(BitAccentColorSwitcher.DefaultAccents.Count, swatches.Count);

        // Blue is the packaged palette's own primary, so it must render as the active swatch on a
        // fresh visit - a switcher with nothing marked would read as "no accent chosen yet".
        var active = component.FindAll(".bit-acs-act");
        Assert.AreEqual(1, active.Count);
        Assert.AreEqual("Blue", active[0].GetAttribute("title"));
        Assert.AreEqual("true", active[0].GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRenderCustomAccents()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Accents, new[]
            {
                new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" },
                new BitAccentColorItem { Name = "Indigo", Color = "#4B0082" },
            });
        });

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.AreEqual(2, swatches.Count);
        Assert.AreEqual("Crimson", swatches[0].GetAttribute("title"));
        StringAssert.Contains(swatches[0].GetAttribute("style"), "--bit-acs-clr:#DC143C", System.StringComparison.Ordinal,
            "The swatch paints itself through the --bit-acs-clr custom property.");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitAccentColorSwitcherShouldRespectIsEnabled(bool isEnabled)
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-acs");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.FindAll(".bit-acs-swt").All(s => s.HasAttribute("disabled") == (isEnabled is false)),
            "Disabled must reach the buttons themselves, not just the styling, so the swatches drop out of interaction.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldApplyClassesAndStyles()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-root");
            parameters.Add(p => p.Classes, new BitAccentColorSwitcherClassStyles { Swatch = "custom-swatch", ActiveSwatch = "custom-active" });
            parameters.Add(p => p.Styles, new BitAccentColorSwitcherClassStyles { Swatch = "outline-width:3px" });
        });

        var root = component.Find(".bit-acs");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));

        var swatches = component.FindAll(".bit-acs-swt");
        Assert.IsTrue(swatches.All(s => s.ClassList.Contains("custom-swatch")));
        StringAssert.Contains(swatches[0].GetAttribute("style"), "outline-width:3px", System.StringComparison.Ordinal);

        // ActiveSwatch composes on top of Swatch for the active one only.
        var active = component.Find(".bit-acs-act");
        Assert.IsTrue(active.ClassList.Contains("custom-active"));
        Assert.AreEqual(1, swatches.Count(s => s.ClassList.Contains("custom-active")));
    }

    [TestMethod]
    public void BitAccentColorSwitcherWithACssStrategyShouldMarkTheActiveSwatchThroughTheRootAttribute()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.FirstPaintStrategy, BitAccentColorFirstPaintStrategy.StaticCss);
            parameters.Add(p => p.Persistence, BitAccentColorPersistence.All);
        });

        // The built-in ring must not come from the C# state: prerendered markup renders before the
        // accent is restored, so a class-driven ring would mark the default swatch and visibly jump
        // to the visitor's accent at hydration. The emitted rules key on the bit-accent root
        // attribute instead, which the inline head script sets before first paint.
        Assert.AreEqual(0, component.FindAll(".bit-acs-act").Count,
            "With a CSS strategy the built-in ring is attribute-driven, so the class must not render.");

        var purpleToken = BitAccentColorPresets.Purple.TrimStart('#').ToLowerInvariant();
        var purple = component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Purple");
        Assert.AreEqual(purpleToken, purple.GetAttribute("bit-accent-swatch"),
            "Each swatch must carry its token for the attribute-keyed rules to target.");

        var style = component.Find("style").TextContent;
        StringAssert.Contains(style, $":root[{BitAccentColorNames.Attribute}=\"{purpleToken}\"]", System.StringComparison.Ordinal);
        StringAssert.Contains(style, $"[bit-accent-swatch=\"{purpleToken}\"]", System.StringComparison.Ordinal);

        var blueToken = BitAccentColorPresets.Blue.TrimStart('#').ToLowerInvariant();
        StringAssert.Contains(style, $":root:not([{BitAccentColorNames.Attribute}]) ", System.StringComparison.Ordinal);
        StringAssert.Contains(style, $"[bit-accent-swatch=\"{blueToken}\"]", System.StringComparison.Ordinal,
            "With no bit-accent attribute set (no override), the packaged primary's swatch must ring.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldApplyWithTheConfiguredStrategyAndPersistence()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Persistence, BitAccentColorPersistence.LocalStorage);
        });

        component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Purple").Click();

        var apply = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.AccentColor.apply");
        Assert.AreEqual("8764b8", apply.Arguments[0]);
        Assert.IsNull(apply.Arguments[1], "The default None strategy keeps no palette snapshot.");
        Assert.AreEqual(false, apply.Arguments[3], "The default None strategy sets no bit-accent attribute.");
        Assert.AreEqual((int)BitAccentColorPersistence.LocalStorage, apply.Arguments[4],
            "The persistence flags must reach the JS side - they decide which stores are written and which are cleaned up.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRaiseOnChangeWithTheClickedColor()
    {
        RegisterServices();

        string? changed = null;

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string color) => changed = color);
        });

        var purple = component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Purple");
        purple.Click();

        Assert.AreEqual(BitAccentColorPresets.Purple, changed);

        // The service is the single owner of the accent, so the click must also move the active
        // marking - that is what keeps multiple switcher instances in sync.
        var active = component.Find(".bit-acs-act");
        Assert.AreEqual("Purple", active.GetAttribute("title"));
    }
}
