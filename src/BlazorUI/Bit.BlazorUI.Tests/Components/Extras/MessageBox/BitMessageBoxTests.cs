using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MessageBox;

[TestClass]
public class BitMessageBoxTests : BunitTestContext
{
    [TestMethod]
    public void BitMessageBoxShouldRenderTitleBodyAndDefaultOk()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.Title, "Sample Title");
            parameters.Add(p => p.Body, "Sample Body");
        });

        Assert.IsTrue(component.Markup.Contains("Sample Title"));
        Assert.IsTrue(component.Markup.Contains("Sample Body"));

        var okButtonText = component.Find(".bit-msb-ftr .bit-btn-prt");
        Assert.AreEqual("Ok", okButtonText.TextContent);
    }

    [TestMethod]
    public void BitMessageBoxShouldRenderCustomOkText()
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OkText, "Confirm");
        });

        var okButton = component.Find(".bit-msb-ftr .bit-btn-prt");

        Assert.AreEqual("Confirm", okButton.TextContent);
    }

    [TestMethod]
    public void BitMessageBoxShouldInvokeOnCloseFromCloseButton()
    {
        var closed = 0;

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        var closeButton = component.Find(".bit-msb-hdr .bit-btn");

        closeButton.Click();

        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public void BitMessageBoxShouldInvokeOnCloseFromOkButton()
    {
        var closed = 0;

        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        var okButton = component.Find(".bit-msb-ftr .bit-btn");

        okButton.Click();

        Assert.AreEqual(1, closed);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMessageBoxShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitMessageBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-msb");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }
}
