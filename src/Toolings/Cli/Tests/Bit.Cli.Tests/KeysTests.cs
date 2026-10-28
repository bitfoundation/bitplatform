using System.Buffers.Text;
using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using Bit.Cli.Commands;
using Bit.Cli.Infrastructure;
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

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("keys", "certificate", "--production", "-o", Path.Combine(host.WorkingDirectory, "keys", "production")));
        Assert.IsFalse(Directory.Exists(Path.Combine(host.WorkingDirectory, "keys")));

        var outside = Path.Combine(host.Root, "keys");
        host.Runner.On("git", "rev-parse --show-toplevel", 128, "fatal: not a git repository");

        Assert.AreEqual(0, await host.RunAsync("keys", "certificate", "--production", "--pfx", "-o", outside));
        var pair = AppCertificates.Read(outside, AppCertificates.ActiveName)!;
        StringAssert.Contains(AppCertificates.Subject(pair), "OU=Production");

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(outside, "AppCertificate.key")));
            Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(outside, "AppCertificate.pfx")));
        }

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

    [TestMethod]
    public async Task KeysApple_Should_KeepTheP12OutOfTheRepository()
    {
        using var apple = new FakeAppStoreConnect();
        using var host = new TestHost();
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse --show-toplevel", 0, host.WorkingDirectory);
        host.Runner.On("git", "check-ignore", 1);
        var p8 = Path.Combine(host.Root, "AuthKey_2X9R4HXF34.p8");
        File.WriteAllText(p8, apple.Key.PrivateKey);

        Assert.AreEqual(CliApp.ExitUsage, await KeysCommand.AppleAsync(host.Services, new AppleKeysRequest(apple.Key.IssuerId, apple.Key.KeyId, p8, "com.contoso.shop", null, host.WorkingDirectory), CancellationToken.None, apple));

        Assert.IsEmpty(apple.Requests);
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "com.contoso.shop.p12")));
    }

    private static string Project(TestHost host, string name)
    {
        File.WriteAllText(Path.Combine(host.WorkingDirectory, $"{name}.slnx"), "<Solution />");
        return Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "Server", $"{name}.Server.Api")).FullName;
    }

    [TestMethod]
    public void AppleToken_Should_BeAnEs256JwtForAppStoreConnect()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var key = new AppleApiKey("57246542-96fe-1a63-e053-0824d011072a", "2X9R4HXF34", signer.ExportPkcs8PrivateKeyPem());
        var now = DateTimeOffset.UtcNow;

        var parts = AppleSigning.CreateToken(key, now).Split('.');

        Assert.HasCount(3, parts);
        var header = JsonNode.Parse(Base64Url.DecodeFromChars(parts[0]))!;
        var payload = JsonNode.Parse(Base64Url.DecodeFromChars(parts[1]))!;
        var signature = Base64Url.DecodeFromChars(parts[2]);

        Assert.AreEqual("ES256", (string?)header["alg"]);
        Assert.AreEqual("2X9R4HXF34", (string?)header["kid"]);
        Assert.AreEqual("JWT", (string?)header["typ"]);
        Assert.AreEqual(key.IssuerId, (string?)payload["iss"]);
        Assert.AreEqual("appstoreconnect-v1", (string?)payload["aud"]);
        Assert.AreEqual(now.ToUnixTimeSeconds(), (long)payload["iat"]!);
        Assert.IsGreaterThan(now.ToUnixTimeSeconds(), (long)payload["exp"]!);
        Assert.IsLessThanOrEqualTo(now.ToUnixTimeSeconds() + 20 * 60, (long)payload["exp"]!);
        Assert.HasCount(64, signature);
        Assert.IsTrue(signer.VerifyData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"), signature, HashAlgorithmName.SHA256));
    }

    [TestMethod]
    public async Task AppleSigning_Should_RefuseAKeyThatIsntAnAppStoreConnectKey()
    {
        using var rsa = RSA.Create(2048);
        using var handler = new FakeAppStoreConnect();

        var error = await Assert.ThrowsAsync<AppleSigningException>(() => new AppleSigning(handler).CreateAppStoreSigningAsync(new AppleApiKey("issuer", "KEY", rsa.ExportPkcs8PrivateKeyPem()), "com.contoso.shop", "Shop", CancellationToken.None));

        StringAssert.Contains(error.Message, ".p8");
        Assert.IsEmpty(handler.Requests);
    }

    [TestMethod]
    public async Task AppleSigning_Should_RegisterTheBundleIdAndCreateTheCertificateProfileAndP12()
    {
        using var apple = new FakeAppStoreConnect();
        apple.BundleIds.Add(("B0", "com.contoso.shop.widget", "TEAM123456"));
        apple.Profiles.Add(("P0", "com.contoso.shop App Store"));
        apple.Profiles.Add(("P9", "com.contoso.shop App Store 2"));

        var result = await new AppleSigning(apple).CreateAppStoreSigningAsync(apple.Key, "com.contoso.shop", "com.contoso.shop App Store", CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            "GET /v1/bundleIds?filter[identifier]=com.contoso.shop&limit=200",
            "POST /v1/bundleIds",
            "POST /v1/certificates",
            "GET /v1/profiles?filter[name]=com.contoso.shop App Store&limit=200",
            "DELETE /v1/profiles/P0",
            "POST /v1/profiles"
        }, apple.Requests.Select(r => r.Line).ToArray());

        foreach (var request in apple.Requests)
        {
            Assert.AreEqual("2X9R4HXF34", (string?)JsonNode.Parse(Base64Url.DecodeFromChars(request.Token!.Split('.')[0]))!["kid"]);
        }

        var bundleId = apple.Body("POST /v1/bundleIds")["data"]!;
        Assert.AreEqual("bundleIds", (string?)bundleId["type"]);
        Assert.AreEqual("com.contoso.shop", (string?)bundleId["attributes"]!["identifier"]);
        Assert.AreEqual("com contoso shop", (string?)bundleId["attributes"]!["name"]);
        Assert.AreEqual("IOS", (string?)bundleId["attributes"]!["platform"]);

        var certificate = apple.Body("POST /v1/certificates")["data"]!;
        Assert.AreEqual("certificates", (string?)certificate["type"]);
        Assert.AreEqual("DISTRIBUTION", (string?)certificate["attributes"]!["certificateType"]);
        var csr = CertificateRequest.LoadSigningRequestPem((string)certificate["attributes"]!["csrContent"]!, HashAlgorithmName.SHA256);
        Assert.AreEqual(2048, csr.PublicKey.GetRSAPublicKey()!.KeySize);

        var profile = apple.Body("POST /v1/profiles")["data"]!;
        Assert.AreEqual("profiles", (string?)profile["type"]);
        Assert.AreEqual("com.contoso.shop App Store", (string?)profile["attributes"]!["name"]);
        Assert.AreEqual("IOS_APP_STORE", (string?)profile["attributes"]!["profileType"]);
        Assert.AreEqual("bundleIds", (string?)profile["relationships"]!["bundleId"]!["data"]!["type"]);
        Assert.AreEqual("B1", (string?)profile["relationships"]!["bundleId"]!["data"]!["id"]);
        Assert.HasCount(1, profile["relationships"]!["certificates"]!["data"]!.AsArray());
        Assert.AreEqual("certificates", (string?)profile["relationships"]!["certificates"]!["data"]![0]!["type"]);
        Assert.AreEqual("C1", (string?)profile["relationships"]!["certificates"]!["data"]![0]!["id"]);

        Assert.AreEqual("com.contoso.shop App Store", result.ProfileName);
        CollectionAssert.AreEqual(FakeAppStoreConnect.ProfileContent, result.Profile);
        Assert.AreEqual("TEAM123456", result.TeamId);
        Assert.AreEqual(apple.NotAfter, result.CertificateExpiry);
        Assert.IsGreaterThanOrEqualTo(20, result.Pkcs12Password.Length);

        using var pkcs12 = X509CertificateLoader.LoadPkcs12(result.Pkcs12, result.Pkcs12Password);
        Assert.AreEqual(apple.Issued!.Thumbprint, pkcs12.Thumbprint);
        Assert.IsTrue(pkcs12.HasPrivateKey);
        using var privateKey = pkcs12.GetRSAPrivateKey()!;
        CollectionAssert.AreEqual(csr.PublicKey.GetRSAPublicKey()!.ExportParameters(false).Modulus, privateKey.ExportParameters(false).Modulus);
        Assert.ThrowsExactly<CryptographicException>(() => X509CertificateLoader.LoadPkcs12(result.Pkcs12, "wrong password"));

        Assert.IsTrue(Contains(result.Pkcs12, Oid("1.2.840.113549.1.12.1.3")), "TripleDES with SHA1");
        Assert.IsTrue(Contains(result.Pkcs12, Oid("1.3.14.3.2.26")), "a SHA1 MAC");
        Assert.IsFalse(Contains(result.Pkcs12, Oid("1.2.840.113549.1.5.13")), "no PBES2, which older macOS can't read");
    }

    [TestMethod]
    public async Task AppleSigning_Should_ReuseTheRegisteredBundleId()
    {
        using var apple = new FakeAppStoreConnect { TeamUnit = null };
        apple.BundleIds.Add(("B7", "COM.Contoso.Shop", "SEED123456"));

        var result = await new AppleSigning(apple).CreateAppStoreSigningAsync(apple.Key, "com.contoso.shop", "Shop", CancellationToken.None);

        Assert.IsFalse(apple.Requests.Any(r => r.Line is "POST /v1/bundleIds"));
        Assert.IsFalse(apple.Requests.Any(r => r.Method == HttpMethod.Delete));
        Assert.AreEqual("B7", (string?)apple.Body("POST /v1/profiles")["data"]!["relationships"]!["bundleId"]!["data"]!["id"]);
        Assert.AreEqual("SEED123456", result.TeamId);
    }

    [TestMethod]
    public async Task AppleSigning_Should_ListTheTeamsCertificatesWhenItHasTooMany()
    {
        using var apple = new FakeAppStoreConnect();
        apple.Respond["POST /v1/certificates"] = (HttpStatusCode.Conflict, FakeAppStoreConnect.Error("409", "ENTITY_ERROR", "You already have a current Distribution certificate or a pending certificate request."));
        apple.Respond["GET /v1/certificates?filter[certificateType]=DISTRIBUTION,IOS_DISTRIBUTION&limit=200"] = (HttpStatusCode.OK, new JsonObject
        {
            ["data"] = new JsonArray(
                new JsonObject { ["type"] = "certificates", ["id"] = "X1", ["attributes"] = new JsonObject { ["name"] = "Apple Distribution: Contoso", ["expirationDate"] = "2027-03-01T10:00:00.000+0000", ["serialNumber"] = "1A2B3C" } },
                new JsonObject { ["type"] = "certificates", ["id"] = "X2", ["attributes"] = new JsonObject { ["name"] = "iOS Distribution: Contoso", ["expirationDate"] = "2026-12-24T10:00:00.000+0000", ["serialNumber"] = "4D5E6F" } })
        }.ToJsonString());

        var error = await Assert.ThrowsAsync<AppleSigningException>(() => new AppleSigning(apple).CreateAppStoreSigningAsync(apple.Key, "com.contoso.shop", "Shop", CancellationToken.None));

        StringAssert.Contains(error.Message, "as many distribution certificates as Apple allows");
        StringAssert.Contains(error.Message, "You already have a current Distribution certificate");
        StringAssert.Contains(error.Message, "Apple Distribution: Contoso, expires 2027-03-01, serial 1A2B3C");
        StringAssert.Contains(error.Message, "iOS Distribution: Contoso, expires 2026-12-24, serial 4D5E6F");
        StringAssert.Contains(error.Message, $"Revoke one that no build signs with anymore at {AppleSigning.CertificatesPage}");
        Assert.IsFalse(apple.Requests.Any(r => r.Line.Contains("/v1/profiles", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task AppleSigning_Should_ExplainAKeyAppStoreConnectRefuses()
    {
        using var apple = new FakeAppStoreConnect();
        apple.Respond["GET /v1/bundleIds?filter[identifier]=com.contoso.shop&limit=200"] = (HttpStatusCode.Unauthorized, FakeAppStoreConnect.Error("401", "NOT_AUTHORIZED", "Provide a properly configured and signed bearer token, and make sure that it has not expired."));

        var error = await Assert.ThrowsAsync<AppleSigningException>(() => new AppleSigning(apple).CreateAppStoreSigningAsync(apple.Key, "com.contoso.shop", "Shop", CancellationToken.None));

        StringAssert.Contains(error.Message, "didn't accept the API key");
        StringAssert.Contains(error.Message, "issuer id");
    }

    [TestMethod]
    public async Task AppleSigning_Should_AskForAnAdminKeyWhenTheRoleCantCreateIt()
    {
        using var apple = new FakeAppStoreConnect();
        apple.Respond["POST /v1/bundleIds"] = (HttpStatusCode.Forbidden, FakeAppStoreConnect.Error("403", "FORBIDDEN_ERROR", "The API key in use does not allow this request"));

        var error = await Assert.ThrowsAsync<AppleSigningException>(() => new AppleSigning(apple).CreateAppStoreSigningAsync(apple.Key, "com.contoso.shop", "Shop", CancellationToken.None));

        StringAssert.Contains(error.Message, "isn't allowed to register the bundle id com.contoso.shop");
        StringAssert.Contains(error.Message, "Admin role");
        StringAssert.Contains(error.Message, "The API key in use does not allow this request");
    }

    [TestMethod]
    public async Task AppleSigning_Should_RevokeTheNewCertificateWhenTheProfileFails()
    {
        using var apple = new FakeAppStoreConnect();
        apple.Respond["POST /v1/profiles"] = (HttpStatusCode.Conflict, FakeAppStoreConnect.Error("409", "ENTITY_ERROR.ATTRIBUTE.INVALID", "Multiple profiles found with the name 'Shop'."));

        var error = await Assert.ThrowsAsync<AppleSigningException>(() => new AppleSigning(apple).CreateAppStoreSigningAsync(apple.Key, "com.contoso.shop", "Shop", CancellationToken.None));

        StringAssert.Contains(error.Message, "Couldn't create the profile Shop. App Store Connect answered 409: Multiple profiles found");
        Assert.AreEqual("DELETE /v1/certificates/C1", apple.Requests[^1].Line);
    }

    [TestMethod]
    public async Task KeysApple_Should_WriteTheFilesAndNameTheCdSecrets()
    {
        using var host = new TestHost(HostOs.Windows);
        using var apple = new FakeAppStoreConnect();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "Shop.Web.slnf"), "{}");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "src", "Client", "Shop.Client.Maui")).FullName, "Shop.Client.Maui.csproj"), "<Project>\n  <PropertyGroup>\n    <ApplicationId>com.contoso.shop</ApplicationId>\n  </PropertyGroup>\n</Project>\n");
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "AuthKey_2X9R4HXF34.p8"), apple.Key.PrivateKey);

        var exitCode = await KeysCommand.AppleAsync(host.Services, new AppleKeysRequest("issuer-id", "2X9R4HXF34", "AuthKey_2X9R4HXF34.p8", null, null, "signing"), CancellationToken.None, apple);

        Assert.AreEqual(0, exitCode, host.Output);
        var p12 = Path.Combine(host.WorkingDirectory, "signing", "com.contoso.shop.p12");
        var password = host.Output.Split('\n').Single(l => l.Contains("Password", StringComparison.Ordinal)).Replace("Password", "", StringComparison.Ordinal).Trim();
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(p12, password);
        Assert.IsTrue(certificate.HasPrivateKey);
        CollectionAssert.AreEqual(FakeAppStoreConnect.ProfileContent, File.ReadAllBytes(Path.Combine(host.WorkingDirectory, "signing", "com.contoso.shop.mobileprovision")));
        Assert.AreEqual("GET /v1/bundleIds?filter[identifier]=com.contoso.shop&limit=200", apple.Requests[0].Line);

        foreach (var text in new[] { "TEAM123456", "APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64", "APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD", "APPSTORE_API_KEY_ISSUER_ID", "APPSTORE_API_KEY_ID", "APPSTORE_API_KEY_PRIVATE_KEY", "IOS_CODE_SIGN_PROVISION", "variable: com.contoso.shop App Store", "the text of AuthKey_2X9R4HXF34.p8" })
        {
            StringAssert.Contains(host.Output, text);
        }

        StringAssert.Contains(host.Output, $"  [Convert]::ToBase64String([IO.File]::ReadAllBytes('{p12}'))");

        Assert.AreEqual(CliApp.ExitUsage, await KeysCommand.AppleAsync(host.Services, new AppleKeysRequest("issuer-id", "2X9R4HXF34", "AuthKey_2X9R4HXF34.p8", null, null, "signing"), CancellationToken.None, apple));
        StringAssert.Contains(host.FlatOutput, "may be the only copy of a certificate's private key");
        Assert.HasCount(5, apple.Requests);
    }

    [TestMethod]
    public async Task KeysApple_Should_NeedABundleIdOutsideAProject()
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "AuthKey.p8"), "");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("keys", "apple", "--issuer-id", "issuer", "--key-id", "KEY", "--private-key", "AuthKey.p8"));

        StringAssert.Contains(host.Output, "--bundle-id");
    }

    [TestMethod]
    public async Task KeysApple_Should_PrintWhyAppStoreConnectRefused()
    {
        using var host = new TestHost();
        using var apple = new FakeAppStoreConnect();
        apple.Respond["GET /v1/bundleIds?filter[identifier]=com.contoso.shop&limit=200"] = (HttpStatusCode.Unauthorized, FakeAppStoreConnect.Error("401", "NOT_AUTHORIZED", "Provide a properly configured and signed bearer token."));
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "AuthKey.p8"), apple.Key.PrivateKey);

        Assert.AreEqual(CliApp.ExitFailed, await KeysCommand.AppleAsync(host.Services, new AppleKeysRequest("issuer-id", "KEY", "AuthKey.p8", "com.contoso.shop", null, null), CancellationToken.None, apple));

        StringAssert.Contains(host.Output, "didn't accept the API key");
        Assert.IsFalse(File.Exists(Path.Combine(host.WorkingDirectory, "com.contoso.shop.p12")));
    }

    private static byte[] Oid(string value)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.WriteObjectIdentifier(value);
        return writer.Encode();
    }

    private static bool Contains(byte[] data, byte[] part) => data.AsSpan().IndexOf(part) >= 0;

    internal sealed class FakeAppStoreConnect : HttpMessageHandler
    {
        public static readonly byte[] ProfileContent = Encoding.UTF8.GetBytes("<plist>an App Store profile</plist>");

        private readonly ECDsa apiKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private readonly RSA authorityKey = RSA.Create(2048);
        private readonly X509Certificate2 authority;

        public FakeAppStoreConnect()
        {
            Key = new AppleApiKey("57246542-96fe-1a63-e053-0824d011072a", "2X9R4HXF34", apiKey.ExportPkcs8PrivateKeyPem());
            var request = new CertificateRequest("CN=Test Apple WWDR", authorityKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            authority = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        }

        public AppleApiKey Key { get; }

        public string? TeamUnit { get; init; } = "TEAM123456";

        public DateTimeOffset NotAfter { get; } = new(DateTime.UtcNow.Date.AddDays(365), TimeSpan.Zero);

        public X509Certificate2? Issued { get; private set; }

        public List<(string Id, string Identifier, string SeedId)> BundleIds { get; } = [];

        public List<(string Id, string Name)> Profiles { get; } = [];

        public Dictionary<string, (HttpStatusCode Status, string Body)> Respond { get; } = [];

        public List<(HttpMethod Method, string Line, string? Token, JsonNode? Body)> Requests { get; } = [];

        public JsonNode Body(string line) => Requests.Single(r => r.Line == line).Body!;

        public static string Error(string status, string code, string detail)
        {
            return new JsonObject { ["errors"] = new JsonArray(new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["status"] = status, ["code"] = code, ["title"] = "There is a problem with the request entity", ["detail"] = detail }) }.ToJsonString();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("api.appstoreconnect.apple.com", request.RequestUri!.Host);
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);

            var line = $"{request.Method} {Uri.UnescapeDataString(request.RequestUri.PathAndQuery)}";
            var body = request.Content is null ? null : JsonNode.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
            Requests.Add((request.Method, line, request.Headers.Authorization?.Parameter, body));

            var (status, content) = Respond.TryGetValue(line, out var canned) ? canned : Answer(request.Method, line, body);
            return new HttpResponseMessage(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
        }

        private (HttpStatusCode, string) Answer(HttpMethod method, string line, JsonNode? body)
        {
            if (line.StartsWith("GET /v1/bundleIds?", StringComparison.Ordinal))
                return (HttpStatusCode.OK, List(BundleIds.Select(b => Resource("bundleIds", b.Id, new JsonObject { ["identifier"] = b.Identifier, ["name"] = "An app", ["platform"] = "IOS", ["seedId"] = b.SeedId }))));

            if (line is "POST /v1/bundleIds")
                return (HttpStatusCode.Created, Single(Resource("bundleIds", "B1", new JsonObject { ["identifier"] = (string?)body!["data"]!["attributes"]!["identifier"], ["platform"] = "IOS", ["seedId"] = "SEED000000" })));

            if (line is "POST /v1/certificates")
            {
                var csr = CertificateRequest.LoadSigningRequestPem((string)body!["data"]!["attributes"]!["csrContent"]!, HashAlgorithmName.SHA256);
                var subject = new X500DistinguishedNameBuilder();
                subject.AddCommonName($"Apple Distribution: Contoso ({TeamUnit ?? "none"})");

                if (TeamUnit is not null)
                {
                    subject.AddOrganizationalUnitName(TeamUnit);
                }

                subject.AddOrganizationName("Contoso");
                subject.AddCountryOrRegion("US");

                Issued = new CertificateRequest(subject.Build(), csr.PublicKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Create(authority, DateTimeOffset.UtcNow.AddMinutes(-1), NotAfter, RandomNumberGenerator.GetBytes(8));
                return (HttpStatusCode.Created, Single(Resource("certificates", "C1", new JsonObject { ["certificateType"] = "DISTRIBUTION", ["certificateContent"] = Convert.ToBase64String(Issued.RawData) })));
            }

            if (line.StartsWith("GET /v1/profiles?", StringComparison.Ordinal))
                return (HttpStatusCode.OK, List(Profiles.Select(p => Resource("profiles", p.Id, new JsonObject { ["name"] = p.Name, ["profileType"] = "IOS_APP_STORE", ["profileState"] = "ACTIVE" }))));

            if (method == HttpMethod.Delete)
                return (HttpStatusCode.NoContent, "");

            if (line is "POST /v1/profiles")
                return (HttpStatusCode.Created, Single(Resource("profiles", "P1", new JsonObject { ["name"] = (string?)body!["data"]!["attributes"]!["name"], ["profileType"] = "IOS_APP_STORE", ["profileContent"] = Convert.ToBase64String(ProfileContent), ["uuid"] = Guid.NewGuid().ToString() })));

            return (HttpStatusCode.NotFound, Error("404", "NOT_FOUND", $"No route for {line}."));
        }

        private static JsonObject Resource(string type, string id, JsonObject attributes) => new() { ["type"] = type, ["id"] = id, ["attributes"] = attributes };

        private static string List(IEnumerable<JsonObject> items) => new JsonObject { ["data"] = new JsonArray([.. items]) }.ToJsonString();

        private static string Single(JsonObject item) => new JsonObject { ["data"] = item }.ToJsonString();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                apiKey.Dispose();
                authorityKey.Dispose();
                authority.Dispose();
                Issued?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
