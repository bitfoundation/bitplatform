//+:cnd:noEmit
namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// "Remember me" decides one thing: whether this sign-in survives the app being closed. That decision has to hold for
/// <b>both</b> tokens and it has to survive a refresh, because a refresh is what happens minutes into every session -
/// <c>AuthManager.StoreTokens</c> is called again with no <c>rememberMe</c> argument and re-derives it from storage.
/// <para>
/// Two defects lived in exactly that gap. The access token was stored persistently regardless of the user's choice,
/// so a "don't remember me" session left it behind - and <c>GetAuthenticationStateAsync</c> parses a leftover access
/// token <b>without validating expiry</b>, so the app kept rendering the previous user's name, e-mail and roles with
/// no path back to anonymous. Separately, the storage implementations answered "is this key persistent?" from
/// whichever store happened to hold it rather than from the persistent one, so the first refresh silently promoted a
/// session the user asked not to remember.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Identity")]
public partial class AuthManagerTokenPersistenceTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Drives the real sequence - sign in, then refresh - and asserts the persistence of both tokens at both points.
    /// The refresh is the half that matters: at sign-in <c>rememberMe</c> is passed explicitly, so only the second
    /// check can catch a session being promoted behind the user's back.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task BothTokens_Should_KeepTheRememberMeTheUserChose_AcrossARefresh(bool rememberMe)
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services => services.AddIntegrationApiOnlyTestsServices())
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var requiresTwoFactor = await authManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword,
            RememberMe = rememberMe
        }, TestContext.CancellationToken);

        Assert.IsFalse(requiresTwoFactor, $"'{TestData.DefaultTestEmail}' is not expected to have two factor authentication enabled.");

        await AssertPersistence("right after signing in");

        // A real refresh against the real endpoint, which is what re-enters StoreTokens with no rememberMe argument.
        var refreshedAccessToken = await authManager.RefreshToken(requestedBy: nameof(BothTokens_Should_KeepTheRememberMeTheUserChose_AcrossARefresh));

        Assert.IsFalse(string.IsNullOrEmpty(refreshedAccessToken),
            "The refresh did not succeed, so the assertions below would be about a session that never got a second pair of tokens.");

        await AssertPersistence("after a refresh");

        async Task AssertPersistence(string when)
        {
            Assert.AreEqual(rememberMe, await storageService.IsPersistent("refresh_token"),
                $"The refresh token's persistence {when} does not match the user's 'Remember me' choice.");

            Assert.AreEqual(rememberMe, await storageService.IsPersistent("access_token"),
                $"The access token's persistence {when} does not match the user's 'Remember me' choice. It used to be " +
                $"stored persistently regardless, which left a signed-out browser rendering the previous user's identity.");
        }
    }

    /// <summary>
    /// The other half of the storage contract, and the one no shipped implementation used to honour: a key belongs to
    /// exactly one of the two stores. A write that leaves the superseded copy behind is not merely untidy - every
    /// implementation's <c>GetItem</c> prefers one store over the other, so the stale copy WINS the next read. On the
    /// web client that means a sign-in with "Remember me" unchecked writes its refresh token where nothing will read
    /// it, while the previous session's token in localStorage keeps being used - so the user carries on as the
    /// account they just replaced.
    /// </summary>
    [TestMethod]
    public async Task AWriteToOneStore_Should_EvictTheKeyFromTheOther()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services => services.AddIntegrationApiOnlyTestsServices())
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        await storageService.SetItem("refresh_token", "remembered", persistent: true);
        Assert.IsTrue(await storageService.IsPersistent("refresh_token"));

        await storageService.SetItem("refresh_token", "not-remembered", persistent: false);

        Assert.AreEqual("not-remembered", await storageService.GetItem("refresh_token"),
            "The persistent copy shadowed the value that was just written, so the app would keep using the superseded token.");
        Assert.IsFalse(await storageService.IsPersistent("refresh_token"),
            "A key written to temporary storage must not still report as persistent - AuthManager.StoreTokens derives " +
            "'Remember me' from that answer on every refresh, so a wrong one re-promotes the session.");

        await storageService.SetItem("refresh_token", "remembered-again", persistent: true);

        Assert.AreEqual("remembered-again", await storageService.GetItem("refresh_token"));
        Assert.IsTrue(await storageService.IsPersistent("refresh_token"));

        await storageService.RemoveItem("refresh_token");
        Assert.IsNull(await storageService.GetItem("refresh_token"), "RemoveItem has to clear both stores.");
    }
}
