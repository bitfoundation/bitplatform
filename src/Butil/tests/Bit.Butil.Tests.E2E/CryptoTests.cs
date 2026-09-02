using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class CryptoTests : ButilPageTest
{
    [TestMethod]
    public async Task RandomUuid_Returns_Valid_V4_Guid()
    {
        await ClickAndExpectAsync("crypto-uuid", "crypto:uuid:");
        var status = await CurrentStatusAsync();
        var guid = status["crypto:uuid:".Length..];
        Assert.AreEqual(36, guid.Length);
        Assert.MatchesRegex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$", guid);
    }

    [TestMethod]
    public async Task GetRandomValues_Returns_Requested_Length()
    {
        await ClickAndExpectAsync("crypto-rand", "crypto:rand:32");
    }

    [TestMethod]
    public async Task Digest_Sha256_Matches_Known_Hello_Hash()
    {
        // The well-known SHA-256("hello") digest. Pinning it ensures Butil's byte→hex pipeline
        // doesn't drift in either direction.
        await ClickAndExpectAsync("crypto-digest",
            "crypto:sha256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [TestMethod]
    public async Task AesGcm_Roundtrip_Recovers_Plaintext()
    {
        await ClickAndExpectAsync("crypto-roundtrip", "crypto:aes-gcm:True");
    }

    [TestMethod]
    public async Task AesCbc_Roundtrip_Recovers_Plaintext()
    {
        await ClickAndExpectAsync("crypto-aes-cbc", "crypto:aes-cbc:True");
    }

    [TestMethod]
    public async Task Hmac_Sign_Then_Verify_Succeeds()
    {
        await ClickAndExpectAsync("crypto-hmac", "crypto:hmac:True");
    }

    [TestMethod]
    public async Task Ecdsa_Sign_Then_Verify_Succeeds()
    {
        await ClickAndExpectAsync("crypto-ecdsa", "crypto:ecdsa:True");
    }

    [TestMethod]
    public async Task Pbkdf2_Derives_Requested_Bit_Length()
    {
        // 256 bits requested → 32 bytes derived.
        await ClickAndExpectAsync("crypto-pbkdf2", "crypto:pbkdf2:32");
    }

    [TestMethod]
    public async Task JsonWebKey_Export_Then_Import_Recovers_The_Same_Key()
    {
        // "oct" is the JWK key type for a symmetric key - anything else means the export came back
        // as a shape .NET could not read.
        await ClickAndExpectAsync("crypto-jwk", "crypto:jwk:oct/True");
    }

    [TestMethod]
    public async Task ExportKey_Round_Trips_Spki_And_Pkcs8_Unchanged()
    {
        // Importing and re-exporting under the same format has to be the identity, or the key
        // material is being altered somewhere between .NET and the browser.
        await ClickAndExpectAsync("crypto-export", "crypto:export:True/True");
    }

    [TestMethod]
    public async Task Ecdh_Both_Sides_Derive_The_Same_Key()
    {
        await ClickAndExpectAsync("crypto-ecdh", "crypto:ecdh:True");
    }

    [TestMethod]
    public async Task Hkdf_Is_Deterministic_And_Separated_By_Info()
    {
        // First flag: the same inputs derive the same key through both the bits and the key form.
        // Second: a different info string derives a different key, which is what info is for.
        await ClickAndExpectAsync("crypto-hkdf", "crypto:hkdf:True/True");
    }

    [TestMethod]
    public async Task AesKw_Wrap_Then_Unwrap_Recovers_The_Key()
    {
        // AES-KW appends an 8-byte integrity check, so a wrapped 32-byte key is 40 bytes - a
        // wrapped form the same size as the key would mean nothing was actually wrapped.
        await ClickAndExpectAsync("crypto-wrap", "crypto:wrap:40/True");
    }
}
