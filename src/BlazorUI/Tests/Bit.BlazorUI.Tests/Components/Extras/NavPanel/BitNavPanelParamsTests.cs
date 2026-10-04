using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NavPanel;

/// <summary>
/// Covers the BitParams cascade of the NavPanel: what a BitNavPanelParams fills in, what it leaves alone because
/// the panel wrote it for itself, and that what the panel derives from its parameters (its classes, its styles,
/// its controls and their names) follows the cascaded values.
/// </summary>
[TestClass]
public class BitNavPanelParamsTests : BunitTestContext
{
    // What belongs to a single panel rather than to a group of them: its items, the state it two-way binds, the
    // initial selection, the templates and the selectors typed over its items, and its event callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitNavPanel<BitNavItem>.CascadingParameters),
        nameof(BitNavPanel<BitNavItem>.DefaultSelectedItem),
        nameof(BitNavPanel<BitNavItem>.HeaderTemplate),
        nameof(BitNavPanel<BitNavItem>.IsOpen),
        nameof(BitNavPanel<BitNavItem>.IsOpenChanged),
        nameof(BitNavPanel<BitNavItem>.IsToggled),
        nameof(BitNavPanel<BitNavItem>.IsToggledChanged),
        nameof(BitNavPanel<BitNavItem>.Items),
        nameof(BitNavPanel<BitNavItem>.ItemTemplate),
        nameof(BitNavPanel<BitNavItem>.NameSelectors),
        nameof(BitNavPanel<BitNavItem>.OnItemClick),
        nameof(BitNavPanel<BitNavItem>.OnItemToggle),
        nameof(BitNavPanel<BitNavItem>.OnSearch),
        nameof(BitNavPanel<BitNavItem>.OnSelectItem),
        nameof(BitNavPanel<BitNavItem>.SearchFilter),
        nameof(BitNavPanel<BitNavItem>.SearchText),
        nameof(BitNavPanel<BitNavItem>.SearchTextChanged),
        nameof(BitNavPanel<BitNavItem>.SelectedItem),
        nameof(BitNavPanel<BitNavItem>.SelectedItemChanged),
    ];

    private static List<BitNavItem> GetItems() =>
    [
        new() { Text = "Home", Url = "/home", IconName = "Home" },
        new() { Text = "Docs", Url = "/docs", IconName = "Document" },
        new() { Text = "Settings", IconName = "Settings" },
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitNavPanelParams panelParams, Action<RenderTreeBuilder>? extraAttributes = null, List<BitNavItem>? items = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { panelParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNavPanel<BitNavItem>>(0);
                builder.AddAttribute(1, nameof(BitNavPanel<BitNavItem>.Items), items ?? GetItems());
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitNavPanelParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitNavPanel", BitNavPanelParams.ParamName);
    }

    [TestMethod]
    public void BitNavPanelParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitNavPanelParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitNavPanelParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitNavPanelParamsShouldCarryEveryParameterThatBelongsToAGroupOfPanels()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitNavPanel<BitNavItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                        .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                        .Select(p => p.Name)
                                                        .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitNavPanelParams).GetProperty(name), $"BitNavPanelParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitNavPanelParamsShouldOnlyCarryParametersOfThePanel()
    {
        var own = typeof(BitNavPanelParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                           .Select(p => p.Name)
                                           .Where(n => n is not nameof(IBitComponentParams.Name));

        foreach (var name in own)
        {
            var target = typeof(BitNavPanel<BitNavItem>).GetProperty(name);

            Assert.IsNotNull(target?.GetCustomAttribute<ParameterAttribute>(), $"BitNavPanel has no {name} parameter.");
        }
    }

    [TestMethod]
    public void BitNavPanelShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitNavPanelParams
        {
            FitWidth = true,
            StickyEnds = true,
            NoPad = true,
            Position = BitNavPanelPosition.End,
            Width = 260,
            ToggledWidth = 72,
            Top = 10,
            ShowCloseButton = true,
            CloseAriaLabel = "Schließen",
            ToggleAriaLabel = "Umschalten",
            SearchBoxPlaceholder = "Suchen",
            IconUrl = "/logo.png",
            IconAriaLabel = "Startseite",
            Class = "cascaded",
            Classes = new() { Container = "cascaded-container" },
            Styles = new() { Root = "margin:1px" },
        });

        var root = component.Find(".bit-npn");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-fiw"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-ste"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-npd"));
        Assert.IsTrue(root.ClassList.Contains("bit-npn-end"));

        var style = root.GetAttribute("style");

        StringAssert.Contains(style, "margin:1px");
        StringAssert.Contains(style, "--bit-npn-w:260px");
        StringAssert.Contains(style, "--bit-npn-tw:72px");
        StringAssert.Contains(style, "top:10px");

        Assert.AreEqual(1, component.FindAll(".bit-npn-cnt.cascaded-container").Count);
        Assert.AreEqual("Schließen", component.Find(".bit-npn-cbn").GetAttribute("aria-label"));
        Assert.AreEqual("Umschalten", component.Find(".bit-npn-tbn").GetAttribute("aria-label"));
        Assert.AreEqual("Suchen", component.Find(".bit-srb input").GetAttribute("placeholder"));
        Assert.AreEqual("Startseite", component.Find(".bit-npn-img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitNavPanelShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitNavPanelParams
        {
            FitWidth = true,
            Width = 260,
            NoSearchBox = true,
            ToggleAriaLabel = "Cascaded",
            ToggleIcon = BitIconInfo.Css("cascaded-icon"),
            Class = "cascaded",
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.FitWidth), false);
            builder.AddAttribute(3, nameof(BitNavPanel<BitNavItem>.Width), 200);
            builder.AddAttribute(4, nameof(BitNavPanel<BitNavItem>.NoSearchBox), false);
            builder.AddAttribute(5, nameof(BitNavPanel<BitNavItem>.ToggleAriaLabel), "Own");
            builder.AddAttribute(6, nameof(BitNavPanel<BitNavItem>.ToggleIconName), "GlobalNavButton");
            builder.AddAttribute(7, nameof(BitComponentBase.Class), "own");
        });

        var root = component.Find(".bit-npn");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsFalse(root.ClassList.Contains("bit-npn-fiw"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-npn-w:200px");

        Assert.AreEqual(1, component.FindAll(".bit-srb").Count);
        Assert.AreEqual("Own", component.Find(".bit-npn-tbn").GetAttribute("aria-label"));

        // A cascaded icon is only a default for a panel that names neither the icon nor the icon name.
        Assert.AreEqual(0, component.FindAll(".cascaded-icon").Count);
        Assert.AreEqual(1, component.FindAll(".bit-npn-tbn .bit-icon--GlobalNavButton").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldTakeACascadedIconWhenItNamesNone()
    {
        var component = RenderWithParams(new BitNavPanelParams
        {
            ToggleIcon = BitIconInfo.Css("cascaded-toggle"),
            ChevronDownIconName = "Add",
        });

        Assert.AreEqual(1, component.FindAll(".bit-npn-tbn .cascaded-toggle").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldTakeACascadedNavModeBeforeItsInitialSelection()
    {
        var items = GetItems();

        // The initial selection is made in the first pass of the panel, which runs before OnParametersSet, so
        // the cascaded NavMode it depends on has to be applied by then.
        var component = RenderWithParams(new BitNavPanelParams { NavMode = BitNavMode.Manual }, builder =>
        {
            builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.DefaultSelectedItem), items[2]);
        }, items);

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count));
        Assert.IsTrue(component.Find(".bit-nav-sel").TextContent.Contains("Settings"));
    }

    [TestMethod]
    public void BitNavPanelShouldTakeACascadedSearchAnnouncementProvider()
    {
        var component = RenderWithParams(new BitNavPanelParams
        {
            SearchAnnouncementProvider = count => $"{count} Treffer",
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitNavPanel<BitNavItem>.SearchText), "Docs");
        });

        component.WaitForAssertion(() => Assert.AreEqual("1 Treffer", component.Find(".bit-npn-lvr").TextContent));
    }

    [TestMethod]
    public void BitNavPanelShouldTakeACascadedHeaderAndFooter()
    {
        var component = RenderWithParams(new BitNavPanelParams
        {
            Header = builder => builder.AddMarkupContent(0, "<div class=\"cascaded-header\">Header</div>"),
            Footer = builder => builder.AddMarkupContent(0, "<div class=\"cascaded-footer\">Footer</div>"),
        });

        Assert.AreEqual(1, component.FindAll(".cascaded-header").Count);
        Assert.AreEqual(1, component.FindAll(".cascaded-footer").Count);
        Assert.AreEqual(0, component.FindAll(".bit-npn-hdr").Count);
    }

    [TestMethod]
    public void BitNavPanelShouldGoBackToItsOwnValuesWhenTheCascadeStopsSettingThem()
    {
        var component = RenderWithParams(new BitNavPanelParams { FitWidth = true, Width = 260 });

        Assert.IsTrue(component.Find(".bit-npn").ClassList.Contains("bit-npn-fiw"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitNavPanelParams() });
        });

        var root = component.Find(".bit-npn");

        Assert.IsFalse(root.ClassList.Contains("bit-npn-fiw"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("--bit-npn-w"));
    }
}
