namespace Boilerplate.Server.Api.Features.Identity.Models;

public class UserToken : IdentityUserToken<Guid>
{
    public User? User { get; set; }
}
