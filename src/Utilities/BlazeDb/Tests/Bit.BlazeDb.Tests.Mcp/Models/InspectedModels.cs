// Compiled into this test assembly by the real generator AND embedded as text, which the inspector tests
// hand to InspectBlazeDbTable: what the inspector reports about this file has to be what the generator
// built from it. Between them the types cover every property category, explicit and automatic field
// numbers, every index kind, a custom index name, a unique index, both compound kinds and a positional record.

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

// A positional record has no parameterless constructor: the generated reader builds it through its
// primary constructor and sets the rest afterwards, so the inspector has to read the fields off both.
[BlazeDbTable("inspected-records")]
public partial record InspectedRecord([property: BlazeDbKey] int Id, string Name, [property: BlazeDbOrderedIndex] DateOnly Day)
{
    public Half Level { get; init; }

    public HashSet<string> Labels { get; init; } = [];
}

// One positional parameter: the record's copy constructor takes as many arguments as its primary one, and
// the inspector has to read the fields off the constructor the reader really calls.
[BlazeDbTable("inspected-single")]
public partial record SingleParameterRecord([property: BlazeDbKey] int Id)
{
    public string Note { get; init; } = "";
}

// A required property the constructor also takes is passed to it and set again in the reader's
// initializer - one field, to be reported once.
[BlazeDbTable("inspected-required")]
public partial class RequiredBoundItem
{
    public RequiredBoundItem(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [BlazeDbKey]
    public int Id { get; }

    public required string Name { get; init; }
}
