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
