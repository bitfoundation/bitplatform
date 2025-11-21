using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.PullToRefresh;

[TestClass]
public class BitPullToRefreshTests : BunitTestContext
{
    [TestMethod]
    public void BitPullToRefreshShouldRenderStructure()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        var root = component.Find(".bit-ptr");
        Assert.IsNotNull(root);

        var loading = component.Find(".bit-ptr-lod");
        var spinnerWrapper = component.Find(".bit-ptr-spw");
        var spinner = component.Find(".bit-ptr-spn");

        Assert.IsNotNull(loading);
        Assert.IsNotNull(spinnerWrapper);
        Assert.IsNotNull(spinner);
    }

    [TestMethod]
    public void BitPullToRefreshShouldInvokeOnRefresh()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var refreshed = false;
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => refreshed = true));
        });

        component.Instance._Refresh().GetAwaiter().GetResult();

        Assert.IsTrue(refreshed);
    }

    [TestMethod]
    public void BitPullToRefreshShouldInvokePullCallbacks()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        BitPullToRefreshPullStartArgs? startArgs = null;
        decimal moveDiff = 0;
        decimal endDiff = 0;

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnPullStart, EventCallback.Factory.Create<BitPullToRefreshPullStartArgs>(this, args => startArgs = args));
            parameters.Add(p => p.OnPullMove, EventCallback.Factory.Create<decimal>(this, diff => moveDiff = diff));
            parameters.Add(p => p.OnPullEnd, EventCallback.Factory.Create<decimal>(this, diff => endDiff = diff));
        });

        component.Instance._OnStart(10m, 20m, 100m).GetAwaiter().GetResult();
        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        component.Instance._OnEnd(60m).GetAwaiter().GetResult();

        Assert.IsNotNull(startArgs);
        Assert.AreEqual(10m, startArgs!.Top);
        Assert.AreEqual(20m, startArgs.Left);
        Assert.AreEqual(100m, startArgs.Width);

        Assert.AreEqual(80m, moveDiff);
        Assert.AreEqual(60m, endDiff);
    }

    [TestMethod]
    public void BitPullToRefreshShouldRespectClassesAndStyles()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var classes = new BitPullToRefreshClassStyles
        {
            Root = "custom-root",
            Loading = "custom-loading",
            SpinnerWrapper = "custom-spw",
            Spinner = "custom-spn"
        };

        var styles = new BitPullToRefreshClassStyles
        {
            Loading = "background:red;",
            SpinnerWrapper = "background:cyan;",
            Spinner = "color:green;"
        };

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
        });

        component.Instance._OnMove(80m).GetAwaiter().GetResult();

        var root = component.Find(".bit-ptr");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));

        var loading = component.Find(".bit-ptr-lod");
        Assert.IsTrue(loading.ClassList.Contains("custom-loading"));
        Assert.AreEqual("background:red;", loading.GetAttribute("style"));

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("custom-spw"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "background:cyan");

        var spinner = component.Find(".bit-ptr-spn");
        Assert.IsTrue(spinner.ClassList.Contains("custom-spn"));
        StringAssert.Contains(spinner.GetAttribute("style"), "color:green");
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderChildContent()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.AddChildContent("<div class=\"anchor\">content</div>");
        });

        var content = component.Find(".anchor");
        Assert.IsNotNull(content);
        Assert.AreEqual("content", content.TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldCallJsSetupOnFirstRender()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.PullToRefresh.setup");
    }
}
