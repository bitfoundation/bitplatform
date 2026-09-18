using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The conversation is kept on the device (See <c>AppAiChatPanel.RestoreHistory</c>), and its hardest moment is a soft
/// restart: a culture change rebuilds the whole component tree, so the panel reading the conversation back is not the
/// one that wrote it.
/// <para>
/// Signed in on purpose: a rebuilt <c>MainLayout</c> fetches the profile over http, so the cascading
/// <c>CurrentUser</c> is null meanwhile - and a conversation stored under an account, read back by a panel that
/// believes nobody is signed in, belongs to somebody else.
/// </para>
/// <para>
/// A real browser is the only place for this: IndexedDB, the rebuild and the identity's timing are the framework's
/// doing rather than the panel's.
/// </para>
/// <para>
/// There is only one culture to be in when invariant globalization is on (See <c>CultureInfoManager.InvariantGlobalization</c>),
/// so nothing changes it and no soft restart happens - the test is inconclusive on such a build.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelHistoryUITests : AiChatPanelTestBase
{
    /// <summary>Registered by <c>AIFunctionFactory.Create</c>, so a rename breaks the scripted call rather than the build.</summary>
    private const string SetApplicationCultureTool = "SetApplicationCulture";

    /// <summary>fa-IR: a different culture from the default, and right-to-left, so <c>CurrentDir</c> changes too.</summary>
    private const int PersianLcid = 1065;

    private const string PersianCulture = "fa-IR";

    private const string StoreAdminEmail = "store-admin@bitplatform.dev";
    private const string StoreAdminPassword = "123456";

    [TestMethod]
    public async Task ASignedInUsersConversation_Should_SurviveTheSoftRestartOfACultureChange()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("A culture change, and so the soft restart this test is about, only exists when invariant globalization is disabled.");
            return;
        }

        var chatClient = new TestChatClient();

        await using var server = new AppTestServer(Context);

        await server.Build(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configuration =>
        {
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        await SignInWithPassword(server.WebAppServerAddress, StoreAdminEmail, StoreAdminPassword);

        const string question = "How do I install the PWA version of this app?";
        const string answer = "Open your browser's menu and choose Install.";

        chatClient.StreamingUpdates = (_, conversation) =>
        {
            // Once the culture tool has run, the scripted model stops calling tools, like a real one.
            if (conversation.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any())
                return [new ChatResponseUpdate(ChatRole.Assistant, "Done, the language is changed.")];

            // The second question is the one that asks for a language, and it is the only one that gets the tool.
            if (conversation.Any(message => message.Text.Contains("language", StringComparison.OrdinalIgnoreCase)))
                return
                [
                    new ChatResponseUpdate(ChatRole.Assistant, (IList<AIContent>)
                    [
                        new FunctionCallContent(callId: "culture-call-1", name: SetApplicationCultureTool,
                            arguments: new Dictionary<string, object?> { ["cultureLcid"] = PersianLcid })
                    ])
                    {
                        FinishReason = ChatFinishReason.ToolCalls
                    }
                ];

            return [new ChatResponseUpdate(ChatRole.Assistant, answer)];
        };

        var panel = await OpenChatPanel();

        await SendChatMessage(panel, question, chatClient);
        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();

        await SendFollowUpMessage(panel, "please change the app language to Persian", chatClient);

        // The soft restart, observed rather than waited out: the culture reaches the document only after the tool ran
        // and the tree was rebuilt.
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", PersianCulture);

        // Reloaded rather than reopened, so nothing held in memory can answer for it: what appears below came off the
        // device. A fresh page is also where the identity arrives late, so the panel restores while CurrentUser is
        // still null.
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        panel = await OpenChatPanel();

        await Expect(panel.GetByText(question)).ToBeVisibleAsync();
        await Expect(panel.GetByText(answer)).ToBeVisibleAsync();
    }

    private async Task SignInWithPassword(Uri serverAddress, string email, string password)
    {
        await Page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(serverAddress.ToString());
    }
}
