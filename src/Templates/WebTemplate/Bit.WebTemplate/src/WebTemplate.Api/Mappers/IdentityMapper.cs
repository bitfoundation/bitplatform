﻿using Riok.Mapperly.Abstractions;
using WebTemplate.Api.Models.Identity;
using WebTemplate.Shared.Dtos.Identity;

namespace WebTemplate.Api.Mappers;

/// <summary>
/// More info at Server/Api/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class IdentityMapper
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
