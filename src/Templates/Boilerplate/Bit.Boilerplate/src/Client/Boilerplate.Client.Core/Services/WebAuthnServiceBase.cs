using Fido2NetLib;

namespace Boilerplate.Client.Core.Services;

public abstract partial class WebAuthnServiceBase : IWebAuthnService
{
    private const string STORE_KEY = "bit-webauthn";

    [AutoInject] protected IStorageService storageService = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public abstract ValueTask<AuthenticatorAttestationRawResponse> CreateWebAuthnCredential(CredentialCreateOptions options);

    public abstract ValueTask<AuthenticatorAssertionRawResponse> GetWebAuthnCredential(AssertionOptions options, CancellationToken cancellationToken);

    public abstract ValueTask<bool> IsWebAuthnAvailable();

    public virtual async ValueTask<Guid[]> GetWebAuthnConfiguredUserIds()
    {
        var userIdsAsString = await storageService.GetItem(STORE_KEY);

        if (string.IsNullOrEmpty(userIdsAsString))
            return [];

        return JsonSerializer.Deserialize(userIdsAsString, jsonSerializerOptions.GetTypeInfo<Guid[]>())!;
    }

    public async ValueTask<bool> IsWebAuthnConfigured(Guid? userId = null)
    {
        var userIds = (await GetWebAuthnConfiguredUserIds())!;

        return userId is not null ? userIds.Contains(userId.Value) : userIds.Any();
    }

    public async ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId = null)
    {
        var userIds = (await GetWebAuthnConfiguredUserIds())!;

        await storageService.SetItem(STORE_KEY, JsonSerializer.Serialize(userId.HasValue ? [.. userIds.Where(u => u != userId.Value)] : [], jsonSerializerOptions.GetTypeInfo<Guid[]>()));
    }

    public async ValueTask SetWebAuthnConfiguredUserId(Guid userId)
    {
        var userIds = (await GetWebAuthnConfiguredUserIds())!;

        await storageService.SetItem(STORE_KEY, JsonSerializer.Serialize([.. userIds.Union([userId])], jsonSerializerOptions.GetTypeInfo<Guid[]>()));
    }
}
