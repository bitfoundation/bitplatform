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
}
