using System.Buffers.Text;
using System.Security.Cryptography;
using Bit.Cli.Keys;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class KeysTests
{
    [TestMethod]
    public void Vapid_Should_CreateAMatchingP256KeyPair()
    {
        var keys = VapidKeys.Create();

        var publicKey = Base64Url.DecodeFromChars(keys.PublicKey);
        var privateKey = Base64Url.DecodeFromChars(keys.PrivateKey);

        Assert.HasCount(65, publicKey);
        Assert.AreEqual(0x04, publicKey[0]);
        Assert.HasCount(32, privateKey);

        using var imported = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = privateKey });
        var derived = imported.ExportParameters(false);

        CollectionAssert.AreEqual(publicKey[1..33], derived.Q.X);
        CollectionAssert.AreEqual(publicKey[33..], derived.Q.Y);
        Assert.AreNotEqual(keys.PublicKey, VapidKeys.Create().PublicKey);
    }

    [TestMethod]
    public async Task KeysVapid_Should_PrintBothKeys()
    {
        using var host = new TestHost();

        Assert.AreEqual(0, await host.RunAsync("keys", "vapid"));

        StringAssert.Contains(host.Output, "Public key");
        StringAssert.Contains(host.Output, "Private key");
    }
}
