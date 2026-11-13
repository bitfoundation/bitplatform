using Boilerplate.LandingPage.Server.Features.Chat;
using Microsoft.Extensions.AI;

namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class ChatModelTests
{
    private static readonly RecordingChatClient model = new();
    private static LandingTestServer server = default!;

    [ClassInitialize]
    public static async Task Start(TestContext testContext)
    {
        server = await new LandingTestServer().Start(after: services => services.AddSingleton<IChatClient>(model));
    }

    [ClassCleanup]
    public static async Task Stop()
    {
        await server.DisposeAsync();
    }

    [TestMethod]
    public async Task TheModel_Should_GetTheChatOptionsOfTheSettingsOnly()
    {
        await using var scope = server.Services.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<LandingChatService>();

        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "What does it cost?");

        var answer = "";
        await foreach (var evt in chat.StreamAsync(new(conversation.Messages)
        {
            Context = new Dictionary<string, object?> { [LandingChatContext.Site] = LandingSites.All[0].Key, [LandingChatContext.Culture] = "en-US" }
        }, CancellationToken.None))
        {
            answer += (evt as BitAiTextDeltaEvent)?.Delta;
        }

        Assert.AreEqual("It costs nothing.", answer);
        Assert.IsNotNull(model.Options);
        Assert.IsNull(model.Options.Temperature, "gpt-5.6-luna, the default model, refuses any temperature but its own.");
        Assert.IsNotNull(model.Options.Reasoning);
        Assert.AreEqual(ReasoningEffort.None, model.Options.Reasoning.Effort);
        Assert.AreEqual(600, model.Options.MaxOutputTokens);
    }

    [TestMethod]
    public async Task ALongConversation_Should_BeAnsweredFromItsNewestTurns()
    {
        await using var scope = server.Services.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<LandingChatService>();

        // The browser keeps the conversation and a visitor may carry on far past what one question sends the model
        var conversation = new BitAiConversation();
        foreach (var exchange in Enumerable.Range(1, 15))
        {
            conversation.AddMessage(BitAiRole.User, $"Question {exchange}?");
            conversation.AddMessage(BitAiRole.Assistant, $"Answer {exchange}.");
        }
        conversation.AddMessage(BitAiRole.User, "And the last one?");

        var answer = "";
        await foreach (var evt in chat.StreamAsync(Question(conversation), CancellationToken.None))
        {
            answer += (evt as BitAiTextDeltaEvent)?.Delta;
        }

        Assert.AreEqual("It costs nothing.", answer);

        var turns = model.Messages.Where(message => message.Role != ChatRole.System).ToArray();
        Assert.HasCount(LandingChatContext.MaxTurns, turns);
        Assert.AreEqual("And the last one?", turns[^1].Text);
    }

    [TestMethod]
    public async Task UnderBlazorServer_Questions_Should_CountAgainstTheSameAllowance()
    {
        await using var scope = server.Services.CreateAsyncScope();
        var backend = scope.ServiceProvider.GetRequiredService<ILandingChatBackend>();

        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "What does it cost?");
        var request = BitAiWireSerializer.SerializeRequest(BitAiWireSerializer.ToSnapshot(Question(conversation)));

        async Task<List<BitAiEvent>> Ask()
        {
            List<BitAiEvent> events = [];

            await foreach (var item in backend.StreamAsync(request, CancellationToken.None))
            {
                events.Add(BitAiWireSerializer.Deserialize(item)!);
            }

            return events;
        }

        // The socket's allowance (See LandingChatEndpoints.AddLandingChat)
        foreach (var _ in Enumerable.Range(1, 30))
        {
            Assert.IsInstanceOfType<BitAiMessageCompletedEvent>((await Ask())[^1]);
        }

        var refused = (BitAiErrorEvent)(await Ask()).Single();
        Assert.IsFalse(refused.Error.IsRetryable);
        Assert.AreEqual("That's a lot of questions in a short time. Please wait a few minutes, or contact the team directly.", refused.Error.Message);
    }

    private static BitAiRequest Question(BitAiConversation conversation) => new(conversation.Messages)
    {
        Context = new Dictionary<string, object?> { [LandingChatContext.Site] = LandingSites.All[0].Key, [LandingChatContext.Culture] = "en-US" }
    };
}
