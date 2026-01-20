namespace Boilerplate.Server.Api.Features.Identity.Models;

public partial class Role : IdentityRole<Guid>
{
    public List<UserRole> Users { get; set; } = [];
    public List<RoleClaim> Claims { get; set; } = [];
}

