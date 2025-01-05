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

        await server.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
            services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
            services.Replace(ServiceDescriptor.Transient<IAuthTokenProvider, TestAuthTokenProvider>());
        }).Start();

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var authenticationManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        await authenticationManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, default);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var user = await userController.GetCurrentUser(default);

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }

    [TestMethod]
    public async Task UnauthorizedAccessTest()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<IStorageService, TestStorageService>());
            services.Replace(ServiceDescriptor.Transient<IAuthTokenProvider, TestAuthTokenProvider>());
        }).Start();

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExceptionAsync<UnauthorizedException>(() => userController.GetCurrentUser(default));
    }
}
