namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The scripted responses behind the content scenarios: citations, artifacts and structured output.
/// </summary>
public static class AiDemoContentTransports
{
    /// <summary>
    /// An answer with inline citations anchored to the sentences they support.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Cited() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var sources = new[]
            {
                new BitAiCitationSource("s1", "Blazor render modes")
                {
                    Kind = BitAiSourceKind.Web,
                    Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
                    SiteName = "Microsoft Learn",
                    Description = "How interactivity is chosen per component or per page.",
                },
                new BitAiCitationSource("s2", "SignalR circuit lifetime")
                {
                    Kind = BitAiSourceKind.Document,
                    Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
                    SiteName = "Microsoft Learn",
                    Description = "What happens to state when a circuit drops.",
                },
            };

            await yielder(new BitAiMessageStartedEvent());
            await yielder(new BitAiStatusEvent("Searching the documentation"));
            await AiScript.Pause(700, token);
            await yielder(new BitAiStatusEvent(null));

            const string first =
                "Interactive Server keeps component state on the server and streams UI diffs over a " +
                "SignalR circuit.";
            const string second =
                " If that circuit drops, the state goes with it unless you have persisted it yourself.";
            const string third =
                "\n\nThat is the trade the render mode makes: a fast first paint, in exchange for a " +
                "dependency on a live connection.";

            await foreach (var evt in AiScript.Write(first, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiCitationEvent(new BitAiCitation("c1", sources[0])
            {
                StartIndex = 0,
                EndIndex = first.Length,
                Quote = "Interactive Server renders the component on the server and sends UI updates over a circuit.",
            }));

            await foreach (var evt in AiScript.Write(second, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiCitationEvent(new BitAiCitation("c2", sources[1])
            {
                StartIndex = first.Length,
                EndIndex = first.Length + second.Length,
                Quote = "When a circuit is lost, the associated component state is disposed.",
            }));

            await foreach (var evt in AiScript.Write(third, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Writes a document artifact, then revises it on the next turn.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Artifacts() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            const string artifactId = "art-readme";
            var isRevision = request.Messages.Count(m => m.Role.IsUser) > 1;

            await yielder(new BitAiMessageStartedEvent());

            var intro = isRevision
                ? "Revised. I tightened the opening and added the install step."
                : "Here is a first draft. Tell me what to change, and I will revise it.";

            await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

            await yielder(new BitAiArtifactCreatedEvent(artifactId, BitAiArtifactKind.Document, "Project README")
            {
                Language = "markdown",
            });

            var body = isRevision
                ? """
                  # Bit.BlazorUI.AI

                  The AI UI layer of bit BlazorUI: conversations, streaming, tools, agents, and artifacts.

                  ## Install

                  ```bash
                  dotnet add package Bit.BlazorUI.AI
                  ```

                  ## Why

                  Chat is the easy part. What takes the time is everything around it: partial JSON,
                  tool approval, citation anchoring, and not re-rendering the world on every token.
                  """
                : """
                  # Bit.BlazorUI.AI

                  The AI components of bit BlazorUI.

                  ## Why

                  Chat is the easy part. Everything around it is what takes the time.
                  """;

            foreach (var slice in AiScript.Slice(body, 24))
            {
                await AiScript.Pause(45, token);
                await yielder(new BitAiArtifactDeltaEvent(artifactId, slice));
            }

            await yielder(new BitAiArtifactCompletedEvent(artifactId)
            {
                Summary = isRevision ? "Tightened the opening, added install" : "First draft",
            });

            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Streams a structured value, so a table fills in row by row.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Structured() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            await yielder(new BitAiMessageStartedEvent());

            const string intro = "Four candidates, ranked by how much work each one would be.\n\n";
            await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

            const string json = """
                [
                  {"option":"Server-side rendering","effort":"Low","risk":"Low","notes":"Fastest to ship"},
                  {"option":"WebAssembly","effort":"Medium","risk":"Low","notes":"Best offline story"},
                  {"option":"Hybrid per page","effort":"Medium","risk":"Medium","notes":"Two mental models"},
                  {"option":"Rewrite the front end","effort":"High","risk":"High","notes":"Not this quarter"}
                ]
                """;

            foreach (var slice in AiScript.Slice(json, 14))
            {
                await AiScript.Pause(70, token);
                await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "options" });
            }

            await yielder(new BitAiMessageCompletedEvent());
        }));
}
