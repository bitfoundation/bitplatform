namespace Boilerplate.Workbench.Components.Preview;

/// <summary>
/// A page as the app shows it: inside the app's chrome, or bare when the scenario turns the chrome off.
/// </summary>
public partial class PreviewSurface
{
    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    /// <summary>Draws the device's status bar over the top safe area, for previews of handheld devices.</summary>
    [Parameter] public bool ShowStatusBar { get; set; }
}
