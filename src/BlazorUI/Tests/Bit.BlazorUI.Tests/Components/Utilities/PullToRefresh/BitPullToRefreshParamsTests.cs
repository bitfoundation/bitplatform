using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.PullToRefresh;

/// <summary>
/// Covers the BitParams cascade of the PullToRefresh: what a BitPullToRefreshParams fills in, what it leaves alone
/// because the component wrote it for itself, and that the gesture numbers it fills in are the ones js receives.
/// </summary>
[TestClass]
public class BitPullToRefreshParamsTests : BunitTestContext
{
    // What a pull to refresh wraps, what it scrolls and what it reports back are written per instance, so they have
    // no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitPullToRefresh.CascadingParameters),
        nameof(BitPullToRefresh.Anchor),
        nameof(BitPullToRefresh.ChildContent),
        nameof(BitPullToRefresh.ScrollerElement),
        nameof(BitPullToRefresh.ScrollerSelector),
        nameof(BitPullToRefresh.OnRefresh),
        nameof(BitPullToRefresh.OnPullStart),
        nameof(BitPullToRefresh.OnPullMove),
        nameof(BitPullToRefresh.OnPullEnd),
        nameof(BitPullToRefresh.OnPullCancel),
        nameof(BitPullToRefresh.OnStateChange),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitPullToRefreshParams ptrParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { ptrParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPullToRefresh>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitPullToRefreshParamsShouldHaveCorrectParamName()
    {
        var @params = new BitPullToRefreshParams();

        Assert.AreEqual("BitParams.BitPullToRefresh", new BitPullToRefreshParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitPullToRefreshParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitPullToRefreshParamsShouldCarryEveryParameterThatIsNotPerInstance()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitPullToRefresh).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitPullToRefreshParams).GetProperty(name), $"BitPullToRefreshParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitPullToRefreshParams
        {
            FullWidth = true,
            Color = BitColor.Error,
            Classes = new() { Root = "cascaded-root", Loading = "cascaded-loading" },
            Styles = new() { Root = "margin: 1px;", SpinnerWrapper = "padding: 2px;" },
        });

        var root = component.Find(".bit-ptr");
        Assert.IsTrue(root.ClassList.Contains("bit-ptr-flw"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.Contains("--bit-ptr-color:var(--bit-clr-err)", root.GetAttribute("style")!);
        Assert.Contains("margin: 1px;", root.GetAttribute("style")!);

        Assert.IsTrue(component.Find(".bit-ptr-lod").ClassList.Contains("cascaded-loading"));
        Assert.Contains("padding: 2px", component.Find(".bit-ptr-spw").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCascadedCustomColor()
    {
        var component = RenderWithParams(new BitPullToRefreshParams { CustomColor = "#b400ff" });

        Assert.Contains("--bit-ptr-color:#b400ff", component.Find(".bit-ptr").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitPullToRefreshShouldPassCascadedGestureNumbersToJs()
    {
        var component = RenderWithParams(new BitPullToRefreshParams
        {
            Trigger = 120,
            Factor = 2m,
            Margin = 10,
            Threshold = 5,
            MaxPull = 160,
            NoMouse = true,
            Direction = BitPullToRefreshDirection.Up,
        });

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.PullToRefresh.setup");

        Assert.AreEqual(120, setup.Arguments[5]);
        Assert.AreEqual(2m, setup.Arguments[6]);
        Assert.AreEqual(10, setup.Arguments[7]);
        Assert.AreEqual(5, setup.Arguments[8]);
        Assert.AreEqual(160, setup.Arguments[9]);
        Assert.IsTrue(Assert.IsInstanceOfType<bool>(setup.Arguments[11]));
        Assert.AreEqual(BitPullToRefreshDirection.Up, setup.Arguments[12]);

        // The cascaded direction also turns the strip, which the stylesheet reads off the root's class.
        Assert.IsTrue(component.Find(".bit-ptr").ClassList.Contains("bit-ptr-up"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCascadedLabelsAndTemplates()
    {
        var component = RenderWithParams(new BitPullToRefreshParams
        {
            ReleaseLabel = "Let go",
            Release = b => b.AddMarkupContent(0, "<b class=\"cascaded-release\">!</b>"),
        });

        var ptr = component.FindComponent<BitPullToRefresh>();
        ptr.InvokeAsync(() => ptr.Instance._OnMove(80m)).GetAwaiter().GetResult();

        Assert.AreEqual("Let go", component.Find(".bit-ptr-vhd").TextContent);
        Assert.IsNotNull(component.Find(".bit-ptr-spn .cascaded-release"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyACascadedIndicatorTemplate()
    {
        var component = RenderWithParams(new BitPullToRefreshParams
        {
            IndicatorTemplate = ctx => b => b.AddMarkupContent(0, $"<span class=\"cascaded-indicator\">{ctx.State}</span>"),
        });

        var ptr = component.FindComponent<BitPullToRefresh>();
        ptr.InvokeAsync(() => ptr.Instance._OnMove(80m)).GetAwaiter().GetResult();

        Assert.AreEqual("CanRelease", component.Find(".bit-ptr-ind .cascaded-indicator").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-ptr-spw"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCascadedCompleteDelay()
    {
        var component = RenderWithParams(new BitPullToRefreshParams { CompleteDelay = 5000, CompleteLabel = "Done" });

        var ptr = component.FindComponent<BitPullToRefresh>();

        Assert.AreEqual(5000, ptr.Instance.CompleteDelay);
        Assert.AreEqual("Done", ptr.Instance.CompleteLabel);
    }

    [TestMethod]
    public void BitPullToRefreshShouldLetAnEmptyCascadedReleaseLabelSilenceTheReleaseState()
    {
        var component = RenderWithParams(new BitPullToRefreshParams { ReleaseLabel = string.Empty });

        var ptr = component.FindComponent<BitPullToRefresh>();
        ptr.InvokeAsync(() => ptr.Instance._OnMove(80m)).GetAwaiter().GetResult();

        Assert.AreEqual(string.Empty, component.Find(".bit-ptr-vhd").TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitPullToRefreshParams { Dir = BitDir.Rtl, Disabled = true, AriaLabel = "Messages" });

        var root = component.Find(".bit-ptr");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("Messages", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitPullToRefreshParams
        {
            Trigger = 120,
            Color = BitColor.Error,
            FullWidth = true,
            ReleaseLabel = "Cascaded",
            Direction = BitPullToRefreshDirection.Up,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPullToRefresh.Trigger), 60);
            builder.AddAttribute(2, nameof(BitPullToRefresh.Color), (BitColor?)BitColor.Success);
            builder.AddAttribute(3, nameof(BitPullToRefresh.FullWidth), false);
            builder.AddAttribute(4, nameof(BitPullToRefresh.ReleaseLabel), "Own");
            builder.AddAttribute(5, nameof(BitPullToRefresh.Direction), BitPullToRefreshDirection.Down);
        });

        var root = component.Find(".bit-ptr");
        Assert.Contains("--bit-ptr-color:var(--bit-clr-suc)", root.GetAttribute("style")!);
        Assert.IsFalse(root.ClassList.Contains("bit-ptr-flw"));
        Assert.IsFalse(root.ClassList.Contains("bit-ptr-up"));

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.PullToRefresh.setup");
        Assert.AreEqual(60, setup.Arguments[5]);
        Assert.AreEqual(BitPullToRefreshDirection.Down, setup.Arguments[12]);

        var ptr = component.FindComponent<BitPullToRefresh>();
        ptr.InvokeAsync(() => ptr.Instance._OnMove(60m)).GetAwaiter().GetResult();
        Assert.AreEqual("Own", component.Find(".bit-ptr-vhd").TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitPullToRefreshParams());

        var root = component.Find(".bit-ptr");
        Assert.AreEqual("bit-ptr", root.GetAttribute("class"));
        Assert.IsNull(root.GetAttribute("style"));

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.PullToRefresh.setup");
        Assert.AreEqual(80, setup.Arguments[5]);
        Assert.AreEqual(1.5m, setup.Arguments[6]);
        Assert.IsFalse(Assert.IsInstanceOfType<bool>(setup.Arguments[11]));
    }
}
