using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The exported PNG's own header is what these measure - the picture's dimensions read back out of
/// the bytes, rather than "some bytes came back".
/// </summary>
[TestClass]
public class CanvasTests : ButilPageTest
{
    [TestMethod]
    public async Task SetSize_Changes_The_Pixel_Buffer()
    {
        await ClickAndExpectAsync("canvas-size", "canvas:size:60/40");
    }

    [TestMethod]
    public async Task A_Drawn_Canvas_Exports_A_Png_Of_The_Buffers_Size()
    {
        await ClickAndExpectAsync("canvas-export", "canvas:export:True/60x40");
    }

    [TestMethod]
    public async Task Capture_With_One_Dimension_Keeps_The_Aspect_Ratio()
    {
        // The source is 200x100, so 50 wide comes back 25 tall.
        await ClickAndExpectAsync("canvas-capture", "canvas:capture:50x25");
    }

    [TestMethod]
    public async Task ToDataUrl_Returns_A_Png_Data_Url()
    {
        await ClickAndExpectAsync("canvas-dataurl", "canvas:dataurl:True");
    }

    [TestMethod]
    public async Task Clear_Reports_Rather_Than_Throwing_On_A_Non_Canvas()
    {
        await ClickAndExpectAsync("canvas-clear", "canvas:clear:True/False");
    }
}
