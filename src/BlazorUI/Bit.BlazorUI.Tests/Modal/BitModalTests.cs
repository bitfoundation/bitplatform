using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Modal;

[TestClass]
public class BitModalTests : BunitTestContext
{
    private bool isModalOpen = true;

    [DataTestMethod,
        DataRow(null),
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsAlertTest(bool? isAlert)
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.IsAlert, isAlert);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl > div");
        Assert.AreEqual(element.Attributes["role"].Value, isAlert.HasValue && isAlert.Value ? "alertdialog" : "dialog");
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsBlockingTest(bool isBlocking)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsBlocking, isBlocking);
            parameters.Add(p => p.IsOpen, isModalOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(bitModal.Count, 1);

        var overlayElement = com.Find(".overlay");
        overlayElement.Click();

        bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(bitModal.Count, isBlocking ? 1 : 0);
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsModelessTest(bool isModeless)
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.IsModeless, isModeless);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl > div");
        Assert.AreEqual(element.Attributes["aria-modal"].Value, (isModeless is false).ToString());

        var elementOverlay = com.FindAll(".overlay");
        Assert.AreEqual(elementOverlay.Count, isModeless ? 0 : 1);
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsOpenTest(bool isOpen)
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var bitModel = com.FindAll(".bit-mdl");
        Assert.AreEqual(bitModel.Count, isOpen ? 1 : 0);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-S-A-Id")
    ]
    public void BitModalSubtitleAriaIdTest(string subtitleAriaId)
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.SubtitleAriaId, subtitleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl > div");

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
    public void BitModalTitleAriaIdTest(string titleAriaId)
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.TitleAriaId, titleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl > div");

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
    public void BitModalContentTest()
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var elementContent = com.Find(".scroll-content");

        elementContent.MarkupMatches("<div class=\"scroll-content\"><div>Test Content</div></div>");
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
        Assert.AreEqual(bitModal.Count, 1);

        var overlayElement = com.Find(".overlay");
        overlayElement.Click();

        bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(bitModal.Count, 0);
    }

    [TestMethod]
    public void BitModalOnDismissShouldWorkCorrect()
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var overlayElement = com.Find(".overlay");

        overlayElement.Click();

        Assert.AreEqual(1, com.Instance.CurrentCount);
    }

    [DataTestMethod,
        DataRow(BitModalPosition.Center),
        DataRow(BitModalPosition.TopLeft),
        DataRow(BitModalPosition.TopCenter),
        DataRow(BitModalPosition.TopRight),
        DataRow(BitModalPosition.CenterLeft),
        DataRow(BitModalPosition.CenterRight),
        DataRow(BitModalPosition.BottomLeft),
        DataRow(BitModalPosition.BottomCenter),
        DataRow(BitModalPosition.BottomRight)
    ]
    public void BitModalPositionTest(BitModalPosition position)
    {
        var com = RenderComponent<BitModalTest>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Position, position);
        });

        var modalElement = com.Find(".modal");

        var positionClass = position switch
        {
            BitModalPosition.Center => $"center",

            BitModalPosition.TopLeft => $"top-left",
            BitModalPosition.TopCenter => $"top-center",
            BitModalPosition.TopRight => $"top-right",

            BitModalPosition.CenterLeft => $"center-left",
            BitModalPosition.CenterRight => $"center-right",

            BitModalPosition.BottomLeft => $"bottom-left",
            BitModalPosition.BottomCenter => $"bottom-center",
            BitModalPosition.BottomRight => $"bottom-right",

            _ => $"center",
        };

        Assert.IsTrue(modalElement.ClassList.Contains(positionClass));
    }

    private void HandleIsOpenChanged(bool isOpen)
    {
        isModalOpen = isOpen;
    }
}
