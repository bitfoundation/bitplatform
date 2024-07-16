using Humanizer;
using Riok.Mapperly.Abstractions;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class IdentityMapper
{
    public static partial UserDto Map(this User source);
    public static partial void Patch(this EditUserDto source, User destination);

    // https://mapperly.riok.app/docs/configuration/before-after-map/
    private static partial UserSessionDto AutoMap(this UserSession source);

    public static UserSessionDto Map(this UserSession source, TimeSpan refreshTokenExpiresIn)
    {
        var dto = source.AutoMap();

        dto.LastSeenOn = source.RenewedOn is null ||
                         DateTimeOffset.UtcNow - source.RenewedOn < TimeSpan.FromMinutes(5)
                         ? AppStrings.Online
                         : DateTimeOffset.UtcNow - source.RenewedOn < TimeSpan.FromMinutes(15)
                            ? AppStrings.Recently
                            : source.RenewedOn.Humanize(culture: CultureInfo.CurrentUICulture);

        dto.IsValid = DateTimeOffset.UtcNow - (source.RenewedOn ?? source.StartedOn) < refreshTokenExpiresIn;

        return dto;
    }
}
