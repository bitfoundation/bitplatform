namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest"), TestCategory("Identity")]
public partial class CrossTabSignOutTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task SigningOut_Should_RemoveTheRefreshToken_BeforeTheAccessToken()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
        {
            services.RemoveAll<IStorageService>();
            services.AddScoped<IStorageService, RecordsTokenRemovals>();
        }).Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var storage = (RecordsTokenRemovals)client.Services.GetRequiredService<IStorageService>();

        await client.AuthManager.SignOut(TestContext.CancellationToken);

        Assert.IsTrue(storage.AccessTokenRemoved, "Signing out did not remove the access token.");
        Assert.IsFalse(storage.RefreshTokenLeftWhenAccessTokenRemoved,
            "Another tab that sees the access token go would still find the revoked refresh token and try to refresh with it.");
    }

    private sealed class RecordsTokenRemovals : IStorageService
    {
        private readonly TestStorageService inner = new();

        public bool AccessTokenRemoved { get; private set; }
        public bool RefreshTokenLeftWhenAccessTokenRemoved { get; private set; }

        public ValueTask<string?> GetItem(string key) => inner.GetItem(key);
        public ValueTask<bool> IsPersistent(string key) => inner.IsPersistent(key);
        public ValueTask SetItem(string key, string? value, bool persistent = true) => inner.SetItem(key, value, persistent);
        public ValueTask Clear() => inner.Clear();

        public async ValueTask RemoveItem(string key)
        {
            await inner.RemoveItem(key);

            if (key is "access_token")
            {
                AccessTokenRemoved = true;
                RefreshTokenLeftWhenAccessTokenRemoved = await inner.GetItem("refresh_token") is not null;
            }
        }
    }
}
