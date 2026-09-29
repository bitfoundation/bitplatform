using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Splitter;

/// <summary>
/// Covers the BitParams cascade of the Splitter: what a BitSplitterParams fills in, and what it leaves alone because
/// the splitter wrote it for itself.
/// </summary>
[TestClass]
public class BitSplitterParamsTests : BunitTestContext
{
    private IRenderedComponent<BitParams> RenderWithParams(BitSplitterParams splitterParams, System.Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { splitterParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSplitter>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitSplitterParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSplitter)}", BitSplitterParams.ParamName);
        Assert.AreEqual("BitParams.BitSplitter", BitSplitterParams.ParamName);
    }

    [TestMethod]
    public void BitSplitterParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSplitterParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitSplitterParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSplitterParamsShouldCarryEveryParameterButThePerInstanceOnes()
    {
        // What each splitter holds for itself - its content, its position, its fold, the key it remembers them under
        // and its callbacks - is not a group default; everything else is.
        string[] perInstance =
        [
            nameof(BitSplitter.FirstPanel), nameof(BitSplitter.SecondPanel),
            nameof(BitSplitter.Percent), nameof(BitSplitter.Collapsed), nameof(BitSplitter.PersistKey),
            nameof(BitSplitter.CascadingParameters),
        ];

        var parameters = typeof(BitSplitter).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(p => p.DeclaringType == typeof(BitSplitter))
                                            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                            .Where(p => p.PropertyType.IsGenericType is false || p.PropertyType.GetGenericTypeDefinition() != typeof(EventCallback<>))
                                            .Where(p => p.PropertyType != typeof(EventCallback))
                                            .Select(p => p.Name)
                                            .Except(perInstance)
                                            .ToArray();

        var carried = typeof(BitSplitterParams).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Select(p => p.Name)
                                               .ToHashSet();

        var missing = parameters.Where(p => carried.Contains(p) is false).ToArray();

        Assert.AreEqual(0, missing.Length, $"Missing from BitSplitterParams: {string.Join(", ", missing)}");

        foreach (var name in perInstance)
        {
            Assert.IsFalse(carried.Contains(name), $"{name} is per instance, yet BitSplitterParams carries it.");
        }
    }

    [TestMethod]
    public void BitSplitterShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitSplitterParams
        {
            Vertical = true,
            ReadOnly = true,
            Collapsible = true,
            CollapseSecondPanel = true,
            Class = "cascaded-class",
            AriaLabel = "Cascaded splitter",
        });

        var root = component.Find(".bit-spl");

        Assert.IsTrue(root.ClassList.Contains("bit-spl-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-spl-rdo"));
        Assert.IsTrue(root.ClassList.Contains("bit-spl-cpb"));
        Assert.IsTrue(root.ClassList.Contains("bit-spl-cse"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));

        // Read-only takes the name off the separator along with the rest of its widget semantics, so the name is
        // checked on one that can still be moved.
        var movable = RenderWithParams(new BitSplitterParams { AriaLabel = "Cascaded splitter" });

        Assert.AreEqual("Cascaded splitter", movable.Find("[role=separator]").GetAttribute("aria-label"));
        Assert.AreEqual("horizontal", component.Find("[role=separator]").GetAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitSplitterParamsShouldApplyTheSizes()
    {
        var component = RenderWithParams(new BitSplitterParams
        {
            FirstPanelSize = 120,
            FirstPanelMinSize = 60,
            FirstPanelMaxSize = 300,
            SecondPanelSize = 200,
            SecondPanelMinSize = 80,
            SecondPanelMaxSize = 400,
            GutterSize = 12,
            GutterHitSize = 30,
            CollapsedSize = 8,
        });

        var style = component.Find(".bit-spl").GetAttribute("style")!;

        Assert.Contains("--bit-spl-fpn-size:120px", style);
        Assert.Contains("--bit-spl-fpn-min:60px", style);
        Assert.Contains("--bit-spl-fpn-max:300px", style);
        Assert.Contains("--bit-spl-spn-size:200px", style);
        Assert.Contains("--bit-spl-spn-min:80px", style);
        Assert.Contains("--bit-spl-spn-max:400px", style);
        Assert.Contains("--bit-spl-gtr-size:12px", style);
        Assert.Contains("--bit-spl-hit-size:30px", style);
        Assert.Contains("--bit-spl-col-size:8px", style);
    }

    [TestMethod]
    public void BitSplitterParamsShouldApplyTheBehavior()
    {
        var component = RenderWithParams(new BitSplitterParams
        {
            DefaultPercent = 35,
            DragStep = 20,
            KeyboardStep = 25,
            SnapSize = 40,
            LazyResize = true,
            NoResetOnDoubleClick = true,
            PersistInSessionStorage = true,
        });

        var splitter = component.FindComponent<BitSplitter>().Instance;

        Assert.AreEqual(35d, splitter.DefaultPercent);
        Assert.Contains("--bit-spl-fpn-size:35%", component.Find(".bit-spl").GetAttribute("style")!);
        Assert.AreEqual(20, splitter.DragStep);
        Assert.AreEqual(25, splitter.KeyboardStep);
        Assert.AreEqual(40, splitter.SnapSize);
        Assert.IsTrue(splitter.LazyResize);
        Assert.IsTrue(splitter.NoResetOnDoubleClick);
        Assert.IsTrue(splitter.PersistInSessionStorage);
    }

    [TestMethod]
    public void BitSplitterParamsShouldApplyTheGutterAndTheCollapseButton()
    {
        var component = RenderWithParams(new BitSplitterParams
        {
            Collapsible = true,
            ShowCollapseButton = true,
            CollapseIconName = "ClosePane",
            GutterIconName = "GripperDotsVertical",
            Classes = new() { Gutter = "cascaded-gutter" },
            Styles = new() { Root = "outline: 1px solid red", FirstPanel = "color: red" },
        });

        Assert.IsTrue(component.Find(".bit-spl-cbi").ClassList.Contains("bit-icon--ClosePane"));
        Assert.IsTrue(component.Find(".bit-spl-gic").ClassList.Contains("bit-icon--GripperDotsVertical"));
        Assert.IsTrue(component.Find(".bit-spl-gtr").ClassList.Contains("cascaded-gutter"));
        Assert.Contains("outline: 1px solid red", component.Find(".bit-spl").GetAttribute("style")!);
        Assert.AreEqual("color: red", component.Find(".bit-spl-fpn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitSplitterParamsShouldApplyTheGutterTemplate()
    {
        RenderFragment template = builder => builder.AddMarkupContent(0, "<span class=\"cascaded-grip\"></span>");

        var component = RenderWithParams(new BitSplitterParams { GutterTemplate = template });

        Assert.AreEqual(1, component.FindAll(".bit-spl-gtr .cascaded-grip").Count);
        Assert.AreEqual(0, component.FindAll(".bit-spl-gti").Count);
    }

    [TestMethod]
    public void BitSplitterShouldKeepItsOwnParametersOverTheCascade()
    {
        var component = RenderWithParams(new BitSplitterParams
        {
            Vertical = true,
            GutterSize = 12,
            KeyboardStep = 25,
            Class = "cascaded-class",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSplitter.Vertical), false);
            builder.AddAttribute(2, nameof(BitSplitter.GutterSize), 4);
            builder.AddAttribute(3, nameof(BitSplitter.KeyboardStep), 5);
            builder.AddAttribute(4, nameof(BitSplitter.Class), "own-class");
        });

        var root = component.Find(".bit-spl");
        var splitter = component.FindComponent<BitSplitter>().Instance;

        Assert.IsFalse(root.ClassList.Contains("bit-spl-vrt"));
        Assert.IsTrue(root.ClassList.Contains("own-class"));
        Assert.IsFalse(root.ClassList.Contains("cascaded-class"));
        Assert.Contains("--bit-spl-gtr-size:4px", root.GetAttribute("style")!);
        Assert.AreEqual(5, splitter.KeyboardStep);
    }

    [TestMethod]
    public void BitSplitterShouldIgnoreParamsOfAnotherComponent()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitActionButtonParams { Class = "button-class" } });
            parameters.AddChildContent<BitSplitter>();
        });

        Assert.IsFalse(component.Find(".bit-spl").ClassList.Contains("button-class"));
    }
}
