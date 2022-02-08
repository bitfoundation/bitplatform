﻿using TodoTemplate.Shared.Dtos.Account;
using TodoTemplate.Shared.Enums;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public string? Email { get; set; }

    public string? EmailError { get; set; }

    public string? Password { get; set; }

    public string? PasswordError { get; set; }

    public bool IsAcceptPrivacy { get; set; }

    public bool HasMessageBar { get; set; }

    public bool IsSuccessMessageBar { get; set; }

    public string? MessageBarText { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    private async Task OnClickSignUp()
    {
        EmailError = string.IsNullOrEmpty(Email) ? "Please enter your email" : null;

        PasswordError = string.IsNullOrEmpty(Password) ? "Please enter your password" : null;

        if (EmailError is not null || PasswordError is not null) return;

        try
        {
            await HttpClient.PostAsync("User/Create", JsonContent.Create(new UserDto
            {
                UserName = Email,
                Email = Email,
                Password = Password
            }));

            await TodoTemplateAuthenticationService.SignIn(new RequestTokenDto
            {
                UserName = Email,
                Password = Password
            });

            IsSuccessMessageBar = true;
            MessageBarText = "Sign-up successfully";
        }
        catch (Exception e)
        {
            MessageBarText = e.Message;
        }

        HasMessageBar = true;
    }
}

