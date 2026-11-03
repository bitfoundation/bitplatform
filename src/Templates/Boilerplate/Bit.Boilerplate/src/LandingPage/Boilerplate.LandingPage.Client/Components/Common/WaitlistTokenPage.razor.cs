namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class WaitlistTokenPage
{
    [AutoInject] private IWaitlistClient waitlistClient = default!;

    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "token")] public string? QueryToken { get; set; }

    private bool isConfirm;
    private bool isSubmitting;
    private bool isDone;
    private int position;
    private string? token;
    private string? error;

    private string Title => isConfirm ? Localizer[nameof(LandingStrings.WaitlistConfirmTitle)] : Localizer[nameof(LandingStrings.WaitlistUnsubscribeTitle)];

    protected override void OnParametersSet()
    {
        isConfirm = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).AbsolutePath.EndsWith("/confirm", StringComparison.OrdinalIgnoreCase);
        token = QueryToken;

        base.OnParametersSet();
    }

    private async Task Submit()
    {
        if (string.IsNullOrEmpty(token))
            return;

        isSubmitting = true;
        error = null;

        try
        {
            if (isConfirm)
            {
                position = (await waitlistClient.ConfirmAsync(token, CancellationToken.None)).Position;
            }
            else
            {
                await waitlistClient.UnsubscribeAsync(token, CancellationToken.None);
            }

            isDone = true;
        }
        catch (WaitlistException)
        {
            error = Localizer[nameof(LandingStrings.WaitlistLinkExpired)];
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
