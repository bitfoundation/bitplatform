﻿namespace Boilerplate.Server.Api.Models.Identity;

public class User : IdentityUser<int>
{
    [PersonalData]
    public string? FullName { get; set; }

    [PersonalData]
    public Gender? Gender { get; set; }

    [PersonalData]
    public DateTimeOffset? BirthDate { get; set; }

    [PersonalData]
    public string? ProfileImageName { get; set; }

    public DateTimeOffset? ConfirmationEmailRequestedOn { get; set; }

    public DateTimeOffset? ResetPasswordEmailRequestedOn { get; set; }

    public string? DisplayName => FullName ?? NormalizedUserName;
}
