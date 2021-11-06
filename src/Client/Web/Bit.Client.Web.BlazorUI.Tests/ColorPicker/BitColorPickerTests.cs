using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Bit.Client.Web.BlazorUI.Tests.ColorPicker
{
    [TestClass]
    public class BitColorPickerTests : BunitTestContext
    {
        [TestInitialize]
        public void SetupJsInteropMode()
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [DataTestMethod, DataRow(Visual.Fluent),
            DataRow(Visual.Cupertino),
            DataRow(Visual.Material)]
        public void BitColorPickerMustRespectBasicClasses(Visual visual)
        {
            var cut = RenderComponent<BitColorPickerTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Color, "rgb(255,255,255)");
            });

            var BitColorPicker = cut.Find(".bit-clr-pkr");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(BitColorPicker.ClassList.Contains($"bit-clr-pkr-{visualClass}") && BitColorPicker.ClassList.Contains($"bit-clr-pkr"));
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)]
        public void BitColorPickerMustRespecUiChange(bool visibility)
        {
            var cut = RenderComponent<BitColorPickerTest>(parameters =>
            {
                parameters.Add(p => p.ShowAlphaSlider, visibility);
                parameters.Add(p => p.ShowPreview, visibility);
                parameters.Add(p => p.Color, "rgb(255,255,255)");
            });

            var alphaSlider = cut.Find(".bit-clr-pkr .alpha-slider");
            var previewBox = cut.Find(".bit-clr-pkr .preview-box");

            if (visibility)
            {
                Assert.IsTrue(alphaSlider.ToMarkup().Contains("display: block"));
                Assert.IsTrue(previewBox.ToMarkup().Contains("display: block"));
            }
            else
            {
                Assert.IsTrue(alphaSlider.ToMarkup().Contains("display: none"));
                Assert.IsTrue(previewBox.ToMarkup().Contains("display: none"));
            }
        }

        [DataTestMethod,
            DataRow("#fc5603", "#fc5603", "rgb(252,86,3)", 1),
            DataRow("rgba(3,98,252,0.3)", "#0362fc", "rgb(3,98,252)", 0.3),
            DataRow("rgb(252,3,240)", "#fc03f0", "rgb(252,3,240)", 1)]
        public void BitColorPickerMustRespecValueChange(string color, string hex, string rgb, double alpha)
        {
            var cut = RenderComponent<BitColorPickerTest>(parameters =>
            {
                parameters.Add(p => p.Color, color);
            });

            Assert.AreEqual(color, cut.Instance.Color);
            Assert.AreEqual(hex, cut.Instance.ElementReference.Hex);
            Assert.AreEqual(rgb, cut.Instance.ElementReference.Rgb);
            Assert.AreEqual(alpha, cut.Instance.Alpha);
        }
    }
}
