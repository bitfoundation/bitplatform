using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The state a <see cref="BitBhDateField"/>, <see cref="BitBhTimeField"/>, <see cref="BitBhDateTimeField"/>, a picker
/// (<see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/>) or a <see cref="BitBhDateRangePickerInput"/> shares
/// with its <see cref="BitBhDateFieldSegments"/> and <see cref="BitBhDateFieldSegment"/>s.
/// </summary>
public sealed class BitBhDateFieldContext
{
    internal BitBhDateFieldContext(IBitBhDateFieldOwner owner) => Owner = owner;

    internal IBitBhDateFieldOwner Owner { get; }

    /// <summary>The segments in the order the culture writes them, separators included.</summary>
    public IReadOnlyList<BitBhDateSegment> Segments => Owner.Segments;

    /// <summary>Whether every segment is filled (the field then holds a value).</summary>
    public bool IsComplete => Owner.IsComplete;

    /// <summary>Whether the field is disabled.</summary>
    public bool IsDisabled => Owner.IsDisabled;

    /// <summary>Whether the field is read-only.</summary>
    public bool IsReadOnly => Owner.IsReadOnly;

    /// <summary>Whether the field is invalid (marked so, by validation, or its value is out of range).</summary>
    public bool IsInvalid => Owner.IsInvalid;

    /// <summary>Whether the field is required.</summary>
    public bool IsRequired => Owner.IsRequired;
}
