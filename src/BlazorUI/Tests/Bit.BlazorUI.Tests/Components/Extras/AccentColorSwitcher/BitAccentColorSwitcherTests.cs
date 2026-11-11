using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

[TestClass]
public class BitAccentColorSwitcherTests : BunitTestContext
{
    // The second of the DefaultAccents, standing in here for "a configured accent that is not the
    // neutral one". Read off the preset rather than spelled out, so a retune of the preset cannot
    // leave these tests asserting on a hex nothing offers any more.
    private static readonly string PurpleToken = BitAccentColorPresets.Purple.TrimStart('#').ToLowerInvariant();

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

        Assert.HasCount(BitAccentColorSwitcher.DefaultAccents.Count, swatches);

        // Blue is the packaged palette's own primary, so it must render as the active swatch on a
        // fresh visit - a switcher with nothing marked would read as "no accent chosen yet".
        var active = component.FindAll(".bit-acs-act");
        Assert.ContainsSingle(active);
        Assert.AreEqual("Default", active[0].GetAttribute("title"));
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

        Assert.HasCount(2, swatches);
        Assert.AreEqual("Crimson", swatches[0].GetAttribute("title"));
        Assert.Contains("--bit-acs-clr:#DC143C", swatches[0].GetAttribute("style")!, System.StringComparison.Ordinal, "The swatch paints itself through the --bit-acs-clr custom property.");
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

        Assert.Contains("--bit-acs-clr:#DC143C", component.Find(".bit-acs-swt").GetAttribute("style")!, System.StringComparison.Ordinal, "A bare token is a valid accent everywhere else in the feature, and '--bit-acs-clr:DC143C' is not a color - the swatch would paint nothing.");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitAccentColorSwitcherShouldRespectDisabled(bool isEnabled)
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
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
        Assert.Contains("outline-width:3px", swatches[0].GetAttribute("style")!, System.StringComparison.Ordinal);

        // ActiveSwatch composes on top of Swatch for the active one only.
        var active = component.Find(".bit-acs-act");
        Assert.IsTrue(active.ClassList.Contains("custom-active"));
        Assert.ContainsSingle(s => s.ClassList.Contains("custom-active"), swatches);
    }

    [TestMethod]
    public void BitAccentColorSwitcherWithACssStrategyShouldTagEachSwatchWithItsTokenAndEmitNoStyleOfItsOwn()
    {
        RegisterServices();

        // The restore never answers, so the switcher stays where prerendered markup is: rendered,
        // but not yet handed the accent state.
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true);

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

        Assert.IsEmpty(component.FindAll("style"), "The marker CSS belongs to the head, where the response's CSP nonce is: a style element rendered here has none to carry, so a style-src 'nonce-…' policy would block it and leave no swatch marked.");
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

        Assert.IsEmpty(component.FindAll("script"), "A caller-supplied id must not be able to open an element of its own.");
        Assert.IsEmpty(component.FindAll("style"));
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
        Assert.ContainsSingle(active);
        Assert.AreEqual("Default", active[0].GetAttribute("title"),
            "Nothing is persisted here, so the packaged primary is the active accent.");

        // The hand-off is whole: the marker CSS's selectors outrank a single class, so a token left
        // on a swatch would keep painting the built-in ring over an app's Classes.ActiveSwatch.
        Assert.DoesNotContain(s => s.HasAttribute(BitAccentColorNames.SwatchAttribute), component.FindAll(".bit-acs-swt"), "Once the class marks the swatch, no swatch may still carry the token the marker CSS keys on.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherWithACssStrategyShouldDropTheTokensWhenTheRestoreLands()
    {
        RegisterServices();

        var getPersisted = Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true);

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
        });

        Assert.IsTrue(component.FindAll(".bit-acs-swt").All(s => s.HasAttribute(BitAccentColorNames.SwatchAttribute)));
        Assert.IsEmpty(component.FindAll(".bit-acs-act"), "Before the restore lands the C# state may be another visitor's, so the class marks nothing.");

        getPersisted.SetResult(null);

        component.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(component.FindAll(".bit-acs-act"));
            Assert.DoesNotContain(s => s.HasAttribute(BitAccentColorNames.SwatchAttribute), component.FindAll(".bit-acs-swt"));
        });
    }

    [TestMethod]
    public void BitAccentColorSwitcherActiveSwatchClassShouldNotBeOutrankedByTheMarkerCssOnceInteractive()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
            parameters.Add(p => p.Classes, new BitAccentColorSwitcherClassStyles { ActiveSwatch = "custom-active" });
        });

        var active = component.Find(".bit-acs-act");
        Assert.IsTrue(active.ClassList.Contains("custom-active"));
        Assert.IsFalse(active.HasAttribute(BitAccentColorNames.SwatchAttribute),
            "A token left on the active swatch keeps the marker CSS - :root[bit-accent] [bit-accent-swatch] - matching it, and that outranks the app's one class.");
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
        Assert.AreEqual(PurpleToken, apply.Arguments[0]);
        Assert.IsNull(apply.Arguments[1], "The default None strategy keeps no palette snapshot.");
        Assert.IsFalse(Assert.IsInstanceOfType<bool>(apply.Arguments[3]), "The default None strategy sets no bit-accent attribute.");
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

        // Held before the restore lands, while the swatches still carry their tokens.
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true);

        var component = RenderComponent<BitAccentColorSwitcher>();

        // The swatch tokens (which only a CSS strategy renders, for the head's marker rules to key
        // on until the switcher is interactive) are the observable proof that the strategy reached the switcher without any Config
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

        Assert.ContainsSingle(swatches);
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

    [TestMethod]
    public void BitAccentColorSwitcherShouldKeepASplattedAriaLabel()
    {
        RegisterServices();

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAccentColorSwitcher>(0);
            builder.AddAttribute(1, "aria-label", "Main");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-acs");

        Assert.AreEqual("Main", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRenderAGroupNamedAccentColorByDefault()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>();

        var root = component.Find(".bit-acs");

        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Accent color", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldNameTheGroupWithTheAriaLabelParameter()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Brand color");
        });

        Assert.AreEqual("Brand color", component.Find(".bit-acs").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldComposeTheSwatchAriaLabelFromTheName()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Accents =
                [
                    new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" },
                    new BitAccentColorItem { Name = "Indigo", AriaLabel = "Pick indigo", Color = "#4B0082" },
                    new BitAccentColorItem { Color = "#228B22" },
                ]
            });
        });

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.AreEqual("Apply the Crimson accent color", swatches[0].GetAttribute("aria-label"));
        // An explicit AriaLabel is how an app localizes the label, so it wins over the composed English one.
        Assert.AreEqual("Pick indigo", swatches[1].GetAttribute("aria-label"));
        Assert.AreEqual("Indigo", swatches[1].GetAttribute("title"));
        // An item without a name is still announced as something to press.
        Assert.AreEqual("Apply this accent color", swatches[2].GetAttribute("aria-label"));
        Assert.IsFalse(swatches[2].HasAttribute("title"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRenderEverySwatchAsAButtonOfTypeButton()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>();

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.IsTrue(swatches.All(s => s.TagName == "BUTTON"));
        // Inside a form a button without a type submits it; a swatch never may.
        Assert.IsTrue(swatches.All(s => s.GetAttribute("type") == "button"));
        Assert.AreEqual(BitAccentColorSwitcher.DefaultAccents.Count - 1, swatches.Count(s => s.GetAttribute("aria-pressed") == "false"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldPaintTheNeutralSwatchThroughTheNeutralCustomProperty()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Accents =
                [
                    new BitAccentColorItem { Name = "Default", Color = BitAccentColorPresets.Blue },
                    // The same accent spelled another way is still the neutral one.
                    new BitAccentColorItem { Name = "Bare", Color = BitAccentColorPresets.Blue.TrimStart('#').ToLowerInvariant() },
                ]
            });
        });

        // The neutral swatch shows the primary of the active packaged preset, which an applied accent overrides in
        // --bit-clr-pri, so it is read off --bit-acs-ntr with the token's own hex as the fallback.
        var expected = $"--bit-acs-clr:var(--bit-acs-ntr, {BitAccentColorPresets.Blue})";

        foreach (var swatch in component.FindAll(".bit-acs-swt"))
        {
            Assert.Contains(expected, swatch.GetAttribute("style")!, System.StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldPassAColorThatIsNotHexThroughForCssToJudge()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                Accents = [new BitAccentColorItem { Name = "Named", Color = "rebeccapurple" }]
            });
        });

        // Only a missing # is supplied; anything else is the author's spelling, and a non-hex value gets no #.
        Assert.Contains("--bit-acs-clr:rebeccapurple", component.Find(".bit-acs-swt").GetAttribute("style")!, System.StringComparison.Ordinal);
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldPairDisabledWithAriaDisabledOnEverySwatch()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        foreach (var swatch in component.FindAll(".bit-acs-swt"))
        {
            Assert.IsTrue(swatch.HasAttribute("disabled"));
            Assert.AreEqual("true", swatch.GetAttribute("aria-disabled"));
        }

        component.Render(parameters => parameters.Add(p => p.Disabled, false));

        foreach (var swatch in component.FindAll(".bit-acs-swt"))
        {
            Assert.IsFalse(swatch.HasAttribute("disabled"));
            Assert.IsFalse(swatch.HasAttribute("aria-disabled"));
        }

        Assert.IsFalse(component.Find(".bit-acs").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldNotApplyAClickWhileDisabled()
    {
        RegisterServices();

        var changed = 0;

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnChange, (string _) => changed++);
        });

        // A script or an assistive technology can still deliver a click to the button; the handler guards it.
        component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Purple").Click();

        Assert.AreEqual(0, changed);
        Assert.DoesNotContain(i => i.Identifier == "BitBlazorUI.AccentColor.apply", Context.JSInterop.Invocations);
        Assert.AreEqual("Default", component.Find(".bit-acs-act").GetAttribute("title"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldClearTheOverridesWhenTheNeutralSwatchIsPicked()
    {
        RegisterServices();

        string? changed = null;

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string color) => changed = color);
        });

        component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Purple").Click();
        component.FindAll(".bit-acs-swt").First(s => s.GetAttribute("title") == "Default").Click();

        // The packaged primary reproduces the packaged palette: nothing is applied, the stores and the attribute
        // are cleared instead.
        Assert.ContainsSingle(i => i.Identifier == "BitBlazorUI.AccentColor.apply", Context.JSInterop.Invocations);
        Assert.ContainsSingle(i => i.Identifier == "BitBlazorUI.AccentColor.clear", Context.JSInterop.Invocations);
        Assert.AreEqual(BitAccentColorPresets.Blue, changed);
        Assert.AreEqual("Default", component.Find(".bit-acs-act").GetAttribute("title"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldMoveTheActiveStylesWithTheActiveSwatch()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitAccentColorSwitcherClassStyles { Root = "padding:4px", ActiveSwatch = "border-color:red" });
        });

        Assert.Contains("padding:4px", component.Find(".bit-acs").GetAttribute("style")!, System.StringComparison.Ordinal);

        var swatches = component.FindAll(".bit-acs-swt");
        Assert.ContainsSingle(s => s.GetAttribute("style")!.Contains("border-color:red"), swatches);
        Assert.Contains("border-color:red", swatches.First(s => s.GetAttribute("title") == "Default").GetAttribute("style")!, System.StringComparison.Ordinal);

        swatches.First(s => s.GetAttribute("title") == "Purple").Click();

        swatches = component.FindAll(".bit-acs-swt");
        Assert.ContainsSingle(s => s.GetAttribute("style")!.Contains("border-color:red"), swatches);
        Assert.Contains("border-color:red", swatches.First(s => s.GetAttribute("title") == "Purple").GetAttribute("style")!, System.StringComparison.Ordinal);
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldKeepEveryInstanceOnTheOneAccent()
    {
        RegisterServices();

        // Two switchers of one app chrome share the scoped service, and its AccentChanged event is what re-renders
        // the one that was not clicked.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAccentColorSwitcher>(0);
            builder.AddComponentParameter(1, nameof(BitAccentColorSwitcher.Class), "first");
            builder.CloseComponent();
            builder.OpenComponent<BitAccentColorSwitcher>(2);
            builder.AddComponentParameter(3, nameof(BitAccentColorSwitcher.Class), "second");
            builder.CloseComponent();
        });

        component.FindAll(".first .bit-acs-swt").First(s => s.GetAttribute("title") == "Purple").Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("Purple", component.Find(".second .bit-acs-act").GetAttribute("title"));
            Assert.AreEqual("true", component.FindAll(".second .bit-acs-swt").First(s => s.GetAttribute("title") == "Purple").GetAttribute("aria-pressed"));
        });
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRenderTheBaseParameters()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>(parameters =>
        {
            parameters.Add(p => p.Id, "accents");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Style, "margin:1px");
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        var root = component.Find(".bit-acs");

        Assert.AreEqual("accents", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.Contains("margin:1px", root.GetAttribute("style")!, System.StringComparison.Ordinal);
        Assert.Contains("visibility:hidden", root.GetAttribute("style")!, System.StringComparison.Ordinal);

        component.Render(parameters => parameters.Add(p => p.Visibility, BitVisibility.Collapsed));

        Assert.Contains("display:none", component.Find(".bit-acs").GetAttribute("style")!, System.StringComparison.Ordinal);
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldSplatHtmlAttributesOnTheRoot()
    {
        RegisterServices();

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitAccentColorSwitcher>(0);
            builder.AddAttribute(1, "data-test", "accents");
            builder.CloseComponent();
        });

        Assert.AreEqual("accents", component.Find(".bit-acs").GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldReflectAnAccentAppliedThroughTheService()
    {
        RegisterServices();

        var component = RenderComponent<BitAccentColorSwitcher>();

        // An app applying an accent programmatically (a settings page, a free-form picker) moves the ring too.
        var service = Context.Services.GetRequiredService<BitAccentColorService>();
        service.ApplyAsync(BitAccentColorPresets.Green).GetAwaiter().GetResult();

        component.WaitForAssertion(() => Assert.AreEqual("Green", component.Find(".bit-acs-act").GetAttribute("title")));

        // An accent none of the swatches offers rings none of them.
        service.ApplyAsync("#123456").GetAwaiter().GetResult();

        component.WaitForAssertion(() => Assert.IsEmpty(component.FindAll(".bit-acs-act")));
    }
}
