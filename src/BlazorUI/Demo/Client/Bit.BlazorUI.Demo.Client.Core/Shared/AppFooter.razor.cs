namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class AppFooter
{
    [Parameter] public BitAppShell? AppShell { get; set; }


    private async Task GoToTop()
    {
        if (AppShell is null) return;

        await AppShell.GoToTop(BitScrollBehavior.Instant);
    }
}
