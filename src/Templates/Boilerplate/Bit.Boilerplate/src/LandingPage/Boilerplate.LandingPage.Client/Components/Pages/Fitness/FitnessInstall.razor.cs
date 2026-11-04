using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public partial class FitnessInstall
{
    private static readonly (FitnessInstallGuide Kind, string Device, string Steps)[] guides =
    [
        (FitnessInstallGuide.Iphone, nameof(FitnessStrings.InstallIphone), nameof(FitnessStrings.InstallIphoneSteps)),
        (FitnessInstallGuide.Android, nameof(FitnessStrings.InstallAndroid), nameof(FitnessStrings.InstallAndroidSteps)),
        (FitnessInstallGuide.Computer, nameof(FitnessStrings.InstallComputer), nameof(FitnessStrings.InstallComputerSteps)),
        (FitnessInstallGuide.Mac, nameof(FitnessStrings.InstallMac), nameof(FitnessStrings.InstallMacSteps))
    ];

    [AutoInject] private InstallPrompt installPrompt = default!;
    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private IStringLocalizer<FitnessStrings> F = default!;

    [Parameter] public bool Compact { get; set; }

    [Parameter] public string? Class { get; set; }

    private bool canPrompt;
    private bool isStandalone;
    private bool installed;
    private bool dismissed;
    private FitnessInstallGuide? guide;
    private ButilSubscription? availability;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            isStandalone = await installPrompt.IsStandalone();
            canPrompt = await installPrompt.IsAvailable();
            guide = Detect(await navigator.GetUserAgent());
            availability = await installPrompt.OnAvailable(platforms => _ = InvokeAsync(() =>
            {
                canPrompt = true;
                StateHasChanged();
            }));

            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Install()
    {
        var result = await installPrompt.Prompt();

        canPrompt = false;
        installed = result.Accepted;
        dismissed = result.Outcome is InstallPromptOutcome.Dismissed;
    }

    private static FitnessInstallGuide Detect(string userAgent) => userAgent switch
    {
        _ when userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) => FitnessInstallGuide.Iphone,
        _ when userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) => FitnessInstallGuide.Android,
        _ when userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)
            && userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase)
            && userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase) is false
            && userAgent.Contains("Edg", StringComparison.OrdinalIgnoreCase) is false
            && userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase) is false => FitnessInstallGuide.Mac,
        _ => FitnessInstallGuide.Computer
    };

    public async ValueTask DisposeAsync()
    {
        if (availability is not null)
        {
            await availability.DisposeAsync();
        }
    }
}

public enum FitnessInstallGuide
{
    Iphone,
    Android,
    Computer,
    Mac
}
