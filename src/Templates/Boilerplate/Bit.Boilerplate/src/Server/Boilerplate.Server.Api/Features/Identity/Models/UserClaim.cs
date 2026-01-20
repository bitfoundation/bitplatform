namespace Boilerplate.Server.Api.Features.Identity.Models;

public class UserClaim : IdentityUserClaim<Guid>
{
    public User? User { get; set; }
}
