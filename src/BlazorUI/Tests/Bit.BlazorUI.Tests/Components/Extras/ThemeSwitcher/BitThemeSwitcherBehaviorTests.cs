using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ThemeSwitcher;

/// <summary>
/// Covers what <see cref="BitThemeSwitcherTests"/> leaves out: the picker's items and their accessible names, the
/// defaults of the titles and icons, the round trips to the theme script (the applied theme read back on the first
/// render, the theme changes it follows afterwards, a pair the page refuses), the values OnChange reports when the
/// script cannot answer, the disabled state, the keyboard and the parts that Classes and Styles reach.
/// </summary>
[TestClass]
public class BitThemeSwitcherBehaviorTests : BunitTestContext
{
    private void RegisterServices()
    {
        Context.Services.AddBitBlazorUIExtrasServices();
    }

    private static string SelectedDesignSystem(IRenderedComponent<BitThemeSwitcher> component)
        => component.Find(".bit-ths-dds .bit-drp-tdp").TextContent.Trim();

    private static List<AngleSharp.Dom.IElement> Items(IRenderedComponent<BitThemeSwitcher> component)
        => [.. component.FindAll(".bit-drp-itm")];

    private void SetupState(string theme, string light, string dark, string configuredLight, string configuredDark)
    {
        Context.JSInterop.Setup<string?[]?>("BitBlazorUI.Theme.getState")
                         .SetResult([theme, light, dark, configuredLight, configuredDark]);
    }

    /// <summary>
    /// Plays the part of the theme script reporting a change of the document's bit-theme attribute: the receiver the
    /// manager registered through registerDotNetNotifier is what the script calls back.
    /// </summary>
    private async Task RaiseThemeChangedFromJs(IRenderedComponent<BitThemeSwitcher> component, string? newTheme, string? oldTheme)
    {
        var reference = Context.JSInterop.Invocations["BitBlazorUI.Theme.registerDotNetNotifier"].Last().Arguments[0]!;
        var receiver = reference.GetType().GetProperty(nameof(DotNetObjectReference<object>.Value))!.GetValue(reference)!;
        var notify = receiver.GetType().GetMethod("NotifyThemeChangedFromJs")!;

        await component.InvokeAsync(() => (Task)notify.Invoke(receiver, [newTheme, oldTheme])!);
    }

    #region Picker items

    [TestMethod]
    public void BitThemeSwitcherShouldListOneItemPerDefaultDesignSystem()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();

        var items = Items(component);
        CollectionAssert.AreEqual(
            BitThemeSwitcher.DefaultDesignSystems.Select(i => i.Text).ToArray(),
            items.Select(i => i.TextContent.Trim()).ToArray());

        // The item's AriaLabel falls back to its Text.
        CollectionAssert.AreEqual(
            new[] { "Fluent", "Fluent 2", "Material", "Cupertino" },
            items.Select(i => i.GetAttribute("aria-label")).ToArray());

        Assert.AreEqual(1, items.Count(i => i.GetAttribute("aria-selected") == "true"));
        Assert.AreEqual("true", items[0].GetAttribute("aria-selected"), "the first item is the fallback until the theme is read back");
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNameAnItemByItsValueAndAriaLabelWhenItCarriesNoText()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem>
            {
                new() { Value = "acme" },
                new() { Text = "Brand", Value = "brand", AriaLabel = "Brand design system" },
            });
        });

        var items = Items(component);
        Assert.AreEqual("acme", items[0].TextContent.Trim());
        Assert.AreEqual("Brand", items[1].TextContent.Trim());
        Assert.AreEqual("Brand design system", items[1].GetAttribute("aria-label"));
        Assert.AreEqual("acme", SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldRenderADisabledItemAsADisabledOption()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem>
            {
                new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
                new() { Text = "Material", Value = BitThemePresets.Material, IsDisabled = true },
            });
        });

        var material = Items(component).Single(i => i.TextContent.Trim() == "Material");
        Assert.IsTrue(material.HasAttribute("disabled"));
        Assert.AreEqual("true", material.GetAttribute("aria-disabled"));

        var fluent = Items(component).Single(i => i.TextContent.Trim() == "Fluent");
        Assert.IsFalse(fluent.HasAttribute("disabled"));
        Assert.IsNull(fluent.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNameThePickerThroughDesignSystemTitle()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();
        var trigger = component.Find(".bit-ths-dds .bit-drp-wrp");
        Assert.AreEqual("Design system", trigger.GetAttribute("title"));
        Assert.AreEqual("Design system", trigger.GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.DesignSystemTitle, "Skin"));

        trigger = component.Find(".bit-ths-dds .bit-drp-wrp");
        Assert.AreEqual("Skin", trigger.GetAttribute("title"));
        Assert.AreEqual("Skin", trigger.GetAttribute("aria-label"));
    }

    #endregion

    #region Defaults of the scheme buttons

    [TestMethod]
    public void BitThemeSwitcherShouldRenderTheDefaultTitlesAndIcons()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();

        // The button shown while the dark scheme is active turns the light on, and the other one off.
        var dark = component.Find(".bit-ths-drk");
        Assert.AreEqual("Turn on light", dark.GetAttribute("title"));
        Assert.AreEqual("Turn on light", dark.GetAttribute("aria-label"));
        Assert.IsTrue(dark.QuerySelector("i")!.ClassList.Contains("bit-icon--Sunny"));

        var light = component.Find(".bit-ths-lgt");
        Assert.AreEqual("Turn off light", light.GetAttribute("title"));
        Assert.AreEqual("Turn off light", light.GetAttribute("aria-label"));
        Assert.IsTrue(light.QuerySelector("i")!.ClassList.Contains("bit-icon--ClearNight"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldRenderTheSchemeButtonsAsNativeButtonsWithHiddenIcons()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.LightSchemeIconName, "Brightness");
        });

        foreach (var button in component.FindAll(".bit-ths-csb"))
        {
            // A native button is what makes Enter and Space operate the toggle, and type=button keeps it from
            // submitting a form the switcher is placed in.
            Assert.AreEqual("BUTTON", button.TagName);
            Assert.AreEqual("button", button.GetAttribute("type"));

            var icon = button.QuerySelector("i")!;
            Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
            Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        }

        Assert.IsTrue(component.Find(".bit-ths-drk i").ClassList.Contains("bit-icon--Brightness"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNameTheRootThemeByDefault()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();

        var root = component.Find(".bit-ths");
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Theme", root.GetAttribute("aria-label"));
    }

    #endregion

    #region Reading the applied theme back

    [TestMethod]
    public void BitThemeSwitcherShouldSelectTheDesignSystemOfTheAppliedThemeAfterTheFirstRender()
    {
        RegisterServices();
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.get").SetResult(BitThemePresets.CupertinoDark);

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, BitThemePresets.MaterialLight);
        });

        // The applied theme is what the page wears, so it wins over what InitialTheme guessed for prerendering.
        Assert.AreEqual("Cupertino", SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldKeepTheInitialThemeWhenTheAppliedThemeCannotBeRead()
    {
        RegisterServices();
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.get").SetResult(null);

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, BitThemePresets.MaterialLight);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.get");
        Assert.AreEqual("Material", SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNotReadTheAppliedThemeWithoutAPicker()
    {
        RegisterServices();

        RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.NoDesignSystem, true);
        });

        // With no picker there is nothing to reflect, so neither the read nor the subscription it follows is made.
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.get");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.registerDotNetNotifier");
    }

    [TestMethod]
    public async Task BitThemeSwitcherShouldFollowAThemeAppliedElsewhere()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();
        Assert.AreEqual("Fluent", SelectedDesignSystem(component));

        await RaiseThemeChangedFromJs(component, BitThemePresets.MaterialDark, BitThemePresets.Light);
        Assert.AreEqual("Material", SelectedDesignSystem(component));

        // A theme no item claims is painted with the first design system.
        await RaiseThemeChangedFromJs(component, "acme-dark", BitThemePresets.MaterialDark);
        Assert.AreEqual("Fluent", SelectedDesignSystem(component));
    }

    [TestMethod]
    public async Task BitThemeSwitcherShouldIgnoreAnUnknownThemeChange()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, BitThemePresets.CupertinoLight);
        });

        // A null new theme means "unknown", not a theme name, so the picker keeps what it knows.
        await RaiseThemeChangedFromJs(component, null, BitThemePresets.CupertinoLight);

        Assert.AreEqual("Cupertino", SelectedDesignSystem(component));
    }

    [TestMethod]
    public async Task BitThemeSwitcherShouldStopFollowingTheThemeOnceDisposed()
    {
        RegisterServices();

        RenderComponent<BitThemeSwitcher>();
        var notifications = Context.Services.GetRequiredService<BitThemeNotifications>();
        Assert.IsNotNull(ThemeChangedHandlers(notifications), "the picker follows the theme while it is rendered");

        var reference = Context.JSInterop.Invocations["BitBlazorUI.Theme.registerDotNetNotifier"].Last().Arguments[0]!;
        var receiver = reference.GetType().GetProperty(nameof(DotNetObjectReference<object>.Value))!.GetValue(reference)!;
        var notify = receiver.GetType().GetMethod("NotifyThemeChangedFromJs")!;

        await Context.DisposeComponentsAsync();

        // BitThemeNotifications is scoped, so a handler left behind would keep the component rooted for the circuit.
        Assert.IsNull(ThemeChangedHandlers(notifications));
        await (Task)notify.Invoke(receiver, [BitThemePresets.MaterialDark, BitThemePresets.Light])!;
    }

    private static Delegate? ThemeChangedHandlers(BitThemeNotifications notifications)
        => (Delegate?)typeof(BitThemeNotifications)
            .GetField("_themeChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(notifications);

    #endregion

    #region Picking a design system

    [TestMethod]
    public void BitThemeSwitcherShouldReportTheLightThemeWhenTheScriptCannotAnswerAPick()
    {
        RegisterServices();
        string? changed = null;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string theme) => changed = theme);
        });

        Items(component).Single(i => i.TextContent.Trim() == "Cupertino").Click();

        // setPair answers null when the script cannot be reached, so OnChange reports the pair's light theme.
        Assert.AreEqual(BitThemePresets.CupertinoLight, changed);
        Assert.AreEqual("Cupertino", SelectedDesignSystem(component));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldReportTheThemeTheScriptApplied()
    {
        RegisterServices();
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.setPair", _ => true).SetResult(BitThemePresets.MaterialDark);
        string? changed = null;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string theme) => changed = theme);
        });

        Items(component).Single(i => i.TextContent.Trim() == "Material").Click();

        // The scheme is carried over by the script, so the theme it lands on is the one reported.
        Assert.AreEqual(BitThemePresets.MaterialDark, changed);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldSpellACustomPairFromTheItemsValue()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem>
            {
                new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
                new() { Text = "Acme", Value = "acme" },
                new() { Text = "Brand", Value = "brand", LightTheme = "brand-day", DarkTheme = "brand-night" },
            });
        });

        Items(component).Single(i => i.TextContent.Trim() == "Acme").Click();

        var acme = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual("acme-light", acme.Arguments[0]);
        Assert.AreEqual("acme-dark", acme.Arguments[1]);

        Items(component).Single(i => i.TextContent.Trim() == "Brand").Click();

        var brand = Context.JSInterop.Invocations["BitBlazorUI.Theme.setPair"].Last();
        Assert.AreEqual("brand-day", brand.Arguments[0]);
        Assert.AreEqual("brand-night", brand.Arguments[1]);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldGoBackToTheWornDesignSystemWhenThePageRefusesThePair()
    {
        RegisterServices();
        SetupState(BitThemePresets.Fluent2Dark, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark);
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.get").SetResult(BitThemePresets.Fluent2Dark);
        // The page does not offer Material (bit-theme-available), so the script keeps the theme it had.
        Context.JSInterop.Setup<string?>("BitBlazorUI.Theme.setPair", _ => true).SetResult(BitThemePresets.Fluent2Dark);
        var changes = 0;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string _) => changes++);
        });
        Assert.AreEqual("Fluent 2", SelectedDesignSystem(component));

        Items(component).Single(i => i.TextContent.Trim() == "Material").Click();

        Assert.AreEqual("Fluent 2", SelectedDesignSystem(component));
        Assert.AreEqual(0, changes, "nothing changed, so nothing is reported");
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNotApplyADisabledDesignSystem()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem>
            {
                new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
                new() { Text = "Material", Value = BitThemePresets.Material, IsDisabled = true },
            });
        });

        var material = Items(component).Single(i => i.TextContent.Trim() == "Material");
        try
        {
            material.Click();
        }
        catch (MissingEventHandlerException)
        {
            // A disabled button may carry no handler at all, which is just as good.
        }

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.setPair");
        Assert.AreEqual("Fluent", SelectedDesignSystem(component));
    }

    #endregion

    #region Toggling the scheme

    [TestMethod]
    public void BitThemeSwitcherShouldToggleWithinThePickersSelectionWhenTheThemeCannotBeRead()
    {
        RegisterServices();
        string? changed = null;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.InitialTheme, BitThemePresets.MaterialLight);
            parameters.Add(p => p.OnChange, (string theme) => changed = theme);
        });

        component.Find(".bit-ths-lgt").Click();

        // No state to go on: the design system the visitor is looking at decides the pair, and with no applied
        // theme to read the scheme from the target is the dark one.
        var toggle = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.toggleDarkLightWithin");
        Assert.AreEqual(BitThemePresets.MaterialLight, toggle.Arguments[0]);
        Assert.AreEqual(BitThemePresets.MaterialDark, toggle.Arguments[1]);
        Assert.AreEqual(BitThemePresets.MaterialDark, changed);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldReadACustomDarkThemeByItsSuffix()
    {
        RegisterServices();
        SetupState("acme-dark", "acme-light", "acme-dark", "acme-light", "acme-dark");
        string? changed = null;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.DesignSystems, new List<BitThemeSwitcherItem> { new() { Text = "Acme", Value = "acme" } });
            parameters.Add(p => p.OnChange, (string theme) => changed = theme);
        });

        component.Find(".bit-ths-drk").Click();

        // "acme-dark" ends in "dark" the way the stylesheet's [bit-theme$="dark"] reads it, so the toggle heads light.
        Assert.AreEqual("acme-light", changed);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldToggleTheSameWayFromEitherButton()
    {
        RegisterServices();
        SetupState(BitThemePresets.CupertinoLight, BitThemePresets.CupertinoLight, BitThemePresets.CupertinoDark, BitThemePresets.CupertinoLight, BitThemePresets.CupertinoDark);
        var changed = new List<string>();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.OnChange, (string theme) => changed.Add(theme));
        });

        // Only one of the two is visible at a time, and both flip the scheme rather than set one.
        component.Find(".bit-ths-lgt").Click();
        component.Find(".bit-ths-drk").Click();

        Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Theme.toggleDarkLightWithin"].Count);
        CollectionAssert.AreEqual(new[] { BitThemePresets.CupertinoDark, BitThemePresets.CupertinoDark }, changed);
    }

    #endregion

    #region Disabled

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitThemeSwitcherShouldPairDisabledWithAriaDisabled(bool disabled)
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, disabled);
        });

        foreach (var button in component.FindAll(".bit-ths-csb"))
        {
            Assert.AreEqual(disabled, button.HasAttribute("disabled"));
            Assert.AreEqual(disabled ? "true" : null, button.GetAttribute("aria-disabled"));
        }

        // The picker is disabled with the switcher: its trigger leaves the tab order and says so.
        var trigger = component.Find(".bit-ths-dds .bit-drp-wrp");
        Assert.AreEqual(disabled ? "true" : null, trigger.GetAttribute("aria-disabled"));
        Assert.AreEqual(disabled ? "-1" : "0", trigger.GetAttribute("tabindex"));
        Assert.AreEqual(disabled, component.Find(".bit-ths-dds").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNotToggleWhileDisabled()
    {
        RegisterServices();
        var changes = 0;

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnChange, (string _) => changes++);
        });

        // A script or an assistive technology can still deliver a click to a disabled button.
        component.Find(".bit-ths-lgt").Click();
        component.Find(".bit-ths-drk").Click();

        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.getState");
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.toggleDarkLightWithin");
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldNotOpenThePickerWhileDisabled()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        component.Find(".bit-ths-dds .bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("false", component.Find(".bit-ths-dds .bit-drp-wrp").GetAttribute("aria-expanded"));
    }

    #endregion

    #region Keyboard

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("ArrowDown")]
    public void BitThemeSwitcherShouldOpenThePickerFromTheKeyboard(string key)
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();
        var trigger = component.Find(".bit-ths-dds .bit-drp-wrp");
        Assert.AreEqual("combobox", trigger.GetAttribute("role"));
        Assert.AreEqual("false", trigger.GetAttribute("aria-expanded"));

        trigger.KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual("true", component.Find(".bit-ths-dds .bit-drp-wrp").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldCloseThePickerOnEscape()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();
        component.Find(".bit-ths-dds .bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.AreEqual("true", component.Find(".bit-ths-dds .bit-drp-wrp").GetAttribute("aria-expanded"));

        component.Find(".bit-ths-dds .bit-drp-wrp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("false", component.Find(".bit-ths-dds .bit-drp-wrp").GetAttribute("aria-expanded"));
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Theme.setPair");
    }

    #endregion

    #region Direction, Classes and Styles

    [TestMethod]
    [DataRow(BitDir.Rtl, "rtl", true)]
    [DataRow(BitDir.Ltr, "ltr", false)]
    [DataRow(BitDir.Auto, "auto", false)]
    public void BitThemeSwitcherShouldRenderTheDirection(BitDir dir, string expected, bool rtlClass)
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-ths");
        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(rtlClass, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldLeaveTheDirectionToThePageByDefault()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>();

        var root = component.Find(".bit-ths");
        Assert.IsFalse(root.HasAttribute("dir"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldApplyTheRemainingPartStyles()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitThemeSwitcherClassStyles
            {
                DesignSystem = "min-width:9rem",
                LightSchemeButton = "outline-style:dotted",
                Icon = "font-size:20px"
            });
            parameters.Add(p => p.Classes, new BitThemeSwitcherClassStyles
            {
                LightSchemeButton = "custom-lgt"
            });
        });

        Assert.IsTrue(component.Find(".bit-ths-dds").GetAttribute("style")!.Contains("min-width:9rem"));

        var light = component.Find(".bit-ths-lgt");
        Assert.IsTrue(light.GetAttribute("style")!.Contains("outline-style:dotted"));
        Assert.IsFalse(component.Find(".bit-ths-drk").GetAttribute("style")!.Contains("outline-style:dotted"));
        Assert.IsFalse(component.Find(".bit-ths-drk").ClassList.Contains("custom-lgt"));

        foreach (var icon in component.FindAll(".bit-ths-csb i"))
        {
            Assert.AreEqual("font-size:20px", icon.GetAttribute("style"));
        }
    }

    [TestMethod]
    public void BitThemeSwitcherShouldApplyStyleClassAndId()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Style, "padding:0.5rem");
            parameters.Add(p => p.Class, "header-switcher");
            parameters.Add(p => p.Id, "theme-switcher");
        });

        var root = component.Find(".bit-ths");
        Assert.IsTrue(root.ClassList.Contains("header-switcher"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("padding:0.5rem"));
        Assert.AreEqual("theme-switcher", root.Id);
    }

    [TestMethod]
    public void BitThemeSwitcherShouldHideWhenCollapsed()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        Assert.IsTrue(component.Find(".bit-ths").GetAttribute("style")!.Contains("display:none"));
    }

    [TestMethod]
    public void BitThemeSwitcherShouldKeepItsPlaceWhenHidden()
    {
        RegisterServices();

        var component = RenderComponent<BitThemeSwitcher>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
            parameters.Add(p => p.Style, "gap:2rem");
        });

        var style = component.Find(".bit-ths").GetAttribute("style")!;
        StringAssert.Contains(style, "visibility:hidden");
        StringAssert.Contains(style, "gap:2rem");
    }

    #endregion

    #region Splatted attributes and prerendering

    [TestMethod]
    public void BitThemeSwitcherShouldSplatTheHtmlAttributesOnTheRoot()
    {
        RegisterServices();

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitThemeSwitcher>(0);
            builder.AddAttribute(1, "data-test", "root");
            builder.AddAttribute(2, "lang", "fa");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ths");

        Assert.AreEqual("root", root.GetAttribute("data-test"));
        Assert.AreEqual("fa", root.GetAttribute("lang"));
        // The attributes are the root's alone.
        Assert.IsNull(component.Find(".bit-ths-lgt").GetAttribute("data-test"));
        Assert.IsNull(component.Find(".bit-ths-dds").GetAttribute("data-test"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BitThemeSwitcherShouldDropTheSplattedActivationHandlersWhileDisabled(bool disabled)
    {
        RegisterServices();
        var clicks = 0;
        var hovers = 0;

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitThemeSwitcher>(0);
            builder.AddAttribute(1, nameof(BitThemeSwitcher.Disabled), disabled);
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++));
            builder.AddAttribute(3, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () => hovers++));
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ths");

        // A click of a scheme button bubbles up to the root, and a screen reader dispatches one to a disabled button
        // whatever its pointer-events say; a hover only follows the pointer and is kept.
        Assert.AreEqual(disabled is false, root.HasAttribute("blazor:onclick"));
        Assert.IsTrue(root.HasAttribute("blazor:onmouseover"));

        root.MouseOver();

        Assert.AreEqual(1, hovers);
        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public async Task BitThemeSwitcherShouldPrerenderTheInitialDesignSystemAndBothSchemeButtons()
    {
        // No OnAfterRender runs before the page is interactive, so the applied theme is never read back: the picker shows
        // what InitialTheme names, and both scheme buttons are in the markup for the stylesheet to pick between.
        var html = await Prerenderer.RenderAsync<BitThemeSwitcher>(new Dictionary<string, object?>
        {
            [nameof(BitThemeSwitcher.InitialTheme)] = BitThemePresets.MaterialDark,
        });

        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument($"<!DOCTYPE html><html><body>{html}</body></html>");

        Assert.AreEqual("Material", document.QuerySelector(".bit-ths-dds .bit-drp-tdp")!.TextContent.Trim());
        Assert.AreEqual(1, document.QuerySelectorAll(".bit-ths-drk").Length);
        Assert.AreEqual(1, document.QuerySelectorAll(".bit-ths-lgt").Length);
        Assert.AreEqual("group", document.QuerySelector(".bit-ths")!.GetAttribute("role"));
    }

    [TestMethod]
    public async Task BitThemeSwitcherShouldPrerenderOnlyTheRequestedParts()
    {
        var html = await Prerenderer.RenderAsync<BitThemeSwitcher>(new Dictionary<string, object?>
        {
            [nameof(BitThemeSwitcher.NoDesignSystem)] = true,
        });

        Assert.IsFalse(html.Contains("bit-ths-dds"));
        StringAssert.Contains(html, "bit-ths-csb");
    }

    #endregion
}
