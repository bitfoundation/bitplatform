﻿using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignIn
{
    public string? Email { get; set; }

    public string? EmailError { get; set; }

    public string? Password { get; set; }

    public string? PasswordError { get; set; }

    public bool HasMessageBar { get; set; }

    public bool IsSuccessMessageBar { get; set; }

    public string? MessageBarText { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    [Inject]
    public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    async Task DoSignIn()
    {
        EmailError = string.IsNullOrEmpty(Email) ? "Please enter your email" : null;
        PasswordError = string.IsNullOrEmpty(Password) ? "Please enter your password" : null;

        if (EmailError is not null || PasswordError is not null)
            return;

        try
        {
            await TodoTemplateAuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = Password
            });

            IsSuccessMessageBar = true;
            MessageBarText = "Sign-up successfully";

            if (NavigationManager.Uri.EndsWith("/sign-in", StringComparison.InvariantCultureIgnoreCase))
                NavigationManager.NavigateTo("/");
        }
        catch (Exception e)
        {
            IsSuccessMessageBar = false;

            if (e is KnownException)
            {
                MessageBarText = ErrorStrings.ResourceManager.GetString(e.Message);
            }
            else
            {
                MessageBarText = ErrorStrings.UnknownException;
                throw;
            }
        }

        HasMessageBar = true;
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

