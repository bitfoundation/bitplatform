using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The scripted responses behind the interactive scenarios: generative UI, a proposed form, an error that recovers on
/// retry, metered usage, and slash commands with mentions.
/// </summary>
public static partial class AiDemoInteractiveTransports
{
    /// <summary>
    /// Produces a component the application registered, bound to model-supplied properties.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport GenerativeUi() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            await yielder(new BitAiMessageStartedEvent());

            const string intro = "Here is the week ahead for the three cities you asked about.\n\n";
            await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

            string[] cities =
            [
                """{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""",
                """{"city":"Lisbon","degrees":17,"condition":"Clear","humidity":54}""",
                """{"city":"Auckland","degrees":22,"condition":"Showers","humidity":72}""",
            ];

            foreach (var json in cities)
            {
                await AiScript.Pause(350, token);
                await yielder(new BitAiGenerativeUiEvent("forecast_card", AiScript.Json(json)));
            }

            const string outro = "\n\nOslo is the outlier. If the trip is flexible, move it a month.";
            await foreach (var evt in AiScript.Write(outro, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Proposes a form for the user to check before anything is booked.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport ProposesForm() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            await yielder(new BitAiMessageStartedEvent());

            const string intro =
                "I can book that. Check the details first, since I inferred two of them.\n\n";
            await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

            const string schema = """
                {
                  "title": "Book a table",
                  "type": "object",
                  "required": ["restaurant", "guests", "date"],
                  "properties": {
                    "restaurant": { "type": "string", "title": "Restaurant" },
                    "guests": { "type": "integer", "title": "Guests", "minimum": 1, "maximum": 12 },
                    "date": { "type": "string", "format": "date", "title": "Date" },
                    "seating": { "type": "string", "enum": ["Inside", "Terrace", "No preference"] },
                    "notes": { "type": "string", "format": "textarea", "title": "Notes for the restaurant" }
                  }
                }
                """;

            foreach (var slice in AiScript.Slice(schema, 30))
            {
                await AiScript.Pause(50, token);
                await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "booking_form" });
            }

            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Fails the first time and succeeds on retry, so error recovery can be seen working.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport FailsThenRecovers()
    {
        var attempts = 0;

        return new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            await yielder(new BitAiMessageStartedEvent());

            if (Interlocked.Increment(ref attempts) == 1)
            {
                await foreach (var evt in AiScript.Write("Looking that up", 4, 25, token)) await yielder(evt);
                await AiScript.Pause(400, token);

                await yielder(new BitAiErrorEvent(new BitAiError("The model is rate limited right now.")
                {
                    Code = "rate_limit",
                    Kind = BitAiErrorKind.RateLimit,
                    IsRetryable = true,
                    RetryAfter = TimeSpan.FromSeconds(3),
                    Detail = "HTTP 429\nx-ratelimit-remaining: 0\nretry-after: 3",
                }));

                return;
            }

            const string answer =
                "That worked on the second attempt. The first request was rate limited, which the " +
                "session surfaced as a retryable error rather than an exception.";

            await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiMessageCompletedEvent());
        }));
    }

    /// <summary>
    /// Reports usage, cost, and context size on every turn.
    /// </summary>
    /// <param name="model">The model the turn runs on.</param>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Metered(BitAiModel model) =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            await yielder(new BitAiMessageStartedEvent());
            await yielder(new BitAiMetadataEvent(new BitAiGenerationMetadata
            {
                ModelId = model.ModelId,
                ProviderId = model.ProviderId,
                ContextWindow = model.ContextWindow,
                Temperature = 0.7,
            }));

            var answer =
                $"This turn ran on {model.Name}. The readout under the message is real accounting: " +
                "input and output tokens reported by the transport, priced with the model rates in " +
                "the catalog, and timed by the session.";

            await foreach (var evt in AiScript.Write(answer, 4, 10, token)) await yielder(evt);

            var input = request.Messages.Sum(m => m.GetText().Length) / 4;
            var output = answer.Length / 4;

            await yielder(new BitAiUsageUpdatedEvent(new BitAiTokenUsage
            {
                InputTokens = input,
                OutputTokens = output,
                CachedInputTokens = input / 3,
                Cost = model.EstimateCost(new BitAiTokenUsage { InputTokens = input, OutputTokens = output }),
                Currency = model.Currency,
            }));

            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Answers prompts written with slash commands and @-mentions, naming what was referenced.
    /// </summary>
    /// <returns>The transport.</returns>
    /// <remarks>
    /// A mention arrives as plain text, <c>@roadmap.md</c>. Resolving it to a file, a person, or a data source is the
    /// application's job, which is why the composer hands over identifiers and labels and not content.
    /// </remarks>
    public static IBitAiTransport Commands() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            await yielder(new BitAiMessageStartedEvent());

            var text = AiScript.LastUserText(request);
            var mentions = MentionRegex().Matches(text)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var answer = text.StartsWith("Summarize", StringComparison.OrdinalIgnoreCase)
                ? $"""
                  So far:

                  - {request.Messages.Count(m => m.Role.IsUser)} messages from you, {request.Messages.Count(m => m.Role.IsAssistant)} from me.
                  - Nothing has been decided yet.
                  - The next step is yours.
                  """
                : mentions.Length > 0
                    ? "You referenced " + string.Join(", ", mentions.Select(m => $"**{m}**")) +
                      ". A real application resolves each one by identifier and puts its content into the prompt; " +
                      "the model never has to guess which file or person you meant."
                    : "Type `/` for commands or `@` to reference a file, a person, or a data source.";

            await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
            await yielder(AiScript.Usage(request, answer));
            await yielder(new BitAiMessageCompletedEvent());
        }));

    [GeneratedRegex(@"(?<=^|\s)@([\w.\-]+)")]
    private static partial Regex MentionRegex();
}
