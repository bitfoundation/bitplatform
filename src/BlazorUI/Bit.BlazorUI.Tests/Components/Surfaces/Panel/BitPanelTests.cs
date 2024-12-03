using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using AngleSharp.Css.Dom;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

[TestClass]
public class BitPanelTests : BunitTestContext
{
    private bool isPanelOpen = true;

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelBlockingTest(bool blocking)
    {
        var com = RenderComponent<BitPanel>(parameters =>
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
    public void BitPanelModelessTest(bool isModeless)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.Modeless, isModeless);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-pnl");
        Assert.AreEqual(element.Attributes["aria-modal"].Value, (isModeless is false).ToString());

        var elementOverlay = com.FindAll(".bit-pnl-ovl");
        Assert.AreEqual(isModeless ? 0 : 1, elementOverlay.Count);
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitPanelIsOpenTest(bool isOpen)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var container = com.Find(".bit-pnl-cnt");
        Assert.AreEqual(isOpen, container.GetStyle().CssText.Contains("opacity: 1"));
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-S-A-Id")
    ]
    public void BitPanelSubtitleAriaIdTest(string subtitleAriaId)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.SubtitleAriaId, subtitleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-pnl");

        if (subtitleAriaId == null)
        {
            Assert.IsFalse(element.HasAttribute("aria-describedby"));
        }
        else if (subtitleAriaId == string.Empty)
        {
            Assert.AreEqual(element.Attributes["aria-describedby"].Value, string.Empty);
        }
        else
        {
            Assert.AreEqual(element.Attributes["aria-describedby"].Value, subtitleAriaId);
        }
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-T-A-Id")
    ]
    public void BitPanelTitleAriaIdTest(string titleAriaId)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.TitleAriaId, titleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-pnl");

        if (titleAriaId == null)
        {
            Assert.IsFalse(element.HasAttribute("aria-labelledby"));
        }
        else if (titleAriaId == string.Empty)
        {
            Assert.AreEqual(element.Attributes["aria-labelledby"].Value, string.Empty);
        }
        else
        {
            Assert.AreEqual(element.Attributes["aria-labelledby"].Value, titleAriaId);
        }
    }

    [TestMethod]
    public void BitPanelContentTest()
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        var elementContent = com.Find(".bit-pnl-bdy");

        elementContent.MarkupMatches("<div class=\"bit-pnl-bdy\"><div>Test Content</div></div>");
    }

    [TestMethod]
    public void BitPanelFooterContentTest()
    {
        var footerContent = "<div>Test Footer Content</div>";

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FooterTemplate, footerContent);
        });

        var elementContent = com.Find(".bit-pnl-fcn :first-child");

        elementContent.MarkupMatches(footerContent);
    }

    [TestMethod]
    public void BitPanelHeaderContentTest()
    {
        const string headerContent = "<div>Test Header Content</div>";

        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HeaderTemplate, headerContent);
        });

        var elementContent = com.Find(".bit-pnl-hcn :first-child");

        elementContent.MarkupMatches(headerContent);
    }

    [TestMethod]
    public void BitPanelCloseWhenClickOutOfPanelTest()
    {
        var com = RenderComponent<BitPanel>(parameters =>
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
    public void BitPanelOnDismissShouldWorkCorrect()
    {
        var isOpen = true;
        var currentCount = 0;
        var com = RenderComponent<BitPanel>(parameters =>
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
        var com = RenderComponent<BitPanel>(parameters =>
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
