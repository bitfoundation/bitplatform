namespace Boilerplate.Shared.Features.Identity.Dtos;

/// <summary>
/// Which channels <c>UserController.SendElevatedAccessToken</c> reached - only the server knows which identifiers are
/// confirmed. Channels rather than identifiers, because the caller already holds the e-mail and the phone number.
/// </summary>
public partial class ElevatedAccessTokenSentDto
{
    public bool SentToEmail { get; set; }

    public bool SentToPhoneNumber { get; set; }

    /// <summary>Pushed to the user's other sessions - only the ones trusted in their own right (See <c>UserSession.Trusted</c>).</summary>
    public bool SentToOtherDevices { get; set; }
}
