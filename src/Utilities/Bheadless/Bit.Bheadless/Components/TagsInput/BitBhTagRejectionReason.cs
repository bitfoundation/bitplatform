namespace Bit.Bheadless;

/// <summary>Why a <see cref="BitBhTagsInput"/> refused a tag.</summary>
public enum BitBhTagRejectionReason
{
    /// <summary>The tag is already in the list (and <c>AllowDuplicates</c> is off).</summary>
    Duplicate,

    /// <summary>The list already holds <c>Max</c> tags.</summary>
    MaxReached,

    /// <summary>The <c>Validate</c> callback refused it.</summary>
    Invalid,
}
