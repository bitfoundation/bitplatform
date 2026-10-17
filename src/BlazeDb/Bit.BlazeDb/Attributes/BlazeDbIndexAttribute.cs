namespace Bit.BlazeDb;

/// <summary>Declares a hash (equality) secondary index over this property.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class BlazeDbIndexAttribute : Attribute
{
    /// <summary>Index name. Defaults to the property name.</summary>
    public string? Name { get; set; }

    /// <summary>Rejects writes that would duplicate an indexed value. Null values are exempt.</summary>
    public bool Unique { get; set; }
}
