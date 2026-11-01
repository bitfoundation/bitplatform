namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>A settings section's error state, with a retry, for the error data state.</summary>
public partial class SettingsLoadFailed
{
    [Parameter] public EventCallback OnRetry { get; set; }
}
