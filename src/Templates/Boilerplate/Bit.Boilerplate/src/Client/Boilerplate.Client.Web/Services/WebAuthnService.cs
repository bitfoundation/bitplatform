namespace Boilerplate.Client.Web.Services;

public partial class WebAuthnService : WebAuthnServiceBase
{
    [AutoInject] private Bit.Butil.WebAuthn webAuthn = default!;

    public override async ValueTask<bool> IsWebAuthnAvailable()
    {
        return await webAuthn.IsAvailable();
    }

    public override async ValueTask<object> CreateWebAuthnCredential(object options)
    {
        return await webAuthn.CreateCredential(options);
    }

    public override async ValueTask<object> GetWebAuthnCredential(object options)
    {
        return await webAuthn.GetCredential(options);
    }
}
