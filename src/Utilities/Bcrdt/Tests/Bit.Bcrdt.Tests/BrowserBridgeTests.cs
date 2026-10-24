using Microsoft.JSInterop;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// The .NET halves of the browser bridges: the IndexedDB store, against a stand-in for the module that
/// keeps its records the way the real one does, and the text binding's change-based input.
/// </summary>
/// <remarks>
/// The JavaScript itself runs in the end-to-end suite; these pin down what .NET makes of what it says.
/// </remarks>
[TestClass]
public class BrowserBridgeTests
{
    // ---------------------------------------------------------------------------------------------
    // IndexedDB store
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_document_stored_in_the_browser_reloads_whole()
    {
        var browser = new FakeIndexedDb();
        await using var store = new BcrdtIndexedDbDocumentStore(browser);

        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "notes");
            doc.GetText("body").Insert(0, "kept ");
            doc.GetText("body").Insert(5, "offline");
            await persistence.FlushAsync();
        }

        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using BcrdtDocumentPersistence again = await BcrdtDocumentPersistence.AttachAsync(reloaded, store, "notes");

        Assert.AreEqual("kept offline", reloaded.GetText("body").ToString());
        CollectionAssert.AreEquivalent(new[] { "notes" }, (await store.ListAsync()).ToArray());
    }

    [TestMethod]
    public async Task Compaction_replaces_exactly_the_records_it_read()
    {
        var browser = new FakeIndexedDb();
        await using var store = new BcrdtIndexedDbDocumentStore(browser);

        await store.AppendUpdateAsync("doc", Update(1, "a"));
        await store.AppendUpdateAsync("doc", Update(2, "b"));

        // Another tab appends while this one is merging: its record must survive the replacement.
        browser.BeforeReplace = () => browser.Append("doc", Update(3, "c"));
        await store.CompactAsync("doc");

        Assert.AreEqual(2, browser.Count("doc"), "the merged record and the one appended meanwhile");
        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate((await store.LoadAsync("doc")).Update!);
        Assert.AreEqual(3, reloaded.GetText("body").ToString().Length);
    }

    [TestMethod]
    public async Task An_unreadable_record_is_skipped_and_kept_rather_than_locking_the_document_away()
    {
        var browser = new FakeIndexedDb();
        await using var store = new BcrdtIndexedDbDocumentStore(browser);
        var skipped = new List<string>();
        store.UnreadableRecordSkipped += skipped.Add;

        await store.AppendUpdateAsync("doc", Update(1, "fine"));
        browser.Append("doc", [0xFF, 0xFF, 0xFF]);

        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate((await store.LoadAsync("doc")).Update!);
        Assert.AreEqual("fine", reloaded.GetText("body").ToString());
        Assert.HasCount(1, skipped);

        await store.CompactAsync("doc");
        Assert.AreEqual(2, browser.Count("doc"), "compaction does not destroy a record it cannot read");
    }

    [TestMethod]
    public async Task A_full_quota_is_reported_as_such_and_the_write_is_retried()
    {
        var browser = new FakeIndexedDb { RefuseAppends = true };
        await using var store = new BcrdtIndexedDbDocumentStore(browser);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
            doc, store, "doc", new BcrdtDocumentPersistenceOptions { RetryDelay = TimeSpan.FromMilliseconds(10) });
        var failures = new List<Exception>();
        persistence.WriteFailed += failure => { lock (failures) failures.Add(failure); };

        doc.GetText("body").Insert(0, "waiting for room");
        Task flushed = persistence.FlushAsync();
        await Task.Delay(100);
        Assert.IsFalse(flushed.IsCompleted, "the write is still being retried, not given up");

        browser.RefuseAppends = false;
        await flushed.WaitAsync(TimeSpan.FromSeconds(10));

        lock (failures)
        {
            Assert.IsNotEmpty(failures);
            Assert.IsInstanceOfType<IOException>(failures[0]);
            Assert.Contains("quota", failures[0].Message);
        }
        Assert.AreEqual(1, browser.Count("doc"));
    }

    [TestMethod]
    public void Framed_records_are_read_back_exactly_and_truncation_is_refused()
    {
        byte[] framed = [1, .. Frame(1, [1, 2, 3]), .. Frame(7, [])];
        List<(double Id, byte[] Data)> records = BcrdtIndexedDbDocumentStore.Unframe(framed);

        Assert.HasCount(2, records);
        Assert.AreEqual(1d, records[0].Id);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, records[0].Data);
        Assert.AreEqual(7d, records[1].Id);
        Assert.IsEmpty(records[1].Data);

        Assert.ThrowsExactly<InvalidDataException>(() => BcrdtIndexedDbDocumentStore.Unframe(framed.AsSpan(0, framed.Length - 13)));

        // A document with no records is the format byte alone - never empty, which Blazor would refuse to stream -
        // and a format this build does not know is refused rather than misread.
        Assert.IsEmpty(BcrdtIndexedDbDocumentStore.Unframe([1]));
        Assert.ThrowsExactly<InvalidDataException>(() => BcrdtIndexedDbDocumentStore.Unframe([2, .. framed[1..]]));
    }

    // ---------------------------------------------------------------------------------------------
    // Change-based input
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void An_edit_reported_as_a_change_lands_as_the_same_edit()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");
        using var binding = new BcrdtTextInputBinding(text);

        Assert.IsTrue(binding.TryApplyInputChange(5, 6, ", there", 12, 12));
        Assert.AreEqual("hello, there", text.ToString());
        Assert.AreEqual("hello, there", binding.Value);

        // A change that cannot apply to what the control is believed to hold means the two drifted.
        Assert.IsFalse(binding.TryApplyInputChange(20, 1, "x", 0, 0));
        Assert.IsFalse(binding.TryApplyInputChange(3, 50, "x", 0, 0));
        Assert.AreEqual("hello, there", text.ToString());
    }

    [TestMethod]
    public void Changes_reported_while_the_document_settles_build_on_one_another()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "ab");
        using var binding = new BcrdtTextInputBinding(text);

        // Inside a transaction the input waits for the document to settle; a second change must be
        // read against the first, not against what the document showed before either.
        doc.Transact(_ =>
        {
            Assert.IsTrue(binding.TryApplyInputChange(1, 0, "X", 2, 2));
            Assert.IsTrue(binding.TryApplyInputChange(2, 0, "Y", 3, 3));
        });

        Assert.AreEqual("aXYb", text.ToString());
    }

    [TestMethod]
    [DataRow("😃😀", "😀", 0, "😃", "")]
    [DataRow("😀", "😃😀", 0, "", "😃")]
    [DataRow("a😀b", "a😃b", 1, "😀", "😃")]
    [DataRow("hello", "hel!lo", 3, "", "!")]
    [DataRow("same", "same", 4, "", "")]
    public void A_change_sent_to_the_browser_never_cuts_a_surrogate_pair(string before, string after, int index, string removed, string inserted)
    {
        // The change crosses into JavaScript as JSON, which would turn half a pair into U+FFFD.
        (int actualIndex, string actualRemoved, string actualInserted) = Bit.Bcrdt.Blazor.BcrdtTextArea.Change(before, after);

        Assert.AreEqual(index, actualIndex);
        Assert.AreEqual(removed, actualRemoved);
        Assert.AreEqual(inserted, actualInserted);
        Assert.AreEqual(after, before[..actualIndex] + actualInserted + before[(actualIndex + actualRemoved.Length)..]);
    }

    [TestMethod]
    public async Task A_bounded_in_memory_store_never_drops_a_document_still_in_use()
    {
        var store = new BcrdtInMemoryDocumentStore(maxDocuments: 1);
        await store.AppendUpdateAsync("first", Update(1, "a"));
        await store.AppendUpdateAsync("second", Update(2, "b"));

        // Both were used a moment ago - one might still be open with its writer appending - so the store
        // holds more than its bound rather than cut a live document's log.
        Assert.AreEqual(2, store.Count);
        Assert.IsFalse((await store.LoadAsync("first")).IsEmpty);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static byte[] Update(ulong clientId, string text)
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
        doc.GetText("body").Insert(0, text);
        return doc.EncodeStateAsUpdate();
    }

    private static byte[] Frame(double id, byte[] data)
    {
        byte[] frame = new byte[12 + data.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(frame, id);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(8), (uint)data.Length);
        data.CopyTo(frame, 12);
        return frame;
    }

    /// <summary>
    /// Stands in for the browser and the module's IndexedDB functions: records under auto-incremented
    /// keys, indexed by document, read back framed the way the module frames them.
    /// </summary>
    private sealed class FakeIndexedDb : IJSRuntime, IJSObjectReference
    {
        private readonly SortedDictionary<double, (string Document, byte[] Data)> _records = [];
        private double _nextKey = 1;

        public bool RefuseAppends { get; set; }

        public Action? BeforeReplace { get; set; }

        public void Append(string document, byte[] data)
        {
            lock (_records) _records[_nextKey++] = (document, data);
        }

        public int Count(string document)
        {
            lock (_records) return _records.Values.Count(record => record.Document == document);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            args ??= [];
            object? result = identifier switch
            {
                "import" => this,
                "idbAppend" => AppendFromModule((string)args[1]!, (byte[])args[2]!),
                "idbLoad" => Load((string)args[1]!),
                "idbReplace" => Replace((string)args[1]!, (double[])args[2]!, (byte[])args[3]!),
                "idbDelete" => Delete((string)args[1]!),
                "idbList" => List(),
                _ => throw new InvalidOperationException($"Unexpected call to '{identifier}'."),
            };
            return ValueTask.FromResult((TValue)result!);
        }

        private object? AppendFromModule(string document, byte[] data)
        {
            if (RefuseAppends) throw new JSException("QuotaExceededError: The quota has been exceeded.");
            Append(document, data);
            return null;
        }

        // Like the module: an empty stream, never null, for a document with no records - Blazor wraps
        // whatever the function returns into the stream .NET asked for.
        private IJSStreamReference Load(string document)
        {
            byte[] framed;
            lock (_records)
            {
                List<byte> bytes = [];
                foreach ((double key, (string owner, byte[] data)) in _records)
                {
                    if (owner == document) bytes.AddRange(Frame(key, data));
                }
                framed = [1, .. bytes];
            }
            return new StreamReference(framed);
        }

        private object? Replace(string document, double[] keys, byte[] merged)
        {
            BeforeReplace?.Invoke();
            lock (_records)
            {
                foreach (double key in keys) _records.Remove(key);
                _records[_nextKey++] = (document, merged);
            }
            return null;
        }

        private object? Delete(string document)
        {
            lock (_records)
            {
                foreach (double key in _records.Where(record => record.Value.Document == document).Select(record => record.Key).ToArray())
                {
                    _records.Remove(key);
                }
            }
            return null;
        }

        private string[] List()
        {
            lock (_records) return [.. _records.Values.Select(record => record.Document).Distinct()];
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class StreamReference(byte[] data) : IJSStreamReference
        {
            public long Length => data.Length;

            public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
                ValueTask.FromResult<Stream>(new MemoryStream(data));

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
                throw new NotSupportedException();

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
