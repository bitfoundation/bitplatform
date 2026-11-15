using System.Collections.Generic;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeViewerTests
{
    [TestMethod]
    public void ContradictedRootCountShouldFallBackToBufferedLoadingBeforePublication()
    {
        byte[] bytes = BitPdfRangeEngineTests.WrongRootCount(scattered: false);
        using var handler = new FallbackHandler(bytes, weak: false);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        int loaded = 0;
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.OnDocumentLoaded, () => loaded++)
            .Add(v => v.OnError, errors.Add));

        component.WaitForAssertion(() => Assert.AreEqual(1, loaded, string.Join("; ", errors)));
        CollectionAssert.AreEqual(bytes, component.Instance.GetBytes());
        Assert.AreEqual(1, handler.FullRequests);
        Assert.AreEqual(BitPdfDocument.Load(bytes).PageCount, component.Instance.PageCount);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
    }
}
