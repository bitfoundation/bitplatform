namespace Boilerplate.Server.Api.Models.Identity;

public class UserRole : IdentityUserRole<Guid>
{
    public User? User { get; set; }

    public Role? Role { get; set; }
}
