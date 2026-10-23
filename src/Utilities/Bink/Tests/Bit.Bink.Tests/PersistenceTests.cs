using System.Numerics;
using Bit.Bink.Tests.Components;
using Bunit;
using Microsoft.JSInterop;

namespace Bit.Bink.Tests;

/// <summary>A clock tests move by hand. Timers fire, on the calling thread, when the clock passes their due time.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        lock (_timers) _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        _now += by;
        ManualTimer[] due;
        lock (_timers) due = _timers.Where(t => t.Due is { } at && at <= _now).ToArray();
        foreach (ManualTimer timer in due) timer.Fire();
    }

    private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state) : ITimer
    {
        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime;
            return true;
        }

        public void Fire()
        {
            Due = null;
            callback(state);
        }

        public void Dispose() => Due = null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>A store that fails while told to, to show a failure is reported and retried.</summary>
internal sealed class FlakyStore : IBinkDocumentStore
{
    public BinkMemoryDocumentStore Inner { get; } = new();

    public bool Failing { get; set; }

    public int Saves { get; private set; }

    public Task SaveAsync(string key, ReadOnlyMemory<byte> document, CancellationToken cancellationToken = default)
    {
        Saves++;
        return Failing ? throw new IOException("The disk is full.") : Inner.SaveAsync(key, document, cancellationToken);
    }

    public Task<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default) => Inner.LoadAsync(key, cancellationToken);

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default) => Inner.DeleteAsync(key, cancellationToken);
}

/// <summary>A store whose first write waits until released, as a slow disk or a busy IndexedDB does.</summary>
internal sealed class GatedStore : IBinkDocumentStore
{
    private int _saves;

    public BinkMemoryDocumentStore Inner { get; } = new();

    /// <summary>Completes once the first write has begun, by when the document has been serialized.</summary>
    public TaskCompletionSource Writing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task SaveAsync(string key, ReadOnlyMemory<byte> document, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _saves) == 1)
        {
            Writing.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
        await Inner.SaveAsync(key, document, cancellationToken);
    }

    public Task<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default) => Inner.LoadAsync(key, cancellationToken);

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default) => Inner.DeleteAsync(key, cancellationToken);
}

/// <summary>Dirty tracking, autosaving and restoring, and the browser store's contract.</summary>
[TestClass]
public sealed class PersistenceTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private static BinkStroke Stroke(int i) => TestData.Stroke(new Vector2(i * 10, 0), new Vector2(i * 10 + 5, 5));

    /// <summary>Completes on the next successful save.</summary>
    private static Task NextSave(BinkAutosave autosave)
    {
        var saved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? sender, EventArgs e)
        {
            autosave.Saved -= Handler;
            saved.TrySetResult();
        }
        autosave.Saved += Handler;
        return saved.Task.WaitAsync(Wait);
    }

    [TestMethod]
    public void A_document_is_dirty_from_its_first_change_until_it_is_marked_saved()
    {
        var document = new BinkDocument();
        Assert.IsFalse(document.IsDirty);
        document.AddElement(Stroke(0));
        Assert.IsTrue(document.IsDirty);
        document.MarkSaved();
        Assert.IsFalse(document.IsDirty);

        long saving = document.Revision;
        document.AddElement(Stroke(1));
        document.MarkSaved(saving);
        Assert.IsTrue(document.IsDirty, "What changed while an asynchronous save wrote the bytes is still unsaved.");
        Assert.IsTrue(document.Clone().IsDirty);
    }

    [TestMethod]
    public async Task A_change_is_saved_once_editing_pauses()
    {
        var clock = new ManualTimeProvider();
        var store = new BinkMemoryDocumentStore();
        var document = new BinkDocument();
        await using var autosave = new BinkAutosave(document, store, "board", new BinkAutosaveOptions { TimeProvider = clock });

        document.AddElement(Stroke(0));
        Assert.IsTrue(autosave.HasUnsavedChanges);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.IsEmpty(store.Keys, "Not while the user is still drawing.");

        Task saved = NextSave(autosave);
        clock.Advance(TimeSpan.FromSeconds(1.5));
        await saved;

        Assert.IsFalse(autosave.HasUnsavedChanges);
        Assert.IsNotNull(autosave.LastSaved);
        Assert.IsTrue(document.IsDirty, "Autosaving is a recovery copy; it does not count as the application saving.");
        BinkDocument? restored = await BinkAutosave.RestoreAsync(store, "board");
        Assert.AreEqual(1, restored!.ElementCount);
    }

    [TestMethod]
    public async Task Continuous_editing_is_saved_at_least_every_max_delay()
    {
        var clock = new ManualTimeProvider();
        var store = new BinkMemoryDocumentStore();
        var document = new BinkDocument();
        await using var autosave = new BinkAutosave(document, store, "board", new BinkAutosaveOptions { TimeProvider = clock, Delay = TimeSpan.FromSeconds(2), MaxDelay = TimeSpan.FromSeconds(10) });

        // A change every second, never the two-second pause: the ten-second ceiling is what saves. The document is not
        // touched once the save is due, because without a synchronization context the save runs on another thread.
        Task saved = NextSave(autosave);
        for (int i = 0; i < 10; i++)
        {
            Assert.IsFalse(saved.IsCompleted, $"Saved after {i} seconds, before the ceiling.");
            document.AddElement(Stroke(i));
            clock.Advance(TimeSpan.FromSeconds(1));
        }
        await saved;
        Assert.AreEqual(10, (await BinkAutosave.RestoreAsync(store, "board"))!.ElementCount);
    }

    [TestMethod]
    public async Task A_failed_save_is_reported_and_tried_again()
    {
        var clock = new ManualTimeProvider();
        var store = new FlakyStore { Failing = true };
        var document = new BinkDocument();
        await using var autosave = new BinkAutosave(document, store, "board", new BinkAutosaveOptions { TimeProvider = clock, MaxDelay = TimeSpan.FromSeconds(10) });
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        autosave.SaveFailed += (_, ex) => failed.TrySetResult(ex);

        document.AddElement(Stroke(0));
        clock.Advance(TimeSpan.FromSeconds(3));
        Exception error = await failed.Task.WaitAsync(Wait);
        Assert.IsInstanceOfType<IOException>(error);
        Assert.AreSame(error, autosave.LastError);
        Assert.IsTrue(autosave.HasUnsavedChanges);

        store.Failing = false;
        Task saved = NextSave(autosave);
        clock.Advance(TimeSpan.FromSeconds(11));
        await saved;
        Assert.IsNull(autosave.LastError);
        Assert.IsNotNull(await store.LoadAsync("board"));
    }

    [TestMethod]
    public async Task Saving_now_during_a_save_also_saves_what_changed_since_it_began()
    {
        var clock = new ManualTimeProvider();
        var store = new GatedStore();
        var document = new BinkDocument();
        await using var autosave = new BinkAutosave(document, store, "board", new BinkAutosaveOptions { TimeProvider = clock });
        document.AddElement(Stroke(0));
        clock.Advance(TimeSpan.FromSeconds(3));
        await store.Writing.Task.WaitAsync(Wait);

        // The save under way serialized one element; the second arrives while it is being written.
        document.AddElement(Stroke(1));
        Task saving = autosave.SaveNowAsync();
        Assert.IsFalse(saving.IsCompleted);
        store.Release.SetResult();
        await saving.WaitAsync(Wait);

        Assert.IsFalse(autosave.HasUnsavedChanges);
        Assert.AreEqual(2, (await BinkAutosave.RestoreAsync(store, "board"))!.ElementCount);
    }

    [TestMethod]
    public async Task Discarding_removes_the_copy_and_disposing_saves_what_waits()
    {
        var clock = new ManualTimeProvider();
        var store = new BinkMemoryDocumentStore();
        var document = new BinkDocument();
        var autosave = new BinkAutosave(document, store, "board", new BinkAutosaveOptions { TimeProvider = clock });
        document.AddElement(Stroke(0));
        await autosave.SaveNowAsync();
        Assert.IsNotNull(await store.LoadAsync("board"));

        await autosave.DiscardAsync();
        Assert.IsNull(await store.LoadAsync("board"));
        Assert.IsFalse(autosave.HasUnsavedChanges);

        document.AddElement(Stroke(1));
        await autosave.DisposeAsync();
        BinkDocument? restored = await BinkAutosave.RestoreAsync(store, "board");
        Assert.AreEqual(2, restored!.ElementCount, "Disposing saved the change that was still waiting.");
    }

    [TestMethod]
    public async Task Remote_changes_can_be_left_out()
    {
        var clock = new ManualTimeProvider();
        var store = new FlakyStore();
        var document = new BinkDocument();
        await using var autosave = new BinkAutosave(document, store, "board", new BinkAutosaveOptions { TimeProvider = clock, IncludeRemoteChanges = false, SaveOnDispose = false });

        document.Apply(new BinkAddElementOperation(document.Layers[0].Id, Stroke(0)), BinkChangeOrigin.Remote);
        clock.Advance(TimeSpan.FromMinutes(1));
        await Task.Delay(50);
        Assert.AreEqual(0, store.Saves);
    }

    [TestMethod]
    public async Task Restoring_reads_nothing_or_reports_a_copy_that_is_not_a_drawing()
    {
        var store = new BinkMemoryDocumentStore();
        Assert.IsNull(await BinkAutosave.RestoreAsync(store, "missing"));

        await store.SaveAsync("broken", "{\"format\":\"bink\",\"version\":99}"u8.ToArray());
        string? error = null;
        Assert.IsNull(await BinkAutosave.RestoreAsync(store, "broken", onError: e => error = e));
        Assert.Contains("newer version", error!);
    }
}

/// <summary>The IndexedDB store's contract with its browser module.</summary>
[TestClass]
public sealed class BrowserDocumentStoreTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    [TestMethod]
    public async Task Saving_hands_the_bytes_over_and_loading_reads_them_back_as_a_stream()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("storeSave", _ => true).SetVoidResult();
        byte[] stored = "{\"format\":\"bink\"}"u8.ToArray();
        // The browser leads with a byte that says whether anything is stored, because a stream cannot be empty.
        module.Setup<IJSStreamReference>("storeLoad", i => (string)i.Arguments[0]! == "board").SetResult(new TestStreamReference([1, .. stored]));
        module.Setup<IJSStreamReference>("storeLoad", i => (string)i.Arguments[0]! == "missing").SetResult(new TestStreamReference([0]));
        module.Setup<bool>("storeDelete", _ => true).SetResult(true);
        await using var store = new BinkBrowserDocumentStore(JSInterop.JSRuntime);

        await store.SaveAsync("board", stored);
        CollectionAssert.AreEqual(stored, (byte[])module.Invocations.Single(i => i.Identifier == "storeSave").Arguments[1]!);
        CollectionAssert.AreEqual(stored, await store.LoadAsync("board"));
        Assert.IsNull(await store.LoadAsync("missing"), "Nothing stored is the marker byte alone, read as null.");
        Assert.IsTrue(await store.DeleteAsync("board"));
    }

    [TestMethod]
    public async Task A_stored_document_larger_than_the_limit_is_refused()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Setup<IJSStreamReference>("storeLoad", _ => true).SetResult(new TestStreamReference([1, .. new byte[2048]]));
        await using var store = new BinkBrowserDocumentStore(JSInterop.JSRuntime) { MaxDocumentBytes = 1024 };
        await Assert.ThrowsExactlyAsync<IOException>(() => store.LoadAsync("board"));
    }
}
