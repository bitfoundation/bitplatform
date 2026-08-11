namespace Boilerplate.Client.Core.Components.Layout.Header;

/// <summary>
/// The accent color swatches: one brand color fed to <see cref="BitThemeFactory"/> re-themes the
/// whole app live. The accent itself is owned by <see cref="AppAccentColorService"/> - this only
/// renders it and hands clicks over.
/// </summary>
public partial class AccentColorSwitcher
{
    [AutoInject] private AppAccentColorService accentColorService = default!;

    private Action? unsubscribe;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // Subscribed this early - rather than after the first render - so the restore that
        // AppClientCoordinator kicks off cannot land in the gap between rendering the swatches and
        // listening for the accent they mark.
        unsubscribe = PubSubService.Subscribe(ClientAppMessages.ACCENT_COLOR_CHANGED, async _ =>
        {
            await InvokeAsync(StateHasChanged);
        });
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribe?.Invoke();

        await base.DisposeAsync(disposing);
    }
}
