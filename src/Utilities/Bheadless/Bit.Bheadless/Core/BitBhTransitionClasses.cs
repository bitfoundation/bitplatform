namespace Bit.Bheadless;

/// <summary>CSS class sets for <see cref="BitBhTransition"/>.</summary>
public sealed class BitBhTransitionClasses
{
    /// <summary>Classes applied during the whole enter phase.</summary>
    public string? Enter { get; set; }
    /// <summary>Classes applied on the first frame of enter.</summary>
    public string? EnterFrom { get; set; }
    /// <summary>Classes applied from the second frame of enter until it ends.</summary>
    public string? EnterTo { get; set; }
    /// <summary>Classes applied once the enter phase completed.</summary>
    public string? Entered { get; set; }
    /// <summary>Classes applied during the whole leave phase.</summary>
    public string? Leave { get; set; }
    /// <summary>Classes applied on the first frame of leave.</summary>
    public string? LeaveFrom { get; set; }
    /// <summary>Classes applied from the second frame of leave until it ends.</summary>
    public string? LeaveTo { get; set; }

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["enter"] = Enter,
        ["enterFrom"] = EnterFrom,
        ["enterTo"] = EnterTo,
        ["entered"] = Entered,
        ["leave"] = Leave,
        ["leaveFrom"] = LeaveFrom,
        ["leaveTo"] = LeaveTo,
    };
}
