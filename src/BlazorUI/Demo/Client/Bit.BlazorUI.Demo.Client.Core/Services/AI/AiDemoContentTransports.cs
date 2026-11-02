namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The scripted responses behind the content scenarios: citations, artifacts, structured output and attachments.
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
    /// Writes a document artifact, then revises it on every later turn.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Artifacts() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            const string artifactId = "art-readme";

            // Every turn after the first is a revision, numbered in the body, so no version comes out the same as the one
            // before it.
            var revision = request.Messages.Count(m => m.Role.IsUser) - 1;
            var isRevision = revision > 0;

            await yielder(new BitAiMessageStartedEvent());

            var intro = revision switch
            {
                0 => "Here is a first draft. Tell me what to change, and I will revise it.",
                1 => "Revised. I tightened the opening and added the install step.",
                _ => $"Revised again, into revision {revision}.",
            };

            await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

            await yielder(new BitAiArtifactCreatedEvent(artifactId, BitAiArtifactKind.Document, "Project README")
            {
                Language = "markdown",
            });

            var body = isRevision
                ? $"""
                  # Bit.BlazorUI.AI

                  The AI UI layer of bit BlazorUI: conversations, streaming, tools, agents, and artifacts.

                  ## Install

                  ```bash
                  dotnet add package Bit.BlazorUI.AI
                  ```

                  ## Why

                  Chat is the easy part. What takes the time is everything around it: partial JSON,
                  tool approval, citation anchoring, and not re-rendering the world on every token.

                  _Revision {revision}._
                  """
                : """
                  # Bit.BlazorUI.AI

                  The AI components of bit BlazorUI.

                  ## Why

                  Chat is the easy part. Everything around it is what takes the time.
                  """;

            foreach (var slice in AiScript.Slice(body, 12))
            {
                await AiScript.Pause(60, token);
                await yielder(new BitAiArtifactDeltaEvent(artifactId, slice));
            }

            await yielder(new BitAiArtifactCompletedEvent(artifactId)
            {
                Summary = revision switch
                {
                    0 => "First draft",
                    1 => "Tightened the opening, added install",
                    _ => $"Revision {revision}",
                },
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

    /// <summary>
    /// Answers in the shape the request asks for (<see cref="BitAiRequestOptions.ResponseFormat"/>): with a JSON format,
    /// the comparison streams as structured data named after the requested schema; with none, as a Markdown table.
    /// </summary>
    /// <returns>The transport.</returns>
    /// <remarks>
    /// What a real transport does with the format is hand it to its provider, which constrains the model to it. The
    /// name of the format becomes the <see cref="BitAiStructuredDataDeltaEvent.SchemaName"/> of the part, which is what
    /// picks a renderer registered for that schema.
    /// </remarks>
    public static IBitAiTransport StructuredAsRequested() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var format = request.Options?.ResponseFormat ?? BitAiResponseFormat.Text;

            await yielder(new BitAiMessageStartedEvent());

            if (format.Kind is BitAiResponseFormatKind.Text)
            {
                const string text = """
                    No response format was asked for, so here is the comparison as text.

                    | Option | Effort | Risk | Notes |
                    | --- | --- | --- | --- |
                    | Server-side rendering | Low | Low | Fastest to ship |
                    | WebAssembly | Medium | Low | Best offline story |
                    | Hybrid per page | Medium | Medium | Two mental models |
                    | Rewrite the front end | High | High | Not this quarter |
                    """;

                await foreach (var evt in AiScript.Write(text, 5, 10, token)) await yielder(evt);
                await yielder(AiScript.Usage(request, text));
                await yielder(new BitAiMessageCompletedEvent());
                return;
            }

            var intro = $"Asked for {(format.Kind is BitAiResponseFormatKind.JsonSchema ? "JSON matching a schema" : "JSON")}, so here are the four candidates as data.\n\n";
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
                await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = format.Name });
            }

            await yielder(AiScript.Usage(request, json));
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Reads the files of the user's message, the way a transport hands them to its provider, and sends them back the way
    /// a model returns a file it made: an image as an image part, anything else as a file part.
    /// </summary>
    /// <returns>The transport.</returns>
    /// <remarks>
    /// The files arrive on the message (<see cref="IBitAiMessage.Attachments"/>): a small one with its content inline - the
    /// bytes and a data URL - and a larger one with a <see cref="BitAiAttachment.ContentFactory"/> that reads it from the
    /// browser when opened. A model that generates an image or a file streams it back with a <see cref="BitAiImageEvent"/>
    /// or a <see cref="BitAiFileEvent"/>, as this does with the files it was given.
    /// </remarks>
    public static IBitAiTransport ReadsAttachments() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var said = AiScript.LastUserMessage(request);
            IReadOnlyList<BitAiAttachment> files = said is null ? [] : said.Attachments;

            await yielder(new BitAiMessageStartedEvent());

            if (files.Count == 0)
            {
                var nothing = $"You said: {said?.GetText()}\n\nAttach a file as well, and I will read it and send it back.";
                await foreach (var evt in AiScript.Write(nothing, 4, 12, token)) await yielder(evt);
                await yielder(AiScript.Usage(request, nothing));
                await yielder(new BitAiMessageCompletedEvent());
                return;
            }

            // A small file arrives with its bytes; a larger one is read from the browser when something opens it.
            var lines = files.Select(f =>
                $"- **{f.Name}**: {f.ContentType ?? "an unknown type"}, " +
                $"{(f.Size is { } size ? $"{size / 1024.0:0.#} KB" : "size unknown")}, " +
                (f.HasInlineData ? "sent inline" : "read on demand"));

            var reply = (files.Count == 1 ? "I received 1 file:" : $"I received {files.Count} files:") + "\n\n" +
                        string.Join("\n", lines) + "\n\n" +
                        "Here " + (files.Count == 1 ? "it is" : "they are") + " back, the way a model returns a file it made:";

            await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);

            foreach (var file in files)
            {
                await AiScript.Pause(250, token);

                // An image is drawn in the answer - from its data URL, or from the browser's own preview of a file too
                // large to hold inline; anything else is offered as a file.
                if (file.Kind is BitAiAttachmentKind.Image && (file.Url ?? file.ThumbnailUrl) is { Length: > 0 } url)
                {
                    await yielder(new BitAiImageEvent(url) { Alt = file.Name });
                }
                else
                {
                    await yielder(new BitAiFileEvent(file));
                }
            }

            await yielder(AiScript.Usage(request, reply));
            await yielder(new BitAiMessageCompletedEvent());
        }));
}
