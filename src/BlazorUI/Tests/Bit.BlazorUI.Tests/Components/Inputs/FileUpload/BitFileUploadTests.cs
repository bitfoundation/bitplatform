using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

[TestClass]
public class BitFileUploadTests : BunitTestContext
{
    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitUploadFileHasBasicClass(bool isEnabled)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");

        Assert.IsNotNull(bitFileUpload);
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUploadMultipleAttributeTest(bool isMultiple)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, isMultiple);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");

        Assert.AreEqual(isMultiple, bitFileUpload.HasAttribute("multiple"));
    }

    [TestMethod]
    public void BitFileUploadAcceptAttributeTest()
    {
        var allowedExtensions = new List<string> { ".mp4", ".mp3" };

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, allowedExtensions);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");
        var attribute = bitFileUpload.GetAttribute("accept");

        Assert.AreEqual(".mp4,.mp3", attribute);
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUploadIsEnabledTest(bool isEnabled)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitFileUpload = com.Find(".bit-upl");
        var bitFileUploadInput = com.Find(".bit-upl-fi");

        if (isEnabled)
        {
            Assert.IsFalse(bitFileUpload.ClassList.Contains("bit-dis"));
            Assert.IsFalse(bitFileUploadInput.HasAttribute("disabled"));
        }
        else
        {
            Assert.IsTrue(bitFileUpload.ClassList.Contains("bit-dis"));
            Assert.IsTrue(bitFileUploadInput.HasAttribute("disabled"));
        }
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptUploadIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadIconName, "Play");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptUploadIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadIcon, BitIconInfo.Css("fa-solid fa-upload"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptFontAwesomeUploadIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadIcon, BitIconInfo.Fa("solid cloud-arrow-up"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptPauseIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.PauseIconName, "Pause");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptPauseIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.PauseIcon, BitIconInfo.Css("fa-solid fa-pause"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptPauseIconBitInfoParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.PauseIcon, BitIconInfo.Bi("pause-circle"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptCancelIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.CancelIconName, "Cancel");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptCancelIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.CancelIcon, BitIconInfo.Css("fa-solid fa-xmark"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptCancelIconBitInfoParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.CancelIcon, BitIconInfo.Fa("solid circle-xmark"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptRemoveIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveIconName, "Delete");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptRemoveIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveIcon, BitIconInfo.Css("fa-solid fa-trash"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptRemoveIconBitInfoParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveIcon, BitIconInfo.Bi("trash"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }
}
