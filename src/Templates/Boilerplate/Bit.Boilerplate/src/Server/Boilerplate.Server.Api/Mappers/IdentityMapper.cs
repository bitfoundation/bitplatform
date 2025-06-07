using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Dtos.Identity;
using Riok.Mapperly.Abstractions;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class IdentityMapper
{
    public static partial UserDto Map(this User source);
    public static partial User Map(this UserDto source);
    public static partial void Patch(this UserDto source, User dest);
    public static partial void Patch(this EditUserRequestDto source, User destination);

    [MapPropertyFromSource(nameof(UserSessionDto.RenewedOn), Use = nameof(MapRenewedOn))]
    public static partial IQueryable<UserSessionDto> Project(this IQueryable<UserSession> source);

    [MapPropertyFromSource(nameof(UserSessionDto.RenewedOn), Use = nameof(MapRenewedOn))]
    public static partial UserSessionDto Map(this UserSession source);

    public static partial RoleDto Map(this Role source);
    public static partial Role Map(this RoleDto source);
    public static partial void Patch(this RoleDto source, Role dest);
    public static partial IQueryable<RoleDto> Project(this IQueryable<Role> query);
    public static partial IQueryable<UserDto> Project(this IQueryable<User> query);

    public static partial ClaimDto Map(this RoleClaim source);
    public static partial IQueryable<ClaimDto> Project(this IQueryable<RoleClaim> query);

    [UserMapping]
    private static long MapRenewedOn(UserSession us) => us.RenewedOn ?? us.StartedOn;
}
