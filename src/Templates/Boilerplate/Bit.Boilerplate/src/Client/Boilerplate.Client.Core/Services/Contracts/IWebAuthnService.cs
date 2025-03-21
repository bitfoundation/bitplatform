using Fido2NetLib;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IWebAuthnService
{
    ValueTask<bool> IsWebAuthnAvailable();

    ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null);

    ValueTask<Guid[]> GetWebAuthnConfiguredUserIds();

    ValueTask SetWebAuthnConfiguredUserId(Guid userId);

    ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId = null);

    ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(CredentialCreateOptions options);

    ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(AssertionOptions options, CancellationToken cancellationToken);
}
