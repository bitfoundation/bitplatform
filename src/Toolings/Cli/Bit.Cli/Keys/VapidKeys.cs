using System.Buffers.Text;
using System.Security.Cryptography;

namespace Bit.Cli.Keys;

public sealed record VapidKeys(string PublicKey, string PrivateKey)
{
    /// <summary>
    /// The pair bit Boilerplate ships in its appsettings.json files, the same in every project created from it, which its
    /// ClientWebSettings and ServerApiSettings refuse outside Development.
    /// </summary>
    public static VapidKeys Sample { get; } = new("BDSNUvuIISD8NQVByQANEtZ2foKaENIcIGUxsiQs9kDz11fQik8c9WeiMwUHs3iTgNNH4nvXioNQIEsn4OAjTKc", "dMIR1ICj-lDWYZ-ZYCwXKyC2ShYayYYkEL-oOPnpq9c");

    public static VapidKeys Create()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(includePrivateParameters: true);

        var publicKey = new byte[65];
        publicKey[0] = 0x04;
        parameters.Q.X!.CopyTo(publicKey, 1);
        parameters.Q.Y!.CopyTo(publicKey, 33);

        return new(Base64Url.EncodeToString(publicKey), Base64Url.EncodeToString(parameters.D!));
    }
}
