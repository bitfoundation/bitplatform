namespace Boilerplate.Server.Api.Features.Identity.Models;

public class RoleClaim : IdentityRoleClaim<Guid>
{
    public Role? Role { get; set; }
}
