// Model shapes the generator used to emit uncompilable code for, or to crash on: a namespace part and
// property names that are keywords, an explicit interface implementation, and compound-index members named
// like tuple members. That this file compiles is half of the test; GeneratorEdgeCaseTests is the other half.
namespace Bit.BlazeDb.Tests.@event;

public interface IHasTenant
{
    string Tenant { get; set; }
}

[BlazeDbTable("keyword_rows")]
[BlazeDbCompoundIndex("ByItems", nameof(Item2), nameof(Item1))]
[BlazeDbCompoundOrderedIndex("ByRest", nameof(Rest), nameof(@class))]
public partial class KeywordRow : IHasTenant
{
    [BlazeDbKey]
    public int @int { get; set; }

    [BlazeDbIndex]
    public string @class { get; set; } = "";

    [BlazeDbOrderedIndex]
    public long @event { get; set; }

    public string Item1 { get; set; } = "";

    public int Item2 { get; set; }

    public string Rest { get; set; } = "";

    // Not a column: it cannot be named on the row.
    string IHasTenant.Tenant { get; set; } = "";

    public string? Maybe { get; set; }

    public byte[] Blob { get; set; } = [];
}

// A struct's strings are null whenever a row is made without them, with no compiler warning, so a struct keeps
// storing such a null as empty.
[BlazeDbTable("struct_rows")]
public partial struct StructRow
{
    [BlazeDbKey]
    public int Id { get; set; }

    public string Name { get; set; }
}

// A key type whose equality ignores part of its encoding: 1.0m == 1.00m.
[BlazeDbTable("decimal_keys")]
public partial class DecimalKeyed
{
    [BlazeDbKey]
    public decimal Id { get; set; }

    public string Note { get; set; } = "";
}
