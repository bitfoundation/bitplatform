namespace Boilerplate.Shared.Dtos.Identity;

public partial class WebAuthnAssertionOptionsRequestDto
{
    public List<Guid> UserIds { get; set; } = [];
}
