using Microsoft.Playwright.MSTest;

namespace Boilerplate.Tests;

[TestClass]
public partial class IdentityPagesTests : PageTest
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();

        await Page.GotoAsync(new Uri(server.GetServerAddress(), Urls.ProfilePage).ToString());

        await Expect(Page).ToHaveTitleAsync(AppStrings.SignIn);
    }
}
