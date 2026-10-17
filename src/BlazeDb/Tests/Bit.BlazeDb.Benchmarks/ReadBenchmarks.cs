using BenchmarkDotNet.Attributes;
using Bit.BlazeDb;
using Microsoft.Data.Sqlite;

namespace Bit.BlazeDb.Benchmarks;

/// <summary>
/// Read-path comparison: BlazeDb in-memory tables vs SQLite (in-memory, prepared statements,
/// native codegen). Native SQLite is a *favorable* proxy for sqlite-wasm solutions - in the
/// browser those additionally pay JS interop, worker round-trips and wasm execution overhead,
/// so real-world gaps are larger than measured here.
/// </summary>
[MemoryDiagnoser]
public class ReadBenchmarks
{
    private const int Rows = 10_000;

    private BlazeDbDatabase _db = null!;
    private BlazeDbTable<int, BenchPerson> _people = null!;
    private SqliteConnection _sqlite = null!;
    private SqliteCommand _sqliteGet = null!;
    private SqliteCommand _sqliteRange = null!;
    private SqliteCommand _sqliteFlagCount = null!;
    private int _cursor;

    [GlobalSetup]
    public async Task Setup()
    {
        _db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(BenchPerson.Table));
        _people = _db.GetTable(BenchPerson.Table);

        _sqlite = new SqliteConnection("Data Source=:memory:");
        _sqlite.Open();
        Exec("CREATE TABLE people (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Flag INTEGER NOT NULL, Age INTEGER NOT NULL)");
        Exec("CREATE INDEX ix_people_age ON people (Age)");
        Exec("CREATE INDEX ix_people_flag ON people (Flag)");

        using (var txn = _db.BeginTransaction())
        {
            for (var i = 0; i < Rows; i++)
            {
                _people.Insert(new BenchPerson { Id = i, Name = $"Person {i}", Flag = i % 2 == 0, Age = i % 100 });
            }
            txn.Commit();
        }

        using (var sqliteTxn = _sqlite.BeginTransaction())
        {
            using var insert = _sqlite.CreateCommand();
            insert.Transaction = sqliteTxn;
            insert.CommandText = "INSERT INTO people (Id, Name, Flag, Age) VALUES ($id, $name, $flag, $age)";
            var id = insert.Parameters.Add("$id", SqliteType.Integer);
            var name = insert.Parameters.Add("$name", SqliteType.Text);
            var flag = insert.Parameters.Add("$flag", SqliteType.Integer);
            var age = insert.Parameters.Add("$age", SqliteType.Integer);
            for (var i = 0; i < Rows; i++)
            {
                id.Value = i;
                name.Value = $"Person {i}";
                flag.Value = i % 2 == 0 ? 1 : 0;
                age.Value = i % 100;
                insert.ExecuteNonQuery();
            }
            sqliteTxn.Commit();
        }

        _sqliteGet = _sqlite.CreateCommand();
        _sqliteGet.CommandText = "SELECT Id, Name, Flag, Age FROM people WHERE Id = $id";
        _sqliteGet.Parameters.Add("$id", SqliteType.Integer);

        _sqliteRange = _sqlite.CreateCommand();
        _sqliteRange.CommandText = "SELECT COUNT(*) FROM people WHERE Age BETWEEN $from AND $to";
        _sqliteRange.Parameters.Add("$from", SqliteType.Integer);
        _sqliteRange.Parameters.Add("$to", SqliteType.Integer);

        _sqliteFlagCount = _sqlite.CreateCommand();
        _sqliteFlagCount.CommandText = "SELECT COUNT(*) FROM people WHERE Flag = 1";
    }

    private void Exec(string sql)
    {
        using var command = _sqlite.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = true)]
    public BenchPerson? BlazeDb_PointGet() => _people.Get(Next());

    [Benchmark]
    public BenchPerson? Sqlite_PointGet()
    {
        _sqliteGet.Parameters[0].Value = Next();
        using var reader = _sqliteGet.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        return new BenchPerson
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Flag = reader.GetInt32(2) != 0,
            Age = reader.GetInt32(3),
        };
    }

    [Benchmark]
    public int BlazeDb_OrderedRangeCount()
    {
        var count = 0;
        foreach (var _ in _people.Range(BenchPerson.Indexes.Age, 25, 75))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public long Sqlite_RangeCount()
    {
        _sqliteRange.Parameters[0].Value = 25;
        _sqliteRange.Parameters[1].Value = 75;
        return (long)_sqliteRange.ExecuteScalar()!;
    }

    [Benchmark]
    public int BlazeDb_HashIndexCount() => _people.CountBy(BenchPerson.Indexes.Flag, true);

    [Benchmark]
    public long Sqlite_FlagCount() => (long)_sqliteFlagCount.ExecuteScalar()!;

    private int Next() => _cursor = (_cursor + 7919) % Rows;

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _sqliteGet.Dispose();
        _sqliteRange.Dispose();
        _sqliteFlagCount.Dispose();
        _sqlite.Dispose();
        await _db.DisposeAsync();
    }
}
