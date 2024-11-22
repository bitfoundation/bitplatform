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

    public DateTimeOffset RenewedOn { get; set; }

    public bool IsValid { get; set; }
}
