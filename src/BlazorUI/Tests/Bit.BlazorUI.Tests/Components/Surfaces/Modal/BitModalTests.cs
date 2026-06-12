using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalTests : BunitTestContext
{
    private bool isModalOpen = true;

    [TestMethod,
        DataRow(null),
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsAlertTest(bool? isAlert)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsAlert, isAlert);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl");
        Assert.AreEqual(element?.Attributes?["role"]?.Value, isAlert.HasValue && isAlert.Value ? "alertdialog" : "dialog");
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsBlockingTest(bool isBlocking)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.Blocking, isBlocking);
            parameters.Add(p => p.IsOpen, isModalOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(1, bitModal.Count);

        var overlayElement = com.Find(".bit-mdl-ovl");
        overlayElement.Click();

        bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(isBlocking ? 1 : 0, bitModal.Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsOpenTest(bool isOpen)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var bitModel = com.FindAll(".bit-mdl");
        Assert.AreEqual(isOpen ? 1 : 0, bitModel.Count);
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-S-A-Id")
    ]
    public void BitModalSubtitleAriaIdTest(string subtitleAriaId)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.SubtitleAriaId, subtitleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl");

        if (subtitleAriaId == null)
        {
            Assert.IsFalse(element.HasAttribute("aria-describedby"));
        }
        else if (subtitleAriaId == string.Empty)
        {
            Assert.AreEqual(element?.Attributes?["aria-describedby"]?.Value, string.Empty);
        }
        else
        {
            Assert.AreEqual(element?.Attributes?["aria-describedby"]?.Value, subtitleAriaId);
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-T-A-Id")
    ]
    public void BitModalTitleAriaIdTest(string titleAriaId)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.TitleAriaId, titleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl");

        if (titleAriaId == null)
        {
            Assert.IsFalse(element.HasAttribute("aria-labelledby"));
        }
        else if (titleAriaId == string.Empty)
        {
            Assert.AreEqual(element?.Attributes["aria-labelledby"]?.Value, string.Empty);
        }
        else
        {
            Assert.AreEqual(element?.Attributes["aria-labelledby"]?.Value, titleAriaId);
        }
    }

    [TestMethod]
    public void BitModalContentTest()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        var elementContent = com.Find(".bit-mdl-ctn");

        elementContent.MarkupMatches("<div id:ignore style:ignore class=\"bit-mdl-ctn\"><div>Test Content</div></div>");
    }

    [TestMethod]
    public void BitModalCloseWhenClickOutOfModalTest()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isModalOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(1, bitModal.Count);

        var overlayElement = com.Find(".bit-mdl-ovl");
        overlayElement.Click();

        bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(0, bitModal.Count);
    }

    [TestMethod]
    public void BitModalOnDismissShouldWorkCorrect()
    {
        var isOpen = true;
        var currentCount = 0;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, newValue => isOpen = newValue);
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        var overlayElement = com.Find(".bit-mdl-ovl");

        overlayElement.Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, currentCount);
    }

    private void HandleIsOpenChanged(bool isOpen) => isModalOpen = isOpen;
}
