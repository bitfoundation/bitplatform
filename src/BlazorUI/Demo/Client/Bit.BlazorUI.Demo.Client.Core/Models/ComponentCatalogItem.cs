namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// One component as the documentation site talks about it: the gallery card, the home page list and
/// the prev/next pager on a demo page are all the same record rendered three ways.
/// </summary>
public sealed class ComponentCatalogItem
{
    /// <summary>The component's display name, without the Bit prefix (e.g. "DatePicker").</summary>
    public required string Name { get; init; }

    /// <summary>The demo page's route (e.g. "/components/datepicker").</summary>
    public required string Url { get; init; }

    /// <summary>The category the nav groups it under (e.g. "Inputs").</summary>
    public required string Category { get; init; }

    /// <summary>
    /// The names the component is also known by in other libraries ("Select, ComboBox"), taken from
    /// the nav item so the gallery search finds a component by the name the reader already knows.
    /// </summary>
    public string? Aliases { get; init; }

    /// <summary>One line on what the component is for. Shown on the gallery card.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Everything the gallery's search box matches against, lower-cased once at build time.</summary>
    public string SearchText { get; init; } = string.Empty;
}
