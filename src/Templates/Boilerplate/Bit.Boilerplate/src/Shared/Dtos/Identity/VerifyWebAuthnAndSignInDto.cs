namespace Boilerplate.Shared.Dtos.Identity;

public partial class VerifyWebAuthnAndSignInDto<T>
{
    public required T ClientResponse { get; set; }

    public string? TfaCode { get; set; }

    /// <example>Samsung Android 14</example>
    public string? DeviceInfo { get; set; }
}

public class VerifyWebAuthnAndSignInDto : VerifyWebAuthnAndSignInDto<JsonElement>
{

}
