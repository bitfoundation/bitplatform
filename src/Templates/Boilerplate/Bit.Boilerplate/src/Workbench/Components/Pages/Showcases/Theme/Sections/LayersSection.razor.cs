namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The theme's stacking order, disabled opacity, breakpoints and dialog layout.</summary>
public partial class LayersSection
{
    /// <summary>The layers from the back to the front, by the z-index the view resolved; as listed until the values are read.</summary>
    private IEnumerable<ThemeToken> LayersInOrder =>
        ThemeTokens.Layers.OrderBy(token => int.TryParse(ValueOf(token), NumberStyles.Integer, CultureInfo.InvariantCulture, out var zIndex) ? zIndex : 0);
}
