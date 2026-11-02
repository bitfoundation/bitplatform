using BenchmarkDotNet.Attributes;
using Bit.BlazeDb;

namespace Bit.BlazeDb.Benchmarks;

/// <summary>
/// Engine-only index costs as the table grows: the first page of an ordered index (what a paged list
/// does on every render), a page from the middle of a range, a whole range walked end to end, and
/// building the table - every insert maintains the primary key and both secondary indexes.
/// </summary>
[MemoryDiagnoser]
public class IndexBenchmarks
{
    private const int PageSize = 20;

    private BlazeDbDatabase _db = null!;
    private BlazeDbTable<int, BenchPerson> _people = null!;

    [Params(10_000, 100_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(BenchPerson.Table));
        _people = _db.GetTable(BenchPerson.Table);
        Fill(_people, Rows);
    }

    private static void Fill(BlazeDbTable<int, BenchPerson> people, int rows)
    {
        for (var i = 0; i < rows; i++)
        {
            people.Insert(new BenchPerson { Id = i, Name = $"Person {i}", Flag = i % 2 == 0, Age = i % 100 });
        }
    }

    [Benchmark]
    public int OrderBy_FirstPage() => Sum(_people.OrderBy(BenchPerson.Indexes.Age).Take(PageSize));

    [Benchmark]
    public int OrderByDescending_FirstPage() => Sum(_people.OrderBy(BenchPerson.Indexes.Age, descending: true).Take(PageSize));

    [Benchmark]
    public int Range_FirstPage() => Sum(_people.Range(BenchPerson.Indexes.Age, 10, 90).Take(PageSize));

    [Benchmark]
    public int Range_WalkAll()
    {
        var count = 0;
        foreach (var _ in _people.Range(BenchPerson.Indexes.Age, 25, 75))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int Lookup_WalkAll()
    {
        var count = 0;
        foreach (var _ in _people.Lookup(BenchPerson.Indexes.Flag, true))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public async Task<int> Build_Table()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(BenchPerson.Table));
        var people = db.GetTable(BenchPerson.Table);
        Fill(people, Rows);
        return people.Count;
    }

    private static int Sum(IEnumerable<BenchPerson> rows)
    {
        var sum = 0;
        foreach (var row in rows)
        {
            sum += row.Id;
        }
        return sum;
    }

    [GlobalCleanup]
    public async Task Cleanup() => await _db.DisposeAsync();
}
