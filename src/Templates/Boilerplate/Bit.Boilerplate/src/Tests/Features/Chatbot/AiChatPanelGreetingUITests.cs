namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The panel's greeting calls a signed in user by their name, which comes from the cascading <c>CurrentUser</c>. On a
/// fresh load the panel renders first and <c>MainLayout</c> fetches the profile over http afterwards, so a greeting
/// written once on the first render had no name to write (#14685).
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelGreetingUITests : AiChatPanelTestBase
{
    [TestMethod]
    public async Task TheGreeting_Should_CallASignedInUserByName_AfterAFreshLoad()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppAddress, PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SignInPanelUtils.FillCredentials(Page, TestData.DefaultTestEmail, TestData.DefaultTestPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(server.WebAppAddress.ToString());

        // A fresh load, where the panel renders before the profile has arrived.
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        var panel = await OpenChatPanel();

        await Expect(panel).ToContainTextAsync(string.Format(AppStrings.AiChatPanelInitialResponse, $" {TestData.DefaultTestFullName}"));
    }
}
