//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignUpPage
{
    [AutoInject] private IIdentityController identityCtrl = default!;

    private bool isWaiting;
    private bool isConfirmed;
    private string? signUpErrorMessage;
    private readonly ConfirmEmailRequestDto confirmEmailModel = new();
    private readonly ConfirmPhoneRequestDto confirmPhoneModel = new();
    private readonly SignUpRequestDto signUpModel = new()
    {
        UserName = Guid.NewGuid().ToString() /* You can also bind the UserName property to an input */
    };


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
            confirmEmailModel.Email = EmailQueryString;

            if (string.IsNullOrEmpty(EmailTokenQueryString) is false)
            {
                confirmEmailModel.Token = EmailTokenQueryString;
                await ConfirmEmail();
            }
        }

        if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            confirmPhoneModel.PhoneNumber = PhoneNumberQueryString;

            if (string.IsNullOrEmpty(PhoneTokenQueryString) is false)
            {
                confirmPhoneModel.Token = PhoneTokenQueryString;
                await ConfirmPhone();
            }
        }

        await base.OnInitAsync();
    }


    private async Task DoSignUp()
    {
        if (isWaiting) return;

        //#if (captcha == "reCaptcha")
        var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
        {
            signUpErrorMessage = Localizer[nameof(AppStrings.InvalidGoogleRecaptchaChallenge)];
            return;
        }

        signUpModel.GoogleRecaptchaResponse = googleRecaptchaResponse;
        //#endif

        isWaiting = true;
        signUpErrorMessage = null;
        StateHasChanged();

        try
        {
            await identityCtrl.SignUp(signUpModel, CurrentCancellationToken);

            var queryParams = new Dictionary<string, object?>();
            if (string.IsNullOrEmpty(signUpModel.Email) is false)
            {
                queryParams.Add("email", signUpModel.Email);
            }
            if (string.IsNullOrEmpty(signUpModel.PhoneNumber) is false)
            {
                queryParams.Add("phoneNumber", signUpModel.PhoneNumber);
            }
            var confirmUrl = NavigationManager.GetUriWithQueryParameters(queryParams);
            NavigationManager.NavigateTo(confirmUrl);
        }
        catch (ResourceValidationException e)
        {
            signUpErrorMessage = string.Join(" ", e.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message));
        }
        catch (KnownException e)
        {
            signUpErrorMessage = e.Message;
        }
        finally
        {
            //#if (captcha == "reCaptcha")
            await JSRuntime.GoogleRecaptchaReset();
            //#endif
            isWaiting = false;
        }
    }

    private async Task ConfirmEmail()
    {
        if (isWaiting || string.IsNullOrEmpty(confirmEmailModel.Email) || string.IsNullOrEmpty(confirmEmailModel.Token)) return;

        await WrapRequest(async () =>
        {
            var request = new ConfirmEmailRequestDto
            {
                Email = confirmEmailModel.Email,
                Token = confirmEmailModel.Token
            };

            await identityCtrl.ConfirmEmail(request, CurrentCancellationToken);

            isConfirmed = true;
        });
    }

    private async Task ResendEmailToken()
    {
        if (isWaiting || string.IsNullOrEmpty(confirmEmailModel.Email)) return;

        await WrapRequest(async () =>
        {
            await identityCtrl.SendConfirmEmailToken(new() { Email = confirmEmailModel.Email }, CurrentCancellationToken);
        });
    }

    private async Task ConfirmPhone()
    {
        if (isWaiting || string.IsNullOrEmpty(confirmPhoneModel.PhoneNumber) || string.IsNullOrEmpty(confirmPhoneModel.Token)) return;

        await WrapRequest(async () =>
        {
            var request = new ConfirmPhoneRequestDto
            {
                PhoneNumber = confirmPhoneModel.PhoneNumber,
                Token = confirmPhoneModel.Token
            };

            await identityCtrl.ConfirmPhone(request, CurrentCancellationToken);

            isConfirmed = true;
        });
    }

    private async Task ResendPhoneToken()
    {
        if (isWaiting || string.IsNullOrEmpty(confirmPhoneModel.PhoneNumber)) return;

        await WrapRequest(async () =>
        {
            await identityCtrl.SendConfirmPhoneToken(new() { PhoneNumber = confirmPhoneModel.PhoneNumber }, CurrentCancellationToken);
        });
    }

    private async Task WrapRequest(Func<Task> action)
    {
        isWaiting = true;
        signUpErrorMessage = null;
        StateHasChanged();

        try
        {
            await action();
        }
        catch (KnownException e)
        {
            signUpErrorMessage = e.Message;
        }
        finally
        {
            isWaiting = false;
        }
    }
}
