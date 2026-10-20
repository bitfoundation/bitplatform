using BenchmarkDotNet.Attributes;
using Bit.BlazeDb;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Benchmarks;

/// <summary>
/// EF Core LINQ and SaveChanges on the same in-memory store as the native engine APIs.
/// Native numbers here should match <see cref="ReadBenchmarks"/> / <see cref="WriteBenchmarks"/>;
/// the EF methods measure translator and change-tracker overhead on top of that store.
/// </summary>
[MemoryDiagnoser]
public class EfCoreBenchmarks
{
    private const int Rows = 10_000;

    private DbContextOptions<BenchContext> _options = null!;
    private BenchContext _read = null!;
    private BenchContext _write = null!;
    private BlazeDbTable<int, BenchPerson> _people = null!;
    private int _cursor;

    [GlobalSetup]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<BenchContext>()
            .UseBlazeDb("bench-" + Guid.NewGuid().ToString("N"))
            .Options;

        _read = new BenchContext(_options);
        _read.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        _write = new BenchContext(_options);
        _people = _read.Database.GetBlazeDb().GetTable(BenchPerson.Table);

        using var txn = _read.Database.GetBlazeDb().BeginTransaction();
        for (var i = 0; i < Rows; i++)
        {
            _people.Insert(new BenchPerson { Id = i, Name = $"Person {i}", Flag = i % 2 == 0, Age = i % 100 });
        }
        txn.Commit();
    }

    [Benchmark(Baseline = true)]
    public BenchPerson? Native_PointGet() => _people.Get(Next());

    [Benchmark]
    public BenchPerson? EfCore_Find()
    {
        var person = _write.People.Find(Next());
        _write.ChangeTracker.Clear();
        return person;
    }

    [Benchmark]
    public BenchPerson? EfCore_WhereByKey() =>
        _read.People.FirstOrDefault(p => p.Id == Next());

    [Benchmark]
    public int Native_HashIndexCount() => _people.CountBy(BenchPerson.Indexes.Flag, true);

    [Benchmark]
    public int EfCore_WhereFlagCount() => _read.People.Count(p => p.Flag);

    [Benchmark]
    public int Native_OrderedRangeCount()
    {
        var count = 0;
        foreach (var _ in _people.Range(BenchPerson.Indexes.Age, 25, 75))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int EfCore_WhereAgeRangeCount() =>
        _read.People.Count(p => p.Age >= 25 && p.Age <= 75);

    [Benchmark]
    public void Native_Upsert()
    {
        var id = Next();
        _people.Upsert(new BenchPerson { Id = id, Name = "updated", Flag = true, Age = id % 100 });
    }

    [Benchmark]
    public void EfCore_SaveChanges()
    {
        var id = Next();
        var person = _write.People.Find(id)!;
        person.Name = "updated";
        _write.SaveChanges();
        _write.ChangeTracker.Clear();
    }

    private int Next() => _cursor = (_cursor + 7919) % Rows;

    [GlobalCleanup]
    public void Cleanup()
    {
        _read.Dispose();
        _write.Dispose();
        using var wipe = new BenchContext(_options);
        wipe.Database.EnsureDeleted();
    }
}
