namespace Boilerplate.Client.Core.Services.Contracts;

public interface IWebAuthnService
{
    ValueTask<bool> IsWebAuthnAvailable();

    ValueTask<object> CreateWebAuthnCredential(object options);

    ValueTask<object> GetWebAuthnCredential(object options);

    ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null);

    ValueTask<Guid[]> GetWebAuthnConfiguredUserIds();

    ValueTask SetWebAuthnConfiguredUserId(Guid userId);

    ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId = null);
}
