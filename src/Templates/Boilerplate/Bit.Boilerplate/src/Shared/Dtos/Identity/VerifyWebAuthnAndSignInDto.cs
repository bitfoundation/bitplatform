namespace Boilerplate.Shared.Dtos.Identity;

public partial class VerifyWebAuthnAndSignInDto<T>
{
    public required T ClientResponse { get; set; }

    public string? TfaCode { get; set; }
}
