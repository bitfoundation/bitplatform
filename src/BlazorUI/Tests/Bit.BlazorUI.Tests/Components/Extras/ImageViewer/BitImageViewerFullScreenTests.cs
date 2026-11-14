using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ImageViewer;

/// <summary>
/// The full screen of BitImageViewer: the modal dialog it covers the page as, how it is entered and left (its button, its
/// close button, Escape, code), what it holds of the page while it is (the focus, the trap, the scroll) and gives back.
/// </summary>
[TestClass]
public class BitImageViewerFullScreenTests : BunitTestContext
{
    private const string Setup = "BitBlazorUI.BitImageViewer.setup";

    private BunitJSModuleInterop SetupHandle()
    {
        var module = Context.JSInterop.SetupModule(invocation => invocation.Identifier == Setup);
        module.Mode = JSRuntimeMode.Loose;
        return module;
    }

    private IRenderedComponent<BitImageViewer> Render(Action<ComponentParameterCollectionBuilder<BitImageViewer>>? more = null, List<bool>? changes = null)
    {
        return RenderComponent<BitImageViewer>(parameters =>
        {
            parameters.Add(p => p.Id, "imv");
            parameters.Add(p => p.Src, "/photo.jpg");
            parameters.Add(p => p.Alt, "A lighthouse at dusk");
            if (changes is not null) parameters.Add(p => p.FullScreenChanged, (bool v) => changes.Add(v));
            more?.Invoke(parameters);
        });
    }

    private int Calls(string identifier) => Context.JSInterop.Invocations[identifier].Count;

    [TestMethod]
    public void BitImageViewerInThePageShouldBeNoDialog()
    {
        var com = Render();

        var root = com.Find("#imv");

        Assert.IsFalse(root.ClassList.Contains("bit-imv-fsc"));
        Assert.IsNull(root.GetAttribute("role"));
        Assert.IsNull(root.GetAttribute("aria-modal"));
        Assert.IsNull(root.GetAttribute("data-bit-esc"));
        Assert.AreEqual(0, com.FindAll(".bit-imv-cls").Count);
        Assert.AreEqual(0, com.FindAll(".bit-imv-fsb").Count, "the full screen button is opt-in");
    }

    [TestMethod]
    public void FullScreenBitImageViewerShouldBeANamedModalDialogWithACloseButton()
    {
        var com = Render(p => p.Add(x => x.FullScreen, true), changes: []);

        var root = com.Find("#imv");

        Assert.IsTrue(root.ClassList.Contains("bit-imv-fsc"));
        Assert.AreEqual("dialog", root.GetAttribute("role"));
        Assert.AreEqual("true", root.GetAttribute("aria-modal"));
        Assert.AreEqual("A lighthouse at dusk", root.GetAttribute("aria-label"));
        Assert.AreEqual("claim", root.GetAttribute("data-bit-esc"));

        var close = com.Find(".bit-imv-cls");
        Assert.AreEqual("Exit full screen", close.GetAttribute("aria-label"));
        Assert.AreEqual("button", close.GetAttribute("type"));
    }

    [TestMethod]
    public void FullScreenBitImageViewerShouldShowTheCloseButtonWithoutAToolbar()
    {
        var com = Render(p =>
        {
            p.Add(x => x.FullScreen, true);
            p.Add(x => x.NoToolbar, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-imv-tbr").Count);
        Assert.AreEqual(1, com.FindAll(".bit-imv-cls").Count);
    }

    [TestMethod]
    public void FullScreenBitImageViewerShouldFillTheScreenRatherThanItsHeight()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Height, "12rem");
            p.Add(x => x.FullScreen, true);
        });

        Assert.IsNull(com.Find(".bit-imv-vpt").GetAttribute("style"));
    }

    [TestMethod]
    public void BitImageViewerFullScreenButtonShouldEnterAndLeaveFullScreen()
    {
        var changes = new List<bool>();
        var com = Render(p => p.Add(x => x.ShowFullScreenButton, true), changes);

        var button = com.Find(".bit-imv-tbr .bit-imv-fsb");
        Assert.AreEqual("Full screen", button.GetAttribute("aria-label"));
        Assert.AreEqual("dialog", button.GetAttribute("aria-haspopup"));

        button.Click();

        Assert.IsTrue(com.Instance.FullScreen);

        // The same button, which the focus is handed back to on the way out, now leaves it.
        button = com.Find(".bit-imv-tbr .bit-imv-fsb");
        Assert.AreEqual("Exit full screen", button.GetAttribute("aria-label"));
        Assert.IsNull(button.GetAttribute("aria-haspopup"));

        button.Click();

        Assert.IsFalse(com.Instance.FullScreen);
        CollectionAssert.AreEqual(new[] { true, false }, changes);
    }

    [TestMethod]
    public void BitImageViewerCloseButtonShouldLeaveFullScreen()
    {
        var changes = new List<bool>();
        var com = Render(p => p.Add(x => x.FullScreen, true), changes);

        com.Find(".bit-imv-cls").Click();

        Assert.IsFalse(com.Instance.FullScreen);
        Assert.AreEqual(0, com.FindAll(".bit-imv-cls").Count);
        CollectionAssert.AreEqual(new[] { false }, changes);
    }

    [TestMethod]
    public void BitImageViewerShouldLeaveFullScreenOnAPlainEscapeOnly()
    {
        var changes = new List<bool>();
        var com = Render(p => p.Add(x => x.FullScreen, true), changes);

        // An Escape with a modifier is the surface around the viewer's.
        com.Find("#imv").KeyDown(new KeyboardEventArgs { Key = "Escape", Code = "Escape", CtrlKey = true });
        Assert.IsTrue(com.Instance.FullScreen);

        com.Find("#imv").KeyDown(new KeyboardEventArgs { Key = "Escape", Code = "Escape" });
        Assert.IsFalse(com.Instance.FullScreen);
        Assert.IsNull(com.Find("#imv").GetAttribute("data-bit-esc"), "a viewer in the page claims no Escape");

        // In the page, the key is not the viewer's.
        com.Find("#imv").KeyDown(new KeyboardEventArgs { Key = "Escape", Code = "Escape" });
        CollectionAssert.AreEqual(new[] { false }, changes);
    }

    [TestMethod]
    public void BitImageViewerFullScreenBoundOneWayShouldClaimNoEscape()
    {
        var com = Render(p => p.Add(x => x.FullScreen, true));

        Assert.IsNull(com.Find("#imv").GetAttribute("data-bit-esc"));

        com.Find("#imv").KeyDown(new KeyboardEventArgs { Key = "Escape", Code = "Escape" });
        Assert.IsTrue(com.Instance.FullScreen);
    }

    [TestMethod]
    public void BitImageViewerShouldHoldThePageWhileFullScreenAndGiveItBack()
    {
        SetupHandle();
        var changes = new List<bool>();
        var com = Render(p => p.Add(x => x.ShowFullScreenButton, true), changes);

        Assert.AreEqual(0, Calls("BitBlazorUI.Utils.lockScroll"));

        com.Find(".bit-imv-fsb").Click();

        // The focus it came from is remembered, the page held still, Tab kept inside, and the focus taken into the frame.
        Assert.AreEqual("imv", Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"].Single().Arguments[0]);
        Assert.AreEqual("imv", Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Single().Arguments[0]);
        Assert.AreEqual("imv", Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Single().Arguments[0]);

        // The button pressed is focused before the focus is remembered - WebKit does not focus a button it dispatched a
        // click for - and the frame after it (the order is pinned by the E2E tests, in WebKit among the others).
        Assert.AreEqual(2, Calls("Blazor._internal.domWrapper.focus"));

        // A render that changes nothing takes nothing a second time.
        com.Render(p => p.Add(x => x.MaxZoom, 6));
        Assert.AreEqual(1, Calls("BitBlazorUI.Utils.lockScroll"));

        com.Find(".bit-imv-cls").Click();

        Assert.AreEqual("imv", Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Single().Arguments[0]);
        Assert.AreEqual("imv", Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Single().Arguments[0]);

        // The focus is still inside the viewer, which stays in the page, so it is handed back.
        var restore = Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"].Single();
        CollectionAssert.AreEqual(new object?[] { "imv", true, "imv" }, restore.Arguments.ToArray());
    }

    [TestMethod]
    public async Task BitImageViewerDisposedWhileFullScreenShouldGiveThePageBack()
    {
        SetupHandle();
        Render(p => p.Add(x => x.FullScreen, true));

        Assert.AreEqual(1, Calls("BitBlazorUI.Utils.lockScroll"));

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(1, Calls("BitBlazorUI.Utils.unlockScroll"));
        Assert.AreEqual(1, Calls("BitBlazorUI.Utils.disposeFocusTrap"));
        Assert.AreEqual(1, Calls("BitBlazorUI.Utils.forgetFocus"));
        Assert.AreEqual(0, Calls("BitBlazorUI.Utils.restoreFocus"));
    }

    [TestMethod]
    public void BitImageViewerEnteringFullScreenFromCodeShouldLeaveTheFocusOriginAlone()
    {
        SetupHandle();
        var com = Render(p => p.Add(x => x.ShowFullScreenButton, true), changes: []);

        com.InvokeAsync(() => com.Instance.EnterFullScreen()).Wait();

        // Only the frame is focused: what the focus was on is the page's, and that is what it is handed back to.
        Assert.AreEqual(1, Calls("Blazor._internal.domWrapper.focus"));
        Assert.AreEqual(1, Calls("BitBlazorUI.Utils.storeFocus"));
    }

    [TestMethod]
    public void FullScreenBitImageViewerShouldZoomWithThePlainWheel()
    {
        var module = SetupHandle();
        var changes = new List<bool>();
        var com = Render(p => p.Add(x => x.ShowFullScreenButton, true), changes);

        static bool Wheel(object options) => (bool)options.GetType().GetProperty("Wheel")!.GetValue(options)!;

        Assert.IsFalse(Wheel(Context.JSInterop.Invocations[Setup].Single().Arguments[2]!));

        com.Find(".bit-imv-fsb").Click();

        Assert.IsTrue(Wheel(module.Invocations["update"].Last().Arguments[0]!));
    }

    [TestMethod]
    public void DisabledBitImageViewerShouldNotGoFullScreen_ButShouldLeaveIt()
    {
        var changes = new List<bool>();
        var com = Render(p =>
        {
            p.Add(x => x.Disabled, true);
            p.Add(x => x.ShowFullScreenButton, true);
        }, changes);

        var button = com.Find(".bit-imv-fsb");
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));

        com.InvokeAsync(() => com.Instance.EnterFullScreen()).Wait();
        Assert.IsFalse(com.Instance.FullScreen);

        // One that is full screen already is never left with no way back.
        com.Render(p => p.Add(x => x.FullScreen, true));
        Assert.IsFalse(com.Find(".bit-imv-fsb").HasAttribute("disabled"));

        com.Find(".bit-imv-cls").Click();
        Assert.IsFalse(com.Instance.FullScreen);
        CollectionAssert.AreEqual(new[] { false }, changes);
    }

    [TestMethod]
    public void BitImageViewerShouldEnterAndExitFullScreenFromCode()
    {
        var changes = new List<bool>();
        var com = Render(changes: changes);

        com.InvokeAsync(() => com.Instance.EnterFullScreen()).Wait();
        Assert.IsTrue(com.Instance.FullScreen);
        Assert.IsTrue(com.Find("#imv").ClassList.Contains("bit-imv-fsc"), "drawn full screen with no render of the page");

        com.InvokeAsync(() => com.Instance.ExitFullScreen()).Wait();
        Assert.IsFalse(com.Instance.FullScreen);
        Assert.IsFalse(com.Find("#imv").ClassList.Contains("bit-imv-fsc"));

        CollectionAssert.AreEqual(new[] { true, false }, changes);
    }

    [TestMethod]
    public void FullScreenOnlyBitImageViewerShouldCarryItsClass()
    {
        var com = Render(p => p.Add(x => x.FullScreenOnly, true));

        Assert.IsTrue(com.Find("#imv").ClassList.Contains("bit-imv-fso"));
    }

    [TestMethod]
    public void FullScreenBitImageViewerShouldApplyTheClassesAndStylesOfItsFullScreenParts()
    {
        var com = Render(p =>
        {
            p.Add(x => x.FullScreen, true);
            p.Add(x => x.Texts, new BitImageViewerTexts { ExitFullScreen = "Cerrar" });
            p.Add(x => x.Classes, new BitImageViewerClassStyles { FullScreen = "c-fsc", CloseButton = "c-cls" });
            p.Add(x => x.Styles, new BitImageViewerClassStyles { FullScreen = "margin:1px", CloseButton = "margin:2px" });
        });

        var root = com.Find("#imv");
        Assert.IsTrue(root.ClassList.Contains("c-fsc"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        var close = com.Find(".bit-imv-cls");
        Assert.IsTrue(close.ClassList.Contains("c-cls"));
        Assert.AreEqual("margin:2px", close.GetAttribute("style"));
        Assert.AreEqual("Cerrar", close.GetAttribute("aria-label"));

        // The full screen parts are only the full screen's.
        com.Render(p => p.Add(x => x.FullScreen, false));
        Assert.IsFalse(com.Find("#imv").ClassList.Contains("c-fsc"));
        Assert.IsFalse((com.Find("#imv").GetAttribute("style") ?? "").Contains("margin:1px"));
    }
}
