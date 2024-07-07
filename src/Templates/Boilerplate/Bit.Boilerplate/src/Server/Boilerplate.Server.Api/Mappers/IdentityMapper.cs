using Boilerplate.Api.Models.Identity;
using Boilerplate.Shared.Dtos.Identity;
using Riok.Mapperly.Abstractions;

namespace Boilerplate.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class IdentityMapper
{
    public static partial UserDto Map(this User source);
    public static partial void Patch(this EditUserDto source, User destination);
}
