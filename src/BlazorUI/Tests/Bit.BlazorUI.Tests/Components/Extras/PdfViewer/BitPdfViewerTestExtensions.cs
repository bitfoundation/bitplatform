using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// A copy of the JS calls made under <paramref name="identifier"/> so far, taken on the renderer's dispatcher.
    /// The viewer keeps calling into JS after <see cref="WaitForPages"/> returns (its render loop resumes on the
    /// dispatcher after each Task.Delay), and bUnit records the calls in a plain dictionary of lists, so a read from
    /// the test thread could run into one being added ("Collection was modified"). Inside a WaitForAssertion or a
    /// JS handler, which already run on the dispatcher, the copy is taken inline.
    /// </summary>
    public static IReadOnlyList<JSRuntimeInvocation> InvocationsOf(this BunitContext context, string identifier)
    {
        return context.Renderer.Dispatcher.InvokeAsync(() => context.JSInterop.Invocations[identifier].ToArray()).GetAwaiter().GetResult();
    }
}
