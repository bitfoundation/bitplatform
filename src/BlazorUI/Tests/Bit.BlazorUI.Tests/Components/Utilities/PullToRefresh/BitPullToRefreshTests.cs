using System;
using System.Collections.Generic;
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

        // The live region is the screen reader text alone, so whatever a template draws in the indicator is never
        // read out with it; the indicator is the picture of what that text says.
        Assert.IsNull(loading.GetAttribute("role"));
        Assert.AreEqual("status", component.Find(".bit-ptr-vhd").GetAttribute("role"));
        Assert.AreEqual("true", spinnerWrapper.GetAttribute("aria-hidden"));
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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Assert.AreEqual(BitPullToRefreshState.Refreshing, component.Instance.State);

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        spinner = component.Find(".bit-ptr-spn");
        Assert.IsFalse(spinner.ClassList.Contains("bit-ptr-spin"));

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-swr"));
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldResetStateWhenOnRefreshThrows()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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

        Assert.IsFalse(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-swr"));
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
        Assert.AreEqual(0m, component.Instance.PullProgress);
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
    public void BitPullToRefreshShouldLeaveDrawingThePullToTheScript()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(40m).GetAwaiter().GetResult();

        // The script writes the size, the offset and the turn of the indicator on the strip as the pull moves, so
        // nothing about the pull is rendered on the parts themselves - rendering it is what used to re-render the
        // whole anchor for every pixel.
        Assert.IsNull(component.Find(".bit-ptr-spw").GetAttribute("style"));
        Assert.IsNull(component.Find(".bit-ptr-spn").GetAttribute("style"));
        Assert.AreEqual(BitPullToRefreshState.Pulling, component.Instance.State);
        Assert.AreEqual(0.5m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyCanReleaseStateAtTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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

        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
        Assert.AreEqual(0m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldKeepSpinnerOnPullEndAtTrigger()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        component.Instance._OnEnd(80m).GetAwaiter().GetResult();

        Assert.AreEqual(BitPullToRefreshState.CanRelease, component.Instance.State);
        Assert.AreEqual(1m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldResetSpinnerOnCancel()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        component.Instance._OnCancel(40m).GetAwaiter().GetResult();

        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
        Assert.AreEqual(0m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotThrowWhenTriggerIsZero()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
            parameters.Add(p => p.Disabled, true);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(component.Instance.UniqueId, setup.Arguments[0]);
        Assert.AreEqual(100, setup.Arguments[5]);
        Assert.AreEqual(2m, setup.Arguments[6]);
        Assert.AreEqual(20, setup.Arguments[7]);
        Assert.AreEqual(10, setup.Arguments[8]);
        Assert.AreEqual(0, setup.Arguments[9]);
        Assert.AreEqual(false, setup.Arguments[10]);
        Assert.AreEqual(false, setup.Arguments[11]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldApplyDisabledClass()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
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
    public void BitPullToRefreshShouldCallJsUpdateOnDisabledChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();

        component.Render(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(false, update.Arguments[8]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldPassNoMouseToJs()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.NoMouse, true);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(true, setup.Arguments[11]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoMouse, false);
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(false, update.Arguments[9]);
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
            parameters.Add(p => p.Disabled, true);
        });

        await component.Instance.RefreshAsync();

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.refresh"]);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldShowCompleteStateAfterRefreshWhenCompleteDelayIsSet()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
            Assert.AreEqual(BitPullToRefreshState.Complete, component.Instance.State);
            Assert.AreEqual(1m, component.Instance.PullProgress);

            var checkmark = component.Find(".bit-ptr-spn svg path");
            StringAssert.Contains(checkmark.GetAttribute("d"), "16.17");

            var announcement = component.Find(".bit-ptr-vhd");
            Assert.AreEqual("Refresh complete", announcement.TextContent);
        });

        await refreshTask;

        spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-cmp"));
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldRenderCompleteTemplateAndRespectCompleteClassesAndStyles()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Complete, "<div class=\"custom-complete-content\">done!</div>");
        });

        component.Instance._Refresh().GetAwaiter().GetResult();

        Assert.AreEqual(0, component.FindAll(".custom-complete-content").Count);

        var spinnerWrapper = component.Find(".bit-ptr-spw");
        Assert.IsFalse(spinnerWrapper.ClassList.Contains("bit-ptr-cmp"));
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
    }

    [TestMethod]
    public void BitPullToRefreshShouldAnnounceRefreshingLabelWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
    public void BitPullToRefreshShouldApplyTheUpDirectionClass()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();
        Assert.AreEqual(BitPullToRefreshDirection.Down, component.Instance.Direction);
        Assert.IsFalse(component.Find(".bit-ptr").ClassList.Contains("bit-ptr-up"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Direction, BitPullToRefreshDirection.Up);
        });

        Assert.IsTrue(component.Find(".bit-ptr").ClassList.Contains("bit-ptr-up"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Direction, BitPullToRefreshDirection.Down);
        });

        Assert.IsFalse(component.Find(".bit-ptr").ClassList.Contains("bit-ptr-up"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldPassTheDirectionToJs()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Direction, BitPullToRefreshDirection.Up);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.setup"].Single();
        Assert.AreEqual(BitPullToRefreshDirection.Up, setup.Arguments[12]);
        Assert.IsInstanceOfType<Microsoft.JSInterop.DotNetObjectReference<BitPullToRefresh>>(setup.Arguments[13]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Direction, BitPullToRefreshDirection.Down);
        });

        var update = Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Single();
        Assert.AreEqual(BitPullToRefreshDirection.Down, update.Arguments[10]);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Direction, BitPullToRefreshDirection.Down);
        });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.update"].Count);
    }

    [TestMethod]
    public void BitPullToRefreshShouldDropThePullHeightWhenTheDirectionTurnsWhileIdle()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var component = RenderComponent<BitPullToRefresh>();

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        Assert.AreEqual(BitPullToRefreshState.CanRelease, component.Instance.State);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Direction, BitPullToRefreshDirection.Up);
        });

        Assert.AreEqual(0m, component.Instance.PullProgress);
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
        Assert.IsFalse(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
    public void BitPullToRefreshShouldNotMarkTheRootBusyWhileRefreshing()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
        });

        Assert.IsNull(component.Find(".bit-ptr").GetAttribute("aria-busy"));

        // The live region is inside the root, and a screen reader holds back what changes inside a busy element
        // until it is no longer busy - which would swallow the very announcement the refresh makes.
        var refreshTask = component.Instance._Refresh();
        Assert.IsNull(component.Find(".bit-ptr").GetAttribute("aria-busy"));
        Assert.AreEqual("Refreshing", component.Find(".bit-ptr-vhd").TextContent);

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

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

        Assert.AreEqual(BitPullToRefreshState.CanRelease, component.Instance.State);
        Assert.AreEqual(1m, component.Instance.PullProgress);
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderAPullOnlyWhenItsStateChanges()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 80);
        });

        var renderCount = component.RenderCount;

        // Idle and pulling render alike, so starting a pull renders nothing, and neither does any move short of the
        // trigger: the script draws them alone, so re-rendering the component - and the whole anchor with it - is
        // skipped. Crossing the release line either way is what changes the markup.
        component.Instance._OnMove(10m).GetAwaiter().GetResult();
        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        component.Instance._OnMove(79m).GetAwaiter().GetResult();
        Assert.AreEqual(renderCount, component.RenderCount);

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        Assert.AreEqual(renderCount + 1, component.RenderCount);

        component.Instance._OnMove(79m).GetAwaiter().GetResult();
        Assert.AreEqual(renderCount + 2, component.RenderCount);

        // Neither does dropping a pull that never reached the trigger.
        component.Instance._OnCancel(79m).GetAwaiter().GetResult();
        Assert.AreEqual(renderCount + 2, component.RenderCount);
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderTheStartAndDropOfAPullForAnIndicatorTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.IndicatorTemplate, ctx => $"<span class=\"state\">{ctx.State}</span>");
        });

        // The template is handed the state, so for it idle and pulling do differ.
        component.Instance._OnMove(0.2m).GetAwaiter().GetResult();
        Assert.AreEqual("Pulling", component.Find(".state").TextContent);

        component.Instance._OnCancel(0.2m).GetAwaiter().GetResult();
        Assert.AreEqual("Idle", component.Find(".state").TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldRenderEveryPixelOfAPullForAnIndicatorTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.IndicatorTemplate, ctx => $"<span class=\"progress\">{ctx.Progress:0.##}</span>");
        });

        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        var renderCount = component.RenderCount;

        // The same whole pixel draws the template the same, so it is still skipped.
        component.Instance._OnMove(40.2m).GetAwaiter().GetResult();
        Assert.AreEqual(renderCount, component.RenderCount);

        component.Instance._OnMove(60m).GetAwaiter().GetResult();
        Assert.IsGreaterThan(renderCount, component.RenderCount);
        Assert.AreEqual("0.75", component.Find(".progress").TextContent);
    }

    [TestMethod]
    public void BitPullToRefreshShouldReplaceTheIndicatorWithTheIndicatorTemplate()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
            parameters.Add(p => p.Loading, "<span class=\"loading\"></span>");
            parameters.Add(p => p.IndicatorTemplate, ctx => $"<span class=\"state\">{ctx.State}</span>");
        });

        // It takes over from the disc and every glyph template inside it, and is a picture of what the live
        // region says, so it is hidden from assistive technology the same way the disc is.
        Assert.IsEmpty(component.FindAll(".bit-ptr-spw"));
        Assert.IsEmpty(component.FindAll(".loading"));
        Assert.AreEqual("true", component.Find(".bit-ptr-ind").GetAttribute("aria-hidden"));
        Assert.AreEqual("Idle", component.Find(".state").TextContent);

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        Assert.AreEqual("CanRelease", component.Find(".state").TextContent);
        Assert.AreEqual("Release to refresh", component.Find(".bit-ptr-vhd").TextContent);

        var refreshTask = component.Instance._Refresh();
        Assert.AreEqual("Refreshing", component.Find(".state").TextContent);

        tcs.SetResult();
        refreshTask.GetAwaiter().GetResult();

        Assert.AreEqual("Idle", component.Find(".state").TextContent);
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
        Assert.AreEqual(1m, component.Instance.PullProgress);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        Assert.AreEqual(0m, component.Instance.PullProgress);
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
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
        Assert.AreEqual(1m, component.Instance.PullProgress);

        // Past the trigger only the strip keeps growing: the progress is held where the trigger left it, and the
        // release state stays on.
        component.Instance._OnMove(120m).GetAwaiter().GetResult();
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
        component.Instance._OnEnd(120m).GetAwaiter().GetResult();

        // A release past the trigger is still a release: the pull is settled at the trigger, where the refresh
        // js is about to ask for holds it, rather than being dropped the way a short pull is.
        Assert.IsTrue(component.Find(".bit-ptr-spw").ClassList.Contains("bit-ptr-crl"));
        Assert.AreEqual(BitPullToRefreshState.CanRelease, component.Instance.State);
        Assert.AreEqual(1m, component.Instance.PullProgress);
    }
    [TestMethod]
    public void BitPullToRefreshShouldReportEachStateOfAPullOnce()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var states = new List<BitPullToRefreshState>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 80);
            parameters.Add(p => p.OnStateChange, (BitPullToRefreshState s) => states.Add(s));
        });

        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);

        component.Instance._OnMove(10m).GetAwaiter().GetResult();
        component.Instance._OnMove(40m).GetAwaiter().GetResult();
        Assert.AreEqual(BitPullToRefreshState.Pulling, component.Instance.State);

        component.Instance._OnMove(80m).GetAwaiter().GetResult();
        component.Instance._OnMove(90m).GetAwaiter().GetResult();
        Assert.AreEqual(BitPullToRefreshState.CanRelease, component.Instance.State);

        component.Instance._OnMove(50m).GetAwaiter().GetResult();
        component.Instance._OnCancel(50m).GetAwaiter().GetResult();
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);

        // Many moves, one report per change.
        CollectionAssert.AreEqual(new[]
        {
            BitPullToRefreshState.Pulling,
            BitPullToRefreshState.CanRelease,
            BitPullToRefreshState.Pulling,
            BitPullToRefreshState.Idle,
        }, states);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldReportTheRefreshAndItsEnd()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        var states = new List<BitPullToRefreshState>();
        var tcs = new TaskCompletionSource();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.CompleteDelay, 1);
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => tcs.Task));
            parameters.Add(p => p.OnStateChange, (BitPullToRefreshState s) => states.Add(s));
        });

        var refreshTask = component.Instance._Refresh();
        Assert.AreEqual(BitPullToRefreshState.Refreshing, component.Instance.State);
        Assert.IsTrue(component.Instance.IsRefreshing);

        tcs.SetResult();
        await refreshTask;

        // The end of the refresh - here the end of the complete state too - is reported, which is what lets a parent
        // that rendered for OnRefresh while IsRefreshing was still true catch up.
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);
        Assert.IsFalse(component.Instance.IsRefreshing);
        CollectionAssert.AreEqual(new[]
        {
            BitPullToRefreshState.Refreshing,
            BitPullToRefreshState.Complete,
            BitPullToRefreshState.Idle,
        }, states);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldReportIdleWhenOnRefreshThrows()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        var states = new List<BitPullToRefreshState>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => throw new InvalidOperationException("refresh failed")));
            parameters.Add(p => p.OnStateChange, (BitPullToRefreshState s) => states.Add(s));
        });

        try { await component.Instance._Refresh(); } catch (InvalidOperationException) { }

        CollectionAssert.AreEqual(new[] { BitPullToRefreshState.Refreshing, BitPullToRefreshState.Idle }, states);
    }

    [TestMethod]
    public void BitPullToRefreshShouldReportIdleWhenAPullIsDroppedByDisabling()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");

        var states = new List<BitPullToRefreshState>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnStateChange, (BitPullToRefreshState s) => states.Add(s));
        });

        component.Instance._OnMove(30m).GetAwaiter().GetResult();
        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        CollectionAssert.AreEqual(new[] { BitPullToRefreshState.Pulling, BitPullToRefreshState.Idle }, states);
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotReportASettledReleaseAsAChange()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var states = new List<BitPullToRefreshState>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.Trigger, 80);
            parameters.Add(p => p.MaxPull, 120);
            parameters.Add(p => p.OnStateChange, (BitPullToRefreshState s) => states.Add(s));
        });

        component.Instance._OnMove(120m).GetAwaiter().GetResult();
        component.Instance._OnEnd(120m).GetAwaiter().GetResult();

        // Settled at the trigger, the release still stands until the refresh js is about to ask for takes over.
        CollectionAssert.AreEqual(new[] { BitPullToRefreshState.CanRelease }, states);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldStillRefreshAndResetWhenOnStateChangeThrows()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.dispose").SetVoidResult();

        var errors = new List<Exception>();
        var refreshed = false;
        var calls = 0;
        var host = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, ex => errors.Add(ex)));
            parameters.AddChildContent<BitPullToRefresh>(ptr =>
            {
                ptr.Add(p => p.OnRefresh, () => refreshed = true);
                ptr.Add(p => p.OnStateChange, (BitPullToRefreshState s) => throw new InvalidOperationException($"state change {++calls}"));
            });
        });
        // Read up front: the boundary takes the component out of the tree once it is handed the failure.
        var ptr = host.FindComponent<BitPullToRefresh>().Instance;

        // The notification is not a step of the refresh: nothing is thrown back over the interop call, and the
        // refresh runs and resets as if the handler had not failed.
        await host.InvokeAsync(() => ptr._Refresh());

        Assert.IsTrue(refreshed);
        Assert.AreEqual(2, calls);
        Assert.AreEqual(BitPullToRefreshState.Idle, ptr.State);
        Assert.IsFalse(ptr.IsRefreshing);

        // ... and the failure is where the framework puts an unhandled one, rather than swallowed.
        Assert.IsNotEmpty(errors);
        Assert.AreEqual("state change 1", errors[0].Message);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldKeepTheOnRefreshExceptionWhenTheCleanupNotificationThrows()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.dispose").SetVoidResult();

        Exception? dispatched = null;
        var host = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, ex => dispatched ??= ex));
            parameters.AddChildContent<BitPullToRefresh>(ptr =>
            {
                ptr.Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => throw new InvalidOperationException("refresh failed")));
                ptr.Add(p => p.OnStateChange, (BitPullToRefreshState s) =>
                {
                    if (s == BitPullToRefreshState.Idle) throw new InvalidOperationException("idle failed");
                });
            });
        });
        var ptr = host.FindComponent<BitPullToRefresh>().Instance;

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => host.InvokeAsync(() => ptr._Refresh()));

        Assert.AreEqual("refresh failed", error.Message);
        Assert.AreEqual(BitPullToRefreshState.Idle, ptr.State);
        Assert.AreEqual("idle failed", dispatched?.Message);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldStillInvokeThePullCallbacksWhenOnStateChangeThrows()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.dispose").SetVoidResult();

        var errors = new List<Exception>();
        decimal? moved = null;
        var host = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, ex => errors.Add(ex)));
            parameters.AddChildContent<BitPullToRefresh>(ptr =>
            {
                ptr.Add(p => p.OnPullMove, (decimal d) => moved = d);
                ptr.Add(p => p.OnStateChange, (BitPullToRefreshState s) => throw new InvalidOperationException(s.ToString()));
            });
        });
        var component = host.FindComponent<BitPullToRefresh>();

        // Nothing is thrown back over the interop call, which the script would only log, and the move is still
        // reported; the failure goes to the error boundary instead.
        await host.InvokeAsync(() => component.Instance._OnMove(40m));

        Assert.AreEqual(40m, moved);
        Assert.AreEqual(nameof(BitPullToRefreshState.Pulling), errors.Single().Message);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldNotWaitForOnStateChangeToRefresh()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        var handler = new TaskCompletionSource();
        var states = new List<BitPullToRefreshState>();
        var refreshed = false;
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnRefresh, () => refreshed = true);
            parameters.Add(p => p.OnStateChange, EventCallback.Factory.Create<BitPullToRefreshState>(this, async s =>
            {
                states.Add(s);
                await handler.Task;
            }));
        });

        // A handler still busy with the first change holds up neither the refresh nor the pull callbacks.
        await component.InvokeAsync(() => component.Instance._Refresh());
        Assert.IsTrue(refreshed);
        Assert.AreEqual(BitPullToRefreshState.Idle, component.Instance.State);

        // The changes after it wait their turn, so they still arrive one at a time and in order.
        CollectionAssert.AreEqual(new[] { BitPullToRefreshState.Refreshing }, states);

        handler.SetResult();

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { BitPullToRefreshState.Refreshing, BitPullToRefreshState.Idle }, states));
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotWaitForOnStateChangeToReportAMove()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");

        var handler = new TaskCompletionSource();
        var moves = new List<decimal>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.OnPullMove, (decimal d) => moves.Add(d));
            parameters.Add(p => p.OnStateChange, EventCallback.Factory.Create<BitPullToRefreshState>(this, _ => handler.Task));
        });

        // The handler never completes, yet the move - which the script waits on before it reports the next one - does.
        var move = component.InvokeAsync(() => component.Instance._OnMove(40m));

        Assert.IsTrue(move.Wait(TimeSpan.FromSeconds(5)));
        CollectionAssert.AreEqual(new[] { 40m }, moves);
    }

    [TestMethod]
    public void BitPullToRefreshShouldHandAnOnStateChangeExceptionOfAParameterChangeToTheErrorBoundary()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.update");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.dispose").SetVoidResult();

        Exception? captured = null;
        var host = RenderComponent<BitErrorBoundary>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<Exception>(this, ex => captured = ex));
            parameters.AddChildContent<BitPullToRefresh>(ptr =>
            {
                ptr.Add(p => p.OnStateChange, (BitPullToRefreshState s) =>
                {
                    if (s == BitPullToRefreshState.Idle) throw new InvalidOperationException("idle failed");
                });
            });
        });
        var component = host.FindComponent<BitPullToRefresh>();

        component.InvokeAsync(() => component.Instance._OnMove(30m)).GetAwaiter().GetResult();

        // Disabling drops the pull; the handler failing on that change does not fail the parameter update, it
        // reaches the error boundary the way a click handler's exception does.
        var ptr = component.Instance;
        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        Assert.AreEqual(BitPullToRefreshState.Idle, ptr.State);
        Assert.AreEqual("idle failed", captured?.Message);
    }

    [TestMethod]
    public async Task BitPullToRefreshShouldLetGoOfTheHeldIndicatorBeforeRenderingTheEndOfARefresh()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.setup");
        Context.JSInterop.SetupVoid("BitBlazorUI.PullToRefresh.release").SetVoidResult();

        // Each render notes how many times the script had been told to let go by then.
        var renders = new List<(BitPullToRefreshState State, int Releases)>();
        var component = RenderComponent<BitPullToRefresh>(parameters =>
        {
            parameters.Add(p => p.IndicatorTemplate, ctx => builder =>
                renders.Add((ctx.State, Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.release"].Count)));
        });
        renders.Clear();

        await component.InvokeAsync(() => component.Instance._Refresh());

        // The script is told to let go of its hold while the refreshing indicator is still what is rendered, so the
        // strip never closes on the idle one drawn at full size.
        CollectionAssert.AreEqual(new[]
        {
            (BitPullToRefreshState.Refreshing, 0),
            (BitPullToRefreshState.Idle, 1),
        }, renders);
        Assert.AreEqual(component.Instance.UniqueId, Context.JSInterop.Invocations["BitBlazorUI.PullToRefresh.release"].Single().Arguments[0]);
    }

    [TestMethod]
    public void BitPullToRefreshShouldKeepASplattedAriaLabel()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitPullToRefresh>(0);
            builder.AddAttribute(1, "aria-label", "Main");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-ptr");

        Assert.AreEqual("Main", root.GetAttribute("aria-label"));
    }
}
