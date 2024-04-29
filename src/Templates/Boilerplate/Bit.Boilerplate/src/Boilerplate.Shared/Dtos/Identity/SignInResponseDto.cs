
namespace Boilerplate.Shared.Dtos.Identity;

public class SignInResponseDto
{
    public string? TokenType { get; set; }

    public string? AccessToken { get; set; }

    /// <summary>
    /// In seconds.
    /// </summary>
    public long ExpiresIn { get; set; }

    public string? RefreshToken { get; set; }

    public bool RequiresTwoFactor { get; set; }
}
