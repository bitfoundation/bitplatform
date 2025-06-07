using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Services.Identity;

public partial class AppUserClaimsPrincipalFactory(UserClaimsService userClaimsService, UserManager<User> userManager, RoleManager<Role> roleManager, IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<User, Role>(userManager, roleManager, optionsAccessor)
{
    /// <summary>
    /// These claims will be included in both the access and refresh tokens only if the successful sign-in happens during the current HTTP request lifecycle.
    /// </summary>
    public List<Claim> SessionClaims { get; set; } = [];

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var result = await GenerateClaims(user);

        foreach (var sessionClaim in SessionClaims)
        {
            if (result.HasClaim(sessionClaim.Type, sessionClaim.Value) is false)
                result.AddClaim(sessionClaim);
        }

        return result;
    }

    /// <summary>
    /// aspnetcore identity's code to retrieve claims is not performant enough,
    /// because it doesn't have access to navigation properties and has to query the database for user claims, user roles and role claims separately,
    /// while we use <see cref="UserClaimsService.GetClaims(Guid,CancellationToken)"/> to retrieve all claims in a single query.
    /// The original code borrowed from https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/UserClaimsPrincipalFactory.cs#L71
    /// </summary>
    private async Task<ClaimsIdentity> GenerateClaims(User user)
    {
        var userId = user.Id.ToString();
        var userName = user.UserName;
        var id = new ClaimsIdentity("Identity.Application", // REVIEW: Used to match Application scheme
            Options.ClaimsIdentity.UserNameClaimType,
            Options.ClaimsIdentity.RoleClaimType);
        id.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
        id.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, userName!));
        var email = user.Email;
        if (string.IsNullOrEmpty(email) is false)
        {
            id.AddClaim(new Claim(Options.ClaimsIdentity.EmailClaimType, email));
        }
        id.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, user.SecurityStamp!));

        foreach (var claim in await userClaimsService.GetClaims(user.Id, default))
        {
            if (id.HasClaim(claim.Type, claim.Value) is false)
                id.AddClaim(new(claim.Type, claim.Value));
        }

        return id;
    }
}
