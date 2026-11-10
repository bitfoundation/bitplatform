using System;
using System.IO;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfViewerStreamTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StreamDetailContentFailureMustNotHideIndependentDetailsButSourceFailureMustPropagate(bool sourceFailure)
    {
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new MemoryStream(TestPdf.HelloWorld())))));
        component.WaitForPages(1);
        var document = component.Instance.Document!;
        var error = sourceFailure
            ? (Exception)Activator.CreateInstance(typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfByteSourceIOException", true)!, ["Provider failed.", null])!
            : new BitPdfFormatException("Malformed form.");
        document.Catalog.Set("AcroForm", new BitPdfRef(12345, 0));
        document.Catalog.XRef = new ThrowingXRef(error);
        var element = new BitPdfDict(); element.Set("S", BitPdfName.Get("P"));
        var structure = new BitPdfDict(); structure.Set("K", element);
        document.Catalog.Set("StructTreeRoot", structure);
        if (sourceFailure)
            await Assert.ThrowsAsync<IOException>(() => component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync()));
        else
        {
            await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
            Assert.AreEqual(0, component.Instance.FormFields.Count);
            Assert.AreEqual(1, component.Instance.StructureTree.Count);
            Assert.AreEqual("P", component.Instance.StructureTree[0].Type);
        }
    }

    private sealed class ThrowingXRef(Exception error) : IBitPdfXRef
    {
        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false) => throw error;
        public object? FetchIfRef(object? value, bool suppressEncryption = false) => value is BitPdfRef reference ? Fetch(reference) : value;
    }
}
