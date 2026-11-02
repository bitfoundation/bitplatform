namespace Bit.BlazeDb.Generators;

internal enum BlazeDbPropCategory
{
    Scalar,
    NullableValueScalar,
    NullableRefScalar,
    ListCollection,
    ArrayCollection,

    /// <summary>A <c>HashSet&lt;T&gt;</c>, or <c>ISet&lt;T&gt;</c> / <c>IReadOnlySet&lt;T&gt;</c>, which are read back as one.</summary>
    SetCollection,
}
