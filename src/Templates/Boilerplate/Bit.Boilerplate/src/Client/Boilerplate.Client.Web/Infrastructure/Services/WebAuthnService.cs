namespace Boilerplate.Client.Web.Infrastructure.Services;

public partial class WebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private Bit.Butil.WebAuthn webAuthn = default!;

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        return await webAuthn.IsAvailable();
    }

    public override async ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options)
    {
        return await webAuthn.CreateCredential<JsonElement, JsonElement>(options);
    }

    public override async ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options)
    {
        return await webAuthn.GetCredential<JsonElement, JsonElement>(options);
    }
}
