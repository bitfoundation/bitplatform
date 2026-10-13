using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ThemeSwitcher;

[TestClass]
public class BitThemeSwitcherTests : BunitTestContext
{
    private void RegisterServices()
    {
        // The switcher resolves BitThemeManager - the same registration an app gets from
        // AddBitBlazorUIExtrasServices (which chains the core services).
        Context.Services.AddBitBlazorUIExtrasServices();
    }

    /// <summary>
    /// The text the design system picker is showing - i.e. which design system it has selected. Read from the
    /// dropdown's own text span rather than from the switcher root, which also carries the dropdown's aria live
    /// region and would therefore report the same text twice.
    /// </summary>
    private static string SelectedDesignSystem(IRenderedComponent<BitThemeSwitcher> component)
        => component.Find(".bit-ths-dds .bit-drp-tdp").TextContent.Trim();

    [TestMethod]
    public void BitThemeSwitcherShouldRenderBothHalvesByDefault()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();

        Assert.AreEqual(1, component.FindAll(".bit-ths-dds").Count);

        // Both scheme buttons are always rendered; which one is visible is a stylesheet decision keyed on the
        // document's bit-theme attribute, so the markup cannot depend on C# knowing the active scheme.
        var buttons = component.FindAll(".bit-ths-csb");
        Assert.AreEqual(2, buttons.Count);
        Assert.AreEqual(1, component.FindAll(".bit-ths-drk").Count);
        Assert.AreEqual(1, component.FindAll(".bit-ths-lgt").Count);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldOfferTheFourPackagedDesignSystems()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();

        CollectionAssert.AreEqual(
            new[] { "fluent", "fluent2", "material", "cupertino" },
            BitThemeSwitcher.DefaultDesignSystems.Select(i => i.Value).ToArray());

        // Fluent's pair is the core light/dark one rather than fluent-light / fluent-dark: those two are what
        // the JS toggle flips between by default, which is what the toggle handler defers to.
        var fluent = BitThemeSwitcher.DefaultDesignSystems[0];
        Assert.AreEqual(BitThemePresets.Light, fluent.LightTheme);
        Assert.AreEqual(BitThemePresets.Dark, fluent.DarkTheme);

        Assert.AreEqual(BitThemePresets.Fluent2Light, BitThemeSwitcher.DefaultDesignSystems[1].LightTheme);
        Assert.AreEqual(BitThemePresets.Fluent2Dark, BitThemeSwitcher.DefaultDesignSystems[1].DarkTheme);
        Assert.AreEqual(BitThemePresets.MaterialDark, BitThemeSwitcher.DefaultDesignSystems[2].DarkTheme);
        Assert.AreEqual(BitThemePresets.CupertinoLight, BitThemeSwitcher.DefaultDesignSystems[3].LightTheme);

        Assert.AreEqual("Fluent", SelectedDesignSystem(component));
    }

    [DataTestMethod]
    [DataRow(true, false, ".bit-ths-dds", ".bit-ths-csb")]
    [DataRow(false, true, ".bit-ths-csb", ".bit-ths-dds")]
    public void BitThemeSwitcherShouldRenderOnlyTheRequestedParts(bool noColorScheme, bool noDesignSystem, string kept, string dropped)
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.NoColorScheme, noColorScheme);
            parameters.Add(p => p.NoDesignSystem, noDesignSystem);
        });

        Assert.AreNotEqual(0, component.FindAll(kept).Count);
        Assert.AreEqual(0, component.FindAll(dropped).Count);
    }

    [DataTestMethod]
    [DataRow("material-dark", "Material")]
    [DataRow("cupertino-light", "Cupertino")]
    [DataRow("material", "Material")]
    [DataRow("dark", "Fluent")]
    [DataRow("fluent-light", "Fluent")]
    // Fluent and Fluent 2 share a prefix but not a token, so neither claims the other's names.
    [DataRow("fluent2", "Fluent 2")]
    [DataRow("fluent2-light", "Fluent 2")]
    [DataRow("fluent2-dark", "Fluent 2")]
    // Neither a custom preset nor the system pseudo-preset is claimed by any item, so both fall back to the
    // first one - the design system the core stylesheet actually paints them with.
    [DataRow("acme-dark", "Fluent")]
    [DataRow("system", "Fluent")]
    [DataRow(null, "Fluent")]
    public void BitThemeSwitcherShouldSelectTheDesignSystemOfTheInitialTheme(string? initialTheme, string expected)
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, initialTheme);
        });

        Assert.AreEqual(expected, SelectedDesignSystem(component));
    }

    [DataTestMethod]
    [DataRow("material-light material-dark", "Material")]
    [DataRow("cupertino-light%20cupertino-dark", "Cupertino")]
    // A scheme marker, or anything else that is no pair, names no design system.
    [DataRow("dark", "Fluent")]
    [DataRow("material-light", "Fluent")]
    [DataRow(null, "Fluent")]
    public void BitThemeSwitcherShouldSelectTheDesignSystemOfTheInitialPairWhenTheInitialThemeNamesNone(string? initialPair, string expected)
    {
        RegisterServices();

        // A visitor who follows the OS has "system" stored as the preference; the pair cookie is what says
        // which design system they picked.
        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, BitThemePresets.System);
            parameters.Add(p => p.InitialThemePair, initialPair);
        });

        Assert.AreEqual(expected, SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldPreferTheInitialThemeOverTheInitialPair()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, BitThemePresets.CupertinoDark);
            parameters.Add(p => p.InitialThemePair, "material-light material-dark");
        });

        Assert.AreEqual("Cupertino", SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldSwitchTheLightDarkPairWhenADesignSystemIsPicked()
    {
        RegisterServices();
        string? changed = null;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string theme) => changed = theme);
        });

        component.FindAll(".bit-drp-itm").Single(i => i.TextContent.Trim() == "Material").Click();

        // The design system is the pair, so it goes through setPair - which carries the scheme over and keeps a
        // visitor who follows the OS following it - rather than pinning one theme of it with set (#13687).
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual(BitThemePresets.MaterialLight, invocation.Arguments[0]);
        Assert.AreEqual(BitThemePresets.MaterialDark, invocation.Arguments[1]);
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.set");

        Assert.AreEqual("Material", SelectedDesignSystem(component));
        Assert.IsNotNull(changed);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldPickThePairThePageRetargetsWithinTheDesignSystem()
    {
        RegisterServices();
        // The page declares fluent-light / fluent-dark - Fluent, spelled its own way - and the visitor is on Fluent 2.
        SetupState(BitThemePresets.Fluent2Dark, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark, BitThemePresets.FluentLight, BitThemePresets.FluentDark);
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.get").SetResult(BitThemePresets.Fluent2Dark);

        var component = RenderComponent<BitThemeSwitcher>();
        Assert.AreEqual("Fluent 2", SelectedDesignSystem(component));

        component.FindAll(".bit-drp-itm").Single(i => i.TextContent.Trim() == "Fluent").Click();

        // Back to the page's own pair rather than the item's light / dark, so the page stays in charge of it.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual(BitThemePresets.FluentLight, invocation.Arguments[0]);
        Assert.AreEqual(BitThemePresets.FluentDark, invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldToggleWithinThePairOfTheAppliedDesignSystem()
    {
        RegisterServices();
        SetupState(BitThemePresets.MaterialDark, BitThemePresets.MaterialLight, BitThemePresets.MaterialDark, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark);
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.toggleDarkLightWithin", _ => true).SetResult(BitThemePresets.MaterialLight);
        string? changed = null;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string theme) => changed = theme);
        });

        component.Find(".bit-ths-lgt").Click();

        // Material is the active pair already: one call flips within it, with no pair switch in front of it.
        var toggle = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.toggleDarkLightWithin");
        Assert.AreEqual(BitThemePresets.MaterialLight, toggle.Arguments[0]);
        Assert.AreEqual(BitThemePresets.MaterialDark, toggle.Arguments[1]);
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.setPair");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.toggleDarkLight");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.set");

        Assert.AreEqual(BitThemePresets.MaterialLight, changed);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldToggleWithinTheAppliedDesignSystemWhenAnotherPairIsActive()
    {
        RegisterServices();
        // The page declares Fluent 2, and the app applied the core dark theme - Fluent's - on its own.
        SetupState(BitThemePresets.Dark, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark);

        var component = RenderComponent<BitThemeSwitcher>();

        component.Find(".bit-ths-drk").Click();

        // Within Fluent's pair, so "Turn on light" turns light on rather than jumping to Fluent 2's dark theme.
        var toggle = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.toggleDarkLightWithin");
        Assert.AreEqual(BitThemePresets.Light, toggle.Arguments[0]);
        Assert.AreEqual(BitThemePresets.Dark, toggle.Arguments[1]);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldLeaveAPairThePageRetargetsToThePage()
    {
        RegisterServices();
        SetupState(BitThemePresets.FluentDark, BitThemePresets.FluentLight, BitThemePresets.FluentDark, BitThemePresets.FluentLight, BitThemePresets.FluentDark);

        var component = RenderComponent<BitThemeSwitcher>();

        component.Find(".bit-ths-drk").Click();

        // A host page can retarget Fluent's pair with bit-theme-light / bit-theme-dark, so the toggle flips within
        // the page's pair rather than imposing the item's light / dark on it.
        var toggle = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.toggleDarkLightWithin");
        Assert.AreEqual(BitThemePresets.FluentLight, toggle.Arguments[0]);
        Assert.AreEqual(BitThemePresets.FluentDark, toggle.Arguments[1]);
    }

    private void SetupState(string theme, string light, string dark, string configuredLight, string configuredDark)
    {
        Context.JSInterop.Setup<string?[]?>("BitBlazorUI.Theme.getState")
                         .SetResult([theme, light, dark, configuredLight, configuredDark]);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldOfferCustomDesignSystems()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem>
            {
                new() { Text = "Acme", Value = "acme" },
                new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
            });
            // Claimed by the first item through its "{Value}-" prefix, without either scheme name being spelled out.
            parameters.Add(p => p.InitialTheme, "acme-dark");
        });

        Assert.AreEqual("Acme", SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldDropItemsWithoutAValue()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem>
            {
                new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
                new() { Text = "Nameless" },
            });
        });

        // An item with no Value names no theme, so it could only ever apply the empty string.
        Assert.AreEqual("Fluent", SelectedDesignSystem(component));
        Assert.AreEqual(0, component.FindAll(".bit-ths-dds .bit-drp-tdp").Count(e => e.TextContent.Contains("Nameless")));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldRenderTheConfiguredIconsAndTitles()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DarkSchemeIconName, "Brightness");
            parameters.Add(p => p.DarkSchemeTitle, "Go dark");
            parameters.Add(p => p.LightSchemeTitle, "Go light");
        });

        // The button visible while the dark scheme is active is the one that switches to light.
        var dark = component.Find(".bit-ths-drk");
        Assert.AreEqual("Go light", dark.GetAttribute("title"));
        Assert.AreEqual("Go light", dark.GetAttribute("aria-label"));
        Assert.IsTrue(dark.QuerySelector("i")!.ClassList.Contains("bit-icon--Sunny"));

        var light = component.Find(".bit-ths-lgt");
        Assert.AreEqual("Go dark", light.GetAttribute("title"));
        Assert.IsTrue(light.QuerySelector("i")!.ClassList.Contains("bit-icon--Brightness"));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitThemeSwitcherShouldRespectDisabled(bool isEnabled)
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        Assert.AreEqual(isEnabled is false, component.Find(".bit-ths").ClassList.Contains("bit-dis"));

        foreach (var button in component.FindAll(".bit-ths-csb"))
        {
            Assert.AreEqual(isEnabled is false, button.HasAttribute("disabled"));
        }
    }

    [TestMethod]
    public void BitThemeSwitcherShouldApplyClassesAndStyles()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitThemeSwitcherClassStyles
            {
                Root = "custom-root",
                DesignSystem = "custom-dds",
                ColorSchemeButton = "custom-csb",
                DarkSchemeButton = "custom-drk",
                LightSchemeButton = "custom-lgt",
                Icon = "custom-icon"
            });
            parameters.Add(p => p.Styles, new BitThemeSwitcherClassStyles
            {
                Root = "gap:1.5rem",
                ColorSchemeButton = "border-radius:0.25rem",
                DarkSchemeButton = "outline-width:3px"
            });
        });

        var root = component.Find(".bit-ths");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("gap:1.5rem"));

        Assert.IsTrue(component.Find(".bit-ths-dds").ClassList.Contains("custom-dds"));

        var dark = component.Find(".bit-ths-drk");
        Assert.IsTrue(dark.ClassList.Contains("custom-csb"));
        Assert.IsTrue(dark.ClassList.Contains("custom-drk"));
        Assert.IsTrue(dark.GetAttribute("style")!.Contains("border-radius:0.25rem"));
        Assert.IsTrue(dark.GetAttribute("style")!.Contains("outline-width:3px"));
        Assert.IsTrue(dark.QuerySelector("i")!.ClassList.Contains("custom-icon"));

        var light = component.Find(".bit-ths-lgt");
        Assert.IsTrue(light.ClassList.Contains("custom-lgt"));
        // The per-scheme style belongs to the other button only.
        Assert.IsFalse(light.GetAttribute("style")!.Contains("outline-width:3px"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldApplyDirAndAriaLabel()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.AriaLabel, "Appearance");
        });

        var root = component.Find(".bit-ths");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Appearance", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldKeepASplattedAriaLabel()
    {
        RegisterServices();

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitThemeSwitcher>(0);
            builder.AddAttribute(1, "aria-label", "Main");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ths");

        Assert.AreEqual("Main", root.GetAttribute("aria-label"));
    }
}
