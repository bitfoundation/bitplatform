using System.Buffers.Text;
using System.Security.Cryptography;

namespace Bit.Cli.Keys;

public sealed record VapidKeys(string PublicKey, string PrivateKey)
{
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
