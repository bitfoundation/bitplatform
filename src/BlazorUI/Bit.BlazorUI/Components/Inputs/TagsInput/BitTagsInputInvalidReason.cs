namespace Bit.BlazorUI;

/// <summary>
/// The reason a tag was rejected by <see cref="BitTagsInput"/>, reported through its OnInvalid callback.
/// </summary>
public enum BitTagsInputInvalidReason
{
    /// <summary>
    /// The tag is already in the list and duplicates are not allowed.
    /// </summary>
    Duplicate,

    /// <summary>
    /// The list already holds the maximum number of tags.
    /// </summary>
    MaxTags,

    /// <summary>
    /// The tag is shorter than the minimum length.
    /// </summary>
    MinLength,

    /// <summary>
    /// The tag does not match the required pattern.
    /// </summary>
    Pattern,

    /// <summary>
    /// The tag was rejected by the custom validator.
    /// </summary>
    Validator,
}
