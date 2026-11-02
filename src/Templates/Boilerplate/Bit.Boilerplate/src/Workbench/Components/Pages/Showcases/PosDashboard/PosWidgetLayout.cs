namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

/// <summary>
/// One widget's place on the dashboard: which widget, its size and whether it is shown. The saved layout lists these
/// in display order.
/// </summary>
public sealed class PosWidgetLayout
{
    public string Key { get; set; } = string.Empty;

    public PosWidgetSize Size { get; set; }

    public bool IsVisible { get; set; } = true;
}
