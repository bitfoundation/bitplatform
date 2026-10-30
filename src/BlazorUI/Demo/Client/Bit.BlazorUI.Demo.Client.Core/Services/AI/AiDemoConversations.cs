namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// Builds conversations for the demos that need history rather than a live model.
/// </summary>
public static class AiDemoConversations
{
    /// <summary>
    /// Builds a long, heterogeneous conversation: prose, code, tables, tool calls, and citations.
    /// </summary>
    /// <param name="messageCount">How many messages to build.</param>
    /// <returns>The conversation.</returns>
    /// <remarks>
    /// Deliberately varied. A thousand identical paragraphs would virtualize beautifully and prove nothing; the hard case
    /// is mixed content with wildly different heights.
    /// </remarks>
    public static BitAiConversation Long(int messageCount)
    {
        var conversation = new BitAiConversation();
        conversation.SetTitle($"{messageCount} message conversation");

        var source = new BitAiCitationSource("s1", "Performance notes")
        {
            Kind = BitAiSourceKind.Document,
            SiteName = "Internal wiki",
            Url = "https://example.com/perf",
        };

        for (var i = 0; i < messageCount; i += 2)
        {
            conversation.AddMessage(BitAiRole.User, $"Question {i / 2 + 1}: what happens at scale here?");

            var assistant = new BitAiMessage(BitAiRole.Assistant);

            switch ((i / 2) % 4)
            {
                case 0:
                    assistant.AddPart(new BitAiTextPart(
                        $"Answer {i / 2 + 1}. The cost is dominated by rendering, not by parsing. " +
                        "Coalescing updates removes most of it, and virtualization removes the rest."));
                    break;

                case 1:
                    assistant.AddPart(new BitAiMarkdownPart(
                        "Here is the shape of it:\n\n" +
                        "```csharp\n" +
                        $"var budget = TimeSpan.FromMilliseconds({16 + (i % 40)});\n" +
                        "scheduler.Request();\n" +
                        "```\n\n" +
                        "The interval is the only dial that matters."));
                    break;

                case 2:
                    var query = $"scale {i}";
                    var call = assistant.AddPart(new BitAiToolCallPart(BitAiId.NewToolCallId(), "search_docs"));
                    call.CompleteArguments(AiScript.Json($$"""{"query":"{{query}}"}"""));
                    call.SetResult(BitAiToolOutcome.Success(AiDemoCatalog.SearchHits(query), AiDemoJsonContext.Default.SearchResult));
                    assistant.AddPart(new BitAiTextPart("Two passages matched, and they say the same thing."));
                    break;

                default:
                    assistant.AddPart(new BitAiMarkdownPart(
                        "| Messages | Rendered | Notes |\n" +
                        "| --- | ---: | --- |\n" +
                        "| 60 | all | below the threshold |\n" +
                        "| 500 | visible only | virtualized |\n"));
                    assistant.AddCitation(new BitAiCitation(BitAiId.New("cit"), source));
                    break;
            }

            assistant.MergeMetadata(new BitAiGenerationMetadata
            {
                ModelId = AiDemoCatalog.DefaultModelId,
                ContextWindow = 200_000,
                Usage = new BitAiTokenUsage { InputTokens = 120 + i, OutputTokens = 60 + (i % 90) },
            });

            conversation.AddMessage(assistant);
        }

        return conversation;
    }
}
