﻿using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.FileUpload
{
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

            var bitFileUpload = com.Find(".bit-fl-up");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitFileUpload.ClassList.Contains($"bit-fl-up-{visualClass}") && bitFileUpload.ClassList.Contains($"bit-fl-up"));
        }

        [DataTestMethod,
           DataRow(true),
           DataRow(false)
        ]
        public void BitFileUpload_MultipleAttribute_Test(bool isMultiFile)
        {
            var com = RenderComponent<BitFileUploadTest>(parameters =>
            {
                parameters.Add(p => p.IsMultiFile, isMultiFile);
            });

            var bitFileUpload = com.Find("input[type=file]");
            var attribute = bitFileUpload.GetAttribute("multiple");
            var isMultiFileStr = isMultiFile == true ? "" : null;
            Assert.AreEqual(isMultiFileStr, attribute);
        }

        [TestMethod]
        public void BitFileUpload_AcceptAttribute_Test()
        {
            var acceptedExtensions = new List<string> { ".mp4", ".mp3" };
            var com = RenderComponent<BitFileUploadTest>(parameters =>
            {
                parameters.Add(p => p.AcceptedExtensions, acceptedExtensions);
            });

            var bitFileUpload = com.Find("input[type=file]");
            var attribute = bitFileUpload.GetAttribute("accept");
            Assert.AreEqual(".mp4,.mp3", attribute);
        }
    }
}
