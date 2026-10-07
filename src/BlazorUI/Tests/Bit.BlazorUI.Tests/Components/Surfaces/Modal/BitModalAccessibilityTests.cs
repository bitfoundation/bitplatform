using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalAccessibilityTests : BunitTestContext
{
    [TestMethod]
    public void BitModalShouldBeNamedByTheTitleItShows()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Release notes");
        });

        var title = com.Find(".bit-mdl-hdr");

        Assert.IsFalse(string.IsNullOrEmpty(title.Id));
        Assert.AreEqual(title.Id, com.Find(".bit-mdl-ctn").GetAttribute("aria-labelledby"));

        // The text title is announced as the heading it looks like.
        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual("2", title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitModalShouldNotBeNamedByTheWholeOfAHeaderTemplate()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Header, (RenderFragment)(b =>
            {
                b.AddMarkupContent(0, "<span id=\"docs-title\">Search the docs</span>");
                b.AddMarkupContent(1, "<input placeholder=\"Search here...\" />");
            }));
        });

        var header = com.Find(".bit-mdl-hdr");

        // Everything the template holds - the search box beside the title too - would otherwise be the name, so the
        // template is neither pointed at nor called a heading.
        Assert.IsNull(com.Find(".bit-mdl-ctn").GetAttribute("aria-labelledby"));
        Assert.IsTrue(string.IsNullOrEmpty(header.Id));
        Assert.IsNull(header.GetAttribute("role"));

        // It names the Modal through the title it points TitleAriaId at.
        com.Render(parameters => parameters.Add(p => p.TitleAriaId, "docs-title"));

        Assert.AreEqual("docs-title", com.Find(".bit-mdl-ctn").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitModalShouldPreferTheNameItWasGiven()
    {
        var pointed = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Shown title");
            parameters.Add(p => p.TitleAriaId, "elsewhere");
        });

        Assert.AreEqual("elsewhere", pointed.Find(".bit-mdl-ctn").GetAttribute("aria-labelledby"));

        // An aria-labelledby would take precedence over an AriaLabel, so the title stands down for one.
        var labelled = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Shown title");
            parameters.Add(p => p.AriaLabel, "Given name");
        });

        var content = labelled.Find(".bit-mdl-ctn");
        Assert.IsNull(content.GetAttribute("aria-labelledby"));
        Assert.AreEqual("Given name", content.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitModalWithoutATitleShouldNotPointAtOne()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        Assert.IsNull(com.Find(".bit-mdl-ctn").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitModalCloseIconShouldBeHiddenFromAssistiveTechnologies()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        // The button carries the name; the glyph inside it is decoration.
        Assert.AreEqual("Close", com.Find(".bit-mdl-cls").GetAttribute("aria-label"));
        Assert.AreEqual("true", com.Find(".bit-mdl-cls i").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitModalCloseButtonShouldBeDisabledWithTheModal()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
        });

        Assert.IsFalse(com.Find(".bit-mdl-cls").HasAttribute("disabled"));

        // A disabled Modal ignores the press, so the button says so instead of taking it.
        com.Render(parameters => parameters.Add(p => p.Disabled, true));

        Assert.IsTrue(com.Find(".bit-mdl-cls").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitModalCanCloseShouldKeepTheModalOpenWhenItRefuses()
    {
        var asked = 0;
        var dismissed = 0;
        var isOpen = true;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CanClose, () => { asked++; return Task.FromResult(false); });
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.Find(".bit-mdl-cls").Click();
        com.Find(".bit-mdl-ovl").Click();
        _ = com.PressEscape();

        // Every way the user dismisses the Modal is put to the guard, none of them closes it, and the refusal is
        // answered with the pulse rather than with nothing.
        Assert.AreEqual(3, asked);
        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, dismissed);
        Assert.AreEqual(1, com.FindAll(".bit-mdl-ctn").Count);
        var content = com.Find(".bit-mdl-ctn");
        Assert.IsTrue(content.ClassList.Contains("bit-mdl-bna") || content.ClassList.Contains("bit-mdl-bnb"));
    }

    [TestMethod]
    public void BitModalCanCloseShouldLetTheModalCloseWhenItAgrees()
    {
        var isOpen = true;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.CanClose, () => Task.FromResult(true));
        });

        com.Find(".bit-mdl-ovl").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public void BitModalCanCloseShouldBeAskedOnceWhileItIsStillAnswering()
    {
        var asked = 0;
        var isOpen = true;
        var answer = new TaskCompletionSource<bool>();

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CanClose, () => { asked++; return answer.Task; });
        });

        // The guard is waiting on a confirmation of its own; the dismissals made in the meantime wait on it too
        // rather than putting the same question to the user again.
        com.Find(".bit-mdl-cls").Click();
        com.Find(".bit-mdl-ovl").Click();
        _ = com.PressEscape();

        Assert.AreEqual(1, asked);
        Assert.IsTrue(isOpen);

        com.InvokeAsync(() => answer.SetResult(true));

        com.WaitForAssertion(() => Assert.IsFalse(isOpen));

        Assert.AreEqual(1, asked);
    }

    [TestMethod]
    public void BitModalCanCloseShouldNotDismissAModalTheAppClosedWhileItWasAnswering()
    {
        var dismissed = 0;
        var answer = new TaskCompletionSource<bool>();

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.CanClose, () => answer.Task);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.Find(".bit-mdl-ovl").Click();

        // The application closes the Modal on its own terms while the guard is still out.
        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        var dismissedByTheApp = dismissed;

        com.InvokeAsync(() => answer.SetResult(true));

        // The late answer finds nothing left to dismiss, so nothing more is reported.
        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-mdl-ctn").Count));
        Assert.AreEqual(dismissedByTheApp, dismissed);
    }

    [TestMethod]
    public void BitModalShouldLeaveAnEscapeClaimedInsideItToThatLayer()
    {
        var escapes = 0;
        var isOpen = true;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
        });

        // A press a layer inside the Modal answered first - a dropdown opened from inside it closed its popup with
        // it, an input method used it to cancel what it was composing - reaches the Modal's handler, and the script
        // does not forward it (Utils.watchEscape).
        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // One press closes one layer: the Modal is told about the key, as it is about every Escape, but it stays
        // open and does not pulse either.
        Assert.AreEqual(1, escapes);
        Assert.IsTrue(isOpen);
        Assert.AreEqual(1, com.FindAll(".bit-mdl-ctn").Count);
        Assert.IsFalse(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bna"));
    }

    [TestMethod]
    public async Task BitModalShouldWatchTheEscapePressesFromItsRootWhileItIsOpen()
    {
        var com = RenderComponent<BitModal>(parameters => parameters.Add(p => p.DefaultIsOpen, true));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.watchEscape"].Count));

        var rootId = com.Find(".bit-mdl").Id;
        Assert.AreEqual(rootId, Context.JSInterop.Invocations["BitBlazorUI.Utils.watchEscape"][0].Arguments[0]);

        // Part of the watch is on the window, so it is taken back with the close rather than left to the element.
        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unwatchEscape"].Count));
        Assert.AreEqual(rootId, Context.JSInterop.Invocations["BitBlazorUI.Utils.unwatchEscape"][0].Arguments[0]);
    }

    [TestMethod]
    public async Task BitModalShouldIgnoreAForwardedEscapeOnceItIsClosed()
    {
        var dismissed = 0;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.AreEqual(1, dismissed));

        // A press the script forwarded just before the Modal closed arrives after it: there is nothing left to dismiss.
        await com.ForwardEscape();

        Assert.AreEqual(1, dismissed);
    }

    [TestMethod]
    public async Task BitModalCanCloseShouldNotBeAskedWhenTheAppClosesIt()
    {
        var asked = 0;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.CanClose, () => { asked++; return Task.FromResult(false); });
        });

        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-mdl").Count));
        Assert.AreEqual(0, asked);
    }
}
