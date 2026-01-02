using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProPanel;

[TestClass]
public class BitProPanelTests : BunitTestContext
{
    [TestMethod]
    public void BitProPanelContentTest()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        var elementContent = com.Find(".bit-ppl-bdy");

        elementContent.MarkupMatches("<div class=\"bit-ppl-bdy\"><div>Test Content</div></div>");
    }

    [TestMethod]
    public void BitProPanelFooterContentTest()
    {
        var footerContent = "<div>Test Footer Content</div>";

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Footer, footerContent);
        });

        var elementContent = com.Find(".bit-ppl-fcn :first-child");

        elementContent.MarkupMatches(footerContent);
    }

    [TestMethod]
    public void BitProPanelHeaderContentTest()
    {
        const string headerContent = "<div>Test Header Content</div>";

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Header, headerContent);
        });

        var elementContent = com.Find(".bit-ppl-hcn :first-child :first-child");

        elementContent.MarkupMatches(headerContent);
    }

    [TestMethod]
    public void BitProPanelShouldRenderHeaderTextAndCloseButton()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderText, "Header Text");
            parameters.Add(p => p.ShowCloseButton, true);
        });

        var header = com.Find(".bit-ppl-hdr");
        var closeButton = com.Find(".bit-ppl-cls");

        Assert.IsNotNull(closeButton);
        Assert.AreEqual("Header Text", header.TextContent);
    }

    [TestMethod]
    public void BitProPanelShouldRenderFooterTextWhenFooterTemplateMissing()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FooterText, "Footer Text");
        });

        var footer = com.Find(".bit-ppl-fcn");

        Assert.AreEqual("Footer Text", footer.TextContent);
    }

    [TestMethod]
    public void BitProPanelModeFullShouldApplyCssClass()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ModeFull, true);
        });

        var root = com.Find(".bit-ppl");

        Assert.IsTrue(root.ClassList.Contains("bit-ppl-mfl"));
    }

    [TestMethod]
    public void BitProPanelShouldInvokeOnOpenWhenOpening()
    {
        var opened = 0;
        var isOpen = false;

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnOpen, EventCallback.Factory.Create(this, () => opened++));
        });

        com.SetParametersAndRender(p => p.Add(x => x.IsOpen, true));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, opened);
        });
    }

    [TestMethod]
    public void BitProPanelShouldInvokeOnDismissWhenCloseButtonClicked()
    {
        var dismissed = 0;
        var isOpen = true;

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
            parameters.Add(p => p.ShowCloseButton, true);
        });

        com.Find(".bit-ppl-cls").Click();

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, dismissed);
            Assert.IsFalse(isOpen);
        });
    }

    [TestMethod]
    public void BitProPanelShouldRespectIsEnabled()
    {
        var dismissed = 0;
        var isOpen = true;

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        com.Find(".bit-ppl-cls").Click();

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, dismissed);
            Assert.IsTrue(isOpen);
            Assert.IsTrue(com.Find(".bit-ppl").ClassList.Contains("bit-dis"));
        });
    }
}
