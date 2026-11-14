using Bit.BlazorUI;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// What <c>AppChatbot</c> reads off the conversation the AI chat panel sends - whether an answer was stopped, the
/// signatures the server wrote on it, the ids its images went up under - is carried by Bit.BlazorUI.AI, twice: the panel
/// keeps the conversation on the device in its snapshot JSON (See AppAiChat.razor.History.cs), and sends it with every
/// question in its wire format (BitAiSignalRTransport, read by BitAiHub). Losing any of it on the way would break no
/// compile: the server would just replay stopped answers, demote every answer to the user's words, or lose the pictures.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class AiChatWireContractTests
{
    [TestMethod]
    public void AStoppedAnswer_Should_ReachTheServerAsStopped()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "what is bit platform?");
        conversation.AddMessage(BitAiRole.Assistant, "bit platform is").SetStatus(BitAiMessageStatus.Cancelled);

        var received = KeptAndSent(conversation);

        Assert.AreEqual(BitAiMessageStatus.Cancelled, received.Messages[1].Status,
            "The server can't tell a stopped answer from a finished one, so it replays truncated answers to the model.");
        Assert.AreEqual("bit platform is", received.Messages[1].GetText(), "What the user saw of a stopped answer must stay.");
    }

    [TestMethod]
    public void WhatTheServerWroteOnAnAnswer_Should_ReachTheServerAsItWroteIt()
    {
        Dictionary<string, object?> written = new()
        {
            [AiChatProtocol.Answer] = "bit platform is a set of tools.",
            [AiChatProtocol.Signature] = "CfDJ8-a-signature-the-server-wrote",
            [AiChatProtocol.Replay] = "Showed a form.\n\nbit platform is a set of tools.",
            [AiChatProtocol.ReplaySignature] = "CfDJ8-another-signature-the-server-wrote"
        };

        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "what is bit platform?");
        conversation.AddMessage(BitAiRole.Assistant, "bit platform is a set of tools.").MergeMetadata(new BitAiGenerationMetadata { Extra = written });

        var received = KeptAndSent(conversation).Messages[1];

        foreach (var (key, value) in written)
        {
            Assert.AreEqual(value, TestAiChatPanel.Extra(received, key),
                $"'{key}' was lost on the way, so the server can't tell its own answers from made up ones and replays them as the user's.");
        }
    }

    [TestMethod]
    public void AnImage_Should_ReachTheServerWithTheIdItWentUpUnder()
    {
        var attachmentId = Guid.NewGuid();

        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "what is in this picture?").AddAttachment(TestAiChatPanel.Image(attachmentId));

        var image = KeptAndSent(conversation).Messages[0].Attachments.Single();

        Assert.AreEqual(attachmentId.ToString(), image.Metadata?.GetValueOrDefault(AiChatProtocol.AttachmentId) switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            _ => null
        }, "The id is all the server reads of an image, so without it the model never sees the picture.");
    }

    [TestMethod]
    public void WhatAQuestionCarriesBesidesTheConversation_Should_ReachTheServer()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "what time is it?");

        var request = new BitAiRequest([.. conversation.Messages])
        {
            Options = new()
            {
                Extra = new Dictionary<string, object?>
                {
                    [AiChatProtocol.CultureId] = "1065",
                    [AiChatProtocol.TimeZoneId] = "Iran Standard Time",
                    [AiChatProtocol.DeviceInfo] = "Windows"
                }
            }
        };

        var received = Sent(request);

        foreach (var (key, value) in request.Options.Extra)
        {
            Assert.AreEqual(value, received.Options?.Extra?.GetValueOrDefault(key) switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
                _ => null
            }, $"'{key}' was lost on the way, so the answer is written without it.");
        }
    }

    /// <summary>
    /// The whole conversation goes up with every question, over a hub that closes the connection on a message past its
    /// size limit - so a long one goes up with as much as the server reads of it (See AiChatHistoryWindow), the question
    /// and what goes with it untouched.
    /// </summary>
    [TestMethod]
    public async Task ALongConversation_Should_GoUpWithTheNewestMessagesTheServerReadsOnly()
    {
        var conversation = new BitAiConversation();

        for (var index = 0; index < AiChatProtocol.MaxHistory + 10; index++)
        {
            conversation.AddMessage(index % 2 == 0 ? BitAiRole.User : BitAiRole.Assistant, $"message {index}");
        }

        var options = new BitAiRequestOptions { Extra = new Dictionary<string, object?> { [AiChatProtocol.CultureId] = "1065" } };

        var server = new RecordingTransport();

        await foreach (var _ in new AiChatHistoryWindow(server).StreamAsync(new BitAiRequest([.. conversation.Messages]) { Options = options }, CancellationToken.None))
        {
        }

        Assert.IsNotNull(server.Request, "The question never went up.");
        Assert.AreSequenceEqual(conversation.Messages.TakeLast(AiChatProtocol.MaxHistory).Select(message => message.MessageId).ToArray(),
                                server.Request.Messages.Select(message => message.MessageId).ToArray(),
                                "The newest messages must go up, in their order, the question last.");
        Assert.AreSame(options, server.Request.Options, "What a question carries besides the conversation must go up with it.");
    }

    /// <summary>The user's decision on a tool call travels through the transport too, so wrapping it must not lose it.</summary>
    [TestMethod]
    public async Task ADecisionOnAToolCall_Should_StillReachTheServer()
    {
        var server = new RecordingTransport();

        var decision = new BitAiApprovalResponse("clear-files", AiChatProtocol.Tools.ClearAppFiles, BitAiApprovalDecision.Rejected);

        await new AiChatHistoryWindow(server).SendApprovalAsync(decision, CancellationToken.None);

        Assert.AreSame(decision, server.Approval, "The decision never reached the server, so the tool waits on it until it gives up.");
    }

    private sealed class RecordingTransport : IBitAiTransport, IBitAiApprovalTransport
    {
        public BitAiRequest? Request { get; private set; }

        public BitAiApprovalResponse? Approval { get; private set; }

        public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken)
        {
            Request = request;

            return AsyncEnumerable.Empty<BitAiEvent>();
        }

        public ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken)
        {
            Approval = response;

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>The conversation as the server reads it after the panel kept it on the device, read it back, and sent it.</summary>
    private static BitAiRequest KeptAndSent(BitAiConversation conversation)
    {
        var kept = BitAiConversationSerializer.FromJson(BitAiConversationSerializer.ToJson(BitAiConversationSerializer.ToSnapshot(conversation)));

        return Sent(new BitAiRequest([.. kept.Messages]));
    }

    private static BitAiRequest Sent(BitAiRequest request)
    {
        return BitAiWireSerializer.ToRequest(BitAiWireSerializer.DeserializeRequest(BitAiWireSerializer.SerializeRequest(BitAiWireSerializer.ToSnapshot(request))));
    }
}
