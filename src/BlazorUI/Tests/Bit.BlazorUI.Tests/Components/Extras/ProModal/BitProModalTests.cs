using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProModal;

[TestClass]
public class BitProModalTests : BunitTestContext
{
    [TestMethod]
    public void BitProModalModelessShouldNotRenderOverlayAndShouldSetAriaModalFalse()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Modeless, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.AreEqual("false", root.Attributes["aria-modal"]?.Value);
        Assert.AreEqual(0, com.FindAll(".bit-mdl-ovl").Count);
    }

    [TestMethod]
    public void BitProModalBlockingShouldPreventOverlayDismissAndOverlayCallback()
    {
        var dismissed = 0;
        var overlayClicked = 0;
        var isOpen = true;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
            parameters.Add(p => p.OnOverlayClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => overlayClicked++));
        });

        com.Find(".bit-mdl-ovl").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsTrue(isOpen);
            Assert.AreEqual(0, dismissed);
            Assert.AreEqual(0, overlayClicked);
            Assert.AreEqual("alertdialog", com.Find(".bit-mdl").Attributes["role"]?.Value);
        });
    }

    [TestMethod]
    public void BitProModalShouldInvokeOnDismissWhenClosedByParent()
    {
        var dismissed = 0;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.SetParametersAndRender(parameters => parameters.Add(p => p.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, dismissed);
            Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public void BitProModalShouldForwardAriaIds()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.TitleAriaId, "title-id");
            parameters.Add(p => p.SubtitleAriaId, "subtitle-id");
        });

        var root = com.Find(".bit-mdl");

        Assert.AreEqual("title-id", root.Attributes["aria-labelledby"]?.Value);
        Assert.AreEqual("subtitle-id", root.Attributes["aria-describedby"]?.Value);
    }

    [TestMethod,
        DataRow(BitPosition.TopLeft, "bit-mdl-tlf"),
        DataRow(BitPosition.TopCenter, "bit-mdl-tcr"),
        DataRow(BitPosition.TopRight, "bit-mdl-trg"),
        DataRow(BitPosition.CenterLeft, "bit-mdl-clf"),
        DataRow(BitPosition.Center, "bit-mdl-ctr"),
        DataRow(BitPosition.CenterRight, "bit-mdl-crg"),
        DataRow(BitPosition.BottomLeft, "bit-mdl-blf"),
        DataRow(BitPosition.BottomCenter, "bit-mdl-bcr"),
        DataRow(BitPosition.BottomRight, "bit-mdl-brg")
    ]
    public void BitProModalPositionShouldApplyCssClass(BitPosition position, string positionClass)
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains(positionClass));
    }
}
