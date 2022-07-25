using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.FileUpload;

[TestClass]
public class BitFileUploadTests : BunitTestContext
{
    [DataTestMethod,
       DataRow(Visual.Fluent),
       DataRow(Visual.Cupertino),
       DataRow(Visual.Material),
    ]
    public void BitUploadFile_HasBasicClasses(Visual visual)
    {
        var com = RenderComponent<BitFileUploadTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
        });

        var bitFileUpload = com.Find(".bit-upl");
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitFileUpload.ClassList.Contains($"bit-upl-{visualClass}") && bitFileUpload.ClassList.Contains($"bit-upl"));
    }

    [DataTestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUpload_MultipleAttribute_Test(bool isMultiSelect)
    {
        var com = RenderComponent<BitFileUploadTest>(parameters =>
        {
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
        });

        var bitFileUpload = com.Find(".file-input");
        var attribute = bitFileUpload.GetAttribute("multiple");
        var isMultiFileStr = isMultiSelect == true ? "" : null;
        Assert.AreEqual(isMultiFileStr, attribute);
    }

    [TestMethod]
    public void BitFileUpload_AcceptAttribute_Test()
    {
        var allowedExtensions = new List<string> { ".mp4", ".mp3" };
        var com = RenderComponent<BitFileUploadTest>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, allowedExtensions);
        });

        var bitFileUpload = com.Find(".file-input");
        var attribute = bitFileUpload.GetAttribute("accept");
        Assert.AreEqual(".mp4,.mp3", attribute);
    }

    [DataTestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUpload_IsEnabled_Test(bool isEnabled)
    {
        var com = RenderComponent<BitFileUploadTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitFileUpload = com.Find(".file-input");
        var hasDisabledAttribute = bitFileUpload.HasAttribute("disabled");

        if (isEnabled)
        {
            Assert.IsFalse(hasDisabledAttribute);
        }
        else
        {
            Assert.IsTrue(hasDisabledAttribute);
        }
    }
}
