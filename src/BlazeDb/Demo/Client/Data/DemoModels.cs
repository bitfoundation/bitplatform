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
/// Proves the source generator is optional: this table's descriptor is written by
/// hand, so a positional record (which the generator rejects) works fine.
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
