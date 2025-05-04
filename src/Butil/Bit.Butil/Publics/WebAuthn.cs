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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthVerifyOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthVerifyRp))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthVerifyUser))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebAuthVerifyPubKeyCredParam))]
    public async Task<bool> Verify(bool forceCreate = false)
    {
        try
        {
            var isRegistered = await localStorage.GetItem(STORAGE_KEY);
            if (isRegistered is null || forceCreate)
            {
                await CreateCredential(new WebAuthVerifyOptions
                {
                    Challenge = "Butil Verify Challenge",
                    Rp = new() { Name = "Butil Verify" },
                    User = new() { Id = "ButilVerifyUserId", Name = "ButilVerifyUser", DisplayName = "ButilVerifyUser" },
                    AuthenticatorSelection = new() { AuthenticatorAttachment = "platform" },
                    PubKeyCredParams = [new WebAuthVerifyPubKeyCredParam() { Alg = -7, Type = "public-key" }]
                });
                await localStorage.SetItem(STORAGE_KEY, "ButilVerifyIsRegistered!");
            }
            else
            {
                await GetCredential(new WebAuthVerifyOptions { Challenge = "Butil Verify Challenge" });
            }

            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
}
