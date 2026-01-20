namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class SignInResponseDto : TokenResponseDto
{
    public bool RequiresTwoFactor { get; set; }
}
