using Bit.Bcrdt.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task An_edit_after_one_whose_report_was_lost_reads_the_value_back_instead_of_landing_out_of_place()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");
        var browser = new FakeTextControl();
        await using TextAreaHost host = await TextAreaHost.RenderAsync(browser, text);

        // The first report never arrives - a dropped connection - so the second is a change to content
        // .NET never saw: applied as it stands, it would put "Y" one character to the left.
        browser.Type(0, 0, "X");
        await host.DeliverAsync(browser.Type(6, 0, "Y"));

        Assert.AreEqual("XhelloY world", text.ToString());
        Assert.AreEqual(1, browser.Count("readValue"));

        // Reported edits follow on from the value read back.
        await host.DeliverAsync(browser.Type(13, 0, "!"));
        Assert.AreEqual("XhelloY world!", text.ToString());
        Assert.AreEqual(1, browser.Count("readValue"));
        host.AssertNoErrors();
    }

    [TestMethod]
    public async Task An_edit_too_large_for_one_message_reaches_the_document_through_a_read()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "start ");
        var browser = new FakeTextControl();
        await using TextAreaHost host = await TextAreaHost.RenderAsync(browser, text);

        // A big paste: the module asks to be read rather than report it, and asks again with every
        // keystroke until it is. A request a read has since answered changes nothing.
        string pasted = string.Concat(Enumerable.Repeat("pasted text 😀 ", 4000));
        int inputs = browser.Paste(6, pasted);
        await host.DispatchAsync(textArea => textArea.OnControlEditTooLarge(browser.Generation, inputs));
        await host.DispatchAsync(textArea => textArea.OnControlEditTooLarge(browser.Generation, inputs));

        Assert.AreEqual("start " + pasted, text.ToString());
        Assert.AreEqual(1, browser.Count("readValue"));

        await host.DeliverAsync(browser.Type(0, 5, "end"));
        Assert.AreEqual("end " + pasted, text.ToString());
        host.AssertNoErrors();
    }

    [TestMethod]
    public async Task Edits_reported_while_the_value_is_read_back_are_applied_on_top_of_it()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");
        var browser = new FakeTextControl();
        await using TextAreaHost host = await TextAreaHost.RenderAsync(browser, text);

        // The value is read; before it arrives the user types on, and those reports overtake it.
        browser.ReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int inputs = browser.Paste(3, new string('x', 5000));
        Task reading = host.DispatchAsync(textArea => textArea.OnControlEditTooLarge(browser.Generation, inputs));
        await host.DeliverAsync(browser.Type(0, 0, "1"));
        await host.DeliverAsync(browser.Type(1, 0, "2"));
        Assert.AreEqual("abc", text.ToString(), "nothing is applied before the value it builds on");

        browser.ReadGate.SetResult();
        await reading.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual("12abc" + new string('x', 5000), text.ToString());
        host.AssertNoErrors();
    }

    [TestMethod]
    public async Task An_update_refused_for_an_edit_that_never_arrives_is_delivered_after_a_read()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");
        var browser = new FakeTextControl();
        await using TextAreaHost host = await TextAreaHost.RenderAsync(browser, text);

        // The user's last keystroke is lost, and a collaborator's change arrives: the module refuses it
        // for the edit it reported, which will never arrive. Without a read, it would be refused for ever.
        browser.Type(3, 0, "X");
        await host.Renderer.Dispatcher.InvokeAsync(() => text.Insert(0, "Z"));

        await WaitUntilAsync(() => browser.Value == "ZabcX");
        Assert.AreEqual("ZabcX", text.ToString());
        Assert.AreEqual(1, browser.Count("readValue"));
        host.AssertNoErrors();
    }

    [TestMethod]
    public async Task An_update_refused_because_the_control_drifted_is_delivered_after_a_read()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");
        var browser = new FakeTextControl();
        await using TextAreaHost host = await TextAreaHost.RenderAsync(browser, text);

        // The control holds something .NET does not know about, with no edit outstanding.
        browser.Value = browser.Known = "abcd";
        await host.Renderer.Dispatcher.InvokeAsync(() => text.Insert(0, "Z"));

        await WaitUntilAsync(() => browser.Value == "Zabcd");
        Assert.AreEqual("Zabcd", text.ToString());
        Assert.AreEqual(1, browser.Count("readValue"));
        host.AssertNoErrors();
    }

    [TestMethod]
    public async Task A_control_that_cannot_be_set_up_is_disabled_once_and_enabled_when_a_later_attempt_succeeds()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        var browser = new FakeTextControl { ImportFailure = new JSException("Refused by the content security policy.") };
        var errors = new List<Exception>();
        await using TextAreaHost host = await TextAreaHost.RenderAsync(browser, doc.GetText("first"), errors.Add);

        // Reporting the failure renders the control, and rendering must not try again.
        await host.RenderAsync(doc.GetText("first"));
        await Task.Delay(200);
        Assert.AreEqual(1, browser.Count("import"));
        Assert.HasCount(1, errors);
        Assert.IsTrue(host.IsDisabled());

        // Bound to another field, it tries again - and is usable once that works.
        browser.ImportFailure = null;
        await host.RenderAsync(doc.GetText("second"));
        await WaitUntilAsync(() => browser.Count("attach") == 1 && !host.IsDisabled());
        Assert.HasCount(1, errors);
        host.AssertNoErrors();
    }

    [TestMethod]
    public void A_value_read_back_keeps_every_code_unit_and_its_state()
    {
        string value = "a\uD800b😀";
        byte[] bytes = FakeTextControl.EncodeValue(7, value, 2, 3, true, 41);

        Assert.IsTrue(BcrdtTextArea.TryReadControlValue(bytes, out BcrdtTextArea.ControlValue read));
        Assert.AreEqual(new BcrdtTextArea.ControlValue(7, value, 2, 3, true, 41), read);

        Assert.IsFalse(BcrdtTextArea.TryReadControlValue(bytes.AsSpan(0, 39), out _));
        Assert.IsFalse(BcrdtTextArea.TryReadControlValue(bytes.AsSpan(0, bytes.Length - 1), out _));
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

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > TimeSpan.FromSeconds(10)) Assert.Fail("Timed out waiting for the control.");
            await Task.Delay(20);
        }
    }

    private sealed class StreamReference(byte[] data, TaskCompletionSource? gate = null) : IJSStreamReference
    {
        public long Length => data.Length;

        public async ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (gate is not null) await gate.Task;
            return new MemoryStream(data);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Stands in for a textarea and the module's text control functions, keeping the state the module
    /// keeps - what .NET believes the control holds, and how many edits it has reported - and answering
    /// the way the module answers.
    /// </summary>
    private sealed class FakeTextControl : IJSRuntime, IJSObjectReference
    {
        private readonly List<string> _calls = [];

        public string Value { get; set; } = string.Empty;

        public string Known { get; set; } = string.Empty;

        public int Inputs { get; private set; }

        public int Generation { get; private set; }

        public long Applied { get; private set; }

        public Exception? ImportFailure { get; set; }

        // Holds back a value read from the control until released.
        public TaskCompletionSource? ReadGate { get; set; }

        public int Count(string identifier)
        {
            lock (_calls) return _calls.Count(call => call == identifier);
        }

        // The user edits the control, and the module numbers the edit and reports it as a change.
        public Edit Type(int index, int removed, string inserted)
        {
            Value = Known = Value[..index] + inserted + Value[(index + removed)..];
            Inputs++;
            return new Edit(Generation, index, removed, inserted, index + inserted.Length, Applied, Inputs);
        }

        // The user edits the control by more than the module reports as a change: it asks to be read instead,
        // quoting the number of edits it has reported.
        public int Paste(int index, string inserted)
        {
            Value = Value[..index] + inserted + Value[index..];
            return Inputs;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            lock (_calls) _calls.Add(identifier);
            args ??= [];
            object? result = identifier switch
            {
                "import" => ImportFailure is { } failure ? throw failure : this,
                "attach" => Attach((int)args[2]!, (string)args[3]!),
                "reset" => Attach((int)args[1]!, (string)args[2]!),
                "detach" => null,
                "setValue" => SetValue(args),
                "readValue" => ReadValue((int)args[1]!),
                "appliedUpdate" => (int)args[1]! == Generation ? Applied : -1L,
                _ => throw new InvalidOperationException($"Unexpected call to '{identifier}'."),
            };
            return ValueTask.FromResult((TValue)result!);
        }

        private object? Attach(int generation, string value)
        {
            Generation = generation;
            Value = Known = value;
            Inputs = 0;
            Applied = 0;
            return null;
        }

        // Like the module: refused while edits are on their way, refused as drifted if the control does not
        // hold what the update was built against.
        private int SetValue(object?[] args)
        {
            int generation = (int)args[1]!;
            int inputs = (int)args[2]!;
            int expectedLength = (int)args[3]!;
            int start = (int)args[4]!;
            string removed = (string)args[5]!;
            string inserted = (string)args[6]!;
            long id = (long)args[9]!;

            if (generation != Generation || inputs != Inputs) return 0;
            if (Value != Known || Value.Length != expectedLength) return 2;
            if (start + removed.Length > Value.Length || Value.Substring(start, removed.Length) != removed) return 2;

            Value = Known = Value[..start] + inserted + Value[(start + removed.Length)..];
            Applied = id;
            return 1;
        }

        private IJSStreamReference ReadValue(int generation)
        {
            if (generation != Generation) return new StreamReference(EncodeValue(-1, string.Empty, 0, 0, false, 0), ReadGate);

            Known = Value;
            Inputs++;
            return new StreamReference(EncodeValue(Inputs, Value, Value.Length, Value.Length, false, Applied), ReadGate);
        }

        // The module's layout: five little-endian float64s, then the value as little-endian UTF-16.
        public static byte[] EncodeValue(int inputs, string value, int start, int end, bool backward, long applied)
        {
            byte[] bytes = new byte[40 + value.Length * 2];
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(bytes, inputs);
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(8), start);
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(16), end);
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(24), backward ? 1 : 0);
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(32), applied);
            for (int i = 0; i < value.Length; i++)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(40 + i * 2), value[i]);
            }
            return bytes;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public sealed record Edit(int Generation, int Index, int Removed, string Inserted, int Caret, long Applied, int Inputs);
    }

    /// <summary>A <see cref="BcrdtTextArea"/> rendered on its own, talking to a <see cref="FakeTextControl"/>.</summary>
    private sealed class TextAreaHost : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly AsyncServiceScope _scope;
        private readonly Action<Exception>? _onError;
        private readonly int _id;

        private TextAreaHost(ServiceProvider services, Action<Exception>? onError)
        {
            _services = services;
            _scope = services.CreateAsyncScope();
            _onError = onError;
            Renderer = new TestRenderer(_scope.ServiceProvider);
            TextArea = (BcrdtTextArea)Renderer.Instantiate(typeof(BcrdtTextArea));
            _id = Renderer.Attach(TextArea);
        }

        public TestRenderer Renderer { get; }

        public BcrdtTextArea TextArea { get; }

        public static async Task<TextAreaHost> RenderAsync(FakeTextControl browser, BcrdtText text, Action<Exception>? onError = null)
        {
            ServiceCollection services = new();
            services.AddBitBcrdtServices();
            services.AddSingleton<IJSRuntime>(browser);

            TextAreaHost host = new(services.BuildServiceProvider(), onError);
            await host.RenderAsync(text);
            return host;
        }

        public Task RenderAsync(BcrdtText text)
        {
            Dictionary<string, object?> parameters = new() { [nameof(BcrdtTextArea.Text)] = text };
            if (_onError is not null) parameters[nameof(BcrdtTextArea.OnError)] = new EventCallback<Exception>(null, _onError);
            return Renderer.Dispatcher.InvokeAsync(() => Renderer.RenderRootAsync(_id, ParameterView.FromDictionary(parameters)));
        }

        public Task DeliverAsync(FakeTextControl.Edit edit) => DispatchAsync(textArea => textArea.OnControlEdit(
            edit.Generation, edit.Index, edit.Removed, edit.Inserted, edit.Caret, edit.Caret, false, edit.Applied, edit.Inputs));

        public Task DispatchAsync(Func<BcrdtTextArea, Task> call) => Renderer.Dispatcher.InvokeAsync(() => call(TextArea));

        public bool IsDisabled() => Renderer.Dispatcher.InvokeAsync(() => Renderer.HasAttribute(_id, "disabled")).GetAwaiter().GetResult();

        public void AssertNoErrors()
        {
            lock (Renderer.Errors) Assert.IsEmpty(Renderer.Errors);
        }

        public async ValueTask DisposeAsync()
        {
            await Renderer.Dispatcher.InvokeAsync(async () => await ((IAsyncDisposable)Renderer).DisposeAsync());
            await _scope.DisposeAsync();
            await _services.DisposeAsync();
        }
    }

    // A renderer for one component, built from the render tree types on purpose: there is no other way
    // to render a component outside Blazor without a test library.
#pragma warning disable BL0006
    private sealed class TestRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        public List<Exception> Errors { get; } = [];

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public IComponent Instantiate(Type type) => InstantiateComponent(type);

        public int Attach(IComponent component) => AssignRootComponentId(component);

        public Task RenderRootAsync(int id, ParameterView parameters) => RenderRootComponentAsync(id, parameters);

        public bool HasAttribute(int id, string name)
        {
            ArrayRange<RenderTreeFrame> frames = GetCurrentRenderTreeFrames(id);
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames.Array[i] is { FrameType: RenderTreeFrameType.Attribute } frame && frame.AttributeName == name) return true;
            }
            return false;
        }

        protected override void HandleException(Exception exception)
        {
            lock (Errors) Errors.Add(exception);
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006

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
    }
}
