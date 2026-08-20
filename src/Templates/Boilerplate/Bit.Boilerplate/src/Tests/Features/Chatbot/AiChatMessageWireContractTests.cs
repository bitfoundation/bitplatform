using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// <c>AiChatMessageResponse.Successful</c> is the typed signal for "this answer was cancelled or failed". The client sets it
/// and renders it as the "Canceled" tag, and the server drops those turns from the history it resends to the model
/// (See <see cref="AppChatbotHistoryTests"/>) - but only if the flag actually crosses the wire. It carried a
/// <c>[JsonIgnore]</c> for exactly as long as the flag was a client-only concern, and putting it back would not break
/// a single compile: the server would simply start treating every resent turn as successful again and replay
/// truncated answers to the model after every reconnect.
/// <para>
/// Both shapes are asserted because the two ends of this hub method do not share one: the server's payload
/// serializer is configured with <c>ApplyDefaultOptions</c> (See <c>AddJsonProtocol</c> in <c>Program.Services</c>)
/// while the client feeds its <c>AppJsonContext</c> resolver chain into its own.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class AiChatMessageWireContractTests
{
    private static JsonSerializerOptions HubPayloadOptions
    {
        get
        {
            var options = new JsonSerializerOptions();
            options.ApplyDefaultOptions();
            return options;
        }
    }

    [TestMethod]
    public void AnUnsuccessfulTurn_Should_ReachTheServerAsUnsuccessful()
    {
        var request = new StartChatRequest
        {
            ChatMessagesHistory =
            [
                new() { Role = AiChatMessageRole.User, Content = "what is bit platform?" },
                new() { Role = AiChatMessageRole.Assistant, Content = "bit platform is", Successful = false }
            ]
        };

        foreach (var (name, json) in new[]
        {
            ("Hub payload options", JsonSerializer.Serialize(request, HubPayloadOptions)),
            (nameof(AppJsonContext), JsonSerializer.Serialize(request, AppJsonContext.Default.StartChatRequest))
        })
        {
            Assert.Contains("successful", json, StringComparison.OrdinalIgnoreCase,
                $"{name} does not put AiChatMessageResponse.Successful on the wire, so the server cannot tell a canceled answer from a real one and will replay truncated answers to the model. Payload: {json}");

            var received = JsonSerializer.Deserialize<StartChatRequest>(json, HubPayloadOptions)!;

            Assert.IsFalse(received.ChatMessagesHistory[1].Successful,
                $"{name} lost the flag on the way back in. Payload: {json}");
        }
    }

    /// <summary>
    /// The property defaults to <c>true</c>, and that default is load bearing in the other direction: a payload that
    /// omits it - an older client, or any other caller of this hub method - must have its history kept, not silently
    /// discarded as one long string of canceled answers.
    /// </summary>
    [TestMethod]
    public void ATurnThatOmitsTheFlag_Should_BeTreatedAsSuccessful()
    {
        const string json = /*lang=json,strict*/ """
        {
            "chatMessagesHistory": [
                { "role": 0, "content": "what is bit platform?" },
                { "role": 1, "content": "bit platform is a set of tools." }
            ]
        }
        """;

        var received = JsonSerializer.Deserialize<StartChatRequest>(json, HubPayloadOptions)!;

        Assert.IsTrue(received.ChatMessagesHistory.All(message => message.Successful),
            "A turn that does not carry the flag must be treated as a completed one, otherwise a client that does not send it loses its whole conversation on reconnect.");
    }
}
