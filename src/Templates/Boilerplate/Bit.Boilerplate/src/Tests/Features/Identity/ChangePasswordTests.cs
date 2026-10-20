using Microsoft.AspNetCore.Identity;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest")]
public class ChangePasswordTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ChangePassword_Should_AcceptACurrentPasswordShorterThanANewOneMustBe()
    {
        const string currentPassword = "123456";

        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        await using (var scope = server.ApiApp.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user!.PasswordHash = userManager.PasswordHasher.HashPassword(user, currentPassword);
            await userManager.UpdateAsync(user);
        }

        await client.GetController<IUserController>().ChangePassword(new()
        {
            OldPassword = currentPassword,
            NewPassword = TestData.DefaultTestPassword,
            ConfirmPassword = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        await using (var scope = server.ApiApp.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            Assert.IsTrue(await userManager.CheckPasswordAsync(user!, TestData.DefaultTestPassword));
        }
    }
}
