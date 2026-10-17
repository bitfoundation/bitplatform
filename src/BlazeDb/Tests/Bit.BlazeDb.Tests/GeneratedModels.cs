namespace Bit.BlazeDb.Tests;

public enum Priority
{
    Low = 0,
    Normal = 1,
    High = 2,
}

/// <summary>Exercises the source generator across the supported type surface.</summary>
[BlazeDbTable("todos")]
public partial class TodoItem
{
    [BlazeDbKey]
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string? Notes { get; set; }

    [BlazeDbIndex]
    public bool Done { get; set; }

    [BlazeDbIndex]
    public string? Category { get; set; }

    [BlazeDbOrderedIndex]
    public DateTime CreatedAt { get; set; }

    public Priority Priority { get; set; }

    public int? Rating { get; set; }

    public decimal Cost { get; set; }

    public double Weight { get; set; }

    public TimeSpan Duration { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public List<string> Tags { get; set; } = [];

    public int[] Scores { get; set; } = [];

    public byte[] Payload { get; set; } = [];

    [BlazeDbIgnore]
    public string Transient { get; set; } = "not persisted";
}

/// <summary>Exercises explicit field numbers and a string key.</summary>
[BlazeDbTable]
public partial class Setting
{
    [BlazeDbField(5)]
    [BlazeDbKey]
    public string Name { get; set; } = "";

    [BlazeDbField(9)]
    public string Value { get; set; } = "";
}

/// <summary>Exercises unique constraints and compound indexes.</summary>
[BlazeDbTable("accounts")]
[BlazeDbCompoundIndex("TenantEmail", nameof(TenantId), nameof(Email), Unique = true)]
[BlazeDbCompoundOrderedIndex("TenantCreated", nameof(TenantId), nameof(CreatedAt))]
[BlazeDbCompoundOrderedIndex("TenantUsername", nameof(TenantId), nameof(Username))]
public partial class Account
{
    [BlazeDbKey]
    public int Id { get; set; }

    [BlazeDbIndex(Unique = true)]
    public string Username { get; set; } = "";

    [BlazeDbIndex]
    public int TenantId { get; set; }

    [BlazeDbOrderedIndex]
    public string Email { get; set; } = "";

    [BlazeDbOrderedIndex(Unique = true)]
    public int? Slot { get; set; }

    public DateTime CreatedAt { get; set; }
}
