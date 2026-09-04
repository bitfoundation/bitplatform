using System;
using System.Linq;
using System.Threading.Tasks;
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

        Assert.AreEqual("status", loading.GetAttribute("role"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderAriaLabel()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "pull down to refresh");
        });

        var root = component.Find(".bit-ptr");
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("pull down to refresh", root.GetAttribute("aria-label"));
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
    public void BitPullToRefreshShouldShowRefreshingStateDuringOnRefresh()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        var spinner = component.Find(".bit-ptr-spn");
        Assert.IsTrue(spinner.ClassList.Contains("bit-ptr-spin"));

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("bit-ptr-swr"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "margin-top:0px");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:35px");
        StringAssert.Contains(spinner.GetAttribute("style"), "width:24px");

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        spinner = component.Find(".bit-ptr-spn");
        Assert.IsFalse(spinner.ClassList.Contains("bit-ptr-spin"));

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-swr"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:0px");
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldResetStateWhenOnRefreshThrows()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => throw new InvalidOperationException("refresh failed")));
        });

        var thrown = false;
        try
        {
            await component.Instance._Refresh();
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }
        Assert.IsTrue(thrown);

        var spinner = component.Find(".bit-ptr-spn");
        Assert.IsFalse(spinner.ClassList.Contains("bit-ptr-spin"));

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:0px");
    }

    [TestMethod]
    public void BitPullToRefreshShouldInvokePullCallbacks()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        BitPullToRefreshPullStartArgs? startArgs = null;
        decimal moveDiff = 0;
        decimal endDiff = 0;
        decimal cancelDiff = 0;

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnPullStart, EventCallback.Factory.Create<BitPullToRefreshPullStartArgs>(this, args => startArgs = args));
            parameters.Add(p => p.OnPullMove, EventCallback.Factory.Create<decimal>(this, diff => moveDiff = diff));
            parameters.Add(p => p.OnPullEnd, EventCallback.Factory.Create<decimal>(this, diff => endDiff = diff));
            parameters.Add(p => p.OnPullCancel, EventCallback.Factory.Create<decimal>(this, diff => cancelDiff = diff));
        });

        component.Instance._OnStart(10m, 20m, 100m).GetAwaiter().GetResult();
        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        component.Instance._OnEnd(60m).GetAwaiter().GetResult();
        component.Instance._OnCancel(40m).GetAwaiter().GetResult();

        Assert.IsNotNull(startArgs);
        Assert.AreEqual(10m, startArgs!.Top);
        Assert.AreEqual(20m, startArgs.Left);
        Assert.AreEqual(100m, startArgs.Width);

        Assert.AreEqual(80m, moveDiff);
        Assert.AreEqual(60m, endDiff);
        Assert.AreEqual(40m, cancelDiff);
    }

    [TestMethod]
    public void BitPullToRefreshShouldSizeSpinnerBasedOnPullMove()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(40m).GetAwaiter().GetResult();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "margin-top:20px");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:17.5px");

        var spinner = component.Find(".bit-ptr-spn");
        StringAssert.Contains(spinner.GetAttribute("style"), "width:12px");
        StringAssert.Contains(spinner.GetAttribute("style"), "rotate(-80deg)");
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCanReleaseStateAtTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var classes = new BitPullToRefreshClassStyles
        {
            SpinnerWrapperCanRelease = "custom-swc",
            SpinnerCanRelease = "custom-spc"
        };

        var styles = new BitPullToRefreshClassStyles
        {
            SpinnerWrapperCanRelease = "border:2px solid gold;",
            SpinnerCanRelease = "outline:2px solid gold;"
        };

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
        });

        component.Instance._OnMove(40m).GetAwaiter().GetResult();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-crl"));
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("custom-swc"));

        component.Instance._OnMove(80m).GetAwaiter().GetResult();

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("bit-ptr-crl"));
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("custom-swc"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "border:2px solid gold");

        var spinner = component.Find(".bit-ptr-spn");
        Assert.IsTrue(spinner.ClassList.Contains("custom-spc"));
        StringAssert.Contains(spinner.GetAttribute("style"), "outline:2px solid gold");

        component.Instance._OnEnd(80m).GetAwaiter().GetResult();
        component.Instance._Refresh().GetAwaiter().GetResult();

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-crl"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotApplyCanReleaseStateWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-crl"));
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("bit-ptr-swr"));

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();
    }

    [TestMethod]
    public void BitPullToRefreshShouldResetSpinnerOnPullEndBelowTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        component.Instance._OnEnd(40m).GetAwaiter().GetResult();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:0px");
    }

    [TestMethod]
    public void BitPullToRefreshShouldKeepSpinnerOnPullEndAtTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        component.Instance._OnEnd(80m).GetAwaiter().GetResult();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:35px");
    }

    [TestMethod]
    public void BitPullToRefreshShouldResetSpinnerOnCancel()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        component.Instance._OnCancel(40m).GetAwaiter().GetResult();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:0px");
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotThrowWhenTriggerIsZero()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 0);
        });

        component.Instance._OnMove(10m).GetAwaiter().GetResult();
        component.Instance._Refresh().GetAwaiter().GetResult();

        Assert.IsNotNull(component.Find(".bit-ptr-spw"));
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
    public void BitPullToRefreshShouldRespectRefreshingClassesAndStyles()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var classes = new BitPullToRefreshClassStyles
        {
            SpinnerWrapperRefreshing = "custom-swr",
            SpinnerRefreshing = "custom-spr"
        };

        var styles = new BitPullToRefreshClassStyles
        {
            SpinnerWrapperRefreshing = "border:1px solid red;",
            SpinnerRefreshing = "outline:1px solid blue;"
        };

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("custom-swr"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "border:1px solid red");

        var spinner = component.Find(".bit-ptr-spn");
        Assert.IsTrue(spinner.ClassList.Contains("custom-spr"));
        StringAssert.Contains(spinner.GetAttribute("style"), "outline:1px solid blue");

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("custom-swr"));

        spinner = component.Find(".bit-ptr-spn");
        Assert.IsFalse(spinner.ClassList.Contains("custom-spr"));
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
    public void BitPullToRefreshShouldRenderCustomLoadingTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Loading, "<div class=\"custom-loading-content\">loading...</div>");
        });

        var loading = component.Find(".custom-loading-content");
        Assert.IsNotNull(loading);
        Assert.AreEqual("loading...", loading.TextContent);

        Assert.AreEqual(0, component.FindAll(".bit-ptr-spn svg").Count);
    }

    [TestMethod]
    public void BitPullToRefreshShouldCallJsSetupOnFirstRender()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.PullToRefresh.setup");
    }

    [TestMethod]
    public void BitPullToRefreshShouldPassParametersToJsSetup()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 100);
            parameters.Add(p => p.Factor, 2m);
            parameters.Add(p => p.Margin, 20);
            parameters.Add(p => p.Threshold, 10);
            parameters.Add(p => p.IsEnabled, false);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(component.Instance.UniqueId, setup.Arguments[0]);
        Assert.AreEqual(100, setup.Arguments[5]);
        Assert.AreEqual(2m, setup.Arguments[6]);
        Assert.AreEqual(20, setup.Arguments[7]);
        Assert.AreEqual(10, setup.Arguments[8]);
        Assert.AreEqual(0, setup.Arguments[9]);
        Assert.AreEqual(false, setup.Arguments[10]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyDisabledClass()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        var root = component.Find(".bit-ptr");
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldCallJsUpdateOnParameterChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.Trigger, 120);
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(component.Instance.UniqueId, update.Arguments[0]);
        Assert.AreEqual(120, update.Arguments[3]);
        Assert.AreEqual(1.5m, update.Arguments[4]);
        Assert.AreEqual(30, update.Arguments[5]);
        Assert.AreEqual(0, update.Arguments[6]);
        Assert.AreEqual(0, update.Arguments[7]);
        Assert.AreEqual(true, update.Arguments[8]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Trigger, 120);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Count);
    }

    [TestMethod]
    public void BitPullToRefreshShouldCallJsUpdateOnIsEnabledChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(false, update.Arguments[8]);
    }

    [TestMethod]
    public async Task BitPullToRefreshRefreshAsyncShouldCallJsRefresh()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.refresh");

        var component = RenderComponent<BitPullToRefresh>();

        await component.Instance.RefreshAsync();

        var refresh = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.refresh"].Single();
        Assert.AreEqual(component.Instance.UniqueId, refresh.Arguments[0]);
    }

    [TestMethod]
    public async Task BitPullToRefreshRefreshAsyncShouldNotCallJsRefreshWhenDisabled()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.refresh");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.Instance.RefreshAsync();

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.refresh"]);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldShowCompleteStateAfterRefreshWhenCompleteDelayIsSet()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.CompleteDelay, 100);
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-cmp"));
        Assert.IsTrue(spinnerWrapper.ClassList.Contains("bit-ptr-swr"));

        tcs.SetResult();

        component.WaitForAssertion(() =>
        {
            var sw = component.Find(".bit-ptr-spw");
            Assert.IsTrue(sw.ClassList.Contains("bit-ptr-cmp"));
            Assert.IsFalse(sw.ClassList.Contains("bit-ptr-swr"));
            Assert.IsFalse(sw.ClassList.Contains("bit-ptr-crl"));
            StringAssert.Contains(sw.GetAttribute("style"), "margin-top:0px");
            StringAssert.Contains(sw.GetAttribute("style"), "width:35px");

            var checkmark = component.Find(".bit-ptr-spn svg path");
            StringAssert.Contains(checkmark.GetAttribute("d"), "16.17");

            var announcement = component.Find(".bit-ptr-vhd");
            Assert.AreEqual("Refresh complete", announcement.TextContent);
        });

        await refreshTask;

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-cmp"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:0px");
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldRenderCompleteTemplateAndRespectCompleteClassesAndStyles()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.CompleteDelay, 100);
            parameters.Add(p => p.Complete, "<div class=\"custom-complete-content\">done!</div>");
            parameters.Add(p => p.Classes, new BitPullToRefreshClassStyles { SpinnerWrapperComplete = "custom-swcmp", SpinnerComplete = "custom-spcmp" });
            parameters.Add(p => p.Styles, new BitPullToRefreshClassStyles { SpinnerWrapperComplete = "border:2px solid green;", SpinnerComplete = "outline:2px solid green;" });
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        Assert.AreEqual(0, component.FindAll(".custom-complete-content").Count);

        tcs.SetResult();

        component.WaitForAssertion(() =>
        {
            var complete = component.Find(".custom-complete-content");
            Assert.AreEqual("done!", complete.TextContent);

            var sw = component.Find(".bit-ptr-spw");
            Assert.IsTrue(sw.ClassList.Contains("custom-swcmp"));
            StringAssert.Contains(sw.GetAttribute("style"), "border:2px solid green");

            var spinner = component.Find(".bit-ptr-spn");
            Assert.IsTrue(spinner.ClassList.Contains("custom-spcmp"));
            StringAssert.Contains(spinner.GetAttribute("style"), "outline:2px solid green");
        });

        await refreshTask;

        Assert.AreEqual(0, component.FindAll(".custom-complete-content").Count);

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("custom-swcmp"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotShowCompleteStateWhenCompleteDelayIsZero()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Complete, "<div class=\"custom-complete-content\">done!</div>");
        });

        component.Instance._Refresh().GetAwaiter().GetResult();

        Assert.AreEqual(0, component.FindAll(".custom-complete-content").Count);

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-cmp"));
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:0px");
    }

    [TestMethod]
    public void BitPullToRefreshShouldAnnounceRefreshingLabelWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var announcement = component.Find(".bit-ptr-vhd");
        Assert.AreEqual(string.Empty, announcement.TextContent);

        var refreshTask = component.Instance._Refresh();

        announcement = component.Find(".bit-ptr-vhd");
        Assert.AreEqual("Refreshing", announcement.TextContent);

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        announcement = component.Find(".bit-ptr-vhd");
        Assert.AreEqual(string.Empty, announcement.TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldAnnounceCustomRefreshingLabel()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.RefreshingLabel, "Loading new items");
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        var announcement = component.Find(".bit-ptr-vhd");
        Assert.AreEqual("Loading new items", announcement.TextContent);

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldCallJsDisposeOnDispose()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.dispose");

        var component = RenderComponent<BitPullToRefresh>();
        var uniqueId = component.Instance.UniqueId;

        await Context.DisposeComponentsAsync();

        var dispose = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.dispose"].Single();
        Assert.AreEqual(uniqueId, dispose.Arguments[0]);
    }
    [TestMethod]
    public void BitPullToRefreshShouldPassScrollerSelectorToJsSetup()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".scroller");
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(".scroller", setup.Arguments[4]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldCallJsUpdateOnScrollerSelectorChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".first");
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".second");
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(".second", update.Arguments[2]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.ScrollerSelector, ".second");
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Count);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(-10, 1)]
    [DataRow(80, 80)]
    public void BitPullToRefreshShouldClampTriggerForJs(int trigger, int expected)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, trigger);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(expected, setup.Arguments[5]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldClampFactorMarginAndThresholdForJs()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Factor, 0m);
            parameters.Add(p => p.Margin, -5);
            parameters.Add(p => p.Threshold, -5);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(0.1m, setup.Arguments[6]);
        Assert.AreEqual(0, setup.Arguments[7]);
        Assert.AreEqual(0, setup.Arguments[8]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyFullWidthClass()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();
        Assert.IsFalse(component.Find(".bit-ptr").ClassList.Contains("bit-ptr-flw"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
        });

        Assert.IsTrue(component.Find(".bit-ptr").ClassList.Contains("bit-ptr-flw"));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "var(--bit-clr-pri)")]
    [DataRow(BitColor.Info, "var(--bit-clr-inf)")]
    [DataRow(BitColor.Error, "var(--bit-clr-err)")]
    [DataRow(BitColor.TertiaryBorder, "var(--bit-clr-brd-ter)")]
    public void BitPullToRefreshShouldRespectColor(BitColor color, string expected)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        StringAssert.Contains(component.Find(".bit-ptr").GetAttribute("style"), $"--bit-ptr-color:{expected}");
    }

    [TestMethod]
    public void BitPullToRefreshShouldRespectCustomColorOnlyWhileColorIsUnset()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.CustomColor, "#b400ff");
        });

        StringAssert.Contains(component.Find(".bit-ptr").GetAttribute("style"), "--bit-ptr-color:#b400ff");

        component.Render(parameters =>
        {
            parameters.Add(p => p.CustomColor, "#b400ff");
            parameters.Add(p => p.Color, BitColor.Success);
        });

        var style = component.Find(".bit-ptr").GetAttribute("style");
        StringAssert.Contains(style, "--bit-ptr-color:var(--bit-clr-suc)");
        Assert.IsFalse(style!.Contains("#b400ff"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotRenderColorVariableWhenNeitherColorIsSet()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        var style = component.Find(".bit-ptr").GetAttribute("style");
        Assert.IsFalse(style?.Contains("--bit-ptr-color") ?? false);
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderReleaseTemplateOnlyPastTheTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 80);
            parameters.Add(p => p.Loading, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"pulling\">pulling</span>")));
            parameters.Add(p => p.Release, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"release\">release</span>")));
        });

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        Assert.AreEqual(1, component.FindAll(".pulling").Count);
        Assert.IsEmpty(component.FindAll(".release"));

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        Assert.IsEmpty(component.FindAll(".pulling"));
        Assert.AreEqual(1, component.FindAll(".release").Count);

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        Assert.AreEqual(1, component.FindAll(".pulling").Count);
        Assert.IsEmpty(component.FindAll(".release"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldFallBackToLoadingTemplateWithoutAReleaseTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Loading, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"pulling\">pulling</span>")));
        });

        component.Instance._OnMove(80m).GetAwaiter().GetResult();

        Assert.AreEqual(1, component.FindAll(".pulling").Count);
        Assert.IsTrue(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotRenderReleaseTemplateWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Release, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"release\">release</span>")));
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();

        Assert.IsEmpty(component.FindAll(".release"));

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();
    }

    [TestMethod]
    public void BitPullToRefreshShouldAnnounceReleaseLabelPastTheTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        Assert.AreEqual(string.Empty, component.Find(".bit-ptr-vhd").TextContent);

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        Assert.AreEqual(string.Empty, component.Find(".bit-ptr-vhd").TextContent);

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        Assert.AreEqual("Release to refresh", component.Find(".bit-ptr-vhd").TextContent);

        component.Instance._OnCancel(80m).GetAwaiter().GetResult();
        Assert.AreEqual(string.Empty, component.Find(".bit-ptr-vhd").TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldAnnounceCustomReleaseLabel()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.ReleaseLabel, "Let go");
        });

        component.Instance._OnMove(80m).GetAwaiter().GetResult();

        Assert.AreEqual("Let go", component.Find(".bit-ptr-vhd").TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldLeaveTheReleaseStateSilentWithAnEmptyLabel()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.ReleaseLabel, string.Empty);
        });

        component.Instance._OnMove(80m).GetAwaiter().GetResult();

        Assert.AreEqual(string.Empty, component.Find(".bit-ptr-vhd").TextContent);
        Assert.IsTrue(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldMarkTheRootBusyWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        Assert.IsNull(component.Find(".bit-ptr").GetAttribute("aria-busy"));

        var refreshTask = component.Instance._Refresh();
        Assert.AreEqual("true", component.Find(".bit-ptr").GetAttribute("aria-busy"));

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        Assert.IsNull(component.Find(".bit-ptr").GetAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldHideTheDefaultGlyphsFromAssistiveTechnology()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        var svg = component.Find(".bit-ptr-spn svg");
        Assert.AreEqual("true", svg.GetAttribute("aria-hidden"));
        Assert.AreEqual("false", svg.GetAttribute("focusable"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldReportIsRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        Assert.IsFalse(component.Instance.IsRefreshing);

        var refreshTask = component.Instance._Refresh();
        Assert.IsTrue(component.Instance.IsRefreshing);

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsRefreshing);
    }

    [TestMethod]
    public void BitPullToRefreshShouldReportPullProgress()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 100);
        });

        Assert.AreEqual(0m, component.Instance.PullProgress);

        component.Instance._OnMove(25m).GetAwaiter().GetResult();
        Assert.AreEqual(0.25m, component.Instance.PullProgress);

        component.Instance._OnMove(100m).GetAwaiter().GetResult();
        Assert.AreEqual(1m, component.Instance.PullProgress);

        component.Instance._OnCancel(100m).GetAwaiter().GetResult();
        Assert.AreEqual(0m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldReportFullPullProgressWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        var refreshTask = component.Instance._Refresh();
        Assert.AreEqual(1m, component.Instance.PullProgress);

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotDivideByZeroWithAZeroFactorOrNegativeSizes()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 0);
            parameters.Add(p => p.Factor, 0m);
        });

        component.Instance._OnMove(10m).GetAwaiter().GetResult();

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        StringAssert.Contains(spinnerWrapper.GetAttribute("style"), "width:35px");
        Assert.AreEqual(1m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldSkipRenderingForAMoveThatDrawsTheSame()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        var renderCount = component.RenderCount;

        // The same whole pixel and the same release state: nothing about the indicator would be drawn
        // differently, so re-rendering the component - and the whole anchor with it - is skipped.
        component.Instance._OnMove(40.2m).GetAwaiter().GetResult();
        Assert.AreEqual(renderCount, component.RenderCount);

        component.Instance._OnMove(41.6m).GetAwaiter().GetResult();
        Assert.IsGreaterThan(renderCount, component.RenderCount);
    }

    [TestMethod]
    public void BitPullToRefreshShouldStillReportEveryMoveToTheCallback()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var moves = new System.Collections.Generic.List<decimal>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnPullMove, EventCallback.Factory.Create<decimal>(this, diff => moves.Add(diff)));
        });

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        component.Instance._OnMove(40.2m).GetAwaiter().GetResult();
        component.Instance._OnMove(40.4m).GetAwaiter().GetResult();

        CollectionAssert.AreEqual(new[] { 40m, 40.2m, 40.4m }, moves);
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderTheAnchorAliasLikeChildContent()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Anchor, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div class=\"anchored\">content</div>")));
        });

        Assert.AreEqual(1, component.FindAll(".anchored").Count);
    }

    [TestMethod]
    public void BitPullToRefreshShouldPreferAnchorOverChildContent()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Anchor, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div class=\"anchored\">anchor</div>")));
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div class=\"childed\">child</div>")));
        });

        Assert.AreEqual(1, component.FindAll(".anchored").Count);
        Assert.IsEmpty(component.FindAll(".childed"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldDropThePullHeightWhenItGetsDisabledWhileIdle()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        StringAssert.Contains(component.Find(".bit-ptr-spw").GetAttribute("style"), "width:35px");

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        StringAssert.Contains(component.Find(".bit-ptr-spw").GetAttribute("style"), "width:0px");
        Assert.IsFalse(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
    }
    [TestMethod]
    public void BitPullToRefreshShouldPassMaxPullToJsSetup()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.MaxPull, 120);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(120, setup.Arguments[9]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldClampNegativeMaxPullForJs()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.MaxPull, -40);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(0, setup.Arguments[9]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldCallJsUpdateOnMaxPullChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.MaxPull, 110);
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(110, update.Arguments[7]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldHoldTheIndicatorAtFullSizeThroughAnOverpull()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 80);
            parameters.Add(p => p.MaxPull, 120);
        });

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        var atTrigger = component.Find(".bit-ptr-spw").GetAttribute("style");
        StringAssert.Contains(atTrigger, "width:35px");
        StringAssert.Contains(component.Find(".bit-ptr-spn").GetAttribute("style"), "width:24px");
        StringAssert.Contains(component.Find(".bit-ptr-spn").GetAttribute("style"), "rotate(0deg)");

        // Past the trigger only the strip keeps growing: the disc, the glyph and the rotation are all held
        // where the trigger left them, and the release state stays on.
        component.Instance._OnMove(120m).GetAwaiter().GetResult();
        StringAssert.Contains(component.Find(".bit-ptr-spw").GetAttribute("style"), "width:35px");
        StringAssert.Contains(component.Find(".bit-ptr-spn").GetAttribute("style"), "width:24px");
        StringAssert.Contains(component.Find(".bit-ptr-spn").GetAttribute("style"), "rotate(0deg)");
        Assert.IsTrue(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
        Assert.AreEqual(1m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldStillRefreshAfterAnOverpull()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 80);
            parameters.Add(p => p.MaxPull, 120);
        });

        component.Instance._OnMove(120m).GetAwaiter().GetResult();

        // Halfway through the overpull the indicator is drawn lower, since it follows the strip down.
        StringAssert.Contains(component.Find(".bit-ptr-spw").GetAttribute("style"), "margin-top:60px");

        component.Instance._OnEnd(120m).GetAwaiter().GetResult();

        // A release past the trigger is still a release: the pull is settled at the trigger, where the refresh
        // js is about to ask for holds it, rather than being dropped the way a short pull is.
        Assert.IsTrue(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
        StringAssert.Contains(component.Find(".bit-ptr-spw").GetAttribute("style"), "margin-top:40px");
        StringAssert.Contains(component.Find(".bit-ptr-spw").GetAttribute("style"), "width:35px");
        Assert.AreEqual(1m, component.Instance.PullProgress);
    }
}
