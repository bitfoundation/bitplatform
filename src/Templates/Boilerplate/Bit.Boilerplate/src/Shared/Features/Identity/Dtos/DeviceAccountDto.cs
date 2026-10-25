namespace Boilerplate.Shared.Features.Identity.Dtos;

/// <summary>
/// An account signed in on this device besides the active one (See AuthManager.MultiAccount.cs).
/// </summary>
public partial class DeviceAccountDto
{
    public const string QueryParameter = "account";

    public UserDto User { get; set; } = default!;

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }
}
