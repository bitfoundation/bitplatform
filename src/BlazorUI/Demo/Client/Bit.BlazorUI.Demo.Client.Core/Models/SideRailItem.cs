namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// One entry of the "on this page" rail, read out of the DOM rather than declared: any heading a
/// page marks with the example-section-title attribute becomes one of these.
/// </summary>
public class SideRailItem
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    /// <summary>
    /// The heading's level, so the rail can indent a section's examples under the section itself
    /// instead of listing two different kinds of thing as one flat run. 2 for the sections of an
    /// article, 3 for the examples inside one.
    /// </summary>
    public int Level { get; set; } = 2;
}
