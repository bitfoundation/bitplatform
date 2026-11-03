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
            await serviceProvider.GetBonboardingService().StartAsync(LandingTourSteps.Create(T), new() { Resume = false, Force = true });
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
