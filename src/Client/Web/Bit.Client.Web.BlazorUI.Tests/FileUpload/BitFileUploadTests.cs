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
        public void BitUploadFile_BaseTest(Visual visual)
        {
            var com = RenderComponent<BitFileUploadTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
            });

            var bitFileUpload = com.Find(".bit-fu");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitFileUpload.ClassList.Contains($"bit-fu-{visualClass}") && bitFileUpload.ClassList.Contains($"bit-fu"));
        }

        [DataTestMethod,
           DataRow(true),
           DataRow(false)
        ]
        public void BitFileUpload_MultipleTest(bool isMultiFile)
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
    }
}
