using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The chat scrolls its own transcript and keeps the message box at the bottom of the panel. The panel around it scrolls
/// its body too, so anything of the chat that sticks out of that body by even a few pixels gives it a second scrollbar
/// beside the transcript's - and scrolling the outer one moves the message box away.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AiChatPanelLayoutUITests : AiChatPanelTestBase
{
    [TestMethod]
    [DataRow(360, 780, DisplayName = "Phone")]
    [DataRow(1280, 800, DisplayName = "Desktop")]
    public async Task AFullTranscript_Should_ScrollOnItsOwn_WithoutThePanelScrollingAroundIt(int width, int height)
    {
        var chatClient = new TestChatClient
        {
            // Long enough to overflow any panel, so the transcript has to scroll.
            StreamingUpdates = (_, _) => [new ChatResponseUpdate(ChatRole.Assistant,
                string.Join("\n\n", Enumerable.Range(1, 40).Select(line => $"Line {line} of a long answer.")))]
        };

        await using var server = new AppTestServer(Context);

        await server.Build(configureTestServices: services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configureTestConfiguration: configuration =>
        {
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        await Page.SetViewportSizeAsync(width, height);
        await Page.GotoAsync(new Uri(server.WebAppAddress, PageUrls.Home).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var panel = await OpenChatPanel();

        await SendChatMessage(panel, "tell me a long story", chatClient);
        await Expect(panel.GetByText("Line 40 of a long answer.")).ToBeAttachedAsync();

        // How far the panel's body could be scrolled, and what of it sticks out, so a failure says what to fix.
        var overflow = await panel.Locator(".panel-body").EvaluateAsync<string>("""
            body => {
                const extra = body.scrollHeight - body.clientHeight;
                if (extra <= 1) return '';
                const bottom = body.getBoundingClientRect().bottom;
                const culprits = [...body.querySelectorAll('*')]
                    .filter(element => element.getBoundingClientRect().bottom > bottom + 1)
                    .map(element => `${element.tagName.toLowerCase()}.${[...element.classList].join('.')} (+${Math.round(element.getBoundingClientRect().bottom - bottom)}px)`);
                return `${extra}px: ${culprits.join(', ')}`;
            }
            """);

        Assert.AreEqual(string.Empty, overflow, "The panel's body scrolls around the transcript, which already scrolls on its own.");

        // The transcript itself does scroll, so the check above is about the right element.
        var transcriptScrolls = await panel.Locator("[data-message-id]").Last.EvaluateAsync<bool>("""
            message => {
                for (let element = message.parentElement; element; element = element.parentElement) {
                    const overflowY = getComputedStyle(element).overflowY;
                    if (overflowY === 'auto' || overflowY === 'scroll') return element.scrollHeight > element.clientHeight;
                }
                return false;
            }
            """);

        Assert.IsTrue(transcriptScrolls, "The transcript should be what scrolls a conversation longer than the panel.");
    }
}
