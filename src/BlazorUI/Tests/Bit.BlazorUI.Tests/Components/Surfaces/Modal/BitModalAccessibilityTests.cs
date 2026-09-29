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
    public void BitModalShouldBeNamedByAHeaderTemplateWithoutCallingItAHeading()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Header, (RenderFragment)(b => b.AddContent(0, "Search the docs")));
        });

        var title = com.Find(".bit-mdl-hdr");

        Assert.AreEqual(title.Id, com.Find(".bit-mdl-ctn").GetAttribute("aria-labelledby"));
        Assert.IsNull(title.GetAttribute("role"));
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
        com.Render(parameters => parameters.Add(p => p.IsEnabled, false));

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
        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

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
