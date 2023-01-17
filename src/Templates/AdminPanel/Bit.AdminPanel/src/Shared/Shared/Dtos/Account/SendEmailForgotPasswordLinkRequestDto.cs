﻿
namespace AdminPanel.Shared.Dtos.Account;

[DtoResourceType(typeof(AppStrings))]
public class SendResetPasswordEmailRequestDto
{
    [Required(ErrorMessageResourceName = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }
}
