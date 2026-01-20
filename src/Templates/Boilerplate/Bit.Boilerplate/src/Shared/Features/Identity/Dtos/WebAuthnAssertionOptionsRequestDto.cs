namespace Boilerplate.Shared.Features.Identity.Dtos;

public partial class WebAuthnAssertionOptionsRequestDto
{
    public Guid[] UserIds { get; set; } = [];
}
