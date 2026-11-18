namespace Bit.Bheadless;

/// <summary>Whether a <see cref="BitBhToggleGroup"/> allows one or many pressed items.</summary>
public enum BitBhToggleGroupType
{
    /// <summary>At most one item is on (behaves like a radio group).</summary>
    Single,

    /// <summary>Any number of items can be on.</summary>
    Multiple,
}
