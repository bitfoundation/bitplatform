using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

/// <summary>
/// Covers the BitParams cascade of the accent color switcher: what a BitAccentColorSwitcherParams carries (the look of
/// the parts and the app-wide Config, which every switcher is to be handed alike), that the switcher takes it, and that
/// what is set on the switcher itself wins over it.
/// </summary>
[TestClass]
public class BitAccentColorSwitcherParamsTests : BunitTestContext
{
    // What makes one switcher the one it is: the values it is cascaded and its callback.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAccentColorSwitcher.CascadingParameters),
        nameof(BitAccentColorSwitcher.OnChange),
    ];

    private static readonly BitAccentColorConfig _twoAccents = new()
    {
        Accents =
        [
            new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" },
            new BitAccentColorItem { Name = "Indigo", Color = "#4B0082" },
        ]
    };

    private IRenderedComponent<BitParams> RenderWithParams(BitAccentColorSwitcherParams switcherParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        Context.Services.AddBitBlazorUIExtrasServices();

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { switcherParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAccentColorSwitcher>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAccentColorSwitcherParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAccentColorSwitcher", BitAccentColorSwitcherParams.ParamName);
    }

    [TestMethod]
    public void BitAccentColorSwitcherParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitAccentColorSwitcherParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitAccentColorSwitcherParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitAccentColorSwitcherParamsShouldCarryEveryParameterButTheCallback()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAccentColorSwitcher).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                       .Select(p => p.Name)
                                                       .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                       .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAccentColorSwitcherParams).GetProperty(name), $"BitAccentColorSwitcherParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldTakeTheCascadedConfig()
    {
        var component = RenderWithParams(new BitAccentColorSwitcherParams { Config = _twoAccents });

        var swatches = component.FindAll(".bit-acs-swt");

        Assert.AreEqual(2, swatches.Count);
        Assert.AreEqual("Crimson", swatches[0].GetAttribute("title"));
        Assert.AreEqual("Indigo", swatches[1].GetAttribute("title"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldTakeTheCascadedLookOfItsParts()
    {
        var component = RenderWithParams(new BitAccentColorSwitcherParams
        {
            Class = "cascaded-root",
            Dir = BitDir.Rtl,
            Classes = new() { Root = "cascaded-classes-root", Swatch = "cascaded-swatch" },
            Styles = new() { Root = "margin: 1px", Swatch = "padding: 2px" },
        });

        var root = component.Find(".bit-acs");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-classes-root"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");

        foreach (var swatch in component.FindAll(".bit-acs-swt"))
        {
            Assert.IsTrue(swatch.ClassList.Contains("cascaded-swatch"));
            StringAssert.Contains(swatch.GetAttribute("style"), "padding: 2px");
        }
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldTakeACascadedDisabled()
    {
        var component = RenderWithParams(new BitAccentColorSwitcherParams { Disabled = true });

        Assert.IsTrue(component.Find(".bit-acs").ClassList.Contains("bit-dis"));
        Assert.IsTrue(component.FindAll(".bit-acs-swt").All(s => s.HasAttribute("disabled") && s.GetAttribute("aria-disabled") == "true"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var own = new BitAccentColorConfig { Accents = [new BitAccentColorItem { Name = "Teal", Color = "#008080" }] };

        var component = RenderWithParams(new BitAccentColorSwitcherParams
        {
            Config = _twoAccents,
            Classes = new() { Swatch = "cascaded-swatch" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitAccentColorSwitcher.Config), own);
            builder.AddAttribute(2, nameof(BitAccentColorSwitcher.Classes), new BitAccentColorSwitcherClassStyles { Swatch = "own-swatch" });
        });

        var swatch = component.Find(".bit-acs-swt");

        Assert.AreEqual(1, component.FindAll(".bit-acs-swt").Count);
        Assert.AreEqual("Teal", swatch.GetAttribute("title"));
        Assert.IsTrue(swatch.ClassList.Contains("own-swatch"));
        Assert.IsFalse(swatch.ClassList.Contains("cascaded-swatch"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitAccentColorSwitcherParams());

        var root = component.Find(".bit-acs");

        Assert.AreEqual(BitAccentColorSwitcher.DefaultAccents.Count, component.FindAll(".bit-acs-swt").Count);
        Assert.AreEqual("bit-acs", root.GetAttribute("class")!.Trim());
        Assert.IsTrue(string.IsNullOrEmpty(root.GetAttribute("style")));
        Assert.IsNull(root.GetAttribute("dir"));
    }
}
