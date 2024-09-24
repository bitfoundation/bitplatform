using Boilerplate.Client.Core.Services;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Tests;

[TestClass]
public partial class SampleApiTest
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();

        await using var scope = server.Services.CreateAsyncScope();

        var identityController = scope.ServiceProvider.GetRequiredService<AuthenticationManager>();

        var signInResponse = await identityController.SignIn(new()
        {
            Email = "test@bitplatform.dev",
            Password = "123456"
        }, default);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var user = await userController.GetCurrentUser(default);

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }
}
