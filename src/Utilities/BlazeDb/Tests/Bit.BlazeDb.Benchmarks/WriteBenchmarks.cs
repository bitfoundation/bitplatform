using BenchmarkDotNet.Attributes;
using Bit.BlazeDb;
using Bit.BlazeDb.Storage;
using Microsoft.Data.Sqlite;

namespace Bit.BlazeDb.Benchmarks;

/// <summary>
/// Write-path comparison. BlazeDb is measured both without persistence (pure memory) and with
/// the full WAL pipeline over in-memory storage, which is what the browser pays on the commit
/// path (the OPFS flush itself happens in the background off the critical path).
/// </summary>
[MemoryDiagnoser]
public class WriteBenchmarks
{
    private const int Rows = 10_000;
    private const int BatchSize = 1_000;

    private BlazeDbDatabase _memDb = null!;
    private BlazeDbTable<int, BenchPerson> _memPeople = null!;
    private BlazeDbDatabase _journaledDb = null!;
    private BlazeDbTable<int, BenchPerson> _journaledPeople = null!;
    private SqliteConnection _sqlite = null!;
    private SqliteCommand _sqliteUpdate = null!;
    private int _cursor;

    [GlobalSetup]
    public async Task Setup()
    {
        _memDb = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(BenchPerson.Table));
        _memPeople = _memDb.GetTable(BenchPerson.Table);

        _journaledDb = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = new BlazeDbInMemoryStorage(),
            FlushInterval = TimeSpan.FromMilliseconds(100),
            CheckpointWalSize = long.MaxValue,
        }.AddTable(BenchPerson.Table));
        _journaledPeople = _journaledDb.GetTable(BenchPerson.Table);

        _sqlite = new SqliteConnection("Data Source=:memory:");
        _sqlite.Open();
        using (var create = _sqlite.CreateCommand())
        {
            create.CommandText =
                "CREATE TABLE people (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Flag INTEGER NOT NULL, Age INTEGER NOT NULL)";
            create.ExecuteNonQuery();
        }

        foreach (var people in new[] { _memPeople, _journaledPeople })
        {
            for (var i = 0; i < Rows; i++)
            {
                people.Insert(new BenchPerson { Id = i, Name = $"Person {i}", Flag = false, Age = i % 100 });
            }
        }

        using (var insert = _sqlite.CreateCommand())
        {
            insert.CommandText = "INSERT INTO people (Id, Name, Flag, Age) VALUES ($id, $name, 0, $age)";
            var id = insert.Parameters.Add("$id", SqliteType.Integer);
            var name = insert.Parameters.Add("$name", SqliteType.Text);
            var age = insert.Parameters.Add("$age", SqliteType.Integer);
            for (var i = 0; i < Rows; i++)
            {
                id.Value = i;
                name.Value = $"Person {i}";
                age.Value = i % 100;
                insert.ExecuteNonQuery();
            }
        }

        _sqliteUpdate = _sqlite.CreateCommand();
        _sqliteUpdate.CommandText = "UPDATE people SET Name = $name, Age = $age WHERE Id = $id";
        _sqliteUpdate.Parameters.Add("$name", SqliteType.Text);
        _sqliteUpdate.Parameters.Add("$age", SqliteType.Integer);
        _sqliteUpdate.Parameters.Add("$id", SqliteType.Integer);
    }

    [Benchmark(Baseline = true)]
    public void BlazeDb_Upsert_MemoryOnly()
    {
        var id = Next();
        _memPeople.Upsert(new BenchPerson { Id = id, Name = "updated", Flag = true, Age = id % 100 });
    }

    [Benchmark]
    public void BlazeDb_Upsert_WithWal()
    {
        var id = Next();
        _journaledPeople.Upsert(new BenchPerson { Id = id, Name = "updated", Flag = true, Age = id % 100 });
    }

    [Benchmark]
    public void Sqlite_Update()
    {
        var id = Next();
        _sqliteUpdate.Parameters[0].Value = "updated";
        _sqliteUpdate.Parameters[1].Value = id % 100;
        _sqliteUpdate.Parameters[2].Value = id;
        _sqliteUpdate.ExecuteNonQuery();
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void BlazeDb_TransactionBatch_WithWal()
    {
        using var txn = _journaledDb.BeginTransaction();
        for (var i = 0; i < BatchSize; i++)
        {
            var id = Next();
            _journaledPeople.Upsert(new BenchPerson { Id = id, Name = "batch", Flag = false, Age = id % 100 });
        }
        txn.Commit();
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Sqlite_TransactionBatch()
    {
        using var txn = _sqlite.BeginTransaction();
        _sqliteUpdate.Transaction = txn;
        for (var i = 0; i < BatchSize; i++)
        {
            var id = Next();
            _sqliteUpdate.Parameters[0].Value = "batch";
            _sqliteUpdate.Parameters[1].Value = id % 100;
            _sqliteUpdate.Parameters[2].Value = id;
            _sqliteUpdate.ExecuteNonQuery();
        }
        txn.Commit();
        _sqliteUpdate.Transaction = null;
    }

    private int Next() => _cursor = (_cursor + 7919) % Rows;

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _sqliteUpdate.Dispose();
        _sqlite.Dispose();
        await _memDb.DisposeAsync();
        await _journaledDb.DisposeAsync();
    }
}
