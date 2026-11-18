namespace Bit.Bheadless;

/// <summary>Why a <see cref="BitBhFileUpload"/> refused a file.</summary>
[Flags]
public enum BitBhFileRejectionReason
{
    /// <summary>Not refused.</summary>
    None = 0,

    /// <summary>Its type or extension isn't in <c>Accept</c>.</summary>
    Type = 1,

    /// <summary>It is larger than <c>MaxFileSize</c>.</summary>
    TooLarge = 2,

    /// <summary>It is smaller than <c>MinFileSize</c>.</summary>
    TooSmall = 4,

    /// <summary>It would make more than <c>MaxFiles</c> files.</summary>
    TooMany = 8,

    /// <summary>The <c>Validate</c> callback refused it.</summary>
    Invalid = 16,
}
