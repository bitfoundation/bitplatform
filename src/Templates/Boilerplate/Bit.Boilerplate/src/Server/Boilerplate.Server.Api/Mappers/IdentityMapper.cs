using Riok.Mapperly.Abstractions;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class IdentityMapper
{
    public static partial UserDto Map(this User source);
    public static partial void Patch(this EditUserDto source, User destination);

    [MapPropertyFromSource(nameof(UserSessionDto.RenewedOn), Use = nameof(MapRenewedOn))]
    public static partial IQueryable<UserSessionDto> Project(this IQueryable<UserSession> source);

    [MapPropertyFromSource(nameof(UserSessionDto.RenewedOn), Use = nameof(MapRenewedOn))]
    public static partial UserSessionDto Map(this UserSession source);

    [UserMapping]
    private static DateTimeOffset MapRenewedOn(UserSession us) => us.RenewedOn ?? us.StartedOn;
}
