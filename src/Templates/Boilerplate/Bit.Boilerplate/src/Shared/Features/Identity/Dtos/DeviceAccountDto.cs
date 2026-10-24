namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class DeviceAccountDto
{
    public const string QueryParameter = "account";

    public UserDto User { get; set; } = default!;

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }
}
