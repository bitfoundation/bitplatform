namespace Boilerplate.Shared.Dtos.Identity;

public partial class WebAuthnAssertionOptionsRequestDto
{
    public Guid[] UserIds { get; set; } = [];
}
