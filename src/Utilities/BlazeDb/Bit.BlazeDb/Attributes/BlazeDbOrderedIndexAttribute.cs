namespace Bit.BlazeDb;

/// <summary>Declares an ordered secondary index over this property, enabling range scans.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class BlazeDbOrderedIndexAttribute : Attribute
{
    /// <summary>Index name. Defaults to the property name.</summary>
    public string? Name { get; set; }

    /// <summary>Rejects writes that would duplicate an indexed value. Null values are exempt.</summary>
    public bool Unique { get; set; }
}
