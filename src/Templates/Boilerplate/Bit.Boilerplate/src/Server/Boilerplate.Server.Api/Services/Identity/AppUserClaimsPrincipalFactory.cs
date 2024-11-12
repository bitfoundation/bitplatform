using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Services.Identity;

public partial class AppUserClaimsPrincipalFactory(UserManager<User> userManager, RoleManager<Role> roleManager, IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<User, Role>(userManager, roleManager, optionsAccessor)
{
    /// <summary>
    /// These claims will be included in both the access and refresh tokens only if the successful sign-in happens during the current HTTP request lifecycle.
    /// </summary>
    public List<Claim> SessionClaims { get; set; } = [];

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var result = await base.GenerateClaimsAsync(user);

        result.AddClaims(SessionClaims);

        return result;
    }
}
