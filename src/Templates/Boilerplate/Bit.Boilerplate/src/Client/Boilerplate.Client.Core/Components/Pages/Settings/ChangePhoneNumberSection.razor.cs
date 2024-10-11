using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class ChangePhoneNumberSection
{
    private bool isWaiting;
    private bool showConfirmation;
    private BitSnackBar snackbarRef = default!;
    private bool isPhoneNumberUnavailable = true;
    private readonly SendPhoneTokenRequestDto sendModel = new();
    private readonly ChangePhoneNumberRequestDto changeModel = new();


    [AutoInject] private IUserController userController = default!;


    [Parameter] public string? PhoneNumber { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneToken")]
    public string? PhoneNumberTokenQueryString { get; set; }


    protected override async Task OnInitAsync()
    {
        if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            showConfirmation = true;
            isPhoneNumberUnavailable = false;
            changeModel.PhoneNumber = PhoneNumberQueryString;

            if (string.IsNullOrEmpty(PhoneNumberTokenQueryString) is false)
            {
                changeModel.Token = PhoneNumberTokenQueryString;

                if (InPrerenderSession is false)
                {
                    await ChangePhoneNumber();
                }
            }
        }

        await base.OnInitAsync();
    }

    private async Task SendToken()
    {
        if (isWaiting || sendModel.PhoneNumber == PhoneNumber) return;

        isWaiting = true;

        try
        {
            await userController.SendChangePhoneNumberToken(sendModel, CurrentCancellationToken);

            showConfirmation = true;
            isPhoneNumberUnavailable = false;
            changeModel.PhoneNumber = sendModel.PhoneNumber;

            await snackbarRef.Success(Localizer[nameof(AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)]);
        }
        catch (KnownException e)
        {
            await snackbarRef.Error(e.Message);
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

            NavigationManager.NavigateTo(Urls.SettingsPage);
        }
        catch (KnownException e)
        {
            await snackbarRef.Error(e.Message);
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
