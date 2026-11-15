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

    /// <summary>
    /// A get-only property that is indexed but not stored: its value is computed from the stored ones,
    /// so the index keys on it while the row bytes leave it out and no field number is assigned.
    /// </summary>
    public bool IndexOnly;

    /// <summary>Set when the property is read back through a constructor parameter rather than a setter.</summary>
    public bool BoundByConstructor;

    /// <summary>A <c>required</c> member: the reader's object initializer has to set it even when a constructor took it too.</summary>
    public bool IsRequired;

    /// <summary>
    /// A reference-type scalar (<c>string</c>, <c>byte[]</c>) declared non-nullable in a nullable-enabled context:
    /// the writer refuses a null in it, which would otherwise read back as an empty value. Nullable-oblivious
    /// declarations keep the old behavior.
    /// </summary>
    public bool RejectsNull;
    public string? HashIndexName;
    public bool HashIndexUnique;
    public string? OrderedIndexName;
    public bool OrderedIndexUnique;
}
