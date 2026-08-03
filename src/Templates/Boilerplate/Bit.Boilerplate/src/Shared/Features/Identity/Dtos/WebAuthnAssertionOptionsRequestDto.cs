namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class WebAuthnAssertionOptionsRequestDto
{
    /// <summary>
    /// The user ids whose credential descriptors should be offered to the authenticator. The real flow sends
    /// the ids the client already holds in local storage, which is a handful - the cap is here because the
    /// endpoint is anonymous and the list was otherwise unbounded.
    /// </summary>
    [MaxLength(10)]
    public Guid[] UserIds { get; set; } = [];
}
