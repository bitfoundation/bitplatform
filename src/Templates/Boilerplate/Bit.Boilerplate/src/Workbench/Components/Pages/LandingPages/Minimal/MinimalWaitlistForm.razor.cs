namespace Boilerplate.Workbench.Components.Pages.LandingPages.Minimal;

/// <summary>The waitlist form. Joining goes through the scenario state, so the data state switches reach its waiting and failing states.</summary>
public partial class MinimalWaitlistForm
{
    private const int SamplePosition = 1204;

    private static readonly (string Platform, string TextKey)[] platforms =
    [
        ("ios", nameof(WorkbenchStrings.LandingPlatformIos)),
        ("android", nameof(WorkbenchStrings.LandingPlatformAndroid))
    ];

    [AutoInject] private MinimalLandingStore store = default!;

    [Parameter] public string? SubmitText { get; set; }

    [Parameter] public string? Id { get; set; }

    [Parameter] public string? Class { get; set; }

    private bool isSubmitting;
    private string? error;

    protected override void OnInitialized()
    {
        Subscribe(store);

        base.OnInitialized();
    }

    private async Task Submit()
    {
        if (isSubmitting)
            return;

        error = null;

        if (string.IsNullOrWhiteSpace(store.Email) || store.Email.Contains('@') is false)
        {
            error = Localizer[nameof(WorkbenchStrings.LandingWaitlistInvalidEmail)];
            return;
        }

        isSubmitting = true;

        try
        {
            store.Join(await ScenarioState.Load("landing-waitlist", () => SamplePosition, () => SamplePosition, CurrentCancellationToken));
        }
        catch (WorkbenchSimulatedException)
        {
            error = AppLocalizer[nameof(AppStrings.SomethingWentWrong)];
        }
        catch (OperationCanceledException)
        {
            // The page went away while it waited.
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
