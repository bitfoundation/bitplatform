using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Demo.Client.Data;

/// <summary>
/// Helpers for the throwaway databases individual pages open so they can crash,
/// corrupt and hammer them without touching the visitor's real data.
/// </summary>
public static class DemoSandbox
{
    public static BlazeDbDatabaseOptions BuildOptions(
        IBlazeDbStorage? storage = null,
        TimeSpan? flushInterval = null,
        long? checkpointWalSize = null)
    {
        var options = new BlazeDbDatabaseOptions { Storage = storage };

        if (flushInterval is { } interval)
        {
            options.FlushInterval = interval;
        }

        if (checkpointWalSize is { } size)
        {
            options.CheckpointWalSize = size;
        }

        return options
            .AddTable(TodoEntry.Table)
            .AddTable(Setting.Table)
            .AddTable(MetricTable.Descriptor);
    }

    public static ValueTask<BlazeDbDatabase> OpenAsync(
        IBlazeDbStorage? storage = null,
        TimeSpan? flushInterval = null,
        long? checkpointWalSize = null) =>
        BlazeDbDatabase.OpenAsync(BuildOptions(storage, flushInterval, checkpointWalSize));

    public static string Bytes(long value) => value switch
    {
        < 1024 => $"{value} B",
        < 1024 * 1024 => $"{value / 1024.0:0.#} KB",
        _ => $"{value / (1024.0 * 1024.0):0.##} MB",
    };

    /// <summary>Averages are fractional; keep one decimal place below a kilobyte.</summary>
    public static string Bytes(double value) =>
        value < 1024 ? $"{value:0.#} B" : Bytes((long)value);

    public static string Duration(double milliseconds) => milliseconds switch
    {
        < 0.001 => $"{milliseconds * 1_000_000:0} ns",
        < 1 => $"{milliseconds * 1000:0.#} µs",
        < 1000 => $"{milliseconds:0.##} ms",
        _ => $"{milliseconds / 1000:0.##} s",
    };
}

public static class DemoData
{
    private static readonly (string Title, string? Category, Priority Priority, int Effort, string[] Tags)[] Rows =
    [
        ("Design the WAL record layout", "engine", Priority.High, 90, ["wal", "format"]),
        ("Wire OPFS handles through JSImport", "browser", Priority.Critical, 120, ["opfs", "interop"]),
        ("Benchmark point reads against SQLite", "bench", Priority.Normal, 45, ["perf"]),
        ("Emit key extractors from the generator", "codegen", Priority.High, 75, ["roslyn"]),
        ("Add torn-write recovery tests", "engine", Priority.Critical, 60, ["durability", "tests"]),
        ("Document the query primitives", "docs", Priority.Low, 30, ["docs"]),
        ("Ordered index range scan fast path", "engine", Priority.High, 110, ["index"]),
        ("Web Locks single-writer election", "browser", Priority.Normal, 50, ["locks"]),
        ("Snapshot compaction scheduling", "engine", Priority.Normal, 80, ["snapshot"]),
        ("Trim-safe serializer emit", "codegen", Priority.High, 95, ["aot", "roslyn"]),
        ("Measure WAL flush amplification", "bench", Priority.Low, 40, ["perf", "wal"]),
        ("Write the getting-started guide", "docs", Priority.Normal, 55, ["docs"]),
        ("Nullable index key handling", "engine", Priority.Normal, 35, ["index"]),
        ("Reader skip-unknown-field path", "codegen", Priority.Low, 25, ["format"]),
        ("Crash-recovery smoke test in browser", "browser", Priority.High, 70, ["durability"]),
        ("Profile transaction batch commits", "bench", Priority.Normal, 65, ["perf", "txn"]),
        ("Explain the storage abstraction", "docs", Priority.Low, 20, ["docs"]),
        ("Checkpoint while writes are in flight", "engine", Priority.Critical, 130, ["snapshot", "wal"]),
        ("Enum property serialization", "codegen", Priority.Low, 15, ["format"]),
        ("Quota-exceeded handling in OPFS", "browser", Priority.Normal, 60, ["opfs"]),
        (" Compare against IndexedDB wrappers", "bench", Priority.Low, 50, ["perf"]),
        ("Publish the API reference", "docs", Priority.Normal, 85, ["docs"]),
    ];

    /// <summary>
    /// Seeds sample rows in a single transaction, which is both the fastest way
    /// to bulk load and a nice demonstration of batch atomicity.
    /// </summary>
    public static int Seed(BlazeDbDatabase database, BlazeDbTable<Guid, TodoEntry> table, int count = 22, int seed = 20260817)
    {
        var random = new Random(seed);
        var created = DateTime.UtcNow.AddDays(-Rows.Length);
        var inserted = 0;

        using var txn = database.BeginTransaction();

        for (var i = 0; i < count; i++)
        {
            var template = Rows[i % Rows.Length];
            var suffix = i >= Rows.Length ? $" #{i / Rows.Length + 1}" : "";

            table.Insert(new TodoEntry
            {
                Id = Guid.NewGuid(),
                Title = template.Title.Trim() + suffix,
                Notes = i % 4 == 0 ? "Captured from the design review." : null,
                Done = i % 3 == 0,
                Category = i % 7 == 5 ? null : template.Category,
                Priority = template.Priority,
                CreatedAt = created.AddHours(i * 7),
                Effort = template.Effort,
                Rating = i % 5 == 0 ? null : random.Next(1, 6),
                Cost = Math.Round((decimal)(random.NextDouble() * 240), 2),
                Weight = Math.Round(random.NextDouble() * 10, 3),
                Duration = TimeSpan.FromMinutes(template.Effort),
                DueAt = i % 3 == 1 ? null : new DateTimeOffset(created.AddDays(i % 11 + 1), TimeSpan.Zero),
                Tags = [.. template.Tags],
                Scores = [random.Next(0, 100), random.Next(0, 100), random.Next(0, 100)],
                Payload = [(byte)i, (byte)(i * 7 % 251), 0xBE, 0xEF],
            });

            inserted++;
        }

        txn.Commit();
        return inserted;
    }

    /// <summary>Minimal rows for benchmarking, cheap to build in bulk.</summary>
    public static void SeedForBench(BlazeDbDatabase database, BlazeDbTable<Guid, TodoEntry> table, int count, out Guid[] ids)
    {
        ids = new Guid[count];
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        using var txn = database.BeginTransaction();

        for (var i = 0; i < count; i++)
        {
            ids[i] = Guid.NewGuid();
            table.Insert(new TodoEntry
            {
                Id = ids[i],
                Title = $"bench row {i}",
                Done = i % 2 == 0,
                Category = (i % 4) switch { 0 => "engine", 1 => "browser", 2 => "bench", _ => "docs" },
                Priority = (Priority)(i % 4),
                CreatedAt = created.AddMinutes(i),
                Effort = i % 120,
            });
        }

        txn.Commit();
    }

    public static readonly string[] Categories = ["engine", "browser", "bench", "codegen", "docs"];
}
