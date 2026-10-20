using Bit.BlazeDb.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

/// <summary>
/// An index lookup may only stand in for a predicate when it selects exactly the rows the C#
/// operator would. Floating-point keys (NaN) and indexes with a comparer of their own do not, and
/// these queries have to come back as LINQ to Objects would answer them all the same.
/// </summary>
[TestClass]
public class IndexExactnessTests
{
    private static MeasuresContext Open() =>
        new(new DbContextOptionsBuilder<MeasuresContext>()
            .UseBlazeDb("exactness-" + Guid.NewGuid().ToString("N"))
            .Options);

    [TestMethod]
    public void NaN_Rows_Are_Not_Returned_By_A_Range_Or_An_Equality_On_A_Floating_Point_Index()
    {
        using var context = Open();
        context.Readings.Add(new Reading { Id = 1, Score = double.NaN, Level = double.NaN });
        context.Readings.Add(new Reading { Id = 2, Score = 1, Level = 2 });
        context.Readings.Add(new Reading { Id = 3, Score = 7, Level = 2 });
        context.SaveChanges();

        // The ordered index files NaN below every number, inside an open-ended range.
        Assert.AreSequenceEqual([2], context.Readings.Where(r => r.Score <= 5).Select(r => r.Id).ToList());
        Assert.AreSequenceEqual([2, 3], context.Readings.Where(r => r.Score >= 1).Select(r => r.Id).Order().ToList());

        // Both index kinds hold NaN as equal to itself.
        var nan = double.NaN;
        Assert.IsEmpty(context.Readings.Where(r => r.Level == nan).ToList());
        Assert.IsEmpty(context.Readings.Where(r => r.Score == nan).ToList());
        Assert.AreSequenceEqual([2, 3], context.Readings.Where(r => r.Level == 2).Select(r => r.Id).Order().ToList());

        context.Database.EnsureDeleted();
    }

    [TestMethod]
    public void An_Index_With_Its_Own_Comparer_Narrows_A_Query_But_Does_Not_Answer_It()
    {
        using var context = Open();
        context.Members.Add(new Member { Id = 1, Email = "a@x.com", Name = "B", Rank = 1 });
        context.Members.Add(new Member { Id = 2, Email = "b@x.com", Name = "b", Rank = 2 });
        context.Members.Add(new Member { Id = 3, Email = "c@x.com", Name = "a", Rank = 3 });
        context.SaveChanges();

        // The unique index ignores case; == does not.
        Assert.IsEmpty(context.Members.Where(m => m.Email == "A@x.com").ToList());
        Assert.AreSequenceEqual([1], context.Members.Where(m => m.Email == "a@x.com").Select(m => m.Id).ToList());
        var emails = new[] { "A@X.COM", "a@x.com" };
        Assert.AreSequenceEqual([1], context.Members.Where(m => emails.Contains(m.Email)).Select(m => m.Id).ToList());

        // The rank index runs backwards, so the rows of a range are not between its bounds.
        Assert.AreSequenceEqual([2, 3], context.Members.Where(m => m.Rank >= 2).Select(m => m.Id).Order().ToList());
        Assert.AreSequenceEqual([1, 2], context.Members.Where(m => m.Rank <= 2).Select(m => m.Id).Order().ToList());
        Assert.AreSequenceEqual([1, 2, 3], context.Members.OrderBy(m => m.Rank).Select(m => m.Id).ToList());

        // Nor does an ordering by the case-blind name come from that index.
        var names = context.Members.Select(m => m.Name).ToList();
        Assert.AreSequenceEqual(
            names.OrderBy(n => n).ToList(),
            context.Members.OrderBy(m => m.Name).Select(m => m.Name).ToList());

        context.Database.EnsureDeleted();
    }
}

public sealed class MeasuresContext : DbContext
{
    public MeasuresContext(DbContextOptions<MeasuresContext> options) : base(options)
    {
    }

    public DbSet<Reading> Readings => Set<Reading>();

    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Member>().UseBlazeDbTable(MemberTable.Table);
}

[BlazeDbTable("readings")]
public partial class Reading
{
    [BlazeDbKey]
    public int Id { get; set; }

    [BlazeDbOrderedIndex]
    public double Score { get; set; }

    [BlazeDbIndex]
    public double Level { get; set; }
}

public sealed class Member
{
    public int Id { get; set; }

    public string Email { get; set; } = "";

    public string Name { get; set; } = "";

    public int Rank { get; set; }
}

/// <summary>A hand-written descriptor, which is where an index can be given a comparer of its own.</summary>
public static class MemberTable
{
    public static readonly BlazeDbHashIndexDefinition<Member, string> Email =
        new(nameof(Member.Email), static m => m.Email, StringComparer.OrdinalIgnoreCase, unique: true);

    public static readonly BlazeDbOrderedIndexDefinition<Member, string> Name =
        new(nameof(Member.Name), static m => m.Name, StringComparer.OrdinalIgnoreCase);

    public static readonly BlazeDbOrderedIndexDefinition<Member, int> Rank =
        new(nameof(Member.Rank), static m => m.Rank, Comparer<int>.Create(static (x, y) => y.CompareTo(x)));

    public static readonly BlazeDbTableDescriptor<int, Member> Table = new(
        "members",
        static m => m.Id,
        WriteRow,
        ReadRow,
        static (w, k) => w.WriteVarInt(k),
        static (ref BlazeDbBufferReader r) => (int)r.ReadVarInt(),
        [Email, Name, Rank],
        keyMember: nameof(Member.Id));

    private static void WriteRow(BlazeDbBufferWriter writer, Member row)
    {
        writer.WriteTag(1, BlazeDbWireType.VarInt);
        writer.WriteVarInt(row.Id);
        writer.WriteTag(2, BlazeDbWireType.LengthDelimited);
        writer.WriteString(row.Email);
        writer.WriteTag(3, BlazeDbWireType.LengthDelimited);
        writer.WriteString(row.Name);
        writer.WriteTag(4, BlazeDbWireType.VarInt);
        writer.WriteVarInt(row.Rank);
    }

    private static Member ReadRow(ref BlazeDbBufferReader reader)
    {
        var row = new Member();
        while (reader.Remaining > 0)
        {
            var (field, wireType) = reader.ReadTag();
            switch (field)
            {
                case 1:
                    row.Id = (int)reader.ReadVarInt();
                    break;
                case 2:
                    row.Email = reader.ReadString();
                    break;
                case 3:
                    row.Name = reader.ReadString();
                    break;
                case 4:
                    row.Rank = (int)reader.ReadVarInt();
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }
        return row;
    }
}
