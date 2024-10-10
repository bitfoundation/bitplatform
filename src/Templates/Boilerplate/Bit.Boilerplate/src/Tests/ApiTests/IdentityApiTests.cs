﻿using Boilerplate.Client.Core.Services;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Tests.ApiTests;

[TestClass]
public partial class IdentityApiTests : ApiTestBase
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var scope = WebApp.Services.CreateAsyncScope();

        var authenticationManager = scope.ServiceProvider.GetRequiredService<AuthenticationManager>();

        await authenticationManager.SignIn(new()
        {
            Email = "test@bitplatform.dev",
            Password = "123456"
        }, default);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var user = await userController.GetCurrentUser(default);

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user.Id);
    }

    [TestMethod]
    public async Task UnauthorizedAccessTest()
    {
        await using var scope = WebApp.Services.CreateAsyncScope();

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExceptionAsync<UnauthorizedException>(() => userController.GetCurrentUser(default));
    }
}
