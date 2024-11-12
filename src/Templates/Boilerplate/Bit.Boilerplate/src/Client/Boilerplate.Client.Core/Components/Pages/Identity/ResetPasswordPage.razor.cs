using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ResetPasswordPage
{
    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "token")]
    public string? TokenQueryString { get; set; }


    [AutoInject] IIdentityController identityController = default!;


    private bool isWaiting;
    private bool showEmail;
    private bool showPhone;
    private bool isTokenEntered;
    private bool isPasswordChanged;
    private ResetPasswordRequestDto model = new();

    private const string EmailKey = nameof(EmailKey);
    private const string PhoneKey = nameof(PhoneKey);

    private string selectedKey = EmailKey;


    protected override async Task OnInitAsync()
    {
        model.Email = EmailQueryString;
        model.PhoneNumber = PhoneNumberQueryString;
        model.Token = TokenQueryString;

        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            showEmail = true;
        }
        else if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            showPhone = true;
        }
        else
        {
            showEmail = showPhone = true;
        }

        HandleContinue();

        await base.OnInitAsync();
    }

    private void OnSelectedKeyChanged(string key)
    {
        selectedKey = key;

        if (key == EmailKey)
        {
            model.PhoneNumber = null;
        }

        if (key == PhoneKey)
        {
            model.Email = null;
        }
    }

    private void HandleContinue()
    {
        if (string.IsNullOrEmpty(model.Token)) return;
        if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.PhoneNumber)) return;

        isTokenEntered = true;
    }

    private async Task Submit()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            await identityController.ResetPassword(model, CurrentCancellationToken);

            isPasswordChanged = true;
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

    private async Task Resend()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            var resendModel = new SendResetPasswordTokenRequestDto { Email = model.Email, PhoneNumber = model.PhoneNumber };

            await identityController.SendResetPasswordToken(resendModel, CurrentCancellationToken);

            SnackBarService.Success(Localizer[nameof(AppStrings.SuccessfulSendTokenMessage)]);
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
}
