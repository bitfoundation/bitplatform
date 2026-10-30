using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.TestHost.AI;

/// <summary>
/// The replies <see cref="E2EAiTransport"/> plays, one per name. Each is deterministic: the same text, ids and numbers on
/// every run (the ids are numbered by the turn of the conversation, so a script played twice in one conversation does not
/// reuse one), and no pause between its events unless the page asks for one (<see cref="E2EAiScriptSelection.DelayMs"/>).
/// </summary>
/// <remarks>
/// The tools the scripts call are the ones <see cref="E2EAiTools"/> registers; the session runs them and asks again, and
/// the script answers from what they returned. <see cref="Throw"/> is the one script whose failure the session logs as a
/// server error (a transport that throws), so a test playing it allows that error.
/// </remarks>
public static class E2EAiScripts
{
    /// <summary>"You said: ..." a few characters at a time, then the usage.</summary>
    public const string Echo = "echo";

    /// <summary>Headings, emphasis, a list, a table, a link, inline code, a quote and a fenced C# block, with metadata.</summary>
    public const string Markdown = "markdown";

    /// <summary>A sentence and one fenced C# code block.</summary>
    public const string Code = "code";

    /// <summary>Visible reasoning, then the answer.</summary>
    public const string Reasoning = "reasoning";

    /// <summary>A status line ("Searching the documentation"), cleared before the answer.</summary>
    public const string Status = "status";

    /// <summary>A get_weather call for Oslo, its arguments streamed; the session runs it, then the answer reads its result.</summary>
    public const string Tool = "tool";

    /// <summary>A delete_records call, destructive, so it waits for the user's approval; the answer follows the decision.</summary>
    public const string ToolApproval = "tool-approval";

    /// <summary>Two get_weather calls in one round (Oslo and Lisbon), then one answer.</summary>
    public const string ToolParallel = "tool-parallel";

    /// <summary>A search_docs call that reports its progress and returns citations.</summary>
    public const string ToolProgress = "tool-progress";

    /// <summary>A flaky_service call whose tool fails, then an answer that says so.</summary>
    public const string ToolError = "tool-error";

    /// <summary>A web_search the provider ran itself: the call and its result both stream in, nothing runs in the app.</summary>
    public const string ToolResult = "tool-result";

    /// <summary>An agent run: a plan of four steps, each started and completed, then the answer.</summary>
    public const string Agent = "agent";

    /// <summary>An agent run that stops at its second step to ask the user, with suggestions ("Tonight", "Saturday morning",
    /// "Ask me later"); the next turn - the answer - finishes it ("Booked for ...").</summary>
    public const string AgentInput = "agent-input";

    /// <summary>An agent run whose second step fails, so the run fails, then an answer explaining it.</summary>
    public const string AgentFail = "agent-fail";

    /// <summary>Three sentences, the first two each anchored to a citation of a source of their own.</summary>
    public const string Citations = "citations";

    /// <summary>A Markdown document artifact streamed in; the next turn revises it into a second version.</summary>
    public const string Artifact = "artifact";

    /// <summary>A C# code artifact streamed in.</summary>
    public const string ArtifactCode = "artifact-code";

    /// <summary>An array of four objects streamed as structured data (schema "options"), which renders as a table.</summary>
    public const string Structured = "structured";

    /// <summary>A JSON schema streamed as structured data (schema "form"), which the host renders as a BitAiForm.</summary>
    public const string Form = "form";

    /// <summary>Two e2e_card components of generative UI (see <see cref="E2EAiCard"/>) between two sentences.</summary>
    public const string Generative = "generative";

    /// <summary>A component of generative UI no one registered (not_registered).</summary>
    public const string GenerativeUnknown = "generative-unknown";

    /// <summary>A sentence and an image (a 1x1 PNG as a data URL, 64 by 48).</summary>
    public const string Image = "image";

    /// <summary>A sentence and a file (report.pdf, 2 KB).</summary>
    public const string File = "file";

    /// <summary>A few words, then a retryable rate-limit error that ends the turn.</summary>
    public const string Error = "error";

    /// <summary>The rate-limit error on the first attempt and an answer on the retry.</summary>
    public const string ErrorThenRecover = "error-then-recover";

    /// <summary>A transport that throws: the session turns it into an error (and logs it on the server).</summary>
    public const string Throw = "throw";

    /// <summary>Metadata (model, provider, context window) and usage with cached tokens and a cost.</summary>
    public const string Usage = "usage";

    /// <summary>Thirty paragraphs, for a transcript taller than its box.</summary>
    public const string Long = "long";

    /// <summary>One sentence, then nothing until the turn is stopped.</summary>
    public const string Hang = "hang";

    /// <summary>A message that starts and completes with nothing in it.</summary>
    public const string Empty = "empty";

    /// <summary>Reasoning, a status, cited text, an artifact, structured data, generative UI and usage in one reply.</summary>
    public const string Everything = "everything";

    private static readonly Dictionary<string, Func<E2EAiScriptRun, IAsyncEnumerable<BitAiEvent>>> Scripts = new(StringComparer.OrdinalIgnoreCase)
    {
        [Echo] = PlayEcho,
        [Markdown] = PlayMarkdown,
        [Code] = PlayCode,
        [Reasoning] = PlayReasoning,
        [Status] = PlayStatus,
        [Tool] = PlayTool,
        [ToolApproval] = PlayToolApproval,
        [ToolParallel] = PlayToolParallel,
        [ToolProgress] = PlayToolProgress,
        [ToolError] = PlayToolError,
        [ToolResult] = PlayToolResult,
        [Agent] = PlayAgent,
        [AgentInput] = PlayAgentInput,
        [AgentFail] = PlayAgentFail,
        [Citations] = PlayCitations,
        [Artifact] = PlayArtifact,
        [ArtifactCode] = PlayArtifactCode,
        [Structured] = PlayStructured,
        [Form] = PlayForm,
        [Generative] = PlayGenerative,
        [GenerativeUnknown] = PlayGenerativeUnknown,
        [Image] = PlayImage,
        [File] = PlayFile,
        [Error] = PlayError,
        [ErrorThenRecover] = PlayErrorThenRecover,
        [Throw] = PlayThrow,
        [Usage] = PlayUsage,
        [Long] = PlayLong,
        [Hang] = PlayHang,
        [Empty] = PlayEmpty,
        [Everything] = PlayEverything,
    };

    /// <summary>Every script's name.</summary>
    public static IReadOnlyCollection<string> Names => Scripts.Keys;

    /// <summary>Whether a script of that name exists (case-insensitive).</summary>
    public static bool IsScript(string? name) => name is not null && Scripts.ContainsKey(name);

    /// <summary>The script the first word of a user message names, or null.</summary>
    public static string? Named(string? text)
    {
        var word = text?.TrimStart().Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return word is not null && Scripts.ContainsKey(word) ? word.ToLowerInvariant() : null;
    }

    /// <summary>Plays a script.</summary>
    public static IAsyncEnumerable<BitAiEvent> Play(string name, E2EAiScriptRun run) =>
        Scripts.TryGetValue(name, out var script) ? script(run) : throw new ArgumentException($"No E2E AI script is named '{name}'.", nameof(name));

    // ---- text ----

    private static async IAsyncEnumerable<BitAiEvent> PlayEcho(E2EAiScriptRun run)
    {
        var reply = $"You said: {run.LastUserText}";

        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write(reply)) yield return e;
        yield return run.Usage(reply);
        yield return new BitAiMessageCompletedEvent();
    }

    private const string MarkdownText = """
        # Render modes

        Blazor renders in **two places**, and the difference *matters*.

        ## Where the code runs

        - Server, over a circuit
        - WebAssembly, in the browser
        - Auto, the first then the second

        | Mode | Runs on | First paint |
        | --- | --- | :---: |
        | Server | The server | Fast |
        | WebAssembly | The browser | Slower |

        Read [the render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes) or call `StateHasChanged`.

        > Rendering more often than the eye can follow is not smoother.

        ```csharp
        var total = items.Sum(item => item.Price);
        Console.WriteLine(total);
        ```
        """;

    private static async IAsyncEnumerable<BitAiEvent> PlayMarkdown(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        yield return new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = E2EAiModels.DefaultModelId, ProviderId = "e2e", ContextWindow = 128_000 });
        await foreach (var e in run.Write(MarkdownText, 8)) yield return e;
        yield return run.Usage(MarkdownText);
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayCode(E2EAiScriptRun run)
    {
        const string reply = """
            Here is the function:

            ```csharp
            public static int Add(int a, int b)
            {
                return a + b;
            }
            ```
            """;

        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write(reply, 8)) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayReasoning(E2EAiScriptRun run)
    {
        const string thinking = "The user asks about the cost of a round trip. I should separate the fixed cost from the per-token cost.";
        const string answer = "Two costs are worth separating: the connection, and the generation.";

        yield return new BitAiMessageStartedEvent();
        foreach (var word in thinking.Split(' '))
        {
            await run.Pause();
            yield return new BitAiReasoningDeltaEvent(word + " ");
        }
        await foreach (var e in run.Write(answer)) yield return e;
        yield return new BitAiMessageCompletedEvent(BitAiFinishReason.Stop);
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayStatus(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        yield return new BitAiStatusEvent("Searching the documentation");
        await run.Pause();
        yield return new BitAiStatusEvent(null);
        await foreach (var e in run.Write("I found what you were looking for.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    // ---- tools ----

    private static async IAsyncEnumerable<BitAiEvent> PlayTool(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();

        if (run.ToolCallsThisTurn.Count == 0)
        {
            var callId = run.CallId(1);
            yield return new BitAiToolCallStartedEvent(callId, E2EAiTools.GetWeather);
            foreach (var slice in E2EAiScriptRun.Slice("""{"city":"Oslo"}""", 5))
            {
                await run.Pause();
                yield return new BitAiToolCallDeltaEvent(callId, slice);
            }
            yield return new BitAiToolCallCompletedEvent(callId);
            yield return new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls);
            yield break;
        }

        var answer = $"The weather: {Describe(run.ToolCallsThisTurn[0])}.";
        await foreach (var e in run.Write(answer)) yield return e;
        yield return run.Usage(answer);
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayToolApproval(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();

        if (run.ToolCallsThisTurn.Count == 0)
        {
            var callId = run.CallId(1);
            yield return new BitAiToolCallStartedEvent(callId, E2EAiTools.DeleteRecords) { Arguments = E2EAiScriptRun.Json("""{"filter":"inactive users"}""") };
            yield return new BitAiToolCallCompletedEvent(callId);
            yield return new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls);
            yield break;
        }

        await run.Pause();
        var call = run.ToolCallsThisTurn[0];
        var answer = call.Status is BitAiToolCallStatus.Rejected || call.Result is { IsSuccess: false }
            ? "Understood. I left the records alone."
            : "Deleted 3 records matching \"inactive users\".";
        await foreach (var e in run.Write(answer)) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayToolParallel(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();

        if (run.ToolCallsThisTurn.Count == 0)
        {
            yield return new BitAiToolCallStartedEvent(run.CallId(1), E2EAiTools.GetWeather) { Arguments = E2EAiScriptRun.Json("""{"city":"Oslo"}""") };
            await run.Pause();
            yield return new BitAiToolCallStartedEvent(run.CallId(2), E2EAiTools.GetWeather) { Arguments = E2EAiScriptRun.Json("""{"city":"Lisbon"}""") };
            yield return new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls);
            yield break;
        }

        var answer = string.Join(" and ", run.ToolCallsThisTurn.Select(Describe)) + ".";
        await foreach (var e in run.Write(answer)) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayToolProgress(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();

        if (run.ToolCallsThisTurn.Count == 0)
        {
            yield return new BitAiToolCallStartedEvent(run.CallId(1), E2EAiTools.SearchDocs) { Arguments = E2EAiScriptRun.Json("""{"query":"render modes"}""") };
            yield return new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls);
            yield break;
        }

        await foreach (var e in run.Write("Two passages of the documentation matched.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayToolError(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();

        if (run.ToolCallsThisTurn.Count == 0)
        {
            yield return new BitAiToolCallStartedEvent(run.CallId(1), E2EAiTools.FlakyService) { Arguments = E2EAiScriptRun.Json("{}") };
            yield return new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls);
            yield break;
        }

        await foreach (var e in run.Write("The service did not answer, so I could not check.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayToolResult(E2EAiScriptRun run)
    {
        var callId = run.CallId(1);

        yield return new BitAiMessageStartedEvent();
        yield return new BitAiToolCallStartedEvent(callId, "web_search") { Arguments = E2EAiScriptRun.Json("""{"query":"bit platform"}""") };
        yield return new BitAiToolCallCompletedEvent(callId);
        await run.Pause();
        yield return new BitAiToolResultEvent(callId, BitAiToolOutcome.Success(E2EAiScriptRun.Json("""{"hits":2,"top":"bitplatform.dev"}"""), "2 results"));
        await foreach (var e in run.Write("The top result is bitplatform.dev.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static string Describe(BitAiToolCallPart call)
    {
        if (call.Result?.Value is { ValueKind: JsonValueKind.Object } value &&
            value.TryGetProperty("city", out var city) && value.TryGetProperty("degrees", out var degrees) && value.TryGetProperty("condition", out var condition))
        {
            return $"{city.GetString()} is {degrees.GetInt32()} degrees and {condition.GetString()}";
        }

        return call.Result?.GetDisplayText() ?? "no result";
    }

    // ---- agents ----

    private static async IAsyncEnumerable<BitAiEvent> PlayAgent(E2EAiScriptRun run)
    {
        var runId = run.RunId;
        string[] steps = ["Search for sources", "Read the sources", "Reconcile them", "Write the summary"];
        string[] summaries = ["3 sources found", "Two agree, one does not", "They differ in scope", "Summary written"];

        yield return new BitAiMessageStartedEvent();
        yield return new BitAiAgentRunStartedEvent(runId) { AgentName = "Research agent", Goal = run.LastUserText };
        yield return new BitAiAgentPlanEvent(runId, [.. steps.Select((title, i) => new BitAiAgentPlanStep($"s{i + 1}", title))]);

        for (var i = 0; i < steps.Length; i++)
        {
            await run.Pause();
            yield return new BitAiAgentStepStartedEvent(runId, $"s{i + 1}");
            await run.Pause();
            yield return new BitAiAgentStepCompletedEvent(runId, $"s{i + 1}") { Summary = summaries[i] };
        }

        yield return new BitAiAgentRunCompletedEvent(runId);
        await foreach (var e in run.Write("The sources agree on direction and differ in scope.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayAgentInput(E2EAiScriptRun run)
    {
        var runId = run.RunId;

        yield return new BitAiMessageStartedEvent();

        if (run.TurnsSincePrompt == 0)
        {
            yield return new BitAiAgentRunStartedEvent(runId) { AgentName = "Migration agent", Goal = "Move the database to the new schema" };
            yield return new BitAiAgentPlanEvent(runId,
            [
                new BitAiAgentPlanStep("s1", "Inspect the schema"),
                new BitAiAgentPlanStep("s2", "Choose a downtime window"),
                new BitAiAgentPlanStep("s3", "Run the migration"),
            ]);
            yield return new BitAiAgentStepStartedEvent(runId, "s1");
            await run.Pause();
            yield return new BitAiAgentStepCompletedEvent(runId, "s1") { Summary = "2 tables need rewriting" };
            yield return new BitAiAgentStepStartedEvent(runId, "s2");
            yield return new BitAiAgentInputRequestedEvent(runId, "When should I take the downtime?")
            {
                Suggestions = ["Tonight", "Saturday morning", "Ask me later"],
            };
            await foreach (var e in run.Write("I need one answer before I go on.")) yield return e;
            yield return new BitAiMessageCompletedEvent(BitAiFinishReason.Stop);
            yield break;
        }

        yield return new BitAiAgentStepCompletedEvent(runId, "s2") { Summary = $"Window: {run.LastUserText}" };
        yield return new BitAiAgentStepStartedEvent(runId, "s3");
        await run.Pause();
        yield return new BitAiAgentStepCompletedEvent(runId, "s3") { Summary = "14 tables migrated" };
        yield return new BitAiAgentRunCompletedEvent(runId);
        await foreach (var e in run.Write($"Booked for {run.LastUserText}.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayAgentFail(E2EAiScriptRun run)
    {
        var runId = run.RunId;

        yield return new BitAiMessageStartedEvent();
        yield return new BitAiAgentRunStartedEvent(runId) { AgentName = "Deploy agent", Goal = "Ship the release" };
        yield return new BitAiAgentPlanEvent(runId, [new BitAiAgentPlanStep("s1", "Build"), new BitAiAgentPlanStep("s2", "Test"), new BitAiAgentPlanStep("s3", "Deploy")]);
        yield return new BitAiAgentStepStartedEvent(runId, "s1");
        await run.Pause();
        yield return new BitAiAgentStepCompletedEvent(runId, "s1") { Summary = "Build succeeded" };
        yield return new BitAiAgentStepStartedEvent(runId, "s2");
        await run.Pause();
        yield return new BitAiAgentStepCompletedEvent(runId, "s2", BitAiAgentStepStatus.Failed)
        {
            Error = new BitAiError("3 of 812 tests failed.") { Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund", IsRetryable = true },
        };
        yield return new BitAiAgentRunCompletedEvent(runId, BitAiAgentRunStatus.Failed);
        await foreach (var e in run.Write("I stopped before deploying: three checkout tests fail.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    // ---- content ----

    private static async IAsyncEnumerable<BitAiEvent> PlayCitations(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in PlayCitedText(run)) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    // The cited sentences, shared by Citations and Everything.
    private static async IAsyncEnumerable<BitAiEvent> PlayCitedText(E2EAiScriptRun run)
    {
        var sources = new[]
        {
            new BitAiCitationSource("src-1", "Blazor render modes")
            {
                Kind = BitAiSourceKind.Web,
                Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
                SiteName = "Microsoft Learn",
                Description = "How interactivity is chosen per component or per page.",
            },
            new BitAiCitationSource("src-2", "SignalR circuit lifetime")
            {
                Kind = BitAiSourceKind.Document,
                Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
                SiteName = "Microsoft Learn",
                Description = "What happens to state when a circuit drops.",
            },
        };

        const string first = "Interactive Server keeps the state of a component on the server.";
        const string second = " If the circuit drops, the state goes with it.";
        const string third = "\n\nThat is the trade the render mode makes.";

        await foreach (var e in run.Write(first)) yield return e;
        yield return new BitAiCitationEvent(new BitAiCitation("cit-1", sources[0]) { StartIndex = 0, EndIndex = first.Length, Quote = "Interactive Server renders on the server." });
        await foreach (var e in run.Write(second)) yield return e;
        yield return new BitAiCitationEvent(new BitAiCitation("cit-2", sources[1]) { StartIndex = first.Length, EndIndex = first.Length + second.Length, Quote = "When a circuit is lost, its state is disposed." });
        await foreach (var e in run.Write(third)) yield return e;
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayArtifact(E2EAiScriptRun run)
    {
        var artifactId = run.ArtifactId;
        var revision = run.TurnsSincePrompt > 0;

        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write(revision ? "Revised: I added the install step." : "Here is a first draft.")) yield return e;
        await foreach (var e in PlayArtifactBody(run, artifactId, BitAiArtifactKind.Document, "Project README", "markdown", revision
            ? "# Bit.BlazorUI.AI\n\nThe AI components of bit BlazorUI.\n\n## Install\n\n`dotnet add package Bit.BlazorUI.AI`\n"
            : "# Bit.BlazorUI.AI\n\nThe AI components of bit BlazorUI.\n", revision ? "Added the install step" : "First draft")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayArtifactCode(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Here is the class.")) yield return e;
        await foreach (var e in PlayArtifactBody(run, run.ArtifactId, BitAiArtifactKind.Code, "Greeter.cs", "csharp",
            "public sealed class Greeter\n{\n    public string Greet(string name) => $\"Hello, {name}!\";\n}\n", "A greeter")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayArtifactBody(E2EAiScriptRun run, string artifactId, BitAiArtifactKind kind, string title, string language, string body, string summary)
    {
        yield return new BitAiArtifactCreatedEvent(artifactId, kind, title) { Language = language };
        foreach (var slice in E2EAiScriptRun.Slice(body, 16))
        {
            await run.Pause();
            yield return new BitAiArtifactDeltaEvent(artifactId, slice);
        }
        yield return new BitAiArtifactCompletedEvent(artifactId) { Summary = summary };
    }

    private const string OptionsJson = """
        [
          {"option":"Server rendering","effort":"Low","risk":"Low"},
          {"option":"WebAssembly","effort":"Medium","risk":"Low"},
          {"option":"Hybrid per page","effort":"Medium","risk":"Medium"},
          {"option":"Rewrite","effort":"High","risk":"High"}
        ]
        """;

    private static async IAsyncEnumerable<BitAiEvent> PlayStructured(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Four options, ranked by effort.\n\n")) yield return e;
        await foreach (var e in PlayStructuredBody(run, OptionsJson, "options")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayForm(E2EAiScriptRun run)
    {
        const string schema = """
            {
              "title": "Book a table",
              "type": "object",
              "required": ["restaurant", "guests"],
              "properties": {
                "restaurant": { "type": "string", "title": "Restaurant" },
                "guests": { "type": "integer", "title": "Guests", "minimum": 1, "maximum": 12 },
                "seating": { "type": "string", "title": "Seating", "enum": ["Inside", "Terrace"] }
              }
            }
            """;

        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Check the details first.\n\n")) yield return e;
        await foreach (var e in PlayStructuredBody(run, schema, E2EAiSetup.FormSchema)) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayStructuredBody(E2EAiScriptRun run, string json, string schemaName)
    {
        foreach (var slice in E2EAiScriptRun.Slice(json, 24))
        {
            await run.Pause();
            yield return new BitAiStructuredDataDeltaEvent(slice) { SchemaName = schemaName };
        }
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayGenerative(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Here are the numbers.\n\n")) yield return e;
        yield return new BitAiGenerativeUiEvent(E2EAiSetup.CardComponent, E2EAiScriptRun.Json("""{"title":"Visitors","value":1200}"""));
        await run.Pause();
        yield return new BitAiGenerativeUiEvent(E2EAiSetup.CardComponent, E2EAiScriptRun.Json("""{"title":"Signups","value":85}"""));
        await foreach (var e in run.Write("\n\nSignups are up.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayGenerativeUnknown(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("A component nobody registered:\n\n")) yield return e;
        yield return new BitAiGenerativeUiEvent("not_registered", E2EAiScriptRun.Json("""{"title":"Nothing"}"""));
        yield return new BitAiMessageCompletedEvent();
    }

    private const string PixelPng = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    private static async IAsyncEnumerable<BitAiEvent> PlayImage(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Here is the chart.\n\n")) yield return e;
        yield return new BitAiImageEvent(PixelPng) { Alt = "A chart of the visitors", Width = 64, Height = 48 };
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayFile(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("I attached the report.\n\n")) yield return e;
        yield return new BitAiFileEvent(new BitAiAttachment("file-1", "report.pdf") { Size = 2048 });
        yield return new BitAiMessageCompletedEvent();
    }

    // ---- errors ----

    private static BitAiError RateLimit() => new("The model is rate limited right now.")
    {
        Code = "rate_limit",
        Kind = BitAiErrorKind.RateLimit,
        IsRetryable = true,
        RetryAfter = TimeSpan.FromSeconds(3),
        Detail = "HTTP 429\nretry-after: 3",
    };

    private static async IAsyncEnumerable<BitAiEvent> PlayError(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Looking that up")) yield return e;
        yield return new BitAiErrorEvent(RateLimit());
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayErrorThenRecover(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();

        if (run.Attempt == 1)
        {
            await foreach (var e in run.Write("Looking that up")) yield return e;
            yield return new BitAiErrorEvent(RateLimit());
            yield break;
        }

        await foreach (var e in run.Write("That worked on the second attempt.")) yield return e;
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayThrow(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await run.Pause();
        throw new InvalidOperationException("The E2E transport was told to throw.");
    }

    // ---- usage, size, time ----

    private static async IAsyncEnumerable<BitAiEvent> PlayUsage(E2EAiScriptRun run)
    {
        const string answer = "This turn ran on the large model.";

        yield return new BitAiMessageStartedEvent();
        yield return new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = E2EAiModels.DefaultModelId, ProviderId = "e2e", ContextWindow = 200_000, Temperature = 0.7 });
        await foreach (var e in run.Write(answer)) yield return e;
        yield return new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 1200, OutputTokens = 300, CachedInputTokens = 400, Cost = 0.0081m, Currency = "USD" });
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayLong(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        for (var i = 1; i <= 30; i++)
        {
            await foreach (var e in run.Write($"Paragraph {i}: a line long enough to wrap once on a narrow transcript and stay on one on a wide one.\n\n", 32)) yield return e;
        }
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayHang(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        await foreach (var e in run.Write("Working on it")) yield return e;
        await Task.Delay(Timeout.Infinite, run.Token);
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayEmpty(E2EAiScriptRun run)
    {
        await Task.CompletedTask;
        yield return new BitAiMessageStartedEvent();
        yield return new BitAiMessageCompletedEvent();
    }

    private static async IAsyncEnumerable<BitAiEvent> PlayEverything(E2EAiScriptRun run)
    {
        yield return new BitAiMessageStartedEvent();
        yield return new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = E2EAiModels.DefaultModelId, ProviderId = "e2e", ContextWindow = 128_000 });
        foreach (var word in "Let me put it all together.".Split(' '))
        {
            await run.Pause();
            yield return new BitAiReasoningDeltaEvent(word + " ");
        }
        yield return new BitAiStatusEvent("Gathering sources");
        yield return new BitAiStatusEvent(null);
        await foreach (var e in PlayCitedText(run)) yield return e;
        await foreach (var e in PlayArtifactBody(run, run.ArtifactId, BitAiArtifactKind.Document, "Notes", "markdown", "# Notes\n\n- Server\n- WebAssembly\n", "Notes")) yield return e;
        await foreach (var e in PlayStructuredBody(run, OptionsJson, "options")) yield return e;
        yield return new BitAiGenerativeUiEvent(E2EAiSetup.CardComponent, E2EAiScriptRun.Json("""{"title":"Visitors","value":1200}"""));
        yield return new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = 900, OutputTokens = 250, Cost = 0.0065m, Currency = "USD" });
        yield return new BitAiMessageCompletedEvent();
    }
}

/// <summary>One play of a script: the request it answers, where its turns started, and the pace it streams at.</summary>
public sealed class E2EAiScriptRun
{
    private readonly IReadOnlyList<string> _userTexts;

    internal E2EAiScriptRun(BitAiRequest request, IReadOnlyList<string> userTexts, int promptIndex, int attempt, int delayMs, CancellationToken token)
    {
        Request = request;
        _userTexts = userTexts;
        PromptIndex = promptIndex;
        Attempt = attempt;
        DelayMs = delayMs;
        Token = token;
    }

    /// <summary>The request being answered.</summary>
    public BitAiRequest Request { get; }

    /// <summary>The index, among the user messages, of the one the script's turns started at.</summary>
    public int PromptIndex { get; }

    /// <summary>How many user messages came after that one: 0 on the script's first turn.</summary>
    public int TurnsSincePrompt => Math.Max(0, _userTexts.Count - 1 - PromptIndex);

    /// <summary>How often the script has been played for this conversation since its turns started, this play included.</summary>
    public int Attempt { get; }

    /// <summary>The pause between events, in milliseconds.</summary>
    public int DelayMs { get; }

    /// <summary>Cancels the turn.</summary>
    public CancellationToken Token { get; }

    /// <summary>The text of the latest user message.</summary>
    public string LastUserText => _userTexts.Count > 0 ? _userTexts[^1] : string.Empty;

    /// <summary>The id of the agent run the script's turns drive, the same on each of them.</summary>
    public string RunId => $"run-{PromptIndex + 1}";

    /// <summary>The id of the artifact the script's turns write, the same on each of them (a revision is a new version).</summary>
    public string ArtifactId => $"artifact-{PromptIndex + 1}";

    /// <summary>The id of the nth tool call of this turn.</summary>
    public string CallId(int n) => $"call-{_userTexts.Count}-{n}";

    /// <summary>The tool calls the assistant made since the latest user message, the first first.</summary>
    public IReadOnlyList<BitAiToolCallPart> ToolCallsThisTurn
    {
        get
        {
            var calls = new List<BitAiToolCallPart>();
            for (var i = Request.Messages.Count - 1; i >= 0 && Request.Messages[i].Role.IsUser is false; i--)
            {
                calls.InsertRange(0, Request.Messages[i].Parts.OfType<BitAiToolCallPart>());
            }
            return calls;
        }
    }

    /// <summary>The pause between two events: none unless the page asked for one.</summary>
    public Task Pause() => DelayMs > 0 ? Task.Delay(DelayMs, Token) : Task.CompletedTask;

    /// <summary>Streams text a few characters at a time.</summary>
    public async IAsyncEnumerable<BitAiEvent> Write(string text, int chunk = 4)
    {
        foreach (var slice in Slice(text, chunk))
        {
            await Pause();
            Token.ThrowIfCancellationRequested();
            yield return new BitAiTextDeltaEvent(slice);
        }
    }

    /// <summary>The usage of a reply, sized by its text and the request's.</summary>
    public BitAiUsageUpdatedEvent Usage(string output)
    {
        var input = Request.Messages.Sum(m => m.GetText().Length) / 4;
        var generated = output.Length / 4;
        return new BitAiUsageUpdatedEvent(new BitAiTokenUsage { InputTokens = input, OutputTokens = generated, Cost = (input * 3m + generated * 15m) / 1_000_000m, Currency = "USD" });
    }

    /// <summary>Cuts a value into slices of a fixed size.</summary>
    public static IEnumerable<string> Slice(string value, int size)
    {
        for (var i = 0; i < value.Length; i += size) yield return value.Substring(i, Math.Min(size, value.Length - i));
    }

    /// <summary>A JSON literal as a value that outlives its document.</summary>
    public static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
