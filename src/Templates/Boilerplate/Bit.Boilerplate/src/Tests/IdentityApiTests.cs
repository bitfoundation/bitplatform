using Boilerplate.Client.Core.Services;
using Boilerplate.Client.Core.Services.Contracts;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests;

[TestClass]
public partial class IdentityApiTests
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        /* var fakeAuthTokenProvider = A.Fake<IAuthTokenProvider>();
        A.CallTo(() => fakeAuthTokenProvider.GetAccessToken()).ReturnsLazily(() => (string?)null); */

        await server.Build(services =>
        {
            // You can override services here for this specific test if needed:
            // services.Replace(ServiceDescriptor.Scoped(sp => fakeAuthTokenProvider));
        }).Start(TestContext.CancellationToken);

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

        await server.Build().Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(() => userController.GetCurrentUser(TestContext.CancellationToken));
    }

    public TestContext TestContext { get; set; } = default!;
}
