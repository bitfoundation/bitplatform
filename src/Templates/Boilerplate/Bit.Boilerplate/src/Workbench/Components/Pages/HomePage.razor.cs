namespace Boilerplate.Workbench.Components.Pages;

public partial class HomePage
{
    private static readonly (string IconName, string TextKey)[] chips =
    [
        (BitIconName.ClearNight, nameof(WorkbenchStrings.HomeChipDark)),
        (BitIconName.Globe, nameof(WorkbenchStrings.HomeChipRtl)),
        (BitIconName.Sync, nameof(WorkbenchStrings.HomeChipLoading)),
        (BitIconName.Camera, nameof(WorkbenchStrings.HomeChipStore)),
        (BitIconName.ChatBot, nameof(WorkbenchStrings.HomeChipLive))
    ];

    private static readonly (string IconName, string TitleKey, string TextKey)[] reasons =
    [
        (BitIconName.Robot, nameof(WorkbenchStrings.HomeWhyAgentsTitle), nameof(WorkbenchStrings.HomeWhyAgentsText)),
        (BitIconName.ChatBot, nameof(WorkbenchStrings.HomeWhyLiveTitle), nameof(WorkbenchStrings.HomeWhyLiveText)),
        (BitIconName.Code, nameof(WorkbenchStrings.HomeWhyRealTitle), nameof(WorkbenchStrings.HomeWhyRealText)),
        (BitIconName.Split, nameof(WorkbenchStrings.HomeWhyStatesTitle), nameof(WorkbenchStrings.HomeWhyStatesText)),
        (BitIconName.Devices2, nameof(WorkbenchStrings.HomeWhyDevicesTitle), nameof(WorkbenchStrings.HomeWhyDevicesText)),
        (BitIconName.Camera, nameof(WorkbenchStrings.HomeWhyStoresTitle), nameof(WorkbenchStrings.HomeWhyStoresText)),
        (BitIconName.Lightbulb, nameof(WorkbenchStrings.HomeWhyHeadStartTitle), nameof(WorkbenchStrings.HomeWhyHeadStartText)),
        (BitIconName.Lock, nameof(WorkbenchStrings.HomeWhySafeTitle), nameof(WorkbenchStrings.HomeWhySafeText)),
        (BitIconName.Repair, nameof(WorkbenchStrings.HomeWhyCustomizeTitle), nameof(WorkbenchStrings.HomeWhyCustomizeText))
    ];

    private static readonly (string TitleKey, string TextKey)[] steps =
    [
        (nameof(WorkbenchStrings.HomeHowStep1Title), nameof(WorkbenchStrings.HomeHowStep1Text)),
        (nameof(WorkbenchStrings.HomeHowStep2Title), nameof(WorkbenchStrings.HomeHowStep2Text)),
        (nameof(WorkbenchStrings.HomeHowStep3Title), nameof(WorkbenchStrings.HomeHowStep3Text))
    ];

    private bool isPromptCopied;

    [AutoInject] private WorkbenchReviewSession reviewSession = default!;

    private int OpenCount(string pageKey) => reviewSession.ThreadsOf(pageKey).Count(t => t.Status is not WorkbenchReviewStatus.Done);

    protected override void OnInitialized()
    {
        reviewSession.Changed += OnReviewChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await reviewSession.Open(Localizer[nameof(WorkbenchStrings.Reviewer)]);
            StateHasChanged();
        }
    }

    private void OnReviewChanged() => InvokeAsync(StateHasChanged);

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;

        await base.DisposeAsync();
    }

    private static BmTransition Enter(int index) => Bm.Spring(stiffness: 120, damping: 20).WithDelay(index * 0.07);

    private async Task CopyPrompt()
    {
        await JSRuntime.InvokeVoidAsync("workbench.copy", Localizer[nameof(WorkbenchStrings.HomeCtaPrompt)].Value);

        isPromptCopied = true;
        StateHasChanged();

        await Task.Delay(1500);

        isPromptCopied = false;
        StateHasChanged();
    }
}
