using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfStringTests
{
    [TestMethod]
    public void AsTextShouldDecodeTheDashBytesOfPdfDocEncoding()
    {
        // PDF 32000-1 Annex D.2: 0x84 is emdash and 0x85 is endash.
        var text = new BitPdfString([0x84, 0x85]).AsText();

        Assert.AreEqual("—–", text);
    }
}
