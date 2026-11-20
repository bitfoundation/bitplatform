using System.Text.RegularExpressions;
using AspNet.Security.OAuth.Apple;
using Microsoft.Extensions.FileProviders;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// An Apple .p8 key (Sign in with Apple's or APNs') in a setting: the whole AuthKey_&lt;key id&gt;.p8, or only its body on one
/// line, as a CD secret has to carry it (See .docs/16).
/// </summary>
public static partial class AppleP8Key
{
    /// <summary>The key's base64 body, without its BEGIN and END lines, white space or escaped line breaks.</summary>
    public static string Body(string key) => NotBodyRegex().Replace(key, "");

    extension(AppleAuthenticationOptions options)
    {
        /// <summary>
        /// Signs the client secret with <paramref name="privateKey"/> (Authentication:Apple:PrivateKey), or else with
        /// AppleAuthKey.p8 in the content root.
        /// </summary>
        public void UsePrivateKey(string? privateKey, IFileProvider contentRoot)
        {
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                options.UsePrivateKey(_ => contentRoot.GetFileInfo("AppleAuthKey.p8"));
                return;
            }

            ReadOnlyMemory<char> pem = $"-----BEGIN PRIVATE KEY-----\n{Body(privateKey)}\n-----END PRIVATE KEY-----".AsMemory();
            options.GenerateClientSecret = true;
            options.PrivateKey = (_, _) => Task.FromResult(pem);
        }
    }

    [GeneratedRegex(@"-----(BEGIN|END) PRIVATE KEY-----|\\n|\s")]
    private static partial Regex NotBodyRegex();
}
