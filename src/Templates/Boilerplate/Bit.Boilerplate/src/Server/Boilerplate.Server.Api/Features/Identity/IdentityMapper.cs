//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Server.Api.Features.Identity;

/// <summary>
/// More info at src/Server/Boilerplate.Server.Api/Features/Mappers.md
/// </summary>
[Mapper]
public static partial class IdentityMapper
{
    public static partial RoleDto Map(this Role source);
    public static partial Role Map(this RoleDto source);
    public static partial void Patch(this RoleDto source, Role destination);
    public static partial IQueryable<RoleDto> Project(this IQueryable<Role> query);



    [MapProperty(nameof(@User.ConcurrencyStamp), nameof(@UserDto.Version))]
    public static partial UserDto Map(this User source);
    public static partial void Patch(this EditUserRequestDto source, User destination);
    public static partial IQueryable<UserDto> Project(this IQueryable<User> query);



    // The OAuth half lives in its own table, so the dto's flat shape is assembled here rather than on the entity.
    [MapProperty([nameof(@UserSession.OAuthGrant), nameof(@OAuthGrant.ClientId)], [nameof(@UserSessionDto.OAuthClientId)])]
    [MapProperty([nameof(@UserSession.OAuthGrant), nameof(@OAuthGrant.ClientName)], [nameof(@UserSessionDto.OAuthClientName)])]
    [MapProperty([nameof(@UserSession.OAuthGrant), nameof(@OAuthGrant.Scope)], [nameof(@UserSessionDto.OAuthScope)])]
    [MapPropertyFromSource(nameof(@UserSessionDto.RenewedOn), Use = nameof(MapRenewedOn))]
    public static partial UserSessionDto Map(this UserSession source);
    public static partial IQueryable<UserSessionDto> Project(this IQueryable<UserSession> source);

    [UserMapping]
    private static long MapRenewedOn(UserSession us) => us.RenewedOn ?? us.StartedOn;



    public static partial ClaimDto Map(this RoleClaim source);
    public static partial IQueryable<ClaimDto> Project(this IQueryable<RoleClaim> query);
}
