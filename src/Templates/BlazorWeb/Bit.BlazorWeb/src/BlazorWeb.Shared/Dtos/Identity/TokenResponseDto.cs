
namespace BlazorWeb.Shared.Dtos.Identity;

public class TokenResponseDto
{
    public string? TokenType { get; set; }

    public string? AccessToken { get; set; }

    public long ExpiresIn { get; set; }

    public string? RefreshToken { get; set; }

    public const long RefreshTokenExpiresIn = 14 * 24 * 3600; /* 7 days */
}
