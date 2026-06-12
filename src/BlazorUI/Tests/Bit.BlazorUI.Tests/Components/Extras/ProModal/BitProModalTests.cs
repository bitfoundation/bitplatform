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
    public void BitProModalBlockingShouldPreventOverlayDismissButStillInvokeOverlayCallback()
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
            Assert.AreEqual(1, overlayClicked);
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
        DataRow(BitPosition.TopLeft, "bit-pmd-tlf"),
        DataRow(BitPosition.TopCenter, "bit-pmd-tcr"),
        DataRow(BitPosition.TopRight, "bit-pmd-trg"),
        DataRow(BitPosition.CenterLeft, "bit-pmd-clf"),
        DataRow(BitPosition.Center, "bit-pmd-ctr"),
        DataRow(BitPosition.CenterRight, "bit-pmd-crg"),
        DataRow(BitPosition.BottomLeft, "bit-pmd-blf"),
        DataRow(BitPosition.BottomCenter, "bit-pmd-bcr"),
        DataRow(BitPosition.BottomRight, "bit-pmd-brg")
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

    [TestMethod]
    public void BitProModalCloseButtonClickShouldCloseAndInvokeOnDismiss()
    {
        var dismissed = 0;
        var isOpen = true;

        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
        });

        com.Find(".bit-pmd-cls").Click();

        com.WaitForAssertion(() =>
        {
            Assert.IsFalse(isOpen);
            Assert.AreEqual(1, dismissed);
            Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public void BitProModalFullSizeShouldApplyFullWidthAndFullHeightClasses()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullSize, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fhe"));
    }

    [TestMethod]
    public void BitProModalFullWidthShouldApplyOnlyFullWidthClass()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullWidth, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.IsFalse(root.ClassList.Contains("bit-mdl-fhe"));
    }

    [TestMethod]
    public void BitProModalFullHeightShouldApplyOnlyFullHeightClass()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullHeight, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fhe"));
        Assert.IsFalse(root.ClassList.Contains("bit-mdl-fwi"));
    }

    [TestMethod]
    public void BitProModalModeFullShouldApplyCssClass()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ModeFull, true);
        });

        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-pmd-mfl"));
    }

    [TestMethod]
    public void BitProModalNoBorderShouldRemoveTopBorderClass()
    {
        var withBorder = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsTrue(withBorder.Find(".bit-mdl").ClassList.Contains("bit-pmd-tbr"));

        var noBorder = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoBorder, true);
        });

        Assert.IsFalse(noBorder.Find(".bit-mdl").ClassList.Contains("bit-pmd-tbr"));
    }

    [TestMethod]
    public void BitProModalShouldRenderHeaderFooterAndBodyContent()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "header-text");
            parameters.Add(p => p.FooterText, "footer-text");
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.ChildContent, "body-text");
        });

        var header = com.Find(".bit-pmd-hdr");
        var footer = com.Find(".bit-pmd-fcn");
        var body = com.Find(".bit-pmd-bdy");

        Assert.AreEqual("header-text", header.TextContent);
        Assert.AreEqual("footer-text", footer.TextContent);
        Assert.AreEqual("body-text", body.TextContent);
        Assert.AreEqual(1, com.FindAll(".bit-pmd-cls").Count);
    }

    [TestMethod]
    public void BitProModalShouldNotRenderHeaderContainerWhenNoHeaderOrCloseButton()
    {
        var com = RenderComponent<BitProModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-pmd-hcn").Count);
        Assert.AreEqual(0, com.FindAll(".bit-pmd-fcn").Count);
    }
}
