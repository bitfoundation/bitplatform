namespace Bit.Butil;

/// <summary>
/// What JS reports about a session it has just opened: the mode that was granted, and the reference
/// space type it actually got - which may not be the one that was asked for.
/// </summary>
internal class XrSessionJsInfo
{
    public string Mode { get; set; } = string.Empty;

    public string ReferenceSpaceType { get; set; } = string.Empty;
}
