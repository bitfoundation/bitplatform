using Bit.BlazeDb;
using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb.Demo.Client.Data;

public enum Priority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3,
}

/// <summary>
/// The demo's main table. It deliberately covers the whole surface the source
/// generator supports: every scalar kind, nullable values, collections, an
/// ignored property, both index kinds, a custom index name and explicit field
/// numbers are all represented somewhere across this file.
/// </summary>
[BlazeDbTable("todos")]
public partial class TodoEntry
{
    [BlazeDbKey]
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string? Notes { get; set; }

    [BlazeDbIndex]
    public bool Done { get; set; }

    [BlazeDbIndex]
    public string? Category { get; set; }

    [BlazeDbIndex(Name = "ByPriority")]
    public Priority Priority { get; set; }

    [BlazeDbOrderedIndex]
    public DateTime CreatedAt { get; set; }

    /// <summary>Estimated minutes of work - the numeric ordered index.</summary>
    [BlazeDbOrderedIndex]
    public int Effort { get; set; }

    public int? Rating { get; set; }

    public decimal Cost { get; set; }

    public double Weight { get; set; }

    public TimeSpan Duration { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public List<string> Tags { get; set; } = [];

    public int[] Scores { get; set; } = [];

    public byte[] Payload { get; set; } = [];

    /// <summary>Never written to the WAL or a snapshot.</summary>
    [BlazeDbIgnore]
    public string Transient { get; set; } = "computed at runtime";

    public TodoEntry Clone() => new()
    {
        Id = Id,
        Title = Title,
        Notes = Notes,
        Done = Done,
        Category = Category,
        Priority = Priority,
        CreatedAt = CreatedAt,
        Effort = Effort,
        Rating = Rating,
        Cost = Cost,
        Weight = Weight,
        Duration = Duration,
        DueAt = DueAt,
        Tags = [.. Tags],
        Scores = [.. Scores],
        Payload = [.. Payload],
    };
}

/// <summary>
/// A string-keyed table with explicit, stable field numbers. Used by the demo to
/// persist its own UI preferences, which proves multi-table databases work.
/// </summary>
[BlazeDbTable("settings")]
public partial class Setting
{
    [BlazeDbField(1)]
    [BlazeDbKey]
    public string Name { get; set; } = "";

    [BlazeDbField(2)]
    public string Value { get; set; } = "";

    [BlazeDbField(3)]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// The index shapes <see cref="TodoEntry"/> does not use, for the pages that open a private database
/// over it: a unique index, a compound hash index, a compound ordered index that pages by keyset, an
/// ordinal string index for prefix scans and an index-only computed property.
/// </summary>
[BlazeDbTable("people")]
[BlazeDbCompoundIndex("CityRole", nameof(City), nameof(Role))]
[BlazeDbCompoundOrderedIndex("JoinedId", nameof(Joined), nameof(Id))]
public partial class Person
{
    [BlazeDbKey]
    public int Id { get; set; }

    /// <summary>No two people share an address; any number may have none.</summary>
    [BlazeDbIndex(Unique = true)]
    public string? Email { get; set; }

    /// <summary>An ordinal string index, which is what <c>StartsWith</c> scans.</summary>
    [BlazeDbOrderedIndex]
    public string Name { get; set; } = "";

    public string City { get; set; } = "";

    public string Role { get; set; } = "";

    public DateOnly Joined { get; set; }

    /// <summary>Computed from <see cref="Email"/>: indexed, but never written to the row bytes.</summary>
    [BlazeDbIndex]
    public string Domain => Email is { } email ? email[(email.IndexOf('@') + 1)..] : "";

    public Person With(Action<Person> change)
    {
        var copy = new Person { Id = Id, Email = Email, Name = Name, City = City, Role = Role, Joined = Joined };
        change(copy);
        return copy;
    }
}

/// <summary>
/// Unique indexes beyond the single-property hash case: a compound unique index, whose value is the
/// whole tuple - so, unlike a single-property one, null members are not exempt - and a unique
/// ordered index.
/// </summary>
[BlazeDbTable("seats")]
[BlazeDbCompoundIndex("RowNumber", nameof(Row), nameof(Number), Unique = true)]
public partial class Seat
{
    [BlazeDbKey]
    public int Id { get; set; }

    public string? Row { get; set; }

    public int Number { get; set; }

    [BlazeDbOrderedIndex(Unique = true)]
    public string? Code { get; set; }
}

/// <summary>
/// A positional record: no parameterless constructor, so rows are built through the primary
/// constructor and the init-only <see cref="Nickname"/> is set afterwards. <see cref="Initial"/> is an
/// index-only computed property.
/// </summary>
[BlazeDbTable("contacts")]
public partial record Contact([property: BlazeDbKey] int Id, string Name, [property: BlazeDbOrderedIndex] DateOnly Birthday)
{
    public string? Nickname { get; init; }

    [BlazeDbIndex]
    public string Initial => Name.Length == 0 ? "" : Name[..1];
}

/// <summary>
/// Proves the source generator is optional: this table's descriptor is written by
/// hand, serializer and all, for a positional record.
/// </summary>
public sealed record Metric(int Id, string Name, double Value, DateTime At);

public static class MetricTable
{
    // Declared before the descriptor: static initializers run in declaration order.
    public static readonly BlazeDbHashIndexDefinition<Metric, string> ByName =
        new("ByName", static m => m.Name, StringComparer.OrdinalIgnoreCase);

    public static readonly BlazeDbOrderedIndexDefinition<Metric, double> ByValue =
        new("ByValue", static m => m.Value);

    public static readonly BlazeDbTableDescriptor<int, Metric> Descriptor = new(
        "metrics",
        static m => m.Id,
        WriteRow,
        ReadRow,
        static (w, k) => w.WriteVarInt(k),
        static (ref BlazeDbBufferReader r) => (int)r.ReadVarInt(),
        [ByName, ByValue]);

    private static void WriteRow(BlazeDbBufferWriter writer, Metric row)
    {
        writer.WriteTag(1, BlazeDbWireType.VarInt);
        writer.WriteVarInt(row.Id);
        writer.WriteTag(2, BlazeDbWireType.LengthDelimited);
        writer.WriteString(row.Name);
        writer.WriteTag(3, BlazeDbWireType.Fixed64);
        writer.WriteDouble(row.Value);
        writer.WriteTag(4, BlazeDbWireType.Fixed64);
        writer.WriteDateTime(row.At);
    }

    private static Metric ReadRow(ref BlazeDbBufferReader reader)
    {
        var id = 0;
        var name = "";
        var value = 0d;
        var at = default(DateTime);

        while (reader.Remaining > 0)
        {
            var (field, wireType) = reader.ReadTag();
            switch (field)
            {
                case 1: id = (int)reader.ReadVarInt(); break;
                case 2: name = reader.ReadString(); break;
                case 3: value = reader.ReadDouble(); break;
                case 4: at = reader.ReadDateTime(); break;
                default: reader.SkipField(wireType); break;
            }
        }

        return new Metric(id, name, value, at);
    }
}
