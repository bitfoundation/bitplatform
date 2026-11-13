using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ImageViewer;

/// <summary>
/// The BitParams cascade of the image viewer: what a BitImageViewerParams carries (what the image viewers of an app agree
/// on), that the image viewer takes it, and that what is set on the image viewer itself wins over it.
/// </summary>
[TestClass]
public class BitImageViewerParamsTests : BunitTestContext
{
    // What makes one image viewer the one it is: its image and its zoom.
    private static readonly string[] _notCascaded =
    [
        nameof(BitImageViewer.CascadingParameters),
        nameof(BitImageViewer.Alt),
        nameof(BitImageViewer.Src),
        nameof(BitImageViewer.Zoom),
        nameof(BitImageViewer.ZoomChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitImageViewerParams viewerParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { viewerParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitImageViewer>(0);
                builder.AddComponentParameter(1, nameof(BitImageViewer.Src), "/photo.jpg");
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitImageViewerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitImageViewer", BitImageViewerParams.ParamName);
        Assert.AreEqual(BitImageViewerParams.ParamName, new BitImageViewerParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitImageViewerParams());
    }

    [TestMethod]
    public void BitImageViewerParamsShouldCarryEveryParameterButTheStateOfOneViewer()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitImageViewer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                               .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitImageViewerParams).GetProperty(name), $"BitImageViewerParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitImageViewerParams).GetProperty(name), $"BitImageViewerParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitImageViewerShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitImageViewerParams
        {
            Class = "cascaded-root",
            DoubleClickZoom = 3,
            Height = "12rem",
            MaxZoom = 2,
            MinZoom = 0.5,
            NoToolbar = false,
            WheelZoom = true,
            ZoomFactor = 2,
            Texts = new() { ZoomIn = "Closer" },
            Classes = new() { Viewport = "cascaded-vpt" },
            Styles = new() { Toolbar = "margin:1px" },
        });

        var instance = component.FindComponent<BitImageViewer>().Instance;

        Assert.IsTrue(component.Find(".bit-imv").ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-imv-vpt").ClassList.Contains("cascaded-vpt"));
        Assert.AreEqual("height:12rem", component.Find(".bit-imv-vpt").GetAttribute("style"));
        Assert.AreEqual("margin:1px", component.Find(".bit-imv-tbr").GetAttribute("style"));
        Assert.AreEqual("Closer", component.Find(".bit-imv-btn:nth-of-type(2)").GetAttribute("aria-label"));
        Assert.AreEqual(3, instance.DoubleClickZoom);
        Assert.AreEqual(2, instance.MaxZoom);
        Assert.AreEqual(0.5, instance.MinZoom);
        Assert.IsTrue(instance.WheelZoom);
        Assert.AreEqual(2, instance.ZoomFactor);
    }

    [TestMethod]
    public void BitImageViewerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitImageViewerParams { NoToolbar = true, MaxZoom = 2 }, builder =>
        {
            builder.AddComponentParameter(2, nameof(BitImageViewer.NoToolbar), false);
        });

        var instance = component.FindComponent<BitImageViewer>().Instance;

        Assert.IsFalse(instance.NoToolbar);
        Assert.AreEqual(1, component.FindAll(".bit-imv-tbr").Count);
        Assert.AreEqual(2, instance.MaxZoom, "the cascaded value is taken where the viewer sets none");
    }
}
