using AdminPanel.Server.Api.Models.Identity;
using AdminPanel.Shared.Dtos.Identity;
using Riok.Mapperly.Abstractions;

namespace AdminPanel.Server.Api.Mappers;

[Mapper(UseDeepCloning = true)]
public static partial class IdentityMappers
{
    public static partial UserDto Map(this User source);
    public static partial void Patch(this EditUserDto source, User destination);

    private static partial User MapInternal(this SignUpRequestDto source);
    public static User Map(this SignUpRequestDto source)
    {
        var destination = source.MapInternal();

        destination.UserName = source.Email; // after map sample.

        return destination;
    }
}
