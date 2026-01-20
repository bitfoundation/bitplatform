//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Shared.Features.Identity;

[Mapper(UseDeepCloning = true)]
public static partial class IdentityMapper
{
    public static partial void Patch(this UserDto source, UserDto destination);
    public static partial void Patch(this EditUserRequestDto source, UserDto destination);
    public static partial void Patch(this UserDto source, EditUserRequestDto destination);
}
