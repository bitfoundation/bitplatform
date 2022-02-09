namespace TodoTemplate.Api.Models.Account;

public class User : IdentityUser<int>
{
    public string? FullName { get; set; }
    public Gender? Gender { get; set; }
    public DateTimeOffset? BirthDate { get; set; }
}
