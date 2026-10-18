// Compiled into this test assembly by the real generator AND embedded as text, which the inspector tests
// hand to InspectBlazeDbTable: what the inspector reports about this file has to be what the generator
// built from it. Between them the types cover every property category, explicit and automatic field
// numbers, every index kind, a custom index name, a unique index and both compound kinds.

namespace Bit.BlazeDb.Tests.Mcp.Models;

public enum Mood
{
    Calm,
    Busy,
}

[BlazeDbTable("inspected-items")]
[BlazeDbCompoundIndex("OwnerMood", nameof(Owner), nameof(Mood))]
[BlazeDbCompoundOrderedIndex("OwnerCreated", nameof(Owner), nameof(CreatedAt), Unique = true)]
public partial class InspectedItem
{
    [BlazeDbKey]
    public Guid Id { get; set; }

    [BlazeDbIndex(Unique = true)]
    public string Slug { get; set; } = "";

    public string? Owner { get; set; }

    [BlazeDbIndex(Name = "ByMood")]
    public Mood Mood { get; set; }

    [BlazeDbOrderedIndex]
    public DateTime CreatedAt { get; set; }

    public int? Rating { get; set; }

    public double Weight { get; set; }

    public float Ratio { get; set; }

    public decimal Price { get; set; }

    public TimeSpan Duration { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public List<string> Tags { get; set; } = [];

    public long[] Samples { get; set; } = [];

    public byte[] Payload { get; set; } = [];

    [BlazeDbIgnore]
    public string Transient { get; set; } = "";
}

[BlazeDbTable("inspected-numbered")]
public partial class NumberedItem
{
    [BlazeDbKey, BlazeDbField(10)]
    public int Id { get; set; }

    [BlazeDbField(3)]
    public string Name { get; set; } = "";

    [BlazeDbOrderedIndex, BlazeDbField(7)]
    public long Score { get; set; }
}
