namespace Boilerplate.Workbench.Components.Pages;

public partial class HomePage
{
    private static readonly (string IconName, string TextKey)[] chips =
    [
        (BitIconName.ClearNight, nameof(WorkbenchStrings.HomeChipDark)),
        (BitIconName.Globe, nameof(WorkbenchStrings.HomeChipRtl)),
        (BitIconName.Sync, nameof(WorkbenchStrings.HomeChipLoading)),
        (BitIconName.Camera, nameof(WorkbenchStrings.HomeChipStore))
    ];

    private static readonly (string IconName, string Color, string TitleKey, string TextKey)[] reasons =
    [
        (BitIconName.Robot, "#8b5cf6", nameof(WorkbenchStrings.HomeWhyAgentsTitle), nameof(WorkbenchStrings.HomeWhyAgentsText)),
        (BitIconName.Code, "#4f6bed", nameof(WorkbenchStrings.HomeWhyRealTitle), nameof(WorkbenchStrings.HomeWhyRealText)),
        (BitIconName.Split, "#f5a524", nameof(WorkbenchStrings.HomeWhyStatesTitle), nameof(WorkbenchStrings.HomeWhyStatesText)),
        (BitIconName.Devices2, "#0fb5ae", nameof(WorkbenchStrings.HomeWhyDevicesTitle), nameof(WorkbenchStrings.HomeWhyDevicesText)),
        (BitIconName.Camera, "#e5487d", nameof(WorkbenchStrings.HomeWhyStoresTitle), nameof(WorkbenchStrings.HomeWhyStoresText)),
        (BitIconName.Lightbulb, "#22a06b", nameof(WorkbenchStrings.HomeWhyHeadStartTitle), nameof(WorkbenchStrings.HomeWhyHeadStartText)),
        (BitIconName.Lock, "#64748b", nameof(WorkbenchStrings.HomeWhySafeTitle), nameof(WorkbenchStrings.HomeWhySafeText)),
        (BitIconName.People, "#d97706", nameof(WorkbenchStrings.HomeWhyShareTitle), nameof(WorkbenchStrings.HomeWhyShareText))
    ];

    private static readonly (string TitleKey, string TextKey)[] steps =
    [
        (nameof(WorkbenchStrings.HomeHowStep1Title), nameof(WorkbenchStrings.HomeHowStep1Text)),
        (nameof(WorkbenchStrings.HomeHowStep2Title), nameof(WorkbenchStrings.HomeHowStep2Text)),
        (nameof(WorkbenchStrings.HomeHowStep3Title), nameof(WorkbenchStrings.HomeHowStep3Text))
    ];

    private static readonly (string Name, string IconName, string TextKey, string Url)[] products =
    [
        ("bit BlazorUI", BitIconName.Puzzle, nameof(WorkbenchStrings.HomeProductBlazorUI), "https://blazorui.bitplatform.dev"),
        ("bit Blex", BitIconName.Database, nameof(WorkbenchStrings.HomeProductBlex), "https://blex.bitplatform.dev"),
        ("bit Bortable", BitIconName.DragObject, nameof(WorkbenchStrings.HomeProductBortable), "https://bortable.bitplatform.dev"),
        ("bit Bmotion", BitIconName.Flow, nameof(WorkbenchStrings.HomeProductBmotion), "https://bmotion.bitplatform.dev"),
        ("bit Butil", BitIconName.Toolbox, nameof(WorkbenchStrings.HomeProductButil), "https://butil.bitplatform.dev"),
        ("bit Boilerplate", BitIconName.Rocket, nameof(WorkbenchStrings.HomeProductBoilerplate), "https://bitplatform.dev/templates")
    ];

    private bool isPromptCopied;

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
