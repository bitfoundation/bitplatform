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

/// <summary>
/// Exercises constructor binding: a positional record has no parameterless constructor, so rows are
/// built through its primary constructor, with the remaining property set afterwards. Initial is an
/// index-only property - computed, indexed, never stored.
/// </summary>
[BlazeDbTable("contacts")]
public partial record Contact([property: BlazeDbKey] int Id, string Name, [property: BlazeDbOrderedIndex] DateOnly Birthday)
{
    public string? Nickname { get; init; }

    [BlazeDbIndex]
    public string Initial => Name.Length == 0 ? "" : Name[..1];
}

/// <summary>A class whose get-only properties are set through its constructor, and the newer scalar and collection types.</summary>
[BlazeDbTable("measurements")]
public partial class Measurement
{
    public Measurement(Guid id, TimeOnly at, Half value)
    {
        Id = id;
        At = at;
        Value = value;
    }

    [BlazeDbKey]
    public Guid Id { get; }

    [BlazeDbOrderedIndex]
    public TimeOnly At { get; }

    public Half Value { get; }

    [BlazeDbIndex]
    public Int128 Big { get; set; }

    public UInt128? Huge { get; set; }

    [BlazeDbOrderedIndex]
    public DateOnly? Day { get; set; }

    public HashSet<int> Bins { get; set; } = [];

    public ISet<string>? Labels { get; set; }

    public IReadOnlyList<double> Samples { get; set; } = [];

    public IEnumerable<Guid> Links { get; set; } = [];
}

/// <summary>
/// A required member taken by the constructor: the generated reader passes it to the constructor and sets it
/// again in the initializer, which the compiler insists on without [SetsRequiredMembers].
/// </summary>
[BlazeDbTable("required-gadgets")]
public partial class RequiredGadget
{
    public RequiredGadget(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [BlazeDbKey]
    public int Id { get; }

    public required string Name { get; init; }
}

/// <summary>Index-only computed properties, alone and inside a compound index.</summary>
[BlazeDbTable("emails")]
[BlazeDbCompoundIndex("DomainUser", nameof(Domain), nameof(User))]
public partial class EmailAddress
{
    [BlazeDbKey]
    public int Id { get; set; }

    public string Address { get; set; } = "";

    [BlazeDbOrderedIndex]
    public string Domain => Address[(Address.IndexOf('@') + 1)..];

    public string User => Address[..Math.Max(0, Address.IndexOf('@'))];
}
