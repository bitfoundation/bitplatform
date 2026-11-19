using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Bit.Cli.Keys;

/// <summary>
/// An AuthKey_&lt;key id&gt;.p8 from Apple, for APNs or Sign in with Apple, as bit Boilerplate's server takes it from a secret:
/// its body on one line, since the deployment refuses a line break.
/// </summary>
public static partial class AppleP8Key
{
    /// <summary>The key's base64 body, without its BEGIN and END lines, white space or escaped line breaks.</summary>
    public static string Body(string key) => NotBodyRegex().Replace(key, "");

    /// <summary>Whether the body is a P-256 private key, which every key Apple gives is.</summary>
    public static bool IsValid(string body)
    {
        try
        {
            using var key = ECDsa.Create();
            key.ImportPkcs8PrivateKey(Convert.FromBase64String(body), out _);
            return key.KeySize is 256;
        }
        catch (Exception exp) when (exp is FormatException or CryptographicException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"-----(BEGIN|END) PRIVATE KEY-----|\\n|\s")]
    private static partial Regex NotBodyRegex();
}

/// <summary>
/// What a Services ID takes for Sign in with Apple's web flow, for an environment whose api answers at an address.
/// </summary>
public static class SignInWithAppleSite
{
    /// <summary>Where AspNet.Security.OAuth.Apple, bit Boilerplate's Sign in with Apple, takes Apple's answer.</summary>
    public const string CallbackPath = "/signin-apple";

    public static (string Domain, string ReturnUrl)? For(string? apiAddress)
    {
        return Uri.TryCreate(apiAddress?.Trim(), UriKind.Absolute, out var address) && address.Scheme == Uri.UriSchemeHttps
            ? (address.Host, $"{address.AbsoluteUri.TrimEnd('/')}{CallbackPath}")
            : null;
    }
}
