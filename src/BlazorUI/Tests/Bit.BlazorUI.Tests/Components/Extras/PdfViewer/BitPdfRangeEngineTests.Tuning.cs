using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeAccess = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeAccess;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

// The deferred Range optimizations of #14694: batched misses, the cursor's
// block cache, the font-store overlay, hint-validated page jumps and the root
// /Count check.
public partial class BitPdfRangeEngineTests
{
    private const BindingFlags TuningFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type MissingBatch = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfMissingDataBatch", true)!;

    [TestMethod]
    public async Task OutlineDestinationsShouldAcquireEveryMissingRangeOfAPassAtOnce()
    {
        byte[] bytes = SpacedOutline(pages: 12);
        var expected = BitPdfDocument.Load(bytes);

        using var legacyHandler = new RangeHandler(bytes);
        using var legacyHttp = new HttpClient(legacyHandler);
        await using var legacySource = await RangeAccess.Open(legacyHttp, 16 * 1024 * 1024, 4096);
        var legacy = await legacySource.Load();
        int legacyPasses = 0;
        AssertOutline(expected.Outline, await legacySource.Run(legacy, d => { legacyPasses++; return d.Outline; }));

        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        var (outline, passes) = await RunBatched(source, document, d => d.Outline);

        AssertOutline(expected.Outline, outline);
        Assert.AreEqual(12, outline.Count);
        // One pass finds the outline objects, the next every destination page at once.
        Assert.IsTrue(passes <= 3, $"passes: {passes}");
        Assert.IsTrue(legacyPasses >= 12, $"legacy passes: {legacyPasses}");
    }

    [TestMethod]
    public async Task DeferredMissesMustNeverPublishAPartialOutline()
    {
        byte[] bytes = SpacedOutline(pages: 6);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        var outlineField = typeof(BitPdfDocument).GetField("_outline", TuningFlags)!;
        using (var batch = BeginBatch(document.XRef, 1024L * 1024, 4096))
        {
            Assert.ThrowsExactly<BitPdfMissingDataException>(() => { _ = document.Outline; });
            Assert.IsTrue((int)MissingBatch.GetProperty("Count", TuningFlags)!.GetValue(batch)! > 0);
        }

        Assert.IsNull(outlineField.GetValue(document));
    }

    [TestMethod]
    public void MissingBatchShouldStopDeferringAtItsByteBudget()
    {
        var xref = BitPdfDocument.Load(WrongRootCount(scattered: false)).XRef;
        using (var batch = BeginBatch(xref, 8192L, 4096))
        {
            Assert.IsTrue(Defer(xref, 0, 10), "the first miss is always taken");
            Assert.IsTrue(Defer(xref, 100, 10), "a range inside an accepted chunk costs nothing");
            Assert.IsTrue(Defer(xref, 5000, 10));
            Assert.IsFalse(Defer(xref, 9000, 10), "a third chunk exceeds the 8 KiB budget");
            CollectionAssert.AreEqual(new[] { (0, 10), (100, 10), (5000, 10) }, Collect(batch, null).ToArray());
        }

        Assert.IsFalse(Defer(xref, 0, 10), "nothing is deferred without an active batch");
    }

    [TestMethod]
    public void MissingBatchShouldNeverHandOnAPassMissThatExceedsItsBudget()
    {
        var xref = BitPdfDocument.Load(WrongRootCount(scattered: false)).XRef;
        using (var empty = BeginBatch(xref, 8192L, 4096))
        {
            CollectionAssert.AreEqual(new[] { (0, 100_000) }, Collect(empty, Missing(0, 100_000)).ToArray(),
                "a pass that deferred nothing hands on its miss, whatever its size");
        }

        using var batch = BeginBatch(xref, 8192L, 4096);
        Assert.IsTrue(Defer(xref, 0, 10));
        CollectionAssert.AreEqual(new[] { (0, 10), (5000, 10) }, Collect(batch, Missing(5000, 10)).ToArray(), "a miss that fits joins the batch");
        Assert.IsTrue(Defer(xref, 5000, 10));
        // The batch is full: the refused miss is met again by the next pass,
        // once the deferred ranges are cached, rather than overrun the budget.
        CollectionAssert.AreEqual(new[] { (0, 10), (5000, 10) }, Collect(batch, Missing(9000, 10)).ToArray());
        CollectionAssert.AreEqual(new[] { (0, 10), (5000, 10) }, Collect(batch, Missing(0, 10)).ToArray(), "a known range is never repeated");
    }

    [TestMethod]
    public void MissingBatchShouldDeferOnlyForItsOwnXRefOnItsOwnThread()
    {
        var xref = BitPdfDocument.Load(WrongRootCount(scattered: false)).XRef;
        var other = BitPdfDocument.Load(WrongRootCount(scattered: false)).XRef;
        using var batch = BeginBatch(xref, 1024L * 1024, 4096);
        Assert.IsFalse(Defer(other, 0, 10), "another document's reads are never deferred into this pass");
        // A dedicated thread, joined: a Task.Run awaited by blocking may be inlined onto this very thread when the pool
        // is busy, which would make it the thread that began the batch.
        bool deferredElsewhere = true;
        var elsewhere = new System.Threading.Thread(() => deferredElsewhere = Defer(xref, 0, 10));
        elsewhere.Start();
        elsewhere.Join();
        Assert.IsFalse(deferredElsewhere, "nor are reads of another thread");
        Assert.IsFalse(Defer(null, 0, 10));
        Assert.AreEqual(0, (int)MissingBatch.GetProperty("Count", TuningFlags)!.GetValue(batch)!);
        Assert.IsTrue(Defer(xref, 0, 10));
    }

    [TestMethod]
    public async Task CursorShouldReadAPinnedBlockWithoutTheCacheLockUntilItsPinEnds()
    {
        using var handler = new RangeHandler(16384);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16384, 4096);
        await source.Ensure(4096, 4096);

        var operation = source.Operation();
        var cursor = source.Cursor(4096, 4096);
        long before = source.Long("BlockLookups");
        for (int i = 0; i < 4000; i++) Assert.AreEqual(handler.Bytes[4096 + i], cursor.GetByte());
        CollectionAssert.AreEqual(handler.Bytes[8096..8192], cursor.GetBytes(96));
        Assert.AreEqual(before + 1, source.Long("BlockLookups"), "one locked lookup for the whole block");

        // A nested operation must pin the block for itself before trusting it.
        using (source.Operation())
        {
            cursor.Pos = 4096;
            for (int i = 0; i < 100; i++) cursor.GetByte();
            Assert.AreEqual(before + 2, source.Long("BlockLookups"));
        }

        // Once the owning operation ends nothing pins the block: every read looks it up.
        operation.Dispose();
        cursor.Pos = 4096;
        for (int i = 0; i < 10; i++) Assert.AreEqual(handler.Bytes[4096 + i], cursor.GetByte());
        Assert.AreEqual(before + 12, source.Long("BlockLookups"));

        using (source.Operation())
        {
            cursor.Pos = 4096;
            cursor.GetByte();
            await source.DisposeAsync();
            Assert.ThrowsExactly<ObjectDisposedException>(() => { cursor.GetByte(); }, "a retired source must not serve a cached block");
        }
    }

    [TestMethod]
    public async Task CursorShouldDropItsCachedBlockWhenItsOperationReleasesItsPins()
    {
        using var handler = new RangeHandler(16384);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16384, 4096);
        await source.Ensure(4096, 4096);
        var cursor = source.Cursor(4096, 4096);
        var block = cursor.GetType().GetField("_block", TuningFlags)!;
        var owner = cursor.GetType().GetField("_blockOwner", TuningFlags)!;

        using (source.Operation())
        {
            cursor.GetByte();
            Assert.IsNotNull(block.GetValue(cursor));
        }

        // A cursor a parsed stream keeps for the document's lifetime must not keep
        // a block the cache may evict, nor the operation that pinned it.
        Assert.IsNull(block.GetValue(cursor));
        Assert.IsNull(owner.GetValue(cursor));

        // A pin release that leaves the operation current drops the reference too.
        using (var operation = source.Operation())
        {
            cursor.Pos = 4096;
            cursor.GetByte();
            Assert.IsNotNull(block.GetValue(cursor));
            source.ReleasePins(operation);
            Assert.IsNull(block.GetValue(cursor));
        }
    }

    [TestMethod]
    public async Task BlocksACursorKeptReadingShouldBeTheMostRecentlyUsedOnceItsOperationEnds()
    {
        using var handler = new RangeHandler(16384);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16384, 4096);
        await source.Ensure(0, 8192);
        var hot = source.Cursor(4096, 4096);
        var cold = source.Cursor(0, 4096);

        using (source.Operation())
        {
            hot.GetByte();
            cold.GetBytes(16); // a locked read: block 0 becomes the most recent
            for (int i = 0; i < 1000; i++) hot.GetByte(); // served by the cursor alone
        }

        var lru = source.Field("_lru")!;
        var first = lru.GetType().GetProperty("First")!.GetValue(lru)!;
        var value = first.GetType().GetProperty("Value")!.GetValue(first)!;
        Assert.AreEqual(4096, (int)value.GetType().GetField("Start", TuningFlags)!.GetValue(value)!,
            "the block read most must not look cold to eviction");
    }

    [TestMethod]
    public void FontStoreOverlayShouldReadItsBaseWithoutCopyingAndCommitOnlyWhenComplete()
    {
        var shared = new BitPdfFontStore();
        var font = (BitPdfFont)RuntimeHelpers.GetUninitializedObject(typeof(BitPdfFont));
        var other = (BitPdfFont)RuntimeHelpers.GetUninitializedObject(typeof(BitPdfFont));
        Invoke(shared, "AddFont", "known", font);
        Assert.IsTrue((bool)Invoke(shared, "TryAddFamily", "Known")!);
        Invoke(shared, "AppendFace", "Known", "@font-face{known}");

        var abandoned = Invoke(shared, "BeginOverlay")!;
        Assert.AreEqual(0, Fonts(abandoned).Count, "an overlay starts empty instead of copying the store");
        object?[] lookup = ["known", null];
        Assert.IsTrue((bool)typeof(BitPdfFontStore).GetMethod("TryGetFont", TuningFlags)!.Invoke(abandoned, lookup)!);
        Assert.AreSame(font, lookup[1]);
        Assert.IsFalse((bool)Invoke(abandoned, "TryAddFamily", "Known")!, "a family of the base is never emitted twice");
        Invoke(abandoned, "AddFont", "new", other);
        Assert.IsTrue((bool)Invoke(abandoned, "TryAddFamily", "New")!);
        Invoke(abandoned, "AppendFace", "New", "@font-face{new}");
        Assert.AreEqual(1, Fonts(shared).Count, "registrations stay in the attempt until it commits");

        // A missing byte abandoned that attempt: the next one must emit the face again.
        var completed = (BitPdfFontStore)Invoke(shared, "BeginOverlay")!;
        Assert.IsTrue((bool)Invoke(completed, "TryAddFamily", "New")!);
        Invoke(completed, "AddFont", "new", other);
        Invoke(completed, "AppendFace", "New", "@font-face{new}");

        // Another render committed the same family first: its face is emitted once.
        var raced = (BitPdfFontStore)Invoke(shared, "BeginOverlay")!;
        Assert.IsTrue((bool)Invoke(raced, "TryAddFamily", "New")!);
        Invoke(raced, "AppendFace", "New", "@font-face{new}");
        Invoke(completed, "Commit");
        Invoke(raced, "Commit");

        Assert.AreEqual(2, Fonts(shared).Count);
        Assert.AreEqual("<style>@font-face{known}@font-face{new}</style>", shared.FontFaceStyle);
        Assert.IsFalse((bool)Invoke(Invoke(shared, "BeginOverlay")!, "TryAddFamily", "New")!);
        Assert.ThrowsExactly<TargetInvocationException>(() => { Invoke(completed, "BeginOverlay"); });

        static object? Invoke(object target, string name, params object?[] arguments)
            => typeof(BitPdfFontStore).GetMethod(name, TuningFlags)!.Invoke(target, arguments);
        static Dictionary<object, BitPdfFont> Fonts(object store)
            => (Dictionary<object, BitPdfFont>)typeof(BitPdfFontStore).GetProperty("Fonts", TuningFlags)!.GetValue(store)!;
    }

    [TestMethod]
    public async Task ValidatedHintRunShouldJumpToADistantPageWithoutReadingItsSiblings()
    {
        byte[] bytes = LinearizedFlatPages(40);
        var expected = BitPdfDocument.Load(bytes);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        Assert.IsTrue((await source.Run(document, d => Hints(d, 35))).Count > 0, "the fixture's hints must validate");
        int requests = handler.Requests.Count;

        Assert.AreEqual(expected.Pages[35].Width, await source.Run(document, d => d.Pages[35].Width));
        Assert.AreEqual(135d, expected.Pages[35].Width);
        Assert.IsTrue(handler.Requests.Count - requests <= 2, string.Join(",", handler.Requests.Skip(requests)));

        // A destination's page index is derived from the same validated run.
        requests = handler.Requests.Count;
        int target = PageObject(20);
        Assert.AreEqual(21, await source.Run(document, d => d.ResolveDestination(new List<object?> { new BitPdfRef(target, 0), BitPdfName.Get("Fit") })?.PageNumber));
        Assert.IsTrue(handler.Requests.Count - requests <= 2, string.Join(",", handler.Requests.Skip(requests)));

        for (int i = 0; i < 40; i += 7)
        {
            int page = i;
            Assert.AreEqual(expected.Pages[i].Width, await source.Run(document, d => d.Pages[page].Width));
        }

        await source.Run(document, d => { Verify(d); return true; });
    }

    [TestMethod]
    public async Task HintsThatDoNotMatchTheKidsMustFallBackToTheOrdinaryWalk()
    {
        byte[] bytes = LinearizedFlatPages(40, swapKids: true);
        var expected = BitPdfDocument.Load(bytes);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        Assert.IsTrue((await source.Run(document, d => Hints(d, 35))).Count > 0);
        int requests = handler.Requests.Count;

        Assert.AreEqual(expected.Pages[35].Width, await source.Run(document, d => d.Pages[35].Width));
        Assert.AreEqual(expected.Pages[1].Width, await source.Run(document, d => d.Pages[1].Width));
        Assert.AreEqual(102d, expected.Pages[1].Width, "the swapped kids put the third page second");
        Assert.IsTrue(handler.Requests.Count - requests > 10, "without a validated run every sibling is read");
    }

    [TestMethod]
    public async Task ALinearizationPageCountTheTreeContradictsShouldOnlyDisableTheHints()
    {
        byte[] bytes = LinearizedFlatPages(40, linearizedCount: 41);
        var expected = BitPdfDocument.Load(bytes);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        Assert.AreEqual(40, document.PageCount);
        Assert.AreEqual(0, (await source.Run(document, d => Hints(d, 35))).Count, "hints for another page count are ignored");

        // The tree is right: the Range document stays, walked as an unlinearized one.
        await source.Run(document, d => { Verify(d); return true; });
        Assert.AreEqual(expected.Pages[35].Width, await source.Run(document, d => d.Pages[35].Width));
        Assert.AreEqual(21, await source.Run(document, d => d.ResolveDestination(new List<object?> { new BitPdfRef(PageObject(20), 0), BitPdfName.Get("Fit") })?.PageNumber));
    }

    [TestMethod]
    public async Task AHintRunHoldingAPagesNodeMustNeverPlaceItsPagesByTheHints()
    {
        // The hints name page 2's object, which is really a /Pages node over one page.
        byte[] bytes = LinearizedFlatPages(40, nestedAt: 2);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        Assert.IsTrue((await source.Run(document, d => Hints(d, 35))).Count > 0, "the fixture's hints must validate");

        // A page of the run is placed where the walk puts it, and the walk agrees.
        Assert.AreEqual(21, await source.Run(document, d => d.ResolveDestination(new List<object?> { new BitPdfRef(PageObject(20), 0), BitPdfName.Get("Fit") })?.PageNumber));
        Assert.AreEqual(120d, await source.Run(document, d => d.Pages[20].Width));

        // The page under the misdescribed kid is never placed by the run.
        var error = await Assert.ThrowsAsync<Exception>(() => source.Run(document,
            d => d.ResolveDestination(new List<object?> { new BitPdfRef(PageObject(2) + 1, 0), BitPdfName.Get("Fit") })?.PageNumber));
        Assert.AreEqual("BitPdfRangeFallbackException", error.GetType().Name);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RootCountCheckShouldRejectAContradictedCountOnlyFromCachedBytes(bool scattered)
    {
        byte[] bytes = WrongRootCount(scattered);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        Assert.AreEqual(4, document.PageCount);
        Assert.AreEqual(300d, await source.Run(document, d => d.Pages[0].Width));
        int requests = handler.Requests.Count;

        if (scattered)
        {
            // The check never requests bytes: a root it cannot read cheaply is not summed.
            await source.Run(document, d => { Verify(d); return true; });
        }
        else
        {
            var error = await Assert.ThrowsAsync<Exception>(() => source.Run(document, d => { Verify(d); return true; }));
            Assert.AreEqual("BitPdfRangeFallbackException", error.GetType().Name);
        }

        Assert.AreEqual(requests, handler.Requests.Count);
    }

    private static List<(int, int)> Hints(BitPdfDocument document, int page)
        => (List<(int, int)>)typeof(BitPdfXRef).GetMethod("HintRanges", TuningFlags)!
            .Invoke(document.XRef, BindingFlags.DoNotWrapExceptions, null, [page, document.PageCount, 262144], null)!;

    private static void Verify(BitPdfDocument document)
    {
        try { typeof(BitPdfDocument).GetMethod("VerifyPartialPageCount", TuningFlags)!.Invoke(document, null); }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
    }

    private static IDisposable BeginBatch(BitPdfXRef xref, long maxBytes, int chunkSize)
        => (IDisposable)MissingBatch.GetMethod("Begin", TuningFlags)!.Invoke(null, [xref, maxBytes, chunkSize])!;

    private static BitPdfMissingDataException Missing(int offset, int length)
        => (BitPdfMissingDataException)typeof(BitPdfMissingDataException).GetConstructor(TuningFlags, [typeof(int), typeof(int)])!.Invoke([offset, length]);

    private static bool Defer(BitPdfXRef? xref, int offset, int length)
        => (bool)MissingBatch.GetMethod("Defer", TuningFlags)!.Invoke(null, [xref, Missing(offset, length)])!;

    private static List<(int Offset, int Length)> Collect(IDisposable batch, BitPdfMissingDataException? missing)
        => (List<(int Offset, int Length)>)MissingBatch.GetMethod("Collect", TuningFlags)!.Invoke(batch, [missing])!;

    private static async Task<(T Value, int Passes)> RunBatched<T>(RangeAccess source, BitPdfDocument document, Func<BitPdfDocument, T> build)
    {
        using var operation = source.Operation();
        for (int pass = 1; pass <= 64; pass++)
        {
            List<(int Offset, int Length)> ranges;
            using (var frame = (IDisposable)typeof(BitPdfDocument).GetMethod("BeginPartialAttempt", TuningFlags)!.Invoke(document, null)!)
            using (var batch = BeginBatch(document.XRef, 1024L * 1024, 4096))
            {
                try
                {
                    var result = build(document);
                    if ((int)MissingBatch.GetProperty("Count", TuningFlags)!.GetValue(batch)! == 0)
                    {
                        frame.GetType().GetMethod("Commit", TuningFlags)!.Invoke(frame, null);
                        return (result, pass);
                    }

                    ranges = (List<(int, int)>)MissingBatch.GetMethod("Collect", TuningFlags)!.Invoke(batch, [null])!;
                }
                catch (BitPdfMissingDataException missing)
                {
                    ranges = (List<(int, int)>)MissingBatch.GetMethod("Collect", TuningFlags)!.Invoke(batch, [missing])!;
                }
                finally { typeof(BitPdfDocument).GetMethod("ReleaseByteWindows", TuningFlags)!.Invoke(document, null); }

                frame.GetType().GetMethod("CommitObjects", TuningFlags)!.Invoke(frame, null);
            }

            await source.EnsureRanges([.. ranges]);
        }

        throw new InvalidOperationException("The batched PDF operation did not make progress.");
    }

    // A flat page tree whose page dictionaries sit in separate 4 KiB chunks, and
    // a compact outline with one bookmark per page, in page order.
    private static byte[] SpacedOutline(int pages)
    {
        var file = new BitPdfEngineTestFile();
        int outlines = 3, firstItem = 4, firstPage = firstItem + pages;
        file.Obj(1, $"<< /Type /Catalog /Pages 2 0 R /Outlines {outlines} 0 R >>");
        file.Obj(2, $"<< /Type /Pages /Count {pages} /Kids [{string.Join(" ", Enumerable.Range(firstPage, pages).Select(n => $"{n} 0 R"))}] >>");
        file.Obj(outlines, $"<< /Type /Outlines /First {firstItem} 0 R /Last {firstItem + pages - 1} 0 R /Count {pages} >>");
        for (int i = 0; i < pages; i++)
        {
            string next = i + 1 < pages ? $" /Next {firstItem + i + 1} 0 R" : "";
            file.Obj(firstItem + i, $"<< /Title (Bookmark {i + 1}) /Parent {outlines} 0 R /Dest [{firstPage + i} 0 R /Fit]{next} >>");
        }

        for (int i = 0; i < pages; i++)
        {
            file.Raw(new string(' ', 5000));
            file.Obj(firstPage + i, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {200 + i} 792] >>");
        }

        file.XRefTable($"/Size {firstPage + pages} /Root 1 0 R", file.Offsets.Keys.Append(0));
        return file.ToArray();
    }

    // Root Count 4 over three pages. The kids sit in the first chunk, or far apart
    // (beyond the 64 KiB blocks the loader's own reads acquire).
    internal static byte[] WrongRootCount(bool scattered)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Count 4 /Kids [3 0 R 4 0 R 5 0 R] >>");
        for (int i = 0; i < 3; i++)
        {
            if (scattered && i > 0) file.Raw(new string(' ', 200_000));
            file.Obj(3 + i, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {300 + i} 792] >>");
        }

        if (!scattered) file.Raw(new string(' ', 9000));
        file.XRefTable("/Size 6 /Root 1 0 R", file.Offsets.Keys.Append(0));
        return file.ToArray();
    }

    // The page object of page `index` in LinearizedFlatPages (pages after the first
    // are numbered from 1, a page dictionary and its content per page).
    private static int PageObject(int index) => 1 + (index - 1) * 2;

    // ISO 32000-1 Annex F: a linearized file with a flat page tree, each later page
    // an equally long section (page + content) far enough apart to need its own
    // chunk, and a hint stream whose page offset table describes them exactly.
    // swapKids exchanges the second and third root kids, so the hints no longer
    // describe the tree's order.
    // linearizedCount writes another /N than the tree's Count. nestedAt turns that
    // page's hinted object into a /Pages node over one page (its content object
    // number), which the hints cannot tell from a page.
    private static byte[] LinearizedFlatPages(int pages, bool swapKids = false, int? linearizedCount = null, int? nestedAt = null)
    {
        const int section = 9000;
        int m = 2 * (pages - 1);
        int lin = m + 1, catalog = m + 2, root = m + 3, hint = m + 4, first = m + 5, firstContent = m + 6, size = m + 7;
        var kids = new List<int> { first };
        for (int i = 1; i < pages; i++) kids.Add(PageObject(i));
        if (swapKids) (kids[1], kids[2]) = (kids[2], kids[1]);

        int length = 0, hintOffset = 0, hintLength = 0, end = 0, mainXref = 0, firstXref = 0, firstPageOffset = 0;
        byte[] Build()
        {
            var text = new StringBuilder("%PDF-1.4\n% linearized\n");
            var offsets = new Dictionary<int, int>();
            void Obj(int number, string body) { offsets[number] = text.Length; text.Append($"{number} 0 obj\n{body}\nendobj\n"); }
            Obj(lin, $"<< /Linearized 1 /L {length:D10} /H [{hintOffset:D10} {hintLength:D10}] /O {first} /E {end:D10} /N {linearizedCount ?? pages} /T {mainXref:D10} >>");
            firstXref = text.Length;
            text.Append($"xref\n{lin} 6\n");
            for (int n = lin; n < size; n++) text.Append($"{(n == lin ? offsets[lin] : 0):D10} 00000 n \n");
            int entries = firstXref + $"xref\n{lin} 6\n".Length;
            text.Append($"trailer\n<< /Root {catalog} 0 R /Size {size} /Prev {mainXref:D10} >>\nstartxref\n0\n%%EOF\n");
            Obj(catalog, $"<< /Type /Catalog /Pages {root} 0 R >>");
            Obj(root, $"<< /Type /Pages /Count {pages} /Kids [{string.Join(" ", kids.Select(k => $"{k} 0 R"))}] >>");
            byte[] table = HintTable(firstPageOffset - hintLength, section);
            offsets[hint] = text.Length;
            text.Append($"{hint} 0 obj\n<< /Length {table.Length} /S 36 >>\nstream\n").Append(Encoding.Latin1.GetString(table)).Append("\nendstream\nendobj\n");
            hintOffset = offsets[hint];
            hintLength = text.Length - hintOffset;
            firstPageOffset = text.Length;
            Obj(first, $"<< /Type /Page /Parent {root} 0 R /MediaBox [0 0 100 792] /Contents {firstContent} 0 R >>");
            Obj(firstContent, "<< /Length 0 >>\nstream\n\nendstream");
            end = text.Length;
            for (int i = 1; i < pages; i++)
            {
                int start = text.Length;
                if (i == nestedAt)
                {
                    Obj(PageObject(i), $"<< /Type /Pages /Parent {root} 0 R /Count 1 /Kids [{PageObject(i) + 1} 0 R] >>");
                    Obj(PageObject(i) + 1, $"<< /Type /Page /Parent {PageObject(i)} 0 R /MediaBox [0 0 {100 + i} 792] >>");
                }
                else
                {
                    Obj(PageObject(i), $"<< /Type /Page /Parent {root} 0 R /MediaBox [0 0 {100 + i} 792] /Contents {PageObject(i) + 1} 0 R >>");
                    Obj(PageObject(i) + 1, "<< /Length 0 >>\nstream\n\nendstream");
                }

                text.Append(' ', section - (text.Length - start));
            }

            mainXref = text.Length;
            text.Append($"xref\n0 {lin}\n0000000000 65535 f \n");
            for (int n = 1; n < lin; n++) text.Append($"{offsets[n]:D10} 00000 n \n");
            text.Append($"trailer\n<< /Size {size} >>\nstartxref\n{firstXref}\n%%EOF\n");
            length = text.Length;

            // The first-page xref section was written before its objects' offsets were known.
            var rows = new StringBuilder();
            for (int n = lin; n < size; n++) rows.Append($"{offsets[n]:D10} 00000 n \n");
            text.Remove(entries, rows.Length).Insert(entries, rows.ToString());
            return Encoding.Latin1.GetBytes(text.ToString());
        }

        Build();
        Build();
        return Build();
    }

    private static byte[] HintTable(int firstPageHintOffset, int section)
    {
        var output = new List<byte>();
        void Write(uint value, int bytes) { for (int i = bytes - 1; i >= 0; i--) output.Add((byte)(value >> (i * 8))); }
        // Page offset hint table header: every per-page item has a bit width of 0.
        Write(2, 4); Write((uint)firstPageHintOffset, 4); Write(0, 2);
        Write((uint)section, 4); Write(0, 2);
        Write(0, 4); Write(0, 2); Write(0, 4); Write(0, 2);
        Write(0, 2); Write(0, 2); Write(0, 2); Write(0, 2);
        // Shared object hint table header with no shared groups.
        Write(0, 4); Write(0, 4); Write(0, 4); Write(0, 4); Write(0, 2); Write(0, 4); Write(0, 2);
        return [.. output];
    }
}
