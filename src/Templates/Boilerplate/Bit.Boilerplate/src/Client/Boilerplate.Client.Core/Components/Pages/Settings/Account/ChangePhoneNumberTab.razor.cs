namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class ChangePhoneNumberTab
{
    [Parameter] public string? PhoneNumber { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneToken")]
    public string? PhoneNumberTokenQueryString { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }


    [AutoInject] private IUserController userController = default!;


    private bool isWaiting;
    private bool showConfirmation;
    private bool isPhoneNumberUnavailable = true;
    private readonly SendPhoneTokenRequestDto sendModel = new();
    private readonly ChangePhoneNumberRequestDto changeModel = new();


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            showConfirmation = true;
            isPhoneNumberUnavailable = false;
            changeModel.PhoneNumber = PhoneNumberQueryString;
            sendModel.PhoneNumber = PhoneNumberQueryString;

            if (string.IsNullOrEmpty(PhoneNumberTokenQueryString) is false)
            {
                changeModel.Token = PhoneNumberTokenQueryString;

                if (InPrerenderSession is false)
                {
                    await ChangePhoneNumber();
                }
            }
        }
    }


    private async Task SendToken()
    {
        if (isWaiting) return;

        if (sendModel.PhoneNumber == PhoneNumber)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.SamePhoneNumberErrorMessage)]);
            return;
        }

        // Proving the NEW number (the code sent below) is only half of it - the server also requires the user to prove
        // she still holds the CURRENT identifiers, by quoting a code sent to them. That is what elevated access is.
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        isWaiting = true;

        try
        {
            await userController.SendChangePhoneNumberToken(sendModel, CurrentCancellationToken);

            showConfirmation = true;
            isPhoneNumberUnavailable = false;
            changeModel.PhoneNumber = sendModel.PhoneNumber;

            SnackBarService.Success(Localizer[nameof(AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)]);
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

    private async Task ChangePhoneNumber()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            await userController.ChangePhoneNumber(changeModel, CurrentCancellationToken);

            // Changing the phone number regenerates the security stamp on the server, which signs the user out of every
            // device (including this one) on the next token refresh. Refresh the cached user so the UI reflects the new
            // number, then warn about the imminent sign-out. A soft navigation (instead of a forced reload) is used here
            // so the warning snackbar survives to be seen by the user.
            SnackBarService.Warning(Localizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);

            if (CurrentUser is not null)
            {
                CurrentUser.PhoneNumber = changeModel.PhoneNumber;
                PubSubService.Publish(ClientAppMessages.PROFILE_UPDATED, CurrentUser);
            }

            showConfirmation = false;
            isPhoneNumberUnavailable = true;
            sendModel.PhoneNumber = changeModel.PhoneNumber = changeModel.Token = null;
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
        sendModel.PhoneNumber = null;
        changeModel.PhoneNumber = null;

        showConfirmation = false;
        isPhoneNumberUnavailable = true;
    }
}
