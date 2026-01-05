using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

[TestClass]
public class BitFileInputTests : BunitTestContext
{
    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputHasBasicClass(bool isEnabled)
    {
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitFileInput = com.Find(".bit-fin-fi");

        Assert.IsNotNull(bitFileInput);
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputMultipleAttributeTest(bool isMultiple)
    {
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, isMultiple);
        });

        var bitFileInput = com.Find(".bit-fin-fi");
        Assert.AreEqual(isMultiple, bitFileInput.HasAttribute("multiple"));
    }

    [TestMethod]
    public void BitFileInputAcceptAttributeTest()
    {
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Accept, ".jpg,.png");
        });

        var bitFileInput = com.Find(".bit-fin-fi");
        Assert.AreEqual(".jpg,.png", bitFileInput.GetAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputLabelTest()
    {
        var label = "Select File";
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var bitFileInputLabel = com.Find(".bit-fin-lbl");
        Assert.AreEqual(label, bitFileInputLabel.TextContent);
    }

    [TestMethod]
    public void BitFileInputIsEnabledTest()
    {
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        var bitFileInput = com.Find(".bit-fin-fi");
        Assert.IsTrue(bitFileInput.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitFileInputShowRemoveButtonTest()
    {
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        Assert.IsNotNull(com);
    }

    [TestMethod]
    public void BitFileInputHideFileListTest()
    {
        var com = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.HideFileList, true);
        });

        var fileList = com.FindAll(".bit-fin-fl");
        Assert.IsTrue(fileList.Count == 0 || fileList[0].ChildElementCount == 0);
    }
}
