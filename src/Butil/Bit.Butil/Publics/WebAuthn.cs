using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

public class WebAuthn(IJSRuntime js)
{
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
    public async Task<object> CreateCredential(object options)
        => await js.Invoke<object>("BitButil.webAuthn.createCredential", options);

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
    public async Task<object> GetCredential(object options)
        => await js.Invoke<object>("BitButil.webAuthn.getCredential", options);

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
    public async Task<bool> Verify()
    {
        try
        {
            await js.InvokeVoid("BitButil.webAuthn.getCredential", new { challenge = "Verify using Butil" });

            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
}
