namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest")]
public partial class IntegrationTests
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        /* var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow); // Microsoft.Extensions.TimeProvider.Testing */

        await server.Build(configureTestWebAppServices: services =>
        {
            services.AddBrowserlessClientServices();
            // You can override services here for this specific test if needed:
            // services.Replace(ServiceDescriptor.Singleton<TimeProvider>(timeProvider));
        }).Start(TestContext.CancellationToken);

        // AuthManager and the typed api clients come from Server.Web's container, so the test calls the api through the
        // app's whole HttpClient pipeline, error handling included.
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var authenticationManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        await authenticationManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var user = await userController.GetCurrentUser(TestContext.CancellationToken);

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }

    [TestMethod]
    public async Task UnauthorizedAccessTest()
    {
        await using var server = new AppTestServer();

        await server.Build(configureTestWebAppServices: s => s.AddBrowserlessClientServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(() => userController.GetCurrentUser(TestContext.CancellationToken));
    }

    public TestContext TestContext { get; set; } = default!;
}
