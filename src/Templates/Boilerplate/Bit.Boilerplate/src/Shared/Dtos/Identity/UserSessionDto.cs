namespace Boilerplate.Shared.Dtos.Identity;

public partial class UserSessionDto
{
    public Guid Id { get; set; }

    public string? IP { get; set; }

    /// <summary>
    /// Populated during sign-in using the <see cref="SignInRequestDto.DeviceInfo"/> property.
    /// </summary>
    public string? DeviceInfo { get; set; }

    public string? Address { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    public bool Privileged { get; set; }

    public DateTimeOffset RenewedOn { get; set; }

    /// <summary>
    /// If sessions has not be renewed withing the last 14 days (Based on RefreshTokenExpiration in app settings json), the session is considered invalid.
    /// </summary>
    public bool IsValid { get; set; }
}
