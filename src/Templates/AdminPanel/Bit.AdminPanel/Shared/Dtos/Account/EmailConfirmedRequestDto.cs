﻿namespace AdminPanel.Shared.Dtos.Account;

public class EmailConfirmedRequestDto
{
    [Required]
    public string? Email { get; set; }
}
