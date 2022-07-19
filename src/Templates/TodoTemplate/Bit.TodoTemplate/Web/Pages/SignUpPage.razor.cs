﻿using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignUpPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    [AutoInject] private AppAuthenticationStateProvider authStateProvider = default!;

    public SignUpRequestDto SignUpModel { get; set; } = new();

    public bool IsSignedUp { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType SignUpMessageType { get; set; }
    public string? SignUpMessage { get; set; }

    private bool IsSubmitButtonEnabled =>
        SignUpModel.UserName.HasValue()
        && SignUpModel.Password.HasValue()
        && SignUpModel.IsAcceptPrivacy
        && IsLoading is false;

    private async Task DoSignUp()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignUpMessage = null;

        try
        {
            SignUpModel.Email = SignUpModel.UserName;

            await httpClient.PostAsJsonAsync("Auth/SignUp", SignUpModel, AppJsonContext.Default.SignUpRequestDto);

            IsSignedUp = true;
        }
        catch (ResourceValidationException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages)
                .Select(e => ErrorStrings.ResourceManager.Translate(e, SignUpModel.UserName!)));
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = ErrorStrings.ResourceManager.Translate(e.Message, SignUpModel.UserName);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResendLink()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignUpMessage = null;

        try
        {
            await httpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new()
            {
                Email = SignUpModel.Email
            }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            SignUpMessageType = BitMessageBarType.Success;
            SignUpMessage = AuthStrings.ResendConfirmationLinkMessage;
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = ErrorStrings.ResourceManager.Translate(e.Message, SignUpModel.Email);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (await authStateProvider.IsUserAuthenticated())
            {
                navigationManager.NavigateTo("/");
            }
        }
    }
}
