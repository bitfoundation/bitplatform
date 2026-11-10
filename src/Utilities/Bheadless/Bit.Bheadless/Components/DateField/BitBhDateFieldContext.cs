using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>What a segmented field shares with its <see cref="BitBhDateFieldSegment"/>s.</summary>
internal interface IBitBhDateFieldOwner
{
    IReadOnlyList<BitBhDateSegment> Segments { get; }
    bool IsDisabled { get; }
    bool IsReadOnly { get; }
    bool IsInvalid { get; }
    bool IsRequired { get; }
    bool IsComplete { get; }
    BitBhDateSegmentType? FocusedSegment { get; }
    string? DescribedBy { get; }
    string SegmentId(BitBhDateSegmentType type);
    string SegmentName(BitBhDateSegmentType type);
    string ValueText(BitBhDateSegmentType type);
    Task HandleKeyDownAsync(BitBhDateSegmentType type, KeyboardEventArgs e);
    void HandleFocus(BitBhDateSegmentType type);
    Task HandleBlurAsync(BitBhDateSegmentType type);
}

/// <summary>
/// The state a <see cref="BitBhDateField"/>, <see cref="BitBhTimeField"/> or <see cref="BitBhDatePicker"/> shares with
/// its <see cref="BitBhDateFieldSegments"/> and <see cref="BitBhDateFieldSegment"/>s.
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
