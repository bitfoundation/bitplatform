﻿//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignUpPage
{
    private bool isWaiting;
    private string? signUpErrorMessage;
    private ElementReference messageRef = default!;
    private readonly SignUpRequestDto signUpModel = new();


    [AutoInject] private IIdentityController identityController = default!;


    private async Task DoSignUp()
    {
        if (isWaiting) return;

        //#if (captcha == "reCaptcha")
        var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
        {
            signUpErrorMessage = Localizer[nameof(AppStrings.InvalidGoogleRecaptchaChallenge)];
            await messageRef.ScrollIntoView();
            return;
        }

        signUpModel.GoogleRecaptchaResponse = googleRecaptchaResponse;
        //#endif

        isWaiting = true;
        signUpErrorMessage = null;
        StateHasChanged();

        try
        {
            signUpModel.UserName = Guid.NewGuid().ToString(); // You can also bind the UserName property to an input

            await identityController.SignUp(signUpModel, CurrentCancellationToken);

            var queryParams = new Dictionary<string, object?>();
            if (string.IsNullOrEmpty(signUpModel.Email) is false)
            {
                queryParams.Add("email", signUpModel.Email);
            }
            if (string.IsNullOrEmpty(signUpModel.PhoneNumber) is false)
            {
                queryParams.Add("phoneNumber", signUpModel.PhoneNumber);
            }
            var confirmUrl = NavigationManager.GetUriWithQueryParameters("confirm", queryParams);
            NavigationManager.NavigateTo(confirmUrl);
        }
        catch (KnownException e)
        {
            signUpErrorMessage = e is ResourceValidationException re
                                    ? string.Join(" ", re.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message))
                                    : e.Message;
            await messageRef.ScrollIntoView();

            //#if (captcha == "reCaptcha")
            await JSRuntime.GoogleRecaptchaReset();
            //#endif
        }
        finally
        {
            isWaiting = false;
        }
    }
}
