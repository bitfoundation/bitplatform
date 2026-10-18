using Bit.BlazeDb;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Benchmarks;

[BlazeDbTable("people")]
public partial class BenchPerson
{
    [BlazeDbKey]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    [BlazeDbIndex]
    public bool Flag { get; set; }

    [BlazeDbOrderedIndex]
    public int Age { get; set; }
}

public sealed class BenchContext : DbContext
{
    public BenchContext(DbContextOptions<BenchContext> options) : base(options)
    {
    }

    public DbSet<BenchPerson> People => Set<BenchPerson>();
}
