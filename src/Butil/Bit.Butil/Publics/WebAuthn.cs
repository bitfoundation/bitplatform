using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

public class WebAuthn(IJSRuntime js, LocalStorage localStorage)
{
    private const string STORAGE_KEY = "Butil.WebAuthn.Verify";



    /// <summary>
    /// Checks that the WebAuthentication api is available on the client or not.
    /// </summary>
    public async Task<bool> IsAvailable()
        => await js.Invoke<bool>("BitButil.webAuthn.isAvailable");


    /// <summary>
    /// Creates a new credential, which can then be stored and later retrieved using the GetCredential method. 
    /// The retrieved credential can then be used by a website to authenticate a user.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create">https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create</see>
    /// </summary>
    public async Task<JsonElement> CreateCredential(object options)
        => await js.Invoke<JsonElement>("BitButil.webAuthn.createCredential", options);

    /// <summary>
    /// Creates a new credential, which can then be stored and later retrieved using the GetCredential method. 
    /// The retrieved credential can then be used by a website to authenticate a user.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create">https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create</see>
    /// </summary>
    public async Task<JsonElement> CreateCredential(JsonElement options)
        => await js.Invoke<JsonElement>("BitButil.webAuthn.createCredential", options);

    /// <summary>
    /// Creates a new credential, which can then be stored and later retrieved using the GetCredential method. 
    /// The retrieved credential can then be used by a website to authenticate a user.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create">https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/create</see>
    /// </summary>
    public async Task<TResult> CreateCredential<TValue, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(TValue options)
        => await js.Invoke<TResult>("BitButil.webAuthn.createCredential", options);


    /// <summary>
    /// Retrieves a credential which can then be used to authenticate a user to a website.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get</see>
    /// </summary>
    public async Task<JsonElement> GetCredential(object options)
        => await js.Invoke<JsonElement>("BitButil.webAuthn.getCredential", options);

    /// <summary>
    /// Retrieves a credential which can then be used to authenticate a user to a website.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get</see>
    /// </summary>
    public async Task<JsonElement> GetCredential(JsonElement options)
        => await js.Invoke<JsonElement>("BitButil.webAuthn.getCredential", options);

    /// <summary>
    /// Retrieves a credential which can then be used to authenticate a user to a website.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get">https://developer.mozilla.org/en-US/docs/Web/API/CredentialsContainer/get</see>
    /// </summary>
    public async Task<TResult> GetCredential<TValue, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(TValue options)
        => await js.Invoke<TResult>("BitButil.webAuthn.getCredential", options);


    /// <summary>
    /// Tries to get a valid credential using the minimum required options to expose a native verification feature.
    /// </summary>
    /// <param name="forceCreate">Forces the verification to be performed using the create credential approach.</param>
    /// <param name="platform">Forces authenticator selection to be platform, otherwise it will be cross-platform (like hardware authenticators)</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthnVerifyAllowCredential))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthnVerifyAuthenticatorSelection))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthnVerifyOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthnVerifyPubKeyCredParam))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthnVerifyRp))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthnVerifyUser))]
    public async Task<bool> Verify(bool forceCreate = false, bool platform = false)
    {
        try
        {
            var verifyRawId = await localStorage.GetItem(STORAGE_KEY);
            if (verifyRawId is null || forceCreate)
            {
                var result = await CreateCredential(new WebAuthnVerifyOptions
                {
                    Challenge = "Butil Verify Challenge",
                    Attestation = "direct",
                    Rp = new() { Name = "Butil Verify" },
                    User = new() { Id = "ButilVerifyUserId", Name = "ButilVerifyUser", DisplayName = "ButilVerifyUser" },
                    AuthenticatorSelection = new() { AuthenticatorAttachment = platform ? "platform" : "cross-platform" },
                    PubKeyCredParams = [new() { Alg = -7, Type = "public-key" }, new() { Alg = -8, Type = "public-key" }, new() { Alg = -257, Type = "public-key" }]
                });
                var rawId = result.GetProperty("rawId").ToString();

                if (string.IsNullOrWhiteSpace(rawId)) return false;

                await localStorage.SetItem(STORAGE_KEY, rawId);
            }
            else
            {
                await GetCredential(new WebAuthnVerifyOptions
                {
                    Challenge = "Butil Verify Challenge",
                    AllowCredentials = [new WebAuthnVerifyAllowCredential() { Id = verifyRawId, Type = "public-key" }]
                });
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
