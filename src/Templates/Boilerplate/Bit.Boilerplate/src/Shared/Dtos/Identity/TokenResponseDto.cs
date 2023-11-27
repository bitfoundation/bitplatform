﻿
namespace Boilerplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class TokenResponseDto
{
    public string? TokenType { get; set; }

    public string? AccessToken { get; set; }

    /// <summary>
    /// In seconds.
    /// </summary>
    public long ExpiresIn { get; set; }

    public string? RefreshToken { get; set; }
}
