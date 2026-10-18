namespace Bit.BlazeDb;

/// <summary>
/// Marks a partial class or record as a BlazeDb table. The source generator emits a
/// <c>BlazeDbTableDescriptor</c> (binary serializer, key extractor, index definitions) as a static
/// <c>Table</c> member on the type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class BlazeDbTableAttribute : Attribute
{
    public BlazeDbTableAttribute(string? name = null) => Name = name;

    /// <summary>Table name. Defaults to the type name.</summary>
    public string? Name { get; }
}
