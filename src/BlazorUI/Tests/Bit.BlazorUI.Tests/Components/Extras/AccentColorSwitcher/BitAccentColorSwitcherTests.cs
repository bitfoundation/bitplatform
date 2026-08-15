using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

[TestClass]
public class BitAccentColorSwitcherTests : BunitTestContext
{
    private void RegisterServices(System.Action<BitAccentColorConfig>? accentColor = null)
    {
        // The switcher resolves the scoped BitAccentColorService, which itself needs the core
        // theme services - the same registration an app gets from AddBitBlazorUIExtrasServices.
        Context.Services.AddBitBlazorUIExtrasServices(accentColor: accentColor);
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
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Accents =
                [
                    new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" },
                    new BitAccentColorItem { Name = "Indigo", Color = "#4B0082" },
                ]
            });
        });

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.AreEqual(2, swatches.Count);
        Assert.AreEqual("Crimson", swatches[0].GetAttribute("title"));
        StringAssert.Contains(swatches[0].GetAttribute("style"), "--bit-acs-clr:#DC143C", System.StringComparison.Ordinal,
            "The swatch paints itself through the --bit-acs-clr custom property.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldPaintAnAccentSpelledWithoutTheHash()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Accents = [new BitAccentColorItem { Name = "Crimson", Color = "DC143C" }]
            });
        });

        StringAssert.Contains(component.Find(".bit-acs-swt").GetAttribute("style"), "--bit-acs-clr:#DC143C", System.StringComparison.Ordinal,
            "A bare token is a valid accent everywhere else in the feature, and '--bit-acs-clr:DC143C' is not a color - the swatch would paint nothing.");
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
    public void BitAccentColorSwitcherWithACssStrategyShouldTagEachSwatchWithItsTokenAndEmitNoStyleOfItsOwn()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
        });

        // Until the accent has been restored the ring cannot come from the C# state - prerendered
        // markup renders before that, and a cached response's server never saw this visitor at all.
        // It comes from the marker CSS BitAccentColorHead emits, which keys on the bit-accent root
        // attribute and on the token each swatch carries here.
        var purpleToken = BitAccentColorPresets.Purple.TrimStart('#').ToLowerInvariant();
        var purple = component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Purple");
        Assert.AreEqual(purpleToken, purple.GetAttribute(BitAccentColorNames.SwatchAttribute),
            "Each swatch must carry its token for the attribute-keyed rules to target.");

        Assert.AreEqual(0, component.FindAll("style").Count,
            "The marker CSS belongs to the head, where the response's CSP nonce is: a style element rendered here has none to carry, so a style-src 'nonce-…' policy would block it and leave no swatch marked.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldNotLetTheIdEscapeIntoMarkup()
    {
        RegisterServices();

        // The switcher renders no markup of its own construction (its pre-paint marker CSS moved to
        // BitAccentColorHead, where the CSP nonce is), so the id can only reach the document through
        // an encoded attribute. This pins that down: an id is caller-supplied, and it used to be
        // interpolated into a stylesheet rendered as a MarkupString.
        const string hostileId = "x\"></style><script>alert(1)</script><style>";

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Id, hostileId);
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
        });

        Assert.AreEqual(0, component.FindAll("script").Count, "A caller-supplied id must not be able to open an element of its own.");
        Assert.AreEqual(0, component.FindAll("style").Count);
        Assert.AreEqual(hostileId, component.Find(".bit-acs").GetAttribute("id"),
            "The id must survive as one attribute value - parsed as anything else, it escaped.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherWithACssStrategyShouldMarkTheActiveSwatchByClassOnceItIsInteractive()
    {
        RegisterServices();

        // bUnit renders interactively, so the switcher has already initialized the service: the C#
        // accent state is this client's own and takes the marking back from the CSS. That is what
        // rings the swatch of a switcher offering accents the head was not configured with - and of
        // one in an app that skipped BitAccentColorHead altogether.
        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
        });

        var active = component.FindAll(".bit-acs-act");
        Assert.AreEqual(1, active.Count);
        Assert.AreEqual("Blue", active[0].GetAttribute("title"),
            "Nothing is persisted here, so the packaged primary is the active accent.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldApplyWithTheConfiguredStrategyAndPersistence()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Persistence = BitAccentColorPersistence.LocalStorage,
            });
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
    public void BitAccentColorSwitcherShouldFallBackToTheDiRegisteredConfig()
    {
        RegisterServices(accentColor: config =>
        {
            config.FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss;
            config.Persistence = BitAccentColorPersistence.All;
        });

        var component = RenderComponent<BitAccentColorSwitcher>();

        // The swatch tokens (which only a CSS strategy renders, for the head's marker rules to key
        // on) are the observable proof that the strategy reached the switcher without any Config
        // parameter - the whole point of registering the configuration once in DI.
        Assert.IsTrue(component.FindAll(".bit-acs-swt").All(s => s.HasAttribute(BitAccentColorNames.SwatchAttribute)),
            "The DI-registered CSS strategy must reach a parameterless switcher.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherConfigParameterShouldOutrankTheDiRegisteredConfig()
    {
        RegisterServices(accentColor: config =>
        {
            config.Accents = [new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" }];
        });

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Accents = [new BitAccentColorItem { Name = "Indigo", Color = "#4B0082" }],
            });
        });

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.AreEqual(1, swatches.Count);
        Assert.AreEqual("Indigo", swatches[0].GetAttribute("title"),
            "An explicit Config parameter must win over the DI-registered configuration.");
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
