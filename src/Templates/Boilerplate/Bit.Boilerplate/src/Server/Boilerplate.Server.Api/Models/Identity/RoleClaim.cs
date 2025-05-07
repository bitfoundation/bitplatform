namespace Boilerplate.Server.Api.Models.Identity;

public class RoleClaim : IdentityRoleClaim<Guid>
{
    public Role? Role { get; set; }
}
