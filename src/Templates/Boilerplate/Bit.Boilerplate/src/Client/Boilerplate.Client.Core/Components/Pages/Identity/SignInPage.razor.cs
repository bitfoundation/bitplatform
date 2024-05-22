//+:cnd:noEmit
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Client.Core.Services;
using Boilerplate.Shared.Dtos.Identity;
using Microsoft.Extensions.Options;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignInPage
{
    [AutoInject] private IIdentityController identityController = default!;

    private bool isSigningIn;
    private bool isSendingOtp;
    private bool requiresTwoFactor;
    private bool isSendingTfaToken;
    private SignInRequestDto model = new();

    private string? message;
    private BitSeverity messageSeverity;

    [Parameter, SupplyParameterFromQuery(Name = "redirect-url")]
    public string? RedirectUrlQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "otp")]
    public string? OtpQueryString { get; set; }


    [AutoInject] HttpClient httpClient { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            model.Email = EmailQueryString;
        }

        if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            model.PhoneNumber = PhoneNumberQueryString;
        }

        if (string.IsNullOrEmpty(OtpQueryString) is false)
        {
            model.Otp = OtpQueryString;

            if (InPrerenderSession is false &&
                (string.IsNullOrEmpty(model.Email) is false ||
                 string.IsNullOrEmpty(model.PhoneNumber) is false))
            {
                await DoSignIn();
            }
        }
    }

    private async Task DoSignIn()
    {
        if (isSigningIn) return;

        isSigningIn = true;
        message = null;

        try
        {
            if (requiresTwoFactor &&
                string.IsNullOrWhiteSpace(model.TwoFactorCode) &&
                string.IsNullOrWhiteSpace(model.TwoFactorRecoveryCode) &&
                string.IsNullOrWhiteSpace(model.TwoFactorToken)) return;

            requiresTwoFactor = await AuthenticationManager.SignIn(model, CurrentCancellationToken);

            //var url = $"api/Identity/SignIn/";
            //using var request = new HttpRequestMessage(HttpMethod.Post, url);
            //request.Content = JsonContent.Create(model, JsonSerializerOptions.GetTypeInfo<Boilerplate.Shared.Dtos.Identity.SignInRequestDto>());
            //using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, CurrentCancellationToken);
            //var result = await response.Content.ReadFromJsonAsync(JsonSerializerOptions.GetTypeInfo<Boilerplate.Shared.Dtos.Identity.SignInResponseDto>(), CurrentCancellationToken);
            //requiresTwoFactor = result.RequiresTwoFactor;

            if (requiresTwoFactor is false)
            {
                NavigationManager.NavigateTo(RedirectUrlQueryString ?? "/");
            }
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
        }
        finally
        {
            isSigningIn = false;
        }
    }

    private async Task SendOtp()
    {
        if (isSendingOtp) return;

        isSendingOtp = true;
        message = null;

        try
        {
            await identityController.SendOtp(model.ToIdentityRequest(), CurrentCancellationToken);

            message = Localizer[nameof(AppStrings.OtpSentMessage)];
            messageSeverity = BitSeverity.Success;
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
        }
        finally
        {
            isSendingOtp = false;
        }
    }

    private async Task SendTfaToken()
    {
        if (isSendingTfaToken) return;

        isSendingTfaToken = true;
        message = null;

        try
        {
            await identityController.SendTwoFactorToken(model, CurrentCancellationToken);

            message = Localizer[nameof(AppStrings.TfaTokenSentMessage)];
            messageSeverity = BitSeverity.Success;
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
        }
        finally
        {
            isSendingTfaToken = false;
        }
    }
}

