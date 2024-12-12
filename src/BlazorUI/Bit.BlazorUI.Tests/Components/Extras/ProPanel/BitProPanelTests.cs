using AngleSharp.Css.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProPanel;

[TestClass]
public class BitProPanelTests : BunitTestContext
{
    private bool isPanelOpen = true;

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitProPanelBlockingTest(bool blocking)
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.Blocking, blocking);
            parameters.Add(p => p.IsOpen, isPanelOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var container = com.Find(".bit-pnl-cnt");
        Assert.IsTrue(container.GetStyle().CssText.Contains("opacity: 1"));

        var overlayElement = com.Find(".bit-pnl-ovl");
        overlayElement.Click();

        container = com.Find(".bit-pnl-cnt");
        if (blocking)
        {
            Assert.IsTrue(container.GetStyle().CssText.Contains("opacity: 1"));
        }
        else
        {
            Assert.AreEqual("", container.GetStyle().CssText);
        }
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitProPanelModelessTest(bool modeless)
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.Modeless, modeless);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-pnl");
        Assert.AreEqual(element.Attributes["aria-modal"].Value, (modeless is false).ToString());

        var elementOverlay = com.FindAll(".bit-pnl-ovl");
        Assert.AreEqual(modeless ? 0 : 1, elementOverlay.Count);
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitProPanelIsOpenTest(bool isOpen)
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var container = com.Find(".bit-pnl-cnt");
        Assert.AreEqual(isOpen, container.GetStyle().CssText.Contains("opacity: 1"));
    }

    [TestMethod]
    public void BitProPanelContentTest()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        var elementContent = com.Find(".bit-pnl-bdy");

        elementContent.MarkupMatches("<div class=\"bit-pnl-bdy\"><div>Test Content</div></div>");
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

        var elementContent = com.Find(".bit-pnl-fcn :first-child");

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

        var elementContent = com.Find(".bit-pnl-hcn :first-child");

        elementContent.MarkupMatches(headerContent);
    }

    [TestMethod]
    public void BitProPanelCloseWhenClickOutOfPanelTest()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isPanelOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var container = com.Find(".bit-pnl-cnt");
        Assert.IsTrue(container.GetStyle().CssText.Contains("opacity: 1"));

        var overlayElement = com.Find(".bit-pnl-ovl");
        overlayElement.Click();

        container = com.Find(".bit-pnl-cnt");
        Assert.AreEqual("", container.GetStyle().CssText);
    }

    [TestMethod]
    public void BitProPanelOnDismissShouldWorkCorrect()
    {
        var isOpen = true;
        var currentCount = 0;
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, newValue => isOpen = newValue);
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        var overlayElement = com.Find(".bit-pnl-ovl");

        overlayElement.Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, currentCount);
    }

    [DataTestMethod,
        DataRow(BitPanelPosition.End),
        DataRow(BitPanelPosition.Start),
        DataRow(BitPanelPosition.Top),
        DataRow(BitPanelPosition.Bottom),
        DataRow(null)
    ]
    public void BitPanelPositionTest(BitPanelPosition? position)
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            if (position.HasValue)
            {
                parameters.Add(p => p.Position, position.Value);
            }
        });

        var PanelElement = com.Find(".bit-pnl-cnt");

        var positionClass = position switch
        {
            BitPanelPosition.End => "bit-pnl-end",
            BitPanelPosition.Start => "bit-pnl-start",
            BitPanelPosition.Top => "bit-pnl-top",
            BitPanelPosition.Bottom => "bit-pnl-bottom",
            _ => "bit-pnl-end",
        };

        Assert.IsTrue(PanelElement.ClassList.Contains(positionClass));
    }

    private void HandleIsOpenChanged(bool isOpen) => isPanelOpen = isOpen;
}
