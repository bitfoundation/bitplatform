﻿using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignIn
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    
    public string? EmailErrorMessage { get; set; }
    public string? PasswordErrorMessage { get; set; }

    public bool IsSignInButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType SignInMessageType { get; set; }
    public string? SignInMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    [Inject]
    public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public string? RedirectUrl { get; set; }

    private void CheckSignInButtonEnabled()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            IsSignInButtonEnabled = false;
            return;
        }
        IsSignInButtonEnabled = true;
    }

    private bool ValidateSignIn()
    {
        EmailErrorMessage = string.IsNullOrEmpty(Email)
            ? "Please enter your email."
            : null;

        PasswordErrorMessage = string.IsNullOrEmpty(Password)
            ? "Please enter your password."
            : null;

        return string.IsNullOrEmpty(EmailErrorMessage) is not false &&
               string.IsNullOrEmpty(PasswordErrorMessage) is not false;
    }

    private async Task DoSignIn()
    {
        IsLoading = true;

        if (ValidateSignIn() is false)
        {
            IsLoading = false;
            return;
        }

        try
        {
            await TodoTemplateAuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email, 
                Password = Password
            });

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (Exception e)
        {
            SignInMessageType = BitMessageBarType.Error;

            SignInMessage = e is KnownException
                ? ErrorStrings.ResourceManager.GetString(e.Message)
                : ErrorStrings.UnknownException;
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

