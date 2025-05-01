namespace Boilerplate.Client.Core.Services.Contracts;

public interface IWebAuthnService
{
    ValueTask<bool> IsWebAuthnAvailable();

    ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options);

    ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options);

    ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null);

    ValueTask<Guid[]> GetWebAuthnConfiguredUserIds();

    ValueTask SetWebAuthnConfiguredUserId(Guid userId);

    ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId = null);
}
