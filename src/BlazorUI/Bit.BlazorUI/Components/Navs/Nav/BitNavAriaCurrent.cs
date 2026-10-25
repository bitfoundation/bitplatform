namespace Bit.BlazorUI;

/// <summary>
/// Defines the value of the aria-current attribute reported by the current link of a set.
/// </summary>
public enum BitNavAriaCurrent
{
    /// <summary>
    /// Represents the current page within a set of pages.
    /// </summary>
    Page,

    /// <summary>
    /// Represents the current step within a process.
    /// </summary>
    Step,

    /// <summary>
    /// Represents the current location within an environment or context.
    /// </summary>
    Location,

    /// <summary>
    /// Represents the current date within a collection of dates.
    /// </summary>
    Date,

    /// <summary>
    /// Represents the current time within a set of times.
    /// </summary>
    Time,

    /// <summary>
    /// Represents the current item within a set, without saying which kind of set it is.
    /// </summary>
    True
}
