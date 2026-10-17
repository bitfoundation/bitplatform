namespace Bit.BlazeDb.Generators;

internal sealed class BlazeDbPropModel
{
    public string Name = "";
    public int FieldNumber;
    public BlazeDbPropCategory Category;
    public BlazeDbScalarKind Kind;

    /// <summary>Fully-qualified display of the property type as declared (for locals).</summary>
    public string TypeDisplay = "";

    /// <summary>Fully-qualified display of the non-nullable scalar (or element) type.</summary>
    public string ScalarTypeDisplay = "";

    public bool IsKey;
    public string? HashIndexName;
    public bool HashIndexUnique;
    public string? OrderedIndexName;
    public bool OrderedIndexUnique;
}
