namespace Bit.BlazeDb;

/// <summary>Excludes a property from serialization.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class BlazeDbIgnoreAttribute : Attribute;
