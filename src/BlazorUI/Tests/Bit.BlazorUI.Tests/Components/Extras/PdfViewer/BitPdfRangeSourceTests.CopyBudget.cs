using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeSourceTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OneByteCopySegmentsMustExhaustABoundedRequestBudget(bool download)
    {
        using var handler = new RangeHandler(8200) { OpenEndedLimit = 1 };
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);

        var error = await Assert.ThrowsAsync<IOException>(async () =>
        {
            if (download)
            {
                await using var stream = source.OpenRead();
                await stream.CopyToAsync(Stream.Null);
            }
            else await source.ReadAll();
        });

        Assert.AreEqual("BitPdfRangePermanentException", error.GetType().Name);
        StringAssert.Contains(error.Message, "too few bytes");
        // The probe, the grace segments, then a quarter chunk per request on average.
        Assert.IsTrue(handler.Requests.Count < 40, $"requests: {handler.Requests.Count}");
        Assert.AreEqual(8, source.Int("AccountedBytes"), "a failed copy leaves the cache untouched");
    }

    [TestMethod]
    public async Task SeeksMustNotSpendTheCopyBudget()
    {
        using var handler = new RangeHandler(200_000);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 4096);
        await using var stream = source.OpenRead();
        byte[] buffer = new byte[100];

        // Every seek closes the response, so each short read is a request of its own;
        // none is a server continuation, so none counts against the copy.
        for (int i = 0; i < 64; i++)
        {
            stream.Position = i * 3000;
            int read = await stream.ReadAsync(buffer);
            Assert.IsTrue(read > 0);
            CollectionAssert.AreEqual(handler.Bytes[(i * 3000)..(i * 3000 + read)], buffer[..read]);
        }

        Assert.IsTrue(handler.Requests.Count > 64);
    }

    [TestMethod]
    public async Task ACdnCappingOpenEndedResponsesAtAFewKilobytesShouldStillCopy()
    {
        using var handler = new RangeHandler(300_000) { OpenEndedLimit = 2048 };
        using var http = new HttpClient(handler);
        // A quarter chunk would be 16 KiB here; a segment of 1 KiB or more always earns its request.
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 64 * 1024);

        byte[] bytes = await source.ReadAll();

        CollectionAssert.AreEqual(handler.Bytes, bytes);
    }

    [TestMethod]
    [DataRow(4096)]
    [DataRow(2048)]
    public async Task ChunkSizedCopySegmentsShouldFinishWithAShortFinalSegment(int segment)
    {
        using var handler = new RangeHandler(100_003) { OpenEndedLimit = segment };
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 4096);

        byte[] bytes = await source.ReadAll();

        CollectionAssert.AreEqual(handler.Bytes, bytes);
        Assert.AreEqual(1 + (handler.Bytes.Length + segment - 1) / segment, handler.Requests.Count);
    }
}
