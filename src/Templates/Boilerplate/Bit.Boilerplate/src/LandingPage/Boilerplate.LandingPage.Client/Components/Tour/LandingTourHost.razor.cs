using Bit.Bonboarding;

namespace Boilerplate.LandingPage.Client.Components.Tour;

public partial class LandingTourHost
{
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IStringLocalizer<TourStrings> T = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Bonboarding is lazy loaded, so its services aren't registered at startup and come from here instead
            await serviceProvider.GetBonboardingService().StartAsync(LandingTourSteps.Create(T), new() { Resume = false, Force = true });
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
