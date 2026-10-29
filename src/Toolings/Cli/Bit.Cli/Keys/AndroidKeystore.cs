using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Bit.Cli.Keys;

public sealed record AndroidKeystore(byte[] Content, string Alias, string Password, string Sha256Fingerprint)
{
    private static readonly PbeParameters encryption = new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000);

    public static AndroidKeystore Create(string alias, string? password = null)
    {
        password ??= KeyFolder.NewPassword();

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(new X500DistinguishedName($"CN={alias}"), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(30));

        var localKeyId = new Pkcs9LocalKeyId(certificate.GetCertHash());
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteCharacterString(UniversalTagNumber.BMPString, alias);
        var friendlyName = new AsnEncodedData(new Oid("1.2.840.113549.1.9.20"), writer.Encode());

        var keys = new Pkcs12SafeContents();
        var keyBag = keys.AddShroudedKey(rsa, password, encryption);
        keyBag.Attributes.Add(localKeyId);
        keyBag.Attributes.Add(friendlyName);

        var certificates = new Pkcs12SafeContents();
        var certificateBag = certificates.AddCertificate(certificate);
        certificateBag.Attributes.Add(localKeyId);
        certificateBag.Attributes.Add(friendlyName);

        var builder = new Pkcs12Builder();
        builder.AddSafeContentsEncrypted(certificates, password, encryption);
        builder.AddSafeContentsUnencrypted(keys);
        builder.SealWithMac(password, HashAlgorithmName.SHA256, 100_000);

        return new(builder.Encode(), alias, password, Fingerprint(certificate));
    }

    public static string Fingerprint(X509Certificate2 certificate) => string.Join(':', SHA256.HashData(certificate.RawData).Select(b => b.ToString("X2")));
}
