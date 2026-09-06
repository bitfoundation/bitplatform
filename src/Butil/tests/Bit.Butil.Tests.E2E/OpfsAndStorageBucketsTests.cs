using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class OpfsAndStorageBucketsTests : ButilObserversPageTest
{
    [TestMethod]
    public async Task Opfs_Write_Read_List_And_Remove_Roundtrips()
    {
        // written=True, contents, one entry listed, 10 bytes, removed=True, and gone afterwards.
        await ClickAndExpectAsync("opfs-roundtrip", "opfs:roundtrip:True/hello-opfs/1/10/True/False");
    }

    [TestMethod]
    public async Task Opfs_Sync_Access_Handle_Writes_Appends_And_Reads_At_An_Offset()
    {
        await ClickAndExpectAsync("opfs-sync", "opfs:sync:");

        var status = await CurrentStatusAsync();

        // createSyncAccessHandle is worker-only and not everywhere, so the harness reports its
        // absence rather than failing - a browser without it is a fact about the browser.
        if (status == "opfs:sync:unsupported") return;

        // 9 bytes written, 9 appended, 18 in the file, and the second record read back from offset 9 -
        // which is the whole point of the sync handle: no rewrite of what came before it.
        Assert.AreEqual("opfs:sync:True/9/9/18/record-2;", status);
    }

    [TestMethod]
    public async Task Opfs_SyncAccess_Serializes_Aliased_Spellings_Of_One_Path()
    {
        await ClickAndExpectAsync("opfs-sync-aliases", "opfs:aliases:");

        var status = await CurrentStatusAsync();

        if (status == "opfs:aliases:unsupported") return;

        // Three overlapping 3-byte appends to "a/log.bin", "./a/log.bin" and "a//log.bin" - one
        // file under three spellings. No failures, and all nine bytes land: a queue keyed on the
        // raw path would hand out three queues, and the handles that lost the race would come back
        // -1 with a short file behind them.
        Assert.AreEqual("opfs:aliases:0/9", status);
    }

    [TestMethod]
    public async Task StorageBuckets_Open_Write_List_And_Delete_Roundtrips()
    {
        await ClickAndExpectAsync("buckets-roundtrip", "buckets:roundtrip:");

        var status = await CurrentStatusAsync();

        // Storage Buckets is Chromium-only and recent even there, so the harness reports its absence
        // rather than failing - a browser without it is a fact about the browser, not a defect.
        if (status == "buckets:roundtrip:unsupported") return;

        // The two True/True pairs after the text are WriteBytes/ReadBytes: the fourth field is the
        // write, the fifth is the bytes coming back byte-for-byte - which is what ReadText cannot
        // do, since the payload is deliberately not valid UTF-8.
        Assert.AreEqual("buckets:roundtrip:butil-e2e/True/unsent/True/True/True/True", status);
    }
}
