﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

[TestClass]
public class BitFileUploadTests : BunitTestContext
{
    [DataTestMethod,
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

    [DataTestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUploadMultipleAttributeTest(bool isMultiSelect)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.IsMultiSelect, isMultiSelect);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");
        Assert.AreEqual(isMultiSelect, bitFileUpload.HasAttribute("multiple"));
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

    [DataTestMethod,
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
}
