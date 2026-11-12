namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// The Workbench's landing page: why it exists, the agents' long tasks, how to use it, the catalog of pages with their review
/// status, and the flows.
/// </summary>
public partial class HomePage
{
    // The labels floating around the hero's devices.
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
        (BitIconName.TaskList, nameof(WorkbenchStrings.HomeWhyProgressTitle), nameof(WorkbenchStrings.HomeWhyProgressText)),
        (BitIconName.Microphone, nameof(WorkbenchStrings.HomeWhyVoiceTitle), nameof(WorkbenchStrings.HomeWhyVoiceText)),
        (BitIconName.Code, nameof(WorkbenchStrings.HomeWhyRealTitle), nameof(WorkbenchStrings.HomeWhyRealText)),
        (BitIconName.Split, nameof(WorkbenchStrings.HomeWhyStatesTitle), nameof(WorkbenchStrings.HomeWhyStatesText)),
        (BitIconName.Devices2, nameof(WorkbenchStrings.HomeWhyDevicesTitle), nameof(WorkbenchStrings.HomeWhyDevicesText)),
        (BitIconName.Camera, nameof(WorkbenchStrings.HomeWhyStoresTitle), nameof(WorkbenchStrings.HomeWhyStoresText)),
        (BitIconName.Lightbulb, nameof(WorkbenchStrings.HomeWhyHeadStartTitle), nameof(WorkbenchStrings.HomeWhyHeadStartText)),
        (BitIconName.Tiles, nameof(WorkbenchStrings.HomeWhyManyAppsTitle), nameof(WorkbenchStrings.HomeWhyManyAppsText)),
        (BitIconName.Lock, nameof(WorkbenchStrings.HomeWhySafeTitle), nameof(WorkbenchStrings.HomeWhySafeText)),
        (BitIconName.Repair, nameof(WorkbenchStrings.HomeWhyCustomizeTitle), nameof(WorkbenchStrings.HomeWhyCustomizeText))
    ];

    private static readonly (string TitleKey, string TextKey)[] steps =
    [
        (nameof(WorkbenchStrings.HomeHowStep1Title), nameof(WorkbenchStrings.HomeHowStep1Text)),
        (nameof(WorkbenchStrings.HomeHowStepAnswerTitle), nameof(WorkbenchStrings.HomeHowStepAnswerText)),
        (nameof(WorkbenchStrings.HomeHowStep2Title), nameof(WorkbenchStrings.HomeHowStep2Text)),
        (nameof(WorkbenchStrings.HomeHowStep3Title), nameof(WorkbenchStrings.HomeHowStep3Text))
    ];

    private bool isPromptCopied;
    // Hands the cards over to their reveal, which by then has played for those in sight, so they don't blink.
    private bool areRevealsLive;

    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
    [AutoInject] private ILogger<HomePage> logger = default!;

    /// <summary>The page the hero opens and shows on the laptop: the POS dashboard, or the catalog's first page when a project removed it.</summary>
    private static WorkbenchPageInfo? HeroPage => WorkbenchPageCatalog.Find("pos-dashboard") ?? WorkbenchPageCatalog.Pages.FirstOrDefault();

    /// <summary>The page the hero shows on the phone: the buildings map, or another page of the catalog.</summary>
    private static WorkbenchPageInfo? PhonePage => WorkbenchPageCatalog.Find("buildings") ?? WorkbenchPageCatalog.Pages.FirstOrDefault(p => p != HeroPage) ?? HeroPage;

    /// <summary>The catalog's pages by category, in the catalog's order, as the Pages pane groups them.</summary>
    private static IEnumerable<IGrouping<string, WorkbenchPageInfo>> PageGroups => WorkbenchPageCatalog.Pages.GroupBy(p => p.CategoryKey);

    /// <summary>How many of the catalog's columns a group takes: one per card, up to the three a wide screen has.</summary>
    private static string SpanClass(int cardCount) => $"span-{Math.Min(cardCount, 3)}";

    /// <summary>The page's comments that are not done yet.</summary>
    private int OpenCount(string pageKey) => reviewSession.ThreadsOf(pageKey).Count(t => t.Status is not WorkbenchReviewStatus.Done);

    protected override void OnInitialized()
    {
        reviewSession.Changed += OnReviewChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("workbench.loadThumbnails");

            // The review document joins once interactive, for the statuses, comment counts and flows on the cards.
            try
            {
                await reviewSession.Open(Localizer[nameof(WorkbenchStrings.Reviewer)]);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The review document could not be opened for the home page.");
            }

            StateHasChanged();

            await Task.Delay(5000);

            areRevealsLive = true;
            StateHasChanged();
        }
    }

    private void OnReviewChanged() => InvokeAsync(StateHasChanged);

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;

        await base.DisposeAsync();
    }

    /// <summary>The hero's entrance animation, staggered by the element's place.</summary>
    private static BmTransition Enter(int index) => Bm.Spring(stiffness: 120, damping: 20).WithDelay(index * 0.07);

    /// <summary>Copies the sample agent prompt and shows a check mark on the button for a moment.</summary>
    private async Task CopyPrompt()
    {
        // A browser can refuse the clipboard to a copy that arrives after a server round trip; Safari always does.
        try
        {
            await clipboard.WriteText(Localizer[nameof(WorkbenchStrings.HomeCtaPrompt)].Value);
        }
        catch (JSException)
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.CopyFailed)]);
            return;
        }

        isPromptCopied = true;
        StateHasChanged();

        await Task.Delay(1500);

        isPromptCopied = false;
        StateHasChanged();
    }
}
