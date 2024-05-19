using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ConfirmPage
{
    [AutoInject] private IIdentityController identityController = default!;

    private bool isWaiting;
    private bool isEmailConfirmed;
    private bool isPhoneConfirmed;
    private bool showEmailConfirmation;
    private bool showPhoneConfirmation;
    private string? errorMessage;
    private readonly ConfirmEmailRequestDto emailModel = new();
    private readonly ConfirmPhoneRequestDto phoneModel = new();


    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "emailToken")]
    public string? EmailTokenQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneToken")]
    public string? PhoneTokenQueryString { get; set; }


    protected override async Task OnInitAsync()
    {
        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            emailModel.Email = EmailQueryString;
            showEmailConfirmation = true;

            if (string.IsNullOrEmpty(EmailTokenQueryString) is false)
            {
                emailModel.Token = EmailTokenQueryString;
                await ConfirmEmail();
            }
        }

        if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            phoneModel.PhoneNumber = PhoneNumberQueryString;
            showPhoneConfirmation = true;

            if (string.IsNullOrEmpty(PhoneTokenQueryString) is false)
            {
                phoneModel.Token = PhoneTokenQueryString;
                await ConfirmPhone();
            }
        }

        if (string.IsNullOrEmpty(EmailQueryString) && string.IsNullOrEmpty(PhoneNumberQueryString))
        {
            showEmailConfirmation = showPhoneConfirmation = true;
        }

        await base.OnInitAsync();
    }

    private async Task ConfirmEmail()
    {
        if (isWaiting || string.IsNullOrEmpty(emailModel.Email) || string.IsNullOrEmpty(emailModel.Token)) return;

        await WrapRequest(async () =>
        {
            await identityController.ConfirmEmail(new() { Email = emailModel.Email, Token = emailModel.Token }, CurrentCancellationToken);

            isEmailConfirmed = true;
        });
    }

    private async Task ResendEmailToken()
    {
        if (isWaiting || string.IsNullOrEmpty(emailModel.Email)) return;

        await WrapRequest(async () =>
        {
            await identityController.SendConfirmEmailToken(new() { Email = emailModel.Email }, CurrentCancellationToken);
        });
    }

    private async Task ConfirmPhone()
    {
        if (isWaiting || string.IsNullOrEmpty(phoneModel.PhoneNumber) || string.IsNullOrEmpty(phoneModel.Token)) return;

        await WrapRequest(async () =>
        {
            var request = new ConfirmPhoneRequestDto
            {

            };

            await identityController.ConfirmPhone(new() { PhoneNumber = phoneModel.PhoneNumber, Token = phoneModel.Token }, CurrentCancellationToken);

            isPhoneConfirmed = true;
        });
    }

    private async Task ResendPhoneToken()
    {
        if (isWaiting || string.IsNullOrEmpty(phoneModel.PhoneNumber)) return;

        await WrapRequest(async () =>
        {
            await identityController.SendConfirmPhoneToken(new() { PhoneNumber = phoneModel.PhoneNumber }, CurrentCancellationToken);
        });
    }

    private async Task WrapRequest(Func<Task> action)
    {
        isWaiting = true;
        errorMessage = null;

        try
        {
            await action();
        }
        catch (KnownException e)
        {
            errorMessage = e.Message;
        }
        finally
        {
            isWaiting = false;
        }
    }
}
