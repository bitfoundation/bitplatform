namespace Boilerplate.Workbench.Components.Shell;

/// <summary>Mutes or unmutes the tone, and the notification's sound, that announce the agent's replies.</summary>
public partial class SoundToggle
{
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private WorkbenchReviewSession reviewSession = default!;

    [Parameter] public BitSize? Size { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        // The button first renders before the saved state is back, so it shows it once it is.
        await WhenStateRestored();
    }
}
