﻿namespace AdminPanel.Shared.Dtos.Account;

public class SignInResponseDto
{
    public string? AccessToken { get; set; }

    public long ExpiresIn { get; set; }
}
