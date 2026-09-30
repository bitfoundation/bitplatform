namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest")]
public partial class IntegrationTests
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        /* var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow); // Microsoft.Extensions.TimeProvider.Testing */

        await server.Build(configureTestServices: services =>
        {
            // You can override services here for this specific test if needed:
            // services.Replace(ServiceDescriptor.Singleton<TimeProvider>(timeProvider));
        }).Start(TestContext.CancellationToken);

        // Once the client's AuthManager signs a user in, the controllers created from it call the api as that user.
        await using var client = server.CreateAppClient();

        await client.AuthManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        var userController = client.GetController<IUserController>();

        var user = await userController.GetCurrentUser(TestContext.CancellationToken);

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }

    [TestMethod]
    public async Task UnauthorizedAccessTest()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();
        var userController = client.GetController<IUserController>();

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(() => userController.GetCurrentUser(TestContext.CancellationToken));
    }

    public TestContext TestContext { get; set; } = default!;
}
