using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
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

    [TestMethod]
    public async Task KeysCertificate_Should_CreateAndRotateADevelopmentCertificateInServerApi()
    {
        using var host = new TestHost();
        var api = Project(host, "Contoso");

        Assert.AreEqual(0, await host.RunAsync("keys", "certificate"));
        var first = AppCertificates.Read(api, AppCertificates.ActiveName)!;
        StringAssert.Contains(AppCertificates.Subject(first), "OU=Development");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("keys", "certificate"));
        Assert.AreEqual(first, AppCertificates.Read(api, AppCertificates.ActiveName));

        Assert.AreEqual(0, await host.RunAsync("keys", "certificate", "--rotate"));
        var retired = Directory.GetFiles(api, "AppCertificate.*.key").Single();
        Assert.AreEqual(first.PrivateKey, File.ReadAllText(retired));
        Assert.AreNotEqual(first, AppCertificates.Read(api, AppCertificates.ActiveName));

        Assert.AreEqual(0, await host.RunAsync("keys", "certificate", "--list", "-o", api));
        StringAssert.Contains(host.Output, "retired");
    }

    [TestMethod]
    public async Task AProductionCertificate_Should_StayOutOfTheRepository()
    {
        using var host = new TestHost();
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse --show-toplevel", 0, host.WorkingDirectory);
        host.Runner.On("git", "check-ignore", 1);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("keys", "certificate", "--production"));
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "AppCertificate.key")));

        var outside = Path.Combine(host.Root, "keys");
        host.Runner.On("git", "rev-parse --show-toplevel", 128, "fatal: not a git repository");

        Assert.AreEqual(0, await host.RunAsync("keys", "certificate", "--production", "--pfx", "-o", outside));
        var pair = AppCertificates.Read(outside, AppCertificates.ActiveName)!;
        StringAssert.Contains(AppCertificates.Subject(pair), "OU=Production");

        var password = System.Text.RegularExpressions.Regex.Match(host.Output, @"password (?<value>[A-Za-z0-9]{32})").Groups["value"].Value;
        using var pfx = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(outside, "AppCertificate.pfx"), password);
        Assert.IsTrue(pfx.HasPrivateKey);
    }

    [TestMethod]
    public void AnAndroidKeystore_Should_HoldTheKeyUnderItsAliasWithOnePassword()
    {
        var keystore = AndroidKeystore.Create("Contoso");

        var info = Pkcs12Info.Decode(keystore.Content, out _);
        Assert.IsTrue(info.VerifyMac(keystore.Password));

        var bags = info.AuthenticatedSafe.SelectMany(safe =>
        {
            if (safe.ConfidentialityMode is Pkcs12ConfidentialityMode.Password)
            {
                safe.Decrypt(keystore.Password);
            }

            return safe.GetBags();
        }).ToList();

        Assert.HasCount(2, bags);
        Assert.IsTrue(bags.Any(b => b is Pkcs12ShroudedKeyBag));

        foreach (var bag in bags)
        {
            var friendlyName = bag.Attributes.Cast<CryptographicAttributeObject>().Single(a => a.Oid.Value == "1.2.840.113549.1.9.20").Values[0];
            Assert.AreEqual("Contoso", new System.Formats.Asn1.AsnReader(friendlyName.RawData, System.Formats.Asn1.AsnEncodingRules.DER).ReadCharacterString(System.Formats.Asn1.UniversalTagNumber.BMPString));
        }

        using var certificate = X509CertificateLoader.LoadPkcs12(keystore.Content, keystore.Password);
        Assert.IsTrue(certificate.NotAfter > DateTime.UtcNow.AddYears(25));
        Assert.AreEqual(AndroidKeystore.Fingerprint(certificate), keystore.Sha256Fingerprint);
        StringAssert.Matches(keystore.Sha256Fingerprint, new System.Text.RegularExpressions.Regex("^([0-9A-F]{2}:){31}[0-9A-F]{2}$"));
    }

    [TestMethod]
    public async Task KeysAndroid_Should_AddTheFingerprintToAssetLinks()
    {
        using var host = new TestHost();
        Project(host, "Contoso");
        var maui = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "Client", "Contoso.Client.Maui")).FullName;
        File.WriteAllText(Path.Combine(maui, "Contoso.Client.Maui.csproj"), "<Project><PropertyGroup><ApplicationId>com.contoso.shop</ApplicationId></PropertyGroup></Project>");
        var wellKnown = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "Client", "Contoso.Client.Web", "wwwroot", ".well-known")).FullName;
        File.WriteAllText(Path.Combine(wellKnown, "assetlinks.json"), """[{"relation":["delegate_permission/common.handle_all_urls"],"target":{"namespace":"android_app","package_name":"com.bitplatform.Todo.Template","sha256_cert_fingerprints":["AA"]}}]""");

        Assert.AreEqual(0, await host.RunAsync("keys", "android"));

        var keystore = Path.Combine(host.Home, ".bitplatform", "keys", "Contoso", "Contoso.keystore");
        Assert.IsTrue(File.Exists(keystore), host.Output);

        var links = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(wellKnown, "assetlinks.json")))!.AsArray();
        Assert.HasCount(2, links);
        var fingerprint = (string)links[1]!["target"]!["sha256_cert_fingerprints"]![0]!;
        Assert.AreEqual("com.contoso.shop", (string)links[1]!["target"]!["package_name"]!);
        StringAssert.Contains(host.Output, fingerprint);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("keys", "android"));
    }

    private static string Project(TestHost host, string name)
    {
        File.WriteAllText(Path.Combine(host.WorkingDirectory, $"{name}.slnx"), "<Solution />");
        return Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "Server", $"{name}.Server.Api")).FullName;
    }
}
