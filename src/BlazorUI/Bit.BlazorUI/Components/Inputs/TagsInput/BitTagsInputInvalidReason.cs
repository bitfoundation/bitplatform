namespace Bit.BlazorUI;

/// <summary>
/// The reason a tag was rejected by <see cref="BitTagsInput"/>, reported through its OnInvalid callback.
/// </summary>
public enum BitTagsInputInvalidReason
{
    /// <summary>
    /// No reason was given, which is what an uninitialized value stands for rather than an actual rule.
    /// </summary>
    None = 0,

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

    /// <summary>
    /// The tag is not one of the suggestions, which RestrictToSuggestions made the only accepted values.
    /// </summary>
    NotSuggested,
}
