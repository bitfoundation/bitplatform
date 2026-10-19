namespace Bit.BlazeDb;

/// <summary>Marks the primary key property of a table row.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class BlazeDbKeyAttribute : Attribute;
