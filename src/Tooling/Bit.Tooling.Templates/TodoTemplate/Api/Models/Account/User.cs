namespace TodoTemplate.Api.Models.Account;

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

    public DateTimeOffset? ConfirmationEmailLastTimeSent { get; set; }
    
    public DateTimeOffset? ResetPasswordEmailLastTimeSent { get; set; }

    public string DisplayName => FullName ?? NormalizedUserName;
}
