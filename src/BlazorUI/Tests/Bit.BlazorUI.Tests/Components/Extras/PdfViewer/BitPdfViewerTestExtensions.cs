using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

internal static class BitPdfViewerTestExtensions
{
    /// <summary>
    /// Waits until the viewer holds <paramref name="count"/> pages, has finished loading, AND has rendered
    /// them. Each condition is checked on its own, so none of them leans on the order the load sets them in:
    /// PageCount is read off the page list, which fills before the load has finished; aria-busy follows the
    /// loading flag, which a load raises as soon as it starts (so the previous document's state never passes
    /// for the new one's); and the page slots (<c>[data-page]</c>, which the loading placeholder does not
    /// carry) are what the markup actually laid out - none at all for a closed document.
    /// </summary>
    public static void WaitForPages(this IRenderedComponent<BitPdfViewer> component, int count)
    {
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(count, component.Instance.PageCount);
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
            Assert.AreEqual(count, component.FindAll(".bit-pdv-pages .bit-pdv-page[data-page]").Count);
        });
    }
}
