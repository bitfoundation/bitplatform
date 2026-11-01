namespace Boilerplate.Workbench.Components;

/// <summary>
/// The router of every circuit, under the Blex provider that holds the pages' stores.
/// </summary>
public partial class Routes
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// The Redux DevTools bridge in development, for the shell's own circuit only: every preview frame would otherwise
    /// stream its whole state on each action and take time-travel messages that rewrite its stores.
    /// </summary>
    private bool EnableDevTools => AppEnvironment.IsDevelopment()
        && new Uri(NavigationManager.Uri).AbsolutePath.StartsWith("/preview/", StringComparison.OrdinalIgnoreCase) is false;
}
