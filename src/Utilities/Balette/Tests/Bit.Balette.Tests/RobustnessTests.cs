using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Balette.Tests;

[TestClass]
public class HistoryRobustnessTests
{
    private static BaletteHistory Create(IBaletteHistoryStore store, out LogSink log, TimeProvider? time = null)
    {
        log = new LogSink();
        return new BaletteHistory(store, new BaletteOptions(), time, new SinkLogger<BaletteHistory>(log));
    }

    [TestMethod]
    public async Task Entries_the_history_cannot_use_are_dropped_on_load()
    {
        var history = Create(new FixedStore(new Dictionary<string, BaletteCommandUsage>
        {
            ["null"] = null!,
            [""] = new(2, DateTimeOffset.UtcNow, false),
            ["negative"] = new(-7, DateTimeOffset.UtcNow, true),
            ["ok"] = new(3, DateTimeOffset.UtcNow, false),
        }), out _);

        await history.EnsureLoadedAsync();
        await history.RecordAsync("null");

        Assert.AreEqual(1, history.Get("null")!.Count);
        Assert.AreEqual(0, history.Get("negative")!.Count);
        Assert.IsNull(history.Get(""));
        AssertSequence.AreEqual(["negative"], history.Pinned());
        CollectionAssert.AreEquivalent(new[] { "null", "ok" }, history.Recents(10).ToArray());
    }

    [TestMethod]
    public async Task A_store_that_fails_to_load_leaves_an_empty_working_history()
    {
        var history = Create(new FailingStore(failLoad: true, failSave: false), out var log);

        await history.EnsureLoadedAsync();
        await history.RecordAsync("a");

        Assert.IsTrue(history.IsLoaded);
        Assert.AreEqual(1, history.Get("a")!.Count);
        Assert.ContainsSingle(log.Entries.Where(e => e.Level == LogLevel.Warning && e.Exception is IOException));
    }

    [TestMethod]
    public async Task A_store_that_fails_to_save_keeps_the_change_for_the_session()
    {
        var history = Create(new FailingStore(failLoad: false, failSave: true), out var log);

        await history.RecordAsync("a");
        await history.TogglePinAsync("b");

        Assert.AreEqual(1, history.Get("a")!.Count);
        Assert.IsTrue(history.IsPinned("b"));
        Assert.HasCount(2, log.Entries.Where(e => e.Level == LogLevel.Warning && e.Exception is IOException).ToList());
    }

    [TestMethod]
    public async Task A_command_pinned_and_unpinned_without_being_used_is_not_a_recent()
    {
        var history = Create(new BaletteInMemoryHistoryStore(), out _);

        await history.TogglePinAsync("never-used");
        await history.TogglePinAsync("never-used");

        Assert.IsEmpty(history.Recents(5));
    }

    [TestMethod]
    public async Task Readers_never_see_a_snapshot_being_changed()
    {
        var history = Create(new BaletteInMemoryHistoryStore(), out _, new FakeTimeProvider());
        await history.EnsureLoadedAsync();

        // Writers on the thread pool, a reader enumerating as the palette does while it ranks.
        var writers = Enumerable.Range(0, 4).Select(w => Task.Run(async () =>
        {
            for (var i = 0; i < 200; i++)
            {
                await history.RecordAsync($"c{w}-{i % 20}");
            }
        })).ToArray();

        while (!writers.All(t => t.IsCompleted))
        {
            _ = history.Recents(10);
            _ = history.Pinned();
            _ = history.Frecency("c0-1");
        }

        await Task.WhenAll(writers);
        Assert.AreEqual(80, history.Recents(1000).Count);
    }

    private sealed class FixedStore(Dictionary<string, BaletteCommandUsage> data) : IBaletteHistoryStore
    {
        public ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default) => new(data);

        public ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class FailingStore(bool failLoad, bool failSave) : IBaletteHistoryStore
    {
        public ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
            => failLoad ? throw new IOException("Quota exceeded") : new(new Dictionary<string, BaletteCommandUsage>());

        public async ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (failSave)
            {
                throw new IOException("Quota exceeded");
            }
        }
    }
}

[TestClass]
public class UrlSafetyTests
{
    private static readonly HashSet<string> Allowed = new BaletteOptions().AllowedUrlSchemes;

    [TestMethod]
    [DataRow("/settings")]
    [DataRow("settings")]
    [DataRow("../up")]
    [DataRow("?q=a:b")]
    [DataRow("#section:2")]
    [DataRow("a/b:c")]
    [DataRow("https://example.com/x")]
    [DataRow("HTTP://example.com")]
    [DataRow("//cdn.example.com/x")]
    [DataRow("mailto:someone@example.com")]
    [DataRow("tel:+15551234")]
    [DataRow("")]
    public void Relative_links_and_allowed_schemes_may_be_followed(string href)
        => Assert.IsNull(BaletteUrl.UnsafeScheme(href, Allowed));

    [TestMethod]
    [DataRow("javascript:alert(1)", "javascript")]
    [DataRow("JaVaScRiPt:alert(1)", "javascript")]
    [DataRow(" \u0001javascript:alert(1)", "javascript")]
    [DataRow("java\tscript:alert(1)", "javascript")]
    [DataRow("java\nscript:alert(1)", "javascript")]
    [DataRow("data:text/html,<script>alert(1)</script>", "data")]
    [DataRow("vbscript:msgbox", "vbscript")]
    [DataRow("file:///etc/passwd", "file")]
    public void Other_schemes_are_refused(string href, string scheme)
        => Assert.AreEqual(scheme, BaletteUrl.UnsafeScheme(href, Allowed));

    [TestMethod]
    public void The_allow_list_can_be_extended()
    {
        var options = new BaletteOptions();
        options.AllowedUrlSchemes.Add("vscode");

        Assert.IsNull(BaletteUrl.UnsafeScheme("vscode://file/x", options.AllowedUrlSchemes));
    }
}

[TestClass]
public class OptionValidationTests
{
    [TestMethod]
    public void Values_that_would_break_the_palette_are_refused()
    {
        var options = new BaletteOptions();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.DebounceMs = -1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxResults = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxRecents = -1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxHistoryEntries = -1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.RowHeight = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.RowHeight = float.NaN);

        options.DebounceMs = 0;
        options.MaxRecents = 0;
        Assert.AreEqual(0, options.DebounceMs);
        Assert.AreEqual(150, options.MaxResults);
    }
}

[TestClass]
public class DerivedIdTests
{
    [TestMethod]
    public void A_title_of_symbols_only_derives_a_distinct_id()
    {
        var rocket = BaletteCmd.New("🚀").Group("Launch").Build();
        var sparkles = BaletteCmd.New("✨").Group("Launch").Build();

        Assert.AreEqual("launch.u1f680", rocket.Id);
        Assert.AreEqual("launch.u2728", sparkles.Id);
        Assert.AreEqual("u2b", BaletteCmd.New("+").Build().Id);
    }

    [TestMethod]
    public void Letters_keep_deriving_the_same_slug_as_before()
    {
        Assert.AreEqual("navigation.go-to-settings", BaletteCmd.New("Go to Settings").Group("Navigation").Build().Id);
        Assert.AreEqual("تنظیمات", BaletteCmd.New("تنظیمات").Build().Id);
        Assert.AreEqual("rename", BaletteCmd.New("Rename…").Build().Id);
    }
}
