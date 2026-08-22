namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class ChangeEmailTab
{
    [Parameter] public string? Email { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "emailToken")]
    public string? EmailTokenQueryString { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }


    [AutoInject] private IUserController userController = default!;


    private bool isWaiting;
    private bool showConfirmation;
    private bool isEmailUnavailable = true;
    private readonly ChangeEmailRequestDto changeModel = new();
    private readonly SendEmailTokenRequestDto sendModel = new();


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            showConfirmation = true;
            isEmailUnavailable = false;
            changeModel.Email = EmailQueryString;
            sendModel.Email = EmailQueryString;

            if (string.IsNullOrEmpty(EmailTokenQueryString) is false)
            {
                changeModel.Token = EmailTokenQueryString;

                if (InPrerenderSession is false)
                {
                    await ChangeEmail();
                }
            }
        }
    }


    private async Task SendToken()
    {
        if (isWaiting) return;

        if (sendModel.Email == Email)
        {
            SnackBarService.Error(Localizer["That is already your email address. Enter a different one."]);
            return;
        }

        // Proving the NEW address (the code sent below) is only half of it - the server also requires the user to prove
        // she still holds the CURRENT one, by quoting a code sent to it. That is what elevated access is.
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        isWaiting = true;

        try
        {
            await userController.SendChangeEmailToken(sendModel, CurrentCancellationToken);

            showConfirmation = true;
            isEmailUnavailable = false;
            changeModel.Email = sendModel.Email;

            SnackBarService.Success(Localizer[nameof(AppStrings.SuccessfulSendChangeEmailTokenMessage)]);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task ChangeEmail()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            await userController.ChangeEmail(changeModel, CurrentCancellationToken);

            if (CurrentUser is not null)
            {
                CurrentUser.Email = changeModel.Email;
                PubSubService.Publish(ClientAppMessages.PROFILE_UPDATED, CurrentUser);
            }

            SnackBarService.Warning(Localizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);

            showConfirmation = false;
            isEmailUnavailable = true;
            sendModel.Email = changeModel.Email = changeModel.Token = null;
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task GoBack()
    {
        sendModel.Email = null;
        changeModel.Email = null;

        showConfirmation = false;
        isEmailUnavailable = true;
    }
}
