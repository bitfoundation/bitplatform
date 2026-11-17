using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Shared.Features.Identity.Dtos;

/// <summary>
/// A user session kept on this device besides the active one (See AuthManager.MultiAccount.cs).
/// </summary>
public partial class DeviceUserSessionDto
{
    public const string QueryParameter = "account";

    public Guid UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? FullName { get; set; }

    public bool HasProfileImage { get; set; }

    public string? Version { get; set; }

    public Guid? SessionId { get; set; }

    public DateTimeOffset? RefreshTokenExpiresOn { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DevicePinLockDto? PinLock { get; set; }

    public DeviceSealedDataDto? SealedTokens { get; set; }

    public int FailedPinAttempts { get; set; }

    [JsonIgnore] public string? DisplayName => FullName ?? DisplayUserName;

    [JsonIgnore] public string? DisplayUserName => Email ?? PhoneNumber ?? UserName;

    [JsonIgnore] public bool HasPin => PinLock is not null;

    public string? GetProfileImageUrl(Uri absoluteServerAddress)
    {
        return HasProfileImage is false
            ? null
            : new Uri(absoluteServerAddress, $"/api/v1/Attachment/GetAttachment/{UserId}/{AttachmentKind.UserProfileImageSmall}?v={Version}").ToString();
    }
}
