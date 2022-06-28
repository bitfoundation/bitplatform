using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.ColorPicker
{
    [TestClass]
    public class BitColorTests
    {
        [DataTestMethod, DataRow("#5d0914")]
        public void BitColorHexToRgbTest(string color)
        {
            var bitColor = new BitColor(color);

            Assert.AreEqual(bitColor.Red, 93);
            Assert.AreEqual(bitColor.Green, 9);
            Assert.AreEqual(bitColor.Blue, 20);
            Assert.AreEqual(bitColor.Alpha, 1);
        }

        [DataTestMethod, DataRow("#5d0914", 0.6)]
        public void BitColorHexToRgbaTest(string color, double alpha)
        {
            var bitColor = new BitColor(color, alpha);

            Assert.AreEqual(bitColor.Red, 93);
            Assert.AreEqual(bitColor.Green, 9);
            Assert.AreEqual(bitColor.Blue, 20);
            Assert.AreEqual(bitColor.Alpha, alpha);
        }

        [DataTestMethod, DataRow("rgb(93,9,20)")]
        public void BitColorRgbToHexTest(string color)
        {
            var bitColor = new BitColor(color);

            Assert.AreEqual(bitColor.Hex, "#5d0914");
            Assert.AreEqual(bitColor.Alpha, 1);
        }

        [DataTestMethod, DataRow("rgb(93,9,20)", 0.6)]
        public void BitColorRgbaToHexTest(string color, double alpha)
        {
            var bitColor = new BitColor(color, alpha);

            Assert.AreEqual(bitColor.Hex, "#5d0914");
            Assert.AreEqual(bitColor.Alpha, alpha);
        }

        [DataTestMethod, DataRow(93, 9, 20, 0.6)]
        public void BitColorSetRgbaTest(int red, int green, int blue, double alpha)
        {
            var bitColor = new BitColor();
            bitColor.SetColorByRgba(red, green, blue, alpha);

            Assert.AreEqual(bitColor.Hex, "#5d0914");
            Assert.AreEqual(bitColor.Alpha, alpha);
        }

        // In computer graphics,  HSV and RGB parameters may be represented by integers from 0 to 255 instead of  real numbers.
        // In this case, the transformation does not cover every point in the target space, and rounding causes some distortion.
        // https://psychology.fandom.com/wiki/HSV_color_space
        [DataTestMethod, DataRow("#5d0914"), DataRow("#5c0914")]
        public void BitColorHexToHsvTest(string color)
        {
            var bitColor = new BitColor(color);

            Assert.AreEqual(bitColor.Hsv.Hue, 352);
            Assert.AreEqual(bitColor.Hsv.Saturation, 90);
            Assert.AreEqual(bitColor.Hsv.Value, 36);
            Assert.AreEqual(bitColor.Alpha, 1);
        }

        [DataTestMethod, DataRow(352, 90, 36, 0.9)]
        public void BitColorHsvToHexTest(double hue, double saturation, double value, double alpha)
        {
            var bitColor = new BitColor(hue, saturation, value, alpha);

            Assert.AreEqual(bitColor.Hex, "#5b0914");
        }
    }
}
