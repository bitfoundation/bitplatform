using System.Formats.Asn1;
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
            var pkcs8 = Convert.FromBase64String(body);
            using var key = ECDsa.Create();
            key.ImportPkcs8PrivateKey(pkcs8, out _);

            // The curve as PKCS#8 names it, the same on every platform: PrivateKeyInfo's AlgorithmIdentifier parameters.
            var info = new AsnReader(pkcs8, AsnEncodingRules.DER).ReadSequence();
            info.ReadInteger();
            var algorithm = info.ReadSequence();
            algorithm.ReadObjectIdentifier();
            return algorithm.ReadObjectIdentifier() == ECCurve.NamedCurves.nistP256.Oid.Value;
        }
        catch (Exception exp) when (exp is FormatException or CryptographicException or AsnContentException)
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
