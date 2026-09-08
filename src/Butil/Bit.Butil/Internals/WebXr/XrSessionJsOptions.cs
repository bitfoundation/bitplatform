namespace Bit.Butil;

/// <summary>
/// The JS-shaped session options: the same members as <see cref="XrSessionOptions"/>, with the
/// reference space already the hyphenated string the specification uses.
/// </summary>
internal class XrSessionJsOptions
{
    public string[]? RequiredFeatures { get; set; }

    public string[]? OptionalFeatures { get; set; }

    public string ReferenceSpaceType { get; set; } = string.Empty;

    public int PoseIntervalMs { get; set; }
}
