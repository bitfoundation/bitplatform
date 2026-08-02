using Fido2NetLib;
using Fido2NetLib.Objects;
using System.Buffers.Text;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>DeleteWebAuthnCredential</c> takes the credential id straight from the request body. The only thing scoping it
/// to the caller is the <c>&amp;&amp; webAuthCred.UserId == userId</c> term in its <c>ExecuteDeleteAsync</c> predicate -
/// there is no FIDO2 verification on this path, so that one term <b>is</b> the authorization check.
/// <para>
/// Credential ids are not secret: <c>GetWebAuthnAssertionOptions</c> hands out the descriptors of any user it is
/// asked about, so an attacker does not have to guess one. Without the owner term, anyone signed in could strip the
/// passkeys off any account - a targeted denial of the victim's sign-in method, and a way to force a fallback to a
/// weaker one.
/// </para>
/// <para>
/// The predicate is a single conjunct in a one-line query and reads perfectly plausibly with or without it, which is
/// why it gets a test rather than a code comment.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class WebAuthnCredentialOwnershipTests
{
    /// <summary>
    /// A signed-in stranger presenting someone else's credential id must be told the credential does not exist, and
    /// the row must still be there afterwards. The "not found" wording is deliberate: the endpoint must not
    /// distinguish "not yours" from "does not exist", or it becomes an oracle for which credential ids are real.
    /// </summary>
    [TestMethod]
    public async Task DeleteWebAuthnCredential_Should_RefuseACredentialOwnedByAnotherUser()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // The victim is the seeded account; the credential row below is this test's own fixture and is removed again
        // in the finally, so nothing is left behind that would make the app think passwordless is configured.
        var victimUserId = await TestAccountUtils.ReadUserId(server, TestData.DefaultTestEmail, TestContext.CancellationToken);
        var credentialId = await AddCredential(server, victimUserId);

        try
        {
            // The attacker: an ordinary per-run account, signed in through the shipped endpoints.
            await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

            var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

            await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
                () => userController.DeleteWebAuthnCredential(AssertionFor(credentialId), TestContext.CancellationToken),
                "Deleting a passkey must be scoped to the caller; the credential id alone is not authorization.");

            Assert.IsTrue(await CredentialExists(server, credentialId),
                "The victim's credential row must survive the refused delete.");
        }
        finally
        {
            await RemoveCredential(server, credentialId);
        }
    }

    /// <summary>
    /// Non-vacuity: with the same request shape, the owner's own credential really is deleted. Without this, the test
    /// above would keep passing if the endpoint started refusing everything (a broken route, a changed body contract,
    /// a model-binding failure) and would stop being about ownership at all.
    /// </summary>
    [TestMethod]
    public async Task DeleteWebAuthnCredential_Should_RemoveTheCallersOwnCredential()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var credentialId = await AddCredential(server, userId);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IUserController>()
                       .DeleteWebAuthnCredential(AssertionFor(credentialId), TestContext.CancellationToken);

            Assert.IsFalse(await CredentialExists(server, credentialId), "The caller's own credential must be deleted.");
        }
        finally
        {
            await RemoveCredential(server, credentialId);
        }
    }


    /// <summary>
    /// The body the browser posts. Only <c>RawId</c> is read server-side; it is built from the real
    /// <see cref="AuthenticatorAssertionRawResponse"/> type so the wire shape (base64url, casing, the nested response
    /// object) is whatever the library says it is rather than whatever this test guessed.
    /// </summary>
    private static JsonElement AssertionFor(byte[] credentialId)
    {
        var assertion = new AuthenticatorAssertionRawResponse
        {
            Id = Base64Url.EncodeToString(credentialId), // The browser sends the same value twice: base64url as `id`, raw as `rawId`.
            RawId = credentialId,
            Type = PublicKeyCredentialType.PublicKey,
            ClientExtensionResults = new AuthenticationExtensionsClientOutputs(), // [Required] on the DTO, so model validation rejects the request without it.
            Response = new AuthenticatorAssertionRawResponse.AssertionResponse
            {
                AuthenticatorData = [],
                ClientDataJson = [],
                Signature = []
            }
        };

        return JsonSerializer.SerializeToElement(assertion, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// Writes a credential row directly, because the only way to create one through the app is a full FIDO2
    /// registration ceremony, which needs a browser and an authenticator. Nothing this test asserts depends on the
    /// row being cryptographically meaningful - the endpoint under test never looks at the key material.
    /// </summary>
    private async Task<byte[]> AddCredential(AppTestServer server, Guid userId)
    {
        var credentialId = Guid.NewGuid().ToByteArray();

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.WebAuthnCredential.AddAsync(new WebAuthnCredential
        {
            Id = credentialId,
            UserId = userId,
            RegDate = DateTimeOffset.UtcNow
        }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return credentialId;
    }

    private async Task RemoveCredential(AppTestServer server, byte[] credentialId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.WebAuthnCredential.Where(c => c.Id == credentialId).ExecuteDeleteAsync(CancellationToken.None);
    }

    private async Task<bool> CredentialExists(AppTestServer server, byte[] credentialId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.WebAuthnCredential.AnyAsync(c => c.Id == credentialId, TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
