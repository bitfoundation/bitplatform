namespace Boilerplate.Shared.Dtos.Identity;

public partial class TwoFactorAuthResponseDto
{
    public required string SharedKey { get; init; }

    public required string AuthenticatorUri { get; init; }

    public required int RecoveryCodesLeft { get; init; }

    public string[]? RecoveryCodes { get; init; }

    public required bool IsTwoFactorEnabled { get; init; }

    //public required bool IsMachineRemembered { get; init; }

    public string? QrCode { get; set; }
}
