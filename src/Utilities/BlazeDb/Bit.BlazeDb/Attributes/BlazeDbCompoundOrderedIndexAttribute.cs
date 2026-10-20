namespace Bit.BlazeDb;

/// <summary>
/// Declares an ordered index over several properties at once. Rows sort lexicographically by
/// the tuple, so a range over a leading-property prefix is still a single contiguous scan.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class BlazeDbCompoundOrderedIndexAttribute : Attribute
{
    public BlazeDbCompoundOrderedIndexAttribute(string name, params string[] propertyNames)
    {
        Name = name;
        PropertyNames = propertyNames;
    }

    public string Name { get; }

    public string[] PropertyNames { get; }

    /// <summary>
    /// Rejects writes that would duplicate the tuple of indexed values. The tuple is the value, so
    /// unlike a single-property unique index - where a null value is exempt - two rows that agree
    /// on every member conflict even where those members are null. (SQL's <c>UNIQUE</c> generally
    /// treats nulls as distinct and would allow them; this does not.)
    /// </summary>
    public bool Unique { get; set; }
}
