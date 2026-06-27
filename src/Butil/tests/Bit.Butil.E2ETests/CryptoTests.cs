using System.Text.RegularExpressions;
using Bit.Butil.E2ETests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class CryptoTests : ButilPageTest
{
    [Test]
    public async Task RandomUuid_Returns_Valid_V4_Guid()
    {
        await ClickAndExpectAsync("crypto-uuid", "crypto:uuid:");
        var status = await CurrentStatusAsync();
        var guid = status["crypto:uuid:".Length..];
        Assert.That(guid, Has.Length.EqualTo(36));
        Assert.That(Regex.IsMatch(guid, "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$"));
    }

    [Test]
    public async Task GetRandomValues_Returns_Requested_Length()
    {
        await ClickAndExpectAsync("crypto-rand", "crypto:rand:32");
    }

    [Test]
    public async Task Digest_Sha256_Matches_Known_Hello_Hash()
    {
        // The well-known SHA-256("hello") digest. Pinning it ensures Butil's byte→hex pipeline
        // doesn't drift in either direction.
        await ClickAndExpectAsync("crypto-digest",
            "crypto:sha256:2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [Test]
    public async Task AesGcm_Roundtrip_Recovers_Plaintext()
    {
        await ClickAndExpectAsync("crypto-roundtrip", "crypto:aes-gcm:True");
    }

    [Test]
    public async Task AesCbc_Roundtrip_Recovers_Plaintext()
    {
        await ClickAndExpectAsync("crypto-aes-cbc", "crypto:aes-cbc:True");
    }

    [Test]
    public async Task Hmac_Sign_Then_Verify_Succeeds()
    {
        await ClickAndExpectAsync("crypto-hmac", "crypto:hmac:True");
    }

    [Test]
    public async Task Ecdsa_Sign_Then_Verify_Succeeds()
    {
        await ClickAndExpectAsync("crypto-ecdsa", "crypto:ecdsa:True");
    }

    [Test]
    public async Task Pbkdf2_Derives_Requested_Bit_Length()
    {
        // 256 bits requested → 32 bytes derived.
        await ClickAndExpectAsync("crypto-pbkdf2", "crypto:pbkdf2:32");
    }
}
