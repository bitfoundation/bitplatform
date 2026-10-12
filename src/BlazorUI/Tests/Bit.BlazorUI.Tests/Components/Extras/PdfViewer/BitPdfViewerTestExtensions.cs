using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

internal static class BitPdfViewerTestExtensions
{
    /// <summary>
    /// Waits until the viewer holds <paramref name="count"/> pages AND has rendered them. PageCount alone is
    /// not enough: it is read off the page list, which fills before the load has finished, so the markup can
    /// still be the loading placeholder (no layout class, no page slots, a disabled toolbar) when it flips.
    /// The surface's aria-busy reads the same flag the markup branches on, so once it says false the render
    /// that laid the pages out has landed.
    /// </summary>
    public static void WaitForPages(this IRenderedComponent<BitPdfViewer> component, int count)
    {
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(count, component.Instance.PageCount);
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
        });
    }
}
