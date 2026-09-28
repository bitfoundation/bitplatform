using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Progress;

/// <summary>
/// Covers the BitParams cascade of the Progress: what a BitProgressParams fills in, and what it leaves alone because
/// the progress wrote it for itself.
/// </summary>
[TestClass]
public class BitProgressParamsTests : BunitTestContext
{
    private IRenderedComponent<BitParams> RenderWithParams(BitProgressParams progressParams, System.Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { progressParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitProgress>(0);
                builder.AddAttribute(1, nameof(BitProgress.Percent), 40d);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitProgressParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitProgress)}", BitProgressParams.ParamName);
        Assert.AreEqual("BitParams.BitProgress", BitProgressParams.ParamName);
    }

    [TestMethod]
    public void BitProgressParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitProgressParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitProgressParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitProgressParamsShouldCarryEveryParameterButThePerInstanceOnes()
    {
        // What each indicator reports on its own - its value, the words naming it and the templates - is not a
        // group default; everything else is.
        string[] perInstance =
        [
            nameof(BitProgress.Percent), nameof(BitProgress.Value), nameof(BitProgress.Buffer),
            nameof(BitProgress.Label), nameof(BitProgress.LabelTemplate),
            nameof(BitProgress.Description), nameof(BitProgress.DescriptionTemplate),
            nameof(BitProgress.PercentNumberTemplate), nameof(BitProgress.AriaValueText),
        ];

        var parameters = typeof(BitProgress).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(p => p.DeclaringType == typeof(BitProgress))
                                            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                            .Select(p => p.Name)
                                            .Except(perInstance)
                                            .ToArray();

        var carried = typeof(BitProgressParams).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Select(p => p.Name)
                                               .ToHashSet();

        var missing = parameters.Where(p => carried.Contains(p) is false).ToArray();

        Assert.AreEqual(0, missing.Length, $"Missing from BitProgressParams: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void BitProgressShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Rounded = true,
            Reversed = true,
            Segments = 4,
            Delay = 300,
            Class = "cascaded-class",
            AriaLabel = "Cascaded progress",
        });

        var root = component.Find(".bit-prb");

        Assert.IsTrue(root.ClassList.Contains("bit-prb-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-rev"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-seg"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-dly"));
        Assert.Contains("--bit-prb-delay: 300ms;", root.GetAttribute("style")!);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.AreEqual("Cascaded progress", component.Find("[role=progressbar]").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheShape()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            Circular = true,
            GapDegree = 90,
            GapPosition = BitProgressGapPosition.Top,
            Diameter = 64,
            Thickness = 6,
        });

        var root = component.Find(".bit-prb");
        var svg = component.Find("svg.bit-prb-cir");

        Assert.IsTrue(root.ClassList.Contains("bit-prb-gap"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-gpt"));
        Assert.AreEqual("64px", svg.GetAttribute("width"));
        Assert.Contains("stroke-width: min(6px, 20%)", component.Find(".bit-prb-cbr").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheVerticalBar()
    {
        var component = RenderWithParams(new BitProgressParams { Vertical = true, Length = "6rem" });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains("bit-prb-ver"));
        Assert.Contains("height: 6rem", component.Find(".bit-prb-bcn").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheScaleAndTheRole()
    {
        var component = RenderWithParams(new BitProgressParams { Min = 10, Max = 20, Meter = true }, builder =>
        {
            builder.AddAttribute(2, nameof(BitProgress.Value), (double?)15);
        });

        var meter = component.Find("[role=meter]");

        Assert.AreEqual("10", meter.GetAttribute("aria-valuemin"));
        Assert.AreEqual("20", meter.GetAttribute("aria-valuemax"));
        Assert.AreEqual("15", meter.GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheReadout()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            ShowPercentNumber = true,
            PercentNumberFormat = "{0:F1}",
            PercentNumberPosition = BitProgressPercentPosition.Start,
        });

        var readout = component.Find(".bit-prb-pct");

        Assert.IsTrue(readout.ClassList.Contains("bit-prb-pcs"));
        Assert.AreEqual(string.Format("{0:F1}", 40d), readout.TextContent.Trim());
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheLook()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            Striped = true,
            StripedAnimation = true,
            BarColor = "tomato",
            TrackColor = "wheat",
        });

        var bar = component.Find(".bit-prb-bar");
        var style = component.Find(".bit-prb").GetAttribute("style")!;

        Assert.IsTrue(bar.ClassList.Contains("bit-prb-stp"));
        Assert.IsTrue(bar.ClassList.Contains("bit-prb-sta"));
        Assert.Contains("--bit-prb-bar-color: tomato", style);
        Assert.Contains("--bit-prb-track-color: wheat", style);
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheIndeterminateState()
    {
        var component = RenderWithParams(new BitProgressParams { Indeterminate = true });

        Assert.IsTrue(component.Find(".bit-prb-bar").ClassList.Contains("bit-prb-ind"));
        Assert.IsNull(component.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyTheAnnouncements()
    {
        var component = RenderWithParams(new BitProgressParams { AnnounceProgress = true, AnnounceStep = 10 });

        Assert.AreEqual(1, component.FindAll(".bit-prb-lvr").Count);
    }

    [TestMethod]
    public void BitProgressParamsShouldApplyClassesAndStyles()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            Classes = new() { Root = "cascaded-root", Bar = "cascaded-bar" },
            Styles = new() { Root = "margin: 1px;", Track = "opacity: 0.5;" },
        });

        var root = component.Find(".bit-prb");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.Contains("margin: 1px", root.GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bit-prb-bar").ClassList.Contains("cascaded-bar"));
        Assert.Contains("opacity: 0.5", component.Find(".bit-prb-trc").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitProgressDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
            Rounded = true,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitProgress.Color), (BitColor?)BitColor.Info);
            builder.AddAttribute(3, nameof(BitProgress.Rounded), false);
        });

        var root = component.Find(".bit-prb");

        Assert.IsTrue(root.ClassList.Contains("bit-prb-inf"));
        Assert.IsFalse(root.ClassList.Contains("bit-prb-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-prb-rnd"));
        Assert.IsTrue(root.ClassList.Contains("bit-prb-lg"), "the parameters it left unset still come from the cascade");
    }

    [TestMethod]
    public void BitProgressShouldRestoreItsDefaultsWhenTheCascadeStopsSupplyingThem()
    {
        var component = RenderWithParams(new BitProgressParams
        {
            Rounded = true,
            Min = 10,
            Max = 20,
            AnnounceProgress = true,
            Size = BitSize.Large,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitProgress.Value), (double?)15);
            builder.AddAttribute(3, nameof(BitProgress.Size), (BitSize?)BitSize.Small);
        });

        Assert.IsTrue(component.Find(".bit-prb").ClassList.Contains("bit-prb-rnd"));
        Assert.AreEqual("10", component.Find("[role=progressbar]").GetAttribute("aria-valuemin"));
        Assert.AreEqual(1, component.FindAll(".bit-prb-lvr").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitProgressParams { Size = BitSize.Large } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitProgress>(0);
                builder.AddAttribute(1, nameof(BitProgress.Percent), 40d);
                builder.AddAttribute(2, nameof(BitProgress.Value), (double?)16);
                builder.AddAttribute(3, nameof(BitProgress.Size), (BitSize?)BitSize.Small);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-prb");
        var bar = component.Find("[role=progressbar]");

        Assert.IsFalse(root.ClassList.Contains("bit-prb-rnd"));
        Assert.AreEqual("0", bar.GetAttribute("aria-valuemin"));
        Assert.AreEqual("100", bar.GetAttribute("aria-valuemax"));
        Assert.AreEqual(0, component.FindAll(".bit-prb-lvr").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-prb-sm"), "a parameter the markup sets is left alone");
    }

    [TestMethod]
    public void BitProgressParamsShouldNotApplyOutsideTheCascade()
    {
        var component = RenderComponent<BitProgress>(parameters => parameters.Add(p => p.Percent, 40));

        var root = component.Find(".bit-prb");

        Assert.IsTrue(root.ClassList.Contains("bit-prb-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-prb-lg"));
    }
}
