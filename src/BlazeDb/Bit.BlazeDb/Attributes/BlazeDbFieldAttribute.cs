namespace Bit.BlazeDb;

/// <summary>
/// Assigns an explicit, stable field number used in the binary format. Without it, field
/// numbers are assigned by declaration order (1-based). Use explicit numbers once a schema
/// is deployed, so reordering properties does not break persisted data.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class BlazeDbFieldAttribute : Attribute
{
    public BlazeDbFieldAttribute(int number) => Number = number;

    public int Number { get; }
}
