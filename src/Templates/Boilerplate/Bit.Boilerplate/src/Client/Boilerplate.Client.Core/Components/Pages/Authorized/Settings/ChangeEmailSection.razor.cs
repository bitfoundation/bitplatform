using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class ChangeEmailSection
{
    [Parameter] public string? Email { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "emailToken")]
    public string? EmailTokenQueryString { get; set; }


    [AutoInject] private IUserController userController = default!;


    private bool isWaiting;
    private bool showConfirmation;
    private bool isEmailUnavailable = true;
    private readonly ChangeEmailRequestDto changeModel = new();
    private readonly SendEmailTokenRequestDto sendModel = new();


    protected override async Task OnInitAsync()
    {
        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            showConfirmation = true;
            isEmailUnavailable = false;
            changeModel.Email = EmailQueryString;

            if (string.IsNullOrEmpty(EmailTokenQueryString) is false)
            {
                changeModel.Token = EmailTokenQueryString;

                if (InPrerenderSession is false)
                {
                    await ChangeEmail();
                }
            }
        }

        await base.OnInitAsync();
    }


    private async Task SendToken()
    {
        if (isWaiting || sendModel.Email == Email) return;

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

            NavigationManager.NavigateTo($"{Urls.SettingsPage}/{Urls.SettingsSections.Account}", forceLoad: true);
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
