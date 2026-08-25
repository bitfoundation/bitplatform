using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The chatbot's whole point is that it can act on the app, not just talk about it - and the path from "the model
/// decided to call a tool" to "the user's screen changed" runs through six components that no other test crosses at
/// once. This one drives it end to end in a real browser:
/// <list type="number">
/// <item>The user opens the chat panel on the public home page and sends a message (real <c>AppAiChatPanel</c>, real <c>hubConnection.StreamAsync</c>).</item>
/// <item><c>AppHub.StartChat</c> receives it and hands it to the real scoped <c>AppChatbot</c>, which resolves the real keyed SupportAgent with its real seeded system prompt and its real tool set.</item>
/// <item>The model - and ONLY the model - is scripted: it answers with a <c>FunctionCallContent</c> for <c>SetApplicationTheme</c>, exactly as a real one would.</item>
/// <item><c>FunctionInvokingChatClient</c> dispatches that to the real <c>AppChatbot.SetApplicationTheme</c>, which validates the argument and calls back through <c>IHubContext.Clients.Client(connectionId).InvokeAsync&lt;bool&gt;</c>.</item>
/// <item><c>AppClientCoordinator</c>'s <c>CHANGE_THEME</c> handler runs <c>ThemeService.ToggleTheme</c> in the browser, which is observable as the <c>bit-theme</c> attribute on <c>&lt;html&gt;</c> (See <see cref="Theme.ThemeTogglePersistenceUITests"/>).</item>
/// <item>The tool's return value goes back into the conversation, the scripted model turns it into prose, and that prose is streamed to the panel and rendered.</item>
/// </list>
/// <para>
/// Scripting the model is what makes this a test rather than a demo: a real model may or may not decide to call the
/// tool, may call it with "Dark" or "light mode", and costs money per run. Everything the template actually owns -
/// the tool, the hub round trip, the client handler, the streaming - is real.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelThemeUITests : AiChatPanelTestBase
{
    /// <summary>
    /// The name <c>AIFunctionFactory.Create(SetApplicationTheme)</c> registers the tool under (See
    /// <c>AppChatbot.GetAIFunctions</c>). It cannot be a <c>nameof</c> from here because the method is private; if it
    /// is ever renamed, the scripted call finds no matching tool, no theme change happens, and this test fails.
    /// </summary>
    private const string SetApplicationThemeTool = "SetApplicationTheme";

    [TestMethod]
    public async Task Chatbot_Should_ChangeTheAppTheme_WhenTheModelCallsTheTool()
    {
        var chatClient = new TestChatClient();

        await using var server = new AppTestServer(Context);

        await server.Build(services =>
        {
            // The one and only fake in this test. See TestChatClient for why this seam - and not one of the
            // template's own interfaces - is the right place to cut.
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configuration =>
        {
            // Without a chat api key the AI agents are never registered and the panel would have nothing to talk to.
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Home).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var htmlElement = Page.Locator("html");

        // Read the starting theme instead of assuming one, then ask for the opposite - so the test proves a CHANGE
        // rather than passing because the app already happened to be in the requested theme.
        var initialTheme = await htmlElement.GetAttributeAsync("bit-theme");
        Assert.IsFalse(string.IsNullOrEmpty(initialTheme), "The <html> element should carry a bit-theme attribute.");
        var requestedTheme = initialTheme == "dark" ? "light" : "dark";

        const string finalAnswer = "Done, I have switched the theme for you.";

        chatClient.StreamingUpdates = (_, conversation) =>
        {
            // Second time round the tool has already run and its result is in the conversation, so the scripted
            // model does what a real one does at that point: it stops calling tools and answers the user.
            if (conversation.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any())
                return [new ChatResponseUpdate(ChatRole.Assistant, finalAnswer)];

            return
            [
                new ChatResponseUpdate(ChatRole.Assistant, (IList<AIContent>)
                [
                    new FunctionCallContent(callId: "theme-call-1", name: SetApplicationThemeTool,
                        arguments: new Dictionary<string, object?> { ["theme"] = requestedTheme })
                ])
                {
                    FinishReason = ChatFinishReason.ToolCalls
                }
            ];
        };

        var panel = await OpenChatPanel();

        await SendChatMessage(panel, "please switch the app theme", chatClient);

        // The payoff: the browser's own theme attribute, changed by nothing but the chatbot.
        await Expect(htmlElement).ToHaveAttributeAsync("bit-theme", requestedTheme);

        // The answer written after the tool ran reaches the panel, which proves the second half of the round trip -
        // the tool result went back to the model and its prose was streamed to the user.
        await Expect(panel.GetByText(finalAnswer)).ToBeVisibleAsync();

        // The tool really executed on the server rather than the theme changing by some other route: its return
        // value was fed back into the conversation. SetApplicationTheme returns "Theme changed to X successfully"
        // only when the client reported an actual change.
        // Every conversation is searched rather than the last one, because the hub also asks for follow-up
        // suggestions (a separate agent, a separate conversation) and which of the two lands last is a race.
        var toolResults = chatClient.ReceivedConversations
            .SelectMany(conversation => conversation)
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Select(result => result.Result?.ToString())
            .ToArray();

        Assert.Contains(result => result?.Contains($"changed to {requestedTheme}", StringComparison.OrdinalIgnoreCase) is true, toolResults,
            $"The SetApplicationTheme tool did not report a successful change back to the model. Results: [{string.Join(", ", toolResults)}].");
    }

}
