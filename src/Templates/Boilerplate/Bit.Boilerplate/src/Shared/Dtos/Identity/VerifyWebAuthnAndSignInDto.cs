using Fido2NetLib;

namespace Boilerplate.Shared.Dtos.Identity;

public partial class VerifyWebAuthnAndSignInDto
{
    public required AuthenticatorAssertionRawResponse ClientResponse { get; set; }

    public string? TfaCode { get; set; }
}
