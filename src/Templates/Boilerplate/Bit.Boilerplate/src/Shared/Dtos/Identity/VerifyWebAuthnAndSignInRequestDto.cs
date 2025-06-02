namespace Boilerplate.Shared.Dtos.Identity;

public partial class VerifyWebAuthnAndSignInRequestDto<T>
{
    public required T ClientResponse { get; set; }

    public string? TfaCode { get; set; }
}

public class VerifyWebAuthnAndSignInRequestDto : VerifyWebAuthnAndSignInRequestDto<JsonElement>
{

}
